using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StationShadowOptimization
{
    private const string Manifest = "Assets/Optimization/StationShadowProxies/Bindings.tsv";

    [MenuItem("Tools/Foggy Road/Station Shadows/Apply validated proxies")]
    private static void ApplyMenu() => Debug.Log(ApplyValidatedProxies());

    public static string ApplyValidatedProxies()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply shadow proxies in Edit Mode.");

        var targets = new List<(MeshRenderer source, Mesh proxy)>();
        foreach (string line in File.ReadAllLines(Manifest))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] fields = line.Split('\t');
            if (fields.Length < 3 || !GlobalObjectId.TryParse(fields[0], out var id))
                throw new InvalidDataException("Invalid shadow binding: " + line);
            var source = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as MeshRenderer;
            if (source == null)
                throw new InvalidOperationException("Load the station scene before applying its shadow bindings.");
            var sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null ||
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceFilter.sharedMesh)) != fields[1])
                throw new InvalidOperationException("The source mesh changed on " + source.name);
            var proxy = AssetDatabase.LoadAssetAtPath<Mesh>(fields[2]);
            if (proxy == null) throw new InvalidOperationException("Missing shadow mesh: " + fields[2]);
            if (TriangleCount(proxy) >= TriangleCount(sourceFilter.sharedMesh))
                throw new InvalidOperationException("Shadow mesh does not simplify " + source.name);
            targets.Add((source, proxy));
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Optimize station shadow casters");
        int count = 0;
        foreach (var target in targets)
        {
            bool exists = false;
            foreach (var binding in target.source.GetComponentsInChildren<StationShadowCaster>(true))
                if (binding.SourceRenderer == target.source)
                {
                    MatchSourceLods(target.source, binding.GetComponent<Renderer>());
                    exists = true;
                    break;
                }
            if (exists) continue;

            Undo.RecordObject(target.source, "Use station shadow proxy");
            var child = new GameObject("Shadow caster (optimized)");
            Undo.RegisterCreatedObjectUndo(child, "Create station shadow caster");
            child.layer = target.source.gameObject.layer;
            child.transform.SetParent(target.source.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = target.proxy;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = target.source.sharedMaterial;
            var caster = child.AddComponent<StationShadowCaster>();
            caster.Configure(target.source);
            MatchSourceLods(target.source, renderer);
            EditorUtility.SetDirty(caster);
            EditorSceneManager.MarkSceneDirty(target.source.gameObject.scene);
            count++;
        }
        Undo.CollapseUndoOperations(group);
        return "Applied " + count + " validated station shadow casters.";
    }

    public static long TriangleCount(Mesh mesh)
    {
        long count = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            if (mesh.GetTopology(i) == MeshTopology.Triangles) count += (long)mesh.GetIndexCount(i) / 3;
        return count;
    }

    // LODGroup visibility does not change Renderer.enabled. The shadow renderer
    // must be a member of the same levels rather than relying on LateUpdate alone.
    private static void MatchSourceLods(Renderer source, Renderer shadow)
    {
        foreach (var group in source.GetComponentsInParent<LODGroup>(true))
        {
            var levels = group.GetLODs();
            bool changed = false;
            for (int i = 0; i < levels.Length; i++)
                if (levels[i].renderers.Contains(source) && !levels[i].renderers.Contains(shadow))
                {
                    levels[i].renderers = levels[i].renderers.Concat(new[] { shadow }).ToArray();
                    changed = true;
                }
            if (!changed) continue;
            Undo.RecordObject(group, "Match shadow proxy LOD visibility");
            group.SetLODs(levels);
            EditorUtility.SetDirty(group);
        }
    }

    [MenuItem("Tools/Foggy Road/Station Shadows/Revert to original shadows")]
    public static void Revert()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Revert shadow proxies in Edit Mode.");

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore original station shadows");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var caster in UnityEngine.Object.FindObjectsByType<StationShadowCaster>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (caster.gameObject.scene != scene) continue;
            if (caster.SourceRenderer != null)
                Undo.RecordObject(caster.SourceRenderer, "Restore source shadow caster");
            var renderer = caster.GetComponent<Renderer>();
            foreach (var groupLod in caster.GetComponentsInParent<LODGroup>(true))
            {
                var levels = groupLod.GetLODs();
                if (!levels.Any(l => l.renderers.Contains(renderer))) continue;
                Undo.RecordObject(groupLod, "Remove shadow proxy from LOD visibility");
                for (int i = 0; i < levels.Length; i++)
                    levels[i].renderers = levels[i].renderers.Where(r => r != renderer).ToArray();
                groupLod.SetLODs(levels);
            }
            Undo.DestroyObjectImmediate(caster.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(group);
    }
}

