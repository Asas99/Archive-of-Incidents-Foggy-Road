// KAYDET ve KILITLE
//
// Amac: Unity kapatilip tekrar acildiginda ayarlarin AYNI kalmasi.
//
// Bu oturumda ayarlarin surekli geri donmesinin sebebi bir script'ti:
// DynamicWeatherSystem.EnsureSceneComponents() her OnEnable'da (domain reload,
// sahne acilisi, Play'e giris) Terrain ayarlarini sabit degerlerle eziyordu.
// Artik 'overrideTerrainSettings' anahtarina bagli ve varsayilan kapali.
//
// Bu arac:
//   1) Yuklenme aninda deger yazan TUM bilesenleri denetler ve zorlamayi kapatir
//   2) Kilitlenen degerleri Console'a yazar (dogrulayabilesin diye)
//   3) Sahneyi + asset'leri kaydeder
//
// DENETIM SONUCU (sahnedeki tum script'ler tarandi):
//   DynamicWeatherSystem      -> Terrain zorlamasi anahtara baglandi, kapali
//   FoggyRoadFoliageOptimization -> bilesen devre disi, OnEnable calismiyor
//   FoggyRoad_LayerCulling    -> sadece kendi serialize alanlarini kameraya yazar
//                                (bariyer/prop cizim mesafesi), baska ayara dokunmaz
//   FoggyRoad_PowerLine       -> sadece kendi kablo mesh'ini yeniden kurar
//   TerrainPainter.OnEnable   -> sadece static referans atar
//   Direction.OnEnable        -> sadece passIndex atar
//   FpsCounter / KarakterHareket -> ayar yazmaz
//   DynamicWeatherInstaller   -> OLU KOD. Sinifta [InitializeOnLoad] attribute'u
//                                yok, hicbir event'e abone degil, InstallIntoActiveScene
//                                ve TickScenePreview hicbir yerden cagrilmiyor.
//                                (Icinde treeMaximumFullLODCount = 600 ve skybox
//                                degistiren kod VAR ama calismiyor. Arac her
//                                calistiginda bunu tekrar dogrular.)
//   => Terrain / URP / Quality / Golge / Skybox ayarlarini ezen baska script YOK.
//
// Menu: Tools -> Foggy Road/0 - KAYDET ve KILITLE
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FoggyRoad_KaydetVeKilitle
{
    [MenuItem("Tools/Foggy Road/0 - KAYDET ve KILITLE (ayarlar geri donmesin)", false, -100)]
    public static void KaydetVeKilitle()
    {
        Debug.Log("===== [FoggyRoad] KAYDET ve KILITLE =====");

        int zorlamaKapatildi = ZorlamalariKapat();
        DegerleriYaz();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        bool sahneOk = EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (sahneOk)
        {
            Debug.Log("[FoggyRoad] Sahne ve asset'ler KAYDEDILDI. " +
                      (zorlamaKapatildi > 0
                          ? zorlamaKapatildi + " adet zorlama kapatildi."
                          : "Zorlayan bilesen bulunmadi."));
            EditorUtility.DisplayDialog(
                "Kaydedildi",
                "Sahne ve asset'ler kaydedildi.\n\n" +
                "Yuklenme aninda ayar ezen bilesen: " +
                (zorlamaKapatildi > 0 ? zorlamaKapatildi + " adet bulundu ve kapatildi."
                                      : "yok.") +
                "\n\nUnity'yi kapatip acinca ayarlar ayni kalacak.\n" +
                "Console'da kilitlenen degerlerin listesi var.",
                "Tamam");
        }
        else
        {
            Debug.LogError("[FoggyRoad] Sahne KAYDEDILEMEDI. Elle Ctrl+S dene.");
        }
    }

    // ==================================================================
    //  Yuklenmede deger yazan bilesenleri kapat
    // ==================================================================
    static int ZorlamalariKapat()
    {
        int sayac = 0;

        // 1) DynamicWeatherSystem: Terrain ayarlarini eziyordu
        foreach (DynamicWeatherSystem dws in Object.FindObjectsByType<DynamicWeatherSystem>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject so = new SerializedObject(dws);
            SerializedProperty p = so.FindProperty("overrideTerrainSettings");
            if (p == null)
            {
                Debug.LogWarning("[FoggyRoad] '" + dws.name + "': 'overrideTerrainSettings' alani " +
                                 "bulunamadi. DynamicWeatherSystem.cs eski surumde olabilir - " +
                                 "Terrain ayarlarini yine ezebilir.");
                continue;
            }

            if (p.boolValue)
            {
                p.boolValue = false;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dws);
                sayac++;
                Debug.Log("[FoggyRoad] '" + dws.name + "': Terrain zorlamasi KAPATILDI.");
            }
            else
            {
                Debug.Log("[FoggyRoad] '" + dws.name + "': Terrain zorlamasi zaten kapali. OK");
            }
        }

        // 2) FoggyRoadFoliageOptimization: renderer'lari sert esikle kesiyordu
        foreach (FoggyRoadFoliageOptimization opt in
                 Object.FindObjectsByType<FoggyRoadFoliageOptimization>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool acikVar = opt.limitDistantFoliageShadows || opt.limitDistantRockShadows ||
                           opt.cullFogHiddenFoliage || opt.cullSmallFoliageEarlier;

            if (acikVar || opt.enabled)
            {
                Undo.RecordObject(opt, "Zorlamalari kapat");
                opt.limitDistantFoliageShadows = false;
                opt.limitDistantRockShadows = false;
                opt.cullFogHiddenFoliage = false;
                opt.cullSmallFoliageEarlier = false;
                opt.enabled = false;
                EditorUtility.SetDirty(opt);
                sayac++;
                Debug.Log("[FoggyRoad] '" + opt.name + "': FoliageOptimization kesmeleri " +
                          "kapatildi ve bilesen devre disi birakildi.");
            }
            else
            {
                Debug.Log("[FoggyRoad] '" + opt.name + "': FoliageOptimization zaten kapali. OK");
            }
        }

        // 3) Kamera Clear Flags: Skybox DISINDA bir deger, URP'nin skybox pass'ini
        // tamamen devre disi birakir. Sonuc: skybox hic cizilmez (materyali
        // degistirmek hicbir sey yapmaz) ve renk buffer'i temizlenmedigi icin
        // arka plan onceki kareyi tasiyip duz, doygun bir renge doygunlasir.
        foreach (Camera cam in Object.FindObjectsByType<Camera>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (cam.cameraType != CameraType.Game) continue;
            if (!cam.gameObject.activeInHierarchy || !cam.enabled) continue;

            if (cam.clearFlags == CameraClearFlags.Skybox)
            {
                Debug.Log("[FoggyRoad] Kamera '" + cam.name + "': ClearFlags zaten Skybox. OK");
                continue;
            }

            CameraClearFlags eski = cam.clearFlags;
            Undo.RecordObject(cam, "Clear flags duzelt");
            cam.clearFlags = CameraClearFlags.Skybox;
            EditorUtility.SetDirty(cam);
            sayac++;

            Debug.Log("[FoggyRoad] Kamera '" + cam.name + "': ClearFlags " + eski +
                      " -> Skybox DUZELTILDI. Gokyuzu artik cizilecek.");
        }

        // 4) DynamicWeatherInstaller olu mu? Icinde terrain'e 600 yazan ve
        // skybox'i degistiren kod var. Su an hicbir event'e bagli degil, ama
        // biri [InitializeOnLoad] eklerse ayarlar yine ezilmeye baslar.
        // Her kayitta dogrula ki sessizce geri gelmesin.
        System.Type inst = System.Type.GetType("DynamicWeatherInstaller");
        if (inst == null)
        {
            Debug.Log("[FoggyRoad] DynamicWeatherInstaller: sinif yok. OK");
        }
        else if (inst.GetCustomAttributes(typeof(InitializeOnLoadAttribute), false).Length > 0)
        {
            Debug.LogError("[FoggyRoad] DIKKAT: DynamicWeatherInstaller'a [InitializeOnLoad] " +
                           "eklenmis! Bu sinif Terrain'e maxFullLOD = 600 yazar ve skybox'i " +
                           "degistirir. Attribute kaldirilmazsa ayarlar her acilista ezilir.");
        }
        else
        {
            Debug.Log("[FoggyRoad] DynamicWeatherInstaller: [InitializeOnLoad] yok, olu kod. OK");
        }

        return sayac;
    }

    // ==================================================================
    //  Kilitlenen degerleri yaz
    // ==================================================================
    static void DegerleriYaz()
    {
        Debug.Log("--- KILITLENEN DEGERLER ---");

        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            Debug.Log("Terrain '" + t.name + "': " +
                      "billboard = " + t.treeBillboardDistance + " m, " +
                      "treeDistance = " + t.treeDistance + " m, " +
                      "maxFullLOD = " + t.treeMaximumFullLODCount + ", " +
                      "detail = " + t.detailObjectDistance + " m, " +
                      "pixelError = " + t.heightmapPixelError);
        }

        UniversalRenderPipelineAsset urp =
            GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset
            ?? QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel())
               as UniversalRenderPipelineAsset;

        if (urp != null)
        {
            SerializedObject so = new SerializedObject(urp);
            Debug.Log("URP '" + urp.name + "': " +
                      "shadowDistance = " + Fl(so, "m_ShadowDistance") + " m, " +
                      "cascade = " + In(so, "m_ShadowCascadeCount") + ", " +
                      "atlas = " + In(so, "m_MainLightShadowmapResolution") + ", " +
                      "softShadow = " + In(so, "m_SoftShadowQuality"));
        }

        Debug.Log("Quality '" + QualitySettings.names[QualitySettings.GetQualityLevel()] + "': " +
                  "lodBias = " + QualitySettings.lodBias + ", " +
                  "shadowDistance = " + QualitySettings.shadowDistance + ", " +
                  "aniso = " + QualitySettings.anisotropicFiltering);

        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional || l.shadows == LightShadows.None) continue;

            float yukseklik = Mathf.Clamp(-l.transform.forward.y, -1f, 1f);
            float aci = Mathf.Asin(yukseklik) * Mathf.Rad2Deg;
            string golge = aci > 0.5f
                ? (25f / Mathf.Tan(aci * Mathf.Deg2Rad)).ToString("0") + " m"
                : "-";

            Debug.Log("Isik '" + l.name + "': " +
                      "yukseklik = " + aci.ToString("0.0") + " derece, " +
                      "temperature = " + l.colorTemperature + " K, " +
                      "intensity = " + l.intensity + ", " +
                      "25 m agacin golgesi = " + golge +
                      ", ShadowCasterCull(ViewFrustum) = " +
                      (l.useViewFrustumForShadowCasterCull ? "ACIK" : "kapali"));
        }

        Material sis = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/FoggyRoad/Looks/ReferencePhotoFogV2.mat");
        if (sis != null)
        {
            Debug.Log("Sis: start = " + Mf(sis, "_StartDistance") + " m, " +
                      "maxDistance = " + Mf(sis, "_MaxDistance") + " m, " +
                      "maxOpacity = " + Mf(sis, "_MaxOpacity") + ", " +
                      "heightFalloff = " + Mf(sis, "_HeightFalloff"));
        }

        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.cameraType != CameraType.Game || !cam.gameObject.activeInHierarchy) continue;
            Debug.Log("Kamera '" + cam.name + "': clearFlags = " + cam.clearFlags +
                      ", farClip = " + cam.farClipPlane +
                      " m, occlusionCulling = " + (cam.useOcclusionCulling ? "ACIK" : "kapali"));
        }

        SkyboxYaz();
        YolGolgesiYaz();
        AgacLodGolgesiYaz();
    }

    // ==================================================================
    //  Agac LOD golgeleri
    //
    //  Golge pop-in'inin sebebi agac prefabinda LOD1/LOD2'nin castShadows = 0
    //  olmasiydi (agac LOD atlayinca golgesi bir anda yok oluyordu).
    //  SpeedTree .st dosyasi yeniden import edilirse prefab yeniden uretilip
    //  bu ayar kaybolabilir - her kayitta kontrol edilir.
    // ==================================================================
    static void AgacLodGolgesiYaz()
    {
        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (t.terrainData == null) continue;

            foreach (TreePrototype p in t.terrainData.treePrototypes)
            {
                if (p.prefab == null) continue;

                LODGroup grup = p.prefab.GetComponent<LODGroup>();
                if (grup == null) continue;

                LOD[] lodlar = grup.GetLODs();
                string durum = "";
                bool eksik = false;

                for (int i = 0; i < lodlar.Length; i++)
                {
                    bool golge = false;
                    foreach (Renderer r in lodlar[i].renderers)
                        if (r != null && r.shadowCastingMode != ShadowCastingMode.Off)
                            golge = true;

                    durum += "LOD" + i + "=" + (golge ? "On" : "Off") + " ";

                    bool sonLod = i == lodlar.Length - 1;
                    bool billboard = sonLod && grup.lastLODBillboard;
                    // Billboard disinda golgesiz LOD varsa pop-in geri gelmis demektir
                    if (i > 0 && !golge && !billboard) eksik = true;
                }

                Debug.Log("Agac '" + p.prefab.name + "' golgeleri: " + durum);

                if (eksik)
                    Debug.LogWarning("  DIKKAT: golgesiz LOD var. Agac LOD atlayinca golgesi " +
                                     "bir anda kaybolur.\n" +
                                     "  Duzeltmek icin: Tools > Foggy Road > Agac LOD > " +
                                     "2 - AGAC GOLGELERINI DUZELT");
            }
        }
    }

    // ==================================================================
    //  Skybox: sahnenin Lighting ayarinda saklanir, sahne ile kaydedilir.
    // ==================================================================
    static void SkyboxYaz()
    {
        Material sky = RenderSettings.skybox;
        if (sky == null)
        {
            Debug.LogWarning("Skybox: ATANMAMIS. Lighting > Environment bos.");
            return;
        }

        string yol = AssetDatabase.GetAssetPath(sky);
        Debug.Log("Skybox: '" + sky.name + "' (" + yol + "), shader = " +
                  (sky.shader != null ? sky.shader.name : "?"));

        if (sky.shader != null && sky.shader.name == "Environment/Sunset Sky With Clouds")
        {
            Debug.Log("  Bulut: invert = " + Mf(sky, "_CloudInvert") +
                      ", kapsama = " + Mf(sky, "_Coverage") +
                      ", keskinlik = " + Mf(sky, "_Sharpness") +
                      ", girdap = " + Mf(sky, "_WarpStrength") +
                      ", olcek = " + Mf(sky, "_CloudScale") +
                      ", oktav = " + Mf(sky, "_Octaves") +
                      ", ruzgar hizi = " + Mf(sky, "_WindSpeed") +
                      ", foto detay = " + Mf(sky, "_PhotoDetail"));

            if (sky.HasProperty("_DebugMask") && sky.GetFloat("_DebugMask") > 0.5f)
                Debug.LogWarning("  DIKKAT: 'Maskeyi goster' ACIK. Gokyuzu siyah-beyaz " +
                                 "gorunur. Kapatmak icin materyalde _DebugMask = 0 yap.");
        }
    }

    // ==================================================================
    //  Yol gercek zamanli golge aliyor mu
    // ==================================================================
    static void YolGolgesiYaz()
    {
        Material yol = AssetDatabase.LoadAssetAtPath<Material>("Assets/yol 1/RoadWet 1.mat");
        if (yol == null) return;

        Debug.Log("Yol materyali '" + yol.name + "': shader = " +
                  (yol.shader != null ? yol.shader.name : "?") +
                  ", golge alma = " +
                  (yol.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF") ? "KAPALI (!)" : "acik"));
    }

    // ------------------------------------------------------------ yardimcilar
    static string Fl(SerializedObject so, string ad)
    {
        SerializedProperty p = so.FindProperty(ad);
        return p == null ? "?" : p.floatValue.ToString("0.##");
    }

    static string In(SerializedObject so, string ad)
    {
        SerializedProperty p = so.FindProperty(ad);
        return p == null ? "?" : p.intValue.ToString();
    }

    static string Mf(Material m, string prop)
    {
        return m.HasProperty(prop) ? m.GetFloat(prop).ToString("0.###") : "-";
    }
}
#endif
