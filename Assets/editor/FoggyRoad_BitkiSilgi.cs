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
using System.Linq;
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
    enum FircaModu { Sil, Ekle }
    FircaModu mod;
    float yogunluk = 1.3f, yumusaklik = .3f, azamiEgim = 50;
    bool engelleriKoru = true;
    readonly Dictionary<string, float> bitkiAgirliklari = new Dictionary<string, float>();
    readonly Dictionary<TerrainData, HashSet<int>> ziyaretEdilen = new Dictionary<TerrainData, HashSet<int>>();
    readonly HashSet<Terrain> degisenTerrainler = new HashSet<Terrain>();
    System.Random rastgele;
    Vector3 sonDamga; bool damgaVar;
    int toplamEklenen;
    int sahipKontrol;
    bool uzakKirli;
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
        w.mod = FircaModu.Sil;
        w.minSize = new Vector2(360, 500);
        w.Show();
    }

    [MenuItem("Tools/Foggy Road/Bitki Fırçası (Ekle ve Sil)")]
    public static void AcEkle()
    {
        var w = GetWindow<FoggyRoad_BitkiSilgi>("Bitki Fırçası");
        w.mod = FircaModu.Ekle; w.yogunluk = 1.3f; w.yumusaklik = .3f; w.bitkiAgirliklari.Clear(); w.minSize = new Vector2(360, 500); w.Show();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += SahneGui;
    }

    void OnDisable()
    {
        StrokeKapat();
        SceneView.duringSceneGui -= SahneGui;
        FircaAyarla(false);
    }

    void FircaAyarla(bool ac)
    {
        if (ac && EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (ac == fircaAcik) return;
        if (!ac) { StrokeKapat(); if (sahipKontrol != 0 && GUIUtility.hotControl == sahipKontrol) GUIUtility.hotControl = 0; sahipKontrol = 0; }
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
            "Scene View: sol tık / sürükle = ekle veya sil. Ekle modunda Shift = geçici ot silgisi.\n[ ve ] = fırça boyutu. Alt + fare = normal kamera gezintisi.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(aktifStroke != null))
            mod = (FircaModu)GUILayout.Toolbar((int)mod, new[] { "Silgi", "Rastgele bitki ekle" });
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("Boyamak için Play Mode'dan çık.", MessageType.Info);
            EditorGUILayout.EndScrollView(); return;
        }
        // --- firca ac/kapat ---
        if (mod == FircaModu.Ekle) yaricap = Mathf.Min(yaricap, 25f);
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

        yaricap = EditorGUILayout.Slider("Yaricap (m)", yaricap, 0.5f, mod == FircaModu.Ekle ? 25f : 50f);

        EditorGUILayout.Space();
        if (mod == FircaModu.Sil)
        {
            EditorGUILayout.LabelField("Ne silinsin?", EditorStyles.boldLabel);
            silDetay = EditorGUILayout.Toggle("Çimen / detail katmanları", silDetay);
            silAgac = EditorGUILayout.Toggle("Terrain ağaçları", silAgac);
        }

        var terrain = HedefTerrain();
        if (terrain == null)
        {
            EditorGUILayout.HelpBox("Sahnede aktif Terrain bulunamadi.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        var td = terrain.terrainData;

        // --- detail katman secimi ---
        if (mod == FircaModu.Sil && silDetay)
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
        if (mod == FircaModu.Sil && silAgac)
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

        if (mod == FircaModu.Ekle)
        {
            EditorGUILayout.HelpBox("Mevcut Terrain çimen/eğrelti katmanlarını karıştırır. Boyları sahnedeki ayarlardan alınır; eski bitkilerin boyu değişmez. Ağaç ve yükselti eklemez.", MessageType.Info);
            yogunluk = EditorGUILayout.Slider("Örtü yoğunluğu", yogunluk, .05f, 3f);
            yumusaklik = EditorGUILayout.Slider("Yumuşak kenar", yumusaklik, 0, 1);
            azamiEgim = EditorGUILayout.Slider("En fazla eğim", azamiEgim, 0, 80);
            engelleriKoru = EditorGUILayout.Toggle("Yol/beton/collider üstüne ekleme", engelleriKoru);
            EditorGUILayout.LabelField("Bitki karışımı (0 = kapalı)", EditorStyles.boldLabel);
            for (int i = 0; i < td.detailPrototypes.Length; i++)
            {
                var proto = td.detailPrototypes[i]; if (!VegetationBrushPaint.IsPlant(proto)) continue;
                string key = VegetationBrushPaint.Key(proto); VarsayilanAgirlik(proto);
                bitkiAgirliklari[key] = EditorGUILayout.Slider(VegetationBrushPaint.Name(proto), bitkiAgirliklari[key], 0, 2);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dolu çayır")) { yogunluk = 1.3f; yumusaklik = .3f; bitkiAgirliklari.Clear(); }
            if (GUILayout.Button("Çok sık / eğreltili"))
            {
                yogunluk = 2.2f; yumusaklik = .25f; bitkiAgirliklari.Clear();
                foreach (var proto in td.detailPrototypes) if (VegetationBrushPaint.IsPlant(proto)) bitkiAgirliklari[VegetationBrushPaint.Key(proto)] = VegetationBrushPaint.Name(proto).ToLowerInvariant().Contains("dry") ? .25f : 1f;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Tek geçişte çok katmanlı örtü. Yoğunluğu artırmak çizim yükünü de artırır.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Boyut: katmanların mevcut Min/Max Width ve Height ayarları", EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.LabelField("Eklenen hücre: " + toplamEklenen, EditorStyles.miniLabel);
        if (uzakKirli) EditorGUILayout.HelpBox("Boya/silme hazır. 150m uzak örtüde de görünmesi için bitince aşağıdaki Kaydet + uzak çimeni güncelle düğmesine bas.", MessageType.Info);
        // --- geri al / kaydet ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bu oturum: " + toplamSilinenDetay + " detail hucresi, " +
                                   toplamSilinenAgac + " agac silindi.", EditorStyles.miniLabel);

        using (new EditorGUI.DisabledScope(gecmis.Count == 0))
        {
            if (GUILayout.Button("GERI AL  (son darbe)  [" + gecmis.Count + "]"))
                GeriAl();
        }

        if (GUILayout.Button("Kaydet + uzak çimeni güncelle", GUILayout.Height(28)))
        {
            StrokeKapat();
            try
            {
                foreach (var t in degisenTerrainler) if (t) AssetDatabase.SaveAssetIfDirty(t.terrainData);
                EditorUtility.DisplayProgressBar("Bitki Fırçası", "Uzak çimen örtüsü güncelleniyor...", .5f);
                VegetationBrushPaint.RefreshDistantGrass(degisenTerrainler);
                AssetDatabase.SaveAssets(); uzakKirli = false; SceneView.RepaintAll();
            }
            finally { EditorUtility.ClearProgressBar(); }
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
        if (p.usePrototypeMesh && p.prototype != null) return VegetationBrushPaint.Name(p);
        if (p.prototypeTexture != null) return p.prototypeTexture.name;
        return "Detail " + i;
    }

    static Terrain HedefTerrain()
    {
        var hepsi = Terrain.activeTerrains;
        if (hepsi == null || hepsi.Length == 0) return null;
        return hepsi.FirstOrDefault(VegetationBrushPaint.IsGroundTerrain);
    }

    // ==================================================================
    //  Scene View
    // ==================================================================
    void SahneGui(SceneView sv)
    {
        if (!fircaAcik || EditorApplication.isPlayingOrWillChangePlaymode) return;

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
                yaricap = Mathf.Min(mod == FircaModu.Ekle ? 25f : 50f, yaricap * 1.25f);
                e.Use(); Repaint(); sv.Repaint();
            }
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { FircaAyarla(false); e.Use(); return; }
        bool ekle = mod == FircaModu.Ekle && !e.shift;
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

            Handles.color = ekle ? new Color(.25f, 1f, .35f, .20f) : new Color(1f, .35f, .25f, .20f);
            Handles.DrawSolidDisc(sonNokta + normal * 0.05f, normal, yaricap);
            Handles.color = ekle ? new Color(.25f, 1f, .35f, .95f) : new Color(1f, .35f, .25f, .95f);
            Handles.DrawWireDisc(sonNokta + normal * 0.05f, normal, yaricap);

            Handles.BeginGUI();
            var gp = HandleUtility.WorldToGUIPoint(sonNokta);
            GUI.Label(new Rect(gp.x + 14, gp.y - 10, 200, 20),
                (ekle ? "EKLE  r=" : "SİL  r=") + yaricap.ToString("0.0") + " m");
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
                if (!noktaGecerli || e.control || e.command) return;
                aktifStroke = new Stroke(); ziyaretEdilen.Clear(); rastgele = new System.Random(System.Environment.TickCount);
                damgaVar = false; GUIUtility.hotControl = kontrolId; sahipKontrol = kontrolId;
                Uygula(sonNokta, ekle);
                e.Use();
            }
            else if (tip == EventType.MouseDrag)
            {
                if (aktifStroke == null) return;
                if (noktaGecerli)
                {
                    float spacing = Mathf.Max(.25f, yaricap * .25f);
                    float distance = Vector2.Distance(new Vector2(sonDamga.x, sonDamga.z), new Vector2(sonNokta.x, sonNokta.z));
                    if (!damgaVar || distance >= spacing)
                    {
                        Vector3 from = sonDamga; int steps = Mathf.Clamp(Mathf.CeilToInt(distance / spacing), 1, 32);
                        for (int i = 1; i <= steps; i++) Uygula(Vector3.Lerp(from, sonNokta, i / (float)steps), ekle);
                    }
                }
                e.Use();
            }
            else if (tip == EventType.MouseUp)
            {
                StrokeKapat(); GUIUtility.hotControl = 0; sahipKontrol = 0;
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
            if (!VegetationBrushPaint.IsGroundTerrain(t)) continue;
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
        aktifStroke = null; damgaVar = false;
        Repaint();
    }

    void VarsayilanAgirlik(DetailPrototype p)
    {
        string key = VegetationBrushPaint.Key(p); if (bitkiAgirliklari.ContainsKey(key)) return;
        string name = VegetationBrushPaint.Name(p).ToLowerInvariant();
        bitkiAgirliklari[key] = name.Contains("fern") ? .65f : name.Contains("dry") ? .35f : 1f;
    }

    void Uygula(Vector3 merkez, bool ekle)
    {
        sonDamga = merkez; damgaVar = true;
        if (!ekle)
        {
            if (mod == FircaModu.Sil) Sil(merkez);
            else foreach (var t in Terrain.activeTerrains) if (VegetationBrushPaint.IsGroundTerrain(t)) DetaySil(t, merkez);
            return;
        }
        foreach (var t in Terrain.activeTerrains)
        {
            if (!VegetationBrushPaint.IsGroundTerrain(t)) continue;
            var palette = new List<VegetationBrushPaint.Choice>(); var prototypes = t.terrainData.detailPrototypes;
            for (int i = 0; i < prototypes.Length; i++)
            {
                if (!VegetationBrushPaint.IsPlant(prototypes[i])) continue;
                VarsayilanAgirlik(prototypes[i]);
                palette.Add(new VegetationBrushPaint.Choice { layer = i, weight = bitkiAgirliklari[VegetationBrushPaint.Key(prototypes[i])] });
            }
            if (!ziyaretEdilen.TryGetValue(t.terrainData, out var visited)) ziyaretEdilen.Add(t.terrainData, visited = new HashSet<int>());
            var result = VegetationBrushPaint.Paint(t, merkez, yaricap, yogunluk, yumusaklik, azamiEgim, engelleriKoru, palette, visited, rastgele);
            foreach (var patch in result.patches) aktifStroke.detaylar.Add(new DetailYama { td = patch.data, katman = patch.layer, xBase = patch.x, yBase = patch.z, oncekiHarita = patch.before });
            if (result.cells > 0) { toplamEklenen += result.cells; degisenTerrainler.Add(t); uzakKirli = true; }
        }
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
            toplamSilinenDetay += degisen; degisenTerrainler.Add(t); uzakKirli = true;

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

        td.SetTreeInstances(kalan.ToArray(), false);
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
            td.SetTreeInstances(liste.ToArray(), false);
            EditorUtility.SetDirty(td);
        }

        foreach (var t in Terrain.activeTerrains) { t.Flush(); if (VegetationBrushPaint.IsGroundTerrain(t)) degisenTerrainler.Add(t); }
        uzakKirli = true;

        Debug.Log("[Bitki Silgisi] Son darbe geri alindi. Kalan gecmis: " + gecmis.Count);
        SceneView.RepaintAll();
        Repaint();
    }
}
#endif
