// FoggyRoad_GuardrailAutoPlacer.cs
// Unity 6+ Editor tool
// Menu: Tools > Foggy Road > Guardrail Auto Placer (Yola Otomatik Diz)
//
// Yontem (v2 - centerline tabanli):
//   1) Yolun ust yuzeyi XZ duzleminde seyrek bir izgaraya (grid) rasterize edilir.
//   2) Izgarada yolun iki ucu bulunur, aralarinda yolun ORTASINDAN gecen en ucuz yol
//      (Dijkstra + merkez egilimi) hesaplanir. Bu, yolun merkez hattidir.
//   3) Merkez hatti yumusatilir ve sabit araliklarla yeniden orneklenir.
//   4) Her ornek noktasinda saga/sola dik yonde tarama yapilarak asfalt kenari olculur,
//      olculen genislikler de yumusatilir.
//   5) Korkuluk modulleri bu kenar hatlari boyunca uc uca dizilir.
//
// Bu yaklasim mesh'teki kopuk vertexlerden, ayri parcalardan ve duzensiz tesselasyondan
// etkilenmez; sonuc her zaman yol boyunca duzgun ve surekli olur.

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FoggyRoadTools.GuardrailPlacer
{
    public class GuardrailAutoPlacer : EditorWindow
    {
        private const int MaxInstances = 8000;
        private const string RootName = "Guardrails_Auto";

        private enum SideMode { Iki_Taraf = 0, Sadece_Sol = 1, Sadece_Sag = 2 }

        // ---- Hedefler ----
        [SerializeField] private GameObject roadObject;
        [SerializeField] private GameObject legacyRoot;
        [SerializeField] private GameObject moduleLeft;
        [SerializeField] private GameObject moduleRight;

        // ---- Yerlesim ayarlari ----
        [SerializeField] private SideMode sideMode = SideMode.Iki_Taraf;
        [SerializeField] private float outsideOffset = 0.30f;
        [SerializeField] private float heightOffset = -0.10f;
        [SerializeField] private float spacingOverride = 0f;
        [SerializeField] private float spacingScale = 1.0f;
        [SerializeField] private bool followSlope = true;
        [SerializeField] private bool anchorBottom = true;
        [SerializeField] private bool swapSides = false;
        [SerializeField] private bool flipLeft = false;
        [SerializeField] private bool flipRight = false;
        [SerializeField] private bool deactivateLegacy = true;
        [SerializeField] private bool markStatic = false;

        // ---- Analiz ayarlari ----
        [SerializeField] private float topNormalThreshold = 0.35f;
        [SerializeField] private float cellSize = 0.25f;
        [SerializeField] private int centerSmoothPasses = 6;
        [SerializeField] private int widthSmoothWindow = 15;
        [SerializeField] private float centerBias = 6f;
        [SerializeField] private float sampleStep = 1.0f;
        [SerializeField] private float maxHalfWidth = 40f;
        [SerializeField] private bool showAdvanced = false;

        // ---- Analiz sonucu ----
        private struct Cell
        {
            public float ySum;
            public int n;
        }

        private Dictionary<Vector2Int, Cell> grid;
        private List<Vector3> centerLine = new List<Vector3>();
        private List<Vector3> previewLeft = new List<Vector3>();
        private List<Vector3> previewRight = new List<Vector3>();
        private float[] leftDist;
        private float[] rightDist;
        private float roadLength;
        private float roadWidth;
        private bool analyzed;
        private bool meshNotReadable;

        private string status = "1) Yol objesini ve korkuluk modullerini sec, sonra 'YOLU ANALIZ ET'e bas.";
        private MessageType statusType = MessageType.Info;
        private Vector2 scroll;

        [MenuItem("Tools/Foggy Road/Guardrail Auto Placer (Yola Otomatik Diz)")]
        public static void OpenWindow()
        {
            GuardrailAutoPlacer w = GetWindow<GuardrailAutoPlacer>(false, "Guardrail Auto Placer", true);
            w.minSize = new Vector2(430, 560);
            w.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // ==================================================================
        //  UI
        // ==================================================================
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("GUARDRAIL AUTO PLACER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Yolun merkez hattini cikarir, kenarlarini olcer ve korkuluk modullerini " +
                "yol boyunca duzgun sekilde dizer.", MessageType.None);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("1. HEDEFLER", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            roadObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Yol Objesi", "Ornek: yol2"), roadObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                analyzed = false;
                meshNotReadable = false;
            }

            legacyRoot = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Eski Guardrails Objesi", "Blender'dan gelen duz dizilim. Moduller buradan alinir."),
                legacyRoot, typeof(GameObject), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sahneden Otomatik Bul", GUILayout.Height(22)))
                    AutoFindTargets();
                if (GUILayout.Button("Modulleri Otomatik Sec", GUILayout.Height(22)))
                    AutoPickModules();
            }

            moduleLeft = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Sol Modul (kaynak)", "Ornek: GRPRO_L_000_0000m"),
                moduleLeft, typeof(GameObject), true);
            moduleRight = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Sag Modul (kaynak)", "Ornek: GRPRO_R_000_0000m. Bos birakilirsa sol modul kullanilir."),
                moduleRight, typeof(GameObject), true);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("2. YERLESIM", EditorStyles.boldLabel);

            sideMode = (SideMode)EditorGUILayout.EnumPopup("Taraf", sideMode);
            outsideOffset = EditorGUILayout.FloatField(
                new GUIContent("Kenardan Disa Ofset (m)", "Korkuluk asfalt kenarindan ne kadar disarida dursun."),
                outsideOffset);
            heightOffset = EditorGUILayout.FloatField(
                new GUIContent("Yukseklik Ofseti (m)", "Eksi deger direkleri zemine gomer."), heightOffset);
            spacingScale = EditorGUILayout.Slider(
                new GUIContent("Modul Araligi Carpani", "1 = uc uca."), spacingScale, 0.80f, 1.20f);
            followSlope = EditorGUILayout.ToggleLeft("Yol egimini takip et (yokus/inis)", followSlope);
            anchorBottom = EditorGUILayout.ToggleLeft("Modulun ALT ucunu yola oturt", anchorBottom);
            swapSides = EditorGUILayout.ToggleLeft("Sol/Sag modulleri yer degistir", swapSides);
            flipLeft = EditorGUILayout.ToggleLeft("Sol modulu 180 derece cevir", flipLeft);
            flipRight = EditorGUILayout.ToggleLeft("Sag modulu 180 derece cevir", flipRight);
            deactivateLegacy = EditorGUILayout.ToggleLeft("Islem sonunda eski Guardrails objesini gizle", deactivateLegacy);
            markStatic = EditorGUILayout.ToggleLeft("Olusan modulleri Static isaretle", markStatic);

            EditorGUILayout.Space(6);
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Gelismis (yol analizi)", true);
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                topNormalThreshold = EditorGUILayout.Slider(
                    new GUIContent("Ust Yuzey Esigi", "Ucgen normal.y bu degerin ustundeyse yol yuzeyi sayilir."),
                    topNormalThreshold, 0.05f, 0.95f);
                cellSize = EditorGUILayout.Slider(
                    new GUIContent("Izgara Hucresi (m)", "Kucuk deger daha hassas kenar, daha yavas analiz."),
                    cellSize, 0.10f, 2.0f);
                centerSmoothPasses = EditorGUILayout.IntSlider(
                    new GUIContent("Merkez Yumusatma", "Merkez hattinin kac kez yumusatilacagi."),
                    centerSmoothPasses, 0, 30);
                widthSmoothWindow = EditorGUILayout.IntSlider(
                    new GUIContent("Genislik Yumusatma", "Olculen yol genisligi kac ornekte ortalanacak."),
                    widthSmoothWindow, 1, 61);
                centerBias = EditorGUILayout.Slider(
                    new GUIContent("Merkezde Kalma Egilimi", "Yuksek deger hattin yol ortasinda kalmasini zorlar."),
                    centerBias, 0f, 20f);
                sampleStep = EditorGUILayout.Slider(
                    new GUIContent("Ornekleme Araligi (m)", "Merkez hattinin nokta sikligi."), sampleStep, 0.25f, 5f);
                maxHalfWidth = EditorGUILayout.Slider(
                    new GUIContent("Maks Yari Genislik (m)", "Kenar aramasinin duracagi mesafe."), maxHalfWidth, 3f, 100f);
                spacingOverride = EditorGUILayout.FloatField(
                    new GUIContent("Modul Boyu Override (m)", "0 = modulun kendi boyu olculur."), spacingOverride);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("3. CALISTIR", EditorStyles.boldLabel);

            if (GUILayout.Button("YOLU ANALIZ ET", GUILayout.Height(30)))
                Analyze();

            using (new EditorGUI.DisabledScope(!analyzed))
            {
                if (GUILayout.Button("KORKULUKLARI DIZ", GUILayout.Height(34)))
                    PlaceAll();
            }

            if (meshNotReadable)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Yol mesh'i okunabilir degil. Asagidaki butona bas, Unity FBX'i yeniden import edecek.",
                    MessageType.Warning);
                if (GUILayout.Button("Mesh'i Okunabilir Yap (Read/Write Enabled)", GUILayout.Height(24)))
                    MakeRoadMeshReadable();
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Olusturulan '" + RootName + "' objesini sil"))
                DeleteGenerated();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(status, statusType);

            EditorGUILayout.EndScrollView();
        }

        // ==================================================================
        //  Otomatik hedef bulma
        // ==================================================================
        private void AutoFindTargets()
        {
            // Onceki calistirmada uretilmis objeler secilmis olabilir; onlari kaynak sayma
            if (legacyRoot != null && IsUnderGenerated(legacyRoot.transform)) legacyRoot = null;
            if (moduleLeft != null && IsUnderGenerated(moduleLeft.transform)) moduleLeft = null;
            if (moduleRight != null && IsUnderGenerated(moduleRight.transform)) moduleRight = null;

            if (roadObject == null)
            {
                GameObject road = FindSceneObject("yol2");
                if (road == null) road = FindSceneObject("yol");
                roadObject = road;
            }

            if (legacyRoot == null)
            {
                GameObject g = FindSceneObject("Guardrails");
                if (g == null)
                {
                    Transform[] all = Object.FindObjectsByType<Transform>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (IsUnderGenerated(all[i])) continue;
                        if (all[i].name.StartsWith("GRPRO_", System.StringComparison.OrdinalIgnoreCase))
                        {
                            g = all[i].parent != null ? all[i].parent.gameObject : all[i].gameObject;
                            break;
                        }
                    }
                }
                legacyRoot = g;
            }

            AutoPickModules();

            SetStatus(
                "Bulunan -> Yol: " + (roadObject ? roadObject.name : "YOK") +
                " | Guardrails: " + (legacyRoot ? legacyRoot.name : "YOK") +
                " | Sol modul: " + (moduleLeft ? moduleLeft.name : "YOK") +
                " | Sag modul: " + (moduleRight ? moduleRight.name : "YOK"),
                (roadObject != null && moduleLeft != null) ? MessageType.Info : MessageType.Warning);
        }

        private void AutoPickModules()
        {
            if (legacyRoot == null) return;

            Transform[] kids = legacyRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < kids.Length; i++)
            {
                Transform t = kids[i];
                if (t == legacyRoot.transform) continue;
                if (t.GetComponentInChildren<MeshFilter>(true) == null) continue;

                string n = t.name.ToUpperInvariant();
                if (moduleLeft == null && n.Contains("_L_")) moduleLeft = t.gameObject;
                if (moduleRight == null && n.Contains("_R_")) moduleRight = t.gameObject;
                if (moduleLeft != null && moduleRight != null) break;
            }

            if (moduleLeft == null && moduleRight == null)
            {
                for (int i = 0; i < kids.Length; i++)
                {
                    if (kids[i] == legacyRoot.transform) continue;
                    if (kids[i].GetComponentInChildren<MeshFilter>(true) == null) continue;
                    moduleLeft = kids[i].gameObject;
                    break;
                }
            }
            Repaint();
        }

        // ==================================================================
        //  Analiz
        // ==================================================================
        private void Analyze()
        {
            analyzed = false;
            meshNotReadable = false;
            centerLine.Clear();
            previewLeft.Clear();
            previewRight.Clear();
            leftDist = null;
            rightDist = null;

            if (roadObject == null)
            {
                SetStatus("Yol objesi secilmedi.", MessageType.Error);
                return;
            }

            Vector3[] verts;
            int[] tris;
            string err;
            if (!CollectRoadGeometry(out verts, out tris, out err))
            {
                SetStatus(err, MessageType.Error);
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Guardrail Auto Placer", "Yol yuzeyi taraniyor...", 0.15f);
                grid = BuildGrid(verts, tris);

                if (grid.Count < 16)
                {
                    SetStatus("Yol yuzeyi bulunamadi (" + grid.Count + " hucre). " +
                              "'Gelismis' bolumunden Ust Yuzey Esigi'ni dusur.", MessageType.Error);
                    return;
                }

                EditorUtility.DisplayProgressBar("Guardrail Auto Placer", "Merkez hatti hesaplaniyor...", 0.45f);
                List<Vector2Int> path;
                if (!FindCenterPath(out path, out err))
                {
                    SetStatus(err, MessageType.Error);
                    return;
                }

                EditorUtility.DisplayProgressBar("Guardrail Auto Placer", "Hat yumusatiliyor...", 0.75f);
                List<Vector3> raw = new List<Vector3>(path.Count);
                for (int i = 0; i < path.Count; i++) raw.Add(CellToWorld(path[i]));

                raw = Resample(raw, sampleStep);
                raw = Smooth(raw, centerSmoothPasses);
                raw = Resample(raw, sampleStep);
                raw = Smooth(raw, Mathf.Max(1, centerSmoothPasses / 2));
                centerLine = raw;

                if (centerLine.Count < 4)
                {
                    SetStatus("Merkez hatti cok kisa cikti.", MessageType.Error);
                    return;
                }

                EditorUtility.DisplayProgressBar("Guardrail Auto Placer", "Yol genisligi olculuyor...", 0.9f);
                MeasureWidths();

                roadLength = PolylineLength(centerLine);
                float sum = 0f;
                for (int i = 0; i < leftDist.Length; i++) sum += leftDist[i] + rightDist[i];
                roadWidth = sum / Mathf.Max(1, leftDist.Length);

                BuildPreview();

                float step = spacingOverride > 0.01f ? spacingOverride : EstimateModuleLength();
                int estimate = step > 0.01f ? Mathf.Max(1, Mathf.RoundToInt(roadLength / (step * spacingScale))) : 0;

                analyzed = true;
                SetStatus(
                    "Analiz tamam.\n" +
                    "Yol uzunlugu: " + roadLength.ToString("F1") + " m\n" +
                    "Ortalama genislik: " + roadWidth.ToString("F2") + " m\n" +
                    "Izgara hucresi: " + grid.Count + "\n" +
                    "Merkez hatti noktasi: " + centerLine.Count + "\n" +
                    "Modul boyu: " + (step > 0.01f ? step.ToString("F2") + " m" : "olculemedi (modul sec)") + "\n" +
                    "Tahmini modul sayisi (her taraf): " + estimate + "\n" +
                    "Scene'de sari = merkez, yesil = sol kenar, kirmizi = sag kenar.",
                    MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                SceneView.RepaintAll();
                Repaint();
            }
        }

        private bool CollectRoadGeometry(out Vector3[] verts, out int[] tris, out string err)
        {
            verts = null;
            tris = null;
            err = null;

            MeshFilter[] filters = roadObject.GetComponentsInChildren<MeshFilter>(true);
            List<Vector3> vertList = new List<Vector3>();
            List<int> triList = new List<int>();
            int used = 0;

            for (int f = 0; f < filters.Length; f++)
            {
                Mesh mesh = filters[f].sharedMesh;
                if (mesh == null) continue;

                if (!mesh.isReadable)
                {
                    meshNotReadable = true;
                    err = "'" + mesh.name + "' mesh'i okunabilir degil (Read/Write Enabled kapali).";
                    return false;
                }

                Matrix4x4 m = filters[f].transform.localToWorldMatrix;
                Vector3[] mv = mesh.vertices;
                int baseIndex = vertList.Count;
                for (int i = 0; i < mv.Length; i++)
                    vertList.Add(m.MultiplyPoint3x4(mv[i]));

                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    int[] idx = mesh.GetTriangles(s);
                    for (int i = 0; i < idx.Length; i++)
                        triList.Add(idx[i] + baseIndex);
                }
                used++;
            }

            if (used == 0)
            {
                err = "Yol objesinde MeshFilter bulunamadi.";
                return false;
            }

            verts = vertList.ToArray();
            tris = triList.ToArray();
            return true;
        }

        private void MakeRoadMeshReadable()
        {
            if (roadObject == null) return;

            MeshFilter[] filters = roadObject.GetComponentsInChildren<MeshFilter>(true);
            HashSet<string> done = new HashSet<string>();
            int changed = 0;

            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null || mesh.isReadable) continue;

                string path = AssetDatabase.GetAssetPath(mesh);
                if (string.IsNullOrEmpty(path) || done.Contains(path)) continue;

                ModelImporter imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) continue;

                imp.isReadable = true;
                imp.SaveAndReimport();
                done.Add(path);
                changed++;
            }

            meshNotReadable = false;
            SetStatus(changed > 0
                ? changed + " mesh okunabilir yapildi. Simdi tekrar 'YOLU ANALIZ ET'e bas."
                : "Degistirilecek mesh bulunamadi.", MessageType.Info);
        }

        // ==================================================================
        //  Izgara (occupancy grid)
        // ==================================================================
        private Dictionary<Vector2Int, Cell> BuildGrid(Vector3[] verts, int[] tris)
        {
            Dictionary<Vector2Int, Cell> g = new Dictionary<Vector2Int, Cell>(1 << 14);
            float cs = Mathf.Max(0.05f, cellSize);
            float invCs = 1f / cs;

            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                Vector3 p0 = verts[tris[t]];
                Vector3 p1 = verts[tris[t + 1]];
                Vector3 p2 = verts[tris[t + 2]];

                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
                float len = n.magnitude;
                if (len < 1e-9f) continue;
                if (Mathf.Abs(n.y / len) < topNormalThreshold) continue;

                // Kose vertexlerinin dustugu hucreler
                AddToCell(g, p0, invCs);
                AddToCell(g, p1, invCs);
                AddToCell(g, p2, invCs);

                // Ucgenin kapladigi hucreler
                float minX = Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x));
                float maxX = Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x));
                float minZ = Mathf.Min(p0.z, Mathf.Min(p1.z, p2.z));
                float maxZ = Mathf.Max(p0.z, Mathf.Max(p1.z, p2.z));

                int x0 = Mathf.FloorToInt(minX * invCs);
                int x1 = Mathf.FloorToInt(maxX * invCs);
                int z0 = Mathf.FloorToInt(minZ * invCs);
                int z1 = Mathf.FloorToInt(maxZ * invCs);

                long area = (long)(x1 - x0 + 1) * (z1 - z0 + 1);
                if (area <= 1 || area > 65536) continue;

                Vector2 a = new Vector2(p0.x, p0.z);
                Vector2 b = new Vector2(p1.x, p1.z);
                Vector2 c = new Vector2(p2.x, p2.z);

                for (int xi = x0; xi <= x1; xi++)
                {
                    for (int zi = z0; zi <= z1; zi++)
                    {
                        Vector2 p = new Vector2((xi + 0.5f) * cs, (zi + 0.5f) * cs);
                        float u, v, w;
                        if (!Barycentric(p, a, b, c, out u, out v, out w)) continue;

                        float y = p0.y * u + p1.y * v + p2.y * w;
                        AddToCellIndex(g, new Vector2Int(xi, zi), y);
                    }
                }
            }
            return g;
        }

        private static void AddToCell(Dictionary<Vector2Int, Cell> g, Vector3 p, float invCs)
        {
            Vector2Int key = new Vector2Int(Mathf.FloorToInt(p.x * invCs), Mathf.FloorToInt(p.z * invCs));
            AddToCellIndex(g, key, p.y);
        }

        private static void AddToCellIndex(Dictionary<Vector2Int, Cell> g, Vector2Int key, float y)
        {
            Cell c;
            if (g.TryGetValue(key, out c))
            {
                c.ySum += y;
                c.n++;
                g[key] = c;
            }
            else
            {
                c.ySum = y;
                c.n = 1;
                g.Add(key, c);
            }
        }

        private static bool Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c,
                                        out float u, out float v, out float w)
        {
            u = v = w = 0f;
            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = p - a;

            float den = v0.x * v1.y - v1.x * v0.y;
            if (Mathf.Abs(den) < 1e-12f) return false;

            v = (v2.x * v1.y - v1.x * v2.y) / den;
            w = (v0.x * v2.y - v2.x * v0.y) / den;
            u = 1f - v - w;

            return u >= -0.0001f && v >= -0.0001f && w >= -0.0001f;
        }

        private Vector3 CellToWorld(Vector2Int c)
        {
            float cs = Mathf.Max(0.05f, cellSize);
            Cell cell;
            float y = grid.TryGetValue(c, out cell) && cell.n > 0 ? cell.ySum / cell.n : 0f;
            return new Vector3((c.x + 0.5f) * cs, y, (c.y + 0.5f) * cs);
        }

        private Vector2Int WorldToCell(Vector3 p)
        {
            float invCs = 1f / Mathf.Max(0.05f, cellSize);
            return new Vector2Int(Mathf.FloorToInt(p.x * invCs), Mathf.FloorToInt(p.z * invCs));
        }

        private bool IsFilled(Vector3 p)
        {
            return grid.ContainsKey(WorldToCell(p));
        }

        private bool TrySampleHeight(Vector3 p, out float y)
        {
            Cell c;
            if (grid.TryGetValue(WorldToCell(p), out c) && c.n > 0)
            {
                y = c.ySum / c.n;
                return true;
            }
            y = 0f;
            return false;
        }

        // ==================================================================
        //  Merkez hatti
        // ==================================================================
        private static readonly Vector2Int[] Neighbors8 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        private static readonly Vector2Int[] Neighbors4 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        private bool FindCenterPath(out List<Vector2Int> path, out string err)
        {
            path = null;
            err = null;

            // 1) Kenardan uzaklik (multi-source BFS)
            Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>(grid.Count);
            Queue<Vector2Int> q = new Queue<Vector2Int>();

            foreach (KeyValuePair<Vector2Int, Cell> kv in grid)
            {
                bool edge = false;
                for (int i = 0; i < Neighbors4.Length; i++)
                {
                    if (!grid.ContainsKey(kv.Key + Neighbors4[i])) { edge = true; break; }
                }
                if (edge)
                {
                    dist[kv.Key] = 0;
                    q.Enqueue(kv.Key);
                }
            }

            if (q.Count == 0)
            {
                err = "Yol yuzeyinin kenari bulunamadi.";
                return false;
            }

            int maxDist = 0;
            while (q.Count > 0)
            {
                Vector2Int cur = q.Dequeue();
                int d = dist[cur];
                if (d > maxDist) maxDist = d;

                for (int i = 0; i < Neighbors4.Length; i++)
                {
                    Vector2Int nb = cur + Neighbors4[i];
                    if (!grid.ContainsKey(nb) || dist.ContainsKey(nb)) continue;
                    dist[nb] = d + 1;
                    q.Enqueue(nb);
                }
            }

            // 2) Yolun iki ucu: cift BFS taramasi (grafik capi)
            Vector2Int seed = default(Vector2Int);
            foreach (KeyValuePair<Vector2Int, Cell> kv in grid) { seed = kv.Key; break; }

            Vector2Int endA = FarthestCell(seed);
            Vector2Int endB = FarthestCell(endA);

            // 3) Merkezde kalmayi tercih eden Dijkstra
            if (!Dijkstra(endA, endB, dist, Mathf.Max(1, maxDist), out path))
            {
                err = "Yolun iki ucu arasinda surekli bir hat bulunamadi. " +
                      "Yol mesh'i parcali olabilir; Izgara Hucresi degerini buyutmeyi dene.";
                return false;
            }

            if (path.Count < 4)
            {
                err = "Bulunan merkez hatti cok kisa.";
                return false;
            }
            return true;
        }

        private Vector2Int FarthestCell(Vector2Int start)
        {
            Dictionary<Vector2Int, int> seen = new Dictionary<Vector2Int, int>(grid.Count);
            Queue<Vector2Int> q = new Queue<Vector2Int>();
            seen[start] = 0;
            q.Enqueue(start);

            Vector2Int best = start;
            int bestD = 0;

            while (q.Count > 0)
            {
                Vector2Int cur = q.Dequeue();
                int d = seen[cur];
                if (d > bestD) { bestD = d; best = cur; }

                for (int i = 0; i < Neighbors8.Length; i++)
                {
                    Vector2Int nb = cur + Neighbors8[i];
                    if (!grid.ContainsKey(nb) || seen.ContainsKey(nb)) continue;
                    seen[nb] = d + 1;
                    q.Enqueue(nb);
                }
            }
            return best;
        }

        private bool Dijkstra(Vector2Int from, Vector2Int to, Dictionary<Vector2Int, int> edgeDist,
                              int maxDist, out List<Vector2Int> path)
        {
            path = null;

            Dictionary<Vector2Int, float> best = new Dictionary<Vector2Int, float>(grid.Count);
            Dictionary<Vector2Int, Vector2Int> prev = new Dictionary<Vector2Int, Vector2Int>(grid.Count);
            MinHeap heap = new MinHeap(grid.Count + 8);

            best[from] = 0f;
            heap.Push(0f, from);

            while (heap.Count > 0)
            {
                float curCost;
                Vector2Int cur;
                heap.Pop(out curCost, out cur);

                float known;
                if (best.TryGetValue(cur, out known) && curCost > known + 1e-6f) continue;
                if (cur == to) break;

                for (int i = 0; i < Neighbors8.Length; i++)
                {
                    Vector2Int nb = cur + Neighbors8[i];
                    if (!grid.ContainsKey(nb)) continue;

                    float stepLen = (Neighbors8[i].x != 0 && Neighbors8[i].y != 0) ? 1.41421f : 1f;

                    int d;
                    edgeDist.TryGetValue(nb, out d);
                    float centerPenalty = 1f + centerBias * (1f - (float)d / maxDist);

                    float cost = curCost + stepLen * centerPenalty;

                    float old;
                    if (best.TryGetValue(nb, out old) && old <= cost + 1e-6f) continue;

                    best[nb] = cost;
                    prev[nb] = cur;
                    heap.Push(cost, nb);
                }
            }

            if (!best.ContainsKey(to)) return false;

            List<Vector2Int> result = new List<Vector2Int>();
            Vector2Int walk = to;
            int guard = 0;
            while (guard++ <= grid.Count + 4)
            {
                result.Add(walk);
                if (walk == from) break;
                if (!prev.TryGetValue(walk, out walk)) return false;
            }
            result.Reverse();
            path = result;
            return true;
        }

        private class MinHeap
        {
            private struct Node
            {
                public float key;
                public Vector2Int value;
            }

            private Node[] items;
            private int count;

            public int Count { get { return count; } }

            public MinHeap(int capacity)
            {
                items = new Node[Mathf.Max(16, capacity)];
                count = 0;
            }

            public void Push(float key, Vector2Int value)
            {
                if (count == items.Length)
                {
                    Node[] bigger = new Node[items.Length * 2];
                    System.Array.Copy(items, bigger, items.Length);
                    items = bigger;
                }

                Node n;
                n.key = key;
                n.value = value;
                items[count] = n;

                int i = count++;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (items[parent].key <= items[i].key) break;
                    Node tmp = items[parent];
                    items[parent] = items[i];
                    items[i] = tmp;
                    i = parent;
                }
            }

            public void Pop(out float key, out Vector2Int value)
            {
                key = items[0].key;
                value = items[0].value;

                count--;
                items[0] = items[count];

                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1;
                    int r = l + 1;
                    int small = i;

                    if (l < count && items[l].key < items[small].key) small = l;
                    if (r < count && items[r].key < items[small].key) small = r;
                    if (small == i) break;

                    Node tmp = items[small];
                    items[small] = items[i];
                    items[i] = tmp;
                    i = small;
                }
            }
        }

        // ==================================================================
        //  Genislik olcumu
        // ==================================================================
        private void MeasureWidths()
        {
            int n = centerLine.Count;
            leftDist = new float[n];
            rightDist = new float[n];

            float step = Mathf.Max(0.05f, cellSize * 0.5f);

            for (int i = 0; i < n; i++)
            {
                Vector3 tangent = TangentAt(centerLine, i);
                Vector3 right = Vector3.Cross(Vector3.up, tangent);
                right.y = 0f;
                if (right.sqrMagnitude < 1e-8f)
                {
                    leftDist[i] = rightDist[i] = 0f;
                    continue;
                }
                right.Normalize();

                rightDist[i] = March(centerLine[i], right, step);
                leftDist[i] = March(centerLine[i], -right, step);
            }

            leftDist = SmoothArray(leftDist, widthSmoothWindow);
            rightDist = SmoothArray(rightDist, widthSmoothWindow);
        }

        private float March(Vector3 origin, Vector3 dir, float step)
        {
            float d = 0f;
            int misses = 0;

            while (d < maxHalfWidth)
            {
                float next = d + step;
                if (IsFilled(origin + dir * next))
                {
                    d = next;
                    misses = 0;
                }
                else
                {
                    // Kucuk delikleri atla, gercek kenarda dur
                    misses++;
                    if (misses > 2) break;
                    d = next;
                }
            }
            return Mathf.Max(0f, d - step * (misses > 0 ? misses : 0));
        }

        private static float[] SmoothArray(float[] src, int window)
        {
            if (window <= 1 || src.Length < 3) return src;

            int half = window / 2;
            float[] dst = new float[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                float sum = 0f;
                int cnt = 0;
                for (int k = -half; k <= half; k++)
                {
                    int j = i + k;
                    if (j < 0 || j >= src.Length) continue;
                    sum += src[j];
                    cnt++;
                }
                dst[i] = sum / Mathf.Max(1, cnt);
            }
            return dst;
        }

        private void BuildPreview()
        {
            previewLeft.Clear();
            previewRight.Clear();

            for (int i = 0; i < centerLine.Count; i++)
            {
                Vector3 l, r;
                if (TryEdgePoints(i, 0f, out l, out r))
                {
                    previewLeft.Add(l);
                    previewRight.Add(r);
                }
            }
        }

        private bool TryEdgePoints(int i, float extraOffset, out Vector3 left, out Vector3 right)
        {
            Vector3 c = centerLine[i];
            Vector3 tangent = TangentAt(centerLine, i);
            Vector3 rightDir = Vector3.Cross(Vector3.up, tangent);
            rightDir.y = 0f;

            left = right = c;
            if (rightDir.sqrMagnitude < 1e-8f) return false;
            rightDir.Normalize();

            right = c + rightDir * (rightDist[i] + extraOffset);
            left = c - rightDir * (leftDist[i] + extraOffset);

            float y;
            right.y = TrySampleHeight(right, out y) ? y : c.y;
            left.y = TrySampleHeight(left, out y) ? y : c.y;
            return true;
        }

        // ==================================================================
        //  Polyline yardimcilari
        // ==================================================================
        private static Vector3 TangentAt(List<Vector3> pts, int i)
        {
            int lo = Mathf.Max(0, i - 2);
            int hi = Mathf.Min(pts.Count - 1, i + 2);
            Vector3 d = pts[hi] - pts[lo];
            if (d.sqrMagnitude < 1e-10f) d = Vector3.forward;
            return d.normalized;
        }

        private static float PolylineLength(List<Vector3> pts)
        {
            float len = 0f;
            for (int i = 1; i < pts.Count; i++) len += Vector3.Distance(pts[i - 1], pts[i]);
            return len;
        }

        private static List<Vector3> Smooth(List<Vector3> pts, int passes)
        {
            if (passes <= 0 || pts.Count < 5) return pts;

            List<Vector3> cur = new List<Vector3>(pts);
            List<Vector3> next = new List<Vector3>(pts.Count);

            for (int p = 0; p < passes; p++)
            {
                next.Clear();
                next.Add(cur[0]);
                next.Add(cur[1]);

                for (int i = 2; i < cur.Count - 2; i++)
                {
                    Vector3 s = (cur[i - 2] + cur[i - 1] * 2f + cur[i] * 3f + cur[i + 1] * 2f + cur[i + 2]) / 9f;
                    next.Add(s);
                }

                next.Add(cur[cur.Count - 2]);
                next.Add(cur[cur.Count - 1]);

                List<Vector3> tmp = cur;
                cur = new List<Vector3>(next);
                tmp.Clear();
            }
            return cur;
        }

        private static List<Vector3> Resample(List<Vector3> pts, float step)
        {
            List<Vector3> outPts = new List<Vector3>();
            if (pts.Count < 2) return new List<Vector3>(pts);

            step = Mathf.Max(0.05f, step);

            float[] cum = new float[pts.Count];
            float acc = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                acc += Vector3.Distance(pts[i - 1], pts[i]);
                cum[i] = acc;
            }
            if (acc < step) return new List<Vector3>(pts);

            int count = Mathf.Max(2, Mathf.RoundToInt(acc / step) + 1);
            for (int i = 0; i < count; i++)
            {
                float s = acc * i / (count - 1);
                outPts.Add(SampleAt(pts, cum, s));
            }
            return outPts;
        }

        private static Vector3 SampleAt(List<Vector3> pts, float[] cum, float s)
        {
            float total = cum[cum.Length - 1];
            s = Mathf.Clamp(s, 0f, total);

            int lo = 0;
            int hi = cum.Length - 1;
            while (lo < hi - 1)
            {
                int mid = (lo + hi) / 2;
                if (cum[mid] <= s) lo = mid; else hi = mid;
            }

            float segLen = cum[hi] - cum[lo];
            float t = segLen > 1e-6f ? (s - cum[lo]) / segLen : 0f;
            return Vector3.Lerp(pts[lo], pts[hi], t);
        }

        // ==================================================================
        //  Yerlestirme
        // ==================================================================
        private void PlaceAll()
        {
            if (!analyzed || centerLine.Count < 4)
            {
                SetStatus("Once 'YOLU ANALIZ ET'e bas.", MessageType.Error);
                return;
            }

            GameObject srcL = swapSides ? moduleRight : moduleLeft;
            GameObject srcR = swapSides ? moduleLeft : moduleRight;
            if (srcL == null) srcL = srcR;
            if (srcR == null) srcR = srcL;

            if (srcL == null)
            {
                SetStatus("Kaynak korkuluk modulu secilmedi.", MessageType.Error);
                return;
            }

            DeleteGenerated();

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Guardrail Auto Place");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            int madeL = 0, madeR = 0;
            try
            {
                if (sideMode != SideMode.Sadece_Sag)
                    madeL = PlaceSide(false, srcL, root.transform, "L", flipLeft);

                if (sideMode != SideMode.Sadece_Sol)
                    madeR = PlaceSide(true, srcR, root.transform, "R", flipRight);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (deactivateLegacy && legacyRoot != null && !IsChildOf(legacyRoot.transform, root.transform))
            {
                Undo.RecordObject(legacyRoot, "Guardrail Auto Place");
                legacyRoot.SetActive(false);
                EditorUtility.SetDirty(legacyRoot);
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            SetStatus("Bitti. Sol: " + madeL + " modul, Sag: " + madeR + " modul dizildi.\n" +
                      "Hepsi '" + RootName + "' objesinin altinda. Ctrl+Z ile geri alinabilir.",
                      MessageType.Info);
        }

        private int PlaceSide(bool rightSide, GameObject module, Transform parent, string tag, bool flip180)
        {
            Bounds wb;
            if (!TryGetWorldBounds(module, out wb))
            {
                SetStatus("Kaynak modulde mesh bulunamadi: " + module.name, MessageType.Error);
                return 0;
            }

            // Modulun GERCEK ekseni ve tekrar araligi: komsu modul ile arasindaki vektor.
            // Bu, AABB tahmininden cok daha dogru - moduller boylece uc uca oturur.
            Vector3 moduleAxis;
            float moduleLength;
            if (!TryGetModuleAxisAndSpacing(module, out moduleAxis, out moduleLength))
            {
                bool alongX = wb.size.x >= wb.size.z;
                moduleLength = alongX ? wb.size.x : wb.size.z;
                moduleAxis = alongX ? Vector3.right : Vector3.forward;
            }

            if (spacingOverride > 0.01f) moduleLength = spacingOverride;
            if (moduleLength < 0.05f)
            {
                SetStatus("Modul boyu olculemedi. Modul Boyu Override degerini gir.", MessageType.Error);
                return 0;
            }

            if (flip180) moduleAxis = -moduleAxis;

            moduleAxis.y = 0f;
            if (moduleAxis.sqrMagnitude < 1e-8f)
            {
                SetStatus("Modul ekseni belirlenemedi: " + module.name, MessageType.Error);
                return 0;
            }
            moduleAxis.Normalize();

            float step = Mathf.Max(0.05f, moduleLength * spacingScale);

            Vector3 anchorWorld = new Vector3(
                wb.center.x,
                anchorBottom ? wb.min.y : wb.center.y,
                wb.center.z);
            Vector3 pivotToAnchor = anchorWorld - module.transform.position;

            // Ilerleme yonundeki bileseni sifirla: modulun pivotu hattin uzerine otursun,
            // boylece bir modulun bitisi digerinin baslangicina tam denk gelir.
            pivotToAnchor -= Vector3.Dot(pivotToAnchor, moduleAxis) * moduleAxis;

            // Kenar hattini olustur, sonra modul boyuna gore ornekle
            List<Vector3> edge = new List<Vector3>(centerLine.Count);
            for (int i = 0; i < centerLine.Count; i++)
            {
                Vector3 l, r;
                if (!TryEdgePoints(i, outsideOffset, out l, out r)) continue;
                edge.Add(rightSide ? r : l);
            }

            if (edge.Count < 4) return 0;
            edge = Smooth(edge, 3);
            SmoothHeights(edge, 9);

            float total = PolylineLength(edge);
            // CeilToInt: son parcada bosluk kalmasin (moduller cok az bindirir, hic acilmaz)
            int count = Mathf.Max(1, Mathf.CeilToInt(total / step));
            if (count > MaxInstances)
            {
                SetStatus("Guvenlik siniri: " + count + " modul cok fazla.", MessageType.Error);
                return 0;
            }

            float[] cum = new float[edge.Count];
            float acc = 0f;
            for (int i = 1; i < edge.Count; i++)
            {
                acc += Vector3.Distance(edge[i - 1], edge[i]);
                cum[i] = acc;
            }

            float actualStep = total / count;

            GameObject group = new GameObject(RootName + "_" + tag);
            group.transform.SetParent(parent, false);

            Vector3 srcScale = module.transform.lossyScale;
            Quaternion srcRot = module.transform.rotation;
            string baseName = module.name.Replace("(Clone)", "").Trim();
            int made = 0;

            for (int i = 0; i < count; i++)
            {
                if (i % 25 == 0)
                {
                    EditorUtility.DisplayProgressBar("Guardrail Auto Placer",
                        tag + " tarafi diziliyor... " + i + "/" + count, (float)i / count);
                }

                // Modul s0'da baslar, s1'de biter: bir sonraki modul tam oradan devam eder.
                float s0 = i * actualStep;
                float s1 = Mathf.Min(total, s0 + actualStep);

                Vector3 p = SampleAt(edge, cum, s0);
                Vector3 pEnd = SampleAt(edge, cum, s1);

                Vector3 dir = pEnd - p;
                if (dir.sqrMagnitude < 1e-8f) continue;
                dir.Normalize();

                Vector3 dirFlat = new Vector3(dir.x, 0f, dir.z);
                if (dirFlat.sqrMagnitude < 1e-8f) continue;
                dirFlat.Normalize();

                float yaw = Vector3.SignedAngle(moduleAxis, dirFlat, Vector3.up);
                Quaternion delta = Quaternion.AngleAxis(yaw, Vector3.up);

                if (followSlope)
                {
                    float pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
                    Vector3 pitchAxis = Vector3.Cross(Vector3.up, dirFlat);
                    if (pitchAxis.sqrMagnitude > 1e-8f)
                        delta = Quaternion.AngleAxis(-pitch, pitchAxis.normalized) * delta;
                }

                Vector3 target = p + Vector3.up * heightOffset;

                GameObject clone = InstantiateModule(module);
                if (clone == null) continue;

                clone.name = baseName + "_" + tag + "_" + i.ToString("D4");
                clone.transform.SetParent(group.transform, true);
                clone.transform.localScale = srcScale;
                clone.transform.rotation = delta * srcRot;
                clone.transform.position = target - delta * pivotToAnchor;
                clone.SetActive(true);

                if (markStatic)
                    GameObjectUtility.SetStaticEditorFlags(clone,
                        StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.ContributeGI);
                made++;
            }

            EditorUtility.ClearProgressBar();
            return made;
        }

        private static GameObject InstantiateModule(GameObject src)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(src))
                return (GameObject)PrefabUtility.InstantiatePrefab(src);
            return Object.Instantiate(src);
        }

        private static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
        {
            bounds = new Bounds();
            bool has = false;

            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
            for (int f = 0; f < filters.Length; f++)
            {
                Mesh mesh = filters[f].sharedMesh;
                if (mesh == null) continue;

                Matrix4x4 m = filters[f].transform.localToWorldMatrix;
                Bounds mb = mesh.bounds;
                Vector3 c = mb.center;
                Vector3 e = mb.extents;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new Vector3(
                        c.x + (((i & 1) == 0) ? -e.x : e.x),
                        c.y + (((i & 2) == 0) ? -e.y : e.y),
                        c.z + (((i & 4) == 0) ? -e.z : e.z));
                    Vector3 p = m.MultiplyPoint3x4(corner);

                    if (!has) { bounds = new Bounds(p, Vector3.zero); has = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return has;
        }

        private float EstimateModuleLength()
        {
            GameObject m = moduleLeft != null ? moduleLeft : moduleRight;
            if (m == null) return 0f;

            Vector3 axis;
            float spacing;
            if (TryGetModuleAxisAndSpacing(m, out axis, out spacing)) return spacing;

            Bounds b;
            if (!TryGetWorldBounds(m, out b)) return 0f;
            return Mathf.Max(b.size.x, b.size.z);
        }

        // Kaynak modulun komsusuna olan vektoru: gercek dizilim ekseni ve tekrar araligi.
        // Blender'dan gelen dizilimde moduller sabit aralikla dizilidir; bu vektor
        // hem modulun uzunluk yonunu hem de bosluksuz oturmasi gereken adimi verir.
        private static bool TryGetModuleAxisAndSpacing(GameObject module, out Vector3 axis, out float spacing)
        {
            axis = Vector3.right;
            spacing = 0f;

            Transform t = module.transform;
            Transform parent = t.parent;
            if (parent == null) return false;

            string side = SideTag(module.name);
            int myIndex = ModuleIndex(module.name);

            Vector3 delta = Vector3.zero;
            bool found = false;

            if (myIndex >= 0)
            {
                Transform next = FindSiblingByIndex(parent, side, myIndex + 1);
                if (next != null)
                {
                    delta = next.position - t.position;
                    found = true;
                }
                else
                {
                    Transform prev = FindSiblingByIndex(parent, side, myIndex - 1);
                    if (prev != null)
                    {
                        delta = t.position - prev.position;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                // Isimden indeks okunamadi: en yakin kardes modulu kullan
                Transform best = null;
                float bestD = float.MaxValue;

                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform c = parent.GetChild(i);
                    if (c == t) continue;
                    if (c.GetComponentInChildren<MeshFilter>(true) == null) continue;
                    if (side != null && SideTag(c.name) != side) continue;

                    float d = Vector3.Distance(c.position, t.position);
                    if (d > 0.05f && d < bestD) { bestD = d; best = c; }
                }

                if (best == null) return false;
                delta = best.position - t.position;
            }

            delta.y = 0f;
            spacing = delta.magnitude;
            if (spacing < 0.05f) return false;

            axis = delta / spacing;
            return true;
        }

        private static Transform FindSiblingByIndex(Transform parent, string side, int index)
        {
            if (index < 0) return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (side != null && SideTag(c.name) != side) continue;
                if (ModuleIndex(c.name) == index) return c;
            }
            return null;
        }

        private static string SideTag(string name)
        {
            string n = name.ToUpperInvariant();
            if (n.Contains("_L_")) return "L";
            if (n.Contains("_R_")) return "R";
            return null;
        }

        // "GRPRO_L_012_0072m" -> 12
        private static int ModuleIndex(string name)
        {
            string[] parts = name.Split('_');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string p = parts[i].ToUpperInvariant();
                if (p != "L" && p != "R") continue;

                int idx;
                if (int.TryParse(parts[i + 1], out idx)) return idx;
            }
            return -1;
        }

        private static void SmoothHeights(List<Vector3> pts, int window)
        {
            if (window <= 1 || pts.Count < 3) return;

            float[] ys = new float[pts.Count];
            for (int i = 0; i < pts.Count; i++) ys[i] = pts[i].y;

            ys = SmoothArray(ys, window);

            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 p = pts[i];
                p.y = ys[i];
                pts[i] = p;
            }
        }

        private static bool IsChildOf(Transform t, Transform maybeParent)
        {
            while (t != null)
            {
                if (t == maybeParent) return true;
                t = t.parent;
            }
            return false;
        }

        private void DeleteGenerated()
        {
            GameObject existing;
            int guard = 0;
            while ((existing = FindSceneObject(RootName)) != null && guard++ < 8)
                Undo.DestroyObjectImmediate(existing);

            // Onceki surumlerden kalmis olabilecek yalniz kalan gruplar
            guard = 0;
            while ((existing = FindSceneObject(RootName + "_L")) != null && guard++ < 8)
                Undo.DestroyObjectImmediate(existing);

            guard = 0;
            while ((existing = FindSceneObject(RootName + "_R")) != null && guard++ < 8)
                Undo.DestroyObjectImmediate(existing);
        }

        private static GameObject FindSceneObject(string name)
        {
            Transform[] all = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i].gameObject;
            return null;
        }

        private static bool IsUnderGenerated(Transform t)
        {
            while (t != null)
            {
                if (t.name.StartsWith(RootName, System.StringComparison.Ordinal)) return true;
                t = t.parent;
            }
            return false;
        }

        // ==================================================================
        //  Scene onizleme
        // ==================================================================
        private void OnSceneGUI(SceneView view)
        {
            if (!analyzed) return;

            DrawPolyline(centerLine, new Color(1f, 0.92f, 0.2f, 1f));
            DrawPolyline(previewLeft, new Color(0.2f, 1f, 0.3f, 1f));
            DrawPolyline(previewRight, new Color(1f, 0.35f, 0.2f, 1f));
        }

        private static void DrawPolyline(List<Vector3> pts, Color color)
        {
            if (pts == null || pts.Count < 2) return;

            int stride = Mathf.Max(1, pts.Count / 2000);
            List<Vector3> draw = new List<Vector3>(pts.Count / stride + 2);
            for (int i = 0; i < pts.Count; i += stride) draw.Add(pts[i]);
            if (draw[draw.Count - 1] != pts[pts.Count - 1]) draw.Add(pts[pts.Count - 1]);

            Handles.color = color;
            Handles.DrawAAPolyLine(4f, draw.ToArray());
        }

        private void SetStatus(string msg, MessageType type)
        {
            status = msg;
            statusType = type;
            Repaint();
        }
    }
}
#endif
