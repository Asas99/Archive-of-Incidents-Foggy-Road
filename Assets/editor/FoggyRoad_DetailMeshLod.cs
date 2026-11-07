using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Replaces expensive Terrain mesh-detail prototypes with small crossed billboards.
/// The original TerrainData is copied before the first replacement so the operation
/// can be reverted without touching the density maps.
/// </summary>
public static class FoggyRoad_DetailMeshLod
{
    const string Root = "Assets/GeneratedTerrain/DetailMeshLOD";
    const string BackupRoot = "Assets/GeneratedTerrain/DetailMeshLOD/Backups";
    const int TriangleThreshold = 300;

    [MenuItem("Tools/Foggy Road/Cim/6 - Pahali detail meshleri billboard yap", false, 82)]
    public static void ReplaceExpensiveDetails()
    {
        var terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length == 0)
        {
            Debug.LogWarning("[FoggyRoad] Aktif sahnede Terrain bulunamadi.");
            return;
        }

        EnsureFolder(Root);
        EnsureFolder(BackupRoot);
        int changed = 0;

        foreach (var terrain in terrains)
        {
            if (terrain.terrainData == null) continue;
            var data = terrain.terrainData;
            string dataPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(dataPath)) continue;

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = BackupRoot + "/" + data.name + "_beforeDetailLOD_" + stamp + ".asset";
            AssetDatabase.CopyAsset(dataPath, backupPath);
            Debug.Log("[FoggyRoad] Detail LOD yedegi: " + backupPath);

            var prototypes = data.detailPrototypes;
            bool terrainChanged = false;
            for (int i = 0; i < prototypes.Length; i++)
            {
                var p = prototypes[i];
                if (p == null || !p.usePrototypeMesh || p.prototype == null) continue;
                var mf = p.prototype.GetComponentInChildren<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                int tris = mf.sharedMesh.triangles.Length / 3;
                if (tris < TriangleThreshold) continue;

                string proxyPath = Root + "/" + Sanitize(p.prototype.name) + "_detailLOD.prefab";
                string sourceName = p.prototype.name;
                var proxy = AssetDatabase.LoadAssetAtPath<GameObject>(proxyPath);
                if (proxy == null)
                    proxy = CreateProxyPrefab(p.prototype, proxyPath);
                if (proxy == null) continue;

                Undo.RecordObject(data, "Terrain detail mesh LOD");
                p.prototype = proxy;
                prototypes[i] = p;
                terrainChanged = true;
                changed++;
                Debug.Log($"[FoggyRoad] {terrain.name}: detail {i} {sourceName} ({tris} tri) -> crossed billboard");
            }

            if (terrainChanged)
            {
                data.detailPrototypes = prototypes;
                EditorUtility.SetDirty(data);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FoggyRoad] Detail mesh LOD tamamlandi. Degisen prototip: " + changed + ". Geri almak icin yedek TerrainData assetini kullan.");
    }

    [MenuItem("Tools/Foggy Road/Cim/7 - Detail mesh LOD yedegini acikla", false, 83)]
    public static void RevealBackupFolder()
    {
        var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(BackupRoot);
        if (folder != null) EditorGUIUtility.PingObject(folder);
        Debug.Log("[FoggyRoad] Yedekler: " + BackupRoot);
    }

    static GameObject CreateProxyPrefab(GameObject source, string path)
    {
        var sourceRenderer = source.GetComponentInChildren<MeshRenderer>();
        var go = new GameObject(source.name + "_DetailLOD");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = CreateCrossMesh(source.name + "_CrossMesh");
        if (sourceRenderer != null) mr.sharedMaterial = sourceRenderer.sharedMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Save as a real prefab asset; Unity rejects .asset paths here.
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static Mesh CreateCrossMesh(string name)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(-0.5f, 1, 0),
            new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 1, -0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1),
            new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)
        };
        mesh.triangles = new[] { 0,2,1, 0,3,2, 4,6,5, 4,7,6 };
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Root + "/" + name + ".mesh.asset");
        return mesh;
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static string Sanitize(string s)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Replace('\\', '_');
    }
}
