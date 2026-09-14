// POP-IN + PERFORMANS DENGESI
//
// Pop-in ("yurudukce golge/cimen/agac birden beliriyor") ile FPS birbirinin
// zitti. Bu arac ikisini birlikte, TUTARLI sekilde ayarlar - yarim uygulanmis
// ayar kombinasyonu en kotu sonucu verir (maliyeti odersin, karsiligini alamazsin).
//
// Pop-in kaynaklari ve burada nasil ele alindiklari:
//   1) Gunes isiginda "Shadow Caster Culling = View Frustum": kadraj disindaki
//      agaclar golge dokmez, agac kadraja girince golgesi birden dogar. -> kapatilir
//   2) Bake'lenmis Occlusion Culling verisi bayat: hucre sinirini gectikce objeler
//      pat diye girer. -> kamerada occlusion culling kapatilir
//   3) GPU Resident Drawer'in kamera-ici occlusion culling'i bir onceki karenin
//      derinligini kullanir -> hareket ederken bir kare geriden acar. -> kapatilir
//      Small Mesh Screen Percentage de ayni sekilde ani belirme yapar. -> 0
//   4) Terrain agac billboard mesafesi: o mesafeden otesi duz kart, hem bulanik
//      gorunur hem mesh'e donerken golgesi aniden cikar. -> uzaga itilir
//   5) Terrain detail (cim) mesafesi: SERT kesim, fade yok. -> uzaga itilir
//   6) FoggyRoadFoliageOptimization renderer'lari sert esikle keser. -> kapatilir
//   7) Kamera far clip < terrain agac mesafesi olursa agaclar duvara toslar.
//
// Maliyet kaldiraclari (FPS'i belirleyen asil seyler):
//   - Cascade sayisi: golge geometrisi kadraja EK OLARAK bu kadar kez daha cizilir
//   - LOD Bias: agaclarin yuksek detayli LOD0'da kalma mesafesini carpar
//   - Tree Maximum Full LOD Count: kac agacin tam detayda cizilecegi
//   - Shadow Distance: kac obje golge dokucusu olur
//
// Menu: Tools -> Foggy Road/Golge/1 - DENGELI ...
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FoggyRoad_PopInTamDuzeltme
{
    enum Seviye { Dengeli, Kalite, Performans }

    /// <summary>Bir seviyenin tum degerleri. Tek yerde durur ki tutarsiz kalmasin.</summary>
    class Ayar
    {
        public string ad;
        public float shadowDistance;
        public int cascades;
        public float cascadeBorder;
        public float cascade2Split;
        public int shadowAtlas;        // 2048 -> her cascade 1024, 4096 -> 2048
        public int softShadowQuality;  // 1 Low, 2 Medium, 3 High
        public float shadowNormalBias;
        public float lodBias;
        public float smallMeshPercent;
        public bool shadowCasterFrustumCull;   // true = kadraj disi golge kesilir (ucuz ama pop yapar)
        public float farClip;

        public float treeBillboard;
        public float treeDistance;
        public float treeCrossFade;
        public int treeMaxFullLOD;
        public float detailDistance;
        // Cim YOGUNLUGU artik TerrainData'nin detail haritalarinda yonetiliyor
        // (Tools > Foggy Road > Cim). Bu alan her preset'te 1.0 kalir; yoksa
        // preset her calistiginda haritadaki yogunlugu tekrar boler.
        // Preset basina ayrisan kaldirac MESAFE'dir (detailDistance).
        public float detailDensity;
        public float pixelError;
        public bool terrainTwoSidedShadow;

        public float fogStart;
        public float fogMaxDistance;
        public float fogMaxOpacity;
        public float fogHeightFalloff;
        // Gunes sacilimi: directLight = mainLight.color * phase * _SunScattering
        // phase 5'e kadar cikar ve gokyuzu pikselleri HER ZAMAN tam mesafe fog
        // biriktirir (shader'da rayLength = _MaxDistance). Yuksek deger, gunese
        // bakildiginda gokyuzunu duz sariya cevirir.
        public float fogSunScattering;
        public float fogAnisotropy;
    }

    static Ayar Getir(Seviye s)
    {
        switch (s)
        {
            case Seviye.Kalite:
                return new Ayar
                {
                    // Golge mesafesi GUNES ACISINA gore secilir: bir agacin golgesinin
                    // cizilebilmesi icin AGACIN KENDISI menzilde olmali.
                    //   gunes 14 derece -> 35 m agacin golgesi 140 m -> 170 m yeterli (30 m pay)
                    //   gunes  7 derece -> 35 m agacin golgesi 285 m -> 300 m gerekirdi
                    // Gunes 7'den 14'e cikarildigi icin 260 -> 170 dusuruldu; 120 m bosuna
                    // odeniyordu. Golge hacmi mesafenin karesiyle olceklenir: (170/260)^2 = 0.43
                    ad = "KALITE", shadowDistance = 170f, cascades = 3, cascadeBorder = 0.30f,
                    cascade2Split = 0.33f, shadowAtlas = 4096, softShadowQuality = 3,
                    shadowNormalBias = 0.5f,
                    // Asagidakiler gorunumu bozmadan maliyeti dusuren kalemler:
                    //   lodBias 2.2 -> 1.8 : LOD gecisleri hala gec, ucgen belirgin azalir
                    //   treeMaxFullLOD 250 -> 150 : sadece en uzaktaki mesh agaclar billboard olur
                    //                               (billboard zaten 250 m'de basliyor)
                    //   terrainTwoSidedShadow -> false : zemin golgesi iki kez islenmez
                    //   detailDensity 0.65 -> 0.55 : cim CPU maliyeti, mesafe aynen korunur
                    // smallMeshPercent 1: uzaktaki kucuk mesh'leri eler, risk ~sifir
                    lodBias = 1.8f, smallMeshPercent = 1f, shadowCasterFrustumCull = false,
                    farClip = 500f,
                    treeBillboard = 250f, treeDistance = 400f, treeCrossFade = 40f,
                    treeMaxFullLOD = 150, detailDistance = 130f, detailDensity = 1.0f,
                    pixelError = 2f, terrainTwoSidedShadow = false,
                    // Sis, golge menzilinin bittigi yeri ortmeli: 330 m < 260 m golge + pay
                    fogStart = 80f, fogMaxDistance = 330f, fogMaxOpacity = 0.42f, fogHeightFalloff = 0.45f,
                    fogSunScattering = 0.18f, fogAnisotropy = 0.35f
                };

            case Seviye.Performans:
                return new Ayar
                {
                    ad = "PERFORMANS", shadowDistance = 110f, cascades = 2, cascadeBorder = 0.25f,
                    cascade2Split = 0.33f, shadowAtlas = 2048, softShadowQuality = 2,
                    shadowNormalBias = 0.6f,
                    lodBias = 1.0f, smallMeshPercent = 1f, shadowCasterFrustumCull = true,
                    farClip = 320f,
                    treeBillboard = 130f, treeDistance = 260f, treeCrossFade = 25f,
                    treeMaxFullLOD = 60, detailDistance = 70f, detailDensity = 1.0f,
                    pixelError = 8f, terrainTwoSidedShadow = false,
                    fogStart = 50f, fogMaxDistance = 190f, fogMaxOpacity = 0.60f, fogHeightFalloff = 0.45f,
                    fogSunScattering = 0.14f, fogAnisotropy = 0.32f
                };

            default: // Dengeli
                return new Ayar
                {
                    ad = "DENGELI", shadowDistance = 170f, cascades = 2, cascadeBorder = 0.30f,
                    cascade2Split = 0.33f, shadowAtlas = 4096, softShadowQuality = 3,
                    shadowNormalBias = 0.5f,
                    lodBias = 1.4f, smallMeshPercent = 0f, shadowCasterFrustumCull = false,
                    farClip = 400f,
                    treeBillboard = 200f, treeDistance = 300f, treeCrossFade = 35f,
                    treeMaxFullLOD = 120, detailDistance = 110f, detailDensity = 1.0f,
                    pixelError = 4f, terrainTwoSidedShadow = false,
                    fogStart = 70f, fogMaxDistance = 280f, fogMaxOpacity = 0.50f, fogHeightFalloff = 0.45f,
                    fogSunScattering = 0.16f, fogAnisotropy = 0.35f
                };
        }
    }

    // ==================================================================
    [MenuItem("Tools/Foggy Road/Golge/1 - DENGELI (FPS dusukse, gunes 14 derece uzeri ister)")]
    public static void Dengeli() { Uygula(Seviye.Dengeli); }

    [MenuItem("Tools/Foggy Road/Golge/2 - KALITE (ONERILEN - gunes 12 derece ve uzeri)")]
    public static void Kalite() { Uygula(Seviye.Kalite); }

    [MenuItem("Tools/Foggy Road/Golge/3 - PERFORMANS (FPS oncelikli)")]
    public static void Performans() { Uygula(Seviye.Performans); }

    static void Uygula(Seviye seviye)
    {
        Ayar a = Getir(seviye);
        Debug.Log("===== [FoggyRoad] " + a.ad + " PRESET UYGULANIYOR =====");

        IsiklariAyarla(a);
        KameraAyarla(a);
        UrpAyarla(a);
        KaliteAyarla(a);
        ScriptleriAyarla();
        TerrainAyarla(a);
        SisAyarla(a);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        // Sahne ayarlari daha once birkac kez kaydedilmeden kayboldu; hatirlat.
        bool kaydet = EditorUtility.DisplayDialog(
            "Sahneyi kaydet",
            a.ad + " preset'i uygulandi.\n\nTerrain ve kamera ayarlari SAHNEYE yazildi. " +
            "Kaydetmezsen Unity'yi kapatinca bu ayarlar kaybolur (daha once oldu).\n\n" +
            "Simdi kaydedilsin mi?",
            "Kaydet", "Simdi degil");

        if (kaydet)
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[FoggyRoad] Sahne kaydedildi.");
        }
        else
        {
            Debug.LogWarning("[FoggyRoad] Sahne KAYDEDILMEDI. Ctrl+S yapmayi unutma.");
        }
    }

    // ---------------------------------------------------------------- Isik
    static void IsiklariAyarla(Ayar a)
    {
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional || l.shadows == LightShadows.None) continue;
            Undo.RecordObject(l, "Pop-in preset");

            // Kadraj disindaki agaclarin golgesi kesilmesin (pop-in'in en gorunur sebebi).
            l.useViewFrustumForShadowCasterCull = a.shadowCasterFrustumCull;
            l.shadowBias = 0.05f;
            l.shadowNormalBias = 0.4f;

            Debug.Log("[FoggyRoad] Isik '" + l.name + "': ShadowCasterCull(ViewFrustum) = " +
                      (a.shadowCasterFrustumCull ? "ACIK (ucuz, pop yapabilir)" : "KAPALI"));
            EditorUtility.SetDirty(l);

            GolgeUzunluguKontrol(l, a);
        }
    }

    /// <summary>
    /// Alcak gunes = cok uzun golge. Bir agacin golgesinin cizilebilmesi icin AGACIN
    /// KENDISI golge menzilinde olmali. Gunes cok alcaksa, golgesi onunde duran agac
    /// menzilin disinda kalir ve golge "bir anda kesilir" - sik karsilasilan ama
    /// tespiti zor bir pop kaynagi. Burada hesaplayip uyariyoruz.
    /// </summary>
    static void GolgeUzunluguKontrol(Light l, Ayar a)
    {
        const float AgacBoyu = 25f;   // sahnedeki cam agaclarinin yaklasik boyu

        // Isigin ufuktan yuksekligi
        float yukseklik = -l.transform.forward.y;
        yukseklik = Mathf.Clamp(yukseklik, -1f, 1f);
        float aci = Mathf.Asin(yukseklik) * Mathf.Rad2Deg;

        if (aci <= 0.5f) return;   // gunes batmis, golge zaten yok

        float golgeUzunlugu = AgacBoyu / Mathf.Tan(aci * Mathf.Deg2Rad);
        if (golgeUzunlugu <= a.shadowDistance)
        {
            Debug.Log("[FoggyRoad] Gunes " + aci.ToString("0.0") + " derece -> " +
                      AgacBoyu + " m agacin golgesi " + golgeUzunlugu.ToString("0") +
                      " m. Golge mesafesi " + a.shadowDistance + " m, yeterli.");
            return;
        }

        float gerekenAci = Mathf.Atan(AgacBoyu / a.shadowDistance) * Mathf.Rad2Deg;

        string nl = System.Environment.NewLine;
        Debug.LogWarning(
            "[FoggyRoad] GOLGE KESILME UYARISI" + nl +
            "Gunes " + aci.ToString("0.0") + " derece yukseklikte. " + AgacBoyu +
            " m'lik bir agacin golgesi " + golgeUzunlugu.ToString("0") + " m uzuyor, " +
            "ama golge mesafesi sadece " + a.shadowDistance + " m." + nl +
            "Sonuc: golgesi hala kadrajda olan agaclar menzilden cikinca golgeleri " +
            "BIR ANDA kesilir (yurudukce 'arkadaki agacin golgesi kayboldu' sikayeti)." + nl +
            "Cozum A: Directional Light Rotation X >= " + gerekenAci.ToString("0") + " yap." + nl +
            "Cozum B: Shadow Distance'i " + golgeUzunlugu.ToString("0") + " m'ye cikar (FPS pahali).");
    }

    // -------------------------------------------------------------- Kamera
    static void KameraAyarla(Ayar a)
    {
        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.cameraType != CameraType.Game) continue;
            Undo.RecordObject(cam, "Pop-in preset");

            // Bake'lenmis occlusion verisi bayat -> yanlis culling -> obje pop'u.
            cam.useOcclusionCulling = false;

            // Far clip, terrain agac mesafesinin altinda kalirsa agaclar duvara toslar.
            cam.farClipPlane = Mathf.Max(a.farClip, a.treeDistance + 40f);

            Debug.Log("[FoggyRoad] Kamera '" + cam.name + "': OcclusionCulling KAPALI, farClip = " +
                      cam.farClipPlane);
            EditorUtility.SetDirty(cam);
        }
    }

    // ----------------------------------------------------------------- URP
    static void UrpAyarla(Ayar a)
    {
        UniversalRenderPipelineAsset asset =
            GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset
            ?? QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel())
               as UniversalRenderPipelineAsset;

        if (asset == null) { Debug.LogWarning("[FoggyRoad] URP asset yok."); return; }

        SerializedObject so = new SerializedObject(asset);

        // GPU occlusion culling bir onceki karenin derinligini kullanir -> hareket ederken pop.
        SetBool(so, "m_GPUResidentDrawerEnableOcclusionCullingInCameras", false);
        SetFloat(so, "m_SmallMeshScreenPercentage", a.smallMeshPercent);

        SetFloat(so, "m_ShadowDistance", a.shadowDistance);
        SetInt(so, "m_ShadowCascadeCount", a.cascades);
        SetFloat(so, "m_CascadeBorder", a.cascadeBorder);
        SetBool(so, "m_EnableLODCrossFade", true);

        // Cascade kademesi: URP cascade'ler ARASINDA harmanlama yapmaz (HDRP yapar).
        // Sinirda golge cozunurlugu sert biciminde degisir - "golge aniden duzeliyor"
        // sikayetinin sebebi budur. Atlasi buyutup cascade sayisini azaltmak hem
        // kademeyi kucultur hem golge geometrisini daha az kez cizer.
        SetInt(so, "m_MainLightShadowmapResolution", a.shadowAtlas);
        SetInt(so, "m_SoftShadowQuality", a.softShadowQuality);
        SetBool(so, "m_SoftShadowsSupported", true);
        SetFloat(so, "m_ShadowNormalBias", a.shadowNormalBias);
        SetFloat(so, "m_ShadowDepthBias", 0.4f);

        // KRITIK DENGE: Golge mesafesinden otesi GOLGESIZ cizilir. Oyuncu o mesafeden
        // uzagi net gorebiliyorsa, yurudukce golge sinirinin suzulup gectigini gorur
        // ("golge sonradan aniden geliyor"). Bu yuzden golge mesafesi ile sisin
        // kapatma mesafesi birlikte ayarlanir; sis, golge sinirini gizlemelidir.

        // Cascade bolunmeleri: yakini sik, uzagi seyrek
        SerializedProperty c2 = so.FindProperty("m_Cascade2Split");
        if (c2 != null) c2.floatValue = a.cascade2Split;
        // 3 cascade kullanilirsa sinirlari uzaga it: yakin alan tek cascade'de kalsin.
        SerializedProperty c3 = so.FindProperty("m_Cascade3Split");
        if (c3 != null) c3.vector2Value = new Vector2(0.25f, 0.55f);
        SerializedProperty c4 = so.FindProperty("m_Cascade4Split");
        if (c4 != null) c4.vector3Value = new Vector3(0.08f, 0.22f, 0.48f);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        float cascade0 = a.shadowDistance * a.cascade2Split;
        Debug.Log("[FoggyRoad] URP '" + asset.name + "': shadowDistance = " + a.shadowDistance +
                  ", cascade = " + a.cascades + " (ilk sinir ~" + cascade0.ToString("0") + " m)" +
                  ", atlas = " + a.shadowAtlas + ", softShadow = " + a.softShadowQuality +
                  ", smallMesh% = " + a.smallMeshPercent + ", GPU occlusion KAPALI");
    }

    // ------------------------------------------------------------- Quality
    static void KaliteAyarla(Ayar a)
    {
        int onceki = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            if (QualitySettings.names[i] != "PC") continue;

            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.shadowDistance = a.shadowDistance;
            QualitySettings.shadowCascades = a.cascades;
            QualitySettings.lodBias = a.lodBias;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.enableLODCrossFade = true;
            QualitySettings.softVegetation = true;

            Debug.Log("[FoggyRoad] Quality 'PC': lodBias = " + a.lodBias +
                      ", shadowDistance = " + a.shadowDistance +
                      ", cascade = " + a.cascades);
        }
        QualitySettings.SetQualityLevel(onceki, false);
    }

    // ------------------------------------------------------- Sahne scriptleri
    static void ScriptleriAyarla()
    {
        // ASIL SUCLU: DynamicWeatherSystem.EnsureSceneComponents() her OnEnable'da
        // (domain reload, sahne acilisi, Play'e giris) Terrain ayarlarini sabit
        // degerlerle EZIYORDU. Ayarlarin surekli "geri donmesinin" sebebi buydu.
        foreach (DynamicWeatherSystem dws in
                 Object.FindObjectsByType<DynamicWeatherSystem>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject dso = new SerializedObject(dws);
            SerializedProperty prop = dso.FindProperty("overrideTerrainSettings");
            if (prop == null) continue;

            if (prop.boolValue)
            {
                prop.boolValue = false;
                dso.ApplyModifiedProperties();
                Debug.Log("[FoggyRoad] '" + dws.name + "' DynamicWeatherSystem: " +
                          "Terrain override KAPATILDI (sahnedeki Terrain degerleri artik kalici).");
            }
        }

        // Renderer'lari sert mesafe esiginde kesen optimizasyon: gorsel pop'un
        // en dogrudan sebebi. Her seviyede kapali kalir; FPS icin dogru kaldirac
        // LOD/cascade tarafi, burasi degil.
        foreach (FoggyRoadFoliageOptimization opt in
                 Object.FindObjectsByType<FoggyRoadFoliageOptimization>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(opt, "Pop-in preset");
            opt.limitDistantFoliageShadows = false;
            opt.limitDistantRockShadows = false;
            opt.cullFogHiddenFoliage = false;
            opt.cullSmallFoliageEarlier = false;

            // Tum kesmeler kapaliyken bilesen sadece bos yere her 0.2 sn'de tum
            // renderer listesini dolasir. Kapat.
            opt.enabled = false;

            EditorUtility.SetDirty(opt);
            Debug.Log("[FoggyRoad] '" + opt.name + "' FoliageOptimization: sert kesmeler KAPALI, " +
                      "bilesen devre disi (bos dongu maliyeti yok)");
        }

        foreach (FoggyRoad_LayerCulling lc in
                 Object.FindObjectsByType<FoggyRoad_LayerCulling>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(lc, "Pop-in preset");
            lc.bariyerMesafe = 260f;
            lc.propMesafe = 300f;
            lc.Uygula();
            EditorUtility.SetDirty(lc);
            Debug.Log("[FoggyRoad] '" + lc.name + "' LayerCulling: bariyer 260 m, prop 300 m");
        }
    }

    // ------------------------------------------------------------- Terrain
    static void TerrainAyarla(Ayar a)
    {
        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Pop-in preset");

            t.treeBillboardDistance = a.treeBillboard;
            t.treeDistance = a.treeDistance;
            t.treeCrossFadeLength = a.treeCrossFade;

            // EN BUYUK UCGEN KALDIRACI: kac agac tam detayda cizilecek.
            t.treeMaximumFullLODCount = a.treeMaxFullLOD;

            t.detailObjectDistance = a.detailDistance;
            t.detailObjectDensity = a.detailDensity;
            t.heightmapPixelError = a.pixelError;

            // TwoSided golge, golge gecisinde geometriyi iki kez isler.
            t.shadowCastingMode = a.terrainTwoSidedShadow
                ? ShadowCastingMode.TwoSided
                : ShadowCastingMode.On;

            EditorUtility.SetDirty(t);

            Debug.Log("[FoggyRoad] Terrain '" + t.name + "': billboard = " + a.treeBillboard +
                      ", treeDistance = " + a.treeDistance +
                      ", maxFullLOD = " + a.treeMaxFullLOD +
                      ", detail = " + a.detailDistance +
                      ", pixelError = " + a.pixelError);
        }
    }

    // ----------------------------------------------------------------- Sis
    static void SisAyarla(Ayar a)
    {
        const string yol = "Assets/FoggyRoad/Looks/ReferencePhotoFogV2.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(yol);
        if (m == null)
        {
            Debug.Log("[FoggyRoad] Sis materyali bulunamadi, atlandi: " + yol);
            return;
        }

        Undo.RecordObject(m, "Pop-in preset");

        // Sis kameraya GORELI calisir: StartDistance kucukse nereye yurursen yuru
        // o mesafenin otesi hep yikanmis gorunur.
        FloatAta(m, "_StartDistance", a.fogStart);
        FloatAta(m, "_MaxDistance", a.fogMaxDistance);
        FloatAta(m, "_MaxOpacity", a.fogMaxOpacity);
        FloatAta(m, "_HeightFalloff", a.fogHeightFalloff);
        FloatAta(m, "_SunScattering", a.fogSunScattering);
        FloatAta(m, "_Anisotropy", a.fogAnisotropy);

        EditorUtility.SetDirty(m);

        Debug.Log("[FoggyRoad] Sis: start = " + a.fogStart + " m, maxDistance = " + a.fogMaxDistance +
                  " m, maxOpacity = " + a.fogMaxOpacity + ", heightFalloff = " + a.fogHeightFalloff +
                  ", sunScattering = " + a.fogSunScattering + ", anisotropy = " + a.fogAnisotropy);
    }

    // ------------------------------------------------------------ yardimcilar
    static void SetBool(SerializedObject so, string ad, bool deger)
    { SerializedProperty p = so.FindProperty(ad); if (p != null) p.boolValue = deger; }

    static void SetInt(SerializedObject so, string ad, int deger)
    { SerializedProperty p = so.FindProperty(ad); if (p != null) p.intValue = deger; }

    static void SetFloat(SerializedObject so, string ad, float deger)
    {
        SerializedProperty p = so.FindProperty(ad);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Integer) p.intValue = Mathf.RoundToInt(deger);
        else p.floatValue = deger;
    }

    static void FloatAta(Material m, string ad, float v)
    {
        if (m.HasProperty(ad)) m.SetFloat(ad, v);
        else Debug.Log("[FoggyRoad] Sis materyalinde '" + ad + "' property'si yok, atlandi.");
    }
}
#endif
