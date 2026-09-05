using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class CodexTerrainReferenceRecovery
{
    private const string TerrainDataPath =
        "Assets/GeneratedTerrain/New Terrain 2_ForestRoadGenerated 1_ForestRoadGenerated 4.asset";

    static CodexTerrainReferenceRecovery()
    {
        EditorApplication.delayCall += RepairOpenScene;
    }

    private static void RepairOpenScene()
    {
        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (terrainData == null)
        {
            Debug.LogError("[CodexRecovery] Repaired TerrainData could not be loaded.");
            return;
        }

        int repairedObjects = 0;
        foreach (Terrain terrain in Resources.FindObjectsOfTypeAll<Terrain>())
        {
            if (!terrain.gameObject.scene.IsValid() || terrain.name != "Terrain")
                continue;

            Undo.RecordObject(terrain, "Restore Terrain Data reference");
            terrain.terrainData = terrainData;
            EditorUtility.SetDirty(terrain);

            TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Restore Terrain Collider Data reference");
                collider.terrainData = terrainData;
                EditorUtility.SetDirty(collider);
            }

            terrain.Flush();
            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
            repairedObjects++;
        }

        Debug.Log($"[CodexRecovery] Restored TerrainData on {repairedObjects} open-scene Terrain object(s).");
        SceneView.RepaintAll();
    }
}
