// AGAC LOD ve GOLGE TESHISI
//
// SORUN: "geriye gittikce agac kayboluyor bir anda ve golge de gidiyor"
//
// TESPIT (olculdu, tahmin degil):
//   Terrain'in kullandigi Pine_A / Pine_B (SpeedTree) prefablarinda LODGroup'un
//   altindaki renderer'larda castShadows soyleydi:
//       LOD0 = On, LOD1 = Off, LOD2 = Off, LOD3(billboard) = Off
//   Yani agac LOD0'dan LOD1'e gecer gecmez GOLGESI TAMAMEN YOK OLUYOR.
//   LODGroup'ta FadeMode=CrossFade ve AnimateCrossFading=1 oldugu icin agacin
//   GORUNTUSU yumusak geciyor, ama castShadows ikili bir bayrak - fade edilemez.
//   Golge bir karede var, sonrakinde yok.
//
//   Gecis mesafesi (m_Size 29.02, screenRelativeHeight 0.30, FOV 60, lodBias 1.8):
//       d = 29.02 * 1.8 / (2*tan(30) * 0.30) = ~151 m
//   Golge mesafesi ise 170 m. Yani golge, cascadeBorder'in saglayacagi yumusak
//   bitise HIC ULASAMIYORDU - 151 m'de LOD yuzunden sertce kesiliyordu.
//
// OLCULEN GERCEK: agaclar cok farkli olceklerde dikilmis.
//   Pine_B 7736 adet, olcek 0.34 - 0.59  -> LOD0 sadece 49 m'ye kadar
//   Pine_A 4273 adet, olcek 0.45 - 1.03  -> LOD0 sadece 67 m'ye kadar
//   (Prefab boyu 28-29 m ama terrain instance'lari heightScale ile
//    kucultulmus; LOD gecisi BOYA orantili oldugu icin cok yakina geliyor.)
//
// DENENEN 1 - golgeyi LOD gecisinden once bitirmek: BU SAHNEDE ISE YARAMAZ.
//   En kucuk agac 49 m'de LOD atliyor; golge mesafesini 46 m'ye cekmek
//   gerekirdi, o da kullanilamaz.
//
// DENENEN 2 - LOD1 VE LOD2'ye golge acmak: OLCULDU, PAHALI.
//   Shadow casters 2720 -> 9818, Tris 7.8M -> 15.4M, FPS 55 -> 35.
//   Suclu LOD2: 156-404 m gibi devasa bir alani kapsiyor.
//
// KULLANILAN - SADECE LOD1 + golge mesafesini LOD2 gecisinin altina cekmek:
//   LOD1 de golge dokunce sinir bir sonraki gecise kayar:
//     Pine_B LOD2 basi 114 m, Pine_A LOD2 basi 156 m -> belirleyici 114 m
//   shadowDistance 110 secilir. Boylece HER agac, golge menzilinin tamaminda
//   golge doker ve golge 110 m'de cascadeBorder ile YUMUSAK biter.
//   LOD2 ve otesi asla acilmaz.
//
// KURAL: shadowDistance < golge dokmeyen ILK LOD'un basladigi mesafe.
// RAPOR bunu her calistirmada kontrol eder ve dogru degeri yazar.
//
// LOD3 (billboard) her durumda KAPALI birakilir: duz kartin golgesi gercekci
// gorunmez ve zaten golge menzilinin cok otesindedir.
//
// NOT: Bu ayar prefab'a yazilir. SpeedTree .st dosyasi yeniden import edilirse
// prefab yeniden uretilebilir ve ayar kaybolabilir - o zaman '2' tekrar
// calistirilir. '1 - RAPOR' durumu her zaman gosterir.
//
// Menu: Tools -> Foggy Road/Agac LOD/...
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FoggyRoad_AgacGolgeLOD
{
    // Billboard LOD'una golge actirilmaz.
    const bool BillboardGolgesi = false;

    // ==================================================================
    //  1 - RAPOR
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Agac LOD/1 - RAPOR (LOD ve golge mesafelerini olc)", false, 0)]
    public static void Rapor()
    {
        string metin = Olc();
        Debug.Log(metin);
        EditorUtility.DisplayDialog("Agac LOD raporu", metin, "Tamam");
    }

    // ==================================================================
    //  2 - DUZELT
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Agac LOD/2 - LOD1 golgesini AC (LOD2 kapali kalir)", false, 1)]
    public static void Duzelt() { GolgeAyarla(true); }

    // ==================================================================
    //  3 - GERI AL
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Agac LOD/3 - GERI AL (LOD golgelerini kapat)", false, 2)]
    public static void GeriAl() { GolgeAyarla(false); }

    static void GolgeAyarla(bool ac)
    {
        List<GameObject> agaclar = AgacPrefablari();
        if (agaclar.Count == 0)
        {
            Debug.LogError("[FoggyRoad] Terrain'de agac prototipi bulunamadi.");
            return;
        }

        int degisen = 0;

        foreach (GameObject prefab in agaclar)
        {
            LODGroup grup = prefab.GetComponent<LODGroup>();
            if (grup == null)
            {
                Debug.LogWarning("[FoggyRoad] '" + prefab.name + "': LODGroup yok, atlandi.");
                continue;
            }

            LOD[] lodlar = grup.GetLODs();
            bool prefabDegisti = false;

            for (int i = 0; i < lodlar.Length; i++)
            {
                // LOD0 her zaman golge dokmeli, elleme.
                if (i == 0) continue;

                bool sonLod = i == lodlar.Length - 1;
                bool billboard = sonLod && grup.lastLODBillboard;

                // SADECE LOD1. LOD2 ve otesi ASLA acilmaz:
                // LOD2 156-404 m gibi devasa bir alani kapsar ve golge pass'ini
                // patlatir (olculdu: LOD1+LOD2 acikken casters 2720 -> 9818).
                // LOD1 ise golge mesafesinin (110 m) icinde kalan dar bir bant.
                bool hedef = ac && i == 1 && !(billboard && !BillboardGolgesi);

                foreach (Renderer r in lodlar[i].renderers)
                {
                    if (r == null) continue;

                    ShadowCastingMode yeni = hedef ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    if (r.shadowCastingMode == yeni) continue;

                    Undo.RecordObject(r, "Agac LOD golgesi");
                    r.shadowCastingMode = yeni;
                    EditorUtility.SetDirty(r);
                    prefabDegisti = true;
                    degisen++;

                    Debug.Log("[FoggyRoad] '" + prefab.name + "' LOD" + i +
                              (billboard ? " (billboard)" : "") +
                              " castShadows -> " + yeni);
                }
            }

            if (prefabDegisti)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        AssetDatabase.SaveAssets();

        if (degisen == 0)
        {
            Debug.Log("[FoggyRoad] Degisiklik gerekmedi - LOD golgeleri zaten istenen durumda.");
            return;
        }

        Debug.Log("[FoggyRoad] " + degisen + " renderer guncellendi ve prefablar KAYDEDILDI.\n" +
                  (ac
                    ? "  Golge artik LOD gecisinde degil, shadowDistance'ta biter.\n" +
                      "  Play'e girip geri git: golge 151 m'de kaybolmamali, 170 m'de\n" +
                      "  yumusakca solmali. Statistics'te Shadow casters bir miktar artar."
                    : "  LOD golgeleri kapatildi (eski hal)."));
    }

    // ==================================================================
    //  Olcum
    // ==================================================================
    static string Olc()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[FoggyRoad] AGAC LOD / GOLGE RAPORU");

        // --- golge mesafesi
        float golgeMesafesi = QualitySettings.shadowDistance;
        UniversalRenderPipelineAsset urp =
            GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset
            ?? QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel())
               as UniversalRenderPipelineAsset;
        if (urp != null) golgeMesafesi = urp.shadowDistance;

        float lodBias = QualitySettings.lodBias;
        float fov = KameraFov();

        // LOD gecis mesafesi formulu (Unity LODGroup):
        //   ekranYuksekligiOrani = boy / (mesafe * 2 * tan(fov/2))
        //   lodBias bu orani boler, yani gecisi UZAGA iter.
        //   => mesafe = boy * lodBias / (2*tan(fov/2) * oran)
        float tanTerim = 2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

        sb.AppendLine("  Golge mesafesi : " + golgeMesafesi.ToString("F0") + " m");
        sb.AppendLine("  lodBias        : " + lodBias.ToString("F2"));
        sb.AppendLine("  Kamera FOV     : " + fov.ToString("F0"));
        sb.AppendLine();

        List<GameObject> agaclar = AgacPrefablari();
        if (agaclar.Count == 0)
        {
            sb.AppendLine("  Terrain'de agac prototipi bulunamadi.");
            return sb.ToString();
        }

        bool sorunVar = false;

        foreach (GameObject prefab in agaclar)
        {
            LODGroup grup = prefab.GetComponent<LODGroup>();
            if (grup == null)
            {
                sb.AppendLine("  " + prefab.name + " : LODGroup YOK");
                continue;
            }

            LOD[] lodlar = grup.GetLODs();

            // GERCEK boy = prefab boyu x terrain'deki olcek.
            // En kucuk agac en yakinda LOD atlar; golge kesimi icin belirleyici odur.
            float enKucukOlcek, ortOlcek;
            int adet;
            OlcekAraligi(prefab, out enKucukOlcek, out ortOlcek, out adet);

            float boy = grup.size * enKucukOlcek;

            sb.AppendLine("  " + prefab.name + "  (" + adet + " adet dikili)");
            sb.AppendLine("    prefab boyu " + grup.size.ToString("F1") +
                          " m,  olcek en kucuk " + enKucukOlcek.ToString("F2") +
                          " / ortalama " + ortOlcek.ToString("F2"));
            sb.AppendLine("    hesap EN KUCUK agaca gore: " + boy.ToString("F1") + " m" +
                          (grup.lastLODBillboard ? "   (son LOD billboard)" : ""));

            // Golgenin kesildigi ilk mesafe: castShadows kapali ilk LOD'un baslangici
            float golgeKesim = -1f;

            for (int i = 0; i < lodlar.Length; i++)
            {
                float oran = lodlar[i].screenRelativeTransitionHeight;
                // Bu LOD'un BITTIGI (bir sonrakine gectigi) mesafe
                float mesafe = oran > 0.0001f ? boy * lodBias / (tanTerim * oran) : 0f;

                bool golgeVar = false;
                foreach (Renderer r in lodlar[i].renderers)
                    if (r != null && r.shadowCastingMode != ShadowCastingMode.Off)
                        golgeVar = true;

                // LOD i, bir onceki LOD'un bitis mesafesinde BASLAR
                float baslangic = 0f;
                if (i > 0)
                {
                    float oncekiOran = lodlar[i - 1].screenRelativeTransitionHeight;
                    if (oncekiOran > 0.0001f)
                        baslangic = boy * lodBias / (tanTerim * oncekiOran);
                }

                sb.AppendLine("    LOD" + i +
                              " : " + baslangic.ToString("F0") + " - " + mesafe.ToString("F0") + " m" +
                              "   golge " + (golgeVar ? "VAR" : "yok"));

                if (!golgeVar && golgeKesim < 0f && i > 0)
                    golgeKesim = baslangic;
            }

            if (golgeKesim >= 0f && golgeKesim < golgeMesafesi)
            {
                sorunVar = true;
                sb.AppendLine("    >> SORUN: golge " + golgeKesim.ToString("F0") +
                              " m'de LOD yuzunden SERT kesiliyor,");
                sb.AppendLine("       oysa golge mesafesi " + golgeMesafesi.ToString("F0") +
                              " m. Cascade fade'e hic ulasamiyor.");
                sb.AppendLine("       COZUM: golge mesafesini " +
                              (golgeKesim * 0.92f).ToString("F0") + " m'ye cek " +
                              "(LOD gecisinin ALTINA).");
            }
            else if (golgeKesim >= 0f)
            {
                sb.AppendLine("    OK: golge kesimi (" + golgeKesim.ToString("F0") +
                              " m) golge mesafesinin disinda.");
            }
            else
            {
                sb.AppendLine("    OK: tum LOD'lar golge dokuyor.");
            }
            sb.AppendLine();
        }

        // --- terrain tarafi
        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            sb.AppendLine("  Terrain '" + t.name + "': treeDistance = " + t.treeDistance +
                          " m, billboardStart = " + t.treeBillboardDistance +
                          " m, maxMeshTrees = " + t.treeMaximumFullLODCount);

            Camera k = OyunKamerasi();
            if (k != null && t.treeDistance < k.farClipPlane)
                sb.AppendLine("    >> DIKKAT: treeDistance (" + t.treeDistance +
                              ") kamera farClip'inden (" + k.farClipPlane +
                              ") kucuk. Agaclar gorus alani ICINDE fade'siz kesilir.");
        }

        sb.AppendLine();
        sb.AppendLine(sorunVar
            ? "  DUZELTMEK ICIN: golge mesafesini yukarida yazan degere cek.\n" +
              "  'Golge > 2 - KALITE' preset'i bunu zaten uygular.\n" +
              "  (LOD1/LOD2'ye golge actirmak denendi: Tris 2x, FPS yariya dustu.)"
            : "  Golge tarafinda LOD kaynakli sorun gorunmuyor.");

        return sb.ToString();
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================
    /// <summary>Sahnedeki terrain'lerin agac prototipi prefablari (tekrarsiz).</summary>
    static List<GameObject> AgacPrefablari()
    {
        List<GameObject> liste = new List<GameObject>();

        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (t.terrainData == null) continue;

            foreach (TreePrototype p in t.terrainData.treePrototypes)
            {
                if (p.prefab == null) continue;
                if (!liste.Contains(p.prefab)) liste.Add(p.prefab);
            }
        }

        return liste;
    }

    /// <summary>
    /// Terrain'e dikilen agaclarin GERCEK olcegi. Prefabin LODGroup.size'i
    /// olceksiz boyu verir; terrain instance'lari heightScale ile buyutulup
    /// kucultulur. LOD gecisi boya orantili oldugu icin EN KUCUK agac EN YAKINDA
    /// LOD atlar - golge kesimi icin belirleyici olan odur.
    /// </summary>
    static void OlcekAraligi(GameObject prefab, out float enKucuk, out float ortalama, out int adet)
    {
        enKucuk = float.MaxValue;
        float toplam = 0f;
        adet = 0;

        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            TerrainData td = t.terrainData;
            if (td == null) continue;

            // Bu prefabin prototip indeksleri
            List<int> indeksler = new List<int>();
            TreePrototype[] protolar = td.treePrototypes;
            for (int i = 0; i < protolar.Length; i++)
                if (protolar[i] != null && protolar[i].prefab == prefab) indeksler.Add(i);

            if (indeksler.Count == 0) continue;

            TreeInstance[] agaclar = td.treeInstances;
            for (int i = 0; i < agaclar.Length; i++)
            {
                if (!indeksler.Contains(agaclar[i].prototypeIndex)) continue;

                float h = agaclar[i].heightScale;
                if (h <= 0f) continue;

                if (h < enKucuk) enKucuk = h;
                toplam += h;
                adet++;
            }
        }

        if (adet == 0) { enKucuk = 1f; ortalama = 1f; return; }
        ortalama = toplam / adet;
    }

    static Camera OyunKamerasi()
    {
        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c.cameraType == CameraType.Game && c.gameObject.activeInHierarchy && c.enabled)
                return c;
        return null;
    }

    static float KameraFov()
    {
        Camera k = OyunKamerasi();
        if (k != null && !k.orthographic) return k.fieldOfView;

        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            return SceneView.lastActiveSceneView.camera.fieldOfView;

        return 60f;
    }
}
#endif
