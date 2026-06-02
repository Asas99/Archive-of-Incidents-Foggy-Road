using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Korku/gerilim oyunu icin otomatik arazi olusturucu.
///
/// KULLANIM:
/// 1) Bu scripti Terrain GameObject'ine (Add Component) ekle.
/// 2) (Opsiyonel) Tree Prefabs alanina agac prefablarini suruke-birak.
/// 3) (Opsiyonel) Grass/Dirt/Road Layer alanlarina TerrainLayer'lari ata (doku boyamak icin).
/// 4) Component'in sag ust kosesindeki ":" menusunden  ->  "Haritayi Olustur" tikla.
///
/// Tum yol agi, benzinlik platosu ve agac yerlesimi otomatik ayarlanir.
/// Yol/benzinlik noktalarini Inspector'dan tweak'leyip tekrar uretebilirsin.
/// </summary>
[RequireComponent(typeof(Terrain))]
public class HorrorTerrainGenerator : MonoBehaviour
{
    [System.Serializable]
    public class RoadDef
    {
        public string name = "Road";
        [Tooltip("Normalize edilmis noktalar (0..1). X = terrain X ekseni, Y = terrain Z ekseni")]
        public List<Vector2> points = new List<Vector2>();
        [Tooltip("Yol genisligi (metre)")] public float width = 9f;
        [Tooltip("Kenar yumusatma genisligi (metre)")] public float shoulder = 7f;
        [Tooltip("Yol kenarinda agacsiz birakilacak ek mesafe (metre)")] public float treeClearance = 9f;
    }

    // Carve + maske icin hazirlanmis yol verisi
    class BakedRoad
    {
        public RoadDef def;
        public List<Vector3> samples; // (worldX, height01, worldZ)
    }

    // ----------------------------------------------------------------------
    [Header("GENEL")]
    [Tooltip("Ayni seed = ayni harita")] public int seed = 1337;
    [Tooltip("Play'e basildiginda otomatik harita uretsin mi (test icin pratik)")]
    public bool generateOnStart = false;
    [Tooltip("Acik ise terrain boyutunu asagidaki degerle zorlar (gercekci olcek icin onerilir)")]
    public bool applyTerrainSize = false;
    [Tooltip("Terrain boyutu (genislik, yukseklik, uzunluk) metre")]
    public Vector3 terrainSize = new Vector3(1000f, 250f, 1000f);

    [Header("ARAZI - YUKSEKLIK / DAGLAR")]
    [Tooltip("Vadinin taban yuksekligi (0..1)")][Range(0f, 0.5f)] public float baseLevel = 0.12f;
    [Tooltip("Yumusak tepe boyutu (metre, buyuk = genis tepeler)")] public float hillScale = 220f;
    [Tooltip("Tepe yuksekligi siddeti")][Range(0f, 0.4f)] public float hillHeight = 0.10f;
    [Tooltip("Dag boyutu (metre)")] public float mountainScale = 520f;
    [Tooltip("Dag yuksekligi siddeti")][Range(0f, 1f)] public float mountainHeight = 0.55f;
    [Tooltip("Detay noise boyutu (metre)")] public float detailScale = 45f;
    [Tooltip("Detay yuksekligi")][Range(0f, 0.1f)] public float detailHeight = 0.025f;

    [Header("ARAZI - VADI MASKESI")]
    [Tooltip("Merkezdeki duz vadi yaricapi (0..1). Disinda daglar yukselir")]
    [Range(0.1f, 0.6f)] public float valleyRadius = 0.34f;
    [Tooltip("Daglarin kenara dogru ne kadar sert yukseldigi")]
    [Range(1f, 4f)] public float edgePower = 1.6f;

    [Header("YOLLAR")]
    [Tooltip("Yolu zeminden hafif asagi gomer (drenaj / cukur his)")]
    [Range(-0.02f, 0f)] public float roadHeightOffset = -0.004f;
    [Tooltip("Yol boyu egim yumusatma penceresi (yuksek = daha duz/surulebilir yol)")]
    [Range(3, 60)] public int roadSmoothWindow = 21;
    public List<RoadDef> roads = new List<RoadDef>();

    [Header("BENZINLIK ALANI")]
    [Tooltip("Benzinlik merkezi (normalize 0..1). Servis yolundan biraz geride")]
    public Vector2 stationCenter = new Vector2(0.60f, 0.70f);
    [Tooltip("Benzinlik platosu boyutu (metre)")] public Vector2 stationSize = new Vector2(70f, 55f);
    [Tooltip("Plato kenar yumusatmasi (metre)")] public float stationFalloff = 18f;
    [Range(-0.02f, 0.01f)] public float stationHeightOffset = -0.002f;
    [Tooltip("Benzinlik etrafinda agacsiz birakilacak ek mesafe (metre)")]
    public float stationTreeClearance = 14f;
    [Tooltip("Servis yolundan benzinlige otomatik giris yolu eklensin mi")]
    public bool autoDriveway = true;

    [Header("AGACLAR (opsiyonel - prefab gerekli)")]
    public GameObject[] treePrefabs;
    [Tooltip("Agaclar arasi yaklasik mesafe (metre). Kucuk = daha sik orman")]
    public float treeSpacing = 6f;
    [Tooltip("Konum dagilim rastgeleligi (metre)")] public float treeJitter = 3.5f;
    [Tooltip("Genel agac yogunlugu (0..1)")][Range(0f, 1f)] public float treeDensity = 0.85f;
    [Tooltip("Bu egimden (derece) dik yamaclara agac konmaz")] public float maxTreeSlope = 32f;
    [Tooltip("Orman icindeki acikliklarin boyutu (metre)")] public float clearingScale = 90f;
    [Tooltip("Aciklik esigi (yuksek = daha cok aciklik)")][Range(0f, 1f)] public float clearingThreshold = 0.32f;
    public Vector2 treeScaleRange = new Vector2(0.8f, 1.4f);

    [Header("DOKU / TERRAIN LAYERS (opsiyonel)")]
    public TerrainLayer grassLayer;
    public TerrainLayer dirtLayer;   // yol kenari + benzinlik zemini + dik yamac
    public TerrainLayer roadLayer;   // asfalt
    [Tooltip("Bu egimden dik yerlere toprak/kaya boyar")] public float slopeDirtAngle = 30f;

    // ======================================================================
    Terrain _terrain;
    TerrainData _data;
    Vector2 _noiseOffset;

    void Start()
    {
        if (generateOnStart) GenerateAll();
    }

    // ----------------------------------------------------------------------
    [ContextMenu("1) Haritayi Olustur (Hepsi)")]
    public void GenerateAll()
    {
        Prepare();
        InitDefaultsIfNeeded();

        if (applyTerrainSize) _data.size = terrainSize;

        Vector3 size = _data.size;
        int res = _data.heightmapResolution;

        // 1. Yukseklik
        float[,] heights = GenerateHeights(res, size);

        // 2. Yollari hazirla (driveway dahil)
        List<BakedRoad> baked = BuildAllRoads(heights, res, size);

        // 3. Yollari kaz
        foreach (var r in baked) CarveRoad(heights, res, size, r);

        // 4. Benzinlik platosu
        FlattenStation(heights, res, size);

        // 5. Yukseklikleri uygula
        _data.SetHeights(0, 0, heights);

        // 6. Doku boyama (layer atanmissa)
        if (grassLayer || dirtLayer || roadLayer)
            PaintTextures(res, size, baked);

        // 7. Agaclar (prefab varsa)
        if (treePrefabs != null && treePrefabs.Length > 0)
            PlaceTrees(res, size, baked);
        else
            Debug.Log("[HorrorTerrain] Agac prefabi atanmadi - agac yerlesimi atlandi.");

        _terrain.Flush();
        MarkDirty();
        Debug.Log("[HorrorTerrain] Harita olusturuldu.");
    }

    [ContextMenu("2) Sadece Yukseklik + Yol")]
    public void GenerateHeightAndRoads()
    {
        Prepare();
        InitDefaultsIfNeeded();
        if (applyTerrainSize) _data.size = terrainSize;
        Vector3 size = _data.size; int res = _data.heightmapResolution;
        float[,] heights = GenerateHeights(res, size);
        var baked = BuildAllRoads(heights, res, size);
        foreach (var r in baked) CarveRoad(heights, res, size, r);
        FlattenStation(heights, res, size);
        _data.SetHeights(0, 0, heights);
        _terrain.Flush(); MarkDirty();
    }

    [ContextMenu("Agaclari Temizle")]
    public void ClearTrees()
    {
        Prepare();
        _data.SetTreeInstances(new TreeInstance[0], true);
        MarkDirty();
    }

    // ----------------------------------------------------------------------
    void Prepare()
    {
        _terrain = GetComponent<Terrain>();
        _data = _terrain.terrainData;
        var prng = new System.Random(seed);
        _noiseOffset = new Vector2((float)prng.NextDouble() * 9000f, (float)prng.NextDouble() * 9000f);
    }

    void InitDefaultsIfNeeded()
    {
        if (roads != null && roads.Count > 0) { EnsureDriveway(); return; }

        roads = new List<RoadDef>();

        // ANA YOL - sol kenar boyunca asagidan yukari, ortada kavsak
        roads.Add(new RoadDef
        {
            name = "Ana Yol",
            width = 10f,
            shoulder = 8f,
            treeClearance = 11f,
            points = new List<Vector2>
            {
                new Vector2(0.12f, 0.02f),
                new Vector2(0.12f, 0.28f),
                new Vector2(0.145f, 0.50f),  // KAVSAK
                new Vector2(0.18f, 0.74f),
                new Vector2(0.21f, 0.98f),
            }
        });

        // SERVIS YOLU - kavsaktan saga kivrilir, benzinligin onunden gecer, sag kenarda baska yola baglanir
        roads.Add(new RoadDef
        {
            name = "Servis Yolu",
            width = 7.5f,
            shoulder = 7f,
            treeClearance = 9f,
            points = new List<Vector2>
            {
                new Vector2(0.145f, 0.50f),  // KAVSAK (ana yol ile ortak)
                new Vector2(0.30f, 0.485f),
                new Vector2(0.45f, 0.515f),
                new Vector2(0.60f, 0.545f),  // benzinligin onu
                new Vector2(0.75f, 0.50f),
                new Vector2(0.90f, 0.46f),
                new Vector2(0.99f, 0.44f),   // sag kenar -> baska yol baglantisi
            }
        });

        EnsureDriveway();
    }

    void EnsureDriveway()
    {
        if (!autoDriveway) return;
        if (roads.Exists(r => r.name == "Giris Yolu")) return;

        // Servis yolundaki en yakin noktayi bul, benzinlige kisa bir baglanti ekle
        RoadDef service = roads.Find(r => r.name == "Servis Yolu") ?? (roads.Count > 1 ? roads[1] : null);
        if (service == null || service.points.Count == 0) return;

        Vector2 nearest = service.points[0];
        float best = float.MaxValue;
        foreach (var p in service.points)
        {
            float d = (p - stationCenter).sqrMagnitude;
            if (d < best) { best = d; nearest = p; }
        }

        roads.Add(new RoadDef
        {
            name = "Giris Yolu",
            width = 6f,
            shoulder = 5f,
            treeClearance = 7f,
            points = new List<Vector2> { nearest, stationCenter }
        });
    }

    // ======================================================================
    // YUKSEKLIK
    // ======================================================================
    float[,] GenerateHeights(int res, Vector3 size)
    {
        float[,] h = new float[res, res];
        for (int z = 0; z < res; z++)
        {
            float v = (float)z / (res - 1);
            float wz = v * size.z;
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / (res - 1);
                float wx = u * size.x;

                // Yumusak tepeler (merkezlenmis)
                float hills = (Fbm(wx / hillScale + _noiseOffset.x, wz / hillScale + _noiseOffset.y, 4) - 0.5f) * 2f * hillHeight;
                // Detay
                float detail = (Fbm(wx / detailScale + _noiseOffset.x * 2f, wz / detailScale + _noiseOffset.y * 2f, 3) - 0.5f) * 2f * detailHeight;
                // Daglar (ridged) - kenara dogru yukselir
                float edge = EdgeMask(u, v);
                float mountains = Ridged(wx / mountainScale + _noiseOffset.x * 3f, wz / mountainScale + _noiseOffset.y * 3f, 4) * mountainHeight * edge;

                float val = baseLevel + hills + detail + mountains;
                h[z, x] = Mathf.Clamp01(val);
            }
        }
        return h;
    }

    // Merkezde 0 (duz vadi), kenara dogru 1 (daglar)
    float EdgeMask(float u, float v)
    {
        float ex = Mathf.Abs(u - 0.5f) * 2f;
        float ez = Mathf.Abs(v - 0.5f) * 2f;
        float e = Mathf.Max(ex, ez);
        e = Mathf.Clamp01((e - valleyRadius) / Mathf.Max(0.0001f, 1f - valleyRadius));
        e = Mathf.SmoothStep(0f, 1f, e);
        return Mathf.Pow(e, edgePower);
    }

    static float Fbm(float x, float y, int octaves)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
            norm += amp; amp *= 0.5f; freq *= 2f;
        }
        return sum / norm;
    }

    static float Ridged(float x, float y, int octaves)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float n = Mathf.PerlinNoise(x * freq, y * freq);
            n = 1f - Mathf.Abs(2f * n - 1f);
            n *= n;
            sum += amp * n;
            norm += amp; amp *= 0.5f; freq *= 2f;
        }
        return sum / norm;
    }

    // ======================================================================
    // YOLLAR
    // ======================================================================
    List<BakedRoad> BuildAllRoads(float[,] heights, int res, Vector3 size)
    {
        var list = new List<BakedRoad>();
        foreach (var road in roads)
        {
            if (road.points == null || road.points.Count < 2) continue;
            list.Add(new BakedRoad { def = road, samples = BuildRoadSamples(road, heights, res, size) });
        }
        return list;
    }

    // Polyline'i sik orneklere boler, taban yuksekligini orneskler, egimi yumusatir
    List<Vector3> BuildRoadSamples(RoadDef road, float[,] heights, int res, Vector3 size)
    {
        const float step = 2f; // metre
        var world = new List<Vector2>();
        for (int i = 0; i < road.points.Count - 1; i++)
        {
            Vector2 a = new Vector2(road.points[i].x * size.x, road.points[i].y * size.z);
            Vector2 b = new Vector2(road.points[i + 1].x * size.x, road.points[i + 1].y * size.z);
            float len = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(len / step));
            for (int j = 0; j < n; j++) world.Add(Vector2.Lerp(a, b, (float)j / n));
        }
        Vector2 last = new Vector2(road.points[road.points.Count - 1].x * size.x, road.points[road.points.Count - 1].y * size.z);
        world.Add(last);

        // taban yukseklik ornekle
        float[] hgt = new float[world.Count];
        for (int k = 0; k < world.Count; k++)
            hgt[k] = SampleHeight01(heights, res, world[k].x / size.x, world[k].y / size.z);

        // egimi yumusat (moving average)
        hgt = Smooth(hgt, roadSmoothWindow);

        var result = new List<Vector3>(world.Count);
        for (int k = 0; k < world.Count; k++)
            result.Add(new Vector3(world[k].x, Mathf.Clamp01(hgt[k] + roadHeightOffset), world[k].y));
        return result;
    }

    void CarveRoad(float[,] heights, int res, Vector3 size, BakedRoad road)
    {
        float half = road.def.width * 0.5f;
        float corridor = half + road.def.shoulder;
        float cellX = (res - 1) / size.x;
        float cellZ = (res - 1) / size.z;

        var s = road.samples;
        for (int i = 0; i < s.Count - 1; i++)
        {
            Vector3 a = s[i], b = s[i + 1];
            float minX = Mathf.Min(a.x, b.x) - corridor, maxX = Mathf.Max(a.x, b.x) + corridor;
            float minZ = Mathf.Min(a.z, b.z) - corridor, maxZ = Mathf.Max(a.z, b.z) + corridor;

            int ix0 = Mathf.Clamp(Mathf.FloorToInt(minX * cellX), 0, res - 1);
            int ix1 = Mathf.Clamp(Mathf.CeilToInt(maxX * cellX), 0, res - 1);
            int iz0 = Mathf.Clamp(Mathf.FloorToInt(minZ * cellZ), 0, res - 1);
            int iz1 = Mathf.Clamp(Mathf.CeilToInt(maxZ * cellZ), 0, res - 1);

            for (int z = iz0; z <= iz1; z++)
            {
                float wz = (float)z / (res - 1) * size.z;
                for (int x = ix0; x <= ix1; x++)
                {
                    float wx = (float)x / (res - 1) * size.x;
                    float t;
                    float d = PointSegDist(wx, wz, a.x, a.z, b.x, b.z, out t);
                    if (d > corridor) continue;

                    float target = Mathf.Lerp(a.y, b.y, t);
                    float blend;
                    if (d <= half) blend = 1f;
                    else blend = 1f - Mathf.SmoothStep(0f, 1f, (d - half) / road.def.shoulder);

                    if (blend <= 0f) continue;
                    heights[z, x] = Mathf.Lerp(heights[z, x], target, blend);
                }
            }
        }
    }

    void FlattenStation(float[,] heights, int res, Vector3 size)
    {
        float cx = stationCenter.x * size.x;
        float cz = stationCenter.y * size.z;
        float hx = stationSize.x * 0.5f;
        float hz = stationSize.y * 0.5f;
        float margin = stationFalloff;

        float target = Mathf.Clamp01(SampleHeight01(heights, res, stationCenter.x, stationCenter.y) + stationHeightOffset);

        float cellX = (res - 1) / size.x;
        float cellZ = (res - 1) / size.z;
        int ix0 = Mathf.Clamp(Mathf.FloorToInt((cx - hx - margin) * cellX), 0, res - 1);
        int ix1 = Mathf.Clamp(Mathf.CeilToInt((cx + hx + margin) * cellX), 0, res - 1);
        int iz0 = Mathf.Clamp(Mathf.FloorToInt((cz - hz - margin) * cellZ), 0, res - 1);
        int iz1 = Mathf.Clamp(Mathf.CeilToInt((cz + hz + margin) * cellZ), 0, res - 1);

        for (int z = iz0; z <= iz1; z++)
        {
            float wz = (float)z / (res - 1) * size.z;
            for (int x = ix0; x <= ix1; x++)
            {
                float wx = (float)x / (res - 1) * size.x;
                float dx = Mathf.Max(Mathf.Abs(wx - cx) - hx, 0f);
                float dz = Mathf.Max(Mathf.Abs(wz - cz) - hz, 0f);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float blend = d <= 0f ? 1f : (d < margin ? 1f - Mathf.SmoothStep(0f, 1f, d / margin) : 0f);
                if (blend <= 0f) continue;
                heights[z, x] = Mathf.Lerp(heights[z, x], target, blend);
            }
        }
    }

    // ======================================================================
    // DOKU BOYAMA
    // ======================================================================
    void PaintTextures(int res, Vector3 size, List<BakedRoad> baked)
    {
        var layers = new List<TerrainLayer>();
        int iGrass = -1, iDirt = -1, iRoad = -1;
        if (grassLayer) { iGrass = layers.Count; layers.Add(grassLayer); }
        if (dirtLayer) { iDirt = layers.Count; layers.Add(dirtLayer); }
        if (roadLayer) { iRoad = layers.Count; layers.Add(roadLayer); }
        if (layers.Count == 0) return;

        _data.terrainLayers = layers.ToArray();
        int aRes = _data.alphamapResolution;
        float[,,] maps = new float[aRes, aRes, layers.Count];

        // local: gercek hedef agirliklari mevcut slotlara yaz
        System.Action<int, int, float, float, float> WriteCell = (z, x, g, dirt, road) =>
        {
            if (iRoad < 0) { dirt += road; road = 0f; }
            if (iDirt < 0) { g += dirt; dirt = 0f; }
            float sum = g + dirt + road;
            if (sum <= 0f) { g = 1f; sum = 1f; }
            if (iGrass >= 0) maps[z, x, iGrass] = g / sum;
            if (iDirt >= 0) maps[z, x, iDirt] = dirt / sum;
            if (iRoad >= 0) maps[z, x, iRoad] = road / sum;
        };

        // 1) Taban: cim + dik yamaclarda toprak
        for (int z = 0; z < aRes; z++)
        {
            float v = (float)z / (aRes - 1);
            for (int x = 0; x < aRes; x++)
            {
                float u = (float)x / (aRes - 1);
                float steep = _data.GetSteepness(u, v); // derece
                float dirt = Mathf.Clamp01((steep - slopeDirtAngle) / 18f);
                WriteCell(z, x, 1f - dirt, dirt, 0f);
            }
        }

        // 2) Yollar (asfalt + cakil omuz)
        float cellX = (aRes - 1) / size.x;
        float cellZ = (aRes - 1) / size.z;
        foreach (var road in baked)
        {
            float half = road.def.width * 0.5f;
            float shoulder = road.def.shoulder;
            float corridor = half + shoulder;
            var s = road.samples;
            for (int i = 0; i < s.Count - 1; i++)
            {
                Vector3 a = s[i], b = s[i + 1];
                int ix0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, b.x) - corridor) * cellX), 0, aRes - 1);
                int ix1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x, b.x) + corridor) * cellX), 0, aRes - 1);
                int iz0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z, b.z) - corridor) * cellZ), 0, aRes - 1);
                int iz1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.z, b.z) + corridor) * cellZ), 0, aRes - 1);

                for (int z = iz0; z <= iz1; z++)
                {
                    float wz = (float)z / (aRes - 1) * size.z;
                    for (int x = ix0; x <= ix1; x++)
                    {
                        float wx = (float)x / (aRes - 1) * size.x;
                        float t;
                        float d = PointSegDist(wx, wz, a.x, a.z, b.x, b.z, out t);
                        if (d > corridor) continue;
                        if (d <= half) WriteCell(z, x, 0f, 0f, 1f);                         // asfalt
                        else
                        {
                            float k = (d - half) / shoulder;                                  // 0..1
                            float dirt = 1f - Mathf.SmoothStep(0f, 1f, k);                     // omuzda toprak
                            WriteCell(z, x, 1f - dirt, dirt, 0f);
                        }
                    }
                }
            }
        }

        // 3) Benzinlik zemini -> toprak/cakil
        {
            float cx = stationCenter.x * size.x, cz = stationCenter.y * size.z;
            float hx = stationSize.x * 0.5f, hz = stationSize.y * 0.5f, margin = stationFalloff;
            int ix0 = Mathf.Clamp(Mathf.FloorToInt((cx - hx - margin) * cellX), 0, aRes - 1);
            int ix1 = Mathf.Clamp(Mathf.CeilToInt((cx + hx + margin) * cellX), 0, aRes - 1);
            int iz0 = Mathf.Clamp(Mathf.FloorToInt((cz - hz - margin) * cellZ), 0, aRes - 1);
            int iz1 = Mathf.Clamp(Mathf.CeilToInt((cz + hz + margin) * cellZ), 0, aRes - 1);
            for (int z = iz0; z <= iz1; z++)
            {
                float wz = (float)z / (aRes - 1) * size.z;
                for (int x = ix0; x <= ix1; x++)
                {
                    float wx = (float)x / (aRes - 1) * size.x;
                    float dx = Mathf.Max(Mathf.Abs(wx - cx) - hx, 0f);
                    float dz = Mathf.Max(Mathf.Abs(wz - cz) - hz, 0f);
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    float blend = d <= 0f ? 1f : (d < margin ? 1f - Mathf.SmoothStep(0f, 1f, d / margin) : 0f);
                    if (blend <= 0f) continue;
                    float curRoad = iRoad >= 0 ? maps[z, x, iRoad] : 0f; // asfalti ezme
                    if (curRoad > 0.5f) continue;
                    WriteCell(z, x, 1f - blend, blend, 0f);
                }
            }
        }

        _data.SetAlphamaps(0, 0, maps);
    }

    // ======================================================================
    // AGACLAR
    // ======================================================================
    void PlaceTrees(int res, Vector3 size, List<BakedRoad> baked)
    {
        // Prototipleri kur
        var protos = new TreePrototype[treePrefabs.Length];
        for (int i = 0; i < treePrefabs.Length; i++)
            protos[i] = new TreePrototype { prefab = treePrefabs[i] };
        _data.treePrototypes = protos;
        _data.RefreshPrototypes();

        // Yol + benzinlik engel maskesi (hizli arama icin grid)
        const int gres = 256;
        bool[,] blocked = BuildBlockedGrid(gres, size, baked);

        var prng = new System.Random(seed * 7919 + 13);
        var instances = new List<TreeInstance>();

        int cols = Mathf.Max(1, Mathf.RoundToInt(size.x / treeSpacing));
        int rows = Mathf.Max(1, Mathf.RoundToInt(size.z / treeSpacing));

        for (int gz = 0; gz < rows; gz++)
        {
            for (int gx = 0; gx < cols; gx++)
            {
                float wx = (gx + 0.5f) * size.x / cols + ((float)prng.NextDouble() - 0.5f) * 2f * treeJitter;
                float wz = (gz + 0.5f) * size.z / rows + ((float)prng.NextDouble() - 0.5f) * 2f * treeJitter;
                float u = wx / size.x, v = wz / size.z;
                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;

                // engel (yol/benzinlik)
                int bx = Mathf.Clamp(Mathf.RoundToInt(u * (gres - 1)), 0, gres - 1);
                int bz = Mathf.Clamp(Mathf.RoundToInt(v * (gres - 1)), 0, gres - 1);
                if (blocked[bz, bx]) continue;

                // egim
                if (_data.GetSteepness(u, v) > maxTreeSlope) continue;

                // orman aciklik maskesi
                float clear = Mathf.PerlinNoise((wx + _noiseOffset.x * 5f) / clearingScale, (wz + _noiseOffset.y * 5f) / clearingScale);
                if (clear < clearingThreshold) continue;

                // yogunluk
                if (prng.NextDouble() > treeDensity) continue;

                float hNorm = SampleInterpolatedHeight01(u, v);
                float scale = Mathf.Lerp(treeScaleRange.x, treeScaleRange.y, (float)prng.NextDouble());

                instances.Add(new TreeInstance
                {
                    position = new Vector3(u, hNorm, v),
                    prototypeIndex = prng.Next(0, treePrefabs.Length),
                    widthScale = scale,
                    heightScale = scale * Mathf.Lerp(0.95f, 1.15f, (float)prng.NextDouble()),
                    rotation = (float)prng.NextDouble() * Mathf.PI * 2f,
                    color = Color.white,
                    lightmapColor = Color.white
                });
            }
        }

        _data.SetTreeInstances(instances.ToArray(), true);
        Debug.Log($"[HorrorTerrain] {instances.Count} agac yerlestirildi.");
    }

    bool[,] BuildBlockedGrid(int gres, Vector3 size, List<BakedRoad> baked)
    {
        bool[,] blocked = new bool[gres, gres];
        float cellX = (gres - 1) / size.x;
        float cellZ = (gres - 1) / size.z;

        foreach (var road in baked)
        {
            float radius = road.def.width * 0.5f + road.def.treeClearance;
            var s = road.samples;
            for (int i = 0; i < s.Count - 1; i++)
            {
                Vector3 a = s[i], b = s[i + 1];
                int ix0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, b.x) - radius) * cellX), 0, gres - 1);
                int ix1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x, b.x) + radius) * cellX), 0, gres - 1);
                int iz0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z, b.z) - radius) * cellZ), 0, gres - 1);
                int iz1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.z, b.z) + radius) * cellZ), 0, gres - 1);
                for (int z = iz0; z <= iz1; z++)
                {
                    float wz = (float)z / (gres - 1) * size.z;
                    for (int x = ix0; x <= ix1; x++)
                    {
                        if (blocked[z, x]) continue;
                        float wx = (float)x / (gres - 1) * size.x;
                        float t;
                        if (PointSegDist(wx, wz, a.x, a.z, b.x, b.z, out t) <= radius) blocked[z, x] = true;
                    }
                }
            }
        }

        // benzinlik
        float cx = stationCenter.x * size.x, cz = stationCenter.y * size.z;
        float hx = stationSize.x * 0.5f + stationTreeClearance;
        float hz = stationSize.y * 0.5f + stationTreeClearance;
        int sx0 = Mathf.Clamp(Mathf.FloorToInt((cx - hx) * cellX), 0, gres - 1);
        int sx1 = Mathf.Clamp(Mathf.CeilToInt((cx + hx) * cellX), 0, gres - 1);
        int sz0 = Mathf.Clamp(Mathf.FloorToInt((cz - hz) * cellZ), 0, gres - 1);
        int sz1 = Mathf.Clamp(Mathf.CeilToInt((cz + hz) * cellZ), 0, gres - 1);
        for (int z = sz0; z <= sz1; z++)
            for (int x = sx0; x <= sx1; x++)
                blocked[z, x] = true;

        return blocked;
    }

    // ======================================================================
    // YARDIMCILAR
    // ======================================================================
    static float[] Smooth(float[] src, int window)
    {
        if (window <= 1 || src.Length == 0) return src;
        int half = window / 2;
        float[] dst = new float[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            float sum = 0f; int cnt = 0;
            for (int j = -half; j <= half; j++)
            {
                int k = i + j;
                if (k < 0 || k >= src.Length) continue;
                sum += src[k]; cnt++;
            }
            dst[i] = sum / cnt;
        }
        return dst;
    }

    static float PointSegDist(float px, float pz, float ax, float az, float bx, float bz, out float t)
    {
        float abx = bx - ax, abz = bz - az;
        float len2 = abx * abx + abz * abz;
        t = len2 > 1e-6f ? Mathf.Clamp01(((px - ax) * abx + (pz - az) * abz) / len2) : 0f;
        float cx = ax + abx * t, cz = az + abz * t;
        float dx = px - cx, dz = pz - cz;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static float SampleHeight01(float[,] h, int res, float u, float v)
    {
        u = Mathf.Clamp01(u); v = Mathf.Clamp01(v);
        float fx = u * (res - 1), fz = v * (res - 1);
        int x0 = (int)fx, z0 = (int)fz;
        int x1 = Mathf.Min(x0 + 1, res - 1), z1 = Mathf.Min(z0 + 1, res - 1);
        float tx = fx - x0, tz = fz - z0;
        float a = Mathf.Lerp(h[z0, x0], h[z0, x1], tx);
        float b = Mathf.Lerp(h[z1, x0], h[z1, x1], tx);
        return Mathf.Lerp(a, b, tz);
    }

    float SampleInterpolatedHeight01(float u, float v)
    {
        // SetHeights sonrasi guncel terrain'den oku (normalize)
        float worldH = _data.GetInterpolatedHeight(u, v);
        return Mathf.Clamp01(worldH / Mathf.Max(0.0001f, _data.size.y));
    }

    void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(_data);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}


