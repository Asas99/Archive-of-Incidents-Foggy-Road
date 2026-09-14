// Elektrik direklerini yol boyunca dizer ve aralarina kablo ceker.
//
// Yontem:
//   1) Yolun merkez hatti cikarilir (FoggyRoad_YolHatti). Hat birkac kaynaktan
//      gelebilir; tek kaynaga guvenmek kirilgan cunku sahnedeki 'yol2' inactive
//      olabiliyor ve mesh her zaman okunabilir degil.
//   2) Hat uzerinde sabit araliklarla noktalar alinir, saga/sola offset uygulanir.
//   3) Her nokta icin zemin ornekleneir (raycast -> Terrain.SampleHeight).
//   4) TUM yerlesim hesabi bittikten SONRA instantiate edilir; boylece yeni
//      direkler eskilerin collider'ina carpmaz.
//   5) Kok objeye FoggyRoad_PowerLine eklenir, direk listesi ve hat ona verilir.
//      Bundan sonra dizici cekilir - kullanici direkleri elle tasidiginda kabloyu
//      o bilesen kendi basina gunceller.
//
// Direkler HER ZAMAN dik durur. Gercek elektrik diregi zemin egimine yatmaz;
// guardrail placer'dan temel fark budur.
//
// Menu: Tools -> Foggy Road/Elektrik Direkleri (Yola Otomatik Diz)
#if UNITY_EDITOR
using System.Collections.Generic;
using FoggyRoadTools.Yol;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoggyRoadTools.Direk
{
    public class DirekAutoPlacer : EditorWindow
    {
        const string RootName = "ElektrikDirekleri_Auto";
        const string KabloName = "Kablolar";
        const int MaxInstances = 2000;

        enum HatKaynagi
        {
            Otomatik,
            Bariyerlerden,
            Yol_Mesh,
            Secili_Objelerden,
            Elle_Ciz
        }

        /// <summary>
        /// Offset'in olculdugu hat. "Bariyerin hemen yaninda" istendiginde yol
        /// merkezi yerine bariyer hatti referans alinir; boylece offset 1-2 m gibi
        /// kucuk ve anlasilir bir sayi olur.
        /// </summary>
        enum Referans
        {
            Yol_Merkezi,
            Bariyer_Sol,
            Bariyer_Sag
        }

        // ------------------------------------------------------------ ayarlar
        [SerializeField] GameObject poleSource;
        [SerializeField] GameObject roadObject;
        [SerializeField] HatKaynagi kaynak = HatKaynagi.Otomatik;
        [SerializeField] Referans referans = Referans.Yol_Merkezi;

        [SerializeField] float spacing = 35f;
        [SerializeField] float offset = 5f;        // + sag, - sol
        [SerializeField] float yonDuzeltme;        // direk modelinin yaw ofseti (derece)
        [SerializeField] float sinkDepth = 0.20f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] bool zemineOturt = true;
        [SerializeField] float maxGroundDrop = 8f;

        [SerializeField] int wireCount = 2;
        [SerializeField] float sagRatio = 0.030f;
        [SerializeField] float wireRadius = 0.045f;

        // mesh kaynagi parametreleri
        [SerializeField] float cellSize = 0.25f;
        [SerializeField] float topNormalThreshold = 0.35f;
        [SerializeField] float centerBias = 6f;

        // ------------------------------------------------------------ durum
        List<Vector3> hat = new List<Vector3>();
        YolHatti.HatSonucu hatSonucu;
        List<Vector3> elleNoktalar = new List<Vector3>();
        List<Yerlesim> onizleme = new List<Yerlesim>();
        bool elleCizimAktif;
        bool meshOkunamadi;
        string durum = "Basla: 'HATTI BUL'a bas.";
        MessageType durumTipi = MessageType.Info;
        Vector2 scroll;

        // ==================================================================
        [MenuItem("Tools/Foggy Road/Elektrik Direkleri (Yola Otomatik Diz)")]
        public static void Ac()
        {
            DirekAutoPlacer w = GetWindow<DirekAutoPlacer>(false, "Elektrik Direkleri", true);
            w.minSize = new Vector2(420f, 560f);
            w.Show();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            HedefleriBul();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void HedefleriBul()
        {
            if (roadObject == null) roadObject = YolHatti.BulYol();
            if (poleSource == null)
            {
                poleSource = AssetDatabase.LoadAssetAtPath<GameObject>(
                    FoggyRoad_PowerLineAssets.DirekPrefabYolu);
            }
            if (poleSource == null)
            {
                string[] guids = AssetDatabase.FindAssets("elektrikdire t:GameObject");
                if (guids.Length > 0)
                {
                    poleSource = AssetDatabase.LoadAssetAtPath<GameObject>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
        }

        // ==================================================================
        //  Arayuz
        // ==================================================================
        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("ELEKTRIK DIREKLERI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Yol boyunca elektrik direklerini dizer ve aralarina sarkan siyah kablo ceker.\n" +
                "Dizdikten sonra direkleri elle tasiyabilir/silebilirsin; kablo kendini yeniler.",
                MessageType.None);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Hedefler", EditorStyles.boldLabel);
            poleSource = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Direk prefab / FBX"), poleSource, typeof(GameObject), false);
            roadObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Yol objesi (mesh kaynagi icin)"), roadObject, typeof(GameObject), true);

            if (poleSource != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(FoggyRoad_PowerLineAssets.DirekPrefabYolu) == null)
            {
                EditorGUILayout.HelpBox(
                    "Ham FBX kullaniliyor. Kablo baglanti noktalarini Scene view'da elle ayarlamak " +
                    "istersen once prefab variant olustur.", MessageType.None);
                if (GUILayout.Button("Direk prefab'ini hazirla (Anchor_L / Anchor_R ekler)"))
                    PrefabHazirla();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Hat", EditorStyles.boldLabel);
            kaynak = (HatKaynagi)EditorGUILayout.EnumPopup("Hat kaynagi", kaynak);

            if (kaynak == HatKaynagi.Yol_Mesh || kaynak == HatKaynagi.Otomatik)
            {
                EditorGUI.indentLevel++;
                cellSize = EditorGUILayout.Slider("Izgara hucresi", cellSize, 0.1f, 2f);
                topNormalThreshold = EditorGUILayout.Slider("Ust yuzey esigi", topNormalThreshold, 0.1f, 0.9f);
                centerBias = EditorGUILayout.Slider("Merkezde kalma", centerBias, 0f, 20f);
                EditorGUI.indentLevel--;
            }

            if (kaynak == HatKaynagi.Elle_Ciz)
            {
                EditorGUILayout.HelpBox(
                    "Scene view'da CTRL + SOL TIK ile nokta koy. En az 2 nokta gerekli.\n" +
                    "Noktalar zemine raycast ile oturur.", MessageType.Info);

                elleCizimAktif = EditorGUILayout.ToggleLeft(
                    "Elle cizim aktif (Ctrl + sol tik)", elleCizimAktif);
                EditorGUILayout.LabelField("Konulan nokta", elleNoktalar.Count.ToString());

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Son noktayi sil") && elleNoktalar.Count > 0)
                {
                    elleNoktalar.RemoveAt(elleNoktalar.Count - 1);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Hepsini temizle"))
                {
                    elleNoktalar.Clear();
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
            }
            else elleCizimAktif = false;

            if (kaynak == HatKaynagi.Secili_Objelerden)
            {
                EditorGUILayout.HelpBox(
                    "Hierarchy'de yol boyunca uzanan objeleri sec (ornegin bariyer direkleri), " +
                    "sonra 'HATTI BUL'a bas.", MessageType.Info);
                EditorGUILayout.LabelField("Secili obje", Selection.transforms.Length.ToString());
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("HATTI BUL", GUILayout.Height(26))) HattiBul();

            if (meshOkunamadi)
            {
                EditorGUILayout.HelpBox("Yol mesh'i okunabilir degil (Read/Write Enabled kapali).",
                                        MessageType.Warning);
                if (GUILayout.Button("Yol mesh'ini okunabilir yap"))
                {
                    int n = YolHatti.MeshOkunabilirYap(roadObject);
                    meshOkunamadi = false;
                    Durum(n + " mesh okunabilir yapildi. Tekrar 'HATTI BUL'a bas.", MessageType.Info);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Yerlesim", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            Referans yeniReferans = (Referans)EditorGUILayout.EnumPopup("Offset nereden olculsun", referans);
            if (yeniReferans != referans)
            {
                referans = yeniReferans;
                ReferansUygula();
            }
            if (hatSonucu != null && !hatSonucu.tarafliVar && referans != Referans.Yol_Merkezi)
            {
                EditorGUILayout.HelpBox(
                    "Bu hat kaynagi taraf ayrimi uretmedi; bariyer referansi yol merkeziyle ayni. " +
                    "Taraf ayrimi icin objelerin adinda _L_01 / _R_01 gibi indeks olmali.",
                    MessageType.Warning);
            }

            spacing = EditorGUILayout.Slider("Direk araligi (m)", spacing, 8f, 120f);
            offset = EditorGUILayout.Slider("Offset (m)", offset, -20f, 20f);
            EditorGUILayout.LabelField(" ",
                (offset >= 0f ? "sag taraf" : "sol taraf") + " - " +
                (referans == Referans.Yol_Merkezi ? "yol merkezinden" : "bariyer hattindan"),
                EditorStyles.miniLabel);

            yonDuzeltme = EditorGUILayout.Slider("Direk yon duzeltmesi (derece)", yonDuzeltme, -180f, 180f);
            EditorGUILayout.LabelField(" ", "traverse yola dik degilse 90 yap", EditorStyles.miniLabel);

            zemineOturt = EditorGUILayout.Toggle("Zemine oturt", zemineOturt);
            if (zemineOturt)
            {
                EditorGUI.indentLevel++;
                sinkDepth = EditorGUILayout.Slider("Gomme derinligi (m)", sinkDepth, 0f, 1.5f);
                maxGroundDrop = EditorGUILayout.Slider("Izin verilen kot farki (m)", maxGroundDrop, 1f, 40f);
                groundMask = LayerMaskAlani("Zemin layer'lari", groundMask);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck() && hat.Count >= 2)
            {
                // Surgu oynatilinca onizleme aninda guncellensin
                OnizlemeHesapla();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Kablo", EditorStyles.boldLabel);
            wireCount = EditorGUILayout.IntSlider("Kablo sayisi", wireCount, 1, 6);
            sagRatio = EditorGUILayout.Slider("Sarkma orani", sagRatio, 0.002f, 0.12f);
            EditorGUILayout.LabelField(" ", "aciklik x " + sagRatio.ToString("0.000") +
                                       "  (35 m -> " + (35f * sagRatio).ToString("0.00") + " m sarkma)",
                                       EditorStyles.miniLabel);
            wireRadius = EditorGUILayout.Slider("Kablo yaricapi (m)", wireRadius, 0.01f, 0.15f);

            EditorGUILayout.Space(10);
            GUI.enabled = hat.Count >= 2 && poleSource != null;
            if (GUILayout.Button("DIREKLERI DIZ", GUILayout.Height(32))) Diz();
            GUI.enabled = true;

            GameObject mevcutKok = MevcutKok();
            GUI.enabled = mevcutKok != null;
            if (GUILayout.Button("DIREKLERI KALDIR", GUILayout.Height(22))) Kaldir();
            GUI.enabled = true;

            if (hat.Count >= 2)
            {
                float uzunluk = YolHatti.PolylineLength(hat);
                int adet = Mathf.Max(2, Mathf.FloorToInt(uzunluk / Mathf.Max(1f, spacing)) + 1);
                EditorGUILayout.LabelField("Hat uzunlugu", uzunluk.ToString("0") + " m");
                EditorGUILayout.LabelField("Tahmini direk", adet.ToString());
                EditorGUILayout.LabelField("Onizlemede", onizleme.Count + " direk");
                if (onizleme.Count > 0 && onizleme.Count < adet - 1)
                {
                    EditorGUILayout.HelpBox(
                        "Bazi direkler atlandi (zemin bulunamadi veya kot farki sinirini asti). " +
                        "'Izin verilen kot farki' degerini buyut veya 'Zemin layer'larini kontrol et.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Elle duzenleme", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Direkleri Scene view'da serbestce tasi/sil/kopyala - kablo kendini yeniler.\n" +
                "Elle yeni direk koydun ve kabloya girmediyse asagidaki butonu kullan.",
                MessageType.None);

            if (GUILayout.Button("Secili direkleri hatta ekle"))
                SeciliDirekleriEkle();

            if (GUILayout.Button("Kablolari yeniden bagla"))
                FoggyRoad_PowerLineWatcher.YenidenBagla();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(durum, durumTipi);

            EditorGUILayout.EndScrollView();
        }

        static LayerMask LayerMaskAlani(string etiket, LayerMask mask)
        {
            int deger = EditorGUILayout.MaskField(etiket,
                UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(mask),
                UnityEditorInternal.InternalEditorUtility.layers);
            return UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(deger);
        }

        void Durum(string msg, MessageType tip)
        {
            durum = msg;
            durumTipi = tip;
            Repaint();
        }

        // ==================================================================
        //  Hat bulma
        // ==================================================================
        void HattiBul()
        {
            hat.Clear();
            hatSonucu = null;
            meshOkunamadi = false;
            string err;

            switch (kaynak)
            {
                case HatKaynagi.Elle_Ciz:
                    if (elleNoktalar.Count < 2)
                    {
                        Durum("En az 2 nokta koy (Scene view'da Ctrl + sol tik).", MessageType.Error);
                        return;
                    }
                    hatSonucu = TekHat(YolHatti.Smooth(
                        YolHatti.Resample(new List<Vector3>(elleNoktalar), 2f), 2),
                        "elle cizim");
                    break;

                case HatKaynagi.Secili_Objelerden:
                    if (Selection.transforms.Length < 2)
                    {
                        Durum("Hierarchy'de en az 2 obje sec.", MessageType.Error);
                        return;
                    }
                    if (!YolHatti.HatTransformlardan(Selection.transforms, out hatSonucu, out err))
                    {
                        Durum(err, MessageType.Error);
                        return;
                    }
                    break;

                case HatKaynagi.Bariyerlerden:
                    if (!YolHatti.HatBariyerlerden(out hatSonucu, out err))
                    {
                        Durum(err, MessageType.Error);
                        return;
                    }
                    break;

                case HatKaynagi.Yol_Mesh:
                    if (!MeshHatti(out err)) { Durum(err, MessageType.Error); return; }
                    break;

                default: // Otomatik: once bariyer, sonra mesh
                    if (!YolHatti.HatBariyerlerden(out hatSonucu, out err))
                    {
                        string err2;
                        if (!MeshHatti(out err2))
                        {
                            Durum("Hat bulunamadi.\n- Bariyerlerden: " + err +
                                  "\n- Yol mesh'inden: " + err2 +
                                  "\n\n'Elle Ciz' kaynagini deneyebilirsin.", MessageType.Error);
                            return;
                        }
                    }
                    break;
            }

            ReferansUygula();
            OnizlemeHesapla();
            SceneView.RepaintAll();
        }

        static YolHatti.HatSonucu TekHat(List<Vector3> pts, string aciklama)
        {
            YolHatti.HatSonucu s = new YolHatti.HatSonucu();
            s.merkez = pts;
            s.sol = pts;
            s.sag = pts;
            s.tarafliVar = false;
            s.aciklama = aciklama;
            return s;
        }

        /// <summary>Secilen referansa gore aktif hatti belirler.</summary>
        void ReferansUygula()
        {
            if (hatSonucu == null) return;

            switch (referans)
            {
                case Referans.Bariyer_Sol: hat = new List<Vector3>(hatSonucu.sol); break;
                case Referans.Bariyer_Sag: hat = new List<Vector3>(hatSonucu.sag); break;
                default: hat = new List<Vector3>(hatSonucu.merkez); break;
            }

            string tarafBilgi = hatSonucu.tarafliVar
                ? "sol/sag hatlar ayri cikarildi"
                : "tek hat (taraf ayrimi yok, referans secimi etkisiz)";

            Durum("Hat kaynagi: " + hatSonucu.aciklama + "\n" +
                  hat.Count + " nokta, " + YolHatti.PolylineLength(hat).ToString("0") + " m\n" +
                  tarafBilgi, MessageType.Info);
        }

        bool MeshHatti(out string err)
        {
            List<Vector3> bulunan;
            if (!YolHatti.HatMeshten(roadObject, cellSize, topNormalThreshold, centerBias,
                                     out bulunan, out err, out meshOkunamadi))
                return false;

            hatSonucu = TekHat(bulunan, "yol mesh'i (izgara + Dijkstra)");
            return true;
        }

        // ==================================================================
        //  Yerlesim hesabi
        // ==================================================================
        struct Yerlesim
        {
            public Vector3 pos;
            public float yaw;      // dunya Y ekseni etrafinda donus (derece)
        }

        void OnizlemeHesapla()
        {
            onizleme.Clear();
            List<Yerlesim> y = YerlesimHesapla(false);
            onizleme.AddRange(y);
        }

        List<Yerlesim> YerlesimHesapla(bool logla)
        {
            List<Yerlesim> sonuc = new List<Yerlesim>();
            if (hat.Count < 2) return sonuc;

            float[] cum = YolHatti.BuildCumulative(hat);
            float toplam = cum[cum.Length - 1];
            if (toplam < 1f) return sonuc;

            float step = Mathf.Max(1f, spacing);
            int adet = Mathf.FloorToInt(toplam / step) + 1;
            adet = Mathf.Min(adet, MaxInstances);

            // Bosluk kalmasin: adim yeniden dagitilir
            float actualStep = adet > 1 ? toplam / (adet - 1) : toplam;
            int atlanan = 0;

            for (int i = 0; i < adet; i++)
            {
                float s = actualStep * i;
                Vector3 merkez = YolHatti.SampleAt(hat, cum, s);

                // Teget icin hattaki en yakin indeksi kullan
                int idx = Mathf.Clamp(Mathf.RoundToInt(s / Mathf.Max(0.01f, toplam) * (hat.Count - 1)),
                                      0, hat.Count - 1);
                Vector3 tangent = YolHatti.FlatTangentAt(hat, idx);
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

                Vector3 pos;
                if (!NoktaYerlestir(merkez, right, out pos)) { atlanan++; continue; }

                Yerlesim ye;
                ye.pos = pos;
                // SADECE dunya Y ekseni etrafinda donus. Tam rotasyonu yazmak
                // (LookRotation) prefab'in kendi import rotasyonunu ezip diregi
                // yan yatiriyordu - "yamuk dizilme" sorunu buydu.
                ye.yaw = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg;
                sonuc.Add(ye);
            }

            if (logla && atlanan > 0)
            {
                Debug.LogWarning($"[FoggyRoad] {atlanan} direk atlandi: zemin bulunamadi veya " +
                                 $"kot farki {maxGroundDrop} m sinirini asti.");
            }
            return sonuc;
        }

        /// <summary>
        /// Ucurum korumasi: offset'te zemin cok asagidaysa offset kademeli azaltilir.
        /// Hicbiri tutmazsa direk atlanir - kablo sadece var olan direkleri bagalar,
        /// arada uzun bir aciklik olusur ve sarkma otomatik buyur.
        /// </summary>
        bool NoktaYerlestir(Vector3 merkez, Vector3 right, out Vector3 pos)
        {
            pos = merkez;
            float[] carpanlar = { 1f, 0.8f, 0.6f, 0.4f };

            for (int k = 0; k < carpanlar.Length; k++)
            {
                Vector3 aday = merkez + right * (offset * carpanlar[k]);

                if (!zemineOturt)
                {
                    pos = aday;
                    return true;
                }

                float zeminY;
                Vector3 normal;
                if (!YolHatti.ZeminOrnekle(aday, groundMask, out zeminY, out normal)) continue;
                if (Mathf.Abs(zeminY - merkez.y) > maxGroundDrop) continue;

                aday.y = zeminY - sinkDepth;
                pos = aday;
                return true;
            }
            return false;
        }

        // ==================================================================
        //  Dizme
        // ==================================================================
        void PrefabHazirla()
        {
            GameObject fbx = poleSource;
            if (fbx == null)
            {
                Durum("Once direk FBX'ini sec.", MessageType.Error);
                return;
            }

            GameObject prefab = FoggyRoad_PowerLineAssets.DirekPrefabi(
                fbx, new Vector3(-1.20f, 16.91f, 0f), new Vector3(1.20f, 16.91f, 0f));

            if (prefab != null)
            {
                poleSource = prefab;
                Durum("Prefab hazir: " + FoggyRoad_PowerLineAssets.DirekPrefabYolu +
                      "\nAnchor_L / Anchor_R noktalarini Scene view'da surukleyerek ince ayar yapabilirsin.",
                      MessageType.Info);
            }
            else Durum("Prefab olusturulamadi.", MessageType.Error);
        }

        void Diz()
        {
            if (poleSource == null) { Durum("Direk prefab/FBX secilmemis.", MessageType.Error); return; }

            GameObject eski = MevcutKok();
            if (eski != null)
            {
                bool devam = EditorUtility.DisplayDialog(
                    "Mevcut direkler silinecek",
                    "'" + RootName + "' zaten var ve " + (eski.transform.childCount) +
                    " obje iceriyor.\n\nElle yaptigin duzenlemeler kaybolacak. Devam edilsin mi?",
                    "Sil ve yeniden diz", "Vazgec");
                if (!devam) return;

                Undo.DestroyObjectImmediate(eski);
            }

            List<Yerlesim> yerlesim = YerlesimHesapla(true);
            if (yerlesim.Count < 2)
            {
                Durum("Yeterli yerlesim noktasi hesaplanamadi.", MessageType.Error);
                return;
            }

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Elektrik direklerini diz");

            List<Transform> direkler = new List<Transform>(yerlesim.Count);

            try
            {
                for (int i = 0; i < yerlesim.Count; i++)
                {
                    if (i % 16 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Elektrik Direkleri", "Diziliyor...",
                                                         (float)i / yerlesim.Count);
                    }

                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(poleSource, root.transform);
                    if (go == null) go = Instantiate(poleSource, root.transform);

                    go.name = "Direk_" + i.ToString("000");

                    // Prefab'in KENDI durusu korunur, uzerine sadece yaw eklenir.
                    // Blender cikisli FBX'lerde model prefab'inin kokunde -90 X
                    // rotasyonu olabiliyor; tam rotasyon yazmak diregi yatiriyordu.
                    Quaternion varsayilan = go.transform.rotation;
                    go.transform.SetPositionAndRotation(
                        yerlesim[i].pos,
                        Quaternion.Euler(0f, yerlesim[i].yaw + yonDuzeltme, 0f) * varsayilan);

                    Undo.RegisterCreatedObjectUndo(go, "Elektrik direklerini diz");
                    direkler.Add(go.transform);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            // Kablo objesi
            GameObject kablo = new GameObject(KabloName);
            kablo.transform.SetParent(root.transform, false);
            Undo.RegisterCreatedObjectUndo(kablo, "Elektrik direklerini diz");

            FoggyRoad_PowerLine pl = Undo.AddComponent<FoggyRoad_PowerLine>(kablo);
            pl.poles = direkler;
            pl.spine = new List<Vector3>(hat);
            pl.wireCount = wireCount;
            pl.sagRatio = sagRatio;
            pl.wireRadius = wireRadius;
            pl.wireMaterial = FoggyRoad_PowerLineAssets.KabloMateryali();
            pl.Rebuild();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            FoggyRoad_PowerLineWatcher.Refresh();

            Durum(direkler.Count + " direk dizildi, " + ((direkler.Count - 1) * wireCount) +
                  " kablo parcasi cekildi.\nHepsi '" + RootName + "' altinda. " +
                  "Direkleri elle tasiyabilirsin, kablo kendini yeniler.\n" +
                  "Ctrl+Z ile geri alinabilir.", MessageType.Info);

            Debug.Log($"[FoggyRoad] Elektrik direkleri: {direkler.Count} adet, aralik {spacing} m, " +
                      $"offset {offset} m, {wireCount} kablo, sarkma orani {sagRatio}.");

            OnizlemeHesapla();
            SceneView.RepaintAll();
        }

        /// <summary>Dizilen her seyi siler (direkler + kablo).</summary>
        void Kaldir()
        {
            GameObject kok = MevcutKok();
            if (kok == null)
            {
                Durum("Silinecek '" + RootName + "' bulunamadi.", MessageType.Warning);
                return;
            }

            int adet = 0;
            for (int i = 0; i < kok.transform.childCount; i++)
                if (kok.transform.GetChild(i).name != KabloName) adet++;

            bool devam = EditorUtility.DisplayDialog(
                "Direkler silinecek",
                adet + " direk ve kablolari silinecek.\n\nEmin misin?",
                "Sil", "Vazgec");
            if (!devam) return;

            Undo.DestroyObjectImmediate(kok);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            FoggyRoad_PowerLineWatcher.Refresh();

            Durum(adet + " direk silindi. Ctrl+Z ile geri alinabilir.", MessageType.Info);
            Debug.Log($"[FoggyRoad] Elektrik direkleri kaldirildi ({adet} adet).");
        }

        /// <summary>
        /// Kullanicinin elle koydugu/kopyaladigi direkleri kablo hattina katar.
        /// Hierarchy'de direkleri sec, butona bas.
        /// </summary>
        void SeciliDirekleriEkle()
        {
            Transform[] secili = Selection.transforms;
            if (secili.Length == 0)
            {
                Durum("Once Hierarchy'de eklemek istedigin direkleri sec.", MessageType.Warning);
                return;
            }

            FoggyRoad_PowerLine[] hatlar = Object.FindObjectsByType<FoggyRoad_PowerLine>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (hatlar.Length == 0)
            {
                Durum("Sahnede kablo hatti yok. Once 'DIREKLERI DIZ'e bas.", MessageType.Error);
                return;
            }

            FoggyRoad_PowerLine hedef = hatlar[0];
            int eklenen = 0;

            Undo.RecordObject(hedef, "Direkleri hatta ekle");
            for (int i = 0; i < secili.Length; i++)
            {
                Transform t = secili[i];
                if (t == null || t == hedef.transform) continue;
                if (t.GetComponent<FoggyRoad_PowerLine>() != null) continue;
                if (hedef.poles.Contains(t)) continue;

                hedef.poles.Add(t);
                eklenen++;
            }

            hedef.ResortPoles();
            hedef.Rebuild();
            EditorUtility.SetDirty(hedef);

            Durum(eklenen + " direk hatta eklendi, toplam " + hedef.poles.Count + ".",
                  MessageType.Info);
            Debug.Log($"[FoggyRoad] {eklenen} direk kablo hattina eklendi.");
        }

        static GameObject MevcutKok()
        {
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name == RootName && all[i].parent == null)
                    return all[i].gameObject;
            return null;
        }

        // ==================================================================
        //  Scene view
        // ==================================================================
        void OnSceneGUI(SceneView sv)
        {
            // Elle cizim
            if (elleCizimAktif)
            {
                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && e.control)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 5000f, groundMask, QueryTriggerInteraction.Ignore))
                    {
                        elleNoktalar.Add(hit.point);
                        e.Use();
                        Repaint();
                    }
                }

                if (e.type == EventType.Layout)
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                Handles.color = Color.cyan;
                for (int i = 0; i < elleNoktalar.Count; i++)
                {
                    Handles.SphereHandleCap(0, elleNoktalar[i], Quaternion.identity,
                                            HandleUtility.GetHandleSize(elleNoktalar[i]) * 0.15f,
                                            EventType.Repaint);
                }
                if (elleNoktalar.Count > 1)
                    Handles.DrawAAPolyLine(2f, elleNoktalar.ToArray());
            }

            // Bulunan hat
            if (hat.Count > 1)
            {
                Handles.color = new Color(0.2f, 1f, 0.35f, 0.9f);
                Handles.DrawAAPolyLine(4f, hat.ToArray());
            }

            // Planlanan direk yerleri
            if (onizleme.Count > 0)
            {
                for (int i = 0; i < onizleme.Count; i++)
                {
                    Vector3 p = onizleme[i].pos;
                    Vector3 tepe = p + Vector3.up * 16.9f;

                    Handles.color = new Color(1f, 0.85f, 0.1f, 0.95f);
                    Handles.SphereHandleCap(0, p, Quaternion.identity,
                                            HandleUtility.GetHandleSize(p) * 0.12f, EventType.Repaint);
                    Handles.DrawAAPolyLine(2f, p, tepe);

                    // Traverse yonu: kablolarin gececegi eksene dik olmali
                    Quaternion q = Quaternion.Euler(0f, onizleme[i].yaw + yonDuzeltme, 0f);
                    Vector3 kol = q * Vector3.right * 1.2f;
                    Handles.color = new Color(0.3f, 0.9f, 1f, 0.95f);
                    Handles.DrawAAPolyLine(3f, tepe - kol, tepe + kol);
                }

                // Kablo hattinin tahmini gidisati
                if (onizleme.Count > 1)
                {
                    Handles.color = new Color(1f, 1f, 1f, 0.35f);
                    Vector3[] tepeler = new Vector3[onizleme.Count];
                    for (int i = 0; i < onizleme.Count; i++)
                        tepeler[i] = onizleme[i].pos + Vector3.up * 16.9f;
                    Handles.DrawAAPolyLine(1.5f, tepeler);
                }
            }
        }
    }
}
#endif
