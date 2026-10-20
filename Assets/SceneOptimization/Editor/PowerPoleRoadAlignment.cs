using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Editor-only extension of the existing pole placer. Never destroys/reorders poles.
[InitializeOnLoad]
public static class PowerPoleRoadAlignment
{
    const string Request = "Library/PowerPoleAlignment.request";
    const string Pointer = "Library/PowerPoleAlignmentBackup.path";
    [Serializable] sealed class Pose { public string id; public Vector3 position; public Quaternion rotation; public Vector3 scale; }
    [Serializable] sealed class Snapshot { public Pose[] poses; public string line; }
    sealed class Move { public Transform pole; public Renderer shaft; public Vector3 before, after; public Quaternion rotation; public Vector3 scale; public Transform parent; public float clearance; }
    sealed class Rail { public Bounds bounds; public Vector3[] vertices; }
    static PowerPoleRoadAlignment() { EditorApplication.update += Poll; }
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static T[] InScene<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Where(c => c.gameObject.scene == SceneManager.GetActiveScene()).ToArray();
    static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || StaticOcclusionCulling.isRunning || !File.Exists(Request)) return;
        string mode = File.ReadAllText(Request).Trim(); File.Delete(Request);
        try
        {
            if (mode == "APPLY") FoggyRoadTools.Direk.DirekAutoPlacer.MevcutDirekleriYolaUyarla(InScene<MeshFilter>().Single(m => m.name == "NurbsPath_ROAD").gameObject);
            else if (mode == "VIEW")
            {
                var pole = InScene<Transform>().First(t => t.name == "Direk_050");
                Selection.activeGameObject = pole.gameObject;
                SceneView.lastActiveSceneView.LookAt(pole.position + Vector3.up * 4, Quaternion.Euler(25, 60, 0), 32);
                SceneView.RepaintAll();
            }
        }
        catch (Exception e) { File.WriteAllText("Recovery/PowerPoleAlignmentError.txt", e.ToString()); Debug.LogException(e); }
    }
    static Renderer Shaft(Transform pole)
    {
        var candidates = pole.GetComponentsInChildren<Renderer>(true).Where(r => r.name == "Cylinder").ToArray();
        Require(candidates.Length == 1, "Expected exactly one Cylinder shaft: " + pole.name);
        var shaft = candidates[0];
        Require(shaft.bounds.size.y > 4 && shaft.bounds.size.x < 3 && shaft.bounds.size.z < 3, "Unrecognized pole shaft bounds: " + pole.name + " " + shaft.bounds);
        return shaft;
    }
    static float Ground(Vector3 point, Terrain[] terrains)
    {
        var matches = terrains.Where(t => { var p = t.transform.position; var s = t.terrainData.size; return point.x >= p.x && point.x <= p.x + s.x && point.z >= p.z && point.z <= p.z + s.z; }).ToArray();
        Require(matches.Length > 0, "No Terrain under pole at " + point);
        return matches.Max(t => t.SampleHeight(point) + t.transform.position.y);
    }
    public static string Align(GameObject road, float clearance)
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode && !StaticOcclusionCulling.isRunning, "Stop Play Mode/bake first");
        Require(road && road.name == "NurbsPath_ROAD" && road.scene == SceneManager.GetActiveScene(), "Select the current NurbsPath_ROAD");
        Require(clearance >= .5f && clearance <= 4, "Invalid barrier clearance");
        var scene = road.scene; var field = new RoadWidthGeometry(road.GetComponent<MeshFilter>().sharedMesh, road.transform);
        var line = InScene<FoggyRoad_PowerLine>().Single(l => l.transform.parent && l.transform.parent.name == "ElektrikDirekleri_Auto");
        var poles = line.poles.ToArray(); Require(poles.Length > 1 && poles.All(p => p) && poles.Distinct().Count() == poles.Length, "Invalid ordered pole list");
        var terrains = InScene<Terrain>().Where(t => t.terrainData && t.GetComponent<TerrainCollider>()).ToArray();
        var rails = InScene<MeshFilter>().Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.name, @"^GRPRO_[LR]_\d+_")).Select(m => new Rail { bounds = m.GetComponent<Renderer>().bounds, vertices = m.sharedMesh.vertices.Select(v => m.transform.TransformPoint(v)).ToArray() }).ToArray();
        Require(rails.Length > 0, "Guardrail geometry missing");
        var protectedTransforms = InScene<Transform>().Where(t => !poles.Contains(t)).ToDictionary(t => t, t => (t.position, t.rotation, t.localScale, t.parent));
        var moves = new List<Move>();
        foreach (var pole in poles)
        {
            var shaft = Shaft(pole); var b = shaft.bounds; var point = b.center;
            Require(field.Query(point, out var q), "Pole outside road field: " + pole.name);
            float sign = q.side >= 0 ? 1 : -1;
            float radius = Mathf.Abs(q.across.x) * b.extents.x + Mathf.Abs(q.across.z) * b.extents.z;
            var tangent = new Vector3(-q.across.z, 0, q.across.x);
            float edge = q.half + .75f;
            foreach (var rail in rails.Where(r => r.bounds.SqrDistance(point) < 1600))
                foreach (var v in rail.vertices)
                    if (Mathf.Abs(Vector3.Dot(v - q.center, tangent)) <= 4)
                    {
                        float side = sign * Vector3.Dot(v - q.center, q.across);
                        if (side < q.half + 8) edge = Mathf.Max(edge, side);
                    }
            float delta = Mathf.Max(0, edge + clearance + radius - Mathf.Abs(q.side));
            var after = pole.position;
            if (delta > .01f)
            {
                after += q.across * sign * delta;
                // Avoid planting the shaft directly into existing Terrain tree trunks.
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    var center = point + after - pole.position;
                    bool conflict = terrains.Any(t => t.terrainData.treeInstances.Any(tree => {
                        var wp = t.transform.position + Vector3.Scale(tree.position, t.terrainData.size);
                        return new Vector2(wp.x - center.x, wp.z - center.z).sqrMagnitude < Mathf.Pow(.65f + radius, 2);
                    }));
                    if (!conflict) break;
                    after += q.across * sign * .4f;
                    Require(attempt < 11, "Cannot clear tree trunks: " + pole.name);
                }
                var basePoint = point + after - pole.position;
                after.y = Ground(basePoint, terrains) - .2f - (b.min.y - pole.position.y);
            }
            Require(field.Query(point + after - pole.position, out var check) && Mathf.Abs(check.side) - radius > check.half + .5f, "Pole remains in road: " + pole.name);
            moves.Add(new Move { pole = pole, shaft = shaft, before = pole.position, after = after, rotation = pole.rotation, scale = pole.localScale, parent = pole.parent, clearance = Mathf.Abs(check.side) - radius - edge });
        }
        string backup = "Recovery/PowerPoleAlignment_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"); Directory.CreateDirectory(backup);
        Require(EditorSceneManager.SaveScene(scene, backup + "/Before.unity", true), "Scene backup failed");
        File.WriteAllText(Pointer, backup);
        File.WriteAllText(backup + "/Poses.json", JsonUtility.ToJson(new Snapshot { poses = moves.Select(m => new Pose { id = GlobalObjectId.GetGlobalObjectIdSlow(m.pole).ToString(), position = m.before, rotation = m.rotation, scale = m.scale }).ToArray(), line = EditorJsonUtility.ToJson(line) }, true));
        foreach (var path in AssetDatabase.GetDependencies(scene.path).Where(p => p.EndsWith("OcclusionCullingData.asset"))) File.Copy(path, backup + "/Occlusion.before", true);
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Align existing power poles with widened road");
        Undo.RecordObjects(poles.Cast<Object>().Concat(new Object[] { line }).ToArray(), "Align poles and connected cables");
        try
        {
            foreach (var m in moves.Where(m => m.after != m.before))
            {
                m.pole.position = m.after; PrefabUtility.RecordPrefabInstancePropertyModifications(m.pole);
            }
            line.Rebuild(); Physics.SyncTransforms();
            Require(line.poles.SequenceEqual(poles), "Pole connection order changed");
            Require(line.CurrentMesh && line.CurrentMesh.vertexCount > 0 && line.CurrentMesh.vertices.All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)), "Invalid rebuilt cable mesh");
            foreach (var m in moves) Require(m.pole.rotation == m.rotation && m.pole.localScale == m.scale && m.pole.parent == m.parent, "Pole orientation/scale/parent changed");
            // Child transforms follow their pole naturally; unrelated scene objects must remain untouched.
            foreach (var item in protectedTransforms.Where(kv => !poles.Any(p => kv.Key.IsChildOf(p))))
                Require(item.Key.position == item.Value.Item1 && item.Key.rotation == item.Value.Item2 && item.Key.localScale == item.Value.Item3 && item.Key.parent == item.Value.Item4, "Unrelated transform changed: " + item.Key.name);
            RoadWidthInstallation.ValidateInstalled();
            var report = new StringBuilder(); report.AppendLine("PASS: existing pole alignment / cable rebuild");
            report.AppendLine($"Poles retained: {poles.Length}; moved: {moves.Count(m => m.after != m.before)}; cable vertices: {line.CurrentMesh.vertexCount}");
            report.AppendLine($"Minimum lateral shaft-to-barrier envelope clearance: {moves.Min(m => m.clearance):F3}m; requested: {clearance:F2}m");
            report.AppendLine("Ordered pole references, anchors, wire settings, rotation/scale/parent preserved. No road/Terrain/tree/rail edits.");
            foreach (var m in moves) report.AppendLine($"{m.pole.name}: {m.before:F3} -> {m.after:F3}; clearance {m.clearance:F3}m");
            File.WriteAllText(backup + "/Report.txt", report.ToString());
            EditorUtility.SetDirty(line); EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undo);
            RoadWidthInstallation.RebuildOcclusionForGeometryChange();
            Debug.Log("POWER_POLES_ALIGNED: " + backup);
            return $"{poles.Length} direk korundu, {moves.Count(m => m.after != m.before)} tasindi. Kablolar yenilendi. Occlusion bake baslatildi. Yedek: {backup}";
        }
        catch
        {
            foreach (var m in moves) { m.pole.position = m.before; PrefabUtility.RecordPrefabInstancePropertyModifications(m.pole); }
            line.Rebuild(); throw;
        }
    }
}
