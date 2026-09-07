#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Forest Road Vegetation Placer PRO
///
/// Amaç:
/// - Collider kullanmadan Road Root altındaki MeshFilter'lardan yol footprint'i çıkarır.
/// - Yol çevresini boş bırakmamak için Terrain Detail System ile çok yoğun grass/fern üretir.
/// - Rock / büyük shrub / log gibi objeleri GameObject olarak daha seyrek dağıtır.
/// - Yol mesh'i çok yüksek poly olsa bile bir kez raster road mask + distance field üretir.
/// - Runtime'da grass/fern binlerce ayrı GameObject olmadığı için çok daha performanslıdır.
///
/// Menü:
/// Tools > Forest Road > Vegetation Placer PRO
///
/// Not:
/// Terrain Detail olarak kullanılacak prefabların hafif olması önerilir:
/// birkaç quad / düşük poly fern / grass clump.
/// </summary>
public class ForestRoadVegetationPlacer : EditorWindow
{
    public enum SpawnMode
    {
        TerrainDetail = 0,
        GameObject = 1
    }

    public enum DetailCoverageArea
    {
        RoadBand = 0,
        EntireTerrainOutsideRoad = 1
    }

    [Serializable]
    public class SpawnItem
    {
        public bool enabled = true;
        public SpawnMode mode = SpawnMode.TerrainDetail;
        public GameObject prefab;

        [Header("Roadside Corridor")]
        // Eski serialized değerlerle uyumluluk için enum tutuluyor,
        // fakat PRO sürüm sadece RoadBand üretir.
        public DetailCoverageArea detailCoverageArea = DetailCoverageArea.RoadBand;

        [Min(0f)] public float minRoadDistance = 0.08f;
        [Min(0.01f)] public float maxRoadDistance = 26f;
        [Range(0.1f, 8f)] public float nearRoadBias = 1.15f;

        [Tooltip("Bu prefabın otomatik vegetation karışımındaki ağırlığı. Örn Grass 60, Fern 28, Ground Plant 12.")]
        [Range(1f, 100f)] public float mixWeightPercent = 60f;

        // Legacy alanlar: eski sahne verisi bozulmasın diye tutuluyor.
        [HideInInspector] public float edgeBoostDistance = 14f;
        [HideInInspector] public float edgeDensityBoost = 1.25f;

        [Header("Terrain")]
        [Range(0f, 89f)] public float minSlope = 0f;
        [Range(0f, 89f)] public float maxSlope = 78f;

        [Header("Dense Terrain Detail Coverage")]
        [Range(1, 255)] public int coveragePerCell = 220;
        [Range(0, 255)] public int minimumCoveragePerCell = 150;
        [Range(0f, 1f)] public float densityRandomness = 0.12f;

        [Tooltip("CoverageMode prototype density. 1 genellikle iyi başlangıçtır.")]
        [Range(0.05f, 2f)] public float prototypeDensity = 1f;

        [Range(0f, 1f)] public float targetCoverage = 1f;

        [Tooltip("0 düzenli, 100 çok rastgele.")]
        [Range(0f, 100f)] public float positionJitter = 90f;

        [Tooltip("Detail mesh'in eğime ne kadar hizalanacağı.")]
        [Range(0f, 1f)] public float detailAlignToGround = 0.65f;

        [Header("Automatic Random Size")]
        [Tooltip("Terrain her instance için Min-Max aralığından farklı boy seçer.")]
        public bool autoRandomSize = true;

        [Tooltip("Öneri: Grass 3-5.5, Fern 3.5-6.5. Bunlar Terrain Detail size değerleridir.")]
        public Vector2 randomSizeRange = new Vector2(3f, 5.5f);

        [Tooltip("Width ve Height arasında küçük doğal fark oluşturur.")]
        [Range(0f, 0.35f)] public float shapeVariation = 0.12f;

        // Legacy alanlar: eski serialized window state için tutuluyor.
        [HideInInspector] public Vector2 detailWidth = new Vector2(3f, 5.5f);
        [HideInInspector] public Vector2 detailHeight = new Vector2(3f, 5.5f);
        [HideInInspector] public float randomSizeVariation = 0.50f;

        [Header("GameObject Amount")]
        [Min(0)] public int amount = 250;
        [Min(0f)] public float minSpacing = 1.5f;

        [Header("GameObject Transform")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.25f);
        public Vector2 yOffset = new Vector2(-0.03f, 0.02f);
        public bool randomYRotation = true;
        [Range(0f, 30f)] public float randomTilt = 4f;
        public bool alignToTerrain = false;
        [Range(0f, 1f)] public float terrainAlignStrength = 0.65f;

        [Header("Natural Distribution")]
        public bool useClustering = true;
        [Min(0.1f)] public float clusterSize = 10f;
        [Range(0f, 1f)] public float clusterStrength = 0.35f;
        public Vector2 noiseOffset = Vector2.zero;

        [Header("Runtime Optimization")]
        public bool disableGameObjectColliders = true;
        public bool markGameObjectsStatic = true;

        [NonSerialized] public bool foldout = true;
    }

    private struct RoadTriangle
    {
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;
        public Rect bounds;

        public RoadTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;

            float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            float maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));

            bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }

    [SerializeField] private Terrain terrain;
    [SerializeField] private GameObject roadRoot;
    [SerializeField] private Transform generatedParent;

    [Header("Global")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private float extraRoadClearance = 0.04f;

    [Tooltip("Hiçbir TerrainDetail bunun ötesine yerleşmez. Oyuncunun görmeyeceği alanlar boş kalır.")]
    [SerializeField] private float globalRoadsideCorridorWidth = 28f;

    [Tooltip("Eğimli yol kenarlarında vegetation yoğunluğunu artırır.")]
    [SerializeField] private bool boostEmbankmentSlopes = true;

    [SerializeField] private float slopeBoostStartAngle = 4f;
    [SerializeField] private float slopeDensityBoost = 1.30f;

    [Tooltip("Grass/Fern/Ground Plant ağırlıklarını otomatik normalize eder.")]
    [SerializeField] private bool normalizeDetailMixWeights = true;

    [SerializeField] private float globalDetailDensity = 1.0f;
    [SerializeField] private float globalGameObjectDensity = 1.0f;
    [SerializeField] private int gameObjectAttemptsMultiplier = 30;

    [Header("Terrain Detail Runtime")]
    [SerializeField] private bool forceCoverageMode = true;
    [SerializeField] private float terrainDetailDensityScale = 1.0f;
    [SerializeField] private float terrainDetailDistance = 180f;

    [Header("Dense Detail Resolution")]
    [SerializeField] private int denseDetailResolution = 2048;
    [SerializeField] private int denseResolutionPerPatch = 16;

    [Header("Road Analysis")]
    [SerializeField] private bool useTerrainDetailResolutionForAnalysis = true;
    [SerializeField] private int customAnalysisResolution = 1024;

    [Header("Generation")]
    [SerializeField] private bool clearSelectedDetailLayersBeforeGenerate = true;
    [SerializeField] private bool clearGeneratedGameObjectsBeforeGenerate = true;

    [SerializeField] private List<SpawnItem> items = new List<SpawnItem>();

    private Vector2 scroll;

    [SerializeField] private bool showSceneSetup = true;
    [SerializeField] private bool showGlobalSettings = true;
    [SerializeField] private bool showVegetationLayers = true;

    private readonly List<RoadTriangle> roadTriangles = new List<RoadTriangle>();

    // Road raster cache
    private bool[,] roadMask;
    private float[,] roadDistance;
    private int analysisWidth;
    private int analysisHeight;
    private float analysisCellSizeX;
    private float analysisCellSizeZ;
    private Rect roadWorldBounds;
    private bool roadDataReady = false;

    // Slope is calculated lazily and cached per analysis cell.
    private float[,] slopeCache;
    private bool[,] slopeCached;

    [MenuItem("Tools/Forest Road/Vegetation Placer PRO")]
    public static void Open()
    {
        ForestRoadVegetationPlacer window =
            GetWindow<ForestRoadVegetationPlacer>("Road Vegetation PRO");

        window.minSize = new Vector2(760f, 680f);

        Rect p = window.position;

        if (p.width < 900f)
            p.width = 900f;

        if (p.height < 760f)
            p.height = 760f;

        window.position = p;
        window.Show();
    }

    private void OnEnable()
    {
        if (items == null)
            items = new List<SpawnItem>();

        UpgradeLegacyItemsToRoadsideOnly();
    }

    private void OnGUI()
    {
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        float oldFieldWidth = EditorGUIUtility.fieldWidth;

        // Responsive editor layout: labels no longer eat the whole window.
        EditorGUIUtility.labelWidth =
            Mathf.Clamp(
                position.width * 0.31f,
                185f,
                285f);

        EditorGUIUtility.fieldWidth =
            Mathf.Clamp(
                position.width * 0.20f,
                100f,
                180f);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space(8);

        GUIStyle titleStyle =
            new GUIStyle(EditorStyles.boldLabel);

        titleStyle.fontSize = 16;

        EditorGUILayout.LabelField(
            "Forest Road Vegetation Placer PRO",
            titleStyle);

        EditorGUILayout.HelpBox(
            "Yoğun grass / fern Terrain Detail + CoverageMode ile çizilir. " +
            "Pencere artık tek scroll kullanır; bölümleri açıp kapatabilirsin. " +
            "Auto Random Size açıkken ot/fern boyları instance bazında farklılaşır.",
            MessageType.Info);

        EditorGUILayout.Space(4);

        // -------------------------------------------------------------
        // SCENE SETUP
        // -------------------------------------------------------------
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        showSceneSetup =
            EditorGUILayout.Foldout(
                showSceneSetup,
                "1. SCENE SETUP / ROAD",
                true,
                EditorStyles.foldoutHeader);

        if (showSceneSetup)
        {
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            terrain = (Terrain)EditorGUILayout.ObjectField(
                "Terrain",
                terrain,
                typeof(Terrain),
                true);

            roadRoot = (GameObject)EditorGUILayout.ObjectField(
                "Road Root",
                roadRoot,
                typeof(GameObject),
                true);

            if (EditorGUI.EndChangeCheck())
                InvalidateRoadCache();

            generatedParent = (Transform)EditorGUILayout.ObjectField(
                "Generated Parent",
                generatedParent,
                typeof(Transform),
                true);

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                "AUTO FIND TERRAIN",
                GUILayout.Height(28f)))
            {
                terrain = Terrain.activeTerrain;
                InvalidateRoadCache();
            }

            if (GUILayout.Button(
                "ROAD ANALYZE / CACHE",
                GUILayout.Height(28f)))
            {
                AnalyzeRoadWithDialog();
            }

            EditorGUILayout.EndHorizontal();

            if (roadDataReady)
            {
                EditorGUILayout.HelpBox(
                    "Road cache hazır  |  Triangles: " +
                    roadTriangles.Count +
                    "  |  Mask: " +
                    analysisWidth + " x " + analysisHeight +
                    "  |  Cell: " +
                    analysisCellSizeX.ToString("0.00") +
                    " x " +
                    analysisCellSizeZ.ToString("0.00") +
                    " m",
                    MessageType.None);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // -------------------------------------------------------------
        // GLOBAL SETTINGS
        // -------------------------------------------------------------
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        showGlobalSettings =
            EditorGUILayout.Foldout(
                showGlobalSettings,
                "2. GLOBAL / PERFORMANCE",
                true,
                EditorStyles.foldoutHeader);

        if (showGlobalSettings)
        {
            EditorGUI.indentLevel++;
            DrawGlobalSettings();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // -------------------------------------------------------------
        // VEGETATION
        // -------------------------------------------------------------
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        showVegetationLayers =
            EditorGUILayout.Foldout(
                showVegetationLayers,
                "3. VEGETATION LAYERS",
                true,
                EditorStyles.foldoutHeader);

        if (showVegetationLayers)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox(
                "Grass taban katmanı için EntireTerrainOutsideRoad; Fern için RoadBand iyi başlangıçtır. " +
                "Birden fazla grass / fern prefabı eklersen doğal karışım çok daha iyi görünür.",
                MessageType.None);

            for (int i = 0; i < items.Count; i++)
                DrawItem(i);

            EditorGUILayout.Space(6);
            DrawPresetButtons();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.68f, 1f, 0.68f);

        if (GUILayout.Button(
            "GENERATE / FILL VEGETATION",
            GUILayout.Height(46f)))
        {
            GenerateAll();
        }

        GUI.backgroundColor =
            new Color(1f, 0.76f, 0.72f);

        if (GUILayout.Button(
            "CLEAR SELECTED VEGETATION",
            GUILayout.Height(36f)))
        {
            ClearSelectedVegetation(true);
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(12);
        EditorGUILayout.EndScrollView();

        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUIUtility.fieldWidth = oldFieldWidth;
    }

    private void DrawGlobalSettings()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

        seed = EditorGUILayout.IntField("Seed", seed);

        extraRoadClearance = EditorGUILayout.Slider(
            "Road Edge Clearance",
            extraRoadClearance,
            0f,
            2f);

        globalRoadsideCorridorWidth = EditorGUILayout.Slider(
            "Roadside Corridor Width",
            globalRoadsideCorridorWidth,
            5f,
            60f);

        boostEmbankmentSlopes = EditorGUILayout.Toggle(
            "Boost Embankment Slopes",
            boostEmbankmentSlopes);

        if (boostEmbankmentSlopes)
        {
            slopeBoostStartAngle = EditorGUILayout.Slider(
                "Slope Boost Starts",
                slopeBoostStartAngle,
                0f,
                35f);

            slopeDensityBoost = EditorGUILayout.Slider(
                "Slope Density Boost",
                slopeDensityBoost,
                1f,
                2.5f);
        }

        normalizeDetailMixWeights = EditorGUILayout.Toggle(
            "Normalize Auto Mix Weights",
            normalizeDetailMixWeights);

        globalDetailDensity = EditorGUILayout.Slider(
            "Global Detail Coverage",
            globalDetailDensity,
            0.10f,
            2.0f);

        globalGameObjectDensity = EditorGUILayout.Slider(
            "Global GO Density",
            globalGameObjectDensity,
            0.10f,
            3f);

        gameObjectAttemptsMultiplier = EditorGUILayout.IntSlider(
            "GO Attempts",
            gameObjectAttemptsMultiplier,
            5,
            100);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(
            "Terrain Detail Runtime",
            EditorStyles.miniBoldLabel);

        forceCoverageMode = EditorGUILayout.Toggle(
            "Force Coverage Mode",
            forceCoverageMode);

        terrainDetailDensityScale = EditorGUILayout.Slider(
            "Terrain Detail Density",
            terrainDetailDensityScale,
            0.10f,
            1.0f);

        terrainDetailDistance = EditorGUILayout.Slider(
            "Detail Draw Distance",
            terrainDetailDistance,
            30f,
            400f);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
            "APPLY TERRAIN SETTINGS",
            GUILayout.Height(27f)))
        {
            ApplyTerrainDetailRuntimeSettings();
        }

        if (GUILayout.Button(
            "PRO ROADSIDES PRESET",
            GUILayout.Height(27f)))
        {
            ApplyUltraDenseSettingsToActiveItems();
        }

        if (GUILayout.Button(
            "AUTO RANDOM SIZES",
            GUILayout.Height(27f)))
        {
            ApplyNaturalSizeVariationToActiveItems();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(
            "Dense Detail Resolution",
            EditorStyles.miniBoldLabel);

        denseDetailResolution = EditorGUILayout.IntPopup(
            "Target Detail Resolution",
            denseDetailResolution,
            new string[] { "1024", "2048 (Recommended)", "4096 (Heavy)" },
            new int[] { 1024, 2048, 4096 });

        denseResolutionPerPatch = EditorGUILayout.IntPopup(
            "Resolution Per Patch",
            denseResolutionPerPatch,
            new string[] { "8", "16 (Recommended)", "32", "64" },
            new int[] { 8, 16, 32, 64 });

        if (GUILayout.Button("SET DENSE DETAIL RESOLUTION"))
            ApplyDenseDetailResolution();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(
            "Road Analysis Resolution",
            EditorStyles.miniBoldLabel);

        useTerrainDetailResolutionForAnalysis =
            EditorGUILayout.Toggle(
                "Use Terrain Detail Resolution",
                useTerrainDetailResolutionForAnalysis);

        if (!useTerrainDetailResolutionForAnalysis)
        {
            customAnalysisResolution = EditorGUILayout.IntPopup(
                "Custom Resolution",
                customAnalysisResolution,
                new string[] { "512", "1024", "2048" },
                new int[] { 512, 1024, 2048 });
        }

        clearSelectedDetailLayersBeforeGenerate =
            EditorGUILayout.Toggle(
                "Overwrite Selected Detail Layers",
                clearSelectedDetailLayersBeforeGenerate);

        clearGeneratedGameObjectsBeforeGenerate =
            EditorGUILayout.Toggle(
                "Clear Previous GO Groups",
                clearGeneratedGameObjectsBeforeGenerate);

        if (terrain != null && terrain.terrainData != null)
        {
            TerrainData td = terrain.terrainData;

            float cellX =
                td.detailWidth > 0
                    ? td.size.x / td.detailWidth
                    : 0f;

            float cellZ =
                td.detailHeight > 0
                    ? td.size.z / td.detailHeight
                    : 0f;

            EditorGUILayout.HelpBox(
                "Terrain Detail Resolution: " +
                td.detailWidth + " x " + td.detailHeight +
                "\nDetail Cell: " +
                cellX.ToString("0.00") + " x " +
                cellZ.ToString("0.00") + " m" +
                "\nTerrain Density: " +
                terrain.detailObjectDensity.ToString("0.00") +
                " | Distance: " +
                terrain.detailObjectDistance.ToString("0") + " m",
                MessageType.None);

            if (Mathf.Max(cellX, cellZ) > 2.0f)
            {
                EditorGUILayout.HelpBox(
                    "Detail hücrelerinden biri 2 metreden büyük. Bu, küçük grass prefablarında " +
                    "seyrek / ada ada görünüm yaratabilir. 2048 detail resolution önerilir.",
                    MessageType.Warning);
            }
        }
    }

    private void DrawItem(int index)
    {
        SpawnItem item = items[index];

        if (item == null)
        {
            item = CreateDenseGrassPreset(index);
            items[index] = item;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        item.enabled = EditorGUILayout.Toggle(
            item.enabled,
            GUILayout.Width(20f));

        string title =
            item.prefab != null
                ? item.prefab.name
                : "Vegetation " + (index + 1);

        item.foldout = EditorGUILayout.Foldout(
            item.foldout,
            title + "  [" + item.mode + "]",
            true,
            EditorStyles.foldoutHeader);

        if (GUILayout.Button("X", GUILayout.Width(28f)))
        {
            items.RemoveAt(index);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();

        if (item.foldout)
        {
            EditorGUI.indentLevel++;

            item.mode = (SpawnMode)EditorGUILayout.EnumPopup(
                "Mode",
                item.mode);

            item.prefab = (GameObject)EditorGUILayout.ObjectField(
                "Prefab",
                item.prefab,
                typeof(GameObject),
                false);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(
                "Roadside Corridor",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.HelpBox(
                "Bu sürüm TerrainDetail prefablarını sadece yol boyunca belirlenen corridor içinde üretir. " +
                "Haritanın geri kalanı boş bırakılır.",
                MessageType.None);

            // Eski EntireTerrain state'ini burada da zorla kapat.
            if (item.mode == SpawnMode.TerrainDetail)
                item.detailCoverageArea = DetailCoverageArea.RoadBand;

            item.minRoadDistance = Mathf.Max(
                0f,
                EditorGUILayout.FloatField(
                    "Min Road Distance",
                    item.minRoadDistance));

            item.maxRoadDistance = Mathf.Clamp(
                EditorGUILayout.FloatField(
                    "Max Road Distance",
                    item.maxRoadDistance),
                item.minRoadDistance + 0.01f,
                globalRoadsideCorridorWidth);

            item.nearRoadBias = EditorGUILayout.Slider(
                "Near Road Bias",
                item.nearRoadBias,
                0.1f,
                8f);

            if (item.mode == SpawnMode.TerrainDetail)
            {
                item.mixWeightPercent = EditorGUILayout.Slider(
                    "Auto Mix Weight (%)",
                    item.mixWeightPercent,
                    1f,
                    100f);
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(
                "Slope / Embankment",
                EditorStyles.miniBoldLabel);

            item.minSlope = EditorGUILayout.Slider(
                "Min Slope",
                item.minSlope,
                0f,
                89f);

            item.maxSlope = EditorGUILayout.Slider(
                "Max Slope",
                item.maxSlope,
                0f,
                89f);

            if (item.mode == SpawnMode.TerrainDetail)
                DrawDetailSettings(item);
            else
                DrawGameObjectSettings(item);

            DrawNaturalSettings(item);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDetailSettings(SpawnItem item)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            "Dense Terrain Detail",
            EditorStyles.miniBoldLabel);

        item.coveragePerCell = EditorGUILayout.IntSlider(
            "Coverage / Cell",
            item.coveragePerCell,
            1,
            255);

        item.minimumCoveragePerCell = EditorGUILayout.IntSlider(
            "Minimum Coverage",
            item.minimumCoveragePerCell,
            0,
            255);

        item.densityRandomness = EditorGUILayout.Slider(
            "Coverage Randomness",
            item.densityRandomness,
            0f,
            1f);

        item.prototypeDensity = EditorGUILayout.Slider(
            "Prototype Density",
            item.prototypeDensity,
            0.05f,
            2f);

        item.targetCoverage = EditorGUILayout.Slider(
            "Target Coverage",
            item.targetCoverage,
            0f,
            1f);

        item.positionJitter = EditorGUILayout.Slider(
            "Position Jitter",
            item.positionJitter,
            0f,
            100f);

        item.detailAlignToGround = EditorGUILayout.Slider(
            "Align To Ground",
            item.detailAlignToGround,
            0f,
            1f);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            "Automatic Random Size",
            EditorStyles.miniBoldLabel);

        item.autoRandomSize = EditorGUILayout.Toggle(
            "Auto Random Size",
            item.autoRandomSize);

        item.randomSizeRange = EditorGUILayout.Vector2Field(
            "Random Size Min / Max",
            item.randomSizeRange);

        item.randomSizeRange.x =
            Mathf.Max(0.10f, item.randomSizeRange.x);

        item.randomSizeRange.y =
            Mathf.Max(
                item.randomSizeRange.x + 0.05f,
                item.randomSizeRange.y);

        item.shapeVariation = EditorGUILayout.Slider(
            "Shape Variation",
            item.shapeVariation,
            0f,
            0.35f);

        EditorGUILayout.HelpBox(
            "Örn Grass = 3.0 - 5.5, Fern = 3.5 - 6.5. Unity Terrain Detail her instance için " +
            "Min/Max aralığından farklı width/height seçer; bu yüzden aynı-boy halı görünümü kırılır.",
            MessageType.None);
    }

    private void DrawGameObjectSettings(SpawnItem item)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            "Sparse GameObjects",
            EditorStyles.miniBoldLabel);

        item.amount = Mathf.Max(
            0,
            EditorGUILayout.IntField(
                "Amount",
                item.amount));

        item.minSpacing = Mathf.Max(
            0f,
            EditorGUILayout.FloatField(
                "Min Spacing",
                item.minSpacing));

        item.scaleRange = EditorGUILayout.Vector2Field(
            "Scale Min / Max",
            item.scaleRange);

        item.yOffset = EditorGUILayout.Vector2Field(
            "Y Offset Min / Max",
            item.yOffset);

        item.randomYRotation = EditorGUILayout.Toggle(
            "Random Y Rotation",
            item.randomYRotation);

        item.randomTilt = EditorGUILayout.Slider(
            "Random Tilt",
            item.randomTilt,
            0f,
            30f);

        item.alignToTerrain = EditorGUILayout.Toggle(
            "Align To Terrain",
            item.alignToTerrain);

        if (item.alignToTerrain)
        {
            item.terrainAlignStrength =
                EditorGUILayout.Slider(
                    "Align Strength",
                    item.terrainAlignStrength,
                    0f,
                    1f);
        }

        item.disableGameObjectColliders =
            EditorGUILayout.Toggle(
                "Disable Colliders",
                item.disableGameObjectColliders);

        item.markGameObjectsStatic =
            EditorGUILayout.Toggle(
                "Mark Static",
                item.markGameObjectsStatic);
    }

    private void DrawNaturalSettings(SpawnItem item)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            "Natural Distribution",
            EditorStyles.miniBoldLabel);

        item.useClustering = EditorGUILayout.Toggle(
            "Use Clustering",
            item.useClustering);

        if (item.useClustering)
        {
            item.clusterSize = Mathf.Max(
                0.1f,
                EditorGUILayout.FloatField(
                    "Cluster Size",
                    item.clusterSize));

            item.clusterStrength = EditorGUILayout.Slider(
                "Cluster Strength",
                item.clusterStrength,
                0f,
                1f);

            item.noiseOffset = EditorGUILayout.Vector2Field(
                "Noise Offset",
                item.noiseOffset);
        }
    }

    private void DrawPresetButtons()
    {
        EditorGUILayout.LabelField(
            "Quick Presets",
            EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Dense Grass"))
            items.Add(CreateDenseGrassPreset(items.Count));

        if (GUILayout.Button("+ Dense Fern"))
            items.Add(CreateDenseFernPreset(items.Count));

        if (GUILayout.Button("+ Ground Plant"))
            items.Add(CreateGroundPlantPreset(items.Count));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Shrub GO"))
            items.Add(CreateShrubPreset(items.Count));

        if (GUILayout.Button("+ Rock GO"))
            items.Add(CreateRockPreset(items.Count));

        if (GUILayout.Button("+ Log GO"))
            items.Add(CreateLogPreset(items.Count));

        EditorGUILayout.EndHorizontal();
    }

    private SpawnItem CreateDenseGrassPreset(int index)
    {
        return new SpawnItem
        {
            mode = SpawnMode.TerrainDetail,
            detailCoverageArea = DetailCoverageArea.RoadBand,

            minRoadDistance = 0.06f,
            maxRoadDistance = 26f,
            nearRoadBias = 1.10f,
            mixWeightPercent = 60f,

            minSlope = 0f,
            maxSlope = 84f,

            coveragePerCell = 245,
            minimumCoveragePerCell = 180,
            densityRandomness = 0.18f,
            prototypeDensity = 1.10f,
            targetCoverage = 1f,
            positionJitter = 96f,
            detailAlignToGround = 0.72f,

            autoRandomSize = true,
            randomSizeRange = new Vector2(3.0f, 5.5f),
            shapeVariation = 0.14f,

            detailWidth = new Vector2(3.0f, 5.5f),
            detailHeight = new Vector2(3.0f, 5.5f),

            useClustering = true,
            clusterSize = 6f,
            clusterStrength = 0.15f,
            noiseOffset = new Vector2(
                index * 31.7f,
                index * 17.3f)
        };
    }

    private SpawnItem CreateDenseFernPreset(int index)
    {
        return new SpawnItem
        {
            mode = SpawnMode.TerrainDetail,
            detailCoverageArea = DetailCoverageArea.RoadBand,

            minRoadDistance = 0.18f,
            maxRoadDistance = 24f,
            nearRoadBias = 1.00f,
            mixWeightPercent = 28f,

            minSlope = 2f,
            maxSlope = 84f,

            coveragePerCell = 205,
            minimumCoveragePerCell = 28,
            densityRandomness = 0.32f,
            prototypeDensity = 1.0f,
            targetCoverage = 0.92f,
            positionJitter = 98f,
            detailAlignToGround = 0.76f,

            autoRandomSize = true,
            randomSizeRange = new Vector2(3.5f, 6.5f),
            shapeVariation = 0.16f,

            detailWidth = new Vector2(3.5f, 6.5f),
            detailHeight = new Vector2(3.5f, 6.5f),

            useClustering = true,
            clusterSize = 9f,
            clusterStrength = 0.58f,
            noiseOffset = new Vector2(
                100f + index * 23.1f,
                240f + index * 11.9f)
        };
    }

    private SpawnItem CreateGroundPlantPreset(int index)
    {
        return new SpawnItem
        {
            mode = SpawnMode.TerrainDetail,
            detailCoverageArea = DetailCoverageArea.RoadBand,

            minRoadDistance = 0.12f,
            maxRoadDistance = 22f,
            nearRoadBias = 0.95f,
            mixWeightPercent = 12f,

            minSlope = 0f,
            maxSlope = 86f,

            coveragePerCell = 175,
            minimumCoveragePerCell = 18,
            densityRandomness = 0.38f,
            prototypeDensity = 0.90f,
            targetCoverage = 0.84f,
            positionJitter = 99f,
            detailAlignToGround = 0.78f,

            autoRandomSize = true,
            randomSizeRange = new Vector2(2.8f, 5.2f),
            shapeVariation = 0.18f,

            detailWidth = new Vector2(2.8f, 5.2f),
            detailHeight = new Vector2(2.8f, 5.2f),

            useClustering = true,
            clusterSize = 13f,
            clusterStrength = 0.52f,
            noiseOffset = new Vector2(
                430f + index * 19.7f,
                120f + index * 29.2f)
        };
    }

    private SpawnItem CreateShrubPreset(int index)
    {
        return new SpawnItem
        {
            mode = SpawnMode.GameObject,
            minRoadDistance = 1.0f,
            maxRoadDistance = 22f,
            nearRoadBias = 1.0f,
            minSlope = 0f,
            maxSlope = 58f,

            amount = 650,
            minSpacing = 1.8f,
            scaleRange = new Vector2(0.75f, 1.35f),
            yOffset = new Vector2(-0.05f, 0.02f),
            randomTilt = 4f,

            useClustering = true,
            clusterSize = 18f,
            clusterStrength = 0.45f,
            noiseOffset = new Vector2(
                300f + index * 13.7f,
                600f + index * 22.1f),

            disableGameObjectColliders = true,
            markGameObjectsStatic = true
        };
    }

    private SpawnItem CreateRockPreset(int index)
    {
        return new SpawnItem
        {
            mode = SpawnMode.GameObject,
            minRoadDistance = 0.6f,
            maxRoadDistance = 18f,
            nearRoadBias = 1.15f,
            minSlope = 0f,
            maxSlope = 70f,

            amount = 220,
            minSpacing = 2.5f,
            scaleRange = new Vector2(0.55f, 1.45f),
            yOffset = new Vector2(-0.12f, -0.02f),
            randomTilt = 14f,
            alignToTerrain = true,
            terrainAlignStrength = 0.85f,

            useClustering = true,
            clusterSize = 20f,
            clusterStrength = 0.45f,
            noiseOffset = new Vector2(
                760f + index * 18.3f,
                350f + index * 14.6f),

            disableGameObjectColliders = true,
            markGameObjectsStatic = true
        };
    }

    private SpawnItem CreateLogPreset(int index)
    {
        return new SpawnItem
        {
            mode = SpawnMode.GameObject,
            minRoadDistance = 2.5f,
            maxRoadDistance = 22f,
            nearRoadBias = 0.9f,
            minSlope = 0f,
            maxSlope = 48f,

            amount = 60,
            minSpacing = 5f,
            scaleRange = new Vector2(0.8f, 1.25f),
            yOffset = new Vector2(-0.08f, 0.01f),
            randomTilt = 10f,
            alignToTerrain = true,
            terrainAlignStrength = 0.75f,

            useClustering = true,
            clusterSize = 25f,
            clusterStrength = 0.55f,
            noiseOffset = new Vector2(
                900f + index * 12.4f,
                800f + index * 21.7f),

            disableGameObjectColliders = true,
            markGameObjectsStatic = true
        };
    }

    private void InvalidateRoadCache()
    {
        roadDataReady = false;
        roadMask = null;
        roadDistance = null;
        slopeCache = null;
        slopeCached = null;
        roadTriangles.Clear();
    }

    private void AnalyzeRoadWithDialog()
    {
        if (BuildRoadCache())
        {
            EditorUtility.DisplayDialog(
                "Road Cache Hazır",
                "Collider kullanılmadı.\n\n" +
                "Road triangles: " + roadTriangles.Count + "\n" +
                "Raster mask: " + analysisWidth + " x " + analysisHeight + "\n" +
                "Cell size: " +
                analysisCellSizeX.ToString("0.00") + " x " +
                analysisCellSizeZ.ToString("0.00") + " m\n\n" +
                "Artık dense vegetation üretilebilir.",
                "Tamam");
        }
    }

    private bool BuildRoadCache()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain Yok",
                "Terrain seç.",
                "Tamam");
            return false;
        }

        if (roadRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Road Root Yok",
                "Road Root seç.",
                "Tamam");
            return false;
        }

        roadTriangles.Clear();

        MeshFilter[] filters =
            roadRoot.GetComponentsInChildren<MeshFilter>(true);

        if (filters == null || filters.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "MeshFilter Bulunamadı",
                "Road Root veya alt objelerinde MeshFilter bulunamadı.",
                "Tamam");
            return false;
        }

        bool hasBounds = false;
        int validMeshes = 0;

        try
        {
            for (int f = 0; f < filters.Length; f++)
            {
                MeshFilter mf = filters[f];

                if (mf == null || mf.sharedMesh == null)
                    continue;

                Mesh mesh = mf.sharedMesh;

                Vector3[] vertices;
                int[] triangles;

                try
                {
                    vertices = mesh.vertices;
                    triangles = mesh.triangles;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        "[ForestRoadVegetation] Mesh okunamadı: " +
                        mesh.name + "\n" + ex.Message);
                    continue;
                }

                if (vertices == null ||
                    triangles == null ||
                    triangles.Length < 3)
                    continue;

                validMeshes++;

                for (int i = 0;
                     i <= triangles.Length - 3;
                     i += 3)
                {
                    int ia = triangles[i];
                    int ib = triangles[i + 1];
                    int ic = triangles[i + 2];

                    if (ia < 0 || ib < 0 || ic < 0 ||
                        ia >= vertices.Length ||
                        ib >= vertices.Length ||
                        ic >= vertices.Length)
                        continue;

                    Vector3 wa =
                        mf.transform.TransformPoint(vertices[ia]);

                    Vector3 wb =
                        mf.transform.TransformPoint(vertices[ib]);

                    Vector3 wc =
                        mf.transform.TransformPoint(vertices[ic]);

                    Vector2 a = new Vector2(wa.x, wa.z);
                    Vector2 b = new Vector2(wb.x, wb.z);
                    Vector2 c = new Vector2(wc.x, wc.z);

                    float area2 =
                        Mathf.Abs(Cross2D(b - a, c - a));

                    // Dikey side wall triangle'ları XZ'de çizgiye çöker.
                    if (area2 < 0.000001f)
                        continue;

                    RoadTriangle tri =
                        new RoadTriangle(a, b, c);

                    roadTriangles.Add(tri);

                    if (!hasBounds)
                    {
                        roadWorldBounds = tri.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        roadWorldBounds =
                            Encapsulate(
                                roadWorldBounds,
                                tri.bounds);
                    }
                }
            }

            if (validMeshes == 0 ||
                roadTriangles.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Road Triangle Yok",
                    "MeshFilter bulundu fakat kullanılabilir road triangle çıkarılamadı.",
                    "Tamam");
                return false;
            }

            TerrainData td = terrain.terrainData;

            if (useTerrainDetailResolutionForAnalysis &&
                td.detailWidth > 0 &&
                td.detailHeight > 0)
            {
                analysisWidth = td.detailWidth;
                analysisHeight = td.detailHeight;
            }
            else
            {
                analysisWidth =
                    Mathf.Clamp(
                        customAnalysisResolution,
                        128,
                        4096);

                analysisHeight = analysisWidth;
            }

            Vector3 terrainSize = td.size;

            analysisCellSizeX =
                terrainSize.x / analysisWidth;

            analysisCellSizeZ =
                terrainSize.z / analysisHeight;

            roadMask =
                new bool[analysisHeight, analysisWidth];

            roadDistance =
                new float[analysisHeight, analysisWidth];

            slopeCache =
                new float[analysisHeight, analysisWidth];

            slopeCached =
                new bool[analysisHeight, analysisWidth];

            RasterizeRoadMask();

            BuildDistanceField();

            roadDataReady = true;

            return true;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void RasterizeRoadMask()
    {
        TerrainData td = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        float halfDiag =
            Mathf.Sqrt(
                analysisCellSizeX * analysisCellSizeX +
                analysisCellSizeZ * analysisCellSizeZ) * 0.55f;

        int triangleCount = roadTriangles.Count;

        for (int t = 0; t < triangleCount; t++)
        {
            if ((t & 2047) == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Road Analysis",
                    "Road mesh rasterize ediliyor...",
                    t / (float)Mathf.Max(1, triangleCount)))
                {
                    throw new OperationCanceledException(
                        "Road analysis cancelled.");
                }
            }

            RoadTriangle tri = roadTriangles[t];

            int minX = Mathf.Clamp(
                WorldXToAnalysisX(tri.bounds.xMin),
                0,
                analysisWidth - 1);

            int maxX = Mathf.Clamp(
                WorldXToAnalysisX(tri.bounds.xMax),
                0,
                analysisWidth - 1);

            int minZ = Mathf.Clamp(
                WorldZToAnalysisZ(tri.bounds.yMin),
                0,
                analysisHeight - 1);

            int maxZ = Mathf.Clamp(
                WorldZToAnalysisZ(tri.bounds.yMax),
                0,
                analysisHeight - 1);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (roadMask[z, x])
                        continue;

                    Vector2 p =
                        AnalysisCellCenterWorld(x, z);

                    bool inside =
                        PointInTriangle(
                            p,
                            tri.a,
                            tri.b,
                            tri.c);

                    if (!inside)
                    {
                        float d =
                            DistancePointTriangle2D(
                                p,
                                tri.a,
                                tri.b,
                                tri.c);

                        inside = d <= halfDiag;
                    }

                    if (inside)
                        roadMask[z, x] = true;
                }
            }
        }

        // Ufak tek-pixel delikleri kapat.
        bool[,] cleaned =
            (bool[,])roadMask.Clone();

        for (int z = 1; z < analysisHeight - 1; z++)
        {
            for (int x = 1; x < analysisWidth - 1; x++)
            {
                if (roadMask[z, x])
                    continue;

                int neighbors = 0;

                if (roadMask[z, x - 1]) neighbors++;
                if (roadMask[z, x + 1]) neighbors++;
                if (roadMask[z - 1, x]) neighbors++;
                if (roadMask[z + 1, x]) neighbors++;
                if (roadMask[z - 1, x - 1]) neighbors++;
                if (roadMask[z - 1, x + 1]) neighbors++;
                if (roadMask[z + 1, x - 1]) neighbors++;
                if (roadMask[z + 1, x + 1]) neighbors++;

                if (neighbors >= 5)
                    cleaned[z, x] = true;
            }
        }

        roadMask = cleaned;
    }

    private void BuildDistanceField()
    {
        const float INF = 100000000f;

        float dx = analysisCellSizeX;
        float dz = analysisCellSizeZ;
        float diag = Mathf.Sqrt(dx * dx + dz * dz);

        for (int z = 0; z < analysisHeight; z++)
        {
            for (int x = 0; x < analysisWidth; x++)
            {
                roadDistance[z, x] =
                    roadMask[z, x] ? 0f : INF;
            }
        }

        // Forward pass
        for (int z = 0; z < analysisHeight; z++)
        {
            if ((z & 63) == 0)
            {
                EditorUtility.DisplayProgressBar(
                    "Road Analysis",
                    "Road distance field oluşturuluyor (1/2)...",
                    z / (float)Mathf.Max(1, analysisHeight));
            }

            for (int x = 0; x < analysisWidth; x++)
            {
                float value = roadDistance[z, x];

                if (x > 0)
                    value = Mathf.Min(
                        value,
                        roadDistance[z, x - 1] + dx);

                if (z > 0)
                    value = Mathf.Min(
                        value,
                        roadDistance[z - 1, x] + dz);

                if (x > 0 && z > 0)
                    value = Mathf.Min(
                        value,
                        roadDistance[z - 1, x - 1] + diag);

                if (x < analysisWidth - 1 && z > 0)
                    value = Mathf.Min(
                        value,
                        roadDistance[z - 1, x + 1] + diag);

                roadDistance[z, x] = value;
            }
        }

        // Backward pass
        for (int z = analysisHeight - 1; z >= 0; z--)
        {
            if ((z & 63) == 0)
            {
                EditorUtility.DisplayProgressBar(
                    "Road Analysis",
                    "Road distance field oluşturuluyor (2/2)...",
                    1f -
                    z / (float)Mathf.Max(1, analysisHeight));
            }

            for (int x = analysisWidth - 1; x >= 0; x--)
            {
                float value = roadDistance[z, x];

                if (x < analysisWidth - 1)
                    value = Mathf.Min(
                        value,
                        roadDistance[z, x + 1] + dx);

                if (z < analysisHeight - 1)
                    value = Mathf.Min(
                        value,
                        roadDistance[z + 1, x] + dz);

                if (x < analysisWidth - 1 &&
                    z < analysisHeight - 1)
                {
                    value = Mathf.Min(
                        value,
                        roadDistance[z + 1, x + 1] + diag);
                }

                if (x > 0 &&
                    z < analysisHeight - 1)
                {
                    value = Mathf.Min(
                        value,
                        roadDistance[z + 1, x - 1] + diag);
                }

                roadDistance[z, x] = value;
            }
        }
    }

    private void ApplyTerrainDetailRuntimeSettings()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
            return;

        terrain.detailObjectDensity =
            Mathf.Clamp01(terrainDetailDensityScale);

        terrain.detailObjectDistance =
            Mathf.Max(10f, terrainDetailDistance);

        terrain.drawTreesAndFoliage = true;

#if UNITY_2022_2_OR_NEWER
        if (forceCoverageMode)
        {
            TerrainData td = terrain.terrainData;

            if (td.detailScatterMode != DetailScatterMode.CoverageMode)
                td.SetDetailScatterMode(DetailScatterMode.CoverageMode);
        }
#endif

        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(terrain.terrainData);
        terrain.Flush();
    }

    private void ApplyDenseDetailResolution()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain Yok",
                "Önce Terrain seç.",
                "Tamam");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Detail Resolution Değiştir",
            "Terrain Detail Resolution değiştirmek mevcut detail density maplerini temizleyebilir.\n\n" +
            "Yeni Resolution: " + denseDetailResolution +
            "\nPer Patch: " + denseResolutionPerPatch +
            "\n\nDevam edilsin mi?",
            "Evet",
            "Hayır");

        if (!confirm)
            return;

        TerrainData td = terrain.terrainData;

        Undo.RegisterCompleteObjectUndo(
            td,
            "Set Dense Terrain Detail Resolution");

        td.SetDetailResolution(
            denseDetailResolution,
            denseResolutionPerPatch);

        EditorUtility.SetDirty(td);

        InvalidateRoadCache();

        EditorUtility.DisplayDialog(
            "Hazır",
            "Detail Resolution " +
            td.detailWidth + " x " +
            td.detailHeight + " oldu.\n\nRoad cache yeniden analiz edilmelidir.",
            "Tamam");
    }

    private void UpgradeLegacyItemsToRoadsideOnly()
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            SpawnItem item = items[i];

            if (item == null ||
                item.mode != SpawnMode.TerrainDetail)
                continue;

            // En önemli düzeltme: eski EntireTerrainOutsideRoad artık yok.
            item.detailCoverageArea = DetailCoverageArea.RoadBand;

            if (item.maxRoadDistance <= 0f ||
                item.maxRoadDistance > globalRoadsideCorridorWidth)
            {
                item.maxRoadDistance =
                    Mathf.Min(
                        26f,
                        globalRoadsideCorridorWidth);
            }

            string n =
                item.prefab != null
                    ? item.prefab.name.ToLowerInvariant()
                    : string.Empty;

            bool looksFern =
                n.Contains("fern") ||
                n.Contains("egrelti") ||
                n.Contains("eğrelti");

            bool looksGrass =
                n.Contains("grass") ||
                n.Contains("weed") ||
                n.Contains("ot");

            // Yeni serialized field eski window state'te 0 gelebilir.
            if (item.mixWeightPercent <= 0.01f)
            {
                item.mixWeightPercent =
                    looksGrass ? 60f :
                    looksFern ? 28f :
                    12f;
            }

            // Eski 0.4 - 1.5 boy aralıklarını otomatik büyüt.
            if (item.randomSizeRange.y < 2.5f ||
                item.detailHeight.y < 2.5f)
            {
                if (looksFern)
                    item.randomSizeRange = new Vector2(3.5f, 6.5f);
                else if (looksGrass)
                    item.randomSizeRange = new Vector2(3.0f, 5.5f);
                else
                    item.randomSizeRange = new Vector2(2.8f, 5.2f);
            }

            item.autoRandomSize = true;
        }
    }

    private float GetNormalizedDetailMixFactor(
        SpawnItem target)
    {
        if (!normalizeDetailMixWeights)
            return 1f;

        float total = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            SpawnItem item = items[i];

            if (item == null ||
                !item.enabled ||
                item.mode != SpawnMode.TerrainDetail ||
                item.prefab == null)
                continue;

            total += Mathf.Max(
                0.01f,
                item.mixWeightPercent);
        }

        if (total <= 0.01f)
            return 1f;

        float normalized =
            Mathf.Max(
                0.01f,
                target.mixWeightPercent) /
            total;

        // Dominant grass katmanını çok inceltmeden,
        // secondary fern/plant katmanlarını oranlı karıştır.
        return Mathf.Clamp01(
            normalized * 1.70f);
    }

    private void ApplyNaturalSizeVariationToActiveItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            SpawnItem item = items[i];

            if (item == null ||
                !item.enabled ||
                item.mode != SpawnMode.TerrainDetail)
                continue;

            item.autoRandomSize = true;
            item.detailCoverageArea = DetailCoverageArea.RoadBand;

            string n =
                item.prefab != null
                    ? item.prefab.name.ToLowerInvariant()
                    : string.Empty;

            bool looksFern =
                n.Contains("fern") ||
                n.Contains("egrelti") ||
                n.Contains("eğrelti");

            bool looksGrass =
                n.Contains("grass") ||
                n.Contains("weed") ||
                n.Contains("ot");

            if (looksFern)
            {
                item.randomSizeRange =
                    new Vector2(3.5f, 6.5f);
                item.shapeVariation = 0.16f;
                item.mixWeightPercent = 28f;
                item.maxRoadDistance =
                    Mathf.Min(24f, globalRoadsideCorridorWidth);
            }
            else if (looksGrass)
            {
                item.randomSizeRange =
                    new Vector2(3.0f, 5.5f);
                item.shapeVariation = 0.14f;
                item.mixWeightPercent = 60f;
                item.maxRoadDistance =
                    Mathf.Min(26f, globalRoadsideCorridorWidth);
            }
            else
            {
                item.randomSizeRange =
                    new Vector2(2.8f, 5.2f);
                item.shapeVariation = 0.18f;
                item.mixWeightPercent = 12f;
                item.maxRoadDistance =
                    Mathf.Min(22f, globalRoadsideCorridorWidth);
            }

            item.positionJitter =
                Mathf.Max(
                    item.positionJitter,
                    96f);
        }

        Repaint();
    }

    private void ApplyUltraDenseSettingsToActiveItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            SpawnItem item = items[i];

            if (item == null ||
                !item.enabled ||
                item.mode != SpawnMode.TerrainDetail)
                continue;

            string n =
                item.prefab != null
                    ? item.prefab.name.ToLowerInvariant()
                    : string.Empty;

            bool looksFern =
                n.Contains("fern") ||
                n.Contains("egrelti") ||
                n.Contains("eğrelti");

            bool looksGrass =
                n.Contains("grass") ||
                n.Contains("weed") ||
                n.Contains("ot");

            item.detailCoverageArea = DetailCoverageArea.RoadBand;
            item.minRoadDistance = 0.06f;
            item.minSlope = 0f;
            item.maxSlope = 86f;
            item.positionJitter = 98f;
            item.detailAlignToGround = 0.76f;
            item.autoRandomSize = true;

            if (looksFern)
            {
                item.maxRoadDistance =
                    Mathf.Min(24f, globalRoadsideCorridorWidth);

                item.nearRoadBias = 1.0f;
                item.mixWeightPercent = 28f;
                item.coveragePerCell = 205;
                item.minimumCoveragePerCell = 28;
                item.prototypeDensity = 1.0f;
                item.targetCoverage = 0.92f;
                item.clusterSize = 9f;
                item.clusterStrength = 0.58f;
                item.randomSizeRange = new Vector2(3.5f, 6.5f);
                item.shapeVariation = 0.16f;
            }
            else if (looksGrass)
            {
                item.maxRoadDistance =
                    Mathf.Min(26f, globalRoadsideCorridorWidth);

                item.nearRoadBias = 1.1f;
                item.mixWeightPercent = 60f;
                item.coveragePerCell = 245;
                item.minimumCoveragePerCell = 180;
                item.prototypeDensity = 1.10f;
                item.targetCoverage = 1f;
                item.clusterSize = 6f;
                item.clusterStrength = 0.15f;
                item.randomSizeRange = new Vector2(3.0f, 5.5f);
                item.shapeVariation = 0.14f;
            }
            else
            {
                item.maxRoadDistance =
                    Mathf.Min(22f, globalRoadsideCorridorWidth);

                item.nearRoadBias = 0.95f;
                item.mixWeightPercent = 12f;
                item.coveragePerCell = 175;
                item.minimumCoveragePerCell = 18;
                item.prototypeDensity = 0.90f;
                item.targetCoverage = 0.84f;
                item.clusterSize = 13f;
                item.clusterStrength = 0.52f;
                item.randomSizeRange = new Vector2(2.8f, 5.2f);
                item.shapeVariation = 0.18f;
            }

            item.densityRandomness = 0.26f;
        }

        globalDetailDensity = 1.0f;
        terrainDetailDensityScale = 1.0f;
        terrainDetailDistance =
            Mathf.Max(
                terrainDetailDistance,
                180f);

        Repaint();
    }

    private void GenerateAll()
    {
        if (!EnsureRoadCache())
            return;

        bool hasAny = false;

        for (int i = 0; i < items.Count; i++)
        {
            SpawnItem item = items[i];

            if (item != null &&
                item.enabled &&
                item.prefab != null)
            {
                hasAny = true;
                break;
            }
        }

        if (!hasAny)
        {
            EditorUtility.DisplayDialog(
                "Vegetation Yok",
                "En az bir aktif prefab ekle.",
                "Tamam");
            return;
        }

        ApplyTerrainDetailRuntimeSettings();

        UnityEngine.Random.InitState(seed);

        if (clearGeneratedGameObjectsBeforeGenerate)
            ClearGeneratedGameObjects(false);

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(
            "Generate Forest Road Vegetation");

        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                SpawnItem item = items[i];

                if (item == null ||
                    !item.enabled ||
                    item.prefab == null)
                    continue;

                if (item.mode == SpawnMode.TerrainDetail)
                {
                    float mixFactor =
                        GetNormalizedDetailMixFactor(item);

                    GenerateTerrainDetail(
                        item,
                        i,
                        mixFactor);
                }
                else
                {
                    GenerateGameObjects(item, i);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Undo.CollapseUndoOperations(undoGroup);
        }

        if (generatedParent != null)
            Selection.activeTransform = generatedParent;

        terrain.Flush();

        Debug.Log(
            "[ForestRoadVegetation] Roadside generation tamamlandı.");
    }

    private bool EnsureRoadCache()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null || roadRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Eksik",
                "Terrain ve Road Root seç.",
                "Tamam");
            return false;
        }

        if (!roadDataReady)
        {
            try
            {
                return BuildRoadCache();
            }
            catch (OperationCanceledException)
            {
                EditorUtility.ClearProgressBar();
                return false;
            }
        }

        return true;
    }

    private void GenerateTerrainDetail(
        SpawnItem item,
        int itemIndex,
        float mixFactor)
    {
        TerrainData td = terrain.terrainData;

        if (td.detailWidth <= 0 ||
            td.detailHeight <= 0)
        {
            EditorUtility.DisplayDialog(
                "Terrain Detail Resolution Yok",
                "Terrain'in Detail Resolution değeri 0.",
                "Tamam");
            return;
        }

        ApplyTerrainDetailRuntimeSettings();

        // PRO: Whole-terrain generation kesin olarak kapalı.
        item.detailCoverageArea =
            DetailCoverageArea.RoadBand;

        float roadExclusion =
            item.minRoadDistance +
            extraRoadClearance;

        float effectiveMaxDistance =
            Mathf.Clamp(
                item.maxRoadDistance,
                roadExclusion + 0.05f,
                Mathf.Max(
                    roadExclusion + 0.05f,
                    globalRoadsideCorridorWidth));

        int detailIndex =
            FindOrCreateDetailPrototype(
                td,
                item);

        if (detailIndex < 0)
            return;

        int width = td.detailWidth;
        int height = td.detailHeight;

        // Full zero map önemli:
        // Önceki sürümün bütün haritaya bastığı detail'leri de temizler.
        // PRO ROADSIDES: her Generate'de layer sıfırdan oluşturulur.
        // Böylece eski sürümün bütün Terrain'e bastığı grass/fern kesin olarak temizlenir.
        int[,] map =
            new int[height, width];

        // Sadece road bounds + corridor bölgesini dolaş.
        float expand =
            effectiveMaxDistance +
            extraRoadClearance +
            Mathf.Max(
                analysisCellSizeX,
                analysisCellSizeZ) * 2f;

        Rect expanded =
            Rect.MinMaxRect(
                roadWorldBounds.xMin - expand,
                roadWorldBounds.yMin - expand,
                roadWorldBounds.xMax + expand,
                roadWorldBounds.yMax + expand);

        int minDX = Mathf.Clamp(
            WorldXToDetailX(expanded.xMin),
            0,
            width - 1);

        int maxDX = Mathf.Clamp(
            WorldXToDetailX(expanded.xMax),
            0,
            width - 1);

        int minDZ = Mathf.Clamp(
            WorldZToDetailZ(expanded.yMin),
            0,
            height - 1);

        int maxDZ = Mathf.Clamp(
            WorldZToDetailZ(expanded.yMax),
            0,
            height - 1);

        int rows =
            Mathf.Max(
                1,
                maxDZ - minDZ + 1);

        System.Random rng =
            new System.Random(
                seed * 73856093 ^
                itemIndex * 19349663);

        int maxMapValue = 16;

#if UNITY_2022_2_OR_NEWER
        maxMapValue =
            forceCoverageMode
                ? 255
                : Mathf.Max(
                    1,
                    td.maxDetailScatterPerRes);
#endif

        for (int dz = minDZ; dz <= maxDZ; dz++)
        {
            if (((dz - minDZ) & 31) == 0)
            {
                float progress =
                    (dz - minDZ) /
                    (float)rows;

                if (EditorUtility.DisplayCancelableProgressBar(
                    "Professional Roadside Vegetation",
                    item.prefab.name +
                    " | yol corridor dolduruluyor...",
                    progress))
                {
                    break;
                }
            }

            for (int dx = minDX; dx <= maxDX; dx++)
            {
                Vector2 world =
                    DetailCellCenterWorld(
                        dx,
                        dz);

                float roadDist =
                    SampleRoadDistanceWorld(
                        world);

                // Asfalt / road mesh içinde vegetation yok.
                if (roadDist <= roadExclusion ||
                    roadDist > effectiveMaxDistance)
                {
                    if (clearSelectedDetailLayersBeforeGenerate)
                        map[dz, dx] = 0;

                    continue;
                }

                float slope =
                    SampleSlopeWorld(
                        world);

                if (slope < item.minSlope ||
                    slope > item.maxSlope)
                {
                    if (clearSelectedDetailLayersBeforeGenerate)
                        map[dz, dx] = 0;

                    continue;
                }

                // Yol kenarı boyunca devamlı cover.
                float near01 =
                    1f -
                    Mathf.InverseLerp(
                        roadExclusion,
                        effectiveMaxDistance,
                        roadDist);

                near01 =
                    Mathf.Pow(
                        Mathf.Clamp01(near01),
                        item.nearRoadBias);

                // Uzak corridor kenarında da sıfıra düşmez.
                float roadMultiplier =
                    Mathf.Lerp(
                        0.76f,
                        1.24f,
                        near01);

                // Kullanıcının gösterdiği yol yarma/eğim bölgelerini daha sık doldur.
                float slopeMultiplier = 1f;

                if (boostEmbankmentSlopes &&
                    slope > slopeBoostStartAngle)
                {
                    float slope01 =
                        Mathf.InverseLerp(
                            slopeBoostStartAngle,
                            Mathf.Max(
                                slopeBoostStartAngle + 1f,
                                item.maxSlope),
                            slope);

                    slopeMultiplier =
                        Mathf.Lerp(
                            1f,
                            slopeDensityBoost,
                            Mathf.Clamp01(slope01));
                }

                float clusterMultiplier = 1f;

                if (item.useClustering)
                {
                    float cs =
                        Mathf.Max(
                            0.1f,
                            item.clusterSize);

                    float noiseA =
                        Mathf.PerlinNoise(
                            (world.x +
                             item.noiseOffset.x +
                             seed * 0.0137f) / cs,
                            (world.y +
                             item.noiseOffset.y +
                             seed * 0.0271f) / cs);

                    float noiseB =
                        Mathf.PerlinNoise(
                            (world.x -
                             item.noiseOffset.y +
                             seed * 0.041f) /
                            (cs * 0.43f),
                            (world.y +
                             item.noiseOffset.x -
                             seed * 0.023f) /
                            (cs * 0.43f));

                    float combined =
                        noiseA * 0.72f +
                        noiseB * 0.28f;

                    // Grass boşluk bırakmasın; fern/plant daha patchy.
                    float naturalNoise =
                        Mathf.Lerp(
                            0.55f,
                            1.22f,
                            combined);

                    clusterMultiplier =
                        Mathf.Lerp(
                            1f,
                            naturalNoise,
                            item.clusterStrength);
                }

                float randomMul =
                    Mathf.Lerp(
                        1f - item.densityRandomness,
                        1f + item.densityRandomness,
                        (float)rng.NextDouble());

                float coverageValue =
                    item.coveragePerCell *
                    globalDetailDensity *
                    Mathf.Clamp(
                        mixFactor,
                        0.05f,
                        1f) *
                    roadMultiplier *
                    slopeMultiplier *
                    clusterMultiplier *
                    randomMul;

                int coverageInt =
                    Mathf.RoundToInt(
                        coverageValue);

                // Dominant grass'ta minimum cover yüksek.
                // Secondary fern/plant mixFactor'a göre minimumu da azalt.
                int mixedMinimum =
                    Mathf.RoundToInt(
                        item.minimumCoveragePerCell *
                        Mathf.Lerp(
                            0.45f,
                            1f,
                            Mathf.Clamp01(mixFactor)));

                coverageInt =
                    Mathf.Max(
                        mixedMinimum,
                        coverageInt);

                coverageInt =
                    Mathf.Clamp(
                        coverageInt,
                        0,
                        maxMapValue);

                map[dz, dx] =
                    coverageInt;
            }
        }

        td.SetDetailLayer(
            0,
            0,
            detailIndex,
            map);

        EditorUtility.SetDirty(td);

        Debug.Log(
            "[ForestRoadVegetation] " +
            item.prefab.name +
            " | Roadside only " +
            roadExclusion.ToString("0.0") +
            "m - " +
            effectiveMaxDistance.ToString("0.0") +
            "m | Mix " +
            (mixFactor * 100f).ToString("0") +
            "%");
    }

    private long EstimateDetailCount(
        int[,] map,
        int minX,
        int maxX,
        int minZ,
        int maxZ)
    {
        long total = 0;

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
                total += map[z, x];
        }

        return total;
    }

    private int FindOrCreateDetailPrototype(
        TerrainData td,
        SpawnItem item)
    {
        DetailPrototype[] existing =
            td.detailPrototypes;

        for (int i = 0; i < existing.Length; i++)
        {
            DetailPrototype dp = existing[i];

            if (dp != null &&
                dp.usePrototypeMesh &&
                dp.prototype == item.prefab)
            {
                ApplyDetailPrototypeSettings(
                    dp,
                    item);

                existing[i] = dp;
                td.detailPrototypes = existing;

                return i;
            }
        }

        List<DetailPrototype> list =
            new List<DetailPrototype>(
                existing);

        DetailPrototype created =
            new DetailPrototype();

        created.usePrototypeMesh = true;
        created.prototype = item.prefab;

        ApplyDetailPrototypeSettings(
            created,
            item);

        list.Add(created);

        td.detailPrototypes =
            list.ToArray();

        return list.Count - 1;
    }

    private void ApplyDetailPrototypeSettings(
        DetailPrototype dp,
        SpawnItem item)
    {
        dp.usePrototypeMesh = true;
        dp.prototype = item.prefab;

        float minSize =
            Mathf.Max(
                0.10f,
                Mathf.Min(
                    item.randomSizeRange.x,
                    item.randomSizeRange.y));

        float maxSize =
            Mathf.Max(
                minSize + 0.05f,
                Mathf.Max(
                    item.randomSizeRange.x,
                    item.randomSizeRange.y));

        // Kullanıcı özellikle büyük ve belirgin vegetation istedi:
        // eski 0.x / 1.x değerleri otomatik legacy olarak yükseltilir.
        if (item.autoRandomSize &&
            maxSize < 2.5f)
        {
            minSize = 3f;
            maxSize = 5.5f;
        }

        float shape =
            Mathf.Clamp(
                item.shapeVariation,
                0f,
                0.35f);

        // Width/Height aralıkları deliberately farklı:
        // her instance aynı siluete sahip görünmesin.
        float widthMin =
            Mathf.Max(
                0.05f,
                minSize * (1f - shape));

        float widthMax =
            Mathf.Max(
                widthMin + 0.05f,
                maxSize * (1f + shape));

        float heightMin =
            Mathf.Max(
                0.05f,
                minSize * (1f - shape * 0.35f));

        float heightMax =
            Mathf.Max(
                heightMin + 0.05f,
                maxSize * (1f + shape * 0.55f));

        dp.minWidth = widthMin;
        dp.maxWidth = widthMax;
        dp.minHeight = heightMin;
        dp.maxHeight = heightMax;

        // Legacy fields'i de gerçek uygulanan değerlerle senkron tut.
        item.detailWidth =
            new Vector2(
                widthMin,
                widthMax);

        item.detailHeight =
            new Vector2(
                heightMin,
                heightMax);

        dp.noiseSpread =
            Mathf.Max(
                0.03f,
                item.clusterSize * 0.08f);

        dp.renderMode =
            DetailRenderMode.VertexLit;

#if UNITY_2021_2_OR_NEWER
        dp.useInstancing = true;
#endif

#if UNITY_2022_2_OR_NEWER
        dp.density =
            Mathf.Clamp(
                item.prototypeDensity,
                0.05f,
                2f);

        dp.targetCoverage =
            Mathf.Clamp01(
                item.targetCoverage);

        dp.positionJitter =
            Mathf.Clamp(
                item.positionJitter,
                0f,
                100f);

        dp.alignToGround =
            Mathf.Clamp01(
                item.detailAlignToGround);

        dp.useDensityScaling = true;

#endif

        dp.healthyColor = Color.white;
        dp.dryColor = Color.white;
    }

    private void GenerateGameObjects(
        SpawnItem item,
        int itemIndex)
    {
        EnsureParent();

        string groupName =
            "__GO_" +
            SanitizeName(item.prefab.name);

        Transform oldGroup =
            generatedParent.Find(groupName);

        if (oldGroup != null &&
            clearGeneratedGameObjectsBeforeGenerate)
        {
            Undo.DestroyObjectImmediate(
                oldGroup.gameObject);

            oldGroup = null;
        }

        Transform group = oldGroup;

        if (group == null)
        {
            GameObject groupGO =
                new GameObject(groupName);

            Undo.RegisterCreatedObjectUndo(
                groupGO,
                "Vegetation Group");

            groupGO.transform.SetParent(
                generatedParent,
                false);

            group = groupGO.transform;
        }

        int target =
            Mathf.Max(
                0,
                Mathf.RoundToInt(
                    item.amount *
                    globalGameObjectDensity));

        if (target <= 0)
            return;

        int attempts = 0;
        int placed = 0;

        int maxAttempts =
            Mathf.Max(
                target * gameObjectAttemptsMultiplier,
                target + 100);

        List<Vector2> accepted =
            new List<Vector2>(
                target);

        System.Random rng =
            new System.Random(
                seed * 83492791 ^
                itemIndex * 297121507);

        while (placed < target &&
               attempts < maxAttempts)
        {
            attempts++;

            if ((attempts & 127) == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Sparse GameObjects",
                    item.prefab.name +
                    " " + placed +
                    " / " + target,
                    placed /
                    (float)Mathf.Max(1, target)))
                {
                    break;
                }
            }

            Vector2 candidate =
                RandomRoadsideWorldPoint(
                    item,
                    rng);

            float roadDist =
                SampleRoadDistanceWorld(
                    candidate);

            float minDist =
                item.minRoadDistance +
                extraRoadClearance;

            float maxDist =
                Mathf.Clamp(
                    item.maxRoadDistance,
                    minDist + 0.01f,
                    Mathf.Max(
                        minDist + 0.01f,
                        globalRoadsideCorridorWidth));

            if (roadDist <= 0.0001f ||
                roadDist < minDist ||
                roadDist > maxDist)
                continue;

            float slope =
                SampleSlopeWorld(
                    candidate);

            if (slope < item.minSlope ||
                slope > item.maxSlope)
                continue;

            if (item.useClustering)
            {
                float cs =
                    Mathf.Max(
                        0.1f,
                        item.clusterSize);

                float noise =
                    Mathf.PerlinNoise(
                        (candidate.x +
                         item.noiseOffset.x +
                         seed * 0.019f) / cs,
                        (candidate.y +
                         item.noiseOffset.y +
                         seed * 0.031f) / cs);

                float accept =
                    Mathf.Lerp(
                        1f,
                        Mathf.Lerp(
                            0.25f,
                            1f,
                            noise),
                        item.clusterStrength);

                if ((float)rng.NextDouble() >
                    accept)
                    continue;
            }

            if (item.minSpacing > 0f &&
                TooClose(
                    accepted,
                    candidate,
                    item.minSpacing))
                continue;

            Vector3 world =
                new Vector3(
                    candidate.x,
                    0f,
                    candidate.y);

            world.y =
                terrain.SampleHeight(world) +
                terrain.transform.position.y;

            SpawnGameObject(
                item,
                world,
                group);

            accepted.Add(candidate);
            placed++;
        }

        if (placed < target)
        {
            Debug.LogWarning(
                "[ForestRoadVegetation] " +
                item.prefab.name +
                " GO target: " + target +
                " | placed: " + placed);
        }
    }

    private Vector2 RandomRoadsideWorldPoint(
        SpawnItem item,
        System.Random rng)
    {
        TerrainData td = terrain.terrainData;
        Vector3 tp = terrain.transform.position;
        Vector3 size = td.size;

        float expand =
            item.maxRoadDistance +
            extraRoadClearance +
            2f;

        float minX =
            Mathf.Max(
                tp.x,
                roadWorldBounds.xMin - expand);

        float maxX =
            Mathf.Min(
                tp.x + size.x,
                roadWorldBounds.xMax + expand);

        float minZ =
            Mathf.Max(
                tp.z,
                roadWorldBounds.yMin - expand);

        float maxZ =
            Mathf.Min(
                tp.z + size.z,
                roadWorldBounds.yMax + expand);

        float x =
            Mathf.Lerp(
                minX,
                maxX,
                (float)rng.NextDouble());

        float z =
            Mathf.Lerp(
                minZ,
                maxZ,
                (float)rng.NextDouble());

        return new Vector2(x, z);
    }

    private void SpawnGameObject(
        SpawnItem item,
        Vector3 position,
        Transform parent)
    {
        GameObject go =
            PrefabUtility.InstantiatePrefab(
                item.prefab) as GameObject;

        if (go == null)
            go = Instantiate(item.prefab);

        Undo.RegisterCreatedObjectUndo(
            go,
            "Place Vegetation");

        go.transform.SetParent(
            parent,
            true);

        float minScale =
            Mathf.Min(
                item.scaleRange.x,
                item.scaleRange.y);

        float maxScale =
            Mathf.Max(
                item.scaleRange.x,
                item.scaleRange.y);

        float scale =
            UnityEngine.Random.Range(
                minScale,
                maxScale);

        go.transform.localScale =
            item.prefab.transform.localScale *
            scale;

        float minY =
            Mathf.Min(
                item.yOffset.x,
                item.yOffset.y);

        float maxY =
            Mathf.Max(
                item.yOffset.x,
                item.yOffset.y);

        position.y +=
            UnityEngine.Random.Range(
                minY,
                maxY);

        go.transform.position =
            position;

        Quaternion rotation =
            item.prefab.transform.rotation;

        if (item.alignToTerrain)
        {
            Vector3 normal =
                SampleTerrainNormalWorld(
                    new Vector2(
                        position.x,
                        position.z));

            Quaternion align =
                Quaternion.FromToRotation(
                    Vector3.up,
                    normal);

            align =
                Quaternion.Slerp(
                    Quaternion.identity,
                    align,
                    item.terrainAlignStrength);

            rotation =
                align * rotation;
        }

        if (item.randomYRotation)
        {
            rotation =
                Quaternion.AngleAxis(
                    UnityEngine.Random.Range(
                        0f,
                        360f),
                    Vector3.up) *
                rotation;
        }

        if (item.randomTilt > 0f)
        {
            Quaternion tilt =
                Quaternion.Euler(
                    UnityEngine.Random.Range(
                        -item.randomTilt,
                        item.randomTilt),
                    0f,
                    UnityEngine.Random.Range(
                        -item.randomTilt,
                        item.randomTilt));

            rotation =
                tilt * rotation;
        }

        go.transform.rotation =
            rotation;

        if (item.disableGameObjectColliders)
        {
            Collider[] colliders =
                go.GetComponentsInChildren<Collider>(
                    true);

            for (int i = 0;
                 i < colliders.Length;
                 i++)
            {
                colliders[i].enabled = false;
            }
        }

        if (item.markGameObjectsStatic)
        {
            GameObjectUtility.SetStaticEditorFlags(
                go,
                StaticEditorFlags.BatchingStatic);
        }
    }

    private void EnsureParent()
    {
        if (generatedParent != null)
            return;

        GameObject found =
            GameObject.Find(
                "__FOREST_ROAD_VEGETATION");

        if (found == null)
        {
            found =
                new GameObject(
                    "__FOREST_ROAD_VEGETATION");

            Undo.RegisterCreatedObjectUndo(
                found,
                "Vegetation Parent");
        }

        generatedParent =
            found.transform;
    }

    private void ClearSelectedVegetation(
        bool ask)
    {
        if (ask)
        {
            bool result =
                EditorUtility.DisplayDialog(
                    "Vegetation Temizle",
                    "Aktif item'ların Terrain Detail katmanları ve tool GameObject grupları temizlensin mi?",
                    "Evet",
                    "Hayır");

            if (!result)
                return;
        }

        ClearSelectedDetailLayers();
        ClearGeneratedGameObjects(false);

        if (terrain != null)
            terrain.Flush();
    }

    private void ClearSelectedDetailLayers()
    {
        if (terrain == null ||
            terrain.terrainData == null)
            return;

        TerrainData td = terrain.terrainData;

        int width = td.detailWidth;
        int height = td.detailHeight;

        if (width <= 0 || height <= 0)
            return;

        DetailPrototype[] prototypes =
            td.detailPrototypes;

        for (int itemIndex = 0;
             itemIndex < items.Count;
             itemIndex++)
        {
            SpawnItem item =
                items[itemIndex];

            if (item == null ||
                item.prefab == null ||
                item.mode != SpawnMode.TerrainDetail)
                continue;

            for (int i = 0;
                 i < prototypes.Length;
                 i++)
            {
                if (prototypes[i] != null &&
                    prototypes[i].usePrototypeMesh &&
                    prototypes[i].prototype ==
                    item.prefab)
                {
                    int[,] empty =
                        new int[
                            height,
                            width];

                    td.SetDetailLayer(
                        0,
                        0,
                        i,
                        empty);
                }
            }
        }

        EditorUtility.SetDirty(td);
    }

    private void ClearGeneratedGameObjects(
        bool ask)
    {
        if (generatedParent == null)
        {
            GameObject found =
                GameObject.Find(
                    "__FOREST_ROAD_VEGETATION");

            if (found != null)
                generatedParent =
                    found.transform;
        }

        if (generatedParent == null)
            return;

        if (ask)
        {
            bool result =
                EditorUtility.DisplayDialog(
                    "GameObjects Sil",
                    "Tool tarafından oluşturulan GameObject parent silinsin mi?",
                    "Evet",
                    "Hayır");

            if (!result)
                return;
        }

        Undo.DestroyObjectImmediate(
            generatedParent.gameObject);

        generatedParent = null;
    }

    private float SampleRoadDistanceWorld(
        Vector2 world)
    {
        if (!roadDataReady ||
            roadDistance == null)
            return 999999f;

        float fx =
            WorldXToAnalysisFloat(
                world.x);

        float fz =
            WorldZToAnalysisFloat(
                world.y);

        int x0 =
            Mathf.Clamp(
                Mathf.FloorToInt(fx),
                0,
                analysisWidth - 1);

        int z0 =
            Mathf.Clamp(
                Mathf.FloorToInt(fz),
                0,
                analysisHeight - 1);

        int x1 =
            Mathf.Clamp(
                x0 + 1,
                0,
                analysisWidth - 1);

        int z1 =
            Mathf.Clamp(
                z0 + 1,
                0,
                analysisHeight - 1);

        float tx =
            Mathf.Clamp01(
                fx - x0);

        float tz =
            Mathf.Clamp01(
                fz - z0);

        float a =
            Mathf.Lerp(
                roadDistance[z0, x0],
                roadDistance[z0, x1],
                tx);

        float b =
            Mathf.Lerp(
                roadDistance[z1, x0],
                roadDistance[z1, x1],
                tx);

        return Mathf.Lerp(
            a,
            b,
            tz);
    }

    private float SampleSlopeWorld(
        Vector2 world)
    {
        int x =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    WorldXToAnalysisFloat(
                        world.x)),
                0,
                analysisWidth - 1);

        int z =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    WorldZToAnalysisFloat(
                        world.y)),
                0,
                analysisHeight - 1);

        if (slopeCached[z, x])
            return slopeCache[z, x];

        Vector3 normal =
            SampleTerrainNormalWorld(
                world);

        float slope =
            Vector3.Angle(
                normal,
                Vector3.up);

        slopeCache[z, x] =
            slope;

        slopeCached[z, x] =
            true;

        return slope;
    }

    private Vector3 SampleTerrainNormalWorld(
        Vector2 world)
    {
        TerrainData td =
            terrain.terrainData;

        Vector3 tp =
            terrain.transform.position;

        float nx =
            Mathf.Clamp01(
                (world.x - tp.x) /
                td.size.x);

        float nz =
            Mathf.Clamp01(
                (world.y - tp.z) /
                td.size.z);

        return td.GetInterpolatedNormal(
            nx,
            nz).normalized;
    }

    private Vector2 AnalysisCellCenterWorld(
        int x,
        int z)
    {
        Vector3 tp =
            terrain.transform.position;

        return new Vector2(
            tp.x +
            (x + 0.5f) *
            analysisCellSizeX,

            tp.z +
            (z + 0.5f) *
            analysisCellSizeZ);
    }

    private Vector2 DetailCellCenterWorld(
        int x,
        int z)
    {
        TerrainData td =
            terrain.terrainData;

        Vector3 tp =
            terrain.transform.position;

        float cellX =
            td.size.x /
            td.detailWidth;

        float cellZ =
            td.size.z /
            td.detailHeight;

        return new Vector2(
            tp.x +
            (x + 0.5f) *
            cellX,

            tp.z +
            (z + 0.5f) *
            cellZ);
    }

    private int WorldXToAnalysisX(
        float worldX)
    {
        return Mathf.FloorToInt(
            WorldXToAnalysisFloat(
                worldX));
    }

    private int WorldZToAnalysisZ(
        float worldZ)
    {
        return Mathf.FloorToInt(
            WorldZToAnalysisFloat(
                worldZ));
    }

    private float WorldXToAnalysisFloat(
        float worldX)
    {
        Vector3 tp =
            terrain.transform.position;

        return
            (worldX - tp.x) /
            analysisCellSizeX;
    }

    private float WorldZToAnalysisFloat(
        float worldZ)
    {
        Vector3 tp =
            terrain.transform.position;

        return
            (worldZ - tp.z) /
            analysisCellSizeZ;
    }

    private int WorldXToDetailX(
        float worldX)
    {
        TerrainData td =
            terrain.terrainData;

        Vector3 tp =
            terrain.transform.position;

        float nx =
            (worldX - tp.x) /
            td.size.x;

        return Mathf.FloorToInt(
            nx * td.detailWidth);
    }

    private int WorldZToDetailZ(
        float worldZ)
    {
        TerrainData td =
            terrain.terrainData;

        Vector3 tp =
            terrain.transform.position;

        float nz =
            (worldZ - tp.z) /
            td.size.z;

        return Mathf.FloorToInt(
            nz * td.detailHeight);
    }

    private static float DistancePointTriangle2D(
        Vector2 p,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        if (PointInTriangle(
            p,
            a,
            b,
            c))
            return 0f;

        float d0 =
            DistancePointSegment(
                p,
                a,
                b);

        float d1 =
            DistancePointSegment(
                p,
                b,
                c);

        float d2 =
            DistancePointSegment(
                p,
                c,
                a);

        return Mathf.Min(
            d0,
            Mathf.Min(
                d1,
                d2));
    }

    private static float DistancePointSegment(
        Vector2 p,
        Vector2 a,
        Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq =
            ab.sqrMagnitude;

        if (lengthSq <= 0.000001f)
            return Vector2.Distance(
                p,
                a);

        float t =
            Mathf.Clamp01(
                Vector2.Dot(
                    p - a,
                    ab) /
                lengthSq);

        return Vector2.Distance(
            p,
            a + ab * t);
    }

    private static bool PointInTriangle(
        Vector2 p,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        float d1 =
            Sign(
                p,
                a,
                b);

        float d2 =
            Sign(
                p,
                b,
                c);

        float d3 =
            Sign(
                p,
                c,
                a);

        bool hasNeg =
            d1 < 0f ||
            d2 < 0f ||
            d3 < 0f;

        bool hasPos =
            d1 > 0f ||
            d2 > 0f ||
            d3 > 0f;

        return !(hasNeg && hasPos);
    }

    private static float Sign(
        Vector2 p1,
        Vector2 p2,
        Vector2 p3)
    {
        return
            (p1.x - p3.x) *
            (p2.y - p3.y) -
            (p2.x - p3.x) *
            (p1.y - p3.y);
    }

    private static float Cross2D(
        Vector2 a,
        Vector2 b)
    {
        return
            a.x * b.y -
            a.y * b.x;
    }

    private static bool TooClose(
        List<Vector2> points,
        Vector2 p,
        float minSpacing)
    {
        float sq =
            minSpacing *
            minSpacing;

        for (int i = 0;
             i < points.Count;
             i++)
        {
            if ((points[i] - p).sqrMagnitude < sq)
                return true;
        }

        return false;
    }

    private static Rect Encapsulate(
        Rect a,
        Rect b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    private static string SanitizeName(
        string input)
    {
        if (string.IsNullOrEmpty(input))
            return "Vegetation";

        char[] invalid =
            System.IO.Path.GetInvalidFileNameChars();

        for (int i = 0;
             i < invalid.Length;
             i++)
        {
            input =
                input.Replace(
                    invalid[i],
                    '_');
        }

        return input;
    }
}
#endif
