// Katmanli dag silueti - yol hattini takip eder, yolun SAGINDA ve SOLUNDA.
//
// AMAC: agaclarin arkasinda birden fazla derinlikte dag silueti. Daglar
// dunyada SABIT durur, yol boyunca ilerledikce gercek parallax olusur.
//
// ------------------------------------------------------------------------
// NEDEN HALKA DEGIL (denendi, basarisiz oldu):
//
//   SkyboxMountains.prefab 8 parcali kapali bir halka. Olculdu:
//     dis yaricap   ~12.492 m
//     ic kenar      ~2.863 m   -> dis yaricapin sadece %23'u
//
//   Yani halka "ince bir cember" degil; parcalar merkeze dogru cok iceri
//   uzaniyor. 3.500 m yaricapli bir halka kurulunca ic kenar 800 m'ye
//   dusuyor, yol ise merkezden ~1.500 m uzaniyor -> daglar YOLUN USTUNE
//   biniyor. Yolun temiz kalmasi icin yaricapin ~8.000 m olmasi gerekirdi.
//
// ------------------------------------------------------------------------
// KULLANILAN YONTEM: parcalar TEK TEK, yol hatti boyunca, yanal offset ile.
//
//   KRITIK DETAY - MESH MERKEZI TELAFISI:
//   Bu FBX'lerin pivotu origin'de ama geometrisi 2-8 km otede, kendi
//   acisinda duruyor (parca bir halkanin dilimi oldugu icin). Dolayisiyla
//   transform.position'a hedef noktayi yazmak parcayi kilometrelerce oteye
//   atar. Bu yuzden her parca yerlestirilirken mesh'in KENDI merkezi
//   hesaba katilip pozisyon geri kaydirilir (bkz. Yerlestir()).
//
//   Ayrica taban hizalamasi: mesh'in ALT siniri istenen Y'ye oturtulur,
//   yoksa daglar havada asili kalir ya da zemine gomulur.
//
// ------------------------------------------------------------------------
// IKI SAHNE ENGELI ve cozumleri:
//
//   1) KAMERA far clip = 500 m. Daglar kilometrelerce otede olacagi icin
//      hicbiri cizilmez. Arac far clip'i otomatik yukseltir; near clip
//      0.1 -> 0.3 cekilerek depth hassasiyeti korunur.
//
//   2) CIFTE SIS. Volumetrik sisin (MaxDistance 300) disinda RenderSettings
//      Exponential fog da acik (density 0.004) = 1200 m'de %99 opaklik.
//      Daglar konsa bile gorunmezdi. Linear fog'a cevrilip menzil uzatilir.
//
// Menu: Tools -> Foggy Road/Daglar (Katmanli Siluet)
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System.Collections.Generic;
using FoggyRoadTools.Yol;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FoggyRoadTools.Daglar
{
    public class DagYerlestirici : EditorWindow
    {
        const string RootName = "Daglar_Auto";
        const string ModelKlasor = "Assets/TerrainDemoScene_URP/Prefabs/Skybox/Models";

        /// <summary>Bir derinlik katmani.</summary>
        [System.Serializable]
        class Katman
        {
            public string ad;
            public bool acik = true;
            public float mesafeMin;    // yoldan yanal uzaklik (m)
            public float mesafeMax;
            public float genislik;     // tek dagin hedef genisligi (m)
            public float aralik;       // yol boyunca kac metrede bir
            public float tabanY;       // dagin ALT sinirinin oturacagi Y
        }

        readonly Katman[] katmanlar =
        {
            new Katman { ad = "1 - Yakin tepeler", mesafeMin = 700f,  mesafeMax = 1100f,
                         genislik = 700f,  aralik = 650f,  tabanY = -40f },

            new Katman { ad = "2 - Orta daglar",   mesafeMin = 1500f, mesafeMax = 2100f,
                         genislik = 1200f, aralik = 1100f, tabanY = -30f },

            new Katman { ad = "3 - Uzak siluet",   mesafeMin = 2800f, mesafeMax = 3600f,
                         genislik = 2000f, aralik = 1700f, tabanY = -20f },
        };

        // --- kaynak modeller
        List<GameObject> modeller = new List<GameObject>();
        List<bool> modelAcik = new List<bool>();
        List<Bounds> modelBounds = new List<Bounds>();   // local, olceksiz
        List<int> modelUcgen = new List<int>();

        List<Vector3> hat = new List<Vector3>();
        int seed = 12345;
        bool solaDa = true;
        bool sagaDa = true;
        bool kameraAyarla = true;
        bool sisAyarla = true;

        Vector2 kaydir;
        string durum = "";
        MessageType durumTipi = MessageType.None;

        // ==============================================================
        [MenuItem("Tools/Foggy Road/Daglar (Katmanli Siluet)")]
        public static void Ac()
        {
            DagYerlestirici w = GetWindow<DagYerlestirici>(false, "Daglar", true);
            w.minSize = new Vector2(440f, 560f);
            w.ModelleriYukle();
            w.HattiTazele();
            w.Show();
        }

        void OnEnable()
        {
            if (modeller.Count == 0) ModelleriYukle();
        }

        // ==============================================================
        //  Modeller
        // ==============================================================
        void ModelleriYukle()
        {
            modeller.Clear();
            modelAcik.Clear();
            modelBounds.Clear();
            modelUcgen.Clear();

            string[] guidler = AssetDatabase.FindAssets("SkyboxMountains t:GameObject",
                                                        new[] { ModelKlasor });
            List<string> yollar = new List<string>();
            foreach (string g in guidler)
            {
                string y = AssetDatabase.GUIDToAssetPath(g);
                if (y.ToLower().EndsWith(".fbx")) yollar.Add(y);
            }
            yollar.Sort();

            foreach (string y in yollar)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(y);
                if (go == null) continue;

                Bounds b = new Bounds(Vector3.zero, Vector3.zero);
                bool ilk = true;
                int tri = 0;

                foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    tri += mf.sharedMesh.triangles.Length / 3;

                    Bounds mb = mf.sharedMesh.bounds;
                    // FBX kokune gore kaydir (parcalar genelde 0'da ama garanti olsun)
                    mb.center += mf.transform.localPosition;

                    if (ilk) { b = mb; ilk = false; }
                    else b.Encapsulate(mb);
                }

                if (ilk) continue;

                modeller.Add(go);
                modelAcik.Add(true);
                modelBounds.Add(b);
                modelUcgen.Add(tri);
            }

            if (modeller.Count == 0)
                Durum("Model bulunamadi: " + ModelKlasor, MessageType.Error);
        }

        // ==============================================================
        //  Yol hatti
        // ==============================================================
        void HattiTazele()
        {
            hat.Clear();

            YolHatti.HatSonucu sonuc;
            string err;

            if (YolHatti.HatBariyerlerden(out sonuc, out err) && sonuc.merkez.Count >= 2)
            {
                hat = YolHatti.Smooth(new List<Vector3>(sonuc.merkez), 2);
                Durum("Yol hatti hazir: " + sonuc.aciklama, MessageType.Info);
            }
            else
            {
                Durum("Yol hatti bulunamadi (bariyer parcasi yok). " +
                      "Bariyerler sahnede olmali.", MessageType.Error);
            }
        }

        // ==============================================================
        //  UI
        // ==============================================================
        void OnGUI()
        {
            kaydir = EditorGUILayout.BeginScrollView(kaydir);

            EditorGUILayout.HelpBox(
                "Daglari YOL HATTI boyunca, yolun saginda ve solunda dizer.\n" +
                "Dunyada sabit dururlar - ilerledikce parallax olusur.",
                MessageType.None);

            // --- yol
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Yol", EditorStyles.boldLabel);
            if (GUILayout.Button("Hatti tazele")) HattiTazele();
            if (hat.Count >= 2)
                EditorGUILayout.LabelField("Hat uzunlugu",
                    YolHatti.PolylineLength(hat).ToString("N0") + " m  (" + hat.Count + " nokta)");

            // --- modeller
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Kaynak modeller", EditorStyles.boldLabel);

            if (modeller.Count == 0)
            {
                if (GUILayout.Button("Tekrar ara")) ModelleriYukle();
            }
            else
            {
                for (int i = 0; i < modeller.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    modelAcik[i] = EditorGUILayout.Toggle(modelAcik[i], GUILayout.Width(18));
                    EditorGUILayout.LabelField(modeller[i].name, GUILayout.Width(150));
                    Vector3 s = modelBounds[i].size;
                    EditorGUILayout.LabelField(
                        s.x.ToString("N0") + " x " + s.y.ToString("N0") + " m, " +
                        modelUcgen[i].ToString("N0") + " tri", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }

            // --- katmanlar
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Katmanlar", EditorStyles.boldLabel);

            foreach (Katman k in katmanlar)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                k.acik = EditorGUILayout.ToggleLeft(k.ad, k.acik, EditorStyles.boldLabel);

                if (k.acik)
                {
                    EditorGUI.indentLevel++;
                    MinMax("Yoldan uzaklik (m)", ref k.mesafeMin, ref k.mesafeMax, 200f, 6000f);
                    k.genislik = EditorGUILayout.Slider("Dag genisligi (m)", k.genislik, 200f, 4000f);
                    k.aralik = EditorGUILayout.Slider("Yol boyu aralik (m)", k.aralik, 200f, 3000f);
                    k.tabanY = EditorGUILayout.Slider("Taban Y (m)", k.tabanY, -300f, 200f);

                    float o = Olcek(k);
                    float yuk = OrtalamaYukseklik() * o;
                    EditorGUILayout.LabelField(
                        "  -> olcek " + o.ToString("F3") + ",  yukseklik ~" + yuk.ToString("N0") + " m" +
                        ",  " + KatmanAdedi(k) + " dag", EditorStyles.miniLabel);

                    // Dag genisligi araliktan buyukse siluet sureklidir - bu IYI.
                    // Kucukse arada bosluk kalir, uyar.
                    if (k.genislik < k.aralik * 0.9f)
                        EditorGUILayout.HelpBox(
                            "Dag genisligi araliktan kucuk - siluette bosluk kalir.",
                            MessageType.Warning);

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            // --- genel
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            solaDa = EditorGUILayout.ToggleLeft("Sol tarafa", solaDa, GUILayout.Width(110));
            sagaDa = EditorGUILayout.ToggleLeft("Sag tarafa", sagaDa, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();
            seed = EditorGUILayout.IntField("Rastgelelik seed", seed);

            // --- ozet
            int toplam = 0;
            long tri = 0;
            float enUzak = 0f;
            int ortTri = OrtalamaUcgen();

            foreach (Katman k in katmanlar)
                if (k.acik)
                {
                    int n = KatmanAdedi(k);
                    toplam += n;
                    tri += (long)n * ortTri;
                    if (k.mesafeMax > enUzak) enUzak = k.mesafeMax;
                }

            if (toplam > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "TOPLAM: " + toplam + " dag,  ~" + tri.ToString("N0") + " ucgen\n" +
                    "En uzak katman: " + enUzak.ToString("N0") + " m",
                    tri > 3000000 ? MessageType.Warning : MessageType.Info);
            }

            // --- sahne ayarlari
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Sahne ayarlari", EditorStyles.boldLabel);
            kameraAyarla = EditorGUILayout.Toggle("Kamera clip'lerini ayarla", kameraAyarla);
            sisAyarla = EditorGUILayout.Toggle("Sis menzilini ayarla", sisAyarla);

            if (sisAyarla)
                EditorGUILayout.HelpBox(
                    "RenderSettings Exponential fog (density 0.004) 1200 m'de %99 " +
                    "opaklik uretiyor - daglar gorunmez. Linear'a cevrilip menzil uzatilir.",
                    MessageType.None);

            // --- butonlar
            EditorGUILayout.Space(8);
            GUI.enabled = hat.Count >= 2 && modeller.Count > 0 && toplam > 0 && (solaDa || sagaDa);
            if (GUILayout.Button("DAGLARI DIZ", GUILayout.Height(28))) Diz();
            GUI.enabled = true;

            GUI.enabled = MevcutKok() != null;
            if (GUILayout.Button("DAGLARI KALDIR", GUILayout.Height(22))) Kaldir();
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(durum))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(durum, durumTipi);
            }

            EditorGUILayout.EndScrollView();
        }

        static void MinMax(string etiket, ref float min, ref float max, float alt, float ust)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(etiket);
            min = EditorGUILayout.FloatField(min, GUILayout.Width(58));
            EditorGUILayout.MinMaxSlider(ref min, ref max, alt, ust);
            max = EditorGUILayout.FloatField(max, GUILayout.Width(58));
            EditorGUILayout.EndHorizontal();
            if (max < min) max = min;
        }

        /// <summary>Acik modellerin ortalama XZ genisligi (olceksiz).</summary>
        float OrtalamaGenislik()
        {
            float t = 0f; int n = 0;
            for (int i = 0; i < modeller.Count; i++)
            {
                if (!modelAcik[i]) continue;
                Vector3 s = modelBounds[i].size;
                t += Mathf.Max(s.x, s.z);
                n++;
            }
            return n > 0 ? t / n : 1f;
        }

        float OrtalamaYukseklik()
        {
            float t = 0f; int n = 0;
            for (int i = 0; i < modeller.Count; i++)
            {
                if (!modelAcik[i]) continue;
                t += modelBounds[i].size.y;
                n++;
            }
            return n > 0 ? t / n : 1f;
        }

        int OrtalamaUcgen()
        {
            long t = 0; int n = 0;
            for (int i = 0; i < modeller.Count; i++)
            {
                if (!modelAcik[i]) continue;
                t += modelUcgen[i];
                n++;
            }
            return n > 0 ? (int)(t / n) : 0;
        }

        float Olcek(Katman k)
        {
            float g = OrtalamaGenislik();
            return g > 1f ? k.genislik / g : 0.1f;
        }

        int KatmanAdedi(Katman k)
        {
            if (hat.Count < 2) return 0;
            float uzunluk = YolHatti.PolylineLength(hat);
            int n = Mathf.Max(1, Mathf.FloorToInt(uzunluk / Mathf.Max(100f, k.aralik)) + 1);
            int taraf = (solaDa ? 1 : 0) + (sagaDa ? 1 : 0);
            return n * taraf;
        }

        // ==============================================================
        //  Dizme
        // ==============================================================
        void Diz()
        {
            GameObject eski = MevcutKok();
            if (eski != null)
            {
                bool devam = EditorUtility.DisplayDialog(
                    "Daglar yeniden dizilecek",
                    "Sahnede zaten '" + RootName + "' var.\n\n" +
                    "Elle yaptigin duzenlemeler kaybolacak. Devam edilsin mi?",
                    "Sil ve yeniden diz", "Vazgec");
                if (!devam) return;
                Undo.DestroyObjectImmediate(eski);
            }

            List<int> acik = new List<int>();
            for (int i = 0; i < modeller.Count; i++) if (modelAcik[i]) acik.Add(i);
            if (acik.Count == 0) { Durum("Hic model secili degil.", MessageType.Error); return; }

            Random.State eskiState = Random.state;
            Random.InitState(seed);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Daglari diz");

            int toplam = 0;
            float enUzak = 0f;

            try
            {
                for (int ki = 0; ki < katmanlar.Length; ki++)
                {
                    Katman k = katmanlar[ki];
                    if (!k.acik) continue;

                    GameObject katmanKok = new GameObject(k.ad);
                    katmanKok.transform.SetParent(root.transform, false);

                    List<Vector3> noktalar = YolHatti.Resample(hat, Mathf.Max(100f, k.aralik));
                    float olcek = Olcek(k);

                    for (int i = 0; i < noktalar.Count; i++)
                    {
                        if (i % 4 == 0)
                            EditorUtility.DisplayProgressBar("Daglar", k.ad,
                                (float)i / Mathf.Max(1, noktalar.Count));

                        Vector3 teget = YolHatti.FlatTangentAt(noktalar, i);
                        Vector3 sagYon = Vector3.Cross(Vector3.up, teget).normalized;

                        for (int t = 0; t < 2; t++)
                        {
                            bool sag = t == 0;
                            if (sag && !sagaDa) continue;
                            if (!sag && !solaDa) continue;

                            float yon = sag ? 1f : -1f;
                            float mesafe = Random.Range(k.mesafeMin, k.mesafeMax);
                            // Yol boyunca da kaydir ki sira sira dizilmis gorunmesin
                            float boyunca = Random.Range(-k.aralik * 0.3f, k.aralik * 0.3f);

                            Vector3 hedef = noktalar[i]
                                          + sagYon * (mesafe * yon)
                                          + teget * boyunca;

                            int mi = acik[Random.Range(0, acik.Count)];
                            GameObject go = Yerlestir(modeller[mi], modelBounds[mi],
                                                     katmanKok.transform, hedef,
                                                     k.tabanY, olcek,
                                                     Random.Range(0f, 360f));
                            if (go == null) continue;

                            go.name = modeller[mi].name + "_" + (sag ? "R" : "L") + i;
                            Undo.RegisterCreatedObjectUndo(go, "Daglari diz");

                            toplam++;
                            if (mesafe > enUzak) enUzak = mesafe;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Random.state = eskiState;
            }

            float gerekli = enUzak + 500f;
            string kameraNot = kameraAyarla ? KamerayiAyarla(gerekli) : "";
            string sisNot = sisAyarla ? SisiAyarla(gerekli) : "";

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Durum(toplam + " dag dizildi.", MessageType.Info);
            Debug.Log("[FoggyRoad] DAGLAR DIZILDI (yol hatti boyunca)\n" +
                      "  Toplam  : " + toplam + " dag\n" +
                      "  Seed    : " + seed + "\n" +
                      "  En uzak : " + enUzak.ToString("N0") + " m\n" +
                      kameraNot + sisNot +
                      "\n  Begenmezsen seed'i degistir ya da katman mesafelerini " +
                      "buyutup tekrar diz.\n" +
                      "  Kaldirmak icin: DAGLARI KALDIR");
        }

        /// <summary>
        /// Bir dag parcasini hedef noktaya yerlestirir.
        ///
        /// KRITIK: bu FBX'lerin pivotu origin'de ama geometrisi kilometrelerce
        /// otede (parca bir halkanin dilimi). Dolayisiyla position'a dogrudan
        /// hedefi yazmak parcayi bambaska bir yere atar. Burada mesh'in kendi
        /// merkezi, uygulanan donus ve olcekle birlikte hesaplanip pozisyon
        /// GERI KAYDIRILIR; boylece mesh'in merkezi tam hedefe oturur.
        ///
        /// Y ekseninde ise merkez degil ALT SINIR hizalanir - daglar havada
        /// asili kalmasin diye.
        /// </summary>
        static GameObject Yerlestir(GameObject model, Bounds lokal, Transform ebeveyn,
                                    Vector3 hedef, float tabanY, float olcek, float aci)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model, ebeveyn);
            if (go == null) return null;

            Quaternion rot = Quaternion.Euler(0f, aci, 0f);
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * olcek;

            // Mesh merkezinin XZ'de nereye dustugu (donus + olcek sonrasi)
            Vector3 merkezXZ = rot * new Vector3(lokal.center.x * olcek, 0f, lokal.center.z * olcek);

            // Mesh'in alt sinirinin pivota gore Y offseti
            float altOfset = (lokal.center.y - lokal.extents.y) * olcek;

            go.transform.position = new Vector3(
                hedef.x - merkezXZ.x,
                tabanY - altOfset,
                hedef.z - merkezXZ.z);

            // Uzak manzara: golge dokmez/almaz (golge menzili 110 m), batching static
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            foreach (Transform tr in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.BatchingStatic);

            return go;
        }

        // ==============================================================
        //  Kamera / sis
        // ==============================================================
        static string KamerayiAyarla(float gerekliMesafe)
        {
            float hedef = Mathf.Ceil(gerekliMesafe * 1.15f / 100f) * 100f;
            string not = "";

            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (c.cameraType != CameraType.Game) continue;
                if (!c.gameObject.activeInHierarchy || !c.enabled) continue;

                float eskiFar = c.farClipPlane, eskiNear = c.nearClipPlane;
                bool degisti = false;

                Undo.RecordObject(c, "Kamera clip ayari");
                if (c.farClipPlane < hedef) { c.farClipPlane = hedef; degisti = true; }
                if (c.nearClipPlane < 0.3f) { c.nearClipPlane = 0.3f; degisti = true; }

                if (degisti)
                {
                    EditorUtility.SetDirty(c);
                    not += "  Kamera '" + c.name + "': far " + eskiFar.ToString("0") + " -> " +
                           c.farClipPlane.ToString("0") + ", near " + eskiNear.ToString("0.##") +
                           " -> " + c.nearClipPlane.ToString("0.##") + "\n";
                }
            }
            return not;
        }

        static string SisiAyarla(float gerekliMesafe)
        {
            if (!RenderSettings.fog) return "  Sis kapali, dokunulmadi.\n";

            FogMode eskiMod = RenderSettings.fogMode;
            float eskiDensity = RenderSettings.fogDensity;

            // RenderSettings statik; Undo ile kaydedilemez. Eski degerler log'a
            // yazilir, geri almak icin Lighting > Environment > Fog kullanilir.
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 300f;
            RenderSettings.fogEndDistance = Mathf.Max(1500f, gerekliMesafe * 1.6f);

            return "  Sis: " + eskiMod + " (density " + eskiDensity.ToString("0.####") +
                   ") -> Linear 300 - " + RenderSettings.fogEndDistance.ToString("N0") + " m\n";
        }

        // ==============================================================
        void Kaldir()
        {
            GameObject kok = MevcutKok();
            if (kok == null) { Durum("Sahnede '" + RootName + "' yok.", MessageType.Warning); return; }

            int adet = 0;
            foreach (Transform k in kok.transform) adet += k.childCount;

            if (!EditorUtility.DisplayDialog("Daglari kaldir",
                    "'" + RootName + "' ve icindeki " + adet + " dag silinecek.\n\nDevam?",
                    "Sil", "Vazgec"))
                return;

            Undo.DestroyObjectImmediate(kok);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Durum("Daglar kaldirildi.", MessageType.Info);
            Debug.Log("[FoggyRoad] Daglar kaldirildi (" + adet + " obje).\n" +
                      "  Not: kamera clip ve sis ayarlari DEGISMEDI.");
        }

        static GameObject MevcutKok()
        {
            Scene s = SceneManager.GetActiveScene();
            foreach (GameObject g in s.GetRootGameObjects())
                if (g.name == RootName) return g;
            return null;
        }

        void Durum(string mesaj, MessageType tip)
        {
            durum = mesaj;
            durumTipi = tip;
            Repaint();
        }
    }
}
#endif
