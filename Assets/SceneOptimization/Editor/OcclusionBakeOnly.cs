using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Rebakes visibility only; does not reinstall foliage, LODs, lighting or materials.</summary>
[InitializeOnLoad]
public static class OcclusionBakeOnly
{
    const string Request = "Library/OcclusionBakeOnly.request", Pending = "Library/OcclusionBakeOnly.pending.json";
    const string Status = "Library/OcclusionBakeOnly.status", Root = "Occlusion Bake Coverage";
    [Serializable] public sealed class Fingerprint { public string path, hash; }
    [Serializable] public sealed class Session
    {
        public string backup, scene, started, transforms, colliders, glass;
        public List<Fingerprint> protectedAssets = new List<Fingerprint>();
    }
    static bool sawRunning;
    static EditorWindow view;
    static PropertyInfo selectedSize;
    static int previousSize = -1;
    static double readyAt;
    static OcclusionBakeOnly() { EditorApplication.update += Poll; }
    static T[] Objects<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .Where(x => x.gameObject.scene == EditorSceneManager.GetActiveScene()).ToArray();
    static SceneOptimizationController Manager() => Objects<SceneOptimizationController>().Single();
    static string Hash(string path) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
    static string Id(Object o) => o == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
    static string Transforms() => string.Join("\n", Objects<Transform>().Where(t => t.name != Root).OrderBy(t => Id(t)).Select(t =>
        Id(t) + "|" + Id(t.parent) + "|" + t.localPosition.ToString("R") + "|" + t.localRotation.ToString("R") + "|" + t.localScale.ToString("R") + "|" + t.gameObject.activeSelf));
    static string Components<T>() where T : Component => string.Join("\n", Objects<T>().OrderBy(x => Id(x)).Select(x => Id(x) + EditorJsonUtility.ToJson(x)));
    static string Glass() => string.Join("\n", Objects<GasStationPlanarGlass>().OrderBy(x => Id(x)).Select(x => Id(x) + EditorJsonUtility.ToJson(x)));
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        // Console Error Pause can otherwise stop the probe on unrelated connection errors.
        if (EditorApplication.isPlaying && EditorApplication.isPaused && Object.FindFirstObjectByType<CullingValidationProbe>() != null)
            EditorApplication.isPaused = false;
        if (previousSize >= 0 && Object.FindFirstObjectByType<CullingValidationProbe>() == null && !File.Exists(Request))
        { selectedSize.SetValue(view, previousSize); previousSize = -1; }
        if (File.Exists(Pending) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (StaticOcclusionCulling.isRunning) { sawRunning = true; return; }
            var session = JsonUtility.FromJson<Session>(File.ReadAllText(Pending));
            if (!sawRunning && (DateTime.UtcNow - DateTime.Parse(session.started, null, System.Globalization.DateTimeStyles.RoundtripKind)).TotalSeconds < 10) return;
            try { Finish(session); File.Delete(Pending); }
            catch (Exception ex) { Fail(ex); if (File.Exists(Pending + ".failed")) File.Delete(Pending + ".failed"); File.Move(Pending, Pending + ".failed"); }
            return;
        }
        if (!File.Exists(Request)) return;
        string command = File.ReadAllText(Request).Trim();
        try
        {
            if (command.StartsWith("MEASURE_", StringComparison.Ordinal))
            {
                if (!EditorApplication.isPlaying) { EditorApplication.isPlaying = true; return; }
                if (Object.FindFirstObjectByType<CullingValidationProbe>() != null) return;
                if (previousSize < 0) { PrepareView(); return; }
                if (EditorApplication.timeSinceStartup < readyAt) return;
                File.Delete(Request);
                var probe = new GameObject("Temporary culling validation").AddComponent<CullingValidationProbe>();
                probe.Begin(Objects<PlayerInteraction>().Single().GetComponent<Camera>(), command.Substring(8));
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            if (command == "BAKE") Bake();
            else if (command == "AUDIT") Audit();
            else throw new InvalidOperationException("Unknown occlusion-only command: " + command);
        }
        catch (Exception ex) { if (File.Exists(Request)) File.Delete(Request); Fail(ex); }
    }
    static void Fail(Exception ex) { File.WriteAllText(Status, "FAIL: " + ex); Debug.LogException(ex); }
    [MenuItem("Tools/Foggy Road/Scene Optimization/Rebake Occlusion Only")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || StaticOcclusionCulling.isRunning) throw new InvalidOperationException("Stop Play/bake first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/game prototype scene.unity") throw new InvalidOperationException("Open the gameplay scene.");
        var manager = Manager();
        var session = new Session { scene = scene.path, backup = "Recovery/OcclusionOnly_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") };
        Directory.CreateDirectory(session.backup);
        if (!EditorSceneManager.SaveScene(scene, session.backup + "/OpenSceneBefore.unity", true)) throw new IOException("Scene backup failed.");
        AssetDatabase.SaveAssets();
        foreach (string path in AssetDatabase.GetDependencies(scene.path).Where(x => x.EndsWith("OcclusionCullingData.asset")))
        { File.Copy(path, session.backup + "/OcclusionCullingData.before", true); File.Copy(path + ".meta", session.backup + "/OcclusionCullingData.before.meta", true); }
        var protectedPaths = Objects<Terrain>().Where(t => t.terrainData).Select(t => AssetDatabase.GetAssetPath(t.terrainData))
            .Concat(Objects<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m).Select(AssetDatabase.GetAssetPath))
            .Concat(new[] { AssetDatabase.GetAssetPath(manager.profile), "Assets/Settings/PC_RPAsset.asset" }).Where(File.Exists).Distinct();
        foreach (string path in protectedPaths) session.protectedAssets.Add(new Fingerprint { path = path, hash = Hash(path) });
        session.transforms = Transforms(); session.colliders = Components<Collider>(); session.glass = Glass();
        File.WriteAllText(session.backup + "/StaticBefore.tsv", string.Join("\n", Objects<Transform>().Select(t => Id(t.gameObject) + "\t" + t.name + "\t" + (int)GameObjectUtility.GetStaticEditorFlags(t.gameObject))));
        var report = new StringBuilder(); int walls = 0;
        var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        bool gpuDrawer = new SerializedObject(pipeline).FindProperty("m_GPUResidentDrawerMode").intValue != 0;
        foreach (Transform t in Objects<Transform>())
        {
            GameObject go = t.gameObject;
            var flags = GameObjectUtility.GetStaticEditorFlags(go); var next = flags;
            Renderer renderer = go.GetComponent<Renderer>();
            if (IsMoving(t)) next &= ~(StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            else if (IsMountain(t)) next &= ~(StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            else if (renderer)
            {
                bool foliage = IsFoliage(renderer);
                next &= ~StaticEditorFlags.OccluderStatic;
                if (flags != 0 && !foliage) next |= StaticEditorFlags.OccludeeStatic;
                if (IsWall(renderer)) { next |= StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic; walls++; }
                // Static batching takes precedence over the already enabled GPU Resident Drawer.
                if (gpuDrawer && renderer is MeshRenderer && !renderer.HasPropertyBlock() &&
                    renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials.All(m => m && m.shader && m.shader.isSupported &&
                    (m.shader.name.StartsWith("Universal Render Pipeline/") || AssetDatabase.GetAssetPath(m.shader).StartsWith("Assets/SceneOptimization/Generated/Shaders/"))))
                    next &= ~StaticEditorFlags.BatchingStatic;
            }
            var terrain = go.GetComponent<Terrain>();
            if (terrain && !terrain.drawHeightmap) next &= ~(StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            if (next != flags)
            {
                Undo.RecordObject(go, "Prepare occlusion visibility flags"); GameObjectUtility.SetStaticEditorFlags(go, next);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
                report.AppendLine(Id(go) + "\t" + go.name + "\t" + flags + " -> " + next);
            }
        }
        if (walls == 0) throw new InvalidOperationException("No opaque static architecture found.");
        EnsureCoverage(report);
        StaticOcclusionCulling.smallestOccluder = 2f; StaticOcclusionCulling.smallestHole = .25f; StaticOcclusionCulling.backfaceThreshold = 100f;
        manager.occlusionReady = false; EditorUtility.SetDirty(manager);
        foreach (Camera camera in Objects<Camera>().Where(c => c.GetComponent<PlayerInteraction>() || c.GetComponent<DistantMountainBackdrop>()))
        { camera.useOcclusionCulling = false; EditorUtility.SetDirty(camera); }
        report.AppendLine("Opaque architecture=" + walls + "; no lighting/material/density/range changes.");
        File.WriteAllText(session.backup + "/Changes.tsv", report.ToString());
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        session.started = DateTime.UtcNow.ToString("O");
        File.WriteAllText(Pending, JsonUtility.ToJson(session, true)); File.WriteAllText("Library/OcclusionBakeOnly.backup", session.backup);
        sawRunning = false; File.WriteAllText(Status, "BAKING: " + session.backup);
        StaticOcclusionCulling.GenerateInBackground();
    }
    public static bool IsMountain(Transform t)
    {
        for (; t; t = t.parent)
            if (t.gameObject.layer == LayerMask.NameToLayer("DistantMountains") || t.name.StartsWith("SkyboxMountains", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    public static bool IsMoving(Transform t)
    {
        Transform target = t;
        foreach (var g in Objects<GeneratorManager>()) if (g.IsLever(target)) return true;
        for (; t; t = t.parent)
        {
            if (t.gameObject.layer == LayerMask.NameToLayer("pickable") || t.CompareTag("Car") || t.CompareTag("NPC")) return true;
            foreach (Component c in t.GetComponents<Component>())
                if (c && (c is Rigidbody || c is Animator || c is Animation || c is TwoSidedDoor || c is CarController || c is CharacterController ||
                    c.GetType().Name == "ReflectionActor" || c.GetType().Name == "NavMeshAgent")) return true;
        }
        return false;
    }
    public static bool IsFoliage(Renderer r) => r.sharedMaterials.Any(m => m && m.shader &&
        (m.shader.name.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0 || m.shader.name.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0 ||
         m.shader.name.IndexOf("Foliage", StringComparison.OrdinalIgnoreCase) >= 0));
    public static bool IsWall(Renderer r)
    {
        if (IsMoving(r.transform) || IsMountain(r.transform) || IsFoliage(r) || !r.gameObject.isStatic || !(r is MeshRenderer) ||
            r.GetComponent<GlassSurfaceVariation>() || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly || r.sharedMaterials.Length == 0 ||
            r.sharedMaterials.Any(m => !m || m.renderQueue >= 2450 || (m.HasProperty("_Surface") && m.GetFloat("_Surface") > .5f))) return false;
        string n = r.name.ToLowerInvariant();
        return new[] { "duvar", "wall", "roof", "ceiling", "floor", "kolon", "pillar", "concrete", "zemin", "door frame", "doorframe" }.Any(n.Contains);
    }
    static void EnsureCoverage(StringBuilder report)
    {
        var terrains = Objects<Terrain>().Where(t => t.drawHeightmap && t.terrainData).ToArray();
        if (terrains.Length == 0) throw new InvalidOperationException("No playable Terrain for occlusion coverage.");
        Bounds bounds = terrains[0].terrainData.bounds; bounds.center += terrains[0].transform.position;
        foreach (var t in terrains.Skip(1)) { Bounds b = t.terrainData.bounds; b.center += t.transform.position; bounds.Encapsulate(b); }
        foreach (var r in Objects<Renderer>().Where(IsWall)) bounds.Encapsulate(r.bounds);
        bounds.Encapsulate(Objects<PlayerInteraction>().Single().transform.position); bounds.Expand(new Vector3(4, 12, 4));
        bool covered = Objects<OcclusionArea>().Any(a => new Bounds(a.transform.TransformPoint(a.center), Vector3.Scale(a.size, a.transform.lossyScale)).Contains(bounds.min) &&
            new Bounds(a.transform.TransformPoint(a.center), Vector3.Scale(a.size, a.transform.lossyScale)).Contains(bounds.max));
        if (!covered)
        {
            var go = new GameObject(Root); Undo.RegisterCreatedObjectUndo(go, "Cover playable camera positions");
            var area = go.AddComponent<OcclusionArea>(); area.center = bounds.center; area.size = bounds.size;
            var settings = new SerializedObject(area); var volume = settings.FindProperty("m_IsViewVolume");
            if (volume != null) { volume.boolValue = true; settings.ApplyModifiedProperties(); }
            report.AppendLine("Added playable occlusion view volume: " + bounds);
        }
    }
    static void Finish(Session session)
    {
        var manager = Manager();
        string path = AssetDatabase.GetDependencies(session.scene).Single(x => x.EndsWith("OcclusionCullingData.asset"));
        if (File.GetLastWriteTimeUtc(path) < DateTime.Parse(session.started, null, System.Globalization.DateTimeStyles.RoundtripKind) || new FileInfo(path).Length < 1024 ||
            !AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("Fresh bake asset is missing; occlusion remains disabled.");
        foreach (var asset in session.protectedAssets) if (Hash(asset.path) != asset.hash) throw new InvalidOperationException("Protected asset changed: " + asset.path);
        if (Transforms() != session.transforms || Components<Collider>() != session.colliders || Glass() != session.glass)
            throw new InvalidOperationException("Protected transforms, physics or glass changed.");
        manager.occlusionReady = true; EditorUtility.SetDirty(manager);
        foreach (var c in Objects<Camera>().Where(c => c.GetComponent<PlayerInteraction>() || c.GetComponent<DistantMountainBackdrop>()))
        { c.useOcclusionCulling = c.GetComponent<DistantMountainBackdrop>() == null; EditorUtility.SetDirty(c); }
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(manager.gameObject.scene);
        string result = "PASS: fresh native occlusion bake=" + path + "; bytes=" + new FileInfo(path).Length +
            "; native memory=" + StaticOcclusionCulling.umbraDataSize + "; protected assets/transforms/colliders/glass unchanged; main camera occlusion enabled; mountain camera excluded.";
        File.WriteAllText(session.backup + "/Result.txt", result); File.WriteAllText(Status, result); Audit();
    }
    static void Audit()
    {
        var text = new StringBuilder();
        foreach (var r in Objects<Renderer>()) text.AppendLine(Id(r) + "\t" + r.name + "\t" + GameObjectUtility.GetStaticEditorFlags(r.gameObject) + "\tmoving=" + IsMoving(r.transform) + "\twall=" + IsWall(r) + "\tbounds=" + r.bounds);
        foreach (var c in Objects<Camera>()) text.AppendLine("CAMERA " + c.name + " mask=" + c.cullingMask + " far=" + c.farClipPlane + " occlusion=" + c.useOcclusionCulling);
        File.WriteAllText("Library/OcclusionBakeOnly.audit.tsv", text.ToString());
    }
    static void PrepareView()
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type type = Type.GetType("UnityEditor.GameView,UnityEditor", true); view = EditorWindow.GetWindow(type, false, null, false);
        selectedSize = type.GetProperty("selectedSizeIndex", all);
        Type sizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor", true);
        object sizes = sizesType.BaseType.GetProperty("instance", all).GetValue(null);
        var get = sizesType.GetMethod("GetGroup", all); object group = get.Invoke(sizes, new[] { Enum.Parse(get.GetParameters()[0].ParameterType, "Standalone") });
        Type sizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor", true);
        var ctor = Array.Find(sizeType.GetConstructors(all), c => c.GetParameters().Length == 4);
        object size = ctor.Invoke(new[] { Enum.Parse(ctor.GetParameters()[0].ParameterType, "FixedResolution"), (object)1280, 720, "Occlusion validation 1280x720" });
        int index = (int)group.GetType().GetMethod("GetTotalCount", all).Invoke(group, null);
        group.GetType().GetMethod("AddCustomSize", all).Invoke(group, new[] { size });
        previousSize = (int)selectedSize.GetValue(view); selectedSize.SetValue(view, index); view.Focus(); readyAt = EditorApplication.timeSinceStartup + 1;
    }
}
