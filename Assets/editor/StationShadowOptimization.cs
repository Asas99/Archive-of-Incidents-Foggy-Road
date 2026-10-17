using System;
using System.Collections.Generic;
using System.IO;
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
                if (binding.SourceRenderer == target.source) { exists = true; break; }
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
            EditorUtility.SetDirty(caster);
            EditorSceneManager.MarkSceneDirty(target.source.gameObject.scene);
            count++;
        }
        Undo.CollapseUndoOperations(group);
        return "Applied " + count + " validated station shadow casters.";
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
            Undo.DestroyObjectImmediate(caster.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(group);
    }
}

