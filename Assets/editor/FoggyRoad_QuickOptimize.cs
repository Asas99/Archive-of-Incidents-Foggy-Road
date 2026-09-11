// Tek tikla performans duzeltmeleri. Hepsi Ctrl+Z ile geri alinabilir.
// Menu: Tools -> Foggy Road -> ...
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FoggyRoad_QuickOptimize
{
    // ---------- 1) Yol + bariyer golgelerini kapat ----------
    [MenuItem("Tools/Foggy Road/1 - Yol ve Bariyer Golgelerini Kapat")]
    public static void KapatYolGolgeleri()
    {
        var kok = GameObject.Find("yol");
        if (kok == null) { EditorUtility.DisplayDialog("Bulunamadi", "Sahnede 'yol' adli obje yok.", "Tamam"); return; }

        var rends = kok.GetComponentsInChildren<MeshRenderer>(true);
        Undo.RecordObjects(rends, "Golgeleri kapat");
        int n = 0;
        foreach (var r in rends)
        {
            if (r.shadowCastingMode != ShadowCastingMode.Off) { r.shadowCastingMode = ShadowCastingMode.Off; n++; }
            EditorUtility.SetDirty(r);
        }
        Debug.Log($"[FoggyRoad] {n} objenin golgesi kapatildi (toplam {rends.Length} renderer).");
    }

    // ---------- 2) Yol + bariyerleri Static yap (batching) ----------
    [MenuItem("Tools/Foggy Road/2 - Yol ve Bariyerleri Static Yap")]
    public static void StaticYap()
    {
        var kok = GameObject.Find("yol");
        if (kok == null) { EditorUtility.DisplayDialog("Bulunamadi", "Sahnede 'yol' adli obje yok.", "Tamam"); return; }

        var tfs = kok.GetComponentsInChildren<Transform>(true);
        int n = 0;
        foreach (var t in tfs)
        {
            var go = t.gameObject;
            var mevcut = GameObjectUtility.GetStaticEditorFlags(go);
            var hedef = mevcut | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
            if (mevcut != hedef)
            {
                Undo.RecordObject(go, "Static yap");
                GameObjectUtility.SetStaticEditorFlags(go, hedef);
                n++;
            }
        }
        Debug.Log($"[FoggyRoad] {n} obje Static isaretlendi.");
    }

    // ---------- 3) Terrain agac ayarlarini duzelt ----------
    [MenuItem("Tools/Foggy Road/3 - Terrain Agac Ayarlarini Duzelt")]
    public static void TerrainAyarla()
    {
        var terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (terrains.Length == 0) { EditorUtility.DisplayDialog("Bulunamadi", "Sahnede Terrain yok.", "Tamam"); return; }

        foreach (var t in terrains)
        {
            Undo.RecordObject(t, "Terrain agac ayarlari");
            Debug.Log($"[FoggyRoad] ONCE  -> treeDistance={t.treeDistance} billboardStart={t.treeBillboardDistance} " +
                      $"maxFullLOD={t.treeMaximumFullLODCount} crossFade={t.treeCrossFadeLength} " +
                      $"detailDistance={t.detailObjectDistance} shadows={t.shadowCastingMode}");

            t.treeDistance = 250f;              // 350 -> 250
            t.treeBillboardDistance = 90f;      // 350 -> 90  (billboard artik devrede)
            t.treeMaximumFullLODCount = 60;     // 2000 -> 60
            t.treeCrossFadeLength = 15f;
            t.detailObjectDistance = 60f;       // 110 -> 60
            t.shadowCastingMode = ShadowCastingMode.On; // TwoSided -> On

            EditorUtility.SetDirty(t);
            Debug.Log($"[FoggyRoad] SONRA -> treeDistance={t.treeDistance} billboardStart={t.treeBillboardDistance} " +
                      $"maxFullLOD={t.treeMaximumFullLODCount} detailDistance={t.detailObjectDistance} shadows={t.shadowCastingMode}");
        }
    }

    // ---------- 4) Cimen katmanlarindaki agaclari listele ----------
    [MenuItem("Tools/Foggy Road/4 - Cimen Katmanlarini Listele")]
    public static void DetayListele()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var dp = t.terrainData.detailPrototypes;
            Debug.Log($"[FoggyRoad] '{t.name}' detail katmanlari ({dp.Length} adet). " +
                      "Terrain Inspector -> Paint Details sekmesinden asagidaki indexleri silebilirsin:");
            for (int i = 0; i < dp.Length; i++)
            {
                string ad = dp[i].prototype != null ? dp[i].prototype.name
                          : (dp[i].prototypeTexture != null ? dp[i].prototypeTexture.name : "<bos>");
                bool agacMi = ad.StartsWith("Pine") || ad.StartsWith("Conifer");
                Debug.Log($"   [{i}] {ad}   density={dp[i].density}   {(agacMi ? "<<< AGAC - SILINMELI" : "")}");
            }
        }
    }

    // ---------- 5) Kontrol: sonuc ----------
    [MenuItem("Tools/Foggy Road/5 - Golge Durumunu Kontrol Et")]
    public static void GolgeKontrol()
    {
        var rends = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int acik = rends.Count(r => r.shadowCastingMode != ShadowCastingMode.Off);
        Debug.Log($"[FoggyRoad] Aktif MeshRenderer: {rends.Length} | Golge atan: {acik} | Golgesi kapali: {rends.Length - acik}");
    }
}
