using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadNetwork))]
public class RoadNetworkEditor : Editor
{
    static bool drawMode;
    static bool paintMode;
    static int activeRoadIndex;
    static int selectedPointIndex = -1;
    static bool showAdvanced;
    static float lastPaintDistance = -9999f;

    RoadNetwork Network => (RoadNetwork)target;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Road Tool", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = drawMode ? new Color(0.45f, 0.85f, 0.55f) : Color.white;
            if (GUILayout.Button(drawMode ? "Draw Mode: ON" : "Draw Mode: OFF", GUILayout.Height(34)))
            {
                drawMode = !drawMode;
                if (drawMode)
                    paintMode = false;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = paintMode ? new Color(0.55f, 0.72f, 1f) : Color.white;
            if (GUILayout.Button(paintMode ? "Paint Mode: ON" : "Paint Mode: OFF", GUILayout.Height(34)))
            {
                paintMode = !paintMode;
                if (paintMode)
                    drawMode = false;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Rebuild All", GUILayout.Height(34)))
            {
                Undo.RecordObject(Network, "Rebuild Road Meshes");
                Network.RebuildAllMeshes();
                EditorUtility.SetDirty(Network);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Road"))
            {
                Undo.RecordObject(Network, "Add Road");
                activeRoadIndex = Network.AddRoad();
                selectedPointIndex = -1;
                EditorUtility.SetDirty(Network);
            }

            if (GUILayout.Button("Apply Terrain"))
            {
                if (Network.terrain != null)
                    Undo.RegisterCompleteObjectUndo(Network.terrain.terrainData, "Apply Road Terrain");
                Network.ApplyTerrain();
                EditorUtility.SetDirty(Network);
            }

            if (GUILayout.Button("Bake Mesh"))
                Network.BakeGeneratedMeshes();

            if (GUILayout.Button("Bake Paint"))
                Network.BakePaintMeshes();
        }

        activeRoadIndex = Mathf.Clamp(activeRoadIndex, 0, Mathf.Max(0, Network.roads.Count - 1));
        if (Network.roads.Count > 0)
        {
            string[] names = new string[Network.roads.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = string.IsNullOrWhiteSpace(Network.roads[i].name) ? $"Road {i + 1}" : Network.roads[i].name;
            activeRoadIndex = EditorGUILayout.Popup("Active Road", activeRoadIndex, names);
        }

        DrawSourceObjectPanel();
        DrawActiveRoadPanel();
        DrawPaintBrushPanel();
        DrawTerrainPanel();

        EditorGUILayout.HelpBox(
            "Draw Mode yol noktasi ekler. Paint Mode yol ustune brush/stamp boyar. Paint Mode'da sol tik/surukle boya, Shift sil, Ctrl daha hafif boya.",
            MessageType.Info);

        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Raw Inspector");
        if (showAdvanced)
        {
            EditorGUILayout.Space(4);
            DrawDefaultInspector();
        }
    }

    void DrawSourceObjectPanel()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Road Source", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        GameObject source = (GameObject)EditorGUILayout.ObjectField("Road Source Object", Network.roadSourceObject, typeof(GameObject), true);
        bool useMaterial = EditorGUILayout.Toggle("Use Source Material", Network.useSourceMaterial);
        bool useWidth = EditorGUILayout.Toggle("Use Source Width", Network.useSourceWidth);
        bool useMeshTiles = EditorGUILayout.Toggle("Use Source Mesh Tiles", Network.useSourceMeshTiles);
        float multiplier = EditorGUILayout.FloatField("Width Multiplier", Network.sourceWidthMultiplier);
        float minimumWidth = EditorGUILayout.FloatField("Minimum Width", Network.minimumSourceWidth);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(Network, "Change Road Source");
            Network.roadSourceObject = source;
            Network.useSourceMaterial = useMaterial;
            Network.useSourceWidth = useWidth;
            Network.useSourceMeshTiles = useMeshTiles;
            Network.sourceWidthMultiplier = Mathf.Max(0.1f, multiplier);
            Network.minimumSourceWidth = Mathf.Max(0.1f, minimumWidth);
            Network.ApplySourceObjectToRoad(activeRoadIndex);
            EditorUtility.SetDirty(Network);
            SceneView.RepaintAll();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Source to Road"))
            {
                Undo.RecordObject(Network, "Apply Road Source");
                Network.ApplySourceObjectToRoad(activeRoadIndex);
                EditorUtility.SetDirty(Network);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Use Selected Object"))
                UseSelectedObjectAsSource();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selected Material"))
                AssignSelectedRendererMaterial();

            if (GUILayout.Button("Create yol Material"))
                CreateYolMaterial();
        }
    }

    void DrawActiveRoadPanel()
    {
        if (Network.roads.Count == 0)
            return;

        RoadNetwork.RoadPath road = Network.ActiveRoad(activeRoadIndex);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Active Road Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        string roadName = EditorGUILayout.TextField("Name", road.name);
        bool enabled = EditorGUILayout.Toggle("Enabled", road.enabled);
        RoadNetwork.GeometryMode geometryMode = (RoadNetwork.GeometryMode)EditorGUILayout.EnumPopup("Geometry Mode", road.geometryMode);
        RoadNetwork.CurveMode curveMode = (RoadNetwork.CurveMode)EditorGUILayout.EnumPopup("Curve Mode", road.curveMode);
        RoadNetwork.HeightMode heightMode = (RoadNetwork.HeightMode)EditorGUILayout.EnumPopup("Height Mode", road.heightMode);
        float width = EditorGUILayout.FloatField("Width", road.width);
        float shoulder = EditorGUILayout.FloatField("Shoulder Width", road.shoulderWidth);
        float samples = EditorGUILayout.Slider("Samples Per Meter", road.samplesPerMeter, 0.05f, 2f);
        float uv = EditorGUILayout.FloatField("UV Meters Per Tile", road.uvMetersPerTile);
        float yOffset = EditorGUILayout.FloatField("Road Y Offset", road.roadYOffset);
        int smoothWindow = EditorGUILayout.IntSlider("Smooth Grade", road.smoothGradeWindow, 1, 41);
        Material material = (Material)EditorGUILayout.ObjectField("Road Material", road.roadMaterial, typeof(Material), false);
        bool randomizeMaterials = EditorGUILayout.Toggle("Randomize Materials", road.randomizeMaterials);
        int materialSeed = EditorGUILayout.IntField("Material Seed", road.materialSeed);
        float materialRunMeters = EditorGUILayout.FloatField("Material Run Meters", road.materialRunMeters);
        RoadNetwork.UvMode uvMode = (RoadNetwork.UvMode)EditorGUILayout.EnumPopup("UV Mode", road.uvMode);
        Vector2 uvScale = EditorGUILayout.Vector2Field("UV Scale", road.uvScale);
        Vector2 uvOffset = EditorGUILayout.Vector2Field("UV Offset", road.uvOffset);
        bool swapUv = EditorGUILayout.Toggle("Swap U/V", road.swapUv);
        bool flipU = EditorGUILayout.Toggle("Flip U", road.flipU);
        bool flipV = EditorGUILayout.Toggle("Flip V", road.flipV);
        bool flipSourceForward = EditorGUILayout.Toggle("Flip Source Forward", road.flipSourceForward);
        bool enableDeformation = EditorGUILayout.Toggle("Enable Deformation", road.enableDeformation);
        int deformationSeed = EditorGUILayout.IntField("Deformation Seed", road.deformationSeed);
        float deformationScale = EditorGUILayout.FloatField("Deformation Scale", road.deformationScale);
        float surfaceHeightNoise = EditorGUILayout.Slider("Surface Height Noise", road.surfaceHeightNoise, 0f, 1f);
        float edgeWidthNoise = EditorGUILayout.Slider("Edge Width Noise", road.edgeWidthNoise, 0f, 2f);
        float flatSectionChance = EditorGUILayout.Slider("Flat Section Chance", road.flatSectionChance, 0f, 1f);
        float flatSectionMeters = EditorGUILayout.FloatField("Flat Section Meters", road.flatSectionMeters);
        bool enableOverlayProjection = EditorGUILayout.Toggle("Overlay Projection", road.enableOverlayProjection);
        int overlaySeed = EditorGUILayout.IntField("Overlay Seed", road.overlaySeed);
        float overlayDensity = EditorGUILayout.Slider("Overlay Density", road.overlayDensity, 0f, 1f);
        float overlayPatchMeters = EditorGUILayout.FloatField("Overlay Patch Meters", road.overlayPatchMeters);
        float overlayMinWidth = EditorGUILayout.Slider("Overlay Min Width", road.overlayMinWidth, 0.05f, 1f);
        float overlayMaxWidth = EditorGUILayout.Slider("Overlay Max Width", road.overlayMaxWidth, 0.05f, 1.25f);
        float overlayAlpha = EditorGUILayout.Slider("Overlay Alpha", road.overlayAlpha, 0.01f, 1f);
        float overlayYOffset = EditorGUILayout.Slider("Overlay Y Offset", road.overlayYOffset, 0.001f, 0.15f);
        int overlayLayers = EditorGUILayout.IntSlider("Overlay Layers", road.overlayLayers, 1, 5);
        float overlayEdgeJitter = EditorGUILayout.Slider("Overlay Edge Jitter", road.overlayEdgeJitter, 0f, 1f);
        float overlayCenterJitter = EditorGUILayout.Slider("Overlay Center Jitter", road.overlayCenterJitter, 0f, 1f);
        float overlayUvRandomness = EditorGUILayout.Slider("Overlay UV Random", road.overlayUvRandomness, 0f, 1f);
        bool collider = EditorGUILayout.Toggle("Update Collider", road.updateCollider);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(Network, "Change Active Road");
            road.name = roadName;
            road.enabled = enabled;
            road.geometryMode = geometryMode;
            road.curveMode = curveMode;
            road.heightMode = heightMode;
            road.width = Mathf.Max(0.5f, width);
            road.shoulderWidth = Mathf.Max(0f, shoulder);
            road.samplesPerMeter = Mathf.Clamp(samples, 0.05f, 2f);
            road.uvMetersPerTile = Mathf.Max(0.25f, uv);
            road.roadYOffset = yOffset;
            road.smoothGradeWindow = Mathf.Clamp(smoothWindow, 1, 41);
            road.roadMaterial = material;
            road.randomizeMaterials = randomizeMaterials;
            road.materialSeed = materialSeed;
            road.materialRunMeters = Mathf.Max(1f, materialRunMeters);
            road.uvMode = uvMode;
            road.uvScale = new Vector2(Mathf.Approximately(uvScale.x, 0f) ? 1f : uvScale.x, Mathf.Approximately(uvScale.y, 0f) ? 1f : uvScale.y);
            road.uvOffset = uvOffset;
            road.swapUv = swapUv;
            road.flipU = flipU;
            road.flipV = flipV;
            road.flipSourceForward = flipSourceForward;
            road.enableDeformation = enableDeformation;
            road.deformationSeed = deformationSeed;
            road.deformationScale = Mathf.Max(0.1f, deformationScale);
            road.surfaceHeightNoise = Mathf.Clamp01(surfaceHeightNoise);
            road.edgeWidthNoise = Mathf.Clamp(edgeWidthNoise, 0f, 2f);
            road.flatSectionChance = Mathf.Clamp01(flatSectionChance);
            road.flatSectionMeters = Mathf.Max(1f, flatSectionMeters);
            road.enableOverlayProjection = enableOverlayProjection;
            road.overlaySeed = overlaySeed;
            road.overlayDensity = Mathf.Clamp01(overlayDensity);
            road.overlayPatchMeters = Mathf.Max(1f, overlayPatchMeters);
            road.overlayMinWidth = Mathf.Clamp(overlayMinWidth, 0.05f, 1f);
            road.overlayMaxWidth = Mathf.Clamp(Mathf.Max(overlayMinWidth, overlayMaxWidth), 0.05f, 1.25f);
            road.overlayAlpha = Mathf.Clamp(overlayAlpha, 0.01f, 1f);
            road.overlayYOffset = Mathf.Clamp(overlayYOffset, 0.001f, 0.15f);
            road.overlayLayers = Mathf.Clamp(overlayLayers, 1, 5);
            road.overlayEdgeJitter = Mathf.Clamp01(overlayEdgeJitter);
            road.overlayCenterJitter = Mathf.Clamp01(overlayCenterJitter);
            road.overlayUvRandomness = Mathf.Clamp01(overlayUvRandomness);
            road.updateCollider = collider;
            if (Network.autoRebuildMeshes)
                Network.RebuildAllMeshes();
            EditorUtility.SetDirty(Network);
            SceneView.RepaintAll();
        }

        DrawMaterialVariantsPanel(road);
        DrawUvPresetButtons(road);
        DrawRealismPresetButtons(road);
    }

    void DrawMaterialVariantsPanel(RoadNetwork.RoadPath road)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Material Variants", EditorStyles.boldLabel);

        if (road.materialVariants == null)
            road.materialVariants = new List<Material>();

        int removeIndex = -1;
        for (int i = 0; i < road.materialVariants.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                Material material = (Material)EditorGUILayout.ObjectField($"Variant {i + 1}", road.materialVariants[i], typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Network, "Change Material Variant");
                    road.materialVariants[i] = material;
                    Network.RebuildAllMeshes();
                    EditorUtility.SetDirty(Network);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("X", GUILayout.Width(26)))
                    removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            Undo.RecordObject(Network, "Remove Material Variant");
            road.materialVariants.RemoveAt(removeIndex);
            Network.RebuildAllMeshes();
            EditorUtility.SetDirty(Network);
            SceneView.RepaintAll();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Slot"))
            {
                Undo.RecordObject(Network, "Add Material Variant Slot");
                road.materialVariants.Add(null);
                EditorUtility.SetDirty(Network);
            }

            if (GUILayout.Button("Add Selected Materials"))
                AddSelectedMaterialsToRoad();

            if (GUILayout.Button("Load Assets/yol Materials"))
                LoadYolMaterialsToRoad();
        }
    }

    void DrawPaintBrushPanel()
    {
        if (Network.roads.Count == 0)
            return;

        RoadNetwork.RoadPath road = Network.ActiveRoad(activeRoadIndex);
        if (road.brush == null)
            road.brush = new RoadNetwork.RoadBrushSettings();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Paint Brush", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        Material brushMaterial = (Material)EditorGUILayout.ObjectField("Brush Material", road.brush.brushMaterial, typeof(Material), false);
        Texture2D brushTexture = (Texture2D)EditorGUILayout.ObjectField("Brush Texture", road.brush.brushTexture, typeof(Texture2D), false);
        bool randomPalette = EditorGUILayout.Toggle("Palette Random", road.brush.randomPalette);
        float size = EditorGUILayout.FloatField("Brush Size", road.brush.size);
        float opacity = EditorGUILayout.Slider("Opacity", road.brush.opacity, 0.01f, 1f);
        float spacing = EditorGUILayout.FloatField("Spacing", road.brush.spacing);
        float falloff = EditorGUILayout.Slider("Falloff", road.brush.falloff, 0f, 1f);
        float randomRotation = EditorGUILayout.Slider("Random Rotation", road.brush.randomRotation, 0f, 1f);
        float randomScale = EditorGUILayout.Slider("Random Scale", road.brush.randomScale, 0f, 1f);
        float randomOffset = EditorGUILayout.Slider("Random Offset", road.brush.randomOffset, 0f, 1f);
        float edgeJitter = EditorGUILayout.Slider("Edge Jitter", road.brush.edgeJitter, 0f, 1f);
        float yOffset = EditorGUILayout.Slider("Paint Y Offset", road.brush.yOffset, 0.001f, 0.15f);
        RoadNetwork.PaintBlendMode blendMode = (RoadNetwork.PaintBlendMode)EditorGUILayout.EnumPopup("Blend Mode", road.brush.blendMode);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(Network, "Change Paint Brush");
            road.brush.brushMaterial = brushMaterial;
            road.brush.brushTexture = brushTexture;
            road.brush.randomPalette = randomPalette;
            road.brush.size = Mathf.Max(0.1f, size);
            road.brush.opacity = Mathf.Clamp01(opacity);
            road.brush.spacing = Mathf.Max(0.1f, spacing);
            road.brush.falloff = Mathf.Clamp01(falloff);
            road.brush.randomRotation = Mathf.Clamp01(randomRotation);
            road.brush.randomScale = Mathf.Clamp01(randomScale);
            road.brush.randomOffset = Mathf.Clamp01(randomOffset);
            road.brush.edgeJitter = Mathf.Clamp01(edgeJitter);
            road.brush.yOffset = Mathf.Clamp(yOffset, 0.001f, 0.15f);
            road.brush.blendMode = blendMode;
            EditorUtility.SetDirty(Network);
        }

        DrawPaintPalettePanel(road);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Paint"))
            {
                Undo.RecordObject(Network, "Clear Road Paint");
                Network.ClearPaint(activeRoadIndex);
                EditorUtility.SetDirty(Network);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Erase Paint"))
            {
                Undo.RecordObject(Network, "Erase Road Paint");
                road.paintStamps?.Clear();
                Network.RebuildAllMeshes();
                EditorUtility.SetDirty(Network);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Bake Paint Mesh"))
                Network.BakePaintMeshes();
        }
    }

    void DrawPaintPalettePanel(RoadNetwork.RoadPath road)
    {
        if (road.brush.materialPalette == null)
            road.brush.materialPalette = new List<Material>();

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Brush Material Palette", EditorStyles.boldLabel);

        int removeIndex = -1;
        for (int i = 0; i < road.brush.materialPalette.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                Material material = (Material)EditorGUILayout.ObjectField($"Paint {i + 1}", road.brush.materialPalette[i], typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Network, "Change Brush Palette");
                    road.brush.materialPalette[i] = material;
                    EditorUtility.SetDirty(Network);
                }

                if (GUILayout.Button("X", GUILayout.Width(26)))
                    removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            Undo.RecordObject(Network, "Remove Brush Palette Material");
            road.brush.materialPalette.RemoveAt(removeIndex);
            EditorUtility.SetDirty(Network);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Paint Slot"))
            {
                Undo.RecordObject(Network, "Add Brush Palette Slot");
                road.brush.materialPalette.Add(null);
                EditorUtility.SetDirty(Network);
            }

            if (GUILayout.Button("Add Selected Paint Sources"))
                AddSelectedPaintSources();

            if (GUILayout.Button("Load Assets/yol Paint"))
                LoadYolPaintSources();
        }
    }

    void DrawUvPresetButtons(RoadNetwork.RoadPath road)
    {
        EditorGUILayout.Space(3);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("UV Reset"))
                ApplyUvPreset(road, RoadNetwork.UvMode.SourceUv, Vector2.one, Vector2.zero, false, false, false);

            if (GUILayout.Button("Source UV"))
                ApplyUvPreset(road, RoadNetwork.UvMode.SourceUv, road.uvScale, road.uvOffset, road.swapUv, road.flipU, road.flipV);

            if (GUILayout.Button("Swap"))
                ApplyUvPreset(road, road.uvMode, road.uvScale, road.uvOffset, !road.swapUv, road.flipU, road.flipV);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Flip U"))
                ApplyUvPreset(road, road.uvMode, road.uvScale, road.uvOffset, road.swapUv, !road.flipU, road.flipV);

            if (GUILayout.Button("Flip V"))
                ApplyUvPreset(road, road.uvMode, road.uvScale, road.uvOffset, road.swapUv, road.flipU, !road.flipV);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generated UV"))
                ApplyUvPreset(road, RoadNetwork.UvMode.RoadGeneratedUv, new Vector2(1f, 1f), Vector2.zero, false, false, false);

            if (GUILayout.Button("Tile x2"))
                ApplyUvPreset(road, road.uvMode, Vector2.Scale(road.uvScale, new Vector2(1f, 2f)), road.uvOffset, road.swapUv, road.flipU, road.flipV);

            if (GUILayout.Button("Tile x0.5"))
                ApplyUvPreset(road, road.uvMode, Vector2.Scale(road.uvScale, new Vector2(1f, 0.5f)), road.uvOffset, road.swapUv, road.flipU, road.flipV);
        }
    }

    void ApplyUvPreset(RoadNetwork.RoadPath road, RoadNetwork.UvMode mode, Vector2 scale, Vector2 offset, bool swap, bool flipU, bool flipV)
    {
        Undo.RecordObject(Network, "Change Road UV");
        road.uvMode = mode;
        road.uvScale = scale;
        road.uvOffset = offset;
        road.swapUv = swap;
        road.flipU = flipU;
        road.flipV = flipV;
        Network.RebuildAllMeshes();
        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
    }

    void DrawRealismPresetButtons(RoadNetwork.RoadPath road)
    {
        EditorGUILayout.Space(3);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clean Road"))
                ApplyRealismPreset(road, false, 0.02f, 0.04f, 0.75f, 28f, false, 0.15f, 18f, 0.2f, 0.65f, 0.25f);

            if (GUILayout.Button("Realistic Mix"))
                ApplyRealismPreset(road, true, 0.035f, 0.18f, 0.35f, 24f, true, 0.72f, 9f, 0.18f, 1f, 0.46f, 3, 0.75f, 0.65f, 0.95f);

            if (GUILayout.Button("Damaged"))
                ApplyRealismPreset(road, true, 0.075f, 0.42f, 0.15f, 16f, true, 0.95f, 6f, 0.08f, 1.15f, 0.62f, 4, 0.95f, 0.85f, 1f);
        }
    }

    void ApplyRealismPreset(RoadNetwork.RoadPath road, bool enabled, float surfaceNoise, float edgeNoise, float flatChance, float flatMeters, bool overlayEnabled, float overlayDensity, float overlayMeters, float overlayMinWidth, float overlayMaxWidth, float overlayAlpha, int overlayLayers = 1, float edgeJitter = 0.4f, float centerJitter = 0.4f, float uvRandomness = 0.7f)
    {
        Undo.RecordObject(Network, "Change Road Realism");
        road.enableDeformation = enabled;
        road.surfaceHeightNoise = surfaceNoise;
        road.edgeWidthNoise = edgeNoise;
        road.flatSectionChance = flatChance;
        road.flatSectionMeters = flatMeters;
        road.enableOverlayProjection = overlayEnabled;
        road.overlayDensity = overlayDensity;
        road.overlayPatchMeters = overlayMeters;
        road.overlayMinWidth = overlayMinWidth;
        road.overlayMaxWidth = overlayMaxWidth;
        road.overlayAlpha = overlayAlpha;
        road.overlayLayers = overlayLayers;
        road.overlayEdgeJitter = edgeJitter;
        road.overlayCenterJitter = centerJitter;
        road.overlayUvRandomness = uvRandomness;
        Network.RebuildAllMeshes();
        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
    }

    void DrawTerrainPanel()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Terrain", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        Terrain terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", Network.terrain, typeof(Terrain), true);
        bool autoRebuild = EditorGUILayout.Toggle("Auto Rebuild Meshes", Network.autoRebuildMeshes);
        bool deform = EditorGUILayout.Toggle("Deform Terrain Heights", Network.deformTerrainHeights);
        float heightOffset = EditorGUILayout.FloatField("Terrain Height Offset", Network.terrainHeightOffset);
        TerrainLayer roadLayer = (TerrainLayer)EditorGUILayout.ObjectField("Road Layer", Network.roadLayer, typeof(TerrainLayer), false);
        TerrainLayer shoulderLayer = (TerrainLayer)EditorGUILayout.ObjectField("Shoulder Layer", Network.shoulderLayer, typeof(TerrainLayer), false);
        float treeClearance = EditorGUILayout.FloatField("Tree Clearance", Network.treeClearance);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(Network, "Change Road Terrain Settings");
            Network.terrain = terrain;
            Network.autoRebuildMeshes = autoRebuild;
            Network.deformTerrainHeights = deform;
            Network.terrainHeightOffset = heightOffset;
            Network.roadLayer = roadLayer;
            Network.shoulderLayer = shoulderLayer;
            Network.treeClearance = Mathf.Max(0f, treeClearance);
            EditorUtility.SetDirty(Network);
        }
    }

    void OnSceneGUI()
    {
        RoadNetwork network = Network;
        if (network.roads == null || network.roads.Count == 0)
            return;

        activeRoadIndex = Mathf.Clamp(activeRoadIndex, 0, network.roads.Count - 1);
        RoadNetwork.RoadPath road = network.ActiveRoad(activeRoadIndex);

        DrawRoadPreview(network, road);
        DrawPointHandles(network, road);

        if (paintMode)
        {
            HandlePaintSceneGUI(network, road);
            return;
        }

        if (!drawMode)
            return;

        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        DrawCursorPreview();

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            if (TryGetPlacement(out Vector3 position))
            {
                Undo.RecordObject(network, "Add Road Point");
                network.AddPoint(activeRoadIndex, position);
                selectedPointIndex = road.points.Count - 1;
                EditorUtility.SetDirty(network);
                e.Use();
            }
        }

        if ((e.type == EventType.KeyDown) && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
        {
            if (selectedPointIndex >= 0 && selectedPointIndex < road.points.Count)
            {
                Undo.RecordObject(network, "Delete Road Point");
                network.RemovePoint(activeRoadIndex, selectedPointIndex);
                selectedPointIndex = Mathf.Min(selectedPointIndex, road.points.Count - 1);
                EditorUtility.SetDirty(network);
                e.Use();
            }
        }
    }

    void HandlePaintSceneGUI(RoadNetwork network, RoadNetwork.RoadPath road)
    {
        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        if (!TryGetPlacement(out Vector3 position))
            return;

        if (!network.TryProjectWorldToRoad(activeRoadIndex, position, out float distance, out float sideOffset, out RoadNetwork.RoadFrame frame))
            return;

        bool erase = e.shift;
        bool soft = e.control;
        DrawPaintCursor(network, road, frame, sideOffset, erase, soft);

        if ((e.type != EventType.MouseDown && e.type != EventType.MouseDrag) || e.button != 0 || e.alt)
            return;

        float spacing = road.brush.spacing * (soft ? 1.5f : 1f);
        if (!erase && Mathf.Abs(distance - lastPaintDistance) < spacing && e.type == EventType.MouseDrag)
            return;

        Undo.RecordObject(network, erase ? "Erase Road Paint" : "Paint Road");

        if (erase)
        {
            network.RemovePaintStampsNear(activeRoadIndex, distance, sideOffset, road.brush.size);
        }
        else
        {
            RoadNetwork.PaintStamp stamp = CreatePaintStamp(road, distance, sideOffset, soft);
            network.AddPaintStamp(activeRoadIndex, stamp);
            lastPaintDistance = distance;
        }

        EditorUtility.SetDirty(network);
        SceneView.RepaintAll();
        e.Use();
    }

    void DrawPaintCursor(RoadNetwork network, RoadNetwork.RoadPath road, RoadNetwork.RoadFrame frame, float sideOffset, bool erase, bool soft)
    {
        Vector3 center = network.transform.TransformPoint(frame.center + frame.right * sideOffset + Vector3.up * road.brush.yOffset);
        float radius = road.brush.size * (soft ? 0.8f : 1f);
        Handles.color = erase ? new Color(1f, 0.2f, 0.15f, 0.9f) : new Color(0.25f, 0.7f, 1f, 0.9f);
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.color = erase ? new Color(1f, 0.2f, 0.15f, 0.18f) : new Color(0.25f, 0.7f, 1f, 0.14f);
        Handles.DrawSolidDisc(center, Vector3.up, radius);
    }

    RoadNetwork.PaintStamp CreatePaintStamp(RoadNetwork.RoadPath road, float distance, float sideOffset, bool soft)
    {
        int seed = Mathf.Abs(Mathf.RoundToInt(distance * 37f + sideOffset * 101f + road.paintStamps.Count * 9973f));
        Material material = road.brush.brushMaterial;
        if (road.brush.randomPalette && road.brush.materialPalette != null && road.brush.materialPalette.Count > 0)
        {
            List<Material> palette = new List<Material>();
            foreach (Material paletteMaterial in road.brush.materialPalette)
            {
                if (paletteMaterial != null)
                    palette.Add(paletteMaterial);
            }

            if (palette.Count > 0)
                material = palette[Mathf.Abs(seed) % palette.Count];
        }

        float randomScale = Mathf.Lerp(1f - road.brush.randomScale, 1f + road.brush.randomScale, Hash01(seed, 17, 3));
        float randomSide = Mathf.Lerp(-road.brush.randomOffset, road.brush.randomOffset, Hash01(seed, 29, 5)) * road.brush.size;
        float randomForward = Mathf.Lerp(-road.brush.randomOffset, road.brush.randomOffset, Hash01(seed, 41, 7)) * road.brush.size;

        return new RoadNetwork.PaintStamp
        {
            distance = distance + randomForward,
            sideOffset = sideOffset + randomSide,
            radius = road.brush.size,
            opacity = road.brush.opacity * (soft ? 0.45f : 1f),
            rotation = Mathf.Lerp(0f, 360f, Hash01(seed, 53, 11)) * road.brush.randomRotation,
            scale = randomScale,
            falloff = road.brush.falloff,
            edgeJitter = road.brush.edgeJitter,
            seed = seed,
            material = material,
            texture = material == null ? road.brush.brushTexture : null,
            blendMode = road.brush.blendMode
        };
    }

    void DrawPointHandles(RoadNetwork network, RoadNetwork.RoadPath road)
    {
        for (int i = 0; i < road.points.Count; i++)
        {
            Vector3 world = network.GetPointWorld(activeRoadIndex, i);
            float size = HandleUtility.GetHandleSize(world) * 0.12f;
            Handles.color = i == selectedPointIndex ? Color.yellow : new Color(0.2f, 0.75f, 1f);

            if (Handles.Button(world, Quaternion.identity, size, size * 1.35f, Handles.SphereHandleCap))
            {
                selectedPointIndex = i;
                SceneView.RepaintAll();
            }

            Handles.Label(world + Vector3.up * size * 1.5f, i.ToString());
        }

        if (selectedPointIndex < 0 || selectedPointIndex >= road.points.Count)
            return;

        EditorGUI.BeginChangeCheck();
        Vector3 selectedWorld = network.GetPointWorld(activeRoadIndex, selectedPointIndex);
        Vector3 moved = Handles.PositionHandle(selectedWorld, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(network, "Move Road Point");
            network.SetPointWorld(activeRoadIndex, selectedPointIndex, moved);
            EditorUtility.SetDirty(network);
        }
    }

    void DrawRoadPreview(RoadNetwork network, RoadNetwork.RoadPath road)
    {
        if (road.points.Count < 2)
            return;

        Handles.color = new Color(1f, 0.72f, 0.15f, 1f);
        for (int i = 0; i < road.points.Count - 1; i++)
            Handles.DrawAAPolyLine(4f, network.GetPointWorld(activeRoadIndex, i), network.GetPointWorld(activeRoadIndex, i + 1));

        Handles.color = new Color(1f, 0.72f, 0.15f, 0.18f);
        for (int i = 0; i < road.points.Count; i++)
            Handles.DrawWireDisc(network.GetPointWorld(activeRoadIndex, i), Vector3.up, road.width * 0.5f);
    }

    void DrawCursorPreview()
    {
        if (!TryGetPlacement(out Vector3 position))
            return;

        Handles.color = new Color(0.25f, 1f, 0.45f, 0.9f);
        float size = HandleUtility.GetHandleSize(position) * 0.18f;
        Handles.DrawWireDisc(position, Vector3.up, size);
        Handles.DrawLine(position - Vector3.right * size, position + Vector3.right * size);
        Handles.DrawLine(position - Vector3.forward * size, position + Vector3.forward * size);
        SceneView.RepaintAll();
    }

    static bool TryGetPlacement(out Vector3 position)
    {
        if (HandleUtility.PlaceObject(Event.current.mousePosition, out position, out _))
            return true;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float distance))
        {
            position = ray.GetPoint(distance);
            return true;
        }

        position = default;
        return false;
    }

    [MenuItem("GameObject/Road Tool/Create Road Network", false, 10)]
    static void CreateRoadNetwork()
    {
        GameObject obj = new GameObject("Road Network");
        Undo.RegisterCreatedObjectUndo(obj, "Create Road Network");
        RoadNetwork network = obj.AddComponent<RoadNetwork>();
        network.terrain = Terrain.activeTerrain;
        Selection.activeGameObject = obj;
    }

    void AssignSelectedRendererMaterial()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == Network.gameObject)
                continue;

            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Undo.RecordObject(Network, "Assign Road Material");
                Network.ActiveRoad(activeRoadIndex).roadMaterial = renderer.sharedMaterial;
                Network.RebuildAllMeshes();
                EditorUtility.SetDirty(Network);
                return;
            }
        }

        Debug.LogWarning("[RoadTool] Material almak icin Road Network ile birlikte material'i olan bir obje de secili olmali.");
    }

    void AddSelectedMaterialsToRoad()
    {
        List<Material> materials = new List<Material>();
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            if (obj is Material material)
                materials.Add(material);
            else if (obj is GameObject gameObject)
            {
                Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    materials.AddRange(renderer.sharedMaterials);
            }
        }

        if (materials.Count == 0)
        {
            Debug.LogWarning("[RoadTool] Eklenecek material bulunamadi. Project'te material sec veya sahnede material'i olan obje sec.");
            return;
        }

        Undo.RecordObject(Network, "Add Selected Road Materials");
        Network.AddMaterialVariantsToRoad(activeRoadIndex, materials);
        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
    }

    void LoadYolMaterialsToRoad()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/yol" });
        List<Material> materials = new List<Material>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                materials.Add(material);
        }

        if (materials.Count == 0)
        {
            Debug.LogWarning("[RoadTool] Assets/yol altinda material bulunamadi.");
            return;
        }

        Undo.RecordObject(Network, "Load Yol Road Materials");
        Network.AddMaterialVariantsToRoad(activeRoadIndex, materials);
        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
    }

    void AddSelectedPaintSources()
    {
        RoadNetwork.RoadPath road = Network.ActiveRoad(activeRoadIndex);
        if (road.brush.materialPalette == null)
            road.brush.materialPalette = new List<Material>();

        bool changed = false;
        Undo.RecordObject(Network, "Add Selected Paint Sources");
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            if (obj is Material material)
            {
                if (!road.brush.materialPalette.Contains(material))
                    road.brush.materialPalette.Add(material);
                road.brush.brushMaterial = material;
                changed = true;
            }
            else if (obj is Texture2D texture)
            {
                road.brush.brushTexture = texture;
                changed = true;
            }
            else if (obj is GameObject gameObject)
            {
                Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
                if (renderer == null)
                    continue;

                foreach (Material rendererMaterial in renderer.sharedMaterials)
                {
                    if (rendererMaterial != null && !road.brush.materialPalette.Contains(rendererMaterial))
                        road.brush.materialPalette.Add(rendererMaterial);
                }

                if (renderer.sharedMaterial != null)
                    road.brush.brushMaterial = renderer.sharedMaterial;
                changed = true;
            }
        }

        if (!changed)
        {
            Debug.LogWarning("[RoadTool] Paint source icin Project'te material/texture veya sahnede renderer'li obje sec.");
            return;
        }

        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
    }

    void LoadYolPaintSources()
    {
        RoadNetwork.RoadPath road = Network.ActiveRoad(activeRoadIndex);
        if (road.brush.materialPalette == null)
            road.brush.materialPalette = new List<Material>();

        Undo.RecordObject(Network, "Load Yol Paint Sources");
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/yol" });
        foreach (string guid in materialGuids)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null || road.brush.materialPalette.Contains(material))
                continue;

            road.brush.materialPalette.Add(material);
            if (road.brush.brushMaterial == null)
                road.brush.brushMaterial = material;
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/yol" });
        foreach (string guid in textureGuids)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
            if (texture == null)
                continue;

            if (road.brush.brushTexture == null)
                road.brush.brushTexture = texture;
            break;
        }

        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
    }

    void UseSelectedObjectAsSource()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == Network.gameObject)
                continue;

            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            MeshFilter filter = obj.GetComponentInChildren<MeshFilter>();
            if (renderer == null && filter == null)
                continue;

            Undo.RecordObject(Network, "Use Selected Road Source");
            Network.roadSourceObject = obj;
            Network.ApplySourceObjectToRoad(activeRoadIndex);
            EditorUtility.SetDirty(Network);
            SceneView.RepaintAll();
            return;
        }

        Debug.LogWarning("[RoadTool] Kaynak yapmak icin Road Network ile birlikte sahnede veya Project'te bir yol objesi sec.");
    }

    void CreateYolMaterial()
    {
        const string materialPath = "Assets/RoadTool/Yol_Asphalt_URP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader) { name = "Yol_Asphalt_URP" };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/yol/untitled.fbm/DefaultMaterial_BaseColor.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/yol/untitled.fbm/DefaultMaterial_Normal.png");
        Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/yol/untitled.fbm/DefaultMaterial_Roughness.png");

        if (baseColor != null)
        {
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
        }
        if (normal != null)
            material.SetTexture("_BumpMap", normal);
        if (roughness != null)
            material.SetTexture("_MetallicGlossMap", roughness);

        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.28f);
        material.SetFloat("_Glossiness", 0.28f);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(Network, "Assign Created Road Material");
        Network.ActiveRoad(activeRoadIndex).roadMaterial = material;
        Network.RebuildAllMeshes();
        EditorUtility.SetDirty(Network);
    }

    static float Hash01(int a, int b, int c)
    {
        int hash = HashInts(a, b, c);
        return (hash & 0x7fffffff) / (float)0x7fffffff;
    }

    static int HashInts(int a, int b, int c)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + a;
            hash = hash * 31 + b;
            hash = hash * 31 + c;
            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;
            return hash;
        }
    }
}
