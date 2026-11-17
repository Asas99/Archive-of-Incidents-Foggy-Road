using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FoliageMapSetup
{
    const string BackupPath = "Assets/SceneOptimization/Generated/FoliageMapBackup.json";
    [Serializable] public class Entry
    {
        public string materialGuid, property, textureGuid;
        public long textureId;
    }
    [Serializable] public class Backup { public List<Entry> entries = new List<Entry>(); }

    static Material[] Materials()
    {
        return AssetDatabase.FindAssets("t:Material", new[] {
            "Assets/SceneOptimization/Generated/Materials", "Assets/TerrainDemoScene_URP/Prefabs"
        }).Select(AssetDatabase.GUIDToAssetPath)
          .Where(p => p.EndsWith(".mat") || p.EndsWith(".asset"))
          .Select(AssetDatabase.LoadAssetAtPath<Material>)
          .Where(m => m && m.shader &&
              ((m.HasProperty("Texture2D_A5E0646") && m.HasProperty("Texture2D_8713F080")) ||
               (m.HasProperty("_ExtraTex") && m.shader.name.IndexOf("SpeedTree", StringComparison.OrdinalIgnoreCase) >= 0) ||
               (m.HasProperty("_ExtraTex") && m.shader.name.StartsWith("FoggyRoad/Optimization/Graph/"))))
          .ToArray();
    }

    static readonly string[] Properties = {
        "Texture2D_A5E0646", "Texture2D_8713F080", "_ExtraTex", "_SubsurfaceTex"
    };

    [MenuItem("Tools/Foggy Road/Foliage Maps/Remove Mask and Thickness")]
    public static void Remove() { Debug.Log(RemoveMaps()); }

    public static string RemoveMaps()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        var backup = File.Exists(BackupPath) ? JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath)) : new Backup();
        var materials = Materials();
        // Persist original references before modifying any material. Repeated calls retain the first backup.
        foreach (var m in materials)
        foreach (string property in Properties)
        {
            if (!m.HasProperty(property) || !m.GetTexture(property)) continue;
            string materialGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));
            if (backup.entries.Any(e => e.materialGuid == materialGuid && e.property == property)) continue;
            string textureGuid; long textureId;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m.GetTexture(property), out textureGuid, out textureId))
                throw new InvalidOperationException("Cannot back up texture: " + m.name + "/" + property);
            backup.entries.Add(new Entry { materialGuid = materialGuid, property = property, textureGuid = textureGuid, textureId = textureId });
        }
        File.WriteAllText(BackupPath, JsonUtility.ToJson(backup, true));
        AssetDatabase.ImportAsset(BackupPath);
        int changed = 0, slots = 0;
        foreach (var m in materials)
        {
            bool dirty = false;
            foreach (string property in Properties)
            {
                if (!m.HasProperty(property) || !m.GetTexture(property)) continue;
                Undo.RecordObject(m, "Remove foliage mask/thickness maps");
                m.SetTexture(property, null); dirty = true; slots++;
            }
            if (dirty) { EditorUtility.SetDirty(m); changed++; }
        }
        AssetDatabase.SaveAssets();
        return "Removed " + slots + " texture assignments from " + changed + " foliage materials; backup entries=" + backup.entries.Count;
    }

    [MenuItem("Tools/Foggy Road/Foliage Maps/Restore Mask and Thickness")]
    public static void Restore() { Debug.Log(RestoreMaps()); }

    public static string RestoreMaps()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        if (!File.Exists(BackupPath)) return "No foliage map backup exists.";
        var backup = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
        var resolved = new List<Tuple<Material, string, Texture>>();
        // Resolve everything first so a missing asset cannot cause a partial restore.
        foreach (var e in backup.entries)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(e.materialGuid));
            var texture = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(e.textureGuid)).OfType<Texture>()
                .FirstOrDefault(t => { string g; long id; return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(t, out g, out id) && id == e.textureId; });
            if (!m || !texture || !m.HasProperty(e.property)) throw new InvalidOperationException("Backup asset or property missing: " + e.materialGuid + "/" + e.property);
            resolved.Add(Tuple.Create(m, e.property, texture));
        }
        foreach (var item in resolved)
        {
            Undo.RecordObject(item.Item1, "Restore foliage maps");
            item.Item1.SetTexture(item.Item2, item.Item3);
            EditorUtility.SetDirty(item.Item1);
        }
        AssetDatabase.SaveAssets();
        return "Restored " + resolved.Count + " original texture assignments.";
    }
}
