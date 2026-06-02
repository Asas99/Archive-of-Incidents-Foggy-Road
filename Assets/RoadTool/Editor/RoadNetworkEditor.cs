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
    static Vector2 lastPaintUV = new Vector2(-9999f, -9999f);
    static int paintStrokeCounter;
    static bool showRandomize = true;
    static bool showBrushTypeManager;
    static bool showLegacyPaint;

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

        DrawRoadManagementRow();
        DrawSourceObjectPanel();
        DrawActiveRoadPanel();
        DrawPaintBrushPanel();
        DrawTerrainPanel();
        DrawExportPanel();

        EditorGUILayout.HelpBox(
            "Draw Mode yol noktasi ekler. Paint Mode'da hasar tipi grid'inden tip sec, sol tik/surukle boya. [ ve ] fircayi kucult/buyut, - ve = falloff, Shift sil, Ctrl yumusak.",
            MessageType.Info);

        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Raw Inspector");
        if (showAdvanced)
        {
            EditorGUILayout.Space(4);
            DrawDefaultInspector();
        }
    }

    void DrawRoadManagementRow()
    {
        if (Network.roads.Count == 0)
            return;

        RoadNetwork.RoadPath road = Network.ActiveRoad(activeRoadIndex);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Yol Adi", road.name);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(Network, "Rename Road");
                road.name = newName;
                EditorUtility.SetDirty(Network);
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.45f);
            if (GUILayout.Button("Delete Road", GUILayout.Width(96)))
            {
                if (EditorUtility.DisplayDialog("Yolu sil",
                    $"'{road.name}' yolu ve uretilen mesh'leri silinsin mi?\n(Sadece bu yol silinir; diger yollar kalir.)",
                    "Sil", "Vazgec"))
                {
                    Undo.RecordObject(Network, "Delete Road");
                    Network.RemoveRoad(activeRoadIndex);
                    activeRoadIndex = Mathf.Clamp(activeRoadIndex, 0, Mathf.Max(0, Network.roads.Count - 1));
                    selectedPointIndex = -1;
                    EditorUtility.SetDirty(Network);
                    SceneView.RepaintAll();
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            RoadNetwork.RoadType newType = (RoadNetwork.RoadType)EditorGUILayout.EnumPopup("Yol Tipi", road.roadType);
            if (newType != road.roadType)
                ApplyRoadType(road, newType);

            if (road.roadType == RoadNetwork.RoadType.Divided)
            {
                EditorGUI.BeginChangeCheck();
                float median = EditorGUILayout.FloatField("Orta Bant", road.medianWidth);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Network, "Change Median Width");
                    road.medianWidth = Mathf.Clamp(median, 0f, Mathf.Max(0f, road.width - 0.4f));
                    Network.RebuildAllMeshes();
                    EditorUtility.SetDirty(Network);
                    SceneView.RepaintAll();
                }
            }
        }
    }

    // Road-type presets give many quick varieties (Patika .. Otoyol .. Bolunmus).
    // They tune width/shoulder/deformation/uv; only Divided changes geometry (median split).
    void ApplyRoadType(RoadNetwork.RoadPath road, RoadNetwork.RoadType type)
    {
        Undo.RecordObject(Network, "Change Road Type");
        road.roadType = type;
        switch (type)
        {
            case RoadNetwork.RoadType.DirtPath:
                road.width = 2.5f; road.shoulderWidth = 0.6f; road.medianWidth = 0f;
                road.samplesPerMeter = 0.4f; road.uvMetersPerTile = 3f;
                road.enableDeformation = true; road.surfaceHeightNoise = 0.06f; road.edgeWidthNoise = 0.5f; road.flatSectionChance = 0.1f;
                break;
            case RoadNetwork.RoadType.SingleLane:
                road.width = 3.5f; road.shoulderWidth = 1f; road.medianWidth = 0f;
                road.uvMetersPerTile = 4f;
                road.enableDeformation = true; road.surfaceHeightNoise = 0.03f; road.edgeWidthNoise = 0.18f; road.flatSectionChance = 0.35f;
                break;
            case RoadNetwork.RoadType.TwoLane:
                road.width = 7f; road.shoulderWidth = 1.5f; road.medianWidth = 0f;
                road.uvMetersPerTile = 5f;
                road.enableDeformation = true; road.surfaceHeightNoise = 0.03f; road.edgeWidthNoise = 0.15f; road.flatSectionChance = 0.4f;
                break;
            case RoadNetwork.RoadType.RuralDamaged:
                road.width = 5f; road.shoulderWidth = 1f; road.medianWidth = 0f;
                road.uvMetersPerTile = 4f;
                road.enableDeformation = true; road.surfaceHeightNoise = 0.08f; road.edgeWidthNoise = 0.45f; road.flatSectionChance = 0.1f;
                break;
            case RoadNetwork.RoadType.Urban:
                road.width = 9f; road.shoulderWidth = 0.5f; road.medianWidth = 0f;
                road.uvMetersPerTile = 6f;
                road.enableDeformation = false; road.surfaceHeightNoise = 0.01f; road.edgeWidthNoise = 0.05f; road.flatSectionChance = 0.8f;
                break;
            case RoadNetwork.RoadType.Highway:
                road.width = 12f; road.shoulderWidth = 3f; road.medianWidth = 0f;
                road.uvMetersPerTile = 6f;
                road.enableDeformation = false; road.surfaceHeightNoise = 0.015f; road.edgeWidthNoise = 0.08f; road.flatSectionChance = 0.7f;
                break;
            case RoadNetwork.RoadType.Divided:
                road.geometryMode = RoadNetwork.GeometryMode.ProceduralStrip;
                road.width = 16f; road.shoulderWidth = 2.5f; road.medianWidth = 3f;
                road.uvMetersPerTile = 6f;
                road.enableDeformation = false; road.surfaceHeightNoise = 0.015f; road.edgeWidthNoise = 0.08f; road.flatSectionChance = 0.7f;
                break;
            default:
                break;
        }
        Network.RebuildAllMeshes();
        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
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
        EditorGUILayout.LabelField("Paint - Hasar Fircasi", EditorStyles.boldLabel);

        if (!HasAnyDamageTexture())
        {
            EditorGUILayout.HelpBox("Henuz hasar dokusu yok - bu yuzden boyama bos/soluk cikar. Once asagidaki butona bas:", MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Starter Damage Textures", GUILayout.Height(32)))
                    GenerateStarterDamageTextures();
                if (GUILayout.Button("Load Assets/yol Damage", GUILayout.Height(32)))
                    LoadYolDamageTypes();
            }
            EditorGUILayout.Space(4);
        }

        DrawDamageTypeGrid(road);

        EditorGUI.BeginChangeCheck();
        float size = EditorGUILayout.Slider(new GUIContent("Brush Size", "Firca yaricapi (metre). Sahnede [ ve ] ile de degisir."), road.brush.size, 0.1f, 30f);
        float opacity = EditorGUILayout.Slider(new GUIContent("Opacity", "Boya yogunlugu / saydamlik."), road.brush.opacity, 0.01f, 1f);
        float falloff = EditorGUILayout.Slider(new GUIContent("Falloff", "Kenar yumusakligi. 0 = cok yumusak, 1 = sert kenar."), road.brush.falloff, 0f, 1f);
        float flow = EditorGUILayout.Slider(new GUIContent("Flow", "Suruklerken birakma yogunlugu. 1 = her adimda, dusuk = seyrek."), road.brush.flow, 0.05f, 1f);
        float spacing = EditorGUILayout.Slider(new GUIContent("Spacing", "Iki damga arasi minimum mesafe (metre)."), road.brush.spacing, 0.1f, 10f);
        int grid = EditorGUILayout.IntSlider(new GUIContent("Grid Resolution", "Damga mesh cozunurlugu. Yuksek = daha puruzsuz ama agir."), road.brush.gridResolution, 2, 12);
        float yOffset = EditorGUILayout.Slider(new GUIContent("Paint Y Offset", "Yol yuzeyinden yukseklik (z-fighting onler)."), road.brush.yOffset, 0.001f, 0.15f);
        RoadNetwork.PaintBlendMode blendMode = (RoadNetwork.PaintBlendMode)EditorGUILayout.EnumPopup(new GUIContent("Blend Mode", "Alpha = normal, Additive = aydinlatir, Multiply = koyulastirir (kir/yag)."), road.brush.blendMode);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(Network, "Change Paint Brush");
            road.brush.size = Mathf.Max(0.1f, size);
            road.brush.opacity = Mathf.Clamp01(opacity);
            road.brush.falloff = Mathf.Clamp01(falloff);
            road.brush.flow = Mathf.Clamp01(flow);
            road.brush.spacing = Mathf.Max(0.1f, spacing);
            road.brush.gridResolution = Mathf.Clamp(grid, 2, 12);
            road.brush.yOffset = Mathf.Clamp(yOffset, 0.001f, 0.15f);
            road.brush.blendMode = blendMode;
            EditorUtility.SetDirty(Network);
        }

        showRandomize = EditorGUILayout.Foldout(showRandomize, "Randomize (cesitlilik)", true);
        if (showRandomize)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            float randomRotation = EditorGUILayout.Slider(new GUIContent("Random Rotation", "Her damgayi rastgele dondurur (asimetrik dokuda gorunur)."), road.brush.randomRotation, 0f, 1f);
            float randomScale = EditorGUILayout.Slider(new GUIContent("Random Scale", "Boyut cesitliligi."), road.brush.randomScale, 0f, 1f);
            float randomOffset = EditorGUILayout.Slider(new GUIContent("Random Offset", "Konum kaymasi."), road.brush.randomOffset, 0f, 1f);
            float randomOpacity = EditorGUILayout.Slider(new GUIContent("Random Opacity", "Saydamlik cesitliligi."), road.brush.randomOpacity, 0f, 1f);
            float randomTint = EditorGUILayout.Slider(new GUIContent("Random Tint", "Acik/koyu renk cesitliligi."), road.brush.randomTint, 0f, 1f);
            float edgeJitter = EditorGUILayout.Slider(new GUIContent("Edge Jitter", "Kenar duzensizligi."), road.brush.edgeJitter, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(Network, "Change Paint Random");
                road.brush.randomRotation = Mathf.Clamp01(randomRotation);
                road.brush.randomScale = Mathf.Clamp01(randomScale);
                road.brush.randomOffset = Mathf.Clamp01(randomOffset);
                road.brush.randomOpacity = Mathf.Clamp01(randomOpacity);
                road.brush.randomTint = Mathf.Clamp01(randomTint);
                road.brush.edgeJitter = Mathf.Clamp01(edgeJitter);
                EditorUtility.SetDirty(Network);
            }
            EditorGUI.indentLevel--;
        }

        DrawBrushTypeManager();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Paint"))
            {
                Undo.RecordObject(Network, "Clear Road Paint");
                Network.ClearPaint(activeRoadIndex);
                EditorUtility.SetDirty(Network);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Bake Paint Mesh"))
                Network.BakePaintMeshes();
        }

        showLegacyPaint = EditorGUILayout.Foldout(showLegacyPaint, "Advanced (legacy palette)", true);
        if (showLegacyPaint)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            Material brushMaterial = (Material)EditorGUILayout.ObjectField("Brush Material", road.brush.brushMaterial, typeof(Material), false);
            Texture2D brushTexture = (Texture2D)EditorGUILayout.ObjectField("Brush Texture", road.brush.brushTexture, typeof(Texture2D), false);
            bool randomPalette = EditorGUILayout.Toggle("Palette Random", road.brush.randomPalette);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(Network, "Change Paint Brush (legacy)");
                road.brush.brushMaterial = brushMaterial;
                road.brush.brushTexture = brushTexture;
                road.brush.randomPalette = randomPalette;
                EditorUtility.SetDirty(Network);
            }
            DrawPaintPalettePanel(road);
            EditorGUI.indentLevel--;
        }
    }

    void EnsureBrushTypes()
    {
        if (Network.brushTypes == null)
            Network.brushTypes = new List<RoadNetwork.PaintBrushType>();
    }

    bool HasAnyDamageTexture()
    {
        if (Network.brushTypes == null)
            return false;
        foreach (RoadNetwork.PaintBrushType type in Network.brushTypes)
        {
            if (type == null || type.textures == null)
                continue;
            foreach (Texture2D tex in type.textures)
                if (tex != null)
                    return true;
        }
        return false;
    }

    void ApplyBrushTypeDefaults(RoadNetwork.RoadPath road, RoadNetwork.PaintBrushType type)
    {
        road.brush.size = Mathf.Max(0.1f, type.defaultSize);
        road.brush.falloff = Mathf.Clamp01(type.defaultFalloff);
        road.brush.opacity = Mathf.Clamp01(type.defaultOpacity);
        road.brush.blendMode = type.blend;
    }

    // Unity-terrain-brush-style selectable grid of damage types (thumbnails).
    void DrawDamageTypeGrid(RoadNetwork.RoadPath road)
    {
        EnsureBrushTypes();
        if (Network.brushTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("Hasar tipi yok. Asagidaki 'Hasar Tipleri' bolumunden 'Load Assets/yol Damage' veya 'Generate Starter Damage Textures' ile ekle.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Hasar Tipi", EditorStyles.miniBoldLabel);
        const int columns = 4;
        int idx = 0;
        while (idx < Network.brushTypes.Count)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int c = 0; c < columns && idx < Network.brushTypes.Count; c++, idx++)
                {
                    RoadNetwork.PaintBrushType type = Network.brushTypes[idx];
                    Texture2D thumb = (type.textures != null && type.textures.Count > 0) ? type.textures[0] : null;
                    Texture preview = null;
                    if (thumb != null)
                    {
                        preview = AssetPreview.GetAssetPreview(thumb);
                        if (preview == null)
                        {
                            preview = AssetPreview.GetMiniThumbnail(thumb);
                            if (AssetPreview.IsLoadingAssetPreview(thumb.GetInstanceID()))
                                Repaint();
                        }
                    }

                    bool selected = idx == Network.activeBrushTypeIndex;
                    Color prev = GUI.backgroundColor;
                    if (selected)
                        GUI.backgroundColor = new Color(0.55f, 0.72f, 1f);
                    GUIContent content = new GUIContent(preview, type.name);
                    if (GUILayout.Button(content, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        Undo.RecordObject(Network, "Select Damage Type");
                        Network.activeBrushTypeIndex = idx;
                        ApplyBrushTypeDefaults(road, type);
                        EditorUtility.SetDirty(Network);
                    }
                    GUI.backgroundColor = prev;
                }
            }
        }

        int active = Mathf.Clamp(Network.activeBrushTypeIndex, 0, Network.brushTypes.Count - 1);
        EditorGUILayout.LabelField("Secili", Network.brushTypes[active].name, EditorStyles.miniLabel);
    }

    void DrawBrushTypeManager()
    {
        EditorGUILayout.Space(4);
        showBrushTypeManager = EditorGUILayout.Foldout(showBrushTypeManager, "Hasar Tipleri (yonet)", true);
        if (!showBrushTypeManager)
            return;

        EnsureBrushTypes();
        EditorGUI.indentLevel++;
        int removeIndex = -1;
        for (int i = 0; i < Network.brushTypes.Count; i++)
        {
            RoadNetwork.PaintBrushType type = Network.brushTypes[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    string typeName = EditorGUILayout.TextField(type.name);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(Network, "Rename Damage Type");
                        type.name = typeName;
                        EditorUtility.SetDirty(Network);
                    }
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                        removeIndex = i;
                }

                EditorGUI.BeginChangeCheck();
                RoadNetwork.PaintBlendMode blend = (RoadNetwork.PaintBlendMode)EditorGUILayout.EnumPopup("Blend", type.blend);
                Color tint = EditorGUILayout.ColorField("Tint", type.tint);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Network, "Edit Damage Type");
                    type.blend = blend;
                    type.tint = tint;
                    EditorUtility.SetDirty(Network);
                }

                if (type.textures == null)
                    type.textures = new List<Texture2D>();
                int texRemove = -1;
                for (int t = 0; t < type.textures.Count; t++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        Texture2D tex = (Texture2D)EditorGUILayout.ObjectField($"Tex {t + 1}", type.textures[t], typeof(Texture2D), false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(Network, "Set Damage Texture");
                            type.textures[t] = tex;
                            EditorUtility.SetDirty(Network);
                        }
                        if (GUILayout.Button("X", GUILayout.Width(24)))
                            texRemove = t;
                    }
                }
                if (texRemove >= 0)
                {
                    Undo.RecordObject(Network, "Remove Damage Texture");
                    type.textures.RemoveAt(texRemove);
                    EditorUtility.SetDirty(Network);
                }
                if (GUILayout.Button("+ Texture ekle"))
                {
                    Undo.RecordObject(Network, "Add Damage Texture");
                    type.textures.Add(null);
                    EditorUtility.SetDirty(Network);
                }
            }
        }

        if (removeIndex >= 0)
        {
            Undo.RecordObject(Network, "Remove Damage Type");
            Network.brushTypes.RemoveAt(removeIndex);
            Network.activeBrushTypeIndex = Mathf.Clamp(Network.activeBrushTypeIndex, 0, Mathf.Max(0, Network.brushTypes.Count - 1));
            EditorUtility.SetDirty(Network);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Tip ekle"))
            {
                Undo.RecordObject(Network, "Add Damage Type");
                Network.brushTypes.Add(new RoadNetwork.PaintBrushType { name = $"Damage {Network.brushTypes.Count + 1}" });
                EditorUtility.SetDirty(Network);
            }
            if (GUILayout.Button("Load Assets/yol Damage"))
                LoadYolDamageTypes();
        }

        if (GUILayout.Button("Generate Starter Damage Textures"))
            GenerateStarterDamageTextures();

        EditorGUI.indentLevel--;
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

    void DrawExportPanel()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Export (Substance Painter)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Yol mesh'ini disari aktar, Substance Painter'da boya. OBJ paket gerektirmez ve SP direkt acar (UV + normal tasir). FBX icin Unity 'FBX Exporter' paketi gerekir (ilk seferinde sorar, otomatik kurar).", MessageType.None);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Export FBX", GUILayout.Height(30)))
                ExportRoadFbx();
            if (GUILayout.Button("Export OBJ", GUILayout.Height(30)))
                ExportRoadObj();
        }
    }

    List<(Mesh mesh, Transform tf)> GatherRoadMeshes()
    {
        List<(Mesh, Transform)> list = new List<(Mesh, Transform)>();
        if (Network.roads == null)
            return list;
        foreach (RoadNetwork.RoadPath road in Network.roads)
        {
            if (road == null || road.generatedObject == null)
                continue;
            MeshFilter mf = road.generatedObject.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                list.Add((mf.sharedMesh, road.generatedObject.transform));
        }
        return list;
    }

    static string SanitizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "Road";
        return string.Join("_", raw.Split(System.IO.Path.GetInvalidFileNameChars()));
    }

    void RefreshIfInProject(string absolutePath)
    {
        string dataPath = Application.dataPath.Replace('\\', '/');
        string p = absolutePath.Replace('\\', '/');
        if (p.StartsWith(dataPath))
            AssetDatabase.Refresh();
    }

    void ExportRoadObj()
    {
        List<(Mesh mesh, Transform tf)> meshes = GatherRoadMeshes();
        if (meshes.Count == 0)
        {
            Debug.LogWarning("[RoadTool] Disa aktarilacak yol mesh'i yok. Once 'Rebuild All'a bas.");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Export Road OBJ", Application.dataPath, SanitizeName(Network.gameObject.name) + "_Road", "obj");
        if (string.IsNullOrEmpty(path))
            return;

        System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("# RoadTool OBJ export - Substance Painter icin");
        sb.AppendLine("o " + SanitizeName(Network.gameObject.name) + "_Road");

        int vOff = 0, vtOff = 0, vnOff = 0;
        foreach ((Mesh mesh, Transform tf) in meshes)
        {
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            bool hasN = normals != null && normals.Length == verts.Length;
            bool hasUv = uvs != null && uvs.Length == verts.Length;

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 wp = Network.transform.InverseTransformPoint(tf.TransformPoint(verts[i]));
                sb.AppendLine(string.Format(ci, "v {0} {1} {2}", -wp.x, wp.y, wp.z));
            }
            if (hasUv)
                for (int i = 0; i < uvs.Length; i++)
                    sb.AppendLine(string.Format(ci, "vt {0} {1}", uvs[i].x, uvs[i].y));
            if (hasN)
                for (int i = 0; i < normals.Length; i++)
                {
                    Vector3 wn = Network.transform.InverseTransformDirection(tf.TransformDirection(normals[i]));
                    sb.AppendLine(string.Format(ci, "vn {0} {1} {2}", -wn.x, wn.y, wn.z));
                }

            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                int[] tris = mesh.GetTriangles(sm);
                for (int t = 0; t < tris.Length; t += 3)
                    sb.AppendLine(ObjFace(tris[t], tris[t + 1], tris[t + 2], vOff, vtOff, vnOff, hasUv, hasN));
            }

            vOff += verts.Length;
            if (hasUv) vtOff += uvs.Length;
            if (hasN) vnOff += normals.Length;
        }

        System.IO.File.WriteAllText(path, sb.ToString());
        RefreshIfInProject(path);
        Debug.Log("[RoadTool] OBJ kaydedildi: " + path);
        EditorUtility.RevealInFinder(path);
    }

    static string ObjFace(int a, int b, int c, int vOff, int vtOff, int vnOff, bool hasUv, bool hasN)
    {
        return "f " + ObjCorner(a, vOff, vtOff, vnOff, hasUv, hasN)
             + " " + ObjCorner(b, vOff, vtOff, vnOff, hasUv, hasN)
             + " " + ObjCorner(c, vOff, vtOff, vnOff, hasUv, hasN);
    }

    static string ObjCorner(int idx, int vOff, int vtOff, int vnOff, bool hasUv, bool hasN)
    {
        int v = vOff + idx + 1;
        if (hasUv && hasN) return $"{v}/{vtOff + idx + 1}/{vnOff + idx + 1}";
        if (hasUv) return $"{v}/{vtOff + idx + 1}";
        if (hasN) return $"{v}//{vnOff + idx + 1}";
        return v.ToString();
    }

    void ExportRoadFbx()
    {
        List<(Mesh mesh, Transform tf)> meshes = GatherRoadMeshes();
        if (meshes.Count == 0)
        {
            Debug.LogWarning("[RoadTool] Disa aktarilacak yol mesh'i yok. Once 'Rebuild All'a bas.");
            return;
        }

        // Official Unity FBX Exporter, called via reflection so this script compiles even when
        // the package is not installed.
        System.Type exporterType = System.Type.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter, Unity.Formats.Fbx.Editor");
        if (exporterType == null)
        {
            bool install = EditorUtility.DisplayDialog(
                "FBX Exporter gerekli",
                "FBX disa aktarmak icin Unity 'FBX Exporter' paketi gerekli.\n\nSimdi kurulsun mu? (Internet gerekir; birkac saniye surer. Bitince tekrar 'Export FBX'e bas.)\n\nAlternatif: 'Export OBJ' paket istemez ve Substance Painter OBJ'yi acar.",
                "FBX Exporter'i kur", "Iptal");
            if (install)
            {
                UnityEditor.PackageManager.Client.Add("com.unity.formats.fbx");
                Debug.Log("[RoadTool] FBX Exporter paketi kuruluyor... Bitince tekrar 'Export FBX'e bas.");
            }
            return;
        }

        System.Reflection.MethodInfo method = exporterType.GetMethod("ExportObject", new[] { typeof(string), typeof(UnityEngine.Object) });
        if (method == null)
        {
            Debug.LogError("[RoadTool] FBX Exporter API bulunamadi (ExportObject). Lutfen 'Export OBJ' kullan.");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Export Road FBX", Application.dataPath, SanitizeName(Network.gameObject.name) + "_Road", "fbx");
        if (string.IsNullOrEmpty(path))
            return;

        GameObject temp = new GameObject("RoadExport");
        try
        {
            foreach ((Mesh mesh, Transform tf) in meshes)
            {
                GameObject part = new GameObject(tf.gameObject.name);
                part.transform.SetParent(temp.transform, false);
                part.transform.SetPositionAndRotation(tf.position, tf.rotation);
                part.transform.localScale = tf.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer srcRenderer = tf.GetComponent<MeshRenderer>();
                part.AddComponent<MeshRenderer>().sharedMaterials = srcRenderer != null ? srcRenderer.sharedMaterials : new Material[0];
            }

            method.Invoke(null, new object[] { path, temp });
            RefreshIfInProject(path);
            Debug.Log("[RoadTool] FBX kaydedildi: " + path);
            EditorUtility.RevealInFinder(path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temp);
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

        HandlePaintHotkeys(network, road, e);

        if (!TryGetPlacement(out Vector3 position))
            return;

        if (!network.TryProjectWorldToRoad(activeRoadIndex, position, out float distance, out float sideOffset, out RoadNetwork.RoadFrame frame))
            return;

        bool erase = e.shift;
        bool soft = e.control;
        DrawPaintCursor(network, road, frame, sideOffset, erase, soft);

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            lastPaintUV = new Vector2(-9999f, -9999f);
            return;
        }

        if ((e.type != EventType.MouseDown && e.type != EventType.MouseDrag) || e.button != 0 || e.alt)
            return;

        Undo.RecordObject(network, erase ? "Erase Road Paint" : "Paint Road");

        if (erase)
        {
            network.RemovePaintStampsNear(activeRoadIndex, distance, sideOffset, road.brush.size);
            lastPaintUV = new Vector2(distance, sideOffset);
        }
        else
        {
            // Spacing is measured in 2D (along + across the road), so holding still or moving
            // sideways spaces stamps correctly - not just forward distance like before.
            float spacing = Mathf.Max(0.05f, road.brush.spacing) * (soft ? 1.5f : 1f);
            Vector2 here = new Vector2(distance, sideOffset);
            if (e.type == EventType.MouseDrag && Vector2.Distance(here, lastPaintUV) < spacing)
            {
                e.Use();
                return;
            }

            // Flow: probabilistically skip deposits so a low flow paints a sparser stream.
            paintStrokeCounter++;
            if (road.brush.flow < 1f && Hash01(paintStrokeCounter, Mathf.RoundToInt(distance * 7f), 17) > road.brush.flow)
            {
                e.Use();
                return;
            }

            RoadNetwork.PaintStamp stamp = CreatePaintStamp(road, distance, sideOffset, soft);
            network.AddPaintStampFast(activeRoadIndex, stamp);
            lastPaintUV = here;
        }

        EditorUtility.SetDirty(network);
        SceneView.RepaintAll();
        e.Use();
    }

    void HandlePaintHotkeys(RoadNetwork network, RoadNetwork.RoadPath road, Event e)
    {
        if (e.type != EventType.KeyDown)
            return;

        bool handled = true;
        switch (e.keyCode)
        {
            case KeyCode.LeftBracket:
                road.brush.size = Mathf.Max(0.1f, road.brush.size * 0.85f);
                break;
            case KeyCode.RightBracket:
                road.brush.size = Mathf.Min(60f, road.brush.size * 1.15f);
                break;
            case KeyCode.Minus:
            case KeyCode.KeypadMinus:
                road.brush.falloff = Mathf.Clamp01(road.brush.falloff - 0.1f);
                break;
            case KeyCode.Equals:
            case KeyCode.KeypadPlus:
                road.brush.falloff = Mathf.Clamp01(road.brush.falloff + 0.1f);
                break;
            default:
                handled = false;
                break;
        }

        if (handled)
        {
            Undo.RecordObject(network, "Adjust Brush");
            EditorUtility.SetDirty(network);
            Repaint();
            SceneView.RepaintAll();
            e.Use();
        }
    }

    void DrawPaintCursor(RoadNetwork network, RoadNetwork.RoadPath road, RoadNetwork.RoadFrame frame, float sideOffset, bool erase, bool soft)
    {
        Vector3 center = network.transform.TransformPoint(frame.center + frame.right * sideOffset + Vector3.up * road.brush.yOffset);
        float radius = road.brush.size * (soft ? 0.8f : 1f);
        Color main = erase ? new Color(1f, 0.25f, 0.18f, 0.95f) : new Color(0.3f, 0.75f, 1f, 0.95f);

        Handles.color = main;
        Handles.DrawWireDisc(center, Vector3.up, radius);

        // inner ring visualises the solid (pre-falloff) core
        Handles.color = new Color(main.r, main.g, main.b, 0.55f);
        Handles.DrawWireDisc(center, Vector3.up, radius * Mathf.Clamp01(road.brush.falloff));

        Handles.color = new Color(main.r, main.g, main.b, erase ? 0.16f : 0.12f);
        Handles.DrawSolidDisc(center, Vector3.up, radius);

        // forward tick for orientation reference
        Vector3 fwd = network.transform.TransformDirection(frame.tangent);
        Handles.color = main;
        Handles.DrawLine(center, center + fwd * radius * 0.6f);

        string label = erase
            ? "Sil"
            : (Network.brushTypes != null && Network.brushTypes.Count > 0
                ? Network.brushTypes[Mathf.Clamp(Network.activeBrushTypeIndex, 0, Network.brushTypes.Count - 1)].name
                : "Boya");
        Handles.Label(center + Vector3.up * (radius * 0.2f + 0.3f), label);
    }

    RoadNetwork.PaintStamp CreatePaintStamp(RoadNetwork.RoadPath road, float distance, float sideOffset, bool soft)
    {
        int seed = Mathf.Abs(Mathf.RoundToInt(distance * 37f + sideOffset * 101f) + paintStrokeCounter * 9973 + road.paintStamps.Count * 131);

        Texture2D texture = null;
        Material material = null;
        Color typeTint = Color.white;
        RoadNetwork.PaintBlendMode blend = road.brush.blendMode;
        int brushTypeIndex = -1;

        // Active damage type drives the texture (random variant), blend and tint.
        if (Network.brushTypes != null && Network.brushTypes.Count > 0)
        {
            brushTypeIndex = Mathf.Clamp(Network.activeBrushTypeIndex, 0, Network.brushTypes.Count - 1);
            RoadNetwork.PaintBrushType type = Network.brushTypes[brushTypeIndex];
            typeTint = type.tint;
            blend = type.blend;
            List<Texture2D> valid = type.textures != null ? type.textures.FindAll(t => t != null) : null;
            if (valid != null && valid.Count > 0)
                texture = valid[Mathf.Abs(seed) % valid.Count];
        }

        // Legacy fallback when no damage types are defined yet.
        if (texture == null)
        {
            material = road.brush.brushMaterial;
            if (road.brush.randomPalette && road.brush.materialPalette != null && road.brush.materialPalette.Count > 0)
            {
                List<Material> palette = road.brush.materialPalette.FindAll(m => m != null);
                if (palette.Count > 0)
                    material = palette[Mathf.Abs(seed) % palette.Count];
            }
            if (material == null)
                texture = road.brush.brushTexture;
        }

        float randomScale = Mathf.Lerp(1f - road.brush.randomScale, 1f + road.brush.randomScale, Hash01(seed, 17, 3));
        float randomSide = Mathf.Lerp(-road.brush.randomOffset, road.brush.randomOffset, Hash01(seed, 29, 5)) * road.brush.size;
        float randomForward = Mathf.Lerp(-road.brush.randomOffset, road.brush.randomOffset, Hash01(seed, 41, 7)) * road.brush.size;
        float opacityJitter = 1f - road.brush.randomOpacity * Hash01(seed, 67, 13);
        float tintJitter = Mathf.Lerp(1f - road.brush.randomTint, 1f + road.brush.randomTint * 0.4f, Hash01(seed, 71, 19));
        Color tint = new Color(
            Mathf.Clamp01(typeTint.r * tintJitter),
            Mathf.Clamp01(typeTint.g * tintJitter),
            Mathf.Clamp01(typeTint.b * tintJitter),
            typeTint.a);

        return new RoadNetwork.PaintStamp
        {
            distance = distance + randomForward,
            sideOffset = sideOffset + randomSide,
            radius = road.brush.size,
            opacity = Mathf.Clamp01(road.brush.opacity * (soft ? 0.45f : 1f) * opacityJitter),
            rotation = Mathf.Lerp(0f, 360f, Hash01(seed, 53, 11)) * road.brush.randomRotation,
            scale = Mathf.Max(0.05f, randomScale),
            falloff = road.brush.falloff,
            edgeJitter = road.brush.edgeJitter,
            seed = seed,
            material = material,
            texture = texture,
            blendMode = blend,
            tint = tint,
            brushTypeIndex = brushTypeIndex
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

    // Scans Assets/yol (+ Assets/yol/Damage) for textures and sorts them into damage types
    // by filename. The asphalt PBR set (basecolor/normal/rough/metal/height) is skipped.
    void LoadYolDamageTypes()
    {
        string[] folders = AssetDatabase.IsValidFolder("Assets/yol/Damage")
            ? new[] { "Assets/yol", "Assets/yol/Damage" }
            : new[] { "Assets/yol" };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
        if (guids.Length == 0)
        {
            Debug.LogWarning("[RoadTool] Assets/yol (veya Assets/yol/Damage) altinda texture bulunamadi.");
            return;
        }

        Undo.RecordObject(Network, "Load Yol Damage Types");
        EnsureBrushTypes();
        int added = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                continue;

            string lower = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (lower.Contains("basecolor") || lower.Contains("normal") || lower.Contains("rough")
                || lower.Contains("metal") || lower.Contains("height") || lower.Contains("albedo"))
                continue; // road surface PBR maps, not damage decals

            ClassifyDamageTexture(lower, out string typeName, out RoadNetwork.PaintBlendMode blend);
            if (AddTextureToType(typeName, blend, texture))
                added++;
        }

        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
        Debug.Log($"[RoadTool] {added} hasar dokusu yuklendi. Isimsiz/GUID dokulari elle dogru tipe surukleyebilirsin.");
    }

    static void ClassifyDamageTexture(string lower, out string typeName, out RoadNetwork.PaintBlendMode blend)
    {
        if (lower.Contains("crack")) { typeName = "Catlak"; blend = RoadNetwork.PaintBlendMode.Alpha; }
        else if (lower.Contains("pothole") || lower.Contains("hole")) { typeName = "Cukur"; blend = RoadNetwork.PaintBlendMode.Multiply; }
        else if (lower.Contains("patch")) { typeName = "Yama"; blend = RoadNetwork.PaintBlendMode.Alpha; }
        else if (lower.Contains("oil") || lower.Contains("wet") || lower.Contains("stain")) { typeName = "Yag/Leke"; blend = RoadNetwork.PaintBlendMode.Multiply; }
        else if (lower.Contains("wear") || lower.Contains("tire") || lower.Contains("tyre") || lower.Contains("skid")) { typeName = "Asinma"; blend = RoadNetwork.PaintBlendMode.Multiply; }
        else if (lower.Contains("gravel") || lower.Contains("debris") || lower.Contains("dirt")) { typeName = "Cakil/Kir"; blend = RoadNetwork.PaintBlendMode.Alpha; }
        else { typeName = "Diger"; blend = RoadNetwork.PaintBlendMode.Alpha; }
    }

    bool AddTextureToType(string typeName, RoadNetwork.PaintBlendMode blend, Texture2D texture)
    {
        EnsureBrushTypes();
        RoadNetwork.PaintBrushType type = Network.brushTypes.Find(t => t.name == typeName);
        if (type == null)
        {
            type = new RoadNetwork.PaintBrushType { name = typeName, blend = blend };
            Network.brushTypes.Add(type);
        }
        if (type.textures == null)
            type.textures = new List<Texture2D>();
        if (type.textures.Contains(texture))
            return false;

        type.textures.Add(texture);
        return true;
    }

    // Writes a small set of placeholder damage PNGs to Assets/yol/Damage so the tool is usable
    // immediately. Replace them with real authored textures for the best look.
    void GenerateStarterDamageTextures()
    {
        const string folder = "Assets/yol/Damage";
        if (!AssetDatabase.IsValidFolder("Assets/yol"))
            AssetDatabase.CreateFolder("Assets", "yol");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/yol", "Damage");

        Undo.RecordObject(Network, "Generate Starter Damage Textures");
        EnsureBrushTypes();

        WriteDamagePng(folder, "crack_starter", GenerateCrackTexture(256), "Catlak", RoadNetwork.PaintBlendMode.Alpha);
        WriteDamagePng(folder, "pothole_starter", GeneratePotholeTexture(256), "Cukur", RoadNetwork.PaintBlendMode.Multiply);
        WriteDamagePng(folder, "oil_starter", GenerateBlotchTexture(256), "Yag/Leke", RoadNetwork.PaintBlendMode.Multiply);
        WriteDamagePng(folder, "tire_starter", GenerateTireTexture(256), "Asinma", RoadNetwork.PaintBlendMode.Multiply);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(Network);
        SceneView.RepaintAll();
        Debug.Log("[RoadTool] Baslangic hasar dokulari Assets/yol/Damage altina uretildi. Gercek PNG'lerle degistirebilirsin.");
    }

    void WriteDamagePng(string folder, string fileName, Texture2D texture, string typeName, RoadNetwork.PaintBlendMode blend)
    {
        byte[] png = texture.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(texture);
        string path = $"{folder}/{fileName}.png";
        System.IO.File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        Texture2D imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (imported != null)
            AddTextureToType(typeName, blend, imported);
    }

    static Texture2D GenerateCrackTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        for (int i = 0; i < px.Length; i++)
            px[i] = new Color(0.08f, 0.08f, 0.09f, 0f);

        for (int line = 0; line < 3; line++)
        {
            float seed = line * 13.7f + 2.5f;
            float basePos = (line + 1) / 4f;
            bool vertical = (line % 2) == 0;
            for (int t = 0; t < size; t++)
            {
                float n = Mathf.PerlinNoise(t * 0.02f + seed, seed * 1.3f) - 0.5f;
                int pos = Mathf.RoundToInt(basePos * size + n * size * 0.18f);
                int thickness = 1 + Mathf.RoundToInt(Mathf.PerlinNoise(t * 0.05f, seed) * 1.5f);
                for (int w = -thickness; w <= thickness; w++)
                {
                    int x = vertical ? pos + w : t;
                    int y = vertical ? t : pos + w;
                    if (x < 0 || y < 0 || x >= size || y >= size)
                        continue;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(w) / (thickness + 1f));
                    int idx = y * size + x;
                    if (a > px[idx].a)
                        px[idx] = new Color(0.07f, 0.07f, 0.08f, a);
                }
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D GeneratePotholeTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = 0.65f + (Mathf.PerlinNoise(x * 0.05f, y * 0.05f) - 0.5f) * 0.25f;
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(edge * 0.4f, edge, d));
                float shade = Mathf.Lerp(0.12f, 0.4f, Mathf.PerlinNoise(x * 0.08f, y * 0.08f));
                px[y * size + x] = new Color(shade, shade, shade, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D GenerateBlotchTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float n = Mathf.PerlinNoise(x * 0.03f + 5.1f, y * 0.03f + 5.1f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - d) * 1.2f) * Mathf.Lerp(0.5f, 1f, n));
                px[y * size + x] = new Color(0.05f, 0.05f, 0.06f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D GenerateTireTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float band = Mathf.Max(1f - Mathf.Abs(u - 0.36f) / 0.10f, 1f - Mathf.Abs(u - 0.64f) / 0.10f);
                band = Mathf.Clamp01(band);
                float n = Mathf.Lerp(0.6f, 1f, Mathf.PerlinNoise(x * 0.2f, y * 0.06f));
                px[y * size + x] = new Color(0.1f, 0.1f, 0.11f, Mathf.Clamp01(band * n));
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
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
