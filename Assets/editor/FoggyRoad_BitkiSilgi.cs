// BITKI SILGISI: Scene View firca ile terrain uzerindeki cim (detail) ve
// agac (tree) verisini belirli bir noktadan siler.
//
// NEDEN GEREKLI:
//   Unity'nin kendi "Paint Trees > Shift+Click" silgisi SADECE terrain tree
//   verisini siler. Bu projede gorunen otlarin buyuk kismi tree degil DETAIL
//   katmani (Grass_A/B/C/D + Fern_A/B/C, 1024x1024 harita, ~0.5 m hucre).
//   Bu yuzden Paint Trees sekmesinde silmeye calisinca "kalkmiyor" gorunur.
//   Detail silmek icin Paint Details sekmesi gerekir, o da tek seferde tek
//   katman siler -> 13 katman icin 13 ayri firca darbesi.
//   Bu tool ikisini de TEK darbede, secilen tum katmanlar icin siler.
//
// GERI ALMA - EN KRITIK KARAR:
//   Undo.RecordObject / RegisterCompleteObjectUndo TerrainData uzerinde
//   KULLANILMAZ. Gerekcesi FoggyRoad_CimDagilim.cs icinde olculerek yazilmis:
//   TerrainData devasa bir nesne (1024x1024 detail x 13 katman + heightmap +
//   alphamap); undo yigina tam kopyasi girer ve Ctrl+Z yapildiginda editor
//   dakikalarca kilitlenir (olculdu: 'Hold on... Menu.Redo' 4 dk+).
//   Bunun yerine: her firca darbesi (stroke) icin SADECE dokunulan kucuk
//   bolgenin onceki hali + silinen agaclar hafizada tutulur. "Geri Al"
//   butonu bu yamayi geri yazar. Anlik, birkac KB.
//
// TERRAIN ARACI CAKISMASI (ilk denemede "basiyorum silmiyor" sebebi):
//   Unity'nin Terrain Inspector'undaki paint araclari (Paint Trees, Paint
//   Details, Raise/Lower ...) SADECE terrain GameObject'i SECILIYKEN aktiftir
//   ve Scene View'daki sol tiki bizim callback'imizden once yutarlar.
//   Bu yuzden firca acilirken terrain secimi kaldirilir (secim hatirlanir,
//   firca kapatilinca geri verilir). Kod tarafinda ek guvenlik olarak
//   e.rawType de kontrol edilir: olay baska bir kontrol tarafindan
//   Use() edilmis olsa bile ham tipi hala okunabilir.
//
// KULLANIM:
//   Tools -> Foggy Road -> Bitki Silgisi
//   "Firca AC" -> Scene View'da sol tik / surukle = sil
//   [ ve ] tuslari yaricapi degistirir
//   Degisiklikler TerrainData asset'ine yazilir -> pencereden "Kaydet"
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FoggyRoad_BitkiSilgi : EditorWindow
{
    // ------------------------------------------------------------------
    //  Stroke kaydi: tek firca darbesinin geri alinabilmesi icin gereken
    //  minimum veri. Tum TerrainData degil, sadece dokunulan dikdortgen.
    // ------------------------------------------------------------------
    class DetailYama
    {
        public TerrainData td;
        public int katman;
        public int xBase, yBase;
        public int[,] oncekiHarita;
    }

    class Stroke
    {
        public List<DetailYama> detaylar = new List<DetailYama>();
        // Agaclar icin bolge yamasi mantikli degil (dizi indexleri kayar),
        // bu yuzden silinen instance'lar aynen saklanir ve geri eklenir.
        public Dictionary<TerrainData, List<TreeInstance>> silinenAgaclar =
            new Dictionary<TerrainData, List<TreeInstance>>();
    }

    const int MaksStroke = 30;

    // ------------------------------------------------------------------
    //  Durum
    // ------------------------------------------------------------------
    bool fircaAcik;
    float yaricap = 4f;

    bool silDetay = true;
    bool silAgac = true;

    bool tumDetayKatmanlari = true;
    bool tumAgacTipleri = true;

    // Terrain'e gore degisebildigi icin prototip secimleri isim bazli tutulur.
    HashSet<int> seciliDetayIndex = new HashSet<int>();
    HashSet<int> seciliAgacIndex = new HashSet<int>();

    Vector2 kaydirma;
    bool detayKatlandi;
    bool agacKatlandi;

    readonly List<Stroke> gecmis = new List<Stroke>();
    Stroke aktifStroke;

    // Firca acilirken kaldirilan secim; kapatilinca geri verilir.
    Object[] oncekiSecim;

    Terrain sonTerrain;
    Vector3 sonNokta;
    bool noktaGecerli;

    int toplamSilinenDetay;
    int toplamSilinenAgac;

    [MenuItem("Tools/Foggy Road/Bitki Silgisi")]
    static void Ac()
    {
        var w = GetWindow<FoggyRoad_BitkiSilgi>("Bitki Silgisi");
        w.minSize = new Vector2(320, 420);
        w.Show();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += SahneGui;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= SahneGui;
        FircaAyarla(false);
    }

    void FircaAyarla(bool ac)
    {
        if (ac == fircaAcik) return;
        fircaAcik = ac;

        if (ac)
        {
            // Terrain paint araci sadece terrain SECILIYKEN olay yakalar.
            // Secimi kaldirinca Scene View'daki sol tik bize kalir.
            oncekiSecim = Selection.objects;
            Selection.objects = new Object[0];
        }
        else if (oncekiSecim != null)
        {
            Selection.objects = oncekiSecim;
            oncekiSecim = null;
        }

        SceneView.RepaintAll();
    }

    static bool TerrainSecili()
    {
        foreach (var o in Selection.gameObjects)
            if (o != null && o.GetComponent<Terrain>() != null) return true;
        return false;
    }

    // ==================================================================
    //  Pencere
    // ==================================================================
    void OnGUI()
    {
        kaydirma = EditorGUILayout.BeginScrollView(kaydirma);

        EditorGUILayout.HelpBox(
            "Scene View'da sol tik / surukle ile firca altindaki bitkileri siler.\n" +
            "[ ve ] tuslari yaricapi degistirir.",
            MessageType.None);

        // --- firca ac/kapat ---
        var eskiRenk = GUI.backgroundColor;
        GUI.backgroundColor = fircaAcik ? new Color(1f, 0.45f, 0.35f) : eskiRenk;
        if (GUILayout.Button(fircaAcik ? "FIRCA ACIK  (kapatmak icin tikla)" : "FIRCA AC",
                GUILayout.Height(32)))
        {
            FircaAyarla(!fircaAcik);
        }
        GUI.backgroundColor = eskiRenk;

        // Terrain secili kalirsa Unity'nin paint araci tiki yutar.
        if (fircaAcik && TerrainSecili())
        {
            EditorGUILayout.HelpBox(
                "Terrain secili. Unity'nin terrain paint araci sol tiki yutuyor, " +
                "firca calismaz.", MessageType.Error);
            if (GUILayout.Button("Terrain secimini kaldir"))
            {
                oncekiSecim = Selection.objects;
                Selection.objects = new Object[0];
            }
        }

        EditorGUILayout.Space();

        yaricap = EditorGUILayout.Slider("Yaricap (m)", yaricap, 0.5f, 50f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ne silinsin?", EditorStyles.boldLabel);
        silDetay = EditorGUILayout.Toggle("Cim / detail katmanlari", silDetay);
        silAgac = EditorGUILayout.Toggle("Terrain agaclari", silAgac);

        var terrain = HedefTerrain();
        if (terrain == null)
        {
            EditorGUILayout.HelpBox("Sahnede aktif Terrain bulunamadi.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        var td = terrain.terrainData;

        // --- detail katman secimi ---
        if (silDetay)
        {
            EditorGUILayout.Space();
            detayKatlandi = EditorGUILayout.Foldout(detayKatlandi,
                "Detail katmanlari (" + td.detailPrototypes.Length + ")", true);
            if (detayKatlandi)
            {
                EditorGUI.indentLevel++;
                tumDetayKatmanlari = EditorGUILayout.Toggle("Hepsi", tumDetayKatmanlari);
                using (new EditorGUI.DisabledScope(tumDetayKatmanlari))
                {
                    var protos = td.detailPrototypes;
                    for (int i = 0; i < protos.Length; i++)
                    {
                        bool secili = tumDetayKatmanlari || seciliDetayIndex.Contains(i);
                        bool yeni = EditorGUILayout.Toggle(i + " - " + DetayAdi(protos[i], i), secili);
                        if (yeni != secili)
                        {
                            if (yeni) seciliDetayIndex.Add(i);
                            else seciliDetayIndex.Remove(i);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        // --- agac tipi secimi ---
        if (silAgac)
        {
            EditorGUILayout.Space();
            agacKatlandi = EditorGUILayout.Foldout(agacKatlandi,
                "Agac tipleri (" + td.treePrototypes.Length + ")", true);
            if (agacKatlandi)
            {
                EditorGUI.indentLevel++;
                tumAgacTipleri = EditorGUILayout.Toggle("Hepsi", tumAgacTipleri);
                using (new EditorGUI.DisabledScope(tumAgacTipleri))
                {
                    var protos = td.treePrototypes;
                    for (int i = 0; i < protos.Length; i++)
                    {
                        bool secili = tumAgacTipleri || seciliAgacIndex.Contains(i);
                        string ad = protos[i].prefab != null ? protos[i].prefab.name : "Tree " + i;
                        bool yeni = EditorGUILayout.Toggle(i + " - " + ad, secili);
                        if (yeni != secili)
                        {
                            if (yeni) seciliAgacIndex.Add(i);
                            else seciliAgacIndex.Remove(i);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        // --- geri al / kaydet ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bu oturum: " + toplamSilinenDetay + " detail hucresi, " +
                                   toplamSilinenAgac + " agac silindi.", EditorStyles.miniLabel);

        using (new EditorGUI.DisabledScope(gecmis.Count == 0))
        {
            if (GUILayout.Button("GERI AL  (son darbe)  [" + gecmis.Count + "]"))
                GeriAl();
        }

        if (GUILayout.Button("Degisiklikleri Kaydet (AssetDatabase)"))
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[Bitki Silgisi] TerrainData asset'leri kaydedildi.");
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Geri alma bu pencerenin kendi yiginini kullanir (son " + MaksStroke + " darbe).\n" +
            "Ctrl+Z KULLANMAYIN: TerrainData undo yigina tam kopya girer, editor " +
            "dakikalarca kilitlenir. Pencere kapanirsa yigin da silinir; " +
            "memnunsaniz Kaydet'e basin.",
            MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    static string DetayAdi(DetailPrototype p, int i)
    {
        if (p.usePrototypeMesh && p.prototype != null) return p.prototype.name;
        if (p.prototypeTexture != null) return p.prototypeTexture.name;
        return "Detail " + i;
    }

    static Terrain HedefTerrain()
    {
        var hepsi = Terrain.activeTerrains;
        if (hepsi == null || hepsi.Length == 0) return null;
        return hepsi[0];
    }

    // ==================================================================
    //  Scene View
    // ==================================================================
    void SahneGui(SceneView sv)
    {
        if (!fircaAcik) return;

        var e = Event.current;

        // Firca acikken tiklama ile obje secilmesini engelle.
        int kontrolId = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(kontrolId);

        // Yaricap kisayollari
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.LeftBracket)
            {
                yaricap = Mathf.Max(0.5f, yaricap * 0.8f);
                e.Use(); Repaint(); sv.Repaint();
            }
            else if (e.keyCode == KeyCode.RightBracket)
            {
                yaricap = Mathf.Min(50f, yaricap * 1.25f);
                e.Use(); Repaint(); sv.Repaint();
            }
        }

        // Firca konumu
        var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        noktaGecerli = TerrainVur(ray, out sonTerrain, out sonNokta);

        if (noktaGecerli)
        {
            var normal = sonTerrain.terrainData.GetInterpolatedNormal(
                Mathf.InverseLerp(sonTerrain.transform.position.x,
                    sonTerrain.transform.position.x + sonTerrain.terrainData.size.x, sonNokta.x),
                Mathf.InverseLerp(sonTerrain.transform.position.z,
                    sonTerrain.transform.position.z + sonTerrain.terrainData.size.z, sonNokta.z));

            Handles.color = new Color(1f, 0.35f, 0.25f, 0.20f);
            Handles.DrawSolidDisc(sonNokta + normal * 0.05f, normal, yaricap);
            Handles.color = new Color(1f, 0.35f, 0.25f, 0.95f);
            Handles.DrawWireDisc(sonNokta + normal * 0.05f, normal, yaricap);

            Handles.BeginGUI();
            var gp = HandleUtility.WorldToGUIPoint(sonNokta);
            GUI.Label(new Rect(gp.x + 14, gp.y - 10, 200, 20),
                "SIL  r=" + yaricap.ToString("0.0") + " m");
            Handles.EndGUI();

            sv.Repaint();
        }

        // Silme.
        // rawType kullaniliyor: olay baska bir kontrol tarafindan Use() edilmis
        // olsa bile (Unity terrain paint araci gibi) ham tipi hala okunabilir.
        // Bir olay tek bir duringSceneGui cagrisinda gelir, cift silme olmaz.
        var tip = e.type == EventType.Used ? e.rawType : e.type;

        if (e.button == 0 && !e.alt)
        {
            if (tip == EventType.MouseDown)
            {
                aktifStroke = new Stroke();
                if (noktaGecerli) Sil(sonNokta);
                e.Use();
            }
            else if (tip == EventType.MouseDrag)
            {
                if (noktaGecerli) Sil(sonNokta);
                e.Use();
            }
            else if (tip == EventType.MouseUp)
            {
                StrokeKapat();
                e.Use();
            }
        }
    }

    static bool TerrainVur(Ray ray, out Terrain terrain, out Vector3 nokta)
    {
        terrain = null;
        nokta = Vector3.zero;
        float enYakin = float.MaxValue;

        foreach (var t in Terrain.activeTerrains)
        {
            var col = t.GetComponent<TerrainCollider>();
            if (col == null) continue;
            RaycastHit hit;
            if (col.Raycast(ray, out hit, 100000f) && hit.distance < enYakin)
            {
                enYakin = hit.distance;
                terrain = t;
                nokta = hit.point;
            }
        }
        return terrain != null;
    }

    void StrokeKapat()
    {
        if (aktifStroke == null) return;

        if (aktifStroke.detaylar.Count > 0 || aktifStroke.silinenAgaclar.Count > 0)
        {
            gecmis.Add(aktifStroke);
            while (gecmis.Count > MaksStroke) gecmis.RemoveAt(0);
        }
        aktifStroke = null;
        Repaint();
    }

    // ==================================================================
    //  Silme
    // ==================================================================
    void Sil(Vector3 merkez)
    {
        // Firca birden fazla terrain'e tasabilir.
        foreach (var t in Terrain.activeTerrains)
        {
            var td = t.terrainData;
            var tp = t.transform.position;

            // Firca bu terrain'in sinirlarina degiyor mu?
            if (merkez.x + yaricap < tp.x || merkez.x - yaricap > tp.x + td.size.x) continue;
            if (merkez.z + yaricap < tp.z || merkez.z - yaricap > tp.z + td.size.z) continue;

            if (silDetay) DetaySil(t, merkez);
            if (silAgac) AgacSil(t, merkez);
        }
        Repaint();
    }

    void DetaySil(Terrain t, Vector3 merkez)
    {
        var td = t.terrainData;
        var tp = t.transform.position;

        int w = td.detailWidth, h = td.detailHeight;
        if (w < 2 || h < 2) return;

        // Normalize koordinat -> hucre index. Proje kalibi: nx = x / (w - 1).
        float nxMin = (merkez.x - yaricap - tp.x) / td.size.x;
        float nxMax = (merkez.x + yaricap - tp.x) / td.size.x;
        float nzMin = (merkez.z - yaricap - tp.z) / td.size.z;
        float nzMax = (merkez.z + yaricap - tp.z) / td.size.z;

        int xBase = Mathf.Clamp(Mathf.FloorToInt(nxMin * (w - 1)), 0, w - 1);
        int xSon = Mathf.Clamp(Mathf.CeilToInt(nxMax * (w - 1)), 0, w - 1);
        int yBase = Mathf.Clamp(Mathf.FloorToInt(nzMin * (h - 1)), 0, h - 1);
        int ySon = Mathf.Clamp(Mathf.CeilToInt(nzMax * (h - 1)), 0, h - 1);

        int gw = xSon - xBase + 1;
        int gh = ySon - yBase + 1;
        if (gw <= 0 || gh <= 0) return;

        float r2 = yaricap * yaricap;
        var protos = td.detailPrototypes;

        for (int katman = 0; katman < protos.Length; katman++)
        {
            if (!tumDetayKatmanlari && !seciliDetayIndex.Contains(katman)) continue;

            // DIKKAT: detail haritasi [y, x] siralidir (proje kalibi,
            // FoggyRoad_CimDagilim.cs:188 ile ayni).
            int[,] harita = td.GetDetailLayer(xBase, yBase, gw, gh, katman);

            int[,] onceki = null;
            int degisen = 0;

            for (int y = 0; y < gh; y++)
            {
                float nz = (float)(yBase + y) / (h - 1);
                float wz = tp.z + nz * td.size.z;
                float dz = wz - merkez.z;

                for (int x = 0; x < gw; x++)
                {
                    if (harita[y, x] == 0) continue;

                    float nx = (float)(xBase + x) / (w - 1);
                    float wx = tp.x + nx * td.size.x;
                    float dx = wx - merkez.x;

                    if (dx * dx + dz * dz > r2) continue;

                    if (onceki == null)
                    {
                        onceki = new int[gh, gw];
                        System.Array.Copy(harita, onceki, harita.Length);
                    }

                    harita[y, x] = 0;
                    degisen++;
                }
            }

            if (degisen == 0) continue;

            td.SetDetailLayer(xBase, yBase, katman, harita);
            toplamSilinenDetay += degisen;

            if (aktifStroke != null)
            {
                aktifStroke.detaylar.Add(new DetailYama
                {
                    td = td,
                    katman = katman,
                    xBase = xBase,
                    yBase = yBase,
                    oncekiHarita = onceki
                });
            }
        }

        EditorUtility.SetDirty(td);
    }

    void AgacSil(Terrain t, Vector3 merkez)
    {
        var td = t.terrainData;
        var tp = t.transform.position;

        var mevcut = td.treeInstances;
        if (mevcut == null || mevcut.Length == 0) return;

        float r2 = yaricap * yaricap;

        var kalan = new List<TreeInstance>(mevcut.Length);
        var silinen = new List<TreeInstance>();

        for (int i = 0; i < mevcut.Length; i++)
        {
            var inst = mevcut[i];

            bool sil = false;
            if (tumAgacTipleri || seciliAgacIndex.Contains(inst.prototypeIndex))
            {
                float dx = tp.x + inst.position.x * td.size.x - merkez.x;
                float dz = tp.z + inst.position.z * td.size.z - merkez.z;
                sil = dx * dx + dz * dz <= r2;
            }

            if (sil) silinen.Add(inst);
            else kalan.Add(inst);
        }

        if (silinen.Count == 0) return;

        td.SetTreeInstances(kalan.ToArray(), true);
        t.Flush();
        toplamSilinenAgac += silinen.Count;

        if (aktifStroke != null)
        {
            List<TreeInstance> liste;
            if (!aktifStroke.silinenAgaclar.TryGetValue(td, out liste))
            {
                liste = new List<TreeInstance>();
                aktifStroke.silinenAgaclar[td] = liste;
            }
            liste.AddRange(silinen);
        }

        EditorUtility.SetDirty(td);
    }

    // ==================================================================
    //  Geri alma
    // ==================================================================
    void GeriAl()
    {
        if (gecmis.Count == 0) return;

        var s = gecmis[gecmis.Count - 1];
        gecmis.RemoveAt(gecmis.Count - 1);

        // Detail yamalari ters sirada geri yaz (ayni bolge birden cok kez
        // dokunulmus olabilir; en eski hali en sonda yazilmali).
        for (int i = s.detaylar.Count - 1; i >= 0; i--)
        {
            var y = s.detaylar[i];
            if (y.td == null) continue;
            y.td.SetDetailLayer(y.xBase, y.yBase, y.katman, y.oncekiHarita);
            EditorUtility.SetDirty(y.td);
        }

        foreach (var kv in s.silinenAgaclar)
        {
            var td = kv.Key;
            if (td == null) continue;

            var liste = new List<TreeInstance>(td.treeInstances);
            liste.AddRange(kv.Value);
            td.SetTreeInstances(liste.ToArray(), true);
            EditorUtility.SetDirty(td);
        }

        foreach (var t in Terrain.activeTerrains) t.Flush();

        Debug.Log("[Bitki Silgisi] Son darbe geri alindi. Kalan gecmis: " + gecmis.Count);
        SceneView.RepaintAll();
        Repaint();
    }
}
#endif
