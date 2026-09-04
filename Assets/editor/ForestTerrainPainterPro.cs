#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Substance-style professional Terrain painting window for Unity Terrain.
/// Put this file under Assets/Editor/ForestTerrainPainterPro.cs
/// Open: Tools > Environment > Forest Terrain Painter Pro
/// </summary>
public sealed class ForestTerrainPainterPro : EditorWindow
{
    private enum BrushMode { Paint, Replace, Erase, Smooth }
    private enum LayerRole { Custom, BaseSoil, WetMud, PineNeedles, Moss, Rock }
    private enum MaskCombine { Multiply, Minimum }

    [Serializable]
    private sealed class LayerUiState
    {
        public LayerRole role = LayerRole.Custom;
        public float proceduralWeight = 1f;
    }

    private sealed class RoadGrid
    {
        public float cellSize = 25f;
        public readonly Dictionary<long, List<Vector2>> cells = new Dictionary<long, List<Vector2>>();
        public int pointCount;

        private static long Key(int x, int z)
        {
            unchecked { return ((long)x << 32) ^ (uint)z; }
        }

        public void Clear()
        {
            cells.Clear();
            pointCount = 0;
        }

        public void Add(Vector2 p)
        {
            int cx = Mathf.FloorToInt(p.x / cellSize);
            int cz = Mathf.FloorToInt(p.y / cellSize);
            long key = Key(cx, cz);
            if (!cells.TryGetValue(key, out List<Vector2> list))
            {
                list = new List<Vector2>();
                cells.Add(key, list);
            }
            list.Add(p);
            pointCount++;
        }

        public float Distance(Vector2 p, float maxSearch = 220f)
        {
            if (pointCount == 0) return maxSearch;

            int cx = Mathf.FloorToInt(p.x / cellSize);
            int cz = Mathf.FloorToInt(p.y / cellSize);
            int maxRing = Mathf.Max(1, Mathf.CeilToInt(maxSearch / cellSize));
            float bestSq = maxSearch * maxSearch;
            bool found = false;

            for (int ring = 0; ring <= maxRing; ring++)
            {
                for (int dz = -ring; dz <= ring; dz++)
                {
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (ring > 0 && Mathf.Abs(dx) != ring && Mathf.Abs(dz) != ring)
                            continue;

                        if (!cells.TryGetValue(Key(cx + dx, cz + dz), out List<Vector2> list))
                            continue;

                        for (int i = 0; i < list.Count; i++)
                        {
                            float sq = (list[i] - p).sqrMagnitude;
                            if (sq < bestSq)
                            {
                                bestSq = sq;
                                found = true;
                            }
                        }
                    }
                }

                if (found)
                {
                    float currentBest = Mathf.Sqrt(bestSq);
                    if ((ring + 1) * cellSize > currentBest + cellSize)
                        break;
                }
            }

            return found ? Mathf.Sqrt(bestSq) : maxSearch;
        }
    }

    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform roadRoot;
    [SerializeField] private bool autoUseSelectedTerrain = true;

    [SerializeField] private bool scenePaintingEnabled = true;
    [SerializeField] private BrushMode brushMode = BrushMode.Paint;
    [SerializeField, Range(0.5f, 120f)] private float brushSize = 10f;
    [SerializeField, Range(0.01f, 1f)] private float brushOpacity = 0.22f;
    [SerializeField, Range(0f, 1f)] private float brushHardness = 0.45f;
    [SerializeField, Range(0f, 1f)] private float targetOpacity = 1f;
    [SerializeField, Range(0.01f, 1f)] private float brushSpacing = 0.16f;
    [SerializeField, Range(0f, 1f)] private float brushJitter = 0.08f;
    [SerializeField] private int eraseToLayer = 0;
    [SerializeField] private int selectedLayer = 0;

    [SerializeField] private bool useHeightMask = false;
    [SerializeField] private Vector2 heightRange = new Vector2(-100f, 500f);
    [SerializeField, Range(0f, 25f)] private float heightFeather = 5f;
    [SerializeField] private bool useSlopeMask = false;
    [SerializeField] private Vector2 slopeRange = new Vector2(0f, 35f);
    [SerializeField, Range(0f, 25f)] private float slopeFeather = 5f;
    [SerializeField] private bool useNoiseMask = true;
    [SerializeField, Range(1f, 250f)] private float noiseScale = 28f;
    [SerializeField, Range(0f, 2f)] private float noiseStrength = 0.45f;
    [SerializeField, Range(0.1f, 5f)] private float noiseContrast = 1.35f;
    [SerializeField] private Vector2 noiseOffset = new Vector2(137.2f, 411.7f);
    [SerializeField] private bool useCurvatureMask = false;
    [SerializeField, Range(-3f, 3f)] private float curvatureBias = 0f;
    [SerializeField, Range(0f, 8f)] private float curvatureStrength = 2.2f;
    [SerializeField] private bool useRoadMask = false;
    [SerializeField, Range(0f, 150f)] private float roadMaskNear = 0f;
    [SerializeField, Range(0.5f, 250f)] private float roadMaskFar = 18f;
    [SerializeField] private bool invertRoadMask = false;
    [SerializeField] private MaskCombine maskCombine = MaskCombine.Multiply;
    [SerializeField] private bool invertCombinedMask = false;

    [SerializeField, Range(0f, 40f)] private float forksRoadsideWidth = 9f;
    [SerializeField, Range(0f, 80f)] private float forksTransitionWidth = 28f;
    [SerializeField, Range(0f, 90f)] private float forksRockSlope = 36f;
    [SerializeField, Range(0f, 90f)] private float forksMossSlopeMax = 42f;
    [SerializeField, Range(1f, 140f)] private float forksPatchScale = 34f;
    [SerializeField, Range(0f, 2f)] private float forksWetness = 1.15f;
    [SerializeField, Range(0f, 2f)] private float forksMossAmount = 1.15f;
    [SerializeField, Range(0f, 2f)] private float forksNeedleAmount = 0.95f;
    [SerializeField, Range(0f, 2f)] private float forksRockAmount = 1.0f;

    [SerializeField] private Texture2D newAlbedo;
    [SerializeField] private Texture2D newNormal;
    [SerializeField] private Texture2D newMaskMap;
    [SerializeField] private string newLayerName = "Forest_Ground";
    [SerializeField] private Vector2 newLayerTileSize = new Vector2(4f, 4f);
    [SerializeField, Range(0f, 2f)] private float newLayerNormalScale = 0.8f;
    [SerializeField, Range(0f, 1f)] private float newLayerSmoothness = 0.25f;

    private Vector2 leftScroll;
    private Vector2 rightScroll;
    [SerializeField] private List<LayerUiState> layerStates = new List<LayerUiState>();
    private readonly RoadGrid roadGrid = new RoadGrid();
    private bool roadCacheValid;
    private Vector3? lastPaintPoint;
    private bool strokeActive;
    private bool showMasks = true;
    private bool showSmartBlend = true;
    private bool showLayerCreator;
    private bool showAdvanced;

    [MenuItem("Tools/Environment/Forest Terrain Painter Pro")]
    private static void OpenWindow()
    {
        ForestTerrainPainterPro window = GetWindow<ForestTerrainPainterPro>();
        window.titleContent = new GUIContent("Terrain Painter Pro");
        window.minSize = new Vector2(900f, 620f);
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        TryAcquireTerrain();
        SyncLayerStates();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        strokeActive = false;
        lastPaintPoint = null;
    }

    private void OnSelectionChange()
    {
        if (!autoUseSelectedTerrain) return;
        Terrain t = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Terrain>() : null;
        if (t != null && t != targetTerrain)
        {
            targetTerrain = t;
            roadCacheValid = false;
            SyncLayerStates();
            Repaint();
        }
    }

    private void TryAcquireTerrain()
    {
        if (targetTerrain != null) return;
        if (Selection.activeGameObject != null)
            targetTerrain = Selection.activeGameObject.GetComponent<Terrain>();
        if (targetTerrain == null)
            targetTerrain = Terrain.activeTerrain;
    }

    private void SyncLayerStates()
    {
        int count = targetTerrain != null && targetTerrain.terrainData != null
            ? targetTerrain.terrainData.terrainLayers.Length : 0;
        while (layerStates.Count < count) layerStates.Add(new LayerUiState());
        while (layerStates.Count > count) layerStates.RemoveAt(layerStates.Count - 1);
        selectedLayer = Mathf.Clamp(selectedLayer, 0, Mathf.Max(0, count - 1));
        eraseToLayer = Mathf.Clamp(eraseToLayer, 0, Mathf.Max(0, count - 1));
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        DrawTopBar();

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            EditorGUILayout.HelpBox("Hierarchy'den Terrain objesini seç veya Target Terrain alanına sürükle.", MessageType.Warning);
            return;
        }

        SyncLayerStates();
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(320f, position.width * 0.37f)));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        DrawLayerPanel();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Space(6f);

        EditorGUILayout.BeginVertical();
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
        DrawBrushPanel();
        DrawMaskPanel();
        DrawSmartBlendPanel();
        DrawLayerCreator();
        DrawAdvancedPanel();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        DrawBottomStatus();
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        Terrain newTerrain = (Terrain)EditorGUILayout.ObjectField(new GUIContent("Terrain"), targetTerrain, typeof(Terrain), true, GUILayout.MinWidth(220f));
        if (newTerrain != targetTerrain)
        {
            targetTerrain = newTerrain;
            roadCacheValid = false;
            SyncLayerStates();
        }

        GUILayout.Space(8f);
        Transform newRoad = (Transform)EditorGUILayout.ObjectField(new GUIContent("Road Root"), roadRoot, typeof(Transform), true, GUILayout.MinWidth(210f));
        if (newRoad != roadRoot)
        {
            roadRoot = newRoad;
            roadCacheValid = false;
        }

        GUILayout.FlexibleSpace();
        autoUseSelectedTerrain = GUILayout.Toggle(autoUseSelectedTerrain, "Auto Select Terrain", EditorStyles.toolbarButton);
        scenePaintingEnabled = GUILayout.Toggle(scenePaintingEnabled, "Scene Paint", EditorStyles.toolbarButton);
        if (GUILayout.Button("Rebuild Road Cache", EditorStyles.toolbarButton))
        {
            BuildRoadCache();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLayerPanel()
    {
        EditorGUILayout.LabelField("TERRAIN LAYERS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Katmana tıkla ve Scene View'da direkt boya. + ile layer ekle, X ile sil, oklarla sırasını değiştir.", MessageType.Info);

        TerrainData data = targetTerrain.terrainData;
        TerrainLayer[] layers = data.terrainLayers;

        for (int i = 0; i < layers.Length; i++)
        {
            TerrainLayer layer = layers[i];
            bool active = i == selectedLayer;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            Texture preview = layer != null && layer.diffuseTexture != null ? layer.diffuseTexture : Texture2D.grayTexture;
            Rect thumbRect = GUILayoutUtility.GetRect(58f, 58f, GUILayout.Width(58f), GUILayout.Height(58f));
            GUI.DrawTexture(thumbRect, preview, ScaleMode.ScaleAndCrop);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            string display = layer != null ? layer.name : "Missing Layer";
            if (GUILayout.Button((active ? "● " : "") + display, EditorStyles.boldLabel)) selectedLayer = i;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("▲", GUILayout.Width(25f))) MoveLayer(i, i - 1);
            if (GUILayout.Button("▼", GUILayout.Width(25f))) MoveLayer(i, i + 1);
            if (GUILayout.Button("X", GUILayout.Width(25f))) RemoveLayer(i);
            EditorGUILayout.EndHorizontal();

            layerStates[i].role = (LayerRole)EditorGUILayout.EnumPopup("Role", layerStates[i].role);
            layerStates[i].proceduralWeight = EditorGUILayout.Slider("Auto Weight", layerStates[i].proceduralWeight, 0f, 2f);

            if (layer != null)
            {
                EditorGUI.BeginChangeCheck();
                Vector2 tile = EditorGUILayout.Vector2Field("Tile Size (m)", layer.tileSize);
                float normalScale = EditorGUILayout.Slider("Normal", layer.normalScale, 0f, 2f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(layer, "Edit Terrain Layer");
                    layer.tileSize = new Vector2(Mathf.Max(0.05f, tile.x), Mathf.Max(0.05f, tile.y));
                    layer.normalScale = normalScale;
                    EditorUtility.SetDirty(layer);
                    targetTerrain.Flush();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(3f);
        }

        if (layers.Length == 0)
            EditorGUILayout.HelpBox("Terrain'de henüz Terrain Layer yok.", MessageType.Warning);

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ ADD EXISTING LAYER", GUILayout.Height(30f))) ShowAddLayerMenu();
        if (GUILayout.Button("DUPLICATE SELECTED", GUILayout.Height(30f))) DuplicateSelectedLayer();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("NORMALIZE ALL SPLAT WEIGHTS", GUILayout.Height(28f))) NormalizeAllWeights();
    }

    private void ShowAddLayerMenu()
    {
        GenericMenu menu = new GenericMenu();
        string[] guids = AssetDatabase.FindAssets("t:TerrainLayer");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TerrainLayer found = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (found == null) continue;
            TerrainLayer captured = found;
            menu.AddItem(new GUIContent(captured.name), false, () => AddExistingLayer(captured));
        }
        if (guids.Length == 0) menu.AddDisabledItem(new GUIContent("No TerrainLayer assets found"));
        menu.ShowAsContext();
    }

    private void DrawBrushPanel()
    {
        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField("LIVE BRUSH", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        scenePaintingEnabled = EditorGUILayout.ToggleLeft("Enable Scene View Painting", scenePaintingEnabled);
        brushMode = (BrushMode)EditorGUILayout.EnumPopup("Mode", brushMode);
        brushSize = EditorGUILayout.Slider("Size (metres)", brushSize, 0.5f, 120f);
        brushOpacity = EditorGUILayout.Slider("Opacity / Flow", brushOpacity, 0.01f, 1f);
        brushHardness = EditorGUILayout.Slider("Hardness", brushHardness, 0f, 1f);
        targetOpacity = EditorGUILayout.Slider("Target Layer Opacity", targetOpacity, 0f, 1f);
        brushSpacing = EditorGUILayout.Slider("Spacing", brushSpacing, 0.01f, 1f);
        brushJitter = EditorGUILayout.Slider("Jitter", brushJitter, 0f, 1f);
        if (brushMode == BrushMode.Erase)
            eraseToLayer = EditorGUILayout.IntSlider("Erase To Layer", eraseToLayer, 0, Mathf.Max(0, targetTerrain.terrainData.terrainLayers.Length - 1));
        EditorGUILayout.HelpBox("Scene View: Sol tık + sürükle = boya. Alt basılıyken kamera kontrolü korunur. Ctrl+Z Undo çalışır.", MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void DrawMaskPanel()
    {
        showMasks = EditorGUILayout.Foldout(showMasks, "MASK STACK (Substance-style)", true);
        if (!showMasks) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        useHeightMask = EditorGUILayout.ToggleLeft("Height Mask", useHeightMask);
        if (useHeightMask)
        {
            heightRange = EditorGUILayout.Vector2Field("World Y Min/Max", heightRange);
            heightFeather = EditorGUILayout.Slider("Height Feather", heightFeather, 0f, 25f);
        }

        useSlopeMask = EditorGUILayout.ToggleLeft("Slope Mask", useSlopeMask);
        if (useSlopeMask)
        {
            slopeRange = EditorGUILayout.Vector2Field("Slope Min/Max", slopeRange);
            slopeRange.x = Mathf.Clamp(slopeRange.x, 0f, 90f);
            slopeRange.y = Mathf.Clamp(slopeRange.y, 0f, 90f);
            slopeFeather = EditorGUILayout.Slider("Slope Feather", slopeFeather, 0f, 25f);
        }

        useNoiseMask = EditorGUILayout.ToggleLeft("Organic Noise Mask", useNoiseMask);
        if (useNoiseMask)
        {
            noiseScale = EditorGUILayout.Slider("Noise Scale", noiseScale, 1f, 250f);
            noiseStrength = EditorGUILayout.Slider("Noise Strength", noiseStrength, 0f, 2f);
            noiseContrast = EditorGUILayout.Slider("Noise Contrast", noiseContrast, 0.1f, 5f);
            noiseOffset = EditorGUILayout.Vector2Field("Noise Offset", noiseOffset);
            if (GUILayout.Button("Randomize Noise"))
                noiseOffset = new Vector2(UnityEngine.Random.Range(-10000f, 10000f), UnityEngine.Random.Range(-10000f, 10000f));
        }

        useCurvatureMask = EditorGUILayout.ToggleLeft("Hollow / Curvature Mask", useCurvatureMask);
        if (useCurvatureMask)
        {
            curvatureBias = EditorGUILayout.Slider("Curvature Bias", curvatureBias, -3f, 3f);
            curvatureStrength = EditorGUILayout.Slider("Curvature Strength", curvatureStrength, 0f, 8f);
        }

        useRoadMask = EditorGUILayout.ToggleLeft("Road Distance Mask", useRoadMask);
        if (useRoadMask)
        {
            roadMaskNear = EditorGUILayout.Slider("Near", roadMaskNear, 0f, 150f);
            roadMaskFar = EditorGUILayout.Slider("Far", roadMaskFar, 0.5f, 250f);
            invertRoadMask = EditorGUILayout.Toggle("Invert Road Mask", invertRoadMask);
            if (!roadCacheValid)
                EditorGUILayout.HelpBox("Road mask için Road Root ata ve Rebuild Road Cache bas.", MessageType.Warning);
        }

        maskCombine = (MaskCombine)EditorGUILayout.EnumPopup("Combine", maskCombine);
        invertCombinedMask = EditorGUILayout.Toggle("Invert Final Mask", invertCombinedMask);

        EditorGUILayout.Space(3f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("APPLY MASK TO SELECTED LAYER", GUILayout.Height(32f))) ApplyMaskToSelectedLayer(false);
        if (GUILayout.Button("REPLACE WITH MASK", GUILayout.Height(32f))) ApplyMaskToSelectedLayer(true);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawSmartBlendPanel()
    {
        showSmartBlend = EditorGUILayout.Foldout(showSmartBlend, "FORKS / WASHINGTON SMART BLEND", true);
        if (!showSmartBlend) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox("Layer Role alanlarını BaseSoil / WetMud / PineNeedles / Moss / Rock olarak ayarla. Tek butonla yol kenarı, eğim, çukur ve organik patch'lere göre tüm Terrain'i karıştırır.", MessageType.Info);
        forksRoadsideWidth = EditorGUILayout.Slider("Wet Roadside Width", forksRoadsideWidth, 0f, 40f);
        forksTransitionWidth = EditorGUILayout.Slider("Road Transition", forksTransitionWidth, 0f, 80f);
        forksRockSlope = EditorGUILayout.Slider("Rock Starts At Slope", forksRockSlope, 0f, 90f);
        forksMossSlopeMax = EditorGUILayout.Slider("Moss Max Slope", forksMossSlopeMax, 0f, 90f);
        forksPatchScale = EditorGUILayout.Slider("Organic Patch Scale", forksPatchScale, 1f, 140f);
        forksWetness = EditorGUILayout.Slider("Wetness Amount", forksWetness, 0f, 2f);
        forksMossAmount = EditorGUILayout.Slider("Moss Amount", forksMossAmount, 0f, 2f);
        forksNeedleAmount = EditorGUILayout.Slider("Needle Amount", forksNeedleAmount, 0f, 2f);
        forksRockAmount = EditorGUILayout.Slider("Rock Amount", forksRockAmount, 0f, 2f);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("AUTO ASSIGN ROLES BY NAME", GUILayout.Height(30f))) AutoAssignRolesByName();
        if (GUILayout.Button("BUILD FORKS TERRAIN BLEND", GUILayout.Height(34f))) BuildForksBlend();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawLayerCreator()
    {
        showLayerCreator = EditorGUILayout.Foldout(showLayerCreator, "CREATE TERRAIN LAYER FROM PBR MAPS", true);
        if (!showLayerCreator) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        newLayerName = EditorGUILayout.TextField("Layer Name", newLayerName);
        newAlbedo = (Texture2D)EditorGUILayout.ObjectField("Albedo", newAlbedo, typeof(Texture2D), false);
        newNormal = (Texture2D)EditorGUILayout.ObjectField("Normal Map", newNormal, typeof(Texture2D), false);
        newMaskMap = (Texture2D)EditorGUILayout.ObjectField("Mask Map", newMaskMap, typeof(Texture2D), false);
        newLayerTileSize = EditorGUILayout.Vector2Field("Physical Tile Size (m)", newLayerTileSize);
        newLayerNormalScale = EditorGUILayout.Slider("Normal Strength", newLayerNormalScale, 0f, 2f);
        newLayerSmoothness = EditorGUILayout.Slider("Fallback Smoothness", newLayerSmoothness, 0f, 1f);
        if (GUILayout.Button("CREATE + ADD TERRAIN LAYER", GUILayout.Height(34f))) CreateLayerFromMaps();
        EditorGUILayout.EndVertical();
    }

    private void DrawAdvancedPanel()
    {
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "ADVANCED / UTILITIES", true);
        if (!showAdvanced) return;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (GUILayout.Button("CLEAR SELECTED LAYER")) ClearSelectedLayer();
        if (GUILayout.Button("FILL SELECTED LAYER 100%")) FillSelectedLayer();
        if (GUILayout.Button("SAVE TERRAIN ASSETS"))
        {
            EditorUtility.SetDirty(targetTerrain.terrainData);
            AssetDatabase.SaveAssets();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawBottomStatus()
    {
        TerrainData data = targetTerrain != null ? targetTerrain.terrainData : null;
        if (data == null) return;
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Alphamap: " + data.alphamapWidth + "×" + data.alphamapHeight);
        GUILayout.Space(12f);
        GUILayout.Label("Layers: " + data.alphamapLayers);
        GUILayout.Space(12f);
        GUILayout.Label("Terrain: " + data.size.x.ToString("F0") + "×" + data.size.z.ToString("F0") + " m");
        GUILayout.FlexibleSpace();
        GUILayout.Label(roadCacheValid ? "Road cache: " + roadGrid.pointCount + " pts" : "Road cache: not built");
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!scenePaintingEnabled || targetTerrain == null || targetTerrain.terrainData == null) return;

        Event e = Event.current;
        if (e.alt) return;
        if (e.button != 0 && e.type != EventType.MouseMove && e.type != EventType.Repaint) return;

        TerrainCollider collider = targetTerrain.GetComponent<TerrainCollider>();
        if (collider == null) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!collider.Raycast(ray, out RaycastHit hit, 100000f)) return;
        Vector3 p = hit.point;

        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            return;
        }

        if (e.type == EventType.Repaint)
        {
            Handles.color = new Color(0.2f, 0.9f, 0.7f, 0.9f);
            Handles.DrawWireDisc(p, hit.normal, brushSize * 0.5f);
            Handles.color = new Color(0.2f, 0.9f, 0.7f, 0.14f);
            Handles.DrawSolidDisc(p, hit.normal, brushSize * 0.5f);
            return;
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Undo.RegisterCompleteObjectUndo(targetTerrain.terrainData, "Terrain Painter Pro Stroke");
            strokeActive = true;
            lastPaintPoint = null;
            PaintAtWorld(p);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && strokeActive)
        {
            float minimumSpacing = Mathf.Max(0.03f, brushSize * brushSpacing);
            if (!lastPaintPoint.HasValue || Vector3.Distance(lastPaintPoint.Value, p) >= minimumSpacing) PaintAtWorld(p);
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0 && strokeActive)
        {
            strokeActive = false;
            lastPaintPoint = null;
            EditorUtility.SetDirty(targetTerrain.terrainData);
            e.Use();
        }

        SceneView.RepaintAll();
    }

    private void PaintAtWorld(Vector3 worldPoint)
    {
        TerrainData data = targetTerrain.terrainData;
        int layerCount = data.alphamapLayers;
        if (layerCount == 0 || selectedLayer < 0 || selectedLayer >= layerCount) return;

        Vector3 terrainPos = targetTerrain.transform.position;
        float normalizedX = Mathf.InverseLerp(terrainPos.x, terrainPos.x + data.size.x, worldPoint.x);
        float normalizedZ = Mathf.InverseLerp(terrainPos.z, terrainPos.z + data.size.z, worldPoint.z);
        int centerX = Mathf.RoundToInt(normalizedX * (data.alphamapWidth - 1));
        int centerZ = Mathf.RoundToInt(normalizedZ * (data.alphamapHeight - 1));
        float radiusM = brushSize * 0.5f;
        int radiusX = Mathf.Max(1, Mathf.CeilToInt(radiusM / data.size.x * data.alphamapWidth));
        int radiusZ = Mathf.Max(1, Mathf.CeilToInt(radiusM / data.size.z * data.alphamapHeight));
        int x0 = Mathf.Clamp(centerX - radiusX, 0, data.alphamapWidth - 1);
        int z0 = Mathf.Clamp(centerZ - radiusZ, 0, data.alphamapHeight - 1);
        int x1 = Mathf.Clamp(centerX + radiusX, 0, data.alphamapWidth - 1);
        int z1 = Mathf.Clamp(centerZ + radiusZ, 0, data.alphamapHeight - 1);
        int width = x1 - x0 + 1;
        int height = z1 - z0 + 1;
        float[,,] map = data.GetAlphamaps(x0, z0, width, height);
        float[,,] source = brushMode == BrushMode.Smooth ? (float[,,])map.Clone() : null;
        System.Random random = new System.Random((int)(worldPoint.x * 91f + worldPoint.z * 173f + DateTime.Now.Millisecond));

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int ax = x0 + x;
                int az = z0 + z;
                float nx = ax / Mathf.Max(1f, data.alphamapWidth - 1f);
                float nz = az / Mathf.Max(1f, data.alphamapHeight - 1f);
                float wx = terrainPos.x + nx * data.size.x;
                float wz = terrainPos.z + nz * data.size.z;
                float dx = wx - worldPoint.x;
                float dz = wz - worldPoint.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist > radiusM) continue;

                float radial = 1f - Mathf.Clamp01(dist / Mathf.Max(0.001f, radiusM));
                float exponent = Mathf.Lerp(3.5f, 0.35f, brushHardness);
                float falloff = Mathf.Pow(radial, exponent);
                float jitter = brushJitter > 0f ? Mathf.Lerp(1f - brushJitter, 1f + brushJitter, (float)random.NextDouble()) : 1f;
                float worldY = terrainPos.y + data.GetInterpolatedHeight(nx, nz);
                float mask = EvaluateCombinedMask(nx, nz, wx, worldY, wz);
                float amount = Mathf.Clamp01(brushOpacity * falloff * mask * jitter);
                if (amount <= 0.0001f) continue;

                if (brushMode == BrushMode.Smooth)
                {
                    float avg = 0f;
                    int count = 0;
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        int sz = Mathf.Clamp(z + oz, 0, height - 1);
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int sx = Mathf.Clamp(x + ox, 0, width - 1);
                            avg += source[sz, sx, selectedLayer];
                            count++;
                        }
                    }
                    avg /= Mathf.Max(1, count);
                    SetLayerValue(map, z, x, selectedLayer, Mathf.Lerp(map[z, x, selectedLayer], avg, amount));
                }
                else if (brushMode == BrushMode.Erase)
                {
                    int baseLayer = Mathf.Clamp(eraseToLayer, 0, layerCount - 1);
                    if (baseLayer == selectedLayer) baseLayer = selectedLayer == 0 && layerCount > 1 ? 1 : 0;
                    float removed = Mathf.Min(map[z, x, selectedLayer], amount);
                    map[z, x, selectedLayer] -= removed;
                    map[z, x, baseLayer] += removed;
                    NormalizePixel(map, z, x);
                }
                else
                {
                    float current = map[z, x, selectedLayer];
                    float desired = brushMode == BrushMode.Replace ? targetOpacity : Mathf.Max(current, targetOpacity);
                    float value = Mathf.Lerp(current, desired, amount);
                    SetLayerValue(map, z, x, selectedLayer, value);
                }
            }
        }

        data.SetAlphamaps(x0, z0, map);
        targetTerrain.Flush();
        lastPaintPoint = worldPoint;
    }

    private static void SetLayerValue(float[,,] map, int z, int x, int layer, float desired)
    {
        int layers = map.GetLength(2);
        desired = Mathf.Clamp01(desired);
        float others = 0f;
        for (int i = 0; i < layers; i++) if (i != layer) others += map[z, x, i];
        map[z, x, layer] = desired;
        float remaining = 1f - desired;
        if (layers <= 1) return;

        if (others <= 0.000001f)
        {
            float share = remaining / (layers - 1);
            for (int i = 0; i < layers; i++) if (i != layer) map[z, x, i] = share;
        }
        else
        {
            float scale = remaining / others;
            for (int i = 0; i < layers; i++) if (i != layer) map[z, x, i] *= scale;
        }
    }

    private static void NormalizePixel(float[,,] map, int z, int x)
    {
        int layers = map.GetLength(2);
        float total = 0f;
        for (int i = 0; i < layers; i++) total += Mathf.Max(0f, map[z, x, i]);
        if (total <= 0.000001f)
        {
            map[z, x, 0] = 1f;
            for (int i = 1; i < layers; i++) map[z, x, i] = 0f;
            return;
        }
        float inv = 1f / total;
        for (int i = 0; i < layers; i++) map[z, x, i] = Mathf.Max(0f, map[z, x, i]) * inv;
    }

    private float EvaluateCombinedMask(float nx, float nz, float worldX, float worldY, float worldZ)
    {
        TerrainData data = targetTerrain.terrainData;
        List<float> values = new List<float>(5);
        if (useHeightMask) values.Add(RangeMask(worldY, heightRange.x, heightRange.y, heightFeather));
        if (useSlopeMask) values.Add(RangeMask(data.GetSteepness(nx, nz), slopeRange.x, slopeRange.y, slopeFeather));
        if (useNoiseMask)
        {
            float n = FractalNoise((worldX + noiseOffset.x) / noiseScale, (worldZ + noiseOffset.y) / noiseScale, 4, 0.52f);
            n = Mathf.Clamp01((n - 0.5f) * noiseContrast + 0.5f);
            values.Add(Mathf.Lerp(1f, n, Mathf.Clamp01(noiseStrength)));
        }
        if (useCurvatureMask)
        {
            float c = EvaluateConcavity(nx, nz);
            values.Add(Mathf.Clamp01(0.5f + (c + curvatureBias) * curvatureStrength));
        }
        if (useRoadMask)
        {
            if (!roadCacheValid) BuildRoadCache();
            float distance = roadGrid.Distance(new Vector2(worldX, worldZ), Mathf.Max(roadMaskFar + 80f, 160f));
            float v = 1f - Smooth01((distance - roadMaskNear) / Mathf.Max(0.001f, roadMaskFar - roadMaskNear));
            if (invertRoadMask) v = 1f - v;
            values.Add(v);
        }

        float result = 1f;
        if (values.Count > 0)
        {
            if (maskCombine == MaskCombine.Minimum)
            {
                for (int i = 0; i < values.Count; i++) result = Mathf.Min(result, values[i]);
            }
            else
            {
                for (int i = 0; i < values.Count; i++) result *= values[i];
            }
        }
        if (invertCombinedMask) result = 1f - result;
        return Mathf.Clamp01(result);
    }

    private static float RangeMask(float value, float min, float max, float feather)
    {
        if (max < min) { float temp = min; min = max; max = temp; }
        feather = Mathf.Max(0.0001f, feather);
        float low = Smooth01((value - (min - feather)) / feather);
        float high = 1f - Smooth01((value - max) / feather);
        return Mathf.Clamp01(low * high);
    }

    private float EvaluateConcavity(float nx, float nz)
    {
        TerrainData data = targetTerrain.terrainData;
        float step = 1f / Mathf.Max(2f, data.heightmapResolution - 1f);
        float c = data.GetInterpolatedHeight(nx, nz);
        float l = data.GetInterpolatedHeight(Mathf.Clamp01(nx - step), nz);
        float r = data.GetInterpolatedHeight(Mathf.Clamp01(nx + step), nz);
        float d = data.GetInterpolatedHeight(nx, Mathf.Clamp01(nz - step));
        float u = data.GetInterpolatedHeight(nx, Mathf.Clamp01(nz + step));
        return (l + r + d + u) * 0.25f - c;
    }

    private void ApplyMaskToSelectedLayer(bool replace)
    {
        TerrainData data = targetTerrain.terrainData;
        if (data.alphamapLayers == 0) return;
        Undo.RegisterCompleteObjectUndo(data, "Apply Terrain Painter Mask");
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] map = data.GetAlphamaps(0, 0, w, h);
        Vector3 pos = targetTerrain.transform.position;

        try
        {
            for (int z = 0; z < h; z++)
            {
                if ((z & 31) == 0) EditorUtility.DisplayProgressBar("Terrain Painter Pro", "Applying mask...", z / (float)h);
                float nz = z / Mathf.Max(1f, h - 1f);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / Mathf.Max(1f, w - 1f);
                    float wx = pos.x + nx * data.size.x;
                    float wz = pos.z + nz * data.size.z;
                    float wy = pos.y + data.GetInterpolatedHeight(nx, nz);
                    float mask = EvaluateCombinedMask(nx, nz, wx, wy, wz);
                    float desired = replace ? mask : Mathf.Max(map[z, x, selectedLayer], mask * targetOpacity);
                    SetLayerValue(map, z, x, selectedLayer, desired);
                }
            }
            data.SetAlphamaps(0, 0, map);
            targetTerrain.Flush();
            EditorUtility.SetDirty(data);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private void AutoAssignRolesByName()
    {
        TerrainLayer[] layers = targetTerrain.terrainData.terrainLayers;
        SyncLayerStates();
        for (int i = 0; i < layers.Length; i++)
        {
            string n = layers[i] != null ? layers[i].name.ToLowerInvariant() : "";
            LayerRole role = LayerRole.Custom;
            if (ContainsAny(n, "rock", "stone", "gravel", "kaya", "tas")) role = LayerRole.Rock;
            else if (ContainsAny(n, "moss", "green", "yosun")) role = LayerRole.Moss;
            else if (ContainsAny(n, "needle", "pine", "leaf", "çam", "cam", "igne")) role = LayerRole.PineNeedles;
            else if (ContainsAny(n, "mud", "wet", "dirt", "çamur", "camur")) role = LayerRole.WetMud;
            else if (ContainsAny(n, "soil", "earth", "ground", "toprak", "forest")) role = LayerRole.BaseSoil;
            layerStates[i].role = role;
        }
        Repaint();
    }

    private static bool ContainsAny(string source, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++) if (source.Contains(needles[i])) return true;
        return false;
    }

    private void BuildForksBlend()
    {
        TerrainData data = targetTerrain.terrainData;
        int layers = data.alphamapLayers;
        if (layers == 0) return;
        if (roadRoot != null && !roadCacheValid) BuildRoadCache();

        bool hasBaseRole = FindRole(LayerRole.BaseSoil) >= 0;

        Undo.RegisterCompleteObjectUndo(data, "Build Forks Terrain Blend");
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] map = new float[h, w, layers];
        Vector3 pos = targetTerrain.transform.position;

        try
        {
            for (int z = 0; z < h; z++)
            {
                if ((z & 31) == 0) EditorUtility.DisplayProgressBar("Terrain Painter Pro", "Building Forks/Washington blend...", z / (float)h);
                float nz = z / Mathf.Max(1f, h - 1f);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / Mathf.Max(1f, w - 1f);
                    float wx = pos.x + nx * data.size.x;
                    float wz = pos.z + nz * data.size.z;
                    float slope = data.GetSteepness(nx, nz);
                    float concavity = EvaluateConcavity(nx, nz);
                    float patchA = FractalNoise((wx + 211.3f) / forksPatchScale, (wz - 97.8f) / forksPatchScale, 4, 0.52f);
                    float patchB = FractalNoise((wx - 617.1f) / (forksPatchScale * 2.15f), (wz + 341.6f) / (forksPatchScale * 2.15f), 3, 0.55f);
                    float fine = FractalNoise((wx + 731.4f) / 13f, (wz - 413.7f) / 13f, 3, 0.48f);
                    float roadDistance = roadCacheValid ? roadGrid.Distance(new Vector2(wx, wz), Mathf.Max(forksTransitionWidth + 90f, 160f)) : 9999f;
                    float roadside = 1f - Smooth01((roadDistance - forksRoadsideWidth) / Mathf.Max(0.01f, forksTransitionWidth));
                    float forest = 1f - roadside;
                    float rockSlopeMask = Smooth01((slope - forksRockSlope) / 16f);
                    float mossSlopeMask = 1f - Smooth01((slope - forksMossSlopeMax) / 16f);
                    float hollow = Mathf.Clamp01(0.48f + concavity * 2.6f);

                    float mud = roadside * (0.75f + forksWetness * 0.75f) * Mathf.Lerp(0.72f, 1.32f, patchB);
                    mud += hollow * forksWetness * 0.28f * Mathf.Lerp(0.6f, 1.2f, fine);
                    float moss = forest * mossSlopeMask * forksMossAmount * (0.24f + Smooth01((patchA - 0.38f) / 0.50f) * 1.1f);
                    moss += roadside * mossSlopeMask * forksMossAmount * 0.20f * patchB;
                    moss += hollow * forksMossAmount * 0.30f;
                    float needles = forest * (1f - rockSlopeMask) * forksNeedleAmount * (0.30f + Smooth01((0.70f - patchA) / 0.55f) * 0.82f);
                    float rock = rockSlopeMask * forksRockAmount * (0.58f + patchB * 0.82f);
                    rock += forest * forksRockAmount * 0.10f * Smooth01((fine - 0.62f) / 0.32f);
                    float soil = (0.42f + forest * 0.25f + roadside * 0.12f) * Mathf.Lerp(0.85f, 1.14f, fine);

                    // Every TerrainLayer participates. Multiple layers may share the same Role;
                    // Auto Weight lets you decide how much of that role each texture receives.
                    float[] weights = new float[layers];
                    for (int i = 0; i < layers; i++)
                    {
                        float roleWeight;
                        switch (layerStates[i].role)
                        {
                            case LayerRole.BaseSoil:     roleWeight = soil; break;
                            case LayerRole.WetMud:      roleWeight = mud; break;
                            case LayerRole.PineNeedles: roleWeight = needles; break;
                            case LayerRole.Moss:        roleWeight = moss; break;
                            case LayerRole.Rock:        roleWeight = rock; break;
                            default:                    roleWeight = 0.018f * (0.65f + patchA * 0.70f); break;
                        }

                        // If the user has not assigned any BaseSoil role, layer 0 is the safe base.
                        if (!hasBaseRole && i == 0) roleWeight += soil;
                        weights[i] = Mathf.Max(0f, roleWeight) * Mathf.Max(0f, layerStates[i].proceduralWeight);
                    }

                    float total = 0f;
                    for (int i = 0; i < layers; i++) total += weights[i];
                    if (total <= 0.00001f) { weights[0] = 1f; total = 1f; }
                    for (int i = 0; i < layers; i++) map[z, x, i] = weights[i] / total;
                }
            }

            data.SetAlphamaps(0, 0, map);
            targetTerrain.Flush();
            EditorUtility.SetDirty(data);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private int FindRole(LayerRole role)
    {
        for (int i = 0; i < layerStates.Count; i++) if (layerStates[i].role == role) return i;
        return -1;
    }

    private void BuildRoadCache()
    {
        roadGrid.Clear();
        roadCacheValid = false;
        if (roadRoot == null) { Repaint(); return; }

        MeshFilter[] meshes = roadRoot.GetComponentsInChildren<MeshFilter>(true);
        if (meshes == null || meshes.Length == 0) return;
        const float sampleSpacing = 2.5f;

        for (int m = 0; m < meshes.Length; m++)
        {
            MeshFilter mf = meshes[m];
            if (mf == null || mf.sharedMesh == null) continue;
            Mesh mesh = mf.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                Vector3 a = mf.transform.TransformPoint(vertices[triangles[t]]);
                Vector3 b = mf.transform.TransformPoint(vertices[triangles[t + 1]]);
                Vector3 c = mf.transform.TransformPoint(vertices[triangles[t + 2]]);
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (normal.y < 0.15f) continue;

                AddRoadPoint(a); AddRoadPoint(b); AddRoadPoint(c);
                float longest = Mathf.Max(Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)),
                    Mathf.Max(Vector2.Distance(new Vector2(b.x, b.z), new Vector2(c.x, c.z)), Vector2.Distance(new Vector2(c.x, c.z), new Vector2(a.x, a.z))));
                int steps = Mathf.Clamp(Mathf.CeilToInt(longest / sampleSpacing), 1, 12);
                for (int i = 1; i < steps; i++)
                {
                    float q = i / (float)steps;
                    AddRoadPoint(Vector3.Lerp(a, b, q));
                    AddRoadPoint(Vector3.Lerp(b, c, q));
                    AddRoadPoint(Vector3.Lerp(c, a, q));
                }
                AddRoadPoint((a + b + c) / 3f);
            }
        }

        roadCacheValid = roadGrid.pointCount > 0;
        Repaint();
    }

    private void AddRoadPoint(Vector3 p) { roadGrid.Add(new Vector2(p.x, p.z)); }

    private void AddExistingLayer(TerrainLayer layer)
    {
        if (layer == null) return;
        TerrainData data = targetTerrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Add Terrain Layer");
        List<TerrainLayer> list = new List<TerrainLayer>(data.terrainLayers) { layer };
        data.terrainLayers = list.ToArray();
        SyncLayerStates();
        selectedLayer = data.terrainLayers.Length - 1;
        EditorUtility.SetDirty(data);
        targetTerrain.Flush();
    }

    private void RemoveLayer(int index)
    {
        TerrainData data = targetTerrain.terrainData;
        TerrainLayer[] oldLayers = data.terrainLayers;
        if (index < 0 || index >= oldLayers.Length) return;
        if (oldLayers.Length <= 1)
        {
            EditorUtility.DisplayDialog("Terrain Painter Pro", "Son TerrainLayer silinemez. Önce başka bir layer ekle.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Remove Terrain Layer", "'" + oldLayers[index].name + "' layer'ını kaldırmak istiyor musun?", "Remove", "Cancel")) return;

        Undo.RegisterCompleteObjectUndo(data, "Remove Terrain Layer");
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] oldMap = data.GetAlphamaps(0, 0, w, h);
        List<TerrainLayer> newLayers = new List<TerrainLayer>(oldLayers);
        newLayers.RemoveAt(index);
        data.terrainLayers = newLayers.ToArray();
        float[,,] newMap = new float[h, w, newLayers.Count];

        for (int z = 0; z < h; z++)
        {
            for (int x = 0; x < w; x++)
            {
                int dst = 0;
                for (int src = 0; src < oldLayers.Length; src++)
                {
                    if (src == index) continue;
                    newMap[z, x, dst++] = oldMap[z, x, src];
                }
                float total = 0f;
                for (int i = 0; i < newLayers.Count; i++) total += newMap[z, x, i];
                if (total <= 0.00001f) newMap[z, x, 0] = 1f;
                else
                {
                    float inv = 1f / total;
                    for (int i = 0; i < newLayers.Count; i++) newMap[z, x, i] *= inv;
                }
            }
        }

        data.SetAlphamaps(0, 0, newMap);
        if (index < layerStates.Count) layerStates.RemoveAt(index);
        selectedLayer = Mathf.Clamp(selectedLayer, 0, newLayers.Count - 1);
        eraseToLayer = Mathf.Clamp(eraseToLayer, 0, newLayers.Count - 1);
        EditorUtility.SetDirty(data);
        targetTerrain.Flush();
    }

    private void MoveLayer(int from, int to)
    {
        TerrainData data = targetTerrain.terrainData;
        int count = data.terrainLayers.Length;
        if (from < 0 || from >= count || to < 0 || to >= count || from == to) return;
        Undo.RegisterCompleteObjectUndo(data, "Reorder Terrain Layer");

        TerrainLayer[] layers = data.terrainLayers;
        TerrainLayer temp = layers[from]; layers[from] = layers[to]; layers[to] = temp;
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] map = data.GetAlphamaps(0, 0, w, h);
        for (int z = 0; z < h; z++)
        for (int x = 0; x < w; x++)
        {
            float a = map[z, x, from];
            map[z, x, from] = map[z, x, to];
            map[z, x, to] = a;
        }

        data.terrainLayers = layers;
        data.SetAlphamaps(0, 0, map);
        LayerUiState state = layerStates[from]; layerStates[from] = layerStates[to]; layerStates[to] = state;
        if (selectedLayer == from) selectedLayer = to; else if (selectedLayer == to) selectedLayer = from;
        EditorUtility.SetDirty(data);
        targetTerrain.Flush();
    }

    private void DuplicateSelectedLayer()
    {
        TerrainData data = targetTerrain.terrainData;
        if (selectedLayer < 0 || selectedLayer >= data.terrainLayers.Length) return;
        TerrainLayer source = data.terrainLayers[selectedLayer];
        if (source == null) return;

        const string folder = "Assets/GeneratedTerrainLayers";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "GeneratedTerrainLayers");
        TerrainLayer copy = Instantiate(source);
        copy.name = source.name + "_Copy";
        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + copy.name + ".terrainlayer");
        AssetDatabase.CreateAsset(copy, path);
        AssetDatabase.SaveAssets();
        AddExistingLayer(copy);
    }

    private void CreateLayerFromMaps()
    {
        if (newAlbedo == null)
        {
            EditorUtility.DisplayDialog("Terrain Painter Pro", "En az Albedo texture seç.", "OK");
            return;
        }

        const string folder = "Assets/GeneratedTerrainLayers";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "GeneratedTerrainLayers");
        TerrainLayer layer = new TerrainLayer();
        layer.name = string.IsNullOrWhiteSpace(newLayerName) ? "TerrainLayer" : SanitizeName(newLayerName);
        layer.diffuseTexture = newAlbedo;
        layer.normalMapTexture = newNormal;
        layer.maskMapTexture = newMaskMap;
        layer.tileSize = new Vector2(Mathf.Max(0.05f, newLayerTileSize.x), Mathf.Max(0.05f, newLayerTileSize.y));
        layer.normalScale = newLayerNormalScale;
        layer.metallic = 0f;
        layer.smoothness = newLayerSmoothness;
        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + layer.name + ".terrainlayer");
        AssetDatabase.CreateAsset(layer, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AddExistingLayer(layer);
    }

    private static string SanitizeName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value.Replace(" ", "_");
    }

    private void NormalizeAllWeights()
    {
        TerrainData data = targetTerrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Normalize Terrain Splat Weights");
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] map = data.GetAlphamaps(0, 0, w, h);
        for (int z = 0; z < h; z++) for (int x = 0; x < w; x++) NormalizePixel(map, z, x);
        data.SetAlphamaps(0, 0, map);
        targetTerrain.Flush();
        EditorUtility.SetDirty(data);
    }

    private void ClearSelectedLayer()
    {
        TerrainData data = targetTerrain.terrainData;
        if (data.alphamapLayers <= 1) return;
        Undo.RegisterCompleteObjectUndo(data, "Clear Terrain Layer");
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] map = data.GetAlphamaps(0, 0, w, h);
        int fallback = selectedLayer == 0 ? 1 : 0;
        for (int z = 0; z < h; z++)
        for (int x = 0; x < w; x++)
        {
            float amount = map[z, x, selectedLayer];
            map[z, x, selectedLayer] = 0f;
            map[z, x, fallback] += amount;
            NormalizePixel(map, z, x);
        }
        data.SetAlphamaps(0, 0, map);
        targetTerrain.Flush();
        EditorUtility.SetDirty(data);
    }

    private void FillSelectedLayer()
    {
        TerrainData data = targetTerrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Fill Terrain Layer");
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        int layers = data.alphamapLayers;
        float[,,] map = new float[h, w, layers];
        for (int z = 0; z < h; z++) for (int x = 0; x < w; x++) map[z, x, selectedLayer] = 1f;
        data.SetAlphamaps(0, 0, map);
        targetTerrain.Flush();
        EditorUtility.SetDirty(data);
    }

    private static float FractalNoise(float x, float y, int octaves, float persistence)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float max = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }
        return max > 0f ? total / max : 0f;
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
#endif
