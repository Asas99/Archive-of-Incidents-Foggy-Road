// 87 FPS'e ulastiktan sonra kisilan gorsel detaylari geri acar.
// Menu: Tools -> Foggy Road -> GORSEL 1/2/3
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class FoggyRoad_GorselGeriGetir
{
    // ============ 1) Cimenleri geri getir ============
    [MenuItem("Tools/Foggy Road/GORSEL 1 - Cimenleri Geri Getir")]
    public static void CimenGeri()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Cimenleri geri getir");
            Debug.Log("[FoggyRoad] ONCE  " + t.name +
                      ": detailDistance=" + t.detailObjectDistance +
                      " density=" + t.detailObjectDensity +
                      " basemapDistance=" + t.basemapDistance +
                      " pixelError=" + t.heightmapPixelError);

            t.detailObjectDistance = 110f;   // 35 -> 110 (orijinal deger)
            t.detailObjectDensity = 0.40f;   // 0.15 -> 0.40 (orijinalden biraz daha comert)
            t.basemapDistance = 1000f;       // uzak zemin dokusu netlesir
            t.heightmapPixelError = 10f;     // 25 -> 10 (arazi silueti netlesir)

            EditorUtility.SetDirty(t);
            Debug.Log("[FoggyRoad] SONRA " + t.name +
                      ": detailDistance=" + t.detailObjectDistance +
                      " density=" + t.detailObjectDensity +
                      " basemapDistance=" + t.basemapDistance +
                      " pixelError=" + t.heightmapPixelError);
        }
        Debug.Log("[FoggyRoad] GORSEL 1 TAMAM - cimenler geri geldi. Ctrl+S yap.");
    }

    // ============ 2) Agaclari biraz geri ac ============
    [MenuItem("Tools/Foggy Road/GORSEL 2 - Agaclari Biraz Geri AC")]
    public static void AgacGeri()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Agaclari geri ac");
            Debug.Log("[FoggyRoad] ONCE  treeDist=" + t.treeDistance +
                      " billboard=" + t.treeBillboardDistance +
                      " maxFullLOD=" + t.treeMaximumFullLODCount);

            t.treeDistance = 200f;             // 120 -> 200
            t.treeBillboardDistance = 70f;     // 35 -> 70 (yakin agaclar mesh kalir)
            t.treeMaximumFullLODCount = 35;    // 12 -> 35
            t.treeCrossFadeLength = 12f;

            EditorUtility.SetDirty(t);
            Debug.Log("[FoggyRoad] SONRA treeDist=" + t.treeDistance +
                      " billboard=" + t.treeBillboardDistance +
                      " maxFullLOD=" + t.treeMaximumFullLODCount);
        }
        QualitySettings.lodBias = 1.0f;   // 0.8 -> 1.0
        Debug.Log("[FoggyRoad] LOD Bias: 0.8 -> 1.0");
        Debug.Log("[FoggyRoad] GORSEL 2 TAMAM. Ctrl+S yap.");
    }

    // ============ 3) Golge ve sis kalitesini geri ac ============
    [MenuItem("Tools/Foggy Road/GORSEL 3 - Golge ve Sis Kalitesini Geri AC")]
    public static void GolgeSisGeri()
    {
        var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null)
            asset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;

        if (asset != null)
        {
            var so = new SerializedObject(asset);
            Set(so, "m_ShadowDistance", 50);                  // 35 -> 50
            Set(so, "m_ShadowCascadeCount", 2);               // 1 -> 2
            Set(so, "m_SoftShadowQuality", 2);                // Low -> High
            Set(so, "m_MainLightShadowmapResolution", 2048);  // 1024 -> 2048
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }
        QualitySettings.shadowDistance = 50f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.softVegetation = true;

        // Sis adimlarini geri yukselt
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null || mat.shader == null || !mat.shader.name.Contains("VolumetricFog")) continue;
            float once = mat.HasProperty("_Steps") ? mat.GetFloat("_Steps") : -1f;
            if (mat.HasProperty("_Steps")) mat.SetFloat("_Steps", 18f);          // 12 -> 18
            if (mat.HasProperty("_MaxDistance")) mat.SetFloat("_MaxDistance", 120f);
            EditorUtility.SetDirty(mat);
            Debug.Log("[FoggyRoad] Sis '" + mat.name + "' _Steps: " + once + " -> 18, _MaxDistance -> 120");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FoggyRoad] GORSEL 3 TAMAM - golge 50m, cascade 2, yumusak golge High, sis 18 adim. Ctrl+S yap.");
    }

    static void Set(SerializedObject so, string prop, float v)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning("  ? " + prop + " yok"); return; }
        string once;
        if (p.propertyType == SerializedPropertyType.Boolean) { once = p.boolValue.ToString(); p.boolValue = v > 0.5f; }
        else if (p.propertyType == SerializedPropertyType.Float) { once = p.floatValue.ToString(); p.floatValue = v; }
        else { once = p.intValue.ToString(); p.intValue = (int)v; }
        Debug.Log("  " + prop + ": " + once + " -> " + v);
    }

    // ============ HEPSI ============
    [MenuItem("Tools/Foggy Road/GORSEL -- HEPSINI GERI AC --")]
    public static void Hepsi()
    {
        CimenGeri();
        AgacGeri();
        GolgeSisGeri();
        AssetDatabase.SaveAssets();
        Debug.Log("===== [FoggyRoad] GORSEL DETAYLAR GERI ACILDI. Ctrl+S yap, build al, FPS'e bak. =====");
    }
}
