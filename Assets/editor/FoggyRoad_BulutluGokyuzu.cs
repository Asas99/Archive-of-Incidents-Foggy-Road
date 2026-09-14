// Bulutlu gun batimi gokyuzunu kurar.
//
// Mevcut skybox_aksamustu.mat (Skybox/Procedural) gun batimi gradyani ve gunes
// diski veriyor ama bulut YOK. Elde iki equirect bulut fotografi var, ikisi de
// gri/gece tonunda - dogrudan skybox yapilirsa gun batimi sicakligi kayboluyor.
//
// SunsetCloudSky.shader ikisini birlestirir: fotografin LUMINANCE'i bulut
// SEKLINI verir, renk gun batimi paletinden gelir. Gunes yonu sahnenin
// Directional Light'indan okunur, hizalama derdi yok.
//
// skybox_aksamustu.mat'a DOKUNULMAZ - '3 - ESKI GOKYUZUNE DON' tek tikla
// eski haline dondurur.
//
// Menu: Tools -> Foggy Road/Gokyuzu/...
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FoggyRoad_BulutluGokyuzu
{
    const string ShaderAdi = "Environment/Sunset Sky With Clouds";
    const string YeniMatYolu = "Assets/skybox/skybox_aksamustu_bulutlu.mat";
    const string EskiMatYolu = "Assets/skybox/skybox_aksamustu.mat";

    // Iki bulut fotografi.
    //   [0] fde7a0e3 : yogun firtina / overcast - KOYU bolgeler bulut  -> invert 1
    //   [1] eabf86d2 : gece mavisi dramatik kumulus                    -> invert 1
    // Ikisinde de bulut kutleleri gokyuzu acikligindan KOYU oldugu icin maske
    // ters cevrilir. Yeni bir doku eklenirse ve bulutlar acik tonluysa
    // BulutInvert degeri 0 yapilmalidir.
    static readonly string[] BulutDokulari =
    {
        "Assets/skybox/fde7a0e3-364f-445b-9c45-08fb160bcbe4.png",
        "Assets/skybox/eabf86d2-3b59-4e50-bfa2-3e60a9bc6740.png"
    };

    static readonly float[] BulutInvert = { 1f, 1f };

    // ==================================================================
    //  1 - KUR
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Gokyuzu/1 - Bulutlu gokyuzunu KUR", false, 0)]
    public static void Kur()
    {
        Shader sh = Shader.Find(ShaderAdi);
        if (sh == null)
        {
            Debug.LogError("[FoggyRoad] Shader bulunamadi: " + ShaderAdi +
                           "\nAssets/Environment/SunsetCloudSky.shader derlendi mi? Console'da hata var mi?");
            return;
        }

        Material m = AssetDatabase.LoadAssetAtPath<Material>(YeniMatYolu);
        bool yeni = m == null;

        if (yeni)
        {
            m = new Material(sh);
            m.name = Path.GetFileNameWithoutExtension(YeniMatYolu);
            AssetDatabase.CreateAsset(m, YeniMatYolu);
        }
        else if (m.shader != sh)
        {
            m.shader = sh;
        }

        // Bulut dokusu atanmamissa ilkini bagla
        if (m.GetTexture("_CloudTex") == null)
        {
            Texture2D dok = AssetDatabase.LoadAssetAtPath<Texture2D>(BulutDokulari[0]);
            if (dok == null)
            {
                Debug.LogWarning("[FoggyRoad] Bulut dokusu bulunamadi: " + BulutDokulari[0]);
            }
            else
            {
                m.SetTexture("_CloudTex", dok);
                m.SetFloat("_CloudInvert", BulutInvert[0]);
                DokuyuSkyboxaHazirla(BulutDokulari[0]);
            }
        }

        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();

        RenderSettings.skybox = m;

        // Ambient skybox'tan geliyor; kurulumda BIR KEZ guncelle.
        // Her kare cagirmak CPU sinirli sahnede agir bedel olur.
        DynamicGI.UpdateEnvironment();

        Selection.activeObject = m;
        EditorGUIUtility.PingObject(m);

        Debug.Log("[FoggyRoad] Bulutlu gokyuzu " + (yeni ? "olusturuldu" : "guncellendi") + ": " +
                  YeniMatYolu + "\n" +
                  "  Doku: " + DokuAdi(m) + "\n" +
                  "  Ayarlari Inspector'dan degistirebilirsin (materyal secili).\n" +
                  "  Begenmezsen: Tools > Foggy Road > Gokyuzu > 3 - ESKI GOKYUZUNE DON");
    }

    // ==================================================================
    //  2 - Doku degistir
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Gokyuzu/2 - Bulut dokusunu degistir", false, 1)]
    public static void DokuDegistir()
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(YeniMatYolu);
        if (m == null)
        {
            Debug.LogError("[FoggyRoad] Once '1 - Bulutlu gokyuzunu KUR' calistir.");
            return;
        }

        Texture mevcut = m.GetTexture("_CloudTex");
        string mevcutYol = mevcut != null ? AssetDatabase.GetAssetPath(mevcut) : "";

        int sonraki = 0;
        for (int i = 0; i < BulutDokulari.Length; i++)
        {
            if (BulutDokulari[i] == mevcutYol)
            {
                sonraki = (i + 1) % BulutDokulari.Length;
                break;
            }
        }

        Texture2D dok = AssetDatabase.LoadAssetAtPath<Texture2D>(BulutDokulari[sonraki]);
        if (dok == null)
        {
            Debug.LogError("[FoggyRoad] Doku bulunamadi: " + BulutDokulari[sonraki]);
            return;
        }

        Undo.RecordObject(m, "Bulut dokusu degistir");
        m.SetTexture("_CloudTex", dok);
        m.SetFloat("_CloudInvert", BulutInvert[sonraki]);
        DokuyuSkyboxaHazirla(BulutDokulari[sonraki]);

        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        DynamicGI.UpdateEnvironment();

        Debug.Log("[FoggyRoad] Bulut dokusu -> " + dok.name +
                  "\n  Tekrar basarsan digerine geri doner.");
    }

    // ==================================================================
    //  4 - Aksamustu paleti
    //
    //  Varsayilanlar fazla soluk ve duz kaliyordu. Buradaki degerler:
    //    - zenit KOYULASTIRILIR, ufuk ISITILIR -> kontrast, dram
    //    - bulut golgesi koyu, isiyan yuz HDR altin -> "yer yer yogun" hissi
    //    - kenar yumusakligi DUSURULUR -> bulutlar tek parca pus yerine
    //      ayri ayri kutleler halinde okunur
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Gokyuzu/4 - AKSAMUSTU renklerini uygula", false, 3)]
    public static void AksamustuPaleti()
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(YeniMatYolu);
        if (m == null)
        {
            Debug.LogError("[FoggyRoad] Once '1 - Bulutlu gokyuzunu KUR' calistir.");
            return;
        }

        Undo.RecordObject(m, "Aksamustu paleti");

        // --- bulut maskesi
        // Dokuda KOYU = yogun bulut oldugu icin maske ters cevrilir.
        // Dokunun luminance araligi dar (~0.25-0.60); Levels bunu 0-1'e yayar,
        // boylece kaplama ayari tum aralikta kademeli calisir.
        m.SetFloat("_CloudInvert", 1f);
        m.SetFloat("_InBlack", 0.34f);
        m.SetFloat("_InWhite", 0.82f);
        m.SetFloat("_MaskGamma", 1.0f);
        m.SetFloat("_CloudCoverage", 0.55f);
        m.SetFloat("_CloudSoftness", 0.18f);
        m.SetFloat("_DetailBlend", 0.65f);     // ikinci katman = dagilim
        m.SetFloat("_CloudScroll", 0.35f);     // ~4-5 dk'da gogu geciyor

        // --- zenit (kutup tekilligi kapatmasi) ve ufuk incelmesi
        // _PlaneCenter kritik: kaydirmasiz tepe UV'si (0,0)'a gider, orada
        // dokunun parlak sis denizi var -> tam yukarida bulut kaybolur.
        m.SetFloat("_PlaneCurvature", 0.30f);
        m.SetFloat("_PlaneScale", 0.55f);
        m.SetVector("_PlaneCenter", new Vector4(0.5f, 0.72f, 0f, 0f));
        m.SetFloat("_HorizonFade", 0.18f);

        // --- teshis kapali
        m.SetFloat("_DebugMask", 0f);

        // --- gokyuzu gradyani (zenit koyu mor-mavi, ufuk sicak)
        m.SetColor("_ZenithColor", new Color(0.16f, 0.18f, 0.30f, 1f));
        m.SetColor("_HorizonColor", new Color(0.62f, 0.45f, 0.38f, 1f));
        m.SetColor("_HorizonSunColor", new Color(1.25f, 0.55f, 0.22f, 1f));  // HDR
        m.SetFloat("_HorizonPower", 2.8f);
        m.SetColor("_GroundColor", new Color(0.08f, 0.07f, 0.07f, 1f));

        // --- bulut aydinlatmasi
        m.SetColor("_CloudLitColor", new Color(1.35f, 0.72f, 0.38f, 1f));    // HDR altin
        m.SetColor("_CloudShadowColor", new Color(0.20f, 0.17f, 0.24f, 1f));
        m.SetFloat("_CloudLitPower", 3.2f);    // lob biraz genis: gunes tarafi tumuyle olu kalmasin
        m.SetFloat("_CloudThickness", 2.0f);   // kalin cekirdek koyu -> bulut "hacimli" okunur
        m.SetFloat("_RimIntensity", 0.9f);     // kenar parlamasi = siluet belirginligi

        // --- gunes
        m.SetColor("_SunGlowColor", new Color(1.4f, 0.62f, 0.28f, 1f));
        m.SetFloat("_SunGlowPower", 64f);
        m.SetFloat("_SunGlowIntensity", 1.8f);
        m.SetFloat("_SunDiskSize", 0.008f);
        m.SetFloat("_SunDiskIntensity", 8f);

        m.SetFloat("_Exposure", 1.2f);

        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        DynamicGI.UpdateEnvironment();

        Selection.activeObject = m;

        Debug.Log("[FoggyRoad] Aksamustu paleti uygulandi.\n" +
                  "  Daha cok bulut            : Bulut kaplamasi 0.55 -> 0.70\n" +
                  "  Daha keskin kutle         : Bulut kenar yumusakligi 0.18 -> 0.10\n" +
                  "  Daha parcali / dagilmis   : Dagilim 0.65 -> 0.85\n" +
                  "  Tepede bulut az gelirse   : Tepe olcek 0.25 -> 0.18\n" +
                  "  Tepede doku tekrar ederse : Tepe olcek 0.25 -> 0.35\n" +
                  "  Bulut hic gorunmuyorsa    : Maske ters cevir 1 <-> 0 dene\n" +
                  "  Kontrast az/cok gelirse   : Seviye siyah 0.34 / beyaz 0.82\n" +
                  "  Isikta bulut secilmiyorsa : Kalinlik 2.0 -> 3.0, Kenar parlamasi 0.9 -> 1.6\n" +
                  "  NE OLDUGUNU GORMEK ICIN   : 'Maskeyi goster' 1 yap (siyah=gok, beyaz=bulut)\n" +
                  "  Daha sicak ufuk           : Ufuk - gunes tarafi rengini yukselt\n" +
                  "  Bulut daha hizli aksin    : Bulut kayma hizi 0.35 -> 0.8");
    }

    // ==================================================================
    //  3 - Geri don
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Gokyuzu/3 - ESKI GOKYUZUNE DON", false, 2)]
    public static void GeriDon()
    {
        Material eski = AssetDatabase.LoadAssetAtPath<Material>(EskiMatYolu);
        if (eski == null)
        {
            Debug.LogError("[FoggyRoad] Eski skybox bulunamadi: " + EskiMatYolu);
            return;
        }

        RenderSettings.skybox = eski;
        DynamicGI.UpdateEnvironment();

        Debug.Log("[FoggyRoad] Eski gokyuzune donuldu: " + EskiMatYolu +
                  "\n  Bulutlu materyal silinmedi, '1 - KUR' ile geri alinabilir.");
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================
    /// <summary>
    /// Equirect bir dokunun skybox'ta dikisiz gorunmesi icin wrap modu
    /// yatayda Repeat olmali; ayrica tam cozunurluk istiyoruz.
    /// </summary>
    static void DokuyuSkyboxaHazirla(string yol)
    {
        TextureImporter ti = AssetImporter.GetAtPath(yol) as TextureImporter;
        if (ti == null) return;

        bool degisti = false;

        if (ti.wrapMode != TextureWrapMode.Repeat)
        {
            ti.wrapMode = TextureWrapMode.Repeat;
            degisti = true;
        }
        if (ti.filterMode != FilterMode.Trilinear)
        {
            ti.filterMode = FilterMode.Trilinear;
            degisti = true;
        }
        if (ti.anisoLevel < 4)
        {
            ti.anisoLevel = 4;
            degisti = true;
        }
        if (!ti.mipmapEnabled)
        {
            ti.mipmapEnabled = true;
            degisti = true;
        }

        if (degisti)
        {
            ti.SaveAndReimport();
            Debug.Log("[FoggyRoad] Doku import ayarlari duzeltildi: " + Path.GetFileName(yol));
        }
    }

    static string DokuAdi(Material m)
    {
        Texture t = m.GetTexture("_CloudTex");
        return t != null ? t.name : "(atanmamis)";
    }
}
#endif
