// Elektrik hatti: direkler arasina sarkan kablo uretir.
//
// Bu bilesen DIREKLERI YERLESTIRMEZ - sadece verilen direk listesini bagalar.
// Dizici (FoggyRoad_DirekAutoPlacer) direkleri bir kez koyar ve cekilir; kullanici
// sonradan diregi elle tasidiginda / sildiginde / araya yenisini ekledigininde
// bu bilesen kendi basina tepki verir. Ikisi ayni sinifta olsaydi her rebuild
// kullanicinin elle yaptiklarini silerdi.
//
// Kablo temsili: prosedurel tup mesh, hepsi TEK mesh'te.
// LineRenderer kullanilmadi cunku:
//   - 80 direk x 2 kablo = 158 ayri renderer, batch yok. Sahne zaten FPS/pop-in
//     sorunu yasamis (bkz. FoggyRoad_PopInTamDuzeltme.cs).
//   - LineRenderer kalinligi ekran uzayinda sabit: 200 m'deki kablo yakindaki
//     kadar kalin gorunur, sisli sahnede yanlis durur.
// Tup mesh -> 1 draw call, dunya uzayinda kalinlik, mesafeyle dogru incelme.
//
// Sarkma: gercek catenary (a*cosh). Sarkma miktari ACIKLIK YUZDESI olarak
// tanimli (sagRatio), sabit metre degil. Kullanicinin istedigi "mesafeye gore
// kisalip uzasin" davranisi tam olarak budur.
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FoggyRoad_PowerLine : MonoBehaviour
{
    // ---------------------------------------------------------------- veri
    [Tooltip("Sirali direk listesi. Sira bozulursa 'Direkleri yeniden sirala'ya bas.")]
    public List<Transform> poles = new List<Transform>();

    [Tooltip("Yol merkez hatti. Direkleri dogru siraya sokmak icin kullanilir.")]
    public List<Vector3> spine = new List<Vector3>();

    // ------------------------------------------------------------- kablo
    [Header("Kablo")]
    [Range(1, 6)]
    [Tooltip("Direkler arasindaki kablo sayisi.")]
    public int wireCount = 2;

    [Range(0.001f, 0.15f)]
    [Tooltip("Kablo yaricapi (m). Gercek kablo ~1 cm ama 40 m'de sub-pixel olup titrer.")]
    public float wireRadius = 0.045f;

    [Range(3, 8)]
    [Tooltip("Tup kesitinin kose sayisi. 4 yeterli.")]
    public int sides = 4;

    [Tooltip("Kablo materyali. Bos birakilirsa Inspector'daki butonla uretilebilir.")]
    public Material wireMaterial;

    // ------------------------------------------------------------ sarkma
    [Header("Sarkma")]
    [Range(0.002f, 0.12f)]
    [Tooltip("Sarkma = aciklik x bu oran. 0.030 = %3, gercek dagitim hatlarinda %2-4.")]
    public float sagRatio = 0.030f;

    [Min(0f)]
    [Tooltip("En az sarkma (m). Cok kisa aciklikta kablo duz cizgi olmasin.")]
    public float minSag = 0.25f;

    [Min(0.05f)]
    [Tooltip("En fazla sarkma (m). Cok uzun aciklikta kablo yere degmesin.")]
    public float maxSag = 3.0f;

    [Min(0.25f)]
    [Tooltip("Her ~bu kadar metrede bir kesit uretilir.")]
    public float segMeters = 2.0f;

    // ----------------------------------------------------------- anchor
    [Header("Baglanti noktalari")]
    [Tooltip("Direk altinda 'Anchor_' ile baslayan cocuk yoksa bu local offsetler kullanilir. " +
             "Varsayilan degerler elektrikdiregi modelinin izolator tepeleridir.")]
    public Vector3[] fallbackAnchors =
    {
        new Vector3(-1.20f, 16.91f, 0f),
        new Vector3( 1.20f, 16.91f, 0f)
    };

    [Tooltip("Kullanici bir diregi 180 derece dondururse kablolarin caprazlanmasini engeller.")]
    public bool autoPairAnchors = true;

    [Header("Editor")]
    [Tooltip("Editorde direk tasiyinca kablo kendini otomatik yeniler.")]
    public bool autoUpdateInEditor = true;

    // --------------------------------------------------------- ic durum
    [System.NonSerialized] Mesh mesh;
    [System.NonSerialized] MeshFilter mf;
    [System.NonSerialized] MeshRenderer mr;

    // Son bilinen direk pozisyon/rotasyonlari. transform.hasChanged KULLANILMIYOR:
    // baska bir script de onu kullaniyor olabilir, false'a cekmek onu bozar.
    [System.NonSerialized] bool paramsDirty;
    [System.NonSerialized] Vector3[] lastPos;
    [System.NonSerialized] Quaternion[] lastRot;

    // yeniden kullanilan tamponlar (her rebuild'de allocation olmasin)
    static readonly List<Vector3> vBuf = new List<Vector3>();
    static readonly List<Vector3> nBuf = new List<Vector3>();
    static readonly List<Vector2> uvBuf = new List<Vector2>();
    static readonly List<int> tBuf = new List<int>();
    static readonly List<Vector3> ptBuf = new List<Vector3>();

    // ==================================================================
    //  Yasam dongusu
    // ==================================================================
    void OnEnable()
    {
        EnsureComponents();
        Rebuild();
    }

    void OnValidate()
    {
        minSag = Mathf.Min(minSag, maxSag);
        // Rebuild dogrudan cagrilmiyor: OnValidate icinde mesh yaratmak/yok etmek
        // Unity'de uyari uretir. Bayrak konur, Watcher bir sonraki frame'de alir.
        paramsDirty = true;
    }

    void OnDisable()
    {
        if (mf != null) mf.sharedMesh = null;
        DestroyMesh();
    }

    void EnsureComponents()
    {
        if (mf == null) mf = GetComponent<MeshFilter>();
        if (mr == null) mr = GetComponent<MeshRenderer>();

        if (mr != null)
        {
            // 4.5 cm geometri shadow map'te ya gorunmez ya acne yapar; ayrica
            // binlerce ucgeni shadow pass'ten cikarmak net FPS kazancidir.
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            if (wireMaterial != null && mr.sharedMaterial != wireMaterial)
                mr.sharedMaterial = wireMaterial;
        }
    }

    void DestroyMesh()
    {
        if (mesh == null) return;
        if (Application.isPlaying) Destroy(mesh);
        else DestroyImmediate(mesh);
        mesh = null;
    }

    // ==================================================================
    //  Mesh uretimi
    // ==================================================================
    public Mesh CurrentMesh { get { return mesh; } }

    public void Rebuild()
    {
        EnsureComponents();
        if (mf == null) return;

        paramsDirty = false;
        CleanNulls();
        CacheTransforms();

        vBuf.Clear(); nBuf.Clear(); uvBuf.Clear(); tBuf.Clear();

        for (int i = 0; i + 1 < poles.Count; i++)
        {
            Transform a = poles[i];
            Transform b = poles[i + 1];
            if (a == null || b == null) continue;

            List<Vector3> anchorsA = BuildWirePoints(a);
            List<Vector3> anchorsB = BuildWirePoints(b);
            if (anchorsA.Count == 0 || anchorsB.Count == 0) continue;

            if (autoPairAnchors) MaybeFlip(anchorsA, anchorsB);

            int n = Mathf.Min(anchorsA.Count, anchorsB.Count);
            for (int w = 0; w < n; w++)
                AppendSpan(anchorsA[w], anchorsB[w]);
        }

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "FoggyRoad_PowerLine";
            // Sahne dosyasina gomulmesin: .unity sismez, git diff temiz kalir.
            mesh.hideFlags = HideFlags.DontSave;
            mesh.indexFormat = IndexFormat.UInt32;
        }
        else
        {
            mesh.Clear();
        }

        if (vBuf.Count > 0)
        {
            mesh.SetVertices(vBuf);
            mesh.SetNormals(nBuf);
            mesh.SetUVs(0, uvBuf);
            mesh.SetTriangles(tBuf, 0, true);
            mesh.RecalculateBounds();
        }

        mf.sharedMesh = mesh;
    }

    /// <summary>Kablo sayisina gore baglanti noktalari (dunya uzayinda).</summary>
    List<Vector3> BuildWirePoints(Transform pole)
    {
        List<Vector3> anchors = CollectAnchors(pole);
        List<Vector3> result = new List<Vector3>(Mathf.Max(1, wireCount));

        if (anchors.Count == 0) return result;
        if (anchors.Count == 1 || wireCount == 1)
        {
            // Tek anchor / tek kablo: ortadan gecsin
            Vector3 mid = anchors.Count == 1
                ? anchors[0]
                : (anchors[0] + anchors[anchors.Count - 1]) * 0.5f;
            for (int i = 0; i < wireCount; i++) result.Add(mid);
            return result;
        }

        // Anchor'lar arasina wireCount kadar noktayi esit dagit.
        // 2 anchor + 2 kablo -> tam olarak iki izolator.
        // 2 anchor + 3 kablo -> iki izolator + traverse ortasi.
        Vector3 first = anchors[0];
        Vector3 last = anchors[anchors.Count - 1];
        for (int i = 0; i < wireCount; i++)
        {
            float t = wireCount == 1 ? 0.5f : (float)i / (wireCount - 1);
            result.Add(Vector3.Lerp(first, last, t));
        }
        return result;
    }

    /// <summary>
    /// Once direk altinda 'Anchor_' ile baslayan cocuklar aranir (kullanici bunlari
    /// Scene view'da surukleyip ince ayar yapabilir). Bulunamazsa fallback local
    /// offsetler kullanilir - kullanicinin elle koydugu ham FBX instance'lari icin.
    /// </summary>
    public List<Vector3> CollectAnchors(Transform pole)
    {
        List<Vector3> result = new List<Vector3>();
        if (pole == null) return result;

        List<Transform> found = new List<Transform>();
        CollectAnchorChildren(pole, found);

        if (found.Count > 0)
        {
            found.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
            for (int i = 0; i < found.Count; i++) result.Add(found[i].position);
            return result;
        }

        if (fallbackAnchors != null)
        {
            for (int i = 0; i < fallbackAnchors.Length; i++)
                result.Add(pole.TransformPoint(fallbackAnchors[i]));
        }
        return result;
    }

    static void CollectAnchorChildren(Transform t, List<Transform> outList)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            Transform c = t.GetChild(i);
            if (c.name.StartsWith("Anchor_")) outList.Add(c);
            CollectAnchorChildren(c, outList);
        }
    }

    /// <summary>
    /// Direk 180 derece dondurulmusse b'nin anchor sirasi dunya uzayinda tersine
    /// doner ve kablolar caprazlanir. Uc-uca toplam mesafeyi karsilastirip gerekirse
    /// ters cevirir.
    /// </summary>
    static void MaybeFlip(List<Vector3> a, List<Vector3> b)
    {
        if (a.Count < 2 || b.Count < 2) return;

        float duz = Vector3.Distance(a[0], b[0]) + Vector3.Distance(a[a.Count - 1], b[b.Count - 1]);
        float capraz = Vector3.Distance(a[0], b[b.Count - 1]) + Vector3.Distance(a[a.Count - 1], b[0]);
        if (capraz < duz) b.Reverse();
    }

    // ==================================================================
    //  Catenary
    // ==================================================================
    /// <summary>
    /// sag = a * (cosh(L/(2a)) - 1) denklemini a icin cozer.
    /// Fonksiyon a'ya gore kesin azalan (a buyuk -> gergin -> sag kucuk),
    /// bu yuzden bisection guvenli. Baslangic tahmini seri aciliminddan: a0 = L^2/(8*sag).
    /// </summary>
    public static float SolveA(float L, float sag)
    {
        if (sag <= 1e-4f || L <= 1e-4f) return float.PositiveInfinity; // duz cizgi

        float a0 = L * L / (8f * sag);
        float lo = a0 * 0.1f;
        float hi = a0 * 10f;

        for (int i = 0; i < 25; i++)
        {
            float a = 0.5f * (lo + hi);
            float s = a * ((float)System.Math.Cosh(L / (2f * a)) - 1f);
            if (s > sag) lo = a; else hi = a;
        }
        return 0.5f * (lo + hi);
    }

    /// <summary>
    /// Egimli aciklikta duz cizgi + dikey sarkma profili. Uclarda d=0 (anchor'a tam
    /// oturur), ortada d=-sag. Gercek egik-catenary'nin en alcak noktasi merkezden
    /// kayar; direkler dik durdugu ve arazi egimi sinirli oldugu icin 20 derece
    /// altinda gozle ayirt edilemez.
    /// </summary>
    static Vector3 CatenaryPoint(Vector3 A, Vector3 B, float t, float a, float L)
    {
        Vector3 p = Vector3.Lerp(A, B, t);
        if (float.IsInfinity(a) || float.IsNaN(a)) return p;

        float half = L * 0.5f;
        float x = (t - 0.5f) * L;
        float d = a * (float)System.Math.Cosh(x / a) - a * (float)System.Math.Cosh(half / a);
        return p + Vector3.up * d;
    }

    // ==================================================================
    //  Tup mesh
    // ==================================================================
    void AppendSpan(Vector3 A, Vector3 B)
    {
        // Aciklik YATAY olcuulur - dikey bilesen sarkmayi sismesin.
        Vector2 a2 = new Vector2(A.x, A.z);
        Vector2 b2 = new Vector2(B.x, B.z);
        float L = Vector2.Distance(a2, b2);
        if (L < 1e-3f) L = Vector3.Distance(A, B);
        if (L < 1e-3f) return;

        float sag = Mathf.Clamp(L * sagRatio, minSag, Mathf.Max(minSag, maxSag));
        float aCoef = SolveA(L, sag);

        int seg = Mathf.Clamp(Mathf.RoundToInt(L / Mathf.Max(0.25f, segMeters)), 6, 32);

        ptBuf.Clear();
        for (int i = 0; i <= seg; i++)
            ptBuf.Add(CatenaryPoint(A, B, (float)i / seg, aCoef, L));

        AppendTube(ptBuf);
    }

    void AppendTube(List<Vector3> pts)
    {
        int n = pts.Count;
        if (n < 2) return;

        int ring = Mathf.Clamp(sides, 3, 8);
        int baseIndex = vBuf.Count;
        float r = Mathf.Max(0.001f, wireRadius);
        float vCoord = 0f;

        for (int i = 0; i < n; i++)
        {
            Vector3 dir;
            if (i == 0) dir = pts[1] - pts[0];
            else if (i == n - 1) dir = pts[n - 1] - pts[n - 2];
            else dir = pts[i + 1] - pts[i - 1];

            if (dir.sqrMagnitude < 1e-10f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 right = Vector3.Cross(dir, Vector3.up);
            if (right.sqrMagnitude < 1e-8f) right = Vector3.Cross(dir, Vector3.forward);
            right.Normalize();
            Vector3 up = Vector3.Cross(right, dir).normalized;

            if (i > 0) vCoord += Vector3.Distance(pts[i - 1], pts[i]) * 0.5f;

            for (int k = 0; k < ring; k++)
            {
                float ang = (float)k / ring * Mathf.PI * 2f;
                Vector3 nrm = (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)).normalized;
                vBuf.Add(pts[i] + nrm * r);
                nBuf.Add(nrm);
                uvBuf.Add(new Vector2((float)k / ring, vCoord));
            }
        }

        for (int i = 0; i < n - 1; i++)
        {
            int row0 = baseIndex + i * ring;
            int row1 = row0 + ring;
            for (int k = 0; k < ring; k++)
            {
                int k2 = (k + 1) % ring;
                tBuf.Add(row0 + k); tBuf.Add(row1 + k); tBuf.Add(row1 + k2);
                tBuf.Add(row0 + k); tBuf.Add(row1 + k2); tBuf.Add(row0 + k2);
            }
        }
    }

    // ==================================================================
    //  Direk listesi bakimi
    // ==================================================================
    public void CleanNulls()
    {
        for (int i = poles.Count - 1; i >= 0; i--)
            if (poles[i] == null) poles.RemoveAt(i);
    }

    /// <summary>
    /// Direkleri yol hattina projekte edip yay uzunluguna gore siralar. Kullanici
    /// bir diregi yana kaydirsa, araya yenisini koysa veya sirayi karistirsa bile
    /// dogru sonuc verir. Hat yoksa greedy chain (en uctan basla, en yakina gec).
    /// </summary>
    public void ResortPoles()
    {
        CleanNulls();
        if (poles.Count < 3) return;

        if (spine != null && spine.Count >= 2)
        {
            List<KeyValuePair<float, Transform>> keyed =
                new List<KeyValuePair<float, Transform>>(poles.Count);

            for (int i = 0; i < poles.Count; i++)
            {
                float s = ProjectS(spine, poles[i].position);
                keyed.Add(new KeyValuePair<float, Transform>(s, poles[i]));
            }
            keyed.Sort((x, y) => x.Key.CompareTo(y.Key));

            poles.Clear();
            for (int i = 0; i < keyed.Count; i++) poles.Add(keyed[i].Value);
            return;
        }

        GreedyChain();
    }

    static float ProjectS(List<Vector3> pts, Vector3 world)
    {
        float bestSqr = float.MaxValue;
        float bestS = 0f;
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
            if (dSqr < bestSqr) { bestSqr = dSqr; bestS = acc + segLen * t; }
            acc += segLen;
        }
        return bestS;
    }

    void GreedyChain()
    {
        // En uctaki direk: digerlerine toplam mesafesi en buyuk olan
        int start = 0;
        float bestTotal = -1f;
        for (int i = 0; i < poles.Count; i++)
        {
            float total = 0f;
            for (int j = 0; j < poles.Count; j++)
                if (i != j) total += Vector3.Distance(poles[i].position, poles[j].position);
            if (total > bestTotal) { bestTotal = total; start = i; }
        }

        List<Transform> kalan = new List<Transform>(poles);
        List<Transform> sirali = new List<Transform>(poles.Count);

        Transform cur = kalan[start];
        kalan.RemoveAt(start);
        sirali.Add(cur);

        while (kalan.Count > 0)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < kalan.Count; i++)
            {
                float d = Vector3.SqrMagnitude(kalan[i].position - cur.position);
                if (d < bestD) { bestD = d; best = i; }
            }
            cur = kalan[best];
            kalan.RemoveAt(best);
            sirali.Add(cur);
        }

        poles.Clear();
        poles.AddRange(sirali);
    }

    /// <summary>Bu objenin cocuklari arasindaki direkleri listeye toplar.</summary>
    public void CollectPolesFromChildren(Transform parent)
    {
        if (parent == null) parent = transform.parent != null ? transform.parent : transform;

        poles.Clear();
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c == transform) continue;
            if (c.GetComponent<FoggyRoad_PowerLine>() != null) continue;
            poles.Add(c);
        }
        ResortPoles();
    }

    // ==================================================================
    //  Degisiklik takibi (editor)
    // ==================================================================
    void CacheTransforms()
    {
        if (lastPos == null || lastPos.Length != poles.Count)
        {
            lastPos = new Vector3[poles.Count];
            lastRot = new Quaternion[poles.Count];
        }
        for (int i = 0; i < poles.Count; i++)
        {
            lastPos[i] = poles[i] != null ? poles[i].position : Vector3.zero;
            lastRot[i] = poles[i] != null ? poles[i].rotation : Quaternion.identity;
        }
    }

    /// <summary>Direklerden biri tasindi/dondu/silindi mi?</summary>
    public bool IsDirty()
    {
        if (paramsDirty) return true;
        if (lastPos == null || lastPos.Length != poles.Count) return true;

        for (int i = 0; i < poles.Count; i++)
        {
            if (poles[i] == null) return true;
            if ((poles[i].position - lastPos[i]).sqrMagnitude > 1e-8f) return true;
            if (Quaternion.Angle(poles[i].rotation, lastRot[i]) > 0.01f) return true;
        }
        return false;
    }

}
