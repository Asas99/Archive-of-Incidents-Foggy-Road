// GPU MODU - isi CPU'dan alip ekran kartina verir (Unity 6 GPU Resident Drawer)
// Menu: Tools -> Foggy Road -> GPU ...
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class FoggyRoad_GpuMode
{
    // ================= 1) GPU Resident Drawer'i ac =================
    [MenuItem("Tools/Foggy Road/GPU 1 - GPU Resident Drawer AC")]
    public static void GpuDrawerAc()
    {
        var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null)
            asset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;
        if (asset == null) { Debug.LogError("[FoggyRoad] URP Asset bulunamadi."); return; }

        var so = new SerializedObject(asset);
        Set(so, "m_GPUResidentDrawerMode", 1);                        // 1 = Instanced Drawing
        Set(so, "m_GPUResidentDrawerEnableOcclusionCullingInCameras", 1);
        Set(so, "m_SmallMeshScreenPercentage", 2);                    // cok kucuk mesh'leri ele
        Set(so, "m_UseSRPBatcher", 1);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);

        // BatchRendererGroup shader varyantlari korunmali, yoksa GPU drawer calismaz
        BrgVaryantlariKoru();

        Debug.Log("[FoggyRoad] GPU Resident Drawer ACILDI.");
        Debug.Log("[FoggyRoad] Simdi 'GPU 2' adimini calistir (static isaretini kaldirir), sonra Unity'yi yeniden baslat.");
    }

    static void Set(SerializedObject so, string prop, float v)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning("  ? " + prop + " bu Unity surumunde yok"); return; }
        string once;
        if (p.propertyType == SerializedPropertyType.Boolean) { once = p.boolValue.ToString(); p.boolValue = v > 0.5f; }
        else if (p.propertyType == SerializedPropertyType.Float) { once = p.floatValue.ToString(); p.floatValue = v; }
        else { once = p.intValue.ToString(); p.intValue = (int)v; }
        Debug.Log("  " + prop + ": " + once + " -> " + v);
    }

    static void BrgVaryantlariKoru()
    {
        // PlayerSettings.SetBatchRendererGroupStrippingMode - surume gore degisebilir, reflection ile
        var t = typeof(PlayerSettings);
        var m = t.GetMethod("SetBatchRendererGroupStrippingMode", BindingFlags.Public | BindingFlags.Static);
        if (m == null)
        {
            Debug.LogWarning("[FoggyRoad] BRG stripping ayari koddan yapilamadi. ELLE yap:");
            Debug.LogWarning("  Edit -> Project Settings -> Player -> Other Settings -> 'BatchRendererGroup Variants' = 'Keep All'");
            return;
        }
        var enumType = m.GetParameters()[0].ParameterType;
        object keepAll = null;
        foreach (var name in System.Enum.GetNames(enumType))
            if (name.Contains("Keep") || name.Contains("All")) { keepAll = System.Enum.Parse(enumType, name); break; }
        if (keepAll == null) { Debug.LogWarning("[FoggyRoad] KeepAll degeri bulunamadi, elle ayarla."); return; }
        m.Invoke(null, new object[] { keepAll });
        Debug.Log("  BatchRendererGroup Variants -> " + keepAll);
    }

    // ================= 2) Static isaretini kaldir =================
    [MenuItem("Tools/Foggy Road/GPU 2 - Bariyerlerden Static Isaretini Kaldir")]
    public static void StaticKaldir()
    {
        var kok = GameObject.Find("yol");
        if (kok == null) { Debug.LogError("[FoggyRoad] Sahnede 'yol' objesi yok."); return; }

        int n = 0;
        foreach (var tf in kok.GetComponentsInChildren<Transform>(true))
        {
            var go = tf.gameObject;
            var mevcut = GameObjectUtility.GetStaticEditorFlags(go);
            if ((mevcut & StaticEditorFlags.BatchingStatic) == 0) continue;

            Undo.RecordObject(go, "Static kaldir");
            // Batching static'i kaldir (GPU drawer bunlari disliyor),
            // occlusion bayraklari kalsin (gorunmeyeni elemeye yariyor)
            var yeni = mevcut & ~StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(go, yeni);
            EditorUtility.SetDirty(go);
            n++;
        }
        Debug.Log("[FoggyRoad] " + n + " objeden 'Batching Static' kaldirildi. Artik GPU Resident Drawer bunlari alabilir.");
        Debug.Log("[FoggyRoad] Ctrl+S yap, sonra Unity'yi KAPAT-AC.");
    }

    // ================= 3) Graphics Jobs ac =================
    [MenuItem("Tools/Foggy Road/GPU 3 - Graphics Jobs AC (CPU yukunu dagitir)")]
    public static void GraphicsJobsAc()
    {
        try
        {
            bool once = PlayerSettings.graphicsJobs;
            PlayerSettings.graphicsJobs = true;
            Debug.Log("[FoggyRoad] Graphics Jobs: " + once + " -> true");

            var p = typeof(PlayerSettings).GetProperty("graphicsJobMode", BindingFlags.Public | BindingFlags.Static);
            if (p != null)
            {
                var enumType = p.PropertyType;
                foreach (var name in System.Enum.GetNames(enumType))
                {
                    if (!name.Contains("Native")) continue;
                    p.SetValue(null, System.Enum.Parse(enumType, name));
                    Debug.Log("  graphicsJobMode -> " + name);
                    break;
                }
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[FoggyRoad] Graphics Jobs ayarlanamadi: " + e.Message); }

        Debug.Log("[FoggyRoad] Bu ayar ancak BUILD'de etkili olur, editorde degil.");
    }

    // ================= 4) Kaliteyi geri ac =================
    [MenuItem("Tools/Foggy Road/GPU 4 - Kaliteyi Geri AC (GPU'da yer var)")]
    public static void KaliteGeriAc()
    {
        var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null)
            asset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;
        if (asset != null)
        {
            var so = new SerializedObject(asset);
            Set(so, "m_ShadowDistance", 55);       // 35 -> 55
            Set(so, "m_ShadowCascadeCount", 2);    // 1 -> 2
            Set(so, "m_SoftShadowQuality", 2);     // Low -> High
            Set(so, "m_MainLightShadowmapResolution", 2048);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }
        QualitySettings.shadowDistance = 55f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.lodBias = 1.2f;            // 0.8 -> 1.2 (agaclar daha detayli)

        // Sis kalitesini geri yukselt
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.shader == null || !m.shader.name.Contains("VolumetricFog")) continue;
            m.SetFloat("_Steps", 20f);
            if (m.HasProperty("_MaxDistance")) m.SetFloat("_MaxDistance", 120f);
            EditorUtility.SetDirty(m);
            Debug.Log("[FoggyRoad] Sis '" + m.name + "' _Steps -> 20, _MaxDistance -> 120");
        }

        // Terrain agaclarini biraz geri ac
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Kalite geri");
            t.treeDistance = 180f;
            t.treeBillboardDistance = 70f;
            t.treeMaximumFullLODCount = 40;
            t.detailObjectDistance = 60f;
            t.heightmapPixelError = 12f;
            EditorUtility.SetDirty(t);
            Debug.Log("[FoggyRoad] Terrain geri acildi: treeDist=180 billboard=70 maxLOD=40 pixelError=12");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[FoggyRoad] GPU 4 TAMAM - grafikler geri yukseldi. Ctrl+S.");
    }

    // ================= 5) Durum raporu =================
    [MenuItem("Tools/Foggy Road/GPU 5 - Durumu Kontrol Et")]
    public static void Kontrol()
    {
        var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null)
            asset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;
        if (asset == null) { Debug.LogError("URP Asset yok"); return; }

        var so = new SerializedObject(asset);
        string Oku(string p)
        {
            var x = so.FindProperty(p);
            if (x == null) return "<yok>";
            if (x.propertyType == SerializedPropertyType.Boolean) return x.boolValue.ToString();
            if (x.propertyType == SerializedPropertyType.Float) return x.floatValue.ToString();
            return x.intValue.ToString();
        }

        Debug.Log("========== GPU DURUM RAPORU ==========\n" +
                  "  GPU Resident Drawer Mode : " + Oku("m_GPUResidentDrawerMode") + "   (0=kapali, 1=ACIK)\n" +
                  "  GPU Occlusion Culling    : " + Oku("m_GPUResidentDrawerEnableOcclusionCullingInCameras") + "\n" +
                  "  SRP Batcher              : " + Oku("m_UseSRPBatcher") + "\n" +
                  "  Shadow Distance          : " + Oku("m_ShadowDistance") + "\n" +
                  "  Shadow Cascades          : " + Oku("m_ShadowCascadeCount") + "\n" +
                  "  Soft Shadow Quality      : " + Oku("m_SoftShadowQuality") + "\n" +
                  "  Rendering Mode           : " + Oku("m_RenderingMode") + "   (0=Forward, 1=Deferred, 2=Forward+)\n" +
                  "  Graphics Jobs            : " + PlayerSettings.graphicsJobs + "\n" +
                  "  LOD Bias                 : " + QualitySettings.lodBias + "\n" +
                  "  Ekran karti              : " + SystemInfo.graphicsDeviceName + "\n" +
                  "  Grafik API               : " + SystemInfo.graphicsDeviceType + "\n" +
                  "  VRAM                     : " + SystemInfo.graphicsMemorySize + " MB\n" +
                  "  CPU cekirdek             : " + SystemInfo.processorCount + "\n" +
                  "======================================");
    }

    // ================= HEPSI =================
    [MenuItem("Tools/Foggy Road/GPU -- HEPSINI CALISTIR --")]
    public static void Hepsi()
    {
        bool ok = EditorUtility.DisplayDialog("GPU Modu",
            "Isi CPU'dan alip ekran kartina verecegiz.\n\n" +
            "Bittikten sonra UNITY'YI KAPATIP ACMAN gerekiyor.\n\nDevam?", "Evet", "Iptal");
        if (!ok) return;

        GpuDrawerAc();
        StaticKaldir();
        GraphicsJobsAc();
        KaliteGeriAc();
        AssetDatabase.SaveAssets();
        Kontrol();
        Debug.Log("===== [FoggyRoad] GPU MODU KURULDU. Ctrl+S yap, Unity'yi KAPAT-AC, sonra olc. =====");
    }
}
