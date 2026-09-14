// CIM YOGUNLUGU: yogun gorunum + dusuk maliyet
//
// TESPIT (TerrainData binary parse edilerek dogrulandi):
//   4 cim katmani (Grass_A/B/C/D) AYNI hucrelerde 255 degeriyle boyali,
//   ustune 3 fern katmani ~140 ile. Hucre ~0.5 m. Terrain InstanceCountMode'da.
//   InstanceCountMode'da harita degeri "hucre basina instance sayisi"dir.
//   Yogunluk hissi 'density' ayarindan degil bu bindirmeden geliyor.
//
//   ONEMLI: Unity bu degeri maxDetailScatterPerRes ile KIRPIYOR olabilir.
//   RAPOR bunu olcup soyler; kirpma varsa cim sanildigi kadar pahali degildir
//   ve darbogaz baska yerdedir. Bu yuzden UYGULA once RAPOR ister.
//
//   Muhtemel kokeni: ForestRoadVegetationPlacer.cs:3808-3817 haritalari 0-255
//   COVERAGE olceginde yaziyor, ama SetDetailScatterMode(CoverageMode) (satir
//   2265-2270) uygulanmamis. Harita coverage gibi yazilmis, instance sayisi
//   gibi okunuyor.
//
// YAKLASIM: detail haritalarini ORANSAL olarak yeniden olcekle.
//   - Scatter mode'a DOKUNULMAZ. Unity kaynagi: "Changing detail scatter mode to
//     Instance count will erase existing detail placements" -> tek yonlu kapi.
//   - detailPrototypes dizisine DOKUNULMAZ. Eleman cikarmak kalan katmanlarin
//     index'ini kaydirir, harita verisi yanlis katmana duser.
//   - Terrain component'in detailObjectDistance/Density alanina DOKUNULMAZ.
//     Orayi FoggyRoad_PopInTamDuzeltme.cs her preset'te yeniden yaziyor;
//     kalici duzeltme asset verisinde olmali.
//
// Olcekleme oransaldir (yeni = round(eski * k), eski>0 ise yeni>=1). Boylece
// placer'in yazdigi kenar yumusamasi korunur, cim alani kenarda seyrelerek biter.
//
// EN KRITIK KURAL: UYGULA her zaman YEDEKTEKI ORIJINAL haritadan hesaplar,
// mevcut haritadan degil. Aksi halde ikinci calistirma degerleri tekrar boler.
//
// Menu: Tools -> Foggy Road/Cim/...
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FoggyRoad_CimYogunluk
{
    const string YedekKlasor = "Assets/GeneratedTerrain";
    const string YedekOnEk = "_CimYedek_";

    // Rapor calistirilmadan UYGULA'ya izin verilmez.
    static bool raporCalisti;
    static string raporTerrainAdi;

    // ==================================================================
    //  Preset: katman index -> hucre basina hedef instance
    //
    //  Katman rolleri (TerrainData parse'indan):
    //    0 Grass_A   128 ucgen -> ana kutle (en hafif mesh)
    //    2 Grass_C  1002 ucgen -> siluet cesnisi
    //    8 Fern_A    360 ucgen -> orta katman hacmi
    //    1 Grass_B 1136 / 3 Grass_D 1268 / 9 Fern_B / 10 Fern_C -> kapatilir
    // ==================================================================
    class Preset
    {
        public string ad;
        public string aciklama;

        // ORANSAL mod (tercih edilen): TUM katmanlar ayni katsayiyla kucultulur.
        // Katman sayisi, mesh cesitliligi ve katmanlarin birbirine orani AYNEN korunur.
        // %100'un ustundeki kaplama zaten gorunmez, ama 7 farkli mesh'in cesitliligi
        // GORUNUR - bu yuzden katman kapatmak yanlis, oransal kucultmek dogru.
        public float oran;

        // Katman-bazli mod (eski): belirli katmanlari kapatir. Cesitliligi oldurur,
        // kullanilmiyor ama referans olarak duruyor.
        public Dictionary<int, int> hedef;
    }

    static Preset Getir(string ad)
    {
        switch (ad)
        {
            case "CokHafif":
                return new Preset
                {
                    ad = "COK HAFIF",
                    oran = 0.80f,
                    aciklama = "tum katmanlar x0.80 -> kaplama ~%454, gorunum neredeyse ayni"
                };

            case "Hafif":
                return new Preset
                {
                    ad = "HAFIF DOKUNUS",
                    oran = 0.50f,
                    aciklama = "tum katmanlar x0.50 -> kaplama ~%284, instance yariya iner"
                };

            case "Agresif":
                return new Preset
                {
                    ad = "AGRESIF",
                    oran = 0.22f,
                    aciklama = "tum katmanlar x0.22 -> kaplama ~%125, instance 4.5x azalir"
                };

            default:
                return new Preset
                {
                    ad = "DENGELI",
                    oran = 0.35f,
                    aciklama = "tum katmanlar x0.35 -> kaplama ~%199, instance 2.9x azalir"
                };
        }
    }

    // ==================================================================
    //  0 - RAPOR
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim/0 - RAPOR (hicbir sey degistirmez)", false, 0)]
    public static void Rapor()
    {
        Terrain t = TerrainBul();
        if (t == null) return;

        TerrainData td = t.terrainData;
        DetailPrototype[] protos = td.detailPrototypes;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("================ FOGGY ROAD CIM RAPORU ================");
        sb.AppendLine("Terrain     : " + t.name);
        sb.AppendLine("TerrainData : " + AssetDatabase.GetAssetPath(td));
        sb.AppendLine("scatterMode : " + td.detailScatterMode +
                      "   tavan (maxDetailScatterPerRes) = " + td.maxDetailScatterPerRes);
        sb.AppendLine("detailWidth x Height = " + td.detailWidth + " x " + td.detailHeight +
                      "   resolutionPerPatch = " + td.detailResolutionPerPatch);
        sb.AppendLine("Terrain component: detailObjectDistance = " + t.detailObjectDistance +
                      "   detailObjectDensity = " + t.detailObjectDensity);
        sb.AppendLine("Hucre boyu ~ " + (td.size.x / Mathf.Max(1, td.detailWidth)).ToString("0.00") + " m");
        sb.AppendLine();

        int kirpma = td.maxDetailScatterPerRes;
        int etkinToplam = 0;
        long toplamCoverage = 0;
        long toplamUcgenYuku = 0;
        bool agacUyarisi = false;

        sb.AppendLine("--- KATMANLAR ---");
        for (int i = 0; i < protos.Length; i++)
        {
            string ad = ProtoAdi(protos[i]);
            int ucgen = MeshUcgeni(protos[i]);

            int maks;
            int doluHucre;
            long toplam;
            HaritaOzeti(td, i, out maks, out doluHucre, out toplam);

            toplamCoverage += toplam;
            toplamUcgenYuku += toplam * ucgen;

            bool agacMi = ad.StartsWith("Pine") || ad.StartsWith("Conifer") || ad.StartsWith("Spruce");
            if (agacMi && maks > 0) agacUyarisi = true;

            int etkin = Mathf.Min(maks, kirpma);
            etkinToplam += etkin;

            string etiket = doluHucre == 0 ? "  (BOS - coverage yok)" : "";
            if (maks > kirpma) etiket += "  (tavana kirpiliyor)";
            if (agacMi) etiket += "  <<< AGAC PROTOTYPE, DETAY LISTESINDE";

            sb.AppendLine("  [" + i.ToString("00") + "] " + ad.PadRight(14) +
                          " mesh=" + ucgen.ToString().PadLeft(5) + "tri" +
                          "  doluHucre=" + doluHucre.ToString().PadLeft(6) +
                          "  harita=" + maks.ToString().PadLeft(3) +
                          "  etkin=" + etkin.ToString().PadLeft(3) +
                          etiket);
        }

        bool coverage = td.detailScatterMode == DetailScatterMode.CoverageMode;

        sb.AppendLine();
        if (coverage)
        {
            // CoverageMode: harita degeri "kaplanacak alan"dir, 255 = %100.
            // Bir yuzey %100'den fazla kaplanamaz; fazlasi saf ust uste binmedir.
            float yuzde = etkinToplam * 100f / 255f;
            sb.AppendLine("--- TOPLAM KAPLAMA (CoverageMode) ---");
            sb.AppendLine("  Katmanlarin toplami : " + etkinToplam + " / 255");
            sb.AppendLine("  Yani istenen kaplama: %" + yuzde.ToString("0"));
            sb.AppendLine("  (bir yuzey en fazla %100 kaplanabilir - fazlasi ust uste binme)");
        }
        else
        {
            float densityli = etkinToplam * t.detailObjectDensity;
            sb.AppendLine("--- HUCRE BASINA INSTANCE (InstanceCountMode) ---");
            sb.AppendLine("  Katmanlarin etkin toplami : " + etkinToplam);
            sb.AppendLine("  detailObjectDensity (" + t.detailObjectDensity.ToString("0.00") +
                          ") sonrasi : " + densityli.ToString("0.0") + " instance/hucre");
        }

        sb.AppendLine();
        sb.AppendLine("--- TOPLAM (tum terrain) ---");
        sb.AppendLine("  Coverage toplami       : " + toplamCoverage.ToString("N0"));
        sb.AppendLine("  Coverage x mesh ucgeni : " + toplamUcgenYuku.ToString("N0"));
        sb.AppendLine();
        sb.AppendLine("=== KABUL KRITERI ===");
        if (coverage)
        {
            sb.AppendLine("  Toplam kaplama yuzdesine bak:");
            sb.AppendLine("    > %150 -> gereksiz ust uste binme var, UYGULA ile devam et.");
            sb.AppendLine("    < %110 -> kaplama zaten makul, cim suclu degil.");
            sb.AppendLine("              DUR. Darbogaz agaclarda/golgelerde aranmali.");
        }
        else
        {
            sb.AppendLine("  'instance/hucre' sayisina bak:");
            sb.AppendLine("    > 60  -> tespit dogru, UYGULA ile devam et.");
            sb.AppendLine("    < 25  -> cim suclu degil, DUR.");
        }

        if (agacUyarisi)
        {
            sb.AppendLine();
            sb.AppendLine("!!! UYARI: Detay listesinde AGAC prototype'i var ve coverage'i > 0.");
            sb.AppendLine("    Terrain > Paint Details fircasiyla surulurse sahne coker.");
        }

        sb.AppendLine();
        YedekleriListele(sb);

        Debug.Log(sb.ToString());

        raporCalisti = true;
        raporTerrainAdi = t.name;
    }

    // ==================================================================
    //  1 - YEDEK AL
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim/1 - YEDEK AL", false, 1)]
    public static void YedekAl()
    {
        Terrain t = TerrainBul();
        if (t == null) return;

        string kaynak = AssetDatabase.GetAssetPath(t.terrainData);
        if (string.IsNullOrEmpty(kaynak))
        {
            Debug.LogError("[FoggyRoad] TerrainData bir asset degil, yedeklenemez.");
            return;
        }

        string hedef = AssetDatabase.GenerateUniqueAssetPath(
            YedekKlasor + "/" + YedekOnEk + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".asset");

        // File.Copy DEGIL: Unity .meta uretmez, GUID cakisir, asset veritabani bozulur.
        if (!AssetDatabase.CopyAsset(kaynak, hedef))
        {
            Debug.LogError("[FoggyRoad] Yedek alinamadi: " + hedef);
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FoggyRoad] Yedek alindi: " + hedef +
                  "\nGeri yuklemek icin: Tools > Foggy Road > Cim > 4 - GERI YUKLE");
    }

    // ==================================================================
    //  2 - UYGULA
    // ==================================================================
    // Hepsi ORANSAL: katman sayisi ve mesh cesitliligi AYNEN korunur,
    // sadece hepsi ayni katsayiyla kucultulur.
    [MenuItem("Tools/Foggy Road/Cim/2 - UYGULA - Cok hafif (x0.80) (onerilen)", false, 19)]
    public static void UygulaCokHafif() { Uygula("CokHafif"); }

    [MenuItem("Tools/Foggy Road/Cim/2 - UYGULA - Hafif dokunus (x0.50)", false, 20)]
    public static void UygulaHafif() { Uygula("Hafif"); }

    [MenuItem("Tools/Foggy Road/Cim/2 - UYGULA - Dengeli (x0.35)", false, 21)]
    public static void UygulaDengeli() { Uygula("Dengeli"); }

    [MenuItem("Tools/Foggy Road/Cim/2 - UYGULA - Agresif (x0.22)", false, 22)]
    public static void UygulaAgresif() { Uygula("Agresif"); }

    static void Uygula(string presetAdi)
    {
        Terrain t = TerrainBul();
        if (t == null) return;

        if (!raporCalisti || raporTerrainAdi != t.name)
        {
            EditorUtility.DisplayDialog(
                "Once RAPOR calistir",
                "Bu arac, hicbir sey degistirmeden once olcum yapmani ister.\n\n" +
                "Tools > Foggy Road > Cim > 0 - RAPOR\n\n" +
                "Console'daki KABUL KRITERI satirini oku, sonra buraya don.",
                "Tamam");
            return;
        }

        string yedekYol = EnYeniYedek();
        if (string.IsNullOrEmpty(yedekYol))
        {
            bool al = EditorUtility.DisplayDialog(
                "Yedek yok",
                "Bu islem detail haritalarini degistirir; kapatilan katmanlar geri gelmez.\n\n" +
                "Once yedek alinmali. Simdi alayim mi?",
                "Yedek al ve devam et", "Vazgec");
            if (!al) return;

            YedekAl();
            yedekYol = EnYeniYedek();
            if (string.IsNullOrEmpty(yedekYol))
            {
                Debug.LogError("[FoggyRoad] Yedek alinamadi, islem iptal.");
                return;
            }
        }

        // KRITIK: olcekleme her zaman YEDEKTEKI ORIJINAL haritadan hesaplanir.
        TerrainData kaynakTd = AssetDatabase.LoadAssetAtPath<TerrainData>(yedekYol);
        if (kaynakTd == null)
        {
            Debug.LogError("[FoggyRoad] Yedek TerrainData okunamadi: " + yedekYol);
            return;
        }

        TerrainData td = t.terrainData;
        if (kaynakTd.detailWidth != td.detailWidth || kaynakTd.detailHeight != td.detailHeight)
        {
            Debug.LogError("[FoggyRoad] Yedek ile canli terrain'in detail cozunurlugu farkli. Iptal.");
            return;
        }

        Preset p = Getir(presetAdi);
        DetailPrototype[] protos = td.detailPrototypes;

        int[] yeniMaks = new int[protos.Length];
        for (int i = 0; i < protos.Length; i++)
        {
            int eski = HaritaMaks(kaynakTd, i);

            if (p.oran > 0f)
            {
                // Oransal mod: bos katman bos kalir, dolu katman ayni katsayiyla
                // kucultulur ama ASLA sifirlanmaz - cesitlilik korunur.
                yeniMaks[i] = eski > 0 ? Mathf.Max(1, Mathf.RoundToInt(eski * p.oran)) : 0;
            }
            else
            {
                int hedef;
                yeniMaks[i] = p.hedef != null && p.hedef.TryGetValue(i, out hedef) ? hedef : 0;
            }
        }

        // Onay metni: kullanici ne olacagini GORMEDEN evet diyemesin
        StringBuilder onay = new StringBuilder();
        onay.AppendLine(p.ad + " preset'i uygulanacak (" + p.aciklama + ").");
        onay.AppendLine();
        onay.AppendLine("Kaynak (degismeyen orijinal): " + Path.GetFileName(yedekYol));
        onay.AppendLine();

        for (int i = 0; i < protos.Length; i++)
        {
            int eskiMaks = HaritaMaks(kaynakTd, i);
            if (eskiMaks == 0 && yeniMaks[i] == 0) continue;

            onay.AppendLine("  [" + i.ToString("00") + "] " + ProtoAdi(protos[i]).PadRight(14) +
                            "  max/hucre  " + eskiMaks + " -> " + yeniMaks[i] +
                            (yeniMaks[i] == 0 ? "   (KAPATILIR)" : ""));
        }

        if (td.detailScatterMode == DetailScatterMode.CoverageMode)
        {
            int eskiT = 0;
            int yeniT = 0;
            for (int i = 0; i < protos.Length; i++)
            {
                eskiT += HaritaMaks(kaynakTd, i);
                yeniT += yeniMaks[i];
            }
            onay.AppendLine();
            onay.AppendLine("Toplam kaplama: %" + (eskiT * 100f / 255f).ToString("0") +
                            "  ->  %" + (yeniT * 100f / 255f).ToString("0"));
        }

        onay.AppendLine();
        onay.AppendLine("Devam edilsin mi?");

        if (!EditorUtility.DisplayDialog("Cim yogunlugu: " + p.ad, onay.ToString(), "Uygula", "Vazgec"))
            return;

        int w = td.detailWidth;
        int h = td.detailHeight;
        long oncekiToplam = 0;
        long sonrakiToplam = 0;

        try
        {
            for (int i = 0; i < protos.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Cim yogunlugu",
                                                 "Katman " + i + " / " + protos.Length,
                                                 (float)i / protos.Length);

                int[,] kaynak = kaynakTd.GetDetailLayer(0, 0, w, h, i);

                int eskiMaks = 0;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (kaynak[y, x] > eskiMaks) eskiMaks = kaynak[y, x];
                    }
                }

                int[,] yeni = new int[h, w];

                if (yeniMaks[i] > 0 && eskiMaks > 0)
                {
                    float k = yeniMaks[i] / (float)eskiMaks;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int v = kaynak[y, x];
                            oncekiToplam += v;
                            if (v <= 0) continue;

                            // eski>0 ise yeni>=1: kenar yumusamasi kaybolmasin
                            int nv = Mathf.Max(1, Mathf.RoundToInt(v * k));
                            yeni[y, x] = nv;
                            sonrakiToplam += nv;
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            oncekiToplam += kaynak[y, x];
                        }
                    }
                }

                td.SetDetailLayer(0, 0, i, yeni);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        float oran = sonrakiToplam > 0 ? oncekiToplam / (float)sonrakiToplam : 0f;
        Debug.Log("[FoggyRoad] Cim yogunlugu " + p.ad + " uygulandi.\n" +
                  "  Kaynak yedek     : " + yedekYol + "\n" +
                  "  Coverage toplami : " + oncekiToplam.ToString("N0") + "  ->  " +
                  sonrakiToplam.ToString("N0") + "   (" + oran.ToString("0.0") + "x azalma)\n" +
                  "  Simdi '0 - RAPOR'u tekrar calistirip karsilastir, sonra Play'de olc.");
    }

    // ==================================================================
    //  5 - Detail golgeleri
    //
    //  Olcum: Tris sayaci GOLGE PASS'ini de sayar. Iki farkli bakis acisinda
    //    2986 caster -> 7.4M tris
    //    5057 caster -> 14.0M tris
    //  yani ucgen sayisi caster sayisiyla neredeyse dogrusal hareket ediyor.
    //  Cim katmanlari zaten castShadows=0 ama Fern_A/B/C prefab'larinda 1 -
    //  43.097 hucre dolusu egrelti golge dokuyor. Sisli ormanda yer seviyesindeki
    //  egrelti golgesi gorunmez ama golge pass'inde tam bedeli odenir.
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim/5 - Detail golgelerini kapat (egrelti vb.)", false, 80)]
    public static void DetailGolgeleriniKapat() { DetailGolge(false); }

    [MenuItem("Tools/Foggy Road/Cim/5 - Detail golgelerini GERI AC", false, 81)]
    public static void DetailGolgeleriniAc() { DetailGolge(true); }

    static void DetailGolge(bool ac)
    {
        Terrain t = TerrainBul();
        if (t == null) return;

        DetailPrototype[] protos = t.terrainData.detailPrototypes;
        int degisen = 0;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== DETAIL GOLGELERI " + (ac ? "ACILIYOR" : "KAPATILIYOR") + " ===");

        ShadowCastingMode hedef = ac
            ? ShadowCastingMode.On
            : ShadowCastingMode.Off;

        for (int i = 0; i < protos.Length; i++)
        {
            GameObject pf = protos[i].prototype;
            if (pf == null) continue;

            // Coverage'i 0 olan katmani degistirmeye gerek yok
            if (HaritaMaks(t.terrainData, i) <= 0) continue;

            Renderer[] rs = pf.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rs.Length; r++)
            {
                if (rs[r].shadowCastingMode == hedef) continue;

                Undo.RecordObject(rs[r], "Detail golge");
                rs[r].shadowCastingMode = hedef;
                EditorUtility.SetDirty(rs[r]);
                degisen++;

                sb.AppendLine("  " + pf.name + " / " + rs[r].name +
                              "  castShadows -> " + hedef);
            }
        }

        AssetDatabase.SaveAssets();

        if (degisen == 0) sb.AppendLine("  (degisecek bir sey yoktu)");
        sb.AppendLine();
        sb.AppendLine(degisen + " renderer guncellendi. Play'de Shadow casters sayisina bak.");
        Debug.Log(sb.ToString());
    }

    // ==================================================================
    //  3 - ONIZLEME: CoverageMode klonda
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim/3 - ONIZLEME - CoverageMode klonda dene", false, 40)]
    public static void CoverageOnizleme()
    {
        Terrain t = TerrainBul();
        if (t == null) return;

        TerrainData td = t.terrainData;
        string kaynak = AssetDatabase.GetAssetPath(td);
        string klon = AssetDatabase.GenerateUniqueAssetPath(YedekKlasor + "/_CimKlonGecici.asset");

        if (!AssetDatabase.CopyAsset(kaynak, klon))
        {
            Debug.LogError("[FoggyRoad] Klon olusturulamadi.");
            return;
        }

        try
        {
            TerrainData kt = AssetDatabase.LoadAssetAtPath<TerrainData>(klon);
            if (kt == null)
            {
                Debug.LogError("[FoggyRoad] Klon okunamadi.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== CoverageMode ONIZLEME (klon uzerinde) ===");
            sb.AppendLine("Canli terrain'e DOKUNULMADI.");
            sb.AppendLine();
            sb.AppendLine("  Simdiki mod : " + td.detailScatterMode +
                          "   tavan = " + td.maxDetailScatterPerRes);

            kt.SetDetailScatterMode(DetailScatterMode.CoverageMode);

            sb.AppendLine("  Klon (yeni) : " + kt.detailScatterMode +
                          "   tavan = " + kt.maxDetailScatterPerRes);
            sb.AppendLine();
            sb.AppendLine("CoverageMode'da harita degeri 'instance sayisi' degil,");
            sb.AppendLine("'kaplanacak alan' olarak okunur (255 = tam kaplama).");
            sb.AppendLine("Instance sayisini Unity prototype genisliginden turetir;");
            sb.AppendLine("prototype genis oldugu icin ayni gorunum cok daha az");
            sb.AppendLine("instance ile dolar.");
            sb.AppendLine();
            sb.AppendLine("NOT: CoverageMode'a gecis TEK YONLUDUR.");
            sb.AppendLine("Instance Count'a donus mevcut cim yerlesimini SILER.");
            sb.AppendLine("Bu yuzden canli terrain'e uygulanmadi.");

            Debug.Log(sb.ToString());
        }
        finally
        {
            AssetDatabase.DeleteAsset(klon);
            AssetDatabase.Refresh();
        }
    }

    // ==================================================================
    //  4 - GERI YUKLE
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim/4 - GERI YUKLE (yedekten)", false, 60)]
    public static void GeriYukle()
    {
        Terrain t = TerrainBul();
        if (t == null) return;

        string secilen = EditorUtility.OpenFilePanel("Cim yedegini sec",
                                                     Application.dataPath + "/GeneratedTerrain",
                                                     "asset");
        if (string.IsNullOrEmpty(secilen)) return;

        if (!secilen.StartsWith(Application.dataPath))
        {
            Debug.LogError("[FoggyRoad] Yedek proje icinde olmali.");
            return;
        }

        string rel = "Assets" + secilen.Substring(Application.dataPath.Length).Replace('\\', '/');
        TerrainData kaynakTd = AssetDatabase.LoadAssetAtPath<TerrainData>(rel);
        if (kaynakTd == null)
        {
            Debug.LogError("[FoggyRoad] Secilen dosya TerrainData degil: " + rel);
            return;
        }

        TerrainData td = t.terrainData;
        if (kaynakTd == td)
        {
            Debug.LogError("[FoggyRoad] Canli terrain'in kendisi secildi. Baska bir yedek sec.");
            return;
        }
        if (kaynakTd.detailWidth != td.detailWidth || kaynakTd.detailHeight != td.detailHeight)
        {
            Debug.LogError("[FoggyRoad] Cozunurlukler uyusmuyor, geri yukleme guvenli degil.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Geri yukle",
                "Su anki cim yerlesimi silinip yedektekiyle degistirilecek:\n\n" +
                Path.GetFileName(rel) + "\n\nDevam edilsin mi?",
                "Geri yukle", "Vazgec"))
            return;

        // Dosyayi KOPYALAMIYORUZ: canli asset'in GUID'i degisirse sahnedeki
        // Terrain referansi kopar. Sadece veriyi tasiyoruz.
        int w = td.detailWidth;
        int h = td.detailHeight;
        int n = Mathf.Min(kaynakTd.detailPrototypes.Length, td.detailPrototypes.Length);

        try
        {
            for (int i = 0; i < n; i++)
            {
                EditorUtility.DisplayProgressBar("Geri yukleniyor", "Katman " + i + " / " + n,
                                                 (float)i / n);
                td.SetDetailLayer(0, 0, i, kaynakTd.GetDetailLayer(0, 0, w, h, i));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();
        Debug.Log("[FoggyRoad] Cim yerlesimi geri yuklendi: " + rel);
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================
    static Terrain TerrainBul()
    {
        Terrain[] hepsi = UnityEngine.Object.FindObjectsByType<Terrain>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < hepsi.Length; i++)
        {
            if (hepsi[i].terrainData != null) return hepsi[i];
        }

        Debug.LogError("[FoggyRoad] Sahnede TerrainData'si olan bir Terrain bulunamadi.");
        return null;
    }

    static string ProtoAdi(DetailPrototype p)
    {
        if (p.prototype != null) return p.prototype.name;
        if (p.prototypeTexture != null) return p.prototypeTexture.name;
        return "<bos>";
    }

    static int MeshUcgeni(DetailPrototype p)
    {
        if (p.prototype == null) return 0;
        MeshFilter mf = p.prototype.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return 0;
        return mf.sharedMesh.triangles.Length / 3;
    }

    static void HaritaOzeti(TerrainData td, int layer, out int maks, out int doluHucre, out long toplam)
    {
        maks = 0;
        doluHucre = 0;
        toplam = 0;

        int[,] m = td.GetDetailLayer(0, 0, td.detailWidth, td.detailHeight, layer);
        for (int y = 0; y < td.detailHeight; y++)
        {
            for (int x = 0; x < td.detailWidth; x++)
            {
                int v = m[y, x];
                if (v <= 0) continue;
                toplam += v;
                doluHucre++;
                if (v > maks) maks = v;
            }
        }
    }

    static int HaritaMaks(TerrainData td, int layer)
    {
        int maks;
        int dolu;
        long toplam;
        HaritaOzeti(td, layer, out maks, out dolu, out toplam);
        return maks;
    }

    static string EnYeniYedek()
    {
        if (!AssetDatabase.IsValidFolder(YedekKlasor)) return null;

        string[] guids = AssetDatabase.FindAssets("t:TerrainData", new[] { YedekKlasor });
        string enYeni = null;
        DateTime enYeniZaman = DateTime.MinValue;

        for (int i = 0; i < guids.Length; i++)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!Path.GetFileName(yol).StartsWith(YedekOnEk)) continue;

            DateTime z = File.GetLastWriteTime(yol);
            if (z > enYeniZaman)
            {
                enYeniZaman = z;
                enYeni = yol;
            }
        }
        return enYeni;
    }

    static void YedekleriListele(StringBuilder sb)
    {
        sb.AppendLine("--- MEVCUT CIM YEDEKLERI ---");

        string son = EnYeniYedek();
        if (string.IsNullOrEmpty(son))
        {
            sb.AppendLine("  (yok - UYGULA once yedek isteyecek)");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:TerrainData", new[] { YedekKlasor });
        for (int i = 0; i < guids.Length; i++)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!Path.GetFileName(yol).StartsWith(YedekOnEk)) continue;
            sb.AppendLine("  " + yol + (yol == son ? "   <<< en yeni, UYGULA bunu kaynak alir" : ""));
        }
    }
}
#endif
