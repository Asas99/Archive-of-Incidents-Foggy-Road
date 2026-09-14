// CIM DAGILIMI - bos alanlari doldur
//
// TESPIT (Cim > 0 - RAPOR ciktisi):
//   detailWidth x Height = 1024 x 1024 = 1.048.576 hucre
//   Dolu hucre           = 47.788  ->  terrain'in sadece %4,6'si
//   O dar seritte ise 4 cim katmani birden 255/255 ile ust uste binmis (%568).
//
//   Yani cim YANLIS DAGILMIS: bir yerde asiri yogun, %95 alanda hic yok.
//   Kullanicinin gordugu "kimi yerde buyuk ot, kiminde duz zemin" budur.
//
// COZUM: en UCUZ cim mesh'ini (en dusuk ucgen sayili katman) bos hucrelere
// DUSUK yogunlukla yay. Zaten dolu hucrelere DOKUNULMAZ - oradaki cesitlilik
// ve yogunluk aynen kalir.
//
// MALIYET: sahne CPU sinirli ve terrain detail maliyeti tam olarak CPU
// tarafinda. Bu yuzden:
//   - taban icin en dusuk ucgenli katman secilir (Grass_A = 128 tri;
//     Grass_B/C/D 1000-1268 tri, onlar yayilsa maliyet 8-10 kat olurdu)
//   - deger dusuk tutulur (Hafif 25 / Orta 45 / Yogun 70, tavan 255)
//   - detailObjectDistance 130 m oldugu icin sadece o yaricap cizilir
//
// GUVENLIK:
//   - Yazmadan once TerrainData'nin yedegi alinir (geri donulebilir)
//   - Dik yamaclara yazilmaz (egim esigi)
//   - YOL/asfalt splatmap katmanina yazilmaz -> YolKatmani sabiti.
//     Once '0 - SPLATMAP RAPORU' calistirilip dogru indeks buraya yazilmali;
//     -1 iken yol filtresi KAPALI olur.
//
// Menu: Tools -> Foggy Road/Cim Dagilimi/...
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FoggyRoad_CimDagilim
{
    const string YedekKlasor = "Assets/GeneratedTerrain";
    const string YedekOnEk = "_CimDagilimYedek_";

    // '0 - SPLATMAP RAPORU' ciktisina bakip yol/asfalt katmaninin indeksini
    // buraya yaz. -1 = filtre kapali (yolun uzerine de cim cikabilir).
    const int YolKatmani = -1;
    const float YolEsigi = 0.35f;   // bu agirligin ustunde ise cim yazilmaz

    // Bu egimin ustundeki yamaclara taban cim yazilmaz (kayalik gorunsun)
    const float EgimEsigi = 42f;

    // ==================================================================
    //  0 - SPLATMAP RAPORU
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim Dagilimi/0 - SPLATMAP RAPORU", false, 0)]
    public static void SplatmapRaporu()
    {
        Terrain t = TerrainBul();
        if (t == null) return;
        TerrainData td = t.terrainData;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=========== SPLATMAP (terrain dokusu) RAPORU ===========");
        sb.AppendLine("Splatmap = terrain'in uzerine BOYALI doku haritasi.");
        sb.AppendLine("Ucgen maliyeti YOKTUR. 3D cim (detail) ise ayri sistemdir.");
        sb.AppendLine();

        TerrainLayer[] katmanlar = td.terrainLayers;
        sb.AppendLine("Alphamap cozunurlugu : " + td.alphamapWidth + " x " + td.alphamapHeight);
        sb.AppendLine("Katman sayisi        : " + katmanlar.Length);
        sb.AppendLine();

        int aw = td.alphamapWidth, ah = td.alphamapHeight;
        float[,,] alpha = td.GetAlphamaps(0, 0, aw, ah);
        int hucre = aw * ah;

        sb.AppendLine("--- KATMANLAR ---");
        for (int k = 0; k < katmanlar.Length; k++)
        {
            double toplam = 0.0;
            int baskin = 0;   // bu katmanin %50'den fazla oldugu hucreler

            for (int y = 0; y < ah; y++)
                for (int x = 0; x < aw; x++)
                {
                    float a = alpha[y, x, k];
                    toplam += a;
                    if (a > 0.5f) baskin++;
                }

            string ad = katmanlar[k] != null ? katmanlar[k].name : "(bos)";
            string doku = katmanlar[k] != null && katmanlar[k].diffuseTexture != null
                ? katmanlar[k].diffuseTexture.name : "-";

            sb.AppendLine("  [" + k.ToString("00") + "] " + ad.PadRight(28) +
                          " ortalama agirlik %" + (toplam / hucre * 100.0).ToString("F1") +
                          "   baskin oldugu alan %" + ((double)baskin / hucre * 100.0).ToString("F1") +
                          "   doku: " + doku);
        }

        sb.AppendLine();
        sb.AppendLine("=== NE YAPMALI ===");
        sb.AppendLine("  Yukaridaki listede YOL / ASFALT katmanini bul ve indeksini");
        sb.AppendLine("  FoggyRoad_CimDagilim.cs icindeki 'YolKatmani' sabitine yaz.");
        sb.AppendLine("  Su an YolKatmani = " + YolKatmani +
                      (YolKatmani < 0 ? "  (filtre KAPALI - yolun uzerine cim cikabilir)" : ""));

        Debug.Log(sb.ToString());
    }

    // ==================================================================
    //  1 - TABAN CIM YAY
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim Dagilimi/1 - TABAN CIM YAY - Hafif (25)", false, 20)]
    public static void YayHafif() { Yay(25); }

    [MenuItem("Tools/Foggy Road/Cim Dagilimi/1 - TABAN CIM YAY - Orta (45) (onerilen)", false, 21)]
    public static void YayOrta() { Yay(45); }

    [MenuItem("Tools/Foggy Road/Cim Dagilimi/1 - TABAN CIM YAY - Yogun (70)", false, 22)]
    public static void YayYogun() { Yay(70); }

    static void Yay(int deger)
    {
        Terrain t = TerrainBul();
        if (t == null) return;
        TerrainData td = t.terrainData;

        // --- taban katmani sec: en dusuk ucgenli cim
        DetailPrototype[] protolar = td.detailPrototypes;
        int taban = -1;
        int enAzTri = int.MaxValue;

        for (int i = 0; i < protolar.Length; i++)
        {
            int tri = MeshUcgeni(protolar[i]);
            if (tri <= 0) continue;

            string ad = ProtoAdi(protolar[i]);
            // Agac prototipleri detay listesinde de gorunuyor; onlari alma.
            if (ad.StartsWith("Pine")) continue;
            // Egrelti/cali degil, CIM istiyoruz
            if (!ad.StartsWith("Grass")) continue;

            if (tri < enAzTri) { enAzTri = tri; taban = i; }
        }

        if (taban < 0)
        {
            Debug.LogError("[FoggyRoad] Taban icin uygun cim katmani bulunamadi.");
            return;
        }

        string tabanAd = ProtoAdi(protolar[taban]);

        if (!EditorUtility.DisplayDialog(
                "Taban cim yay",
                "Taban katman : " + tabanAd + "  (" + enAzTri + " ucgen - en ucuzu)\n" +
                "Yazilacak deger : " + deger + " / 255\n\n" +
                "Zaten cim olan hucrelere DOKUNULMAZ.\n" +
                "Dik yamaclar (" + EgimEsigi + " derece ustu) atlanir.\n" +
                (YolKatmani >= 0
                    ? "Yol katmani " + YolKatmani + " baskin olan yerler atlanir.\n"
                    : "UYARI: yol filtresi KAPALI (YolKatmani = -1).\n" +
                      "Once '0 - SPLATMAP RAPORU' calistirip yol indeksini ayarlaman onerilir.\n") +
                "\nOnce yedek alinacak. Devam?",
                "Uygula", "Vazgec"))
            return;

        string yedek = YedekAl(td);

        int w = td.detailWidth, h = td.detailHeight;
        int[,] harita = td.GetDetailLayer(0, 0, w, h, taban);

        // Egim ve splatmap farkli cozunurlukte; normalize koordinatla ornekle.
        int aw = td.alphamapWidth, ah = td.alphamapHeight;
        float[,,] alpha = null;
        if (YolKatmani >= 0 && YolKatmani < td.terrainLayers.Length)
            alpha = td.GetAlphamaps(0, 0, aw, ah);

        int yazilan = 0, atlananEgim = 0, atlananYol = 0, zatenDolu = 0;

        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / (h - 1);

            for (int x = 0; x < w; x++)
            {
                if (harita[y, x] > 0) { zatenDolu++; continue; }

                float nx = (float)x / (w - 1);

                // GetSteepness(x, y) normalized koordinat alir
                if (td.GetSteepness(nx, ny) > EgimEsigi) { atlananEgim++; continue; }

                if (alpha != null)
                {
                    int ax = Mathf.Clamp(Mathf.RoundToInt(nx * (aw - 1)), 0, aw - 1);
                    int ay = Mathf.Clamp(Mathf.RoundToInt(ny * (ah - 1)), 0, ah - 1);
                    if (alpha[ay, ax, YolKatmani] > YolEsigi) { atlananYol++; continue; }
                }

                harita[y, x] = deger;
                yazilan++;
            }
        }

        Undo.RecordObject(td, "Taban cim yay");
        td.SetDetailLayer(0, 0, taban, harita);
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        int toplam = w * h;
        Debug.Log("[FoggyRoad] TABAN CIM YAYILDI\n" +
                  "  Katman        : " + tabanAd + " (" + enAzTri + " ucgen)\n" +
                  "  Deger         : " + deger + " / 255\n" +
                  "  Yazilan hucre : " + yazilan + "  (%" +
                  ((double)yazilan / toplam * 100.0).ToString("F1") + ")\n" +
                  "  Zaten doluydu : " + zatenDolu + "\n" +
                  "  Atlanan (egim): " + atlananEgim + "\n" +
                  "  Atlanan (yol) : " + atlananYol + "\n" +
                  "  Yedek         : " + yedek + "\n\n" +
                  "  Play'e girip Statistics'e bak. Agir geldiyse:\n" +
                  "    Cim Dagilimi > 2 - GERI AL  ya da daha dusuk bir deger dene.");
    }

    // ==================================================================
    //  2 - GERI AL
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Cim Dagilimi/2 - GERI AL (son yedekten)", false, 40)]
    public static void GeriAl()
    {
        Terrain t = TerrainBul();
        if (t == null) return;

        string yedekYol = EnYeniYedek();
        if (yedekYol == null)
        {
            Debug.LogError("[FoggyRoad] Bu araca ait yedek bulunamadi (" +
                           YedekOnEk + "*). 'Cim > 4 - GERI YUKLE' de denenebilir.");
            return;
        }

        TerrainData kaynak = AssetDatabase.LoadAssetAtPath<TerrainData>(yedekYol);
        if (kaynak == null)
        {
            Debug.LogError("[FoggyRoad] Yedek okunamadi: " + yedekYol);
            return;
        }

        TerrainData td = t.terrainData;
        int w = td.detailWidth, h = td.detailHeight;

        if (kaynak.detailWidth != w || kaynak.detailHeight != h)
        {
            Debug.LogError("[FoggyRoad] Yedek cozunurlugu farkli, geri yuklenmedi.");
            return;
        }

        Undo.RecordObject(td, "Cim dagilimi geri al");
        for (int i = 0; i < td.detailPrototypes.Length && i < kaynak.detailPrototypes.Length; i++)
            td.SetDetailLayer(0, 0, i, kaynak.GetDetailLayer(0, 0, w, h, i));

        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        Debug.Log("[FoggyRoad] Cim haritalari geri yuklendi: " + yedekYol);
    }

    // ==================================================================
    //  Yardimcilar
    // ==================================================================
    static Terrain TerrainBul()
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null)
        {
            Terrain[] hepsi = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            if (hepsi.Length > 0) t = hepsi[0];
        }

        if (t == null || t.terrainData == null)
            Debug.LogError("[FoggyRoad] Sahnede terrain bulunamadi.");

        return t;
    }

    static string YedekAl(TerrainData td)
    {
        string kaynak = AssetDatabase.GetAssetPath(td);
        string ad = YedekOnEk + System.DateTime.Now.ToString("yyyyMMdd_HHmm") + ".asset";
        string hedef = Path.Combine(YedekKlasor, ad).Replace("\\", "/");

        if (AssetDatabase.CopyAsset(kaynak, hedef))
        {
            AssetDatabase.SaveAssets();
            return hedef;
        }

        Debug.LogWarning("[FoggyRoad] Yedek alinamadi: " + hedef);
        return "(alinamadi)";
    }

    static string EnYeniYedek()
    {
        string[] guidler = AssetDatabase.FindAssets("t:TerrainData", new[] { YedekKlasor });
        string enYeni = null;
        System.DateTime enYeniZaman = System.DateTime.MinValue;

        foreach (string g in guidler)
        {
            string yol = AssetDatabase.GUIDToAssetPath(g);
            if (!Path.GetFileName(yol).StartsWith(YedekOnEk)) continue;

            System.DateTime z = File.GetLastWriteTime(yol);
            if (z > enYeniZaman) { enYeniZaman = z; enYeni = yol; }
        }

        return enYeni;
    }

    static string ProtoAdi(DetailPrototype p)
    {
        if (p == null) return "(bos)";
        if (p.prototype != null) return p.prototype.name;
        if (p.prototypeTexture != null) return p.prototypeTexture.name;
        return "(isimsiz)";
    }

    static int MeshUcgeni(DetailPrototype p)
    {
        if (p == null || p.prototype == null) return 2;   // texture detail = billboard

        int toplam = 0;
        foreach (MeshFilter mf in p.prototype.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) toplam += mf.sharedMesh.triangles.Length / 3;

        return toplam > 0 ? toplam : 2;
    }
}
#endif
