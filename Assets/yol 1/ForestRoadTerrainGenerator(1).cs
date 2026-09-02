#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    [SerializeField, Range(0.5f, 3f)] private float textureContrast = 1.35f;
    [Tooltip("Applies sensible world-space tile sizes to the assigned TerrainLayer assets.")]
    [SerializeField] private bool autoConfigureTiling = true;

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
            "adds broad forest undulation and optionally paints three Terrain Layers.",
            MessageType.Info);

        SerializedObject serializedWindow = new SerializedObject(this);
        serializedWindow.Update();

        DrawProperty(serializedWindow, "targetTerrain");
        DrawProperty(serializedWindow, "roadRoot");
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
        DrawProperty(serializedWindow, "autoConfigureTiling");
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

    private static void DrawProperty(SerializedObject so, string name)
    {
        EditorGUILayout.PropertyField(so.FindProperty(name));
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
        textureContrast = 1.35f;
        smoothingPasses = 1;
        Repaint();
    }

    private void Generate()
    {
        if (!ValidateInputs(out MeshFilter[] roadMeshes))
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

            data.SyncHeightmap();
            targetTerrain.Flush();
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(targetTerrain);
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();

            Debug.Log("Forest Road Terrain generated. Road samples: " + samples.Count);
            EditorUtility.DisplayDialog(
                "Forest Road Terrain",
                "Done. Natural terrain heights and blended forest textures were generated together.\n\n" +
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
        darkForestSoil.tileSize = new Vector2(5.5f, 5.5f);
        pineNeedles.tileSize = new Vector2(3.2f, 3.2f);
        grassMoss.tileSize = new Vector2(4.5f, 4.5f);
        if (wetRock != null)
            wetRock.tileSize = new Vector2(7f, 7f);

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
                float irregularMudWidth = roadsideMudWidth * Mathf.Lerp(0.68f, 1.34f, largePatch);
                float roadside = 1f - Smooth01((distance - 0.15f) / Mathf.Max(1f, irregularMudWidth));
                float forestMask = 1f - roadside;
                float flatness = 1f - rockSlope;

                float mud = 0.04f + roadside * (1.75f + wetPatch * 0.85f);
                mud += hollow * wetPatch * forestMask * 0.35f;

                float soil = 0.34f + smallPatch * 0.22f;
                soil += roadside * 0.34f + rockSlope * 0.20f;

                float needleCluster = Smooth01((0.68f - largePatch) / 0.42f);
                float needles = forestMask * flatness * (0.10f + needleCluster * (0.72f + smallPatch * 0.36f));

                float mossCluster = Smooth01((wetPatch - 0.43f) / 0.42f);
                float moss = forestMask * flatness * (0.06f + mossCluster * (0.80f + hollow * 0.55f));
                moss *= 1f - roadside * 0.55f;

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
