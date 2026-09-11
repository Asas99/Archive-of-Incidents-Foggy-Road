// GPU Resident Drawer'i KAPAT ve static batching'i geri getir.
// Olcumler GPU Resident Drawer'in bu projede zarar verdigini gosterdi:
//   FPS 50.5 -> 32.1, Saved by batching 6427 -> 1132, Batches 206 -> 413
// Sebep: terrain agaclari (ana yuk) GPU Resident Drawer kapsaminda degil,
// ama sistem static batching'i iptal ettirdigi icin net kayip olusuyor.
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class FoggyRoad_GpuModeOff
{
    [MenuItem("Tools/Foggy Road/GERI AL - GPU Modunu Kapat ve Static'i Geri Ver")]
    public static void Kapat()
    {
        // 1) GPU Resident Drawer kapat
        var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null)
            asset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;
        if (asset != null)
        {
            var so = new SerializedObject(asset);
            var p = so.FindProperty("m_GPUResidentDrawerMode");
            if (p != null) { Debug.Log("  GPUResidentDrawerMode: " + p.intValue + " -> 0"); p.intValue = 0; }
            var o = so.FindProperty("m_GPUResidentDrawerEnableOcclusionCullingInCameras");
            if (o != null) o.boolValue = false;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        // 2) BRG shader varyant stripping'i geri ac (build suresi 66 dk -> normal)
        var m = typeof(PlayerSettings).GetMethod("SetBatchRendererGroupStrippingMode", BindingFlags.Public | BindingFlags.Static);
        if (m != null)
        {
            var enumType = m.GetParameters()[0].ParameterType;
            foreach (var name in System.Enum.GetNames(enumType))
            {
                if (!name.Contains("Strip") || name.Contains("All")) continue;
                m.Invoke(null, new object[] { System.Enum.Parse(enumType, name) });
                Debug.Log("  BatchRendererGroup Variants -> " + name);
                break;
            }
        }
        else
        {
            Debug.LogWarning("[FoggyRoad] BRG ayari koddan yapilamadi. ELLE:");
            Debug.LogWarning("  Project Settings -> Graphics -> BatchRendererGroup Variants = 'Strip if no Entities Graphics package'");
        }

        // 3) Bariyerlere Batching Static'i geri ver
        var kok = GameObject.Find("yol");
        int n = 0;
        if (kok != null)
        {
            foreach (var tf in kok.GetComponentsInChildren<Transform>(true))
            {
                var go = tf.gameObject;
                var mevcut = GameObjectUtility.GetStaticEditorFlags(go);
                var hedef = mevcut | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
                if (mevcut == hedef) continue;
                Undo.RecordObject(go, "Static geri ver");
                GameObjectUtility.SetStaticEditorFlags(go, hedef);
                EditorUtility.SetDirty(go);
                n++;
            }
        }
        Debug.Log("[FoggyRoad] " + n + " objeye Batching Static geri verildi.");

        // 4) Kaliteyi dusuk tut (GPU 4 geri acmisti, FPS'i dusurmustu)
        if (asset != null)
        {
            var so2 = new SerializedObject(asset);
            SetF(so2, "m_ShadowDistance", 35);
            SetF(so2, "m_ShadowCascadeCount", 1);
            SetF(so2, "m_SoftShadowQuality", 1);
            SetF(so2, "m_MainLightShadowmapResolution", 1024);
            SetF(so2, "m_RequireOpaqueTexture", 0);
            SetF(so2, "m_MSAA", 1);
            so2.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }
        QualitySettings.shadowDistance = 35f;
        QualitySettings.shadowCascades = 1;
        QualitySettings.lodBias = 0.8f;
        QualitySettings.antiAliasing = 0;

        // Sis tekrar hafif
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null || mat.shader == null || !mat.shader.name.Contains("VolumetricFog")) continue;
            if (mat.HasProperty("_Steps")) mat.SetFloat("_Steps", 12f);
            if (mat.HasProperty("_MaxDistance")) mat.SetFloat("_MaxDistance", 90f);
            EditorUtility.SetDirty(mat);
        }

        // Terrain tekrar kisik
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Terrain kis");
            t.treeDistance = 120f;
            t.treeBillboardDistance = 35f;
            t.treeMaximumFullLODCount = 12;
            t.detailObjectDistance = 35f;
            t.heightmapPixelError = 25f;
            EditorUtility.SetDirty(t);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("===== [FoggyRoad] GPU MODU KAPATILDI, bilinen iyi ayarlara donuldu. Ctrl+S yap. =====");
    }

    static void SetF(SerializedObject so, string prop, float v)
    {
        var p = so.FindProperty(prop);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v > 0.5f;
        else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
        else p.intValue = (int)v;
    }
}
