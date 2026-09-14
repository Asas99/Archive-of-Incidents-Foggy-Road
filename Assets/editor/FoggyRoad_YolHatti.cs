// Yol merkez hatti cikarma ve polyline yardimcilari.
//
// Elektrik direklerini yol boyunca dizmek icin yolun merkez hattina ihtiyac var.
// Projede spline sistemi yok (com.unity.splines kurulu degil), bu yuzden hat
// birkac farkli kaynaktan cikarilabiliyor. Tek kaynaga guvenmek kirilgan:
// sahnedeki 'yol2' objesi inactive olabiliyor ve mesh her zaman okunabilir degil.
// Bu yuzden bir fallback zinciri var:
//
//   A) Bariyer parcalarindan (Edge_L/R, Post_L/R, Rail_L/R, GRPRO_L/R)
//      Sol ve sag ayni indeksli parcalarin ortalamasi = merkez hat.
//      En saglam yontem: mesh okunabilir olmasi gerekmiyor, aktiflikten etkilenmiyor.
//   B) Yol mesh'inden (occupancy grid + Dijkstra)
//      FoggyRoad_GuardrailAutoPlacer.cs icindeki algoritmanin kopyasi.
//      O dosya 1513 satir ve calisiyor; private metotlarini disari cikarmak yerine
//      kopyalandi - regresyon riski sifir.
//   C) Kullanicinin sectigi objelerden (Scene view'da elle isaretleme)
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoggyRoadTools.Yol
{
    public static class YolHatti
    {
        // ==================================================================
        //  Polyline yardimcilari
        // ==================================================================
        public static Vector3 TangentAt(List<Vector3> pts, int i)
        {
            int lo = Mathf.Max(0, i - 2);
            int hi = Mathf.Min(pts.Count - 1, i + 2);
            Vector3 d = pts[hi] - pts[lo];
            if (d.sqrMagnitude < 1e-10f) d = Vector3.forward;
            return d.normalized;
        }

        /// <summary>Yatay (y=0) teget. Direkler dik durdugu icin yaw hesabinda bu kullanilir.</summary>
        public static Vector3 FlatTangentAt(List<Vector3> pts, int i)
        {
            Vector3 t = TangentAt(pts, i);
            t.y = 0f;
            if (t.sqrMagnitude < 1e-8f) return Vector3.forward;
            return t.normalized;
        }

        public static float PolylineLength(List<Vector3> pts)
        {
            float len = 0f;
            for (int i = 1; i < pts.Count; i++) len += Vector3.Distance(pts[i - 1], pts[i]);
            return len;
        }

        public static float[] BuildCumulative(List<Vector3> pts)
        {
            float[] cum = new float[pts.Count];
            float acc = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                acc += Vector3.Distance(pts[i - 1], pts[i]);
                cum[i] = acc;
            }
            return cum;
        }

        public static List<Vector3> Smooth(List<Vector3> pts, int passes)
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

                cur = new List<Vector3>(next);
            }
            return cur;
        }

        public static List<Vector3> Resample(List<Vector3> pts, float step)
        {
            if (pts.Count < 2) return new List<Vector3>(pts);

            step = Mathf.Max(0.05f, step);

            float[] cum = BuildCumulative(pts);
            float acc = cum[cum.Length - 1];
            if (acc < step) return new List<Vector3>(pts);

            List<Vector3> outPts = new List<Vector3>();
            int count = Mathf.Max(2, Mathf.RoundToInt(acc / step) + 1);
            for (int i = 0; i < count; i++)
            {
                float s = acc * i / (count - 1);
                outPts.Add(SampleAt(pts, cum, s));
            }
            return outPts;
        }

        public static Vector3 SampleAt(List<Vector3> pts, float[] cum, float s)
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

        /// <summary>
        /// Bir dunya noktasini hat uzerine projekte eder. 's' = hat basindan itibaren
        /// yay uzunlugu. Direkleri dogru siraya sokmak icin kullanilir: kullanici bir
        /// diregi yana kaydirsa veya araya yenisini koysa bile sira bozulmaz.
        /// </summary>
        public static bool ProjectToPolyline(List<Vector3> pts, Vector3 world,
                                             out float s, out Vector3 onLine)
        {
            s = 0f;
            onLine = world;
            if (pts == null || pts.Count < 2) return false;

            float bestSqr = float.MaxValue;
            float acc = 0f;

            for (int i = 1; i < pts.Count; i++)
            {
                Vector3 a = pts[i - 1];
                Vector3 b = pts[i];
                Vector3 ab = b - a;
                float lenSqr = ab.sqrMagnitude;
                float segLen = Mathf.Sqrt(lenSqr);

                float t = lenSqr > 1e-10f ? Mathf.Clamp01(Vector3.Dot(world - a, ab) / lenSqr) : 0f;
                Vector3 p = a + ab * t;

                float dSqr = (world - p).sqrMagnitude;
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    onLine = p;
                    s = acc + segLen * t;
                }
                acc += segLen;
            }
            return true;
        }

        // ==================================================================
        //  Zemin ornekleme
        // ==================================================================
        /// <summary>
        /// Verilen XZ noktasinda zemin yuksekligini bulur.
        /// 1) Physics.Raycast (Terrain'in TerrainCollider'i var)
        /// 2) Terrain.SampleHeight (collider kapaliysa)
        /// Ikisi de basarisizsa false.
        /// </summary>
        public static bool ZeminOrnekle(Vector3 world, LayerMask mask, out float y, out Vector3 normal)
        {
            y = world.y;
            normal = Vector3.up;

            bool oldTriggers = Physics.queriesHitTriggers;
            Physics.queriesHitTriggers = false;
            try
            {
                RaycastHit hit;
                Vector3 from = new Vector3(world.x, world.y + 400f, world.z);
                if (Physics.Raycast(from, Vector3.down, out hit, 900f, mask, QueryTriggerInteraction.Ignore))
                {
                    y = hit.point.y;
                    normal = hit.normal;
                    return true;
                }
            }
            finally { Physics.queriesHitTriggers = oldTriggers; }

            // Terrain fallback
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            bool found = false;
            float bestY = 0f;
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain t = terrains[i];
                if (t.terrainData == null) continue;

                Vector3 local = world - t.transform.position;
                Vector3 size = t.terrainData.size;
                if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) continue;

                float h = t.SampleHeight(world) + t.transform.position.y;
                if (!found || h > bestY) { bestY = h; found = true; }
            }

            if (found) { y = bestY; return true; }
            return false;
        }

        // ==================================================================
        //  Kaynak: sahnedeki yol objesi
        // ==================================================================
        static readonly string[] YolAdlari = { "yol2", "yol", "yol 1", "Road", "road" };

        /// <summary>
        /// Sahnedeki yol kok objesini bulur. FoggyRoad_Hedefler.YolKoku() kullanilmiyor
        /// cunku o GameObject.Find ile ariyor ve inactive objeleri bulamiyor - diskteki
        /// 'yol2' tam olarak inactive durumda.
        /// </summary>
        public static GameObject BulYol()
        {
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                 FindObjectsSortMode.None);
            for (int a = 0; a < YolAdlari.Length; a++)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    if (all[i].name == YolAdlari[a] && all[i].GetComponentInChildren<MeshFilter>(true) != null)
                        return all[i].gameObject;
                }
            }
            return null;
        }

        // ==================================================================
        //  Kaynak A / C: obje gruplarindan hat
        //
        //  Uc ayri hat uretilir: yolun merkezi, sol bariyer hatti, sag bariyer
        //  hatti. Kullanici direkleri "bariyerin hemen yaninda" istiyorsa merkez
        //  yerine taraf hatti referans alinir; boylece offset kucuk bir sayi olur.
        // ==================================================================
        // Isim kaliplari: Edge_L_01, Post_R_07, Rail_L_12, GRPRO_L_012_0072m
        static readonly string[] BariyerAileleri = { "Edge_", "Post_", "Rail_", "GRPRO_" };

        public class HatSonucu
        {
            public List<Vector3> merkez;
            public List<Vector3> sol;
            public List<Vector3> sag;
            public bool tarafliVar;      // sol/sag gercekten ayri mi, yoksa merkezin kopyasi mi
            public string aciklama;
        }

        public static bool HatBariyerlerden(out HatSonucu sonuc, out string err)
        {
            sonuc = null;
            err = null;

            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None);

            for (int a = 0; a < BariyerAileleri.Length; a++)
            {
                string aile = BariyerAileleri[a];
                List<Transform> parcalar = new List<Transform>();

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    if (all[i].name.StartsWith(aile)) parcalar.Add(all[i]);
                }
                if (parcalar.Count < 4) continue;

                string ihmal;
                if (!HatTransformlardan(parcalar, out sonuc, out ihmal)) continue;

                sonuc.aciklama = aile + " parcalarindan (" + parcalar.Count + " obje)";
                return true;
            }

            err = "Sahnede bariyer parcasi bulunamadi (Edge_/Post_/Rail_/GRPRO_ ile baslayan objeler).";
            return false;
        }

        /// <summary>
        /// Verilen objelerden hat cikarir.
        ///   1) Isimlerde _L_ / _R_ + indeks varsa taraf-taraf eslesir (en dogru sonuc).
        ///   2) Yoksa noktalar MEKANSAL olarak siralanir (greedy zincir).
        ///
        /// Ikinci adim kritik: Unity'de secim sirasi hierarchy sirasidir, mekansal
        /// degil. Siralamadan baglamak "once sol sira, sonra sag sira" seklinde
        /// caprazlama bir cizgi uretir.
        /// </summary>
        public static bool HatTransformlardan(IEnumerable<Transform> objeler,
                                              out HatSonucu sonuc, out string err)
        {
            sonuc = null;
            err = null;

            SortedDictionary<int, List<Vector3>> solIdx = new SortedDictionary<int, List<Vector3>>();
            SortedDictionary<int, List<Vector3>> sagIdx = new SortedDictionary<int, List<Vector3>>();
            List<Vector3> hepsi = new List<Vector3>();

            foreach (Transform t in objeler)
            {
                if (t == null) continue;
                hepsi.Add(t.position);

                char taraf;
                int indeks;
                if (!TarafVeIndeks(t.name, out taraf, out indeks)) continue;

                SortedDictionary<int, List<Vector3>> hedef = (taraf == 'L') ? solIdx : sagIdx;
                List<Vector3> liste;
                if (!hedef.TryGetValue(indeks, out liste))
                {
                    liste = new List<Vector3>();
                    hedef.Add(indeks, liste);
                }
                liste.Add(t.position);
            }

            if (hepsi.Count < 2)
            {
                err = "En az 2 obje gerekli.";
                return false;
            }

            sonuc = new HatSonucu();

            // --- 1) Isimden taraf eslesmesi
            if (solIdx.Count >= 3 && sagIdx.Count >= 3)
            {
                List<Vector3> merkez = new List<Vector3>();
                List<Vector3> sol = new List<Vector3>();
                List<Vector3> sag = new List<Vector3>();

                foreach (KeyValuePair<int, List<Vector3>> kv in solIdx)
                {
                    List<Vector3> karsi;
                    if (!sagIdx.TryGetValue(kv.Key, out karsi)) continue;

                    Vector3 l = Ortalama(kv.Value);
                    Vector3 r = Ortalama(karsi);
                    sol.Add(l);
                    sag.Add(r);
                    merkez.Add((l + r) * 0.5f);
                }

                if (merkez.Count >= 3)
                {
                    sonuc.merkez = Duzelt(merkez);
                    sonuc.sol = Duzelt(sol);
                    sonuc.sag = Duzelt(sag);
                    sonuc.tarafliVar = true;
                    sonuc.aciklama = "isim eslesmesi (_L_/_R_ indeks)";
                    return true;
                }
            }

            // --- 2) Tek taraf: indeksleri olan tarafi kullan
            if (solIdx.Count >= 3 || sagIdx.Count >= 3)
            {
                SortedDictionary<int, List<Vector3>> tek = solIdx.Count >= sagIdx.Count ? solIdx : sagIdx;
                List<Vector3> noktalar = new List<Vector3>();
                foreach (KeyValuePair<int, List<Vector3>> kv in tek) noktalar.Add(Ortalama(kv.Value));

                sonuc.merkez = Duzelt(noktalar);
                sonuc.sol = sonuc.merkez;
                sonuc.sag = sonuc.merkez;
                sonuc.tarafliVar = false;
                sonuc.aciklama = "tek taraf indeksinden";
                return true;
            }

            // --- 3) Isim yok: mekansal siralama
            List<Vector3> zincir = SiralaZincir(hepsi);
            sonuc.merkez = Duzelt(zincir);
            sonuc.sol = sonuc.merkez;
            sonuc.sag = sonuc.merkez;
            sonuc.tarafliVar = false;
            sonuc.aciklama = "mekansal siralama (greedy zincir)";
            return true;
        }

        static List<Vector3> Duzelt(List<Vector3> noktalar)
        {
            if (noktalar.Count < 2) return noktalar;
            return Smooth(Resample(Smooth(noktalar, 1), 2f), 2);
        }

        /// <summary>
        /// Noktalari yol boyunca siralar: once en uctaki nokta bulunur (digerlerine
        /// toplam mesafesi en buyuk olan), sonra her adimda en yakin ziyaret
        /// edilmemis noktaya gecilir. Egri yollarda da dogru calisir.
        /// </summary>
        public static List<Vector3> SiralaZincir(List<Vector3> noktalar)
        {
            int n = noktalar.Count;
            if (n < 3) return new List<Vector3>(noktalar);

            int bas = 0;
            float enBuyuk = -1f;
            for (int i = 0; i < n; i++)
            {
                float toplam = 0f;
                for (int j = 0; j < n; j++)
                    if (i != j) toplam += Vector3.Distance(noktalar[i], noktalar[j]);
                if (toplam > enBuyuk) { enBuyuk = toplam; bas = i; }
            }

            bool[] alindi = new bool[n];
            List<Vector3> sirali = new List<Vector3>(n);

            int cur = bas;
            alindi[cur] = true;
            sirali.Add(noktalar[cur]);

            for (int adim = 1; adim < n; adim++)
            {
                int best = -1;
                float bestD = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (alindi[i]) continue;
                    float d = Vector3.SqrMagnitude(noktalar[i] - noktalar[cur]);
                    if (d < bestD) { bestD = d; best = i; }
                }
                if (best < 0) break;
                alindi[best] = true;
                sirali.Add(noktalar[best]);
                cur = best;
            }
            return sirali;
        }

        static Vector3 Ortalama(List<Vector3> liste)
        {
            Vector3 s = Vector3.zero;
            for (int i = 0; i < liste.Count; i++) s += liste[i];
            return s / Mathf.Max(1, liste.Count);
        }

        /// <summary>"Post_L_07" -> ('L', 7). Bulamazsa false.</summary>
        static bool TarafVeIndeks(string ad, out char taraf, out int indeks)
        {
            taraf = ' ';
            indeks = 0;

            int i = ad.IndexOf("_L_");
            if (i >= 0) taraf = 'L';
            else
            {
                i = ad.IndexOf("_R_");
                if (i < 0) return false;
                taraf = 'R';
            }

            int j = i + 3;
            int basla = j;
            while (j < ad.Length && ad[j] >= '0' && ad[j] <= '9') j++;
            if (j == basla) return false;

            return int.TryParse(ad.Substring(basla, j - basla), out indeks);
        }

        // ==================================================================
        //  Kaynak B: yol mesh'inden (occupancy grid + Dijkstra)
        //  FoggyRoad_GuardrailAutoPlacer.cs:400-960 algoritmasinin kopyasi.
        // ==================================================================
        struct Cell { public float ySum; public int n; }

        static readonly Vector2Int[] Neighbors8 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        static readonly Vector2Int[] Neighbors4 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        public static bool HatMeshten(GameObject road, float cellSize, float topNormalThreshold,
                                      float centerBias, out List<Vector3> hat,
                                      out string err, out bool meshOkunamadi)
        {
            hat = null;
            err = null;
            meshOkunamadi = false;

            if (road == null) { err = "Yol objesi secilmemis."; return false; }

            Vector3[] verts;
            int[] tris;
            if (!ToplaGeometri(road, out verts, out tris, out err, out meshOkunamadi)) return false;

            Dictionary<Vector2Int, Cell> grid = KurGrid(verts, tris, cellSize, topNormalThreshold);
            if (grid.Count < 16)
            {
                err = "Yol yuzeyi izgaraya oturmadi. Izgara Hucresi degerini buyutmeyi dene.";
                return false;
            }

            List<Vector2Int> path;
            if (!MerkezYol(grid, centerBias, out path, out err)) return false;

            float cs = Mathf.Max(0.05f, cellSize);
            List<Vector3> ham = new List<Vector3>(path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                Cell c;
                float y = grid.TryGetValue(path[i], out c) && c.n > 0 ? c.ySum / c.n : 0f;
                ham.Add(new Vector3((path[i].x + 0.5f) * cs, y, (path[i].y + 0.5f) * cs));
            }

            hat = Smooth(Resample(Smooth(ham, 2), 2f), 2);
            return hat.Count >= 4;
        }

        public static int MeshOkunabilirYap(GameObject road)
        {
            if (road == null) return 0;

            MeshFilter[] filters = road.GetComponentsInChildren<MeshFilter>(true);
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
            return changed;
        }

        static bool ToplaGeometri(GameObject road, out Vector3[] verts, out int[] tris,
                                  out string err, out bool meshOkunamadi)
        {
            verts = null;
            tris = null;
            err = null;
            meshOkunamadi = false;

            MeshFilter[] filters = road.GetComponentsInChildren<MeshFilter>(true);
            List<Vector3> vertList = new List<Vector3>();
            List<int> triList = new List<int>();
            int used = 0;

            for (int f = 0; f < filters.Length; f++)
            {
                Mesh mesh = filters[f].sharedMesh;
                if (mesh == null) continue;

                if (!mesh.isReadable)
                {
                    meshOkunamadi = true;
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

        static Dictionary<Vector2Int, Cell> KurGrid(Vector3[] verts, int[] tris,
                                                    float cellSize, float topNormalThreshold)
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

                EkleHucre(g, p0, invCs);
                EkleHucre(g, p1, invCs);
                EkleHucre(g, p2, invCs);

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
                        EkleHucreIndeks(g, new Vector2Int(xi, zi), y);
                    }
                }
            }
            return g;
        }

        static void EkleHucre(Dictionary<Vector2Int, Cell> g, Vector3 p, float invCs)
        {
            Vector2Int key = new Vector2Int(Mathf.FloorToInt(p.x * invCs), Mathf.FloorToInt(p.z * invCs));
            EkleHucreIndeks(g, key, p.y);
        }

        static void EkleHucreIndeks(Dictionary<Vector2Int, Cell> g, Vector2Int key, float y)
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

        static bool Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c,
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

        static bool MerkezYol(Dictionary<Vector2Int, Cell> grid, float centerBias,
                              out List<Vector2Int> path, out string err)
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

            // 2) Yolun iki ucu (grafik capi)
            Vector2Int seed = default(Vector2Int);
            foreach (KeyValuePair<Vector2Int, Cell> kv in grid) { seed = kv.Key; break; }

            Vector2Int endA = EnUzakHucre(grid, seed);
            Vector2Int endB = EnUzakHucre(grid, endA);

            // 3) Merkezde kalmayi tercih eden Dijkstra
            if (!Dijkstra(grid, endA, endB, dist, Mathf.Max(1, maxDist), centerBias, out path))
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

        static Vector2Int EnUzakHucre(Dictionary<Vector2Int, Cell> grid, Vector2Int start)
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

        static bool Dijkstra(Dictionary<Vector2Int, Cell> grid, Vector2Int from, Vector2Int to,
                             Dictionary<Vector2Int, int> edgeDist, int maxDist, float centerBias,
                             out List<Vector2Int> path)
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

        class MinHeap
        {
            struct Node { public float key; public Vector2Int value; }

            Node[] items;
            int count;

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
    }
}
#endif
