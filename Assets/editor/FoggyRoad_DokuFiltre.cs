// Yol bulanikligi + bariyer parlamasi duzeltmesi.
//
// SORUN 1 - Yol ileriye dogru bulanik (ozellikle sari orta serit):
//   road_albedo_4k.png'nin import ayarinda filterMode = Point (0) ve aniso = 1.
//   Point filtreleme anizotropik filtrelemeyi DONANIM seviyesinde iptal eder;
//   QualitySettings'teki "anisotropicTextures: ForceEnable" bu dokuyu kurtaramaz,
//   cunku aniso lineer filtreleme ister.
//   Ustune materyalin _BaseMap tiling'i (1, -15.63). Siyirma acisiyla yola
//   bakildiginda V gradyani baskin olur ve mip seviyesi coker. Anizotropik
//   filtreleme tam olarak bu durum icindir.
//
//   NOT: _BaseMap tiling'ine DOKUNULMAZ. WetAsphalt_URP_PRO_FIXED.shader:292
//   UV'yi bir kez TRANSFORM_TEX(IN.uv0, _BaseMap) ile donusturup ayni uv'yi tum
//   haritalarda kullaniyor (321, 358, 368, 390). Haritalar hizasiz degil;
//   tiling'i dusurmek asfalt dokusunu tum yola yayar ve yolu bozar.
//
//   NOT 2: POM sebep degil. Shader'daki ParallaxOcclusionUV() (satir 204) tanimli
//   ama hicbir yerde cagrilmiyor (satir 313-315'teki yoruma bakiniz). Inspector'daki
//   POM slider'lari olu property.
//
// SORUN 2 - Bariyerler belirli acilardan hep ayni yerlerde parliyor:
//   Sahnedeki bariyerlerin TAMAMI GRPRO_Bolts_Unity.mat kullaniyor:
//   _Metallic 0.4, _EnvironmentReflections 1. Smoothness 0 olsa bile metalik
//   yuzey ortami yansitir ve siyirma acisinda Fresnel ile parlar.
//   Kullanicinin duzenledigi GRPRO_Metal_Unity.mat (0.06 / 0.02) hicbir objede
//   kullanilmiyor - yanlis materyal duzenlenmis. Bu yuzden bu arac materyalleri
//   DOSYA YOLUNDAN degil, sahnedeki renderer'larin gercekten kullandigi
//   sharedMaterials uzerinden bulur.
//
// Kalip referansi: Assets/Guardrail_Unity_Export/Editor/GuardrailAutoSetup.cs:115-127
//
// Menu: Tools -> Foggy Road/Yol/...
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FoggyRoad_DokuFiltre
{
    const int HedefAniso = 8;

    // Yol materyalini tanimak icin shader adinda aranan parcalar
    static readonly string[] YolShaderIpuclari = { "asphalt", "wetasphalt", "road" };

    // Yol materyalinde taranacak doku property'leri
    static readonly string[] DokuPropertyleri =
    {
        "_BaseMap", "_MainTex", "_NormalMap", "_BumpMap", "_RoughnessMap",
        "_AOMap", "_OcclusionMap", "_HeightMap", "_ParallaxMap",
        "_DetailNormalMap", "_DetailAlbedoMap", "_MetallicGlossMap"
    };

    static readonly string[] BariyerOnEkleri = { "GRPRO_", "Rail_", "Post_", "Edge_" };

    // ==================================================================
    //  1 - Yol dokulari
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Yol/1 - Yol Dokularini Duzelt (bulaniklik)")]
    public static void DokulariDuzelt()
    {
        Debug.Log("===== [FoggyRoad] YOL DOKU FILTRELEMESI =====");

        List<Material> yolMateryalleri = YolMateryalleriniBul();
        if (yolMateryalleri.Count == 0)
        {
            Debug.LogWarning("[FoggyRoad] Sahnede yol materyali bulunamadi. " +
                             "Shader adinda 'asphalt' veya 'road' gecen bir materyal araniyor.");
            return;
        }

        HashSet<string> islenen = new HashSet<string>();
        int degisen = 0;

        foreach (Material m in yolMateryalleri)
        {
            Debug.Log("[FoggyRoad] Yol materyali: '" + m.name + "'  (shader: " +
                      (m.shader != null ? m.shader.name : "yok") + ")");

            for (int i = 0; i < DokuPropertyleri.Length; i++)
            {
                string prop = DokuPropertyleri[i];
                if (!m.HasProperty(prop)) continue;

                Texture tex = m.GetTexture(prop);
                if (tex == null) continue;

                string yol = AssetDatabase.GetAssetPath(tex);
                if (string.IsNullOrEmpty(yol) || islenen.Contains(yol)) continue;
                islenen.Add(yol);

                if (DokuyuDuzelt(yol, prop)) degisen++;
            }
        }

        AssetDatabase.Refresh();

        Debug.Log("[FoggyRoad] Bitti. " + islenen.Count + " doku incelendi, " +
                  degisen + " tanesi duzeltildi (filterMode = Trilinear, aniso = " +
                  HedefAniso + ").");

        if (degisen == 0)
        {
            Debug.Log("[FoggyRoad] Hicbir doku degismedi - ayarlar zaten dogruydu. " +
                      "Bulaniklik devam ediyorsa sebep filtreleme degil.");
        }
    }

    /// <summary>
    /// wrapMode'a DOKUNULMAZ: yol albedo'su Mirror ve materyalde negatif tiling
    /// (-15.63) var; Repeat'e cevirmek dokuyu ters cevirir.
    /// maxTextureSize'a da dokunulmaz - kullanici FPS ile bogusuyor ve asil
    /// kazanc aniso'dan geliyor.
    /// </summary>
    static bool DokuyuDuzelt(string yol, string prop)
    {
        TextureImporter ti = AssetImporter.GetAtPath(yol) as TextureImporter;
        if (ti == null)
        {
            Debug.Log("[FoggyRoad]   " + prop + ": '" + yol + "' TextureImporter degil, atlandi.");
            return false;
        }

        FilterMode eskiFilter = ti.filterMode;
        int eskiAniso = ti.anisoLevel;
        bool degisti = false;

        if (ti.filterMode != FilterMode.Trilinear)
        {
            ti.filterMode = FilterMode.Trilinear;
            degisti = true;
        }
        if (ti.anisoLevel < HedefAniso)
        {
            ti.anisoLevel = HedefAniso;
            degisti = true;
        }
        // Mipmap kapaliysa aniso'nun anlami kalmaz
        if (!ti.mipmapEnabled)
        {
            ti.mipmapEnabled = true;
            degisti = true;
        }

        if (!degisti)
        {
            Debug.Log("[FoggyRoad]   " + prop + ": " + System.IO.Path.GetFileName(yol) +
                      " zaten dogru (filter=" + eskiFilter + " aniso=" + eskiAniso + ")");
            return false;
        }

        ti.SaveAndReimport();

        Debug.Log("[FoggyRoad]   " + prop + ": " + System.IO.Path.GetFileName(yol) +
                  "  filter " + eskiFilter + " -> Trilinear,  aniso " + eskiAniso +
                  " -> " + HedefAniso);
        return true;
    }

    static List<Material> YolMateryalleriniBul()
    {
        List<Material> sonuc = new List<Material>();

        foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null || m.shader == null || sonuc.Contains(m)) continue;

                string sh = m.shader.name.ToLowerInvariant();
                for (int k = 0; k < YolShaderIpuclari.Length; k++)
                {
                    if (sh.Contains(YolShaderIpuclari[k])) { sonuc.Add(m); break; }
                }
            }
        }
        return sonuc;
    }

    // ==================================================================
    //  2 - Bariyer parlamasi
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Yol/2 - Bariyer Parlamasini Duzelt")]
    public static void BariyerParlamasiniDuzelt()
    {
        Debug.Log("===== [FoggyRoad] BARIYER PARLAMASI =====");

        Dictionary<Material, int> kullanim = BariyerMateryalleri(false);
        if (kullanim.Count == 0)
        {
            Debug.LogWarning("[FoggyRoad] Aktif bariyer bulunamadi " +
                             "(GRPRO_ / Rail_ / Post_ / Edge_ ile baslayan objeler araniyor).");
            return;
        }

        foreach (KeyValuePair<Material, int> kv in kullanim)
        {
            Material m = kv.Key;
            Undo.RecordObject(m, "Bariyer parlamasi");

            float eskiMetallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : -1f;
            float eskiSmooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : -1f;

            FloatAta(m, "_Metallic", 0.08f);
            FloatAta(m, "_Smoothness", 0.05f);
            FloatAta(m, "_Glossiness", 0.05f);
            FloatAta(m, "_GlossMapScale", 0.05f);

            // Emission kapali kalmali. GRPRO_Reflector_Unity.mat HDR turuncu emission
            // degeri tasiyor; Inspector'da yanlislikla acilirsa bariyerler yanar.
            if (m.IsKeywordEnabled("_EMISSION"))
            {
                m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                Debug.Log("[FoggyRoad]   '" + m.name + "': _EMISSION kapatildi.");
            }

            EditorUtility.SetDirty(m);

            Debug.Log("[FoggyRoad] '" + m.name + "' (" + kv.Value + " renderer kullaniyor): " +
                      "Metallic " + eskiMetallic.ToString("0.00") + " -> 0.08, " +
                      "Smoothness " + eskiSmooth.ToString("0.00") + " -> 0.05");
        }

        AssetDatabase.SaveAssets();
        ProbeKontrol();

        Debug.Log("[FoggyRoad] Bitti. " + kullanim.Count +
                  " bariyer materyali duzeltildi. Ctrl+Z ile geri alinabilir.");
    }

    // ==================================================================
    //  3 - Rapor
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Yol/3 - Rapor (hangi materyal nerede kullaniliyor)")]
    public static void Rapor()
    {
        Debug.Log("===== [FoggyRoad] MATERYAL KULLANIM RAPORU =====");

        Dictionary<Material, int> aktif = BariyerMateryalleri(false);
        Dictionary<Material, int> pasifDahil = BariyerMateryalleri(true);

        Debug.Log("--- Bariyerlerin GERCEKTEN kullandigi materyaller (aktif objeler) ---");
        if (aktif.Count == 0) Debug.Log("   (yok)");
        foreach (KeyValuePair<Material, int> kv in aktif)
        {
            Debug.Log("   " + kv.Key.name + "  ->  " + kv.Value + " renderer\n" +
                      "      " + AssetDatabase.GetAssetPath(kv.Key) + "\n" +
                      "      Metallic = " + Oku(kv.Key, "_Metallic") +
                      ", Smoothness = " + Oku(kv.Key, "_Smoothness") +
                      ", EnvReflections = " + Oku(kv.Key, "_EnvironmentReflections"));
        }

        Debug.Log("--- Sadece PASIF objelerde kullanilan materyaller (ekrani etkilemez) ---");
        bool pasifVar = false;
        foreach (KeyValuePair<Material, int> kv in pasifDahil)
        {
            if (aktif.ContainsKey(kv.Key)) continue;
            pasifVar = true;
            Debug.Log("   " + kv.Key.name + "  ->  " + kv.Value + " renderer (KAPALI objelerde)\n" +
                      "      " + AssetDatabase.GetAssetPath(kv.Key));
        }
        if (!pasifVar) Debug.Log("   (yok)");

        Debug.Log("--- Yol materyalleri ---");
        foreach (Material m in YolMateryalleriniBul())
        {
            Debug.Log("   " + m.name + "  (" + AssetDatabase.GetAssetPath(m) + ")");
            for (int i = 0; i < DokuPropertyleri.Length; i++)
            {
                string prop = DokuPropertyleri[i];
                if (!m.HasProperty(prop)) continue;
                Texture t = m.GetTexture(prop);
                if (t == null) continue;

                string yol = AssetDatabase.GetAssetPath(t);
                TextureImporter ti = AssetImporter.GetAtPath(yol) as TextureImporter;
                string bilgi = ti != null
                    ? "filter=" + ti.filterMode + " aniso=" + ti.anisoLevel +
                      " maxSize=" + ti.maxTextureSize + " mipmap=" + ti.mipmapEnabled
                    : "(importer yok)";
                Debug.Log("      " + prop + ": " + System.IO.Path.GetFileName(yol) + "  " + bilgi);
            }
        }

        ProbeKontrol();
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================
    static Dictionary<Material, int> BariyerMateryalleri(bool pasifleriDahilEt)
    {
        Dictionary<Material, int> sonuc = new Dictionary<Material, int>();

        FindObjectsInactive kapsam = pasifleriDahilEt
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;

        foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(
                     kapsam, FindObjectsSortMode.None))
        {
            if (!BariyerMi(r.name)) continue;
            if (!pasifleriDahilEt && !r.gameObject.activeInHierarchy) continue;

            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                int adet;
                sonuc.TryGetValue(m, out adet);
                sonuc[m] = adet + 1;
            }
        }
        return sonuc;
    }

    static bool BariyerMi(string ad)
    {
        if (string.IsNullOrEmpty(ad)) return false;
        for (int i = 0; i < BariyerOnEkleri.Length; i++)
            if (ad.StartsWith(BariyerOnEkleri[i])) return true;
        return false;
    }

    /// <summary>
    /// Baked modda ama bake edilmemis probe, renderer'lara bayat/varsayilan cubemap
    /// verir; metalik yuzeylerde beklenmedik parlama uretir.
    /// Bake otomatik tetiklenmiyor - uzun surebilir, kullanicinin karari olmali.
    /// </summary>
    static void ProbeKontrol()
    {
        foreach (ReflectionProbe p in Object.FindObjectsByType<ReflectionProbe>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (p.mode != UnityEngine.Rendering.ReflectionProbeMode.Baked) continue;
            if (p.bakedTexture != null) continue;

            Debug.LogWarning("[FoggyRoad] '" + p.name + "' Reflection Probe BAKED modda ama " +
                             "bake edilmis cubemap'i YOK.\n" +
                             "Metalik yuzeyler bayat/varsayilan yansima aliyor.\n" +
                             "Cozum: Window > Rendering > Lighting > Generate Lighting.");
        }
    }

    static string Oku(Material m, string prop)
    {
        if (!m.HasProperty(prop)) return "-";
        return m.GetFloat(prop).ToString("0.00");
    }

    static void FloatAta(Material m, string prop, float v)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, v);
    }
}
#endif
