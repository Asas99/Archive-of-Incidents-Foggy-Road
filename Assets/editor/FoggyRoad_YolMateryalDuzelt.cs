// yol.fbx'in materyallerini kullanicinin commit'indeki materyallere baglar.
//
// Sorun: yol.fbx yeniden export edildiginde 'externalObjects' bos kaldi,
// bu yuzden Unity FBX'in gomulu (beyaz) materyallerini kullaniyor.
// Cozum: ModelImporter.AddRemap ile her FBX materyalini gercek .mat dosyasina baglamak.
//
// Menu: Tools -> Foggy Road -> YOL ...
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FoggyRoad_YolMateryalDuzelt
{
    const string FbxYolu = "Assets/Guardrail_Unity_Export/yol.fbx";

    // Commit'teki hedef materyaller
    const string MatYol       = "Assets/yol 1/RoadWet.mat";
    const string MatMetal     = "Assets/Guardrail_Unity_Export/Materials/GRPRO_Metal_Unity.mat";
    const string MatBolts     = "Assets/Guardrail_Unity_Export/Materials/GRPRO_Bolts_Unity.mat";
    const string MatReflector = "Assets/Guardrail_Unity_Export/Materials/GRPRO_Reflector_Unity.mat";

    // =====================================================================
    // 1) FBX icindeki materyal isimlerini listele (once bunu calistir)
    // =====================================================================
    [MenuItem("Tools/Foggy Road/Yol Materyal/1 - FBX Materyal Isimlerini Listele")]
    public static void Listele()
    {
        var imp = AssetImporter.GetAtPath(FbxYolu) as ModelImporter;
        if (imp == null) { Debug.LogError("[FoggyRoad] FBX bulunamadi: " + FbxYolu); return; }

        var isimler = FbxMateryalIsimleri();
        Debug.Log($"[FoggyRoad] '{FbxYolu}' icinde {isimler.Count} materyal slotu var:");
        foreach (var ad in isimler)
            Debug.Log($"    \"{ad}\"");

        var mevcut = imp.GetExternalObjectMap();
        Debug.Log($"[FoggyRoad] Mevcut remap sayisi: {mevcut.Count}");
        foreach (var kv in mevcut)
            Debug.Log($"    \"{kv.Key.name}\" -> {(kv.Value != null ? AssetDatabase.GetAssetPath(kv.Value) : "<bos>")}");
    }

    // =====================================================================
    // 2) Materyalleri bagla
    // =====================================================================
    [MenuItem("Tools/Foggy Road/Yol Materyal/2 - Materyalleri Committeki Hale Bagla")]
    public static void Bagla()
    {
        var imp = AssetImporter.GetAtPath(FbxYolu) as ModelImporter;
        if (imp == null) { Debug.LogError("[FoggyRoad] FBX bulunamadi: " + FbxYolu); return; }

        var yolMat  = Yukle(MatYol);
        var metal   = Yukle(MatMetal);
        var bolts   = Yukle(MatBolts);
        var reflect = Yukle(MatReflector);

        if (yolMat == null && metal == null)
        {
            Debug.LogError("[FoggyRoad] Hedef materyaller bulunamadi. Yollari kontrol et.");
            return;
        }

        int n = 0;
        foreach (var kaynakAdi in FbxMateryalIsimleri())
        {
            var ad = kaynakAdi.ToLowerInvariant();
            Material hedef = null;

            if (ad.Contains("bolt"))                                   hedef = bolts;
            else if (ad.Contains("reflect"))                           hedef = reflect;
            else if (ad.Contains("road") || ad.Contains("pcrg")
                  || ad.Contains("asphalt") || ad.Contains("asfalt"))  hedef = yolMat;
            else if (ad.Contains("steel") || ad.Contains("metal")
                  || ad.Contains("grpro") || ad.Contains("guardrail")) hedef = metal;
            else                                                       hedef = metal;  // kalanlar metal

            if (hedef == null) { Debug.LogWarning($"    \"{kaynakAdi}\" icin hedef yok, atlandi."); continue; }

            var kimlik = new AssetImporter.SourceAssetIdentifier(typeof(Material), kaynakAdi);
            imp.AddRemap(kimlik, hedef);
            Debug.Log($"    \"{kaynakAdi}\" -> {AssetDatabase.GetAssetPath(hedef)}");
            n++;
        }

        imp.SaveAndReimport();
        AssetDatabase.Refresh();
        Debug.Log($"===== [FoggyRoad] {n} materyal baglandi ve FBX yeniden import edildi. =====");
    }

    // =====================================================================
    // 3) Cakisan eski sistemi kapat (yol2 + arkadasin Rail/Post/Edge)
    // =====================================================================
    [MenuItem("Tools/Foggy Road/Yol Materyal/3 - Eski Yol ve Bariyerleri Kapat")]
    public static void EskiyiKapat()
    {
        int kapatilanBariyer = 0, kapatilanYol = 0;

        // Arkadasin bariyer parcalari
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var ad = go.name;
            bool arkadasinBariyeri = ad.StartsWith("Rail_") || ad.StartsWith("Post_") || ad.StartsWith("Edge_");
            if (!arkadasinBariyeri) continue;
            Undo.RecordObject(go, "Eski bariyer kapat");
            go.SetActive(false);
            EditorUtility.SetDirty(go);
            kapatilanBariyer++;
        }

        // Eski yol objesi
        foreach (var ad in new[] { "yol2" })
        {
            var go = GameObject.Find(ad);
            if (go == null) continue;
            Undo.RecordObject(go, "Eski yol kapat");
            go.SetActive(false);
            EditorUtility.SetDirty(go);
            kapatilanYol++;
            Debug.Log($"    '{ad}' kapatildi.");
        }

        Debug.Log($"[FoggyRoad] Arkadasin bariyer parcasi kapatildi: {kapatilanBariyer}");
        Debug.Log($"[FoggyRoad] Eski yol objesi kapatildi: {kapatilanYol}");
        Debug.Log("[FoggyRoad] YOL 3 TAMAM - artik sadece senin 'yol' objen gorunuyor. Ctrl+S yap.");
    }

    // =====================================================================
    // 4) Geri al
    // =====================================================================
    [MenuItem("Tools/Foggy Road/Yol Materyal/4 - Eski Yol ve Bariyerleri GERI AC")]
    public static void EskiyiAc()
    {
        int n = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var ad = go.name;
            bool hedef = ad.StartsWith("Rail_") || ad.StartsWith("Post_") || ad.StartsWith("Edge_") || ad == "yol2";
            if (!hedef || go.activeSelf) continue;
            Undo.RecordObject(go, "Eski sistemi ac");
            go.SetActive(true);
            EditorUtility.SetDirty(go);
            n++;
        }
        Debug.Log($"[FoggyRoad] {n} obje geri acildi.");
    }

    /// <summary>FBX icindeki materyal slot isimlerini toplar (renderer'lardan okur).</summary>
    static List<string> FbxMateryalIsimleri()
    {
        var isimler = new List<string>();

        // 1) FBX'in kok prefabi uzerinden renderer materyalleri
        var kok = AssetDatabase.LoadAssetAtPath<GameObject>(FbxYolu);
        if (kok != null)
        {
            foreach (var r in kok.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    var ad = m.name.Replace(" (Instance)", "").Trim();
                    if (!isimler.Contains(ad)) isimler.Add(ad);
                }
            }
        }

        // 2) FBX'e gomulu materyal asset'leri (varsa)
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(FbxYolu))
        {
            if (o is Material mm && !isimler.Contains(mm.name)) isimler.Add(mm.name);
        }

        return isimler;
    }

    static Material Yukle(string yol)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
        if (m == null) Debug.LogWarning("[FoggyRoad] Materyal bulunamadi: " + yol);
        return m;
    }
}
