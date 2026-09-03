#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates a believable forest-road terrain around an existing road mesh.
/// Put this file anywhere under Assets/Editor, then open:
/// Tools > Environment > Forest Road Terrain Generator
/// </summary>
public sealed class ForestRoadTerrainGenerator : EditorWindow
{
    [Header("Scene References")]
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform roadRoot;

    [Header("Automatic PBR Layer Setup")]
    [Tooltip("Folder containing the five Albedo, Normal and MaskMap texture sets.")]
    [SerializeField] private DefaultAsset textureFolder;
    [Tooltip("Automatically finds maps and creates/updates TerrainLayer assets before generation.")]
    [SerializeField] private bool autoBuildLayersOnGenerate = true;
    [Tooltip("Creates and assigns the render-pipeline Terrain Lit material, height blend and per-pixel normals.")]
    [SerializeField] private bool configureTerrainLitMaterial = true;
    [SerializeField, Range(0.01f, 0.50f)] private float heightBlendTransition = 0.14f;
    [SerializeField, Range(0.005f, 0.10f)] private float parallaxStrength = 0.035f;

    [Header("Terrain Layers - Recommended Order")]
    [Tooltip("Wet/dark mud or compact dirt used nearest to the asphalt.")]
    [SerializeField] private TerrainLayer roadsideMud;
    [SerializeField] private TerrainLayer darkForestSoil;
    [SerializeField] private TerrainLayer pineNeedles;
    [SerializeField] private TerrainLayer grassMoss;
    [Tooltip("Wet rock, stony soil or exposed earth for steep faces.")]
    [SerializeField] private TerrainLayer wetRock;

    [Header("Texture Painting")]
    [SerializeField, Range(2f, 20f)] private float roadsideMudWidth = 8f;
    [SerializeField, Range(6f, 80f)] private float smallPatchScale = 19f;
    [SerializeField, Range(20f, 180f)] private float largePatchScale = 63f;
    [SerializeField, Range(5f, 60f)] private float rockStartSlope = 27f;
    [SerializeField, Range(0.5f, 3f)] private float textureContrast = 1.18f;
    [Tooltip("Overall amount of green moss in the forest floor. Higher values prevent a fully brown terrain.")]
    [SerializeField, Range(0.25f, 2.5f)] private float forestGreenness = 1.55f;
    [Tooltip("Adds a moist green transition after the bare roadside shoulder.")]
    [SerializeField, Range(0f, 2f)] private float greenRoadEdgeBoost = 1.15f;
    [Tooltip("Applies sensible world-space tile sizes to the assigned TerrainLayer assets.")]
    [SerializeField] private bool autoConfigureTiling = true;

    [Header("Dense Forest Vegetation")]
    [Tooltip("Automatically scatters efficient Terrain tree instances after height and texture generation.")]
    [SerializeField] private bool generateVegetation = true;
    [Tooltip("Large conifers and mature forest canopy trees. Add several prefab variations.")]
    [SerializeField] private GameObject[] canopyTreePrefabs = new GameObject[0];
    [Tooltip("Alternative mature trees for silhouette variation.")]
    [SerializeField] private GameObject[] secondaryTreePrefabs = new GameObject[0];
    [Tooltip("Young trees placed beneath gaps in the canopy.")]
    [SerializeField] private GameObject[] saplingPrefabs = new GameObject[0];
    [Tooltip("Medium bushes and dense roadside shrubs.")]
    [SerializeField] private GameObject[] shrubPrefabs = new GameObject[0];
    [Tooltip("Fern, bracken, tall grass and other low forest-floor prefabs.")]
    [SerializeField] private GameObject[] fernAndGrassPrefabs = new GameObject[0];
    [SerializeField, Range(50, 1200)] private int canopyTreesPerHectare = 520;
    [SerializeField, Range(0, 1000)] private int secondaryTreesPerHectare = 180;
    [SerializeField, Range(0, 1600)] private int saplingsPerHectare = 480;
    [SerializeField, Range(0, 5000)] private int shrubsPerHectare = 1750;
    [SerializeField, Range(0, 7000)] private int fernsAndGrassPerHectare = 2600;
    [SerializeField, Range(500, 30000)] private int maximumVegetationInstances = 16000;
    [SerializeField, Range(1f, 15f)] private float treeRoadClearance = 4.5f;
    [SerializeField, Range(0.25f, 8f)] private float undergrowthRoadClearance = 1.15f;
    [SerializeField, Range(10f, 60f)] private float maximumTreeSlope = 38f;
    [SerializeField, Range(10f, 70f)] private float maximumUndergrowthSlope = 50f;
    [SerializeField, Range(8f, 100f)] private float vegetationClusterScale = 34f;
    [Tooltip("Clears existing Terrain tree instances before creating the forest. Disable only if you intentionally want to append.")]
    [SerializeField] private bool replaceExistingTerrainVegetation = true;

    [Header("Road Fit")]
    [Tooltip("Terrain remains nearly flat this many metres beyond the asphalt edge.")]
    [SerializeField, Min(0f)] private float shoulderWidth = 4.5f;
    [Tooltip("Randomly widens and narrows the shoulder so it does not trace the road uniformly.")]
    [SerializeField, Range(0f, 8f)] private float shoulderIrregularity = 2.5f;
    [Tooltip("Width of the smooth transition from road level to forest terrain.")]
    [SerializeField, Min(1f)] private float roadBlendWidth = 14f;
    [Tooltip("Makes some banks short and steep while others remain broad and gentle.")]
    [SerializeField, Range(0f, 1f)] private float slopeRandomness = 0.72f;
    [Tooltip("Places terrain slightly below the road to prevent z-fighting.")]
    [SerializeField, Range(0.01f, 0.30f)] private float roadClearance = 0.07f;
    [Tooltip("Maximum gap between points sampled from the road mesh.")]
    [SerializeField, Range(0.5f, 4f)] private float roadSampleSpacing = 1.5f;

    [Header("Terrain Shape")]
    [SerializeField, Range(0f, 20f)] private float bankRise = 1.8f;
    [Tooltip("Adds independent high and low bank sections on both sides of the road.")]
    [SerializeField, Range(0f, 15f)] private float bankHeightVariation = 4.8f;
    [SerializeField, Range(0f, 20f)] private float broadHillAmplitude = 6.5f;
    [SerializeField, Range(20f, 300f)] private float broadHillScale = 88f;
    [Tooltip("Occasional local mounds and raised forest banks.")]
    [SerializeField, Range(0f, 15f)] private float moundAmplitude = 4.5f;
    [SerializeField, Range(12f, 120f)] private float moundScale = 42f;
    [Tooltip("Bends the noise coordinates to avoid obvious Perlin blobs and parallel bands.")]
    [SerializeField, Range(0f, 80f)] private float domainWarp = 34f;
    [SerializeField, Range(0f, 5f)] private float groundVariation = 0.9f;
    [SerializeField, Range(3f, 40f)] private float groundVariationScale = 14f;
    [SerializeField, Range(0, 4)] private int smoothingPasses = 1;
    [SerializeField] private int seed = 1847;

    [Header("Safety")]
    [Tooltip("Creates a new TerrainData asset and leaves the original untouched.")]
    [SerializeField] private bool duplicateTerrainData = true;

    private Vector2 scroll;

    [MenuItem("Tools/Environment/Forest Road Terrain Generator")]
    private static void OpenWindow()
    {
        ForestRoadTerrainGenerator window = GetWindow<ForestRoadTerrainGenerator>();
        window.titleContent = new GUIContent("Forest Road Terrain");
        window.minSize = new Vector2(430f, 620f);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Select the Terrain and the parent object containing the road MeshFilter(s). " +
            "The tool samples the real road surface, keeps it clear, raises natural banks, " +
            "creates PBR Terrain Layers from a texture folder and paints organic forest transitions.",
            MessageType.Info);

        SerializedObject serializedWindow = new SerializedObject(this);
        serializedWindow.Update();

        DrawProperty(serializedWindow, "targetTerrain");
        DrawProperty(serializedWindow, "roadRoot");
        EditorGUILayout.Space(8f);
        DrawSection("AUTOMATIC PBR SETUP");
        DrawProperty(serializedWindow, "textureFolder");
        DrawProperty(serializedWindow, "autoBuildLayersOnGenerate");
        DrawProperty(serializedWindow, "configureTerrainLitMaterial");
        DrawProperty(serializedWindow, "heightBlendTransition");
        DrawProperty(serializedWindow, "parallaxStrength");
        if (GUILayout.Button("AUTO FIND MAPS + CREATE PBR LAYERS", GUILayout.Height(34f)))
            BuildTerrainLayersFromFolder(true);
        EditorGUILayout.HelpBox(
            "Expected names: 01_Roadside_Wet_Mud, 02_Dark_Forest_Soil, 03_Pine_Needles, " +
            "04_Wet_Moss and 05_Wet_Rocky_Soil. Normal and MaskMap suffixes are detected automatically.",
            MessageType.None);
        EditorGUILayout.HelpBox(
            "Terrain Lit uses the Mask Map height channel for height-aware blending and enables per-pixel normals. " +
            "True parallax is enabled only when the active render-pipeline terrain shader exposes a parallax property; " +
            "standard URP Terrain Lit does not provide true POM.",
            MessageType.Info);
        EditorGUILayout.Space(8f);
        DrawSection("TERRAIN LAYERS");
        DrawProperty(serializedWindow, "roadsideMud");
        DrawProperty(serializedWindow, "darkForestSoil");
        DrawProperty(serializedWindow, "pineNeedles");
        DrawProperty(serializedWindow, "grassMoss");
        DrawProperty(serializedWindow, "wetRock");
        EditorGUILayout.Space(8f);
        DrawSection("TEXTURE PAINTING");
        DrawProperty(serializedWindow, "roadsideMudWidth");
        DrawProperty(serializedWindow, "smallPatchScale");
        DrawProperty(serializedWindow, "largePatchScale");
        DrawProperty(serializedWindow, "rockStartSlope");
        DrawProperty(serializedWindow, "textureContrast");
        DrawProperty(serializedWindow, "forestGreenness");
        DrawProperty(serializedWindow, "greenRoadEdgeBoost");
        DrawProperty(serializedWindow, "autoConfigureTiling");
        EditorGUILayout.Space(8f);
        DrawSection("DENSE FOREST VEGETATION");
        DrawProperty(serializedWindow, "generateVegetation");
        DrawProperty(serializedWindow, "canopyTreePrefabs", true);
        DrawProperty(serializedWindow, "secondaryTreePrefabs", true);
        DrawProperty(serializedWindow, "saplingPrefabs", true);
        DrawProperty(serializedWindow, "shrubPrefabs", true);
        DrawProperty(serializedWindow, "fernAndGrassPrefabs", true);
        DrawProperty(serializedWindow, "canopyTreesPerHectare");
        DrawProperty(serializedWindow, "secondaryTreesPerHectare");
        DrawProperty(serializedWindow, "saplingsPerHectare");
        DrawProperty(serializedWindow, "shrubsPerHectare");
        DrawProperty(serializedWindow, "fernsAndGrassPerHectare");
        DrawProperty(serializedWindow, "maximumVegetationInstances");
        DrawProperty(serializedWindow, "treeRoadClearance");
        DrawProperty(serializedWindow, "undergrowthRoadClearance");
        DrawProperty(serializedWindow, "maximumTreeSlope");
        DrawProperty(serializedWindow, "maximumUndergrowthSlope");
        DrawProperty(serializedWindow, "vegetationClusterScale");
        DrawProperty(serializedWindow, "replaceExistingTerrainVegetation");
        EditorGUILayout.Space(8f);
        DrawSection("ROAD FIT");
        DrawProperty(serializedWindow, "shoulderWidth");
        DrawProperty(serializedWindow, "shoulderIrregularity");
        DrawProperty(serializedWindow, "roadBlendWidth");
        DrawProperty(serializedWindow, "slopeRandomness");
        DrawProperty(serializedWindow, "roadClearance");
        DrawProperty(serializedWindow, "roadSampleSpacing");
        EditorGUILayout.Space(8f);
        DrawSection("TERRAIN SHAPE");
        DrawProperty(serializedWindow, "bankRise");
        DrawProperty(serializedWindow, "bankHeightVariation");
        DrawProperty(serializedWindow, "broadHillAmplitude");
        DrawProperty(serializedWindow, "broadHillScale");
        DrawProperty(serializedWindow, "moundAmplitude");
        DrawProperty(serializedWindow, "moundScale");
        DrawProperty(serializedWindow, "domainWarp");
        DrawProperty(serializedWindow, "groundVariation");
        DrawProperty(serializedWindow, "groundVariationScale");
        DrawProperty(serializedWindow, "smoothingPasses");
        DrawProperty(serializedWindow, "seed");
        EditorGUILayout.Space(8f);
        DrawSection("SAFETY");
        DrawProperty(serializedWindow, "duplicateTerrainData");

        serializedWindow.ApplyModifiedProperties();

        EditorGUILayout.Space(14f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("REFERENCE FOREST PRESET", GUILayout.Height(28f)))
            ApplyReferencePreset();
        if (GUILayout.Button("NEW RANDOM SEED", GUILayout.Height(28f)))
        {
            seed = UnityEngine.Random.Range(-100000, 100001);
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);
        using (new EditorGUI.DisabledScope(targetTerrain == null || roadRoot == null))
        {
            GUI.backgroundColor = new Color(0.45f, 0.85f, 0.52f);
            if (GUILayout.Button("GENERATE HEIGHT + TEXTURES (FULL)", GUILayout.Height(42f)))
                Generate();
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(0.45f, 0.72f, 0.95f);
            if (GUILayout.Button("PAINT TEXTURES ONLY", GUILayout.Height(34f)))
                PaintTexturesOnly();
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(0.36f, 0.72f, 0.36f);
            if (GUILayout.Button("GENERATE VEGETATION ONLY", GUILayout.Height(34f)))
                GenerateVegetationOnly();
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Reference preset leaves a broad shoulder first, then creates independent steep, low and high " +
            "forest banks. NEW RANDOM SEED changes their locations without changing the controls.",
            MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawSection(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private static void DrawProperty(SerializedObject so, string name, bool includeChildren = false)
    {
        EditorGUILayout.PropertyField(so.FindProperty(name), includeChildren);
    }

    private void ApplyReferencePreset()
    {
        shoulderWidth = 4.5f;
        shoulderIrregularity = 2.5f;
        roadBlendWidth = 14f;
        slopeRandomness = 0.72f;
        roadClearance = 0.07f;
        bankRise = 1.8f;
        bankHeightVariation = 4.8f;
        broadHillAmplitude = 6.5f;
        broadHillScale = 88f;
        moundAmplitude = 4.5f;
        moundScale = 42f;
        domainWarp = 34f;
        groundVariation = 0.9f;
        groundVariationScale = 14f;
        roadsideMudWidth = 8f;
        smallPatchScale = 19f;
        largePatchScale = 63f;
        rockStartSlope = 27f;
        textureContrast = 1.18f;
        forestGreenness = 1.55f;
        greenRoadEdgeBoost = 1.15f;
        canopyTreesPerHectare = 520;
        secondaryTreesPerHectare = 180;
        saplingsPerHectare = 480;
        shrubsPerHectare = 1750;
        fernsAndGrassPerHectare = 2600;
        maximumVegetationInstances = 16000;
        treeRoadClearance = 4.5f;
        undergrowthRoadClearance = 1.15f;
        maximumTreeSlope = 38f;
        maximumUndergrowthSlope = 50f;
        vegetationClusterScale = 34f;
        smoothingPasses = 1;
        Repaint();
    }

    private bool BuildTerrainLayersFromFolder(bool showSuccessDialog)
    {
        string folderPath = ResolveTextureFolderPath();
        if (string.IsNullOrEmpty(folderPath))
        {
            EditorUtility.DisplayDialog(
                "Texture folder missing",
                "Drag the extracted Forest_Road_Terrain_Complete_PBR_4K folder into Texture Folder, " +
                "or select that folder in the Project window and press the button again.",
                "OK");
            return false;
        }

        LayerDefinition[] definitions = GetLayerDefinitions();
        TerrainLayer[] createdLayers = new TerrainLayer[definitions.Length];
        string generatedFolder = EnsureGeneratedLayerFolder(folderPath);
        List<string> missing = new List<string>();

        try
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                LayerDefinition definition = definitions[i];
                EditorUtility.DisplayProgressBar(
                    "Automatic PBR Terrain Layers",
                    "Finding and importing " + definition.displayName + "...",
                    i / (float)definitions.Length);

                string albedoPath = FindTexturePath(folderPath, definition.fileKey, TextureRole.Albedo);
                string normalPath = FindTexturePath(folderPath, definition.fileKey, TextureRole.Normal);
                string maskPath = FindTexturePath(folderPath, definition.fileKey, TextureRole.Mask);

                if (string.IsNullOrEmpty(albedoPath) || string.IsNullOrEmpty(normalPath) || string.IsNullOrEmpty(maskPath))
                {
                    missing.Add(definition.displayName +
                                " (Albedo: " + YesNo(albedoPath) +
                                ", Normal: " + YesNo(normalPath) +
                                ", Mask: " + YesNo(maskPath) + ")");
                    continue;
                }

                ConfigureTextureImporter(albedoPath, TextureRole.Albedo);
                ConfigureTextureImporter(normalPath, TextureRole.Normal);
                ConfigureTextureImporter(maskPath, TextureRole.Mask);

                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
                string layerPath = generatedFolder + "/" + definition.assetName + ".terrainlayer";
                TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);

                if (layer == null)
                {
            layer = new TerrainLayer();
                    layer.name = definition.assetName;
                    AssetDatabase.CreateAsset(layer, layerPath);
                }
                else
                {
                    Undo.RecordObject(layer, "Update PBR Terrain Layer");
                }

                layer.diffuseTexture = albedo;
                layer.normalMapTexture = normal;
                layer.maskMapTexture = mask;
                layer.tileSize = definition.tileSize;
                layer.normalScale = definition.normalScale;
                layer.metallic = 0f;
                layer.smoothness = definition.fallbackSmoothness;
                EditorUtility.SetDirty(layer);
                createdLayers[i] = layer;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Some maps were not found",
                "Keep the original filenames from the ZIP. Missing sets:\n\n" + string.Join("\n", missing),
                "OK");
            return false;
        }

        roadsideMud = createdLayers[0];
        darkForestSoil = createdLayers[1];
        pineNeedles = createdLayers[2];
        grassMoss = createdLayers[3];
        wetRock = createdLayers[4];

        if (configureTerrainLitMaterial && targetTerrain != null)
            ConfigureProfessionalTerrainMaterial(generatedFolder);

        Repaint();
        if (showSuccessDialog)
        {
            EditorUtility.DisplayDialog(
                "PBR Terrain Layers ready",
                "Five TerrainLayer assets were created and assigned with Albedo, Normal and Mask maps. " +
                "Import settings, tiling, normal strength and Terrain Lit settings were configured automatically.",
                "OK");
        }

        return true;
    }

    private string ResolveTextureFolderPath()
    {
        string path = textureFolder != null ? AssetDatabase.GetAssetPath(textureFolder) : string.Empty;
        if (AssetDatabase.IsValidFolder(path))
            return path;

        string selectedPath = Selection.activeObject != null
            ? AssetDatabase.GetAssetPath(Selection.activeObject)
            : string.Empty;
        if (!AssetDatabase.IsValidFolder(selectedPath))
            return string.Empty;

        textureFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(selectedPath);
        return selectedPath;
    }

    private static string EnsureGeneratedLayerFolder(string parentFolder)
    {
        string folder = parentFolder + "/GeneratedTerrainLayers";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(parentFolder, "GeneratedTerrainLayers");
        return folder;
    }

    private static string FindTexturePath(string folderPath, string fileKey, TextureRole role)
    {
        string normalizedKey = NormalizeFileToken(fileKey);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        string bestMatch = string.Empty;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string token = NormalizeFileToken(Path.GetFileNameWithoutExtension(path));
            if (!token.Contains(normalizedKey))
                continue;

            bool isNormal = token.Contains("normal");
            bool isMask = token.Contains("maskmap") || token.Contains("mask");
            bool matches = role == TextureRole.Albedo ? !isNormal && !isMask :
                           role == TextureRole.Normal ? isNormal : isMask;
            if (!matches)
                continue;

            if (string.IsNullOrEmpty(bestMatch) || path.Length < bestMatch.Length)
                bestMatch = path;
        }

        return bestMatch;
    }

    private static string NormalizeFileToken(string value)
    {
        return value.ToLowerInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    private static void ConfigureTextureImporter(string assetPath, TextureRole role)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 8;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.alphaIsTransparency = false;

        if (role == TextureRole.Normal)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.convertToNormalmap = false;
        }
        else
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = role == TextureRole.Albedo;
            if (role == TextureRole.Mask)
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
        }

        importer.SaveAndReimport();
    }

    private void ConfigureProfessionalTerrainMaterial(string generatedFolder)
    {
        if (targetTerrain == null)
            return;

        Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (terrainShader == null)
            terrainShader = Shader.Find("HDRP/TerrainLit");
        if (terrainShader == null)
            terrainShader = Shader.Find("Nature/Terrain/Standard");
        if (terrainShader == null)
        {
            Debug.LogWarning("No supported Terrain Lit shader was found. Layers are valid, but material setup was skipped.");
            return;
        }

        string materialPath = generatedFolder + "/ForestRoadTerrainLit.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null || material.shader != terrainShader)
        {
            if (material != null)
                AssetDatabase.DeleteAsset(materialPath);
            material = new Material(terrainShader) { name = "ForestRoadTerrainLit" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            Undo.RecordObject(material, "Configure Forest Terrain Material");
        }

        SetFloatIfPresent(material, "_EnableHeightBlend", 1f);
        SetFloatIfPresent(material, "_HeightTransition", heightBlendTransition);
        SetFloatIfPresent(material, "_EnableInstancedPerPixelNormal", 1f);
        bool supportsParallax = material.HasProperty("_Parallax") || material.HasProperty("_ParallaxStrength");
        SetFloatIfPresent(material, "_Parallax", parallaxStrength);
        SetFloatIfPresent(material, "_ParallaxStrength", parallaxStrength);
        material.EnableKeyword("_TERRAIN_BLEND_HEIGHT");
        material.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);

        Undo.RecordObject(targetTerrain, "Assign Professional Terrain Material");
        targetTerrain.materialTemplate = material;
        targetTerrain.drawInstanced = true;
        EditorUtility.SetDirty(targetTerrain);
        AssetDatabase.SaveAssets();

        if (!supportsParallax)
        {
            Debug.LogWarning(
                "The selected Terrain Lit shader has no true parallax/POM property. " +
                "Height-based layer blending, mask height and per-pixel normal detail are enabled instead.");
        }
    }

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private static string YesNo(string value)
    {
        return string.IsNullOrEmpty(value) ? "missing" : "found";
    }

    private static LayerDefinition[] GetLayerDefinitions()
    {
        return new[]
        {
            new LayerDefinition("Roadside Mud", "01_Roadside_Wet_Mud", "01_Roadside_Wet_Mud", new Vector2(4f, 4f), 0.35f, 0.62f),
            new LayerDefinition("Dark Forest Soil", "02_Dark_Forest_Soil", "02_Dark_Forest_Soil", new Vector2(5.5f, 5.5f), 0.45f, 0.20f),
            new LayerDefinition("Pine Needles", "03_Pine_Needles", "03_Pine_Needles", new Vector2(3.2f, 3.2f), 0.75f, 0.16f),
            new LayerDefinition("Wet Moss", "04_Wet_Moss", "04_Wet_Moss", new Vector2(4.5f, 4.5f), 0.55f, 0.34f),
            new LayerDefinition("Wet Rocky Soil", "05_Wet_Rocky_Soil", "05_Wet_Rocky_Soil", new Vector2(7f, 7f), 0.85f, 0.48f)
        };
    }

    private void Generate()
    {
        if (!ValidateInputs(out MeshFilter[] roadMeshes))
            return;

        if (autoBuildLayersOnGenerate && !BuildTerrainLayersFromFolder(false))
            return;

        if (!AllPaintLayersAssigned())
        {
            EditorUtility.DisplayDialog(
                "Terrain Layers missing",
                "For FULL generation, assign Roadside Mud, Dark Forest Soil, Pine Needles and Grass/Moss. " +
                "Wet Rock is optional.",
                "OK");
            return;
        }

        try
        {
            TerrainData data = PrepareTerrainData();
            int resolution = data.heightmapResolution;
            Vector3 terrainPosition = targetTerrain.transform.position;
            Vector3 terrainSize = data.size;

            EditorUtility.DisplayProgressBar("Forest Road Terrain", "Sampling road mesh...", 0.05f);
            List<RoadSample> samples = SampleRoadMeshes(roadMeshes, terrainPosition, terrainSize, resolution);
            if (samples.Count == 0)
                throw new InvalidOperationException("No road vertices overlap the selected Terrain.");

            EditorUtility.DisplayProgressBar("Forest Road Terrain", "Building road distance field...", 0.18f);
            RoadField field = BuildRoadField(samples, resolution, terrainSize);

            EditorUtility.DisplayProgressBar("Forest Road Terrain", "Generating natural height field...", 0.40f);
            float[,] heights = GenerateHeights(data, terrainPosition, field);

            for (int pass = 0; pass < smoothingPasses; pass++)
            {
                EditorUtility.DisplayProgressBar(
                    "Forest Road Terrain",
                    "Softening terrain pass " + (pass + 1) + " / " + smoothingPasses + "...",
                    0.62f + pass * 0.05f);
                heights = SmoothAwayFromRoad(heights, field.distance, terrainSize, shoulderWidth + 0.5f);
            }

            Undo.RegisterCompleteObjectUndo(data, "Generate Forest Road Terrain");
            data.SetHeights(0, 0, heights);

            EditorUtility.DisplayProgressBar("Forest Road Terrain", "Painting realistic forest layers...", 0.82f);
            ConfigureLayerTiling();
            PaintTerrainLayers(data, terrainPosition, field);

            int vegetationCount = 0;
            if (generateVegetation && HasAnyVegetationPrefab())
            {
                EditorUtility.DisplayProgressBar("Forest Road Terrain", "Growing dense forest vegetation...", 0.95f);
                vegetationCount = GenerateVegetation(data, terrainPosition, field);
            }

            data.SyncHeightmap();
            targetTerrain.Flush();
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(targetTerrain);
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();

            Debug.Log("Forest Road Terrain generated. Road samples: " + samples.Count +
                      ", vegetation instances: " + vegetationCount);
            EditorUtility.DisplayDialog(
                "Forest Road Terrain",
                "Done. Natural terrain heights, greener blended textures and dense vegetation were generated together.\n" +
                "Vegetation instances added: " + vegetationCount + "\n\n" +
                (duplicateTerrainData ? "The original TerrainData was preserved." : "The current TerrainData was edited."),
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Generation failed", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void PaintTexturesOnly()
    {
        if (!ValidateInputs(out MeshFilter[] roadMeshes))
            return;

        if (autoBuildLayersOnGenerate && !BuildTerrainLayersFromFolder(false))
            return;

        if (!AllPaintLayersAssigned())
        {
            EditorUtility.DisplayDialog(
                "Four layers required",
                "Assign Roadside Mud, Dark Forest Soil, Pine Needles and Grass/Moss. Wet Rock is optional.",
                "OK");
            return;
        }

        try
        {
            TerrainData data = targetTerrain.terrainData;
            int resolution = data.heightmapResolution;
            Vector3 terrainPosition = targetTerrain.transform.position;

            EditorUtility.DisplayProgressBar("Forest Ground Painter", "Sampling road mesh...", 0.08f);
            List<RoadSample> samples = SampleRoadMeshes(roadMeshes, terrainPosition, data.size, resolution);
            if (samples.Count == 0)
                throw new InvalidOperationException("No road vertices overlap the selected Terrain.");

            EditorUtility.DisplayProgressBar("Forest Ground Painter", "Building road influence field...", 0.22f);
            RoadField field = BuildRoadField(samples, resolution, data.size);

            Undo.RegisterCompleteObjectUndo(data, "Paint Forest Ground Textures");
            ConfigureLayerTiling();
            PaintTerrainLayers(data, terrainPosition, field);

            targetTerrain.Flush();
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(targetTerrain);
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();

            EditorUtility.DisplayDialog(
                "Forest Ground Painter",
                "Done. Only the terrain textures were repainted; terrain heights were not changed.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Texture painting failed", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void GenerateVegetationOnly()
    {
        if (!ValidateInputs(out MeshFilter[] roadMeshes))
            return;

        if (!HasAnyVegetationPrefab())
        {
            EditorUtility.DisplayDialog(
                "Vegetation prefabs missing",
                "Add at least one prefab to Canopy Trees, Secondary Trees, Saplings, Shrubs or Fern And Grass.",
                "OK");
            return;
        }

        try
        {
            TerrainData data = targetTerrain.terrainData;
            Vector3 terrainPosition = targetTerrain.transform.position;
            int resolution = data.heightmapResolution;

            EditorUtility.DisplayProgressBar("Dense Forest", "Sampling road mesh...", 0.10f);
            List<RoadSample> samples = SampleRoadMeshes(roadMeshes, terrainPosition, data.size, resolution);
            if (samples.Count == 0)
                throw new InvalidOperationException("No road vertices overlap the selected Terrain.");

            EditorUtility.DisplayProgressBar("Dense Forest", "Building road clearance field...", 0.28f);
            RoadField field = BuildRoadField(samples, resolution, data.size);
            EditorUtility.DisplayProgressBar("Dense Forest", "Scattering clustered forest vegetation...", 0.52f);
            int added = GenerateVegetation(data, terrainPosition, field);

            targetTerrain.Flush();
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(targetTerrain);
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();

            EditorUtility.DisplayDialog(
                "Dense Forest",
                "Done. " + added + " clustered Terrain vegetation instances were added.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Vegetation generation failed", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private bool HasAnyVegetationPrefab()
    {
        return CountValidPrefabs(canopyTreePrefabs) +
               CountValidPrefabs(secondaryTreePrefabs) +
               CountValidPrefabs(saplingPrefabs) +
               CountValidPrefabs(shrubPrefabs) +
               CountValidPrefabs(fernAndGrassPrefabs) > 0;
    }

    private static int CountValidPrefabs(GameObject[] prefabs)
    {
        if (prefabs == null)
            return 0;

        int count = 0;
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null && AssetDatabase.Contains(prefabs[i]))
                count++;
        }
        return count;
    }

    private int GenerateVegetation(TerrainData data, Vector3 terrainPosition, RoadField field)
    {
        Undo.RegisterCompleteObjectUndo(data, "Generate Dense Forest Vegetation");

        List<TreePrototype> prototypes = new List<TreePrototype>();
        List<TreeInstance> instances = new List<TreeInstance>();
        if (!replaceExistingTerrainVegetation)
        {
            prototypes.AddRange(data.treePrototypes);
            instances.AddRange(data.treeInstances);
        }

        PrototypeRange canopyRange = AddTreePrototypes(prototypes, canopyTreePrefabs, 0.16f);
        PrototypeRange secondaryRange = AddTreePrototypes(prototypes, secondaryTreePrefabs, 0.18f);
        PrototypeRange saplingRange = AddTreePrototypes(prototypes, saplingPrefabs, 0.22f);
        PrototypeRange shrubRange = AddTreePrototypes(prototypes, shrubPrefabs, 0.30f);
        PrototypeRange fernRange = AddTreePrototypes(prototypes, fernAndGrassPrefabs, 0.38f);

        if (canopyRange.count + secondaryRange.count + saplingRange.count + shrubRange.count + fernRange.count == 0)
            throw new InvalidOperationException("Vegetation fields contain no valid prefab assets. Drag prefabs from the Project window, not scene objects.");

        data.treePrototypes = prototypes.ToArray();
        data.RefreshPrototypes();

        float hectares = Mathf.Max(0.02f, data.size.x * data.size.z / 10000f);
        int[] targets =
        {
            canopyRange.count > 0 ? Mathf.RoundToInt(canopyTreesPerHectare * hectares) : 0,
            secondaryRange.count > 0 ? Mathf.RoundToInt(secondaryTreesPerHectare * hectares) : 0,
            saplingRange.count > 0 ? Mathf.RoundToInt(saplingsPerHectare * hectares) : 0,
            shrubRange.count > 0 ? Mathf.RoundToInt(shrubsPerHectare * hectares) : 0,
            fernRange.count > 0 ? Mathf.RoundToInt(fernsAndGrassPerHectare * hectares) : 0
        };

        int requested = 0;
        for (int i = 0; i < targets.Length; i++)
            requested += targets[i];
        if (requested > maximumVegetationInstances)
        {
            float reduction = maximumVegetationInstances / (float)requested;
            for (int i = 0; i < targets.Length; i++)
                targets[i] = Mathf.RoundToInt(targets[i] * reduction);
        }

        System.Random random = new System.Random(seed ^ 0x5F3759DF);
        int before = instances.Count;
        ScatterTreeCategory(data, terrainPosition, field, instances, canopyRange, targets[0],
            treeRoadClearance, maximumTreeSlope, 0.82f, 1.28f, 0.90f, 1.36f, 11, false, random);
        ScatterTreeCategory(data, terrainPosition, field, instances, secondaryRange, targets[1],
            treeRoadClearance + 0.8f, maximumTreeSlope, 0.72f, 1.18f, 0.78f, 1.25f, 23, false, random);
        ScatterTreeCategory(data, terrainPosition, field, instances, saplingRange, targets[2],
            treeRoadClearance * 0.72f, maximumTreeSlope + 5f, 0.42f, 0.82f, 0.48f, 0.96f, 37, true, random);
        ScatterTreeCategory(data, terrainPosition, field, instances, shrubRange, targets[3],
            undergrowthRoadClearance, maximumUndergrowthSlope, 0.58f, 1.38f, 0.52f, 1.22f, 53, true, random);
        ScatterTreeCategory(data, terrainPosition, field, instances, fernRange, targets[4],
            undergrowthRoadClearance * 0.65f, maximumUndergrowthSlope, 0.52f, 1.55f, 0.46f, 1.18f, 71, true, random);

        data.SetTreeInstances(instances.ToArray(), true);
        return instances.Count - before;
    }

    private static PrototypeRange AddTreePrototypes(List<TreePrototype> prototypes, GameObject[] prefabs, float bendFactor)
    {
        int start = prototypes.Count;
        if (prefabs != null)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab == null)
                    continue;
                if (!AssetDatabase.Contains(prefab))
                {
                    Debug.LogWarning("Skipped scene object '" + prefab.name + "'. Terrain vegetation requires a prefab from the Project window.");
                    continue;
                }

                TreePrototype prototype = new TreePrototype
                {
                    prefab = prefab,
                    bendFactor = bendFactor
                };
                prototypes.Add(prototype);
            }
        }
        return new PrototypeRange(start, prototypes.Count - start);
    }

    private void ScatterTreeCategory(
        TerrainData data,
        Vector3 terrainPosition,
        RoadField field,
        List<TreeInstance> output,
        PrototypeRange prototypeRange,
        int targetCount,
        float minimumRoadDistance,
        float maximumSlope,
        float minimumWidthScale,
        float maximumWidthScale,
        float minimumHeightScale,
        float maximumHeightScale,
        int salt,
        bool undergrowth,
        System.Random random)
    {
        if (prototypeRange.count <= 0 || targetCount <= 0)
            return;

        int resolution = data.heightmapResolution;
        int placed = 0;
        int attempts = 0;
        int maximumAttempts = Mathf.Max(1000, targetCount * 40);
        while (placed < targetCount && attempts++ < maximumAttempts)
        {
            float nx = (float)random.NextDouble();
            float nz = (float)random.NextDouble();
            int hx = Mathf.Clamp(Mathf.RoundToInt(nx * (resolution - 1)), 0, resolution - 1);
            int hz = Mathf.Clamp(Mathf.RoundToInt(nz * (resolution - 1)), 0, resolution - 1);
            float roadDistance = field.distance[hz, hx];
            if (roadDistance < minimumRoadDistance)
                continue;

            float slope = data.GetSteepness(nx, nz);
            if (slope > maximumSlope)
                continue;

            float worldX = terrainPosition.x + nx * data.size.x;
            float worldZ = terrainPosition.z + nz * data.size.z;
            float cluster = FractalNoise(
                (worldX + seed * 3.17f + salt * 19.1f) / vegetationClusterScale,
                (worldZ - seed * 2.43f - salt * 13.7f) / vegetationClusterScale,
                3,
                0.54f);
            float broadCluster = Mathf.PerlinNoise(
                (worldX - seed * 0.73f + salt * 7.3f) / (vegetationClusterScale * 2.7f),
                (worldZ + seed * 1.11f - salt * 5.9f) / (vegetationClusterScale * 2.7f));

            // A few genuine clearings break uniform distribution. Undergrowth is
            // additionally encouraged near the road, like the reference forest.
            float acceptance = Mathf.Lerp(0.16f, 1f, Smooth01((cluster - 0.25f) / 0.58f));
            acceptance *= Mathf.Lerp(0.35f, 1f, broadCluster);
            if (undergrowth)
            {
                float roadsideBoost = Mathf.Exp(-Mathf.Max(0f, roadDistance - minimumRoadDistance) / 18f);
                acceptance = Mathf.Clamp01(acceptance + roadsideBoost * 0.36f);
            }
            if (random.NextDouble() > acceptance)
                continue;

            int prototypeIndex = prototypeRange.start + random.Next(prototypeRange.count);
            float widthScale = Mathf.Lerp(minimumWidthScale, maximumWidthScale, (float)random.NextDouble());
            float heightScale = Mathf.Lerp(minimumHeightScale, maximumHeightScale, (float)random.NextDouble());
            float localHeight = data.GetInterpolatedHeight(nx, nz);
            float tint = Mathf.Lerp(0.72f, 0.98f, (float)random.NextDouble());
            Color color = new Color(tint * 0.90f, tint, tint * 0.88f, 1f);

            TreeInstance instance = new TreeInstance
            {
                position = new Vector3(nx, Mathf.Clamp01(localHeight / Mathf.Max(0.001f, data.size.y)), nz),
                prototypeIndex = prototypeIndex,
                widthScale = widthScale,
                heightScale = heightScale,
                rotation = (float)random.NextDouble() * Mathf.PI * 2f,
                color = color,
                lightmapColor = Color.white
            };
            output.Add(instance);
            placed++;
        }

        if (placed < targetCount)
            Debug.LogWarning("Placed " + placed + " / " + targetCount + " vegetation instances for category salt " + salt +
                             ". Reduce road clearance/slope restrictions if you need more.");
    }

    private bool AllPaintLayersAssigned()
    {
        return roadsideMud != null &&
               darkForestSoil != null &&
               pineNeedles != null &&
               grassMoss != null;
    }

    private void ConfigureLayerTiling()
    {
        if (!autoConfigureTiling || !AllPaintLayersAssigned())
            return;

        List<TerrainLayer> layerList = new List<TerrainLayer>
        {
            roadsideMud, darkForestSoil, pineNeedles, grassMoss
        };
        if (wetRock != null)
            layerList.Add(wetRock);
        TerrainLayer[] layers = layerList.ToArray();
        Undo.RecordObjects(layers, "Configure Forest Terrain Layer Tiling");
        roadsideMud.tileSize = new Vector2(4f, 4f);
        roadsideMud.normalScale = 0.35f;
        darkForestSoil.tileSize = new Vector2(5.5f, 5.5f);
        darkForestSoil.normalScale = 0.45f;
        pineNeedles.tileSize = new Vector2(3.2f, 3.2f);
        pineNeedles.normalScale = 0.75f;
        grassMoss.tileSize = new Vector2(4.5f, 4.5f);
        grassMoss.normalScale = 0.55f;
        if (wetRock != null)
        {
            wetRock.tileSize = new Vector2(7f, 7f);
            wetRock.normalScale = 0.85f;
        }

        foreach (TerrainLayer layer in layers)
            EditorUtility.SetDirty(layer);
    }

    private bool ValidateInputs(out MeshFilter[] roadMeshes)
    {
        roadMeshes = Array.Empty<MeshFilter>();
        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Missing Terrain", "Assign a Terrain object.", "OK");
            return false;
        }

        if (roadRoot == null)
        {
            EditorUtility.DisplayDialog("Missing Road", "Assign the road object or its parent.", "OK");
            return false;
        }

        roadMeshes = roadRoot.GetComponentsInChildren<MeshFilter>(true);
        List<MeshFilter> valid = new List<MeshFilter>();
        foreach (MeshFilter meshFilter in roadMeshes)
        {
            if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.vertexCount >= 3)
                valid.Add(meshFilter);
        }

        roadMeshes = valid.ToArray();
        if (roadMeshes.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Road mesh not found",
                "The selected Road Root must contain at least one MeshFilter with a readable mesh.",
                "OK");
            return false;
        }

        return true;
    }

    private TerrainData PrepareTerrainData()
    {
        TerrainData original = targetTerrain.terrainData;
        if (!duplicateTerrainData)
            return original;

        TerrainData copy = Instantiate(original);
        copy.name = original.name + "_ForestRoadGenerated";

        const string folder = "Assets/GeneratedTerrain";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GeneratedTerrain");

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + copy.name + ".asset");
        AssetDatabase.CreateAsset(copy, assetPath);

        Undo.RecordObject(targetTerrain, "Assign Generated TerrainData");
        targetTerrain.terrainData = copy;

        TerrainCollider terrainCollider = targetTerrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            Undo.RecordObject(terrainCollider, "Assign Generated TerrainData");
            terrainCollider.terrainData = copy;
        }

        return copy;
    }

    private List<RoadSample> SampleRoadMeshes(
        MeshFilter[] roadMeshes,
        Vector3 terrainPosition,
        Vector3 terrainSize,
        int resolution)
    {
        List<RoadSample> samples = new List<RoadSample>();
        float minX = terrainPosition.x;
        float maxX = terrainPosition.x + terrainSize.x;
        float minZ = terrainPosition.z;
        float maxZ = terrainPosition.z + terrainSize.z;
        float cellX = terrainSize.x / (resolution - 1f);
        float cellZ = terrainSize.z / (resolution - 1f);
        float sampling = Mathf.Max(roadSampleSpacing, Mathf.Min(cellX, cellZ) * 0.75f);

        foreach (MeshFilter meshFilter in roadMeshes)
        {
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int triangle = 0; triangle + 2 < triangles.Length; triangle += 3)
            {
                Vector3 a = meshFilter.transform.TransformPoint(vertices[triangles[triangle]]);
                Vector3 b = meshFilter.transform.TransformPoint(vertices[triangles[triangle + 1]]);
                Vector3 c = meshFilter.transform.TransformPoint(vertices[triangles[triangle + 2]]);

                // Ignore kerbs, underside and other non-driveable faces in a thick road mesh.
                Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;
                if (faceNormal.y < 0.20f)
                    continue;

                float longestEdge = Mathf.Max(
                    HorizontalDistance(a, b),
                    Mathf.Max(HorizontalDistance(b, c), HorizontalDistance(c, a)));
                int steps = Mathf.Clamp(Mathf.CeilToInt(longestEdge / sampling), 1, 24);

                for (int row = 0; row <= steps; row++)
                {
                    for (int column = 0; column <= steps - row; column++)
                    {
                        float u = row / (float)steps;
                        float v = column / (float)steps;
                        Vector3 point = a + (b - a) * u + (c - a) * v;
                        if (point.x < minX || point.x > maxX || point.z < minZ || point.z > maxZ)
                            continue;

                        int x = Mathf.RoundToInt((point.x - minX) / terrainSize.x * (resolution - 1));
                        int z = Mathf.RoundToInt((point.z - minZ) / terrainSize.z * (resolution - 1));
                        samples.Add(new RoadSample(x, z, point.y));
                    }
                }
            }
        }

        return samples;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    private static RoadField BuildRoadField(List<RoadSample> samples, int resolution, Vector3 terrainSize)
    {
        int[,] seedX = new int[resolution, resolution];
        int[,] seedZ = new int[resolution, resolution];
        float[,] seedHeight = new float[resolution, resolution];
        int[,] seedCount = new int[resolution, resolution];

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                seedX[z, x] = -1;
                seedZ[z, x] = -1;
            }
        }

        foreach (RoadSample sample in samples)
        {
            seedHeight[sample.z, sample.x] += sample.worldY;
            seedCount[sample.z, sample.x]++;
            seedX[sample.z, sample.x] = sample.x;
            seedZ[sample.z, sample.x] = sample.z;
        }

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                if (seedCount[z, x] > 0)
                    seedHeight[z, x] /= seedCount[z, x];
            }
        }

        float cellX = terrainSize.x / (resolution - 1f);
        float cellZ = terrainSize.z / (resolution - 1f);

        // Repeated chamfer propagation stores the nearest sampled road cell.
        for (int iteration = 0; iteration < 3; iteration++)
        {
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Relax(seedX, seedZ, x, z, x - 1, z, cellX, cellZ);
                    Relax(seedX, seedZ, x, z, x, z - 1, cellX, cellZ);
                    Relax(seedX, seedZ, x, z, x - 1, z - 1, cellX, cellZ);
                    Relax(seedX, seedZ, x, z, x + 1, z - 1, cellX, cellZ);
                }
            }

            for (int z = resolution - 1; z >= 0; z--)
            {
                for (int x = resolution - 1; x >= 0; x--)
                {
                    Relax(seedX, seedZ, x, z, x + 1, z, cellX, cellZ);
                    Relax(seedX, seedZ, x, z, x, z + 1, cellX, cellZ);
                    Relax(seedX, seedZ, x, z, x + 1, z + 1, cellX, cellZ);
                    Relax(seedX, seedZ, x, z, x - 1, z + 1, cellX, cellZ);
                }
            }
        }

        float[,] distance = new float[resolution, resolution];
        float[,] roadY = new float[resolution, resolution];
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int sx = seedX[z, x];
                int sz = seedZ[z, x];
                if (sx < 0 || sz < 0)
                    throw new InvalidOperationException("Could not construct a road distance field.");

                float dx = (x - sx) * cellX;
                float dz = (z - sz) * cellZ;
                distance[z, x] = Mathf.Sqrt(dx * dx + dz * dz);
                roadY[z, x] = seedHeight[sz, sx];
            }
        }

        return new RoadField(distance, roadY);
    }

    private static void Relax(
        int[,] seedX,
        int[,] seedZ,
        int x,
        int z,
        int neighbourX,
        int neighbourZ,
        float cellX,
        float cellZ)
    {
        int resolution = seedX.GetLength(0);
        if (neighbourX < 0 || neighbourZ < 0 || neighbourX >= resolution || neighbourZ >= resolution)
            return;

        int candidateX = seedX[neighbourZ, neighbourX];
        int candidateZ = seedZ[neighbourZ, neighbourX];
        if (candidateX < 0 || candidateZ < 0)
            return;

        float candidateDx = (x - candidateX) * cellX;
        float candidateDz = (z - candidateZ) * cellZ;
        float candidateDistance = candidateDx * candidateDx + candidateDz * candidateDz;

        int currentX = seedX[z, x];
        int currentZ = seedZ[z, x];
        if (currentX >= 0 && currentZ >= 0)
        {
            float currentDx = (x - currentX) * cellX;
            float currentDz = (z - currentZ) * cellZ;
            float currentDistance = currentDx * currentDx + currentDz * currentDz;
            if (currentDistance <= candidateDistance)
                return;
        }

        seedX[z, x] = candidateX;
        seedZ[z, x] = candidateZ;
    }

    private float[,] GenerateHeights(TerrainData data, Vector3 terrainPosition, RoadField field)
    {
        int resolution = data.heightmapResolution;
        Vector3 size = data.size;
        float[,] heights = new float[resolution, resolution];
        float noiseOffsetX = 1000f + seed * 0.173f;
        float noiseOffsetZ = 2000f + seed * 0.347f;

        for (int z = 0; z < resolution; z++)
        {
            if ((z & 31) == 0)
                EditorUtility.DisplayProgressBar("Forest Road Terrain", "Generating natural height field...", 0.40f + 0.20f * z / resolution);

            float worldZ = terrainPosition.z + z / (resolution - 1f) * size.z;
            for (int x = 0; x < resolution; x++)
            {
                float worldX = terrainPosition.x + x / (resolution - 1f) * size.x;
                float distance = field.distance[z, x];

                // Low-frequency coordinate warping prevents matching shapes on both sides
                // and removes the artificial "road trench" appearance.
                float warpNoiseX = Mathf.PerlinNoise(
                    (worldX + noiseOffsetZ) / 137f,
                    (worldZ - noiseOffsetX) / 137f) - 0.5f;
                float warpNoiseZ = Mathf.PerlinNoise(
                    (worldX - noiseOffsetX) / 121f,
                    (worldZ + noiseOffsetZ) / 121f) - 0.5f;
                float warpedX = worldX + warpNoiseX * domainWarp;
                float warpedZ = worldZ + warpNoiseZ * domainWarp;

                // These fields differ spatially on the left and right sides of the road.
                float edgeNoise = FractalNoise(
                    (warpedX + noiseOffsetX) / 48f,
                    (warpedZ + noiseOffsetZ) / 48f,
                    3,
                    0.53f);
                float steepNoise = FractalNoise(
                    (warpedX - noiseOffsetZ) / 61f,
                    (warpedZ + noiseOffsetX) / 61f,
                    3,
                    0.51f);
                float heightNoise = FractalNoise(
                    (warpedX + noiseOffsetZ * 0.37f) / 74f,
                    (warpedZ - noiseOffsetX * 0.41f) / 74f,
                    4,
                    0.50f);

                float localShoulder = Mathf.Max(
                    1.5f,
                    shoulderWidth + (edgeNoise - 0.5f) * 2f * shoulderIrregularity);
                float steepFactor = Mathf.Lerp(1.45f, 0.42f, steepNoise * slopeRandomness + (1f - slopeRandomness) * 0.5f);
                float localBlendWidth = Mathf.Max(2.5f, roadBlendWidth * steepFactor);
                float transition = Smooth01((distance - localShoulder) / localBlendWidth);

                float broadNoise = FractalNoise(
                    (warpedX + noiseOffsetX) / broadHillScale,
                    (warpedZ + noiseOffsetZ) / broadHillScale,
                    5,
                    0.52f);
                broadNoise = (broadNoise - 0.50f) * 2f * broadHillAmplitude;

                float detailNoise = FractalNoise(
                    (warpedX - noiseOffsetZ) / groundVariationScale,
                    (warpedZ + noiseOffsetX) / groundVariationScale,
                    3,
                    0.48f);
                detailNoise = (detailNoise - 0.5f) * 2f * groundVariation;

                // A thresholded field creates occasional raised sections instead of
                // lifting the complete road edge by the same amount.
                float moundNoise = FractalNoise(
                    (warpedX + noiseOffsetX * 0.23f) / moundScale,
                    (warpedZ - noiseOffsetZ * 0.19f) / moundScale,
                    3,
                    0.55f);
                float moundMask = Smooth01((moundNoise - 0.57f) / 0.28f);
                float mound = moundMask * moundMask * moundAmplitude;

                float localBankHeight = bankRise + Mathf.Pow(heightNoise, 1.7f) * bankHeightVariation;
                float bankTransition = Smooth01((distance - localShoulder) / Mathf.Max(2.5f, localBlendWidth * 0.72f));
                float bank = localBankHeight * bankTransition;
                float roadSurfaceY = field.roadY[z, x] - roadClearance;
                float naturalRelief = Mathf.Max(-1.25f, bank + broadNoise + mound + detailNoise);
                float targetWorldY = roadSurfaceY + transition * naturalRelief;
                heights[z, x] = Mathf.Clamp01((targetWorldY - terrainPosition.y) / size.y);
            }
        }

        return heights;
    }

    private static float[,] SmoothAwayFromRoad(float[,] source, float[,] distance, Vector3 terrainSize, float protectedWidth)
    {
        int resolution = source.GetLength(0);
        float[,] result = (float[,])source.Clone();

        for (int z = 1; z < resolution - 1; z++)
        {
            for (int x = 1; x < resolution - 1; x++)
            {
                if (distance[z, x] <= protectedWidth)
                    continue;

                float average = 0f;
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                        average += source[z + offsetZ, x + offsetX];
                }

                average /= 9f;
                float blend = Mathf.Clamp01((distance[z, x] - protectedWidth) / 4f) * 0.45f;
                result[z, x] = Mathf.Lerp(source[z, x], average, blend);
            }
        }

        return result;
    }

    private void PaintTerrainLayers(TerrainData data, Vector3 terrainPosition, RoadField field)
    {
        // Layer order: roadside mud, dark soil, needles/leaves, moss, optional exposed rock.
        bool hasRockLayer = wetRock != null;
        data.terrainLayers = hasRockLayer
            ? new[] { roadsideMud, darkForestSoil, pineNeedles, grassMoss, wetRock }
            : new[] { roadsideMud, darkForestSoil, pineNeedles, grassMoss };
        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        int heightResolution = data.heightmapResolution;
        int layerCount = hasRockLayer ? 5 : 4;
        float[,,] map = new float[height, width, layerCount];
        float[,] heightMap = data.GetHeights(0, 0, heightResolution, heightResolution);
        float heightCellX = data.size.x / Mathf.Max(1f, heightResolution - 1f);
        float heightCellZ = data.size.z / Mathf.Max(1f, heightResolution - 1f);
        float curvatureScale = Mathf.Max(0.001f, (heightCellX + heightCellZ) * 0.5f);

        for (int z = 0; z < height; z++)
        {
            if ((z & 31) == 0)
                EditorUtility.DisplayProgressBar("Forest Road Terrain", "Painting forest layers...", 0.82f + 0.12f * z / height);

            float nz = z / Mathf.Max(1f, height - 1f);
            int hz = Mathf.Clamp(Mathf.RoundToInt(nz * (heightResolution - 1)), 0, heightResolution - 1);
            for (int x = 0; x < width; x++)
            {
                float nx = x / Mathf.Max(1f, width - 1f);
                int hx = Mathf.Clamp(Mathf.RoundToInt(nx * (heightResolution - 1)), 0, heightResolution - 1);
                float distance = field.distance[hz, hx];
                float slopeDegrees = data.GetSteepness(nx, nz);
                float rockSlope = Smooth01((slopeDegrees - rockStartSlope) / 19f);

                float worldX = terrainPosition.x + nx * data.size.x;
                float worldZ = terrainPosition.z + nz * data.size.z;
                float warpX = (Mathf.PerlinNoise((worldX + seed * 2.3f) / 91f, (worldZ - seed * 4.1f) / 91f) - 0.5f) * 24f;
                float warpZ = (Mathf.PerlinNoise((worldX - seed * 3.7f) / 107f, (worldZ + seed * 1.9f) / 107f) - 0.5f) * 24f;
                float sampleX = worldX + warpX;
                float sampleZ = worldZ + warpZ;

                float largePatch = FractalNoise(
                    (sampleX + seed * 5.7f) / largePatchScale,
                    (sampleZ - seed * 8.9f) / largePatchScale,
                    3,
                    0.53f);
                float smallPatch = FractalNoise(
                    (sampleX - seed * 6.1f) / smallPatchScale,
                    (sampleZ + seed * 3.3f) / smallPatchScale,
                    3,
                    0.49f);
                float wetPatch = FractalNoise(
                    (sampleX + seed * 11.7f) / (largePatchScale * 0.72f),
                    (sampleZ + seed * 7.4f) / (largePatchScale * 0.72f),
                    4,
                    0.51f);

                int left = Mathf.Max(0, hx - 1);
                int right = Mathf.Min(heightResolution - 1, hx + 1);
                int down = Mathf.Max(0, hz - 1);
                int up = Mathf.Min(heightResolution - 1, hz + 1);
                float centreHeight = heightMap[hz, hx];
                float neighbourAverage = (
                    heightMap[hz, left] + heightMap[hz, right] +
                    heightMap[down, hx] + heightMap[up, hx]) * 0.25f;
                float concavityMetres = (neighbourAverage - centreHeight) * data.size.y / curvatureScale;
                float hollow = Smooth01((concavityMetres + 0.015f) / 0.10f);

                // The asphalt edge gets mostly compact wet soil, but the boundary is
                // broken up by large patches instead of being a perfect parallel stripe.
                float irregularMudWidth = roadsideMudWidth * Mathf.Lerp(0.58f, 1.20f, largePatch);
                float roadside = 1f - Smooth01((distance - 0.15f) / Mathf.Max(1f, irregularMudWidth));
                float forestMask = 1f - roadside;
                float flatness = 1f - rockSlope;
                float greenEdge = Smooth01((distance - irregularMudWidth * 0.42f) / Mathf.Max(1f, irregularMudWidth * 0.90f));
                greenEdge *= 1f - Smooth01((distance - irregularMudWidth * 3.4f) / Mathf.Max(2f, irregularMudWidth * 2.2f));

                float mud = 0.018f + roadside * (1.48f + wetPatch * 0.62f);
                mud += hollow * wetPatch * forestMask * 0.18f;

                float soil = 0.13f + smallPatch * 0.12f;
                soil += roadside * 0.24f + rockSlope * 0.18f;

                float needleCluster = Smooth01((0.68f - largePatch) / 0.42f);
                float needles = forestMask * flatness * (0.18f + needleCluster * (0.72f + smallPatch * 0.34f));

                float mossCluster = Smooth01((wetPatch - 0.43f) / 0.42f);
                float moss = forestMask * flatness * (0.36f + mossCluster * (1.08f + hollow * 0.66f));
                moss *= forestGreenness;
                moss += greenEdge * greenRoadEdgeBoost * flatness * (0.72f + wetPatch * 0.48f);
                moss *= 1f - roadside * 0.30f;

                float exposedPatch = Smooth01((smallPatch - 0.58f) / 0.34f);
                float rock = 0.015f + rockSlope * (1.25f + exposedPatch * 0.95f);
                rock *= 1f - roadside * 0.82f;

                // With four layers, exposed slopes use the dark soil material.
                if (!hasRockLayer)
                {
                    soil += rock;
                    rock = 0f;
                }

                mud = Mathf.Pow(Mathf.Max(0.001f, mud), textureContrast);
                soil = Mathf.Pow(Mathf.Max(0.001f, soil), textureContrast);
                needles = Mathf.Pow(Mathf.Max(0.001f, needles), textureContrast);
                moss = Mathf.Pow(Mathf.Max(0.001f, moss), textureContrast);
                rock = Mathf.Pow(Mathf.Max(0.001f, rock), textureContrast);

                float total = Mathf.Max(0.0001f, mud + soil + needles + moss + rock);
                map[z, x, 0] = mud / total;
                map[z, x, 1] = soil / total;
                map[z, x, 2] = needles / total;
                map[z, x, 3] = moss / total;
                if (hasRockLayer)
                    map[z, x, 4] = rock / total;
            }
        }

        data.SetAlphamaps(0, 0, map);
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static float FractalNoise(float x, float z, int octaves, float persistence)
    {
        float sum = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maximum = 0f;

        for (int octave = 0; octave < octaves; octave++)
        {
            sum += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            maximum += amplitude;
            amplitude *= persistence;
            frequency *= 2.03f;
        }

        return maximum > 0f ? sum / maximum : 0f;
    }

    private enum TextureRole
    {
        Albedo,
        Normal,
        Mask
    }

    private readonly struct LayerDefinition
    {
        public readonly string displayName;
        public readonly string fileKey;
        public readonly string assetName;
        public readonly Vector2 tileSize;
        public readonly float normalScale;
        public readonly float fallbackSmoothness;

        public LayerDefinition(
            string displayName,
            string fileKey,
            string assetName,
            Vector2 tileSize,
            float normalScale,
            float fallbackSmoothness)
        {
            this.displayName = displayName;
            this.fileKey = fileKey;
            this.assetName = assetName;
            this.tileSize = tileSize;
            this.normalScale = normalScale;
            this.fallbackSmoothness = fallbackSmoothness;
        }
    }

    private readonly struct RoadSample
    {
        public readonly int x;
        public readonly int z;
        public readonly float worldY;

        public RoadSample(int x, int z, float worldY)
        {
            this.x = x;
            this.z = z;
            this.worldY = worldY;
        }
    }

    private readonly struct PrototypeRange
    {
        public readonly int start;
        public readonly int count;

        public PrototypeRange(int start, int count)
        {
            this.start = start;
            this.count = count;
        }
    }

    private readonly struct RoadField
    {
        public readonly float[,] distance;
        public readonly float[,] roadY;

        public RoadField(float[,] distance, float[,] roadY)
        {
            this.distance = distance;
            this.roadY = roadY;
        }
    }
}
#endif
