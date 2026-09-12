// AGRESIF OPTIMIZASYON - hedef 100 FPS
// Menu: Tools -> Foggy Road -> AGRESIF ...
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class FoggyRoad_AggressiveOptimize
{
    // ============ A) URP + Quality ayarlari ============
    [MenuItem("Tools/Foggy Road/AGRESIF A - URP ve Kalite Ayarlari")]
    public static void UrpAyarlari()
    {
        var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null)
            asset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;
        if (asset == null) { Debug.LogError("[FoggyRoad] URP Asset bulunamadi."); return; }

        var so = new SerializedObject(asset);

        Debug.Log("[FoggyRoad] URP Asset ayarlaniyor: " + asset.name);
        SetProp(so, "m_RequireOpaqueTexture", 0);   // bos yere ekran kopyasi - kapat
        SetProp(so, "m_MSAA", 1);                   // 1 = Disabled
        SetProp(so, "m_ShadowDistance", 35);        // 50 -> 35
        SetProp(so, "m_ShadowCascadeCount", 1);     // 2 -> 1
        SetProp(so, "m_SoftShadowQuality", 1);      // High -> Low
        SetProp(so, "m_MainLightShadowmapResolution", 1024);
        SetProp(so, "m_AdditionalLightsRenderingMode", 2);  // Disabled (tek isik var)
        SetProp(so, "m_AdditionalLightShadowsSupported", 0);
        SetProp(so, "m_ReflectionProbeBlending", 0);
        SetProp(so, "m_ReflectionProbeBoxProjection", 0);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);

        int lvl = QualitySettings.GetQualityLevel();
        QualitySettings.lodBias = 0.8f;
        QualitySettings.shadowDistance = 35f;
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadowCascades = 1;
        QualitySettings.softVegetation = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.vSyncCount = 0;
        Debug.Log("[FoggyRoad] Quality " + QualitySettings.names[lvl] +
                  ": lodBias=0.8 shadowDistance=35 MSAA=0 cascade=1 vSync=0");
        Debug.Log("[FoggyRoad] A TAMAM. Ctrl+S yap.");
    }

    static void SetProp(SerializedObject so, string prop, float v)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning("  ? " + prop + " bulunamadi"); return; }
        string once;
        if (p.propertyType == SerializedPropertyType.Boolean) { once = p.boolValue.ToString(); p.boolValue = v > 0.5f; }
        else if (p.propertyType == SerializedPropertyType.Float) { once = p.floatValue.ToString(); p.floatValue = v; }
        else { once = p.intValue.ToString(); p.intValue = (int)v; }
        Debug.Log("  " + prop + ": " + once + " -> " + v);
    }

    // ============ B) SSAO hafiflet ============
    [MenuItem("Tools/Foggy Road/AGRESIF B - SSAO Hafiflet")]
    public static void SsaoHafiflet()
    {
        int bulundu = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableRendererData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (sub == null) continue;
                if (!sub.GetType().Name.Contains("ScreenSpaceAmbientOcclusion")) continue;

                var so = new SerializedObject(sub);
                var s = so.FindProperty("m_Settings");
                if (s == null) continue;

                SetRel(s, "Downsample", 1);   // yari cozunurluk
                SetRel(s, "AfterOpaque", 1);  // depth prepass atla
                SetRel(s, "BlurQuality", 1);
                SetRel(s, "Samples", 0);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(sub);
                bulundu++;
            }
        }
        Debug.Log(bulundu > 0
            ? "[FoggyRoad] " + bulundu + " SSAO ayari hafifletildi. Ctrl+S."
            : "[FoggyRoad] SSAO bulunamadi.");
    }

    static void SetRel(SerializedProperty s, string n, int v)
    {
        var p = s.FindPropertyRelative(n);
        if (p == null) { Debug.LogWarning("  ? SSAO." + n + " yok"); return; }
        string o;
        if (p.propertyType == SerializedPropertyType.Boolean) { o = p.boolValue.ToString(); p.boolValue = v > 0; }
        else { o = p.intValue.ToString(); p.intValue = v; }
        Debug.Log("  SSAO." + n + ": " + o + " -> " + v);
    }

    // ============ C) Sis materyalini hafiflet ============
    [MenuItem("Tools/Foggy Road/AGRESIF C - Sis Materyalini Hafiflet")]
    public static void SisHafiflet()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.shader == null) continue;
            if (!m.shader.name.Contains("VolumetricFog")) continue;

            Undo.RecordObject(m, "Sis hafiflet");
            float once = m.HasProperty("_Steps") ? m.GetFloat("_Steps") : -1f;
            if (m.HasProperty("_Steps")) m.SetFloat("_Steps", 12f);
            if (m.HasProperty("_MaxDistance")) m.SetFloat("_MaxDistance", 90f);
            EditorUtility.SetDirty(m);
            Debug.Log("[FoggyRoad] " + m.name + " _Steps: " + once + " -> 12, _MaxDistance -> 90");
            n++;
        }
        Debug.Log(n > 0 ? "[FoggyRoad] C TAMAM. Ctrl+S." : "[FoggyRoad] Sis materyali bulunamadi.");
    }

    // ============ D) Agac uzak LOD golgelerini kapat ============
    [MenuItem("Tools/Foggy Road/AGRESIF D - Agac Uzak LOD Golgelerini Kapat")]
    public static void AgacGolgeleri()
    {
        int degisen = 0;
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var proto in t.terrainData.treePrototypes)
            {
                var pf = proto.prefab;
                if (pf == null) continue;
                var lg = pf.GetComponentInChildren<LODGroup>();
                if (lg == null) continue;

                var lods = lg.GetLODs();
                bool dirty = false;
                for (int i = 1; i < lods.Length; i++)   // LOD0 golge atmaya devam etsin
                {
                    foreach (var r in lods[i].renderers)
                    {
                        if (r == null) continue;
                        if (r.shadowCastingMode == ShadowCastingMode.Off) continue;
                        r.shadowCastingMode = ShadowCastingMode.Off;
                        EditorUtility.SetDirty(r);
                        degisen++;
                        dirty = true;
                        Debug.Log("  " + pf.name + " LOD" + i + " -> golge KAPALI");
                    }
                }
                if (dirty) PrefabUtility.SavePrefabAsset(pf);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[FoggyRoad] D TAMAM. " + degisen + " LOD renderer golgesi kapatildi.");
    }

    // ============ E) Terrain daha agresif ============
    [MenuItem("Tools/Foggy Road/AGRESIF E - Terrain Daha Agresif")]
    public static void TerrainAgresif()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Terrain agresif");
            Debug.Log("[FoggyRoad] ONCE  treeDist=" + t.treeDistance +
                      " billboard=" + t.treeBillboardDistance +
                      " maxLOD=" + t.treeMaximumFullLODCount +
                      " detailDist=" + t.detailObjectDistance +
                      " pixelError=" + t.heightmapPixelError);

            t.treeDistance = 160f;
            t.treeBillboardDistance = 45f;
            t.treeMaximumFullLODCount = 20;
            t.treeCrossFadeLength = 8f;
            t.detailObjectDistance = 40f;
            t.detailObjectDensity = 0.2f;
            t.heightmapPixelError = 25f;
            t.basemapDistance = 200f;
            t.shadowCastingMode = ShadowCastingMode.On;
            EditorUtility.SetDirty(t);

            Debug.Log("[FoggyRoad] SONRA treeDist=" + t.treeDistance +
                      " billboard=" + t.treeBillboardDistance +
                      " maxLOD=" + t.treeMaximumFullLODCount +
                      " detailDist=" + t.detailObjectDistance +
                      " pixelError=" + t.heightmapPixelError);
        }
        Debug.Log("[FoggyRoad] E TAMAM. Ctrl+S.");
    }

    // ============ F) HEPSINI CALISTIR ============
    [MenuItem("Tools/Foggy Road/AGRESIF -- HEPSINI CALISTIR --")]
    public static void Hepsi()
    {
        bool ok = EditorUtility.DisplayDialog("Agresif Optimizasyon",
            "A'dan E'ye tum optimizasyonlar uygulanacak.\n\nYedegin var mi?",
            "Evet, uygula", "Iptal");
        if (!ok) return;

        UrpAyarlari();
        SsaoHafiflet();
        SisHafiflet();
        AgacGolgeleri();
        TerrainAgresif();
        AssetDatabase.SaveAssets();
        Debug.Log("===== [FoggyRoad] TUM AGRESIF OPTIMIZASYONLAR UYGULANDI. Ctrl+S yap ve Play'e bas. =====");
    }
}
