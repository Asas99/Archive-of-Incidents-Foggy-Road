// Terrain ayarlarini git HEAD'deki ORIJINAL degerlere dondurur.
// Degerler "git show HEAD:Assets/game prototype scene.unity" ciktisindan birebir alindi.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FoggyRoad_OrijinaleDon
{
    [MenuItem("Tools/Foggy Road/ORIJINAL - Terrain Ayarlarini Git'teki Haline Dondur")]
    public static void Dondur()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Terrain orijinale don");

            Debug.Log("[FoggyRoad] ONCE  " + t.name +
                      "\n  treeDistance=" + t.treeDistance +
                      "  billboardStart=" + t.treeBillboardDistance +
                      "  crossFade=" + t.treeCrossFadeLength +
                      "  maxFullLOD=" + t.treeMaximumFullLODCount +
                      "\n  detailDistance=" + t.detailObjectDistance +
                      "  detailDensity=" + t.detailObjectDensity +
                      "\n  pixelError=" + t.heightmapPixelError +
                      "  basemapDistance=" + t.basemapDistance +
                      "  shadows=" + t.shadowCastingMode);

            // ---- git HEAD degerleri ----
            t.treeDistance = 5000f;                 // m_TreeDistance: 5000
            t.treeBillboardDistance = 50f;          // m_TreeBillboardDistance: 50
            t.treeCrossFadeLength = 5f;             // m_TreeCrossFadeLength: 5
            t.treeMaximumFullLODCount = 50;         // m_TreeMaximumFullLODCount: 50
            t.detailObjectDistance = 180f;          // m_DetailObjectDistance: 180
            t.detailObjectDensity = 1f;             // m_DetailObjectDensity: 1
            t.heightmapPixelError = 15f;            // m_HeightmapPixelError: 15
            t.basemapDistance = 1000f;              // m_SplatMapDistance: 1000
            t.shadowCastingMode = ShadowCastingMode.TwoSided;  // m_ShadowCastingMode: 2
            t.drawTreesAndFoliage = true;
            t.drawInstanced = true;

            EditorUtility.SetDirty(t);

            Debug.Log("[FoggyRoad] SONRA " + t.name + " (git HEAD degerleri)" +
                      "\n  treeDistance=" + t.treeDistance +
                      "  billboardStart=" + t.treeBillboardDistance +
                      "  crossFade=" + t.treeCrossFadeLength +
                      "  maxFullLOD=" + t.treeMaximumFullLODCount +
                      "\n  detailDistance=" + t.detailObjectDistance +
                      "  detailDensity=" + t.detailObjectDensity +
                      "\n  pixelError=" + t.heightmapPixelError +
                      "  basemapDistance=" + t.basemapDistance +
                      "  shadows=" + t.shadowCastingMode);
        }

        // LOD Bias: git'teki proje ayari 2 idi
        Debug.Log("[FoggyRoad] LOD Bias: " + QualitySettings.lodBias + " -> 2 (orijinal)");
        QualitySettings.lodBias = 2f;

        Debug.Log("===== [FoggyRoad] TERRAIN ORIJINALE DONDU. Ctrl+S yap. =====");
        Debug.Log("NOT: Cimen katmanlari (Paint Details) hala eksikse, terrain VERISI degismis demektir;");
        Debug.Log("     o zaman git'ten GeneratedTerrain .asset dosyasini geri almak gerekir.");
    }

    // Cimen katmanlarini listele - eksik olan var mi gormek icin
    [MenuItem("Tools/Foggy Road/ORIJINAL - Cimen Katmanlarini Say")]
    public static void CimenSay()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var td = t.terrainData;
            if (td == null) continue;

            var dp = td.detailPrototypes;
            Debug.Log("[FoggyRoad] '" + t.name + "' -> detail (cimen) katman sayisi: " + dp.Length +
                      "  |  detailResolution=" + td.detailResolution +
                      "  |  agac sayisi=" + td.treeInstances.Length);

            for (int i = 0; i < dp.Length; i++)
            {
                string ad = dp[i].prototype != null ? dp[i].prototype.name
                          : (dp[i].prototypeTexture != null ? dp[i].prototypeTexture.name + " (texture)" : "<bos>");

                // Bu katmanda gercekten boyanmis alan var mi?
                int toplam = 0;
                var layer = td.GetDetailLayer(0, 0, td.detailWidth, td.detailHeight, i);
                foreach (var v in layer) toplam += v;

                Debug.Log("   [" + i + "] " + ad + "   density=" + dp[i].density +
                          "   boyanmis toplam=" + toplam + (toplam == 0 ? "   <<< BU KATMAN BOS" : ""));
            }
        }
    }
}
