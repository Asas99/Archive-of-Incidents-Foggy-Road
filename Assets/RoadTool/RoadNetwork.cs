using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RoadNetwork : MonoBehaviour
{
    public enum CurveMode
    {
        Straight,
        SmoothCatmullRom
    }

    public enum HeightMode
    {
        SnapToTerrain,
        FreeHeight
    }

    public enum GeometryMode
    {
        SourceMeshTiles,
        ProceduralStrip
    }

    public enum UvMode
    {
        SourceUv,
        RoadGeneratedUv
    }

    public enum RoadType
    {
        Custom,
        DirtPath,
        SingleLane,
        TwoLane,
        RuralDamaged,
        Urban,
        Highway,
        Divided
    }

    public enum PaintBlendMode
    {
        Alpha,
        Additive,
        Multiply
    }

    [System.Serializable]
    public class PaintBrushType
    {
        public string name = "Damage";
        public List<Texture2D> textures = new List<Texture2D>();
        public PaintBlendMode blend = PaintBlendMode.Alpha;
        public Color tint = Color.white;
        [Min(0.1f)] public float defaultSize = 3f;
        [Range(0f, 1f)] public float defaultFalloff = 0.6f;
        [Range(0f, 1f)] public float defaultOpacity = 0.7f;
    }

    [System.Serializable]
    public class PaintStamp
    {
        public float distance;
        public float sideOffset;
        public float radius = 2f;
        public float opacity = 0.6f;
        public float rotation;
        public float scale = 1f;
        public float falloff = 0.65f;
        public float edgeJitter = 0.35f;
        public int seed;
        public Material material;
        public Texture2D texture;
        public PaintBlendMode blendMode = PaintBlendMode.Alpha;
        public Color tint = Color.white;
        public int brushTypeIndex = -1;
    }

    [System.Serializable]
    public class RoadBrushSettings
    {
        public Material brushMaterial;
        public Texture2D brushTexture;
        public List<Material> materialPalette = new List<Material>();
        public bool randomPalette;
        [Min(0.1f)] public float size = 4f;
        [Range(0.01f, 1f)] public float opacity = 0.55f;
        [Min(0.1f)] public float spacing = 2f;
        [Range(0f, 1f)] public float falloff = 0.7f;
        [Range(0f, 1f)] public float randomRotation = 1f;
        [Range(0f, 1f)] public float randomScale = 0.35f;
        [Range(0f, 1f)] public float randomOffset = 0.4f;
        [Range(0f, 1f)] public float edgeJitter = 0.45f;
        [Range(0.001f, 0.15f)] public float yOffset = 0.028f;
        public PaintBlendMode blendMode = PaintBlendMode.Alpha;
        [Range(0f, 1f)] public float flow = 1f;
        [Range(0f, 1f)] public float randomOpacity = 0.3f;
        [Range(0f, 1f)] public float randomTint = 0.25f;
        [Range(2, 12)] public int gridResolution = 6;
    }

    [System.Serializable]
    public class RoadPath
    {
        public string name = "Road";
        public bool enabled = true;
        public GeometryMode geometryMode = GeometryMode.SourceMeshTiles;
        public CurveMode curveMode = CurveMode.SmoothCatmullRom;
        public HeightMode heightMode = HeightMode.SnapToTerrain;
        public List<Vector3> points = new List<Vector3>();

        [Header("Shape")]
        public RoadType roadType = RoadType.Custom;
        [Min(0f)] public float medianWidth = 0f;
        [Min(0.5f)] public float width = 5f;
        [Min(0f)] public float shoulderWidth = 2f;
        [Range(0.05f, 2f)] public float samplesPerMeter = 0.25f;
        [Min(0.25f)] public float uvMetersPerTile = 5f;
        public float roadYOffset = 0.035f;
        [Range(1, 41)] public int smoothGradeWindow = 9;

        [Header("Output")]
        public Material roadMaterial;
        public List<Material> materialVariants = new List<Material>();
        public bool randomizeMaterials = true;
        public int materialSeed = 1729;
        [Min(1f)] public float materialRunMeters = 18f;
        public UvMode uvMode = UvMode.SourceUv;
        public Vector2 uvScale = Vector2.one;
        public Vector2 uvOffset = Vector2.zero;
        public bool swapUv;
        public bool flipU;
        public bool flipV;
        public bool flipSourceForward;

        [Header("Realism")]
        public bool enableDeformation = true;
        public int deformationSeed = 911;
        [Min(0.1f)] public float deformationScale = 14f;
        [Range(0f, 1f)] public float surfaceHeightNoise = 0.035f;
        [Range(0f, 2f)] public float edgeWidthNoise = 0.18f;
        [Range(0f, 1f)] public float flatSectionChance = 0.35f;
        [Min(1f)] public float flatSectionMeters = 24f;

        [Header("Overlay Projection")]
        public bool enableOverlayProjection = true;
        public int overlaySeed = 4411;
        [Range(0f, 1f)] public float overlayDensity = 0.55f;
        [Min(1f)] public float overlayPatchMeters = 12f;
        [Range(0.05f, 1f)] public float overlayMinWidth = 0.35f;
        [Range(0.05f, 1.25f)] public float overlayMaxWidth = 1f;
        [Range(0.01f, 1f)] public float overlayAlpha = 0.45f;
        [Range(0.001f, 0.15f)] public float overlayYOffset = 0.018f;
        [Range(1, 5)] public int overlayLayers = 3;
        [Range(0f, 1f)] public float overlayEdgeJitter = 0.65f;
        [Range(0f, 1f)] public float overlayCenterJitter = 0.55f;
        [Range(0f, 1f)] public float overlayUvRandomness = 0.9f;

        public bool updateCollider = true;
        public GameObject generatedObject;
        public GameObject overlayObject;
        public GameObject paintObject;
        public RoadBrushSettings brush = new RoadBrushSettings();
        public List<PaintStamp> paintStamps = new List<PaintStamp>();
    }

    public struct RoadFrame
    {
        public Vector3 center;
        public Vector3 tangent;
        public Vector3 right;
        public float distance;
    }

    struct RoadSample
    {
        public Vector3 center;
        public float distance;
    }

    [Header("Road Source Object")]
    public GameObject roadSourceObject;
    public bool useSourceMaterial = true;
    public bool useSourceWidth;
    [Min(0.1f)] public float sourceWidthMultiplier = 1f;
    [Min(0.1f)] public float minimumSourceWidth = 3f;
    public bool useSourceMeshTiles = true;

    public Terrain terrain;
    public bool autoRebuildMeshes = true;
    public List<RoadPath> roads = new List<RoadPath>();

    [Header("Paint Damage Library")]
    public List<PaintBrushType> brushTypes = new List<PaintBrushType>();
    public int activeBrushTypeIndex;

    [Header("Terrain Deformation")]
    public bool deformTerrainHeights = true;
    public float terrainHeightOffset = -0.02f;
    public TerrainLayer roadLayer;
    public TerrainLayer shoulderLayer;
    public float treeClearance = 8f;

    void Reset()
    {
        terrain = Terrain.activeTerrain;
        EnsureDefaultRoad();
    }

    void OnValidate()
    {
        EnsureDefaultRoad();
    }

    public RoadPath ActiveRoad(int index)
    {
        EnsureDefaultRoad();
        return roads[Mathf.Clamp(index, 0, roads.Count - 1)];
    }

    public int AddRoad()
    {
        roads.Add(new RoadPath { name = $"Road {roads.Count + 1}" });
        return roads.Count - 1;
    }

    public void AddPoint(int roadIndex, Vector3 worldPosition)
    {
        RoadPath road = ActiveRoad(roadIndex);
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        if (road.heightMode == HeightMode.SnapToTerrain)
            local.y = SampleTerrainLocalY(local) + road.roadYOffset;
        road.points.Add(local);
        RebuildAllMeshes();
    }

    public void RemovePoint(int roadIndex, int pointIndex)
    {
        RoadPath road = ActiveRoad(roadIndex);
        if (pointIndex < 0 || pointIndex >= road.points.Count)
            return;

        road.points.RemoveAt(pointIndex);
        RebuildAllMeshes();
    }

    // Removes ONLY this road and its own generated child objects. Other roads are untouched;
    // RebuildAllMeshes then renames the survivors to their new indices.
    public void RemoveRoad(int roadIndex)
    {
        if (roads == null || roadIndex < 0 || roadIndex >= roads.Count)
            return;

        RoadPath road = roads[roadIndex];
        DestroyRoadObject(road.generatedObject);
        DestroyRoadObject(road.overlayObject);
        DestroyRoadObject(road.paintObject);
        roads.RemoveAt(roadIndex);

        EnsureDefaultRoad();
        RebuildAllMeshes();
    }

    void DestroyRoadObject(GameObject go)
    {
        if (go == null)
            return;
        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }

    public Vector3 GetPointWorld(int roadIndex, int pointIndex)
    {
        RoadPath road = ActiveRoad(roadIndex);
        return transform.TransformPoint(road.points[pointIndex]);
    }

    public void SetPointWorld(int roadIndex, int pointIndex, Vector3 worldPosition)
    {
        RoadPath road = ActiveRoad(roadIndex);
        if (pointIndex < 0 || pointIndex >= road.points.Count)
            return;

        Vector3 local = transform.InverseTransformPoint(worldPosition);
        if (road.heightMode == HeightMode.SnapToTerrain)
            local.y = SampleTerrainLocalY(local) + road.roadYOffset;

        road.points[pointIndex] = local;
        RebuildAllMeshes();
    }

    public void RebuildAllMeshes()
    {
        EnsureDefaultRoad();
        for (int i = 0; i < roads.Count; i++)
            RebuildMesh(i);
    }

    public void ApplySourceObjectToRoad(int roadIndex)
    {
        RoadPath road = ActiveRoad(roadIndex);
        if (roadSourceObject == null)
            return;

        if (useSourceMeshTiles)
            road.geometryMode = GeometryMode.SourceMeshTiles;

        if (useSourceMaterial)
        {
            Renderer sourceRenderer = roadSourceObject.GetComponentInChildren<Renderer>();
            if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
            {
                road.roadMaterial = sourceRenderer.sharedMaterial;
                AddUniqueMaterials(road, sourceRenderer.sharedMaterials);
            }
        }

        if (useSourceWidth && TryGetSourceWidth(out float sourceWidth))
        {
            road.width = Mathf.Max(minimumSourceWidth, sourceWidth * sourceWidthMultiplier);
            road.shoulderWidth = Mathf.Max(0.5f, road.width * 0.35f);
        }

        if (TryGetSourceBounds(out SourceBounds bounds))
            road.uvMetersPerTile = Mathf.Max(0.25f, bounds.length);

        RebuildAllMeshes();
    }

    public void ApplyTerrain()
    {
        Terrain target = terrain != null ? terrain : Terrain.activeTerrain;
        if (target == null)
        {
            Debug.LogWarning("[RoadTool] Terrain bulunamadi. Terrain alanini doldur veya sahneye Terrain ekle.");
            return;
        }

        if (deformTerrainHeights)
            ApplyTerrainHeights(target);

        if (roadLayer != null || shoulderLayer != null)
            PaintTerrainLayers(target);

        ClearTreesInRoadCorridors(target);
        target.Flush();
    }

    public void BakeGeneratedMeshes()
    {
#if UNITY_EDITOR
        const string folder = "Assets/RoadTool/BakedMeshes";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
            UnityEditor.AssetDatabase.CreateFolder("Assets/RoadTool", "BakedMeshes");

        foreach (RoadPath road in roads)
        {
            if (road.generatedObject == null)
                continue;

            MeshFilter filter = road.generatedObject.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            Mesh baked = Instantiate(filter.sharedMesh);
            baked.name = $"{road.name}_BakedMesh";
            string safeName = string.Join("_", road.name.Split(System.IO.Path.GetInvalidFileNameChars()));
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}_Road.asset");
            UnityEditor.AssetDatabase.CreateAsset(baked, path);
            filter.sharedMesh = baked;

            MeshCollider collider = road.generatedObject.GetComponent<MeshCollider>();
            if (collider != null)
                collider.sharedMesh = baked;
        }

        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    public void BakePaintMeshes()
    {
#if UNITY_EDITOR
        const string folder = "Assets/RoadTool/BakedMeshes";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
            UnityEditor.AssetDatabase.CreateFolder("Assets/RoadTool", "BakedMeshes");

        foreach (RoadPath road in roads)
        {
            if (road.paintObject == null)
                continue;

            MeshFilter filter = road.paintObject.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            string safeName = string.Join("_", road.name.Split(System.IO.Path.GetInvalidFileNameChars()));

            Mesh baked = Instantiate(filter.sharedMesh);
            baked.name = $"{road.name}_Paint_BakedMesh";
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}_Paint.asset");
            UnityEditor.AssetDatabase.CreateAsset(baked, path);
            filter.sharedMesh = baked;

            // Persist the runtime (DontSave) decal materials as real assets, so the baked
            // scene still renders if this generator component is later removed.
            MeshRenderer renderer = road.paintObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material[] shared = renderer.sharedMaterials;
                Material[] savedMaterials = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null)
                        continue;

                    Material savedMaterial = new Material(shared[i]) { hideFlags = HideFlags.None };
                    string matPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}_PaintMat_{i}.mat");
                    UnityEditor.AssetDatabase.CreateAsset(savedMaterial, matPath);
                    savedMaterials[i] = savedMaterial;
                }
                renderer.sharedMaterials = savedMaterials;
            }
        }

        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    void EnsureDefaultRoad()
    {
        if (roads == null)
            roads = new List<RoadPath>();

        if (roads.Count == 0)
            roads.Add(new RoadPath { name = "Main Road" });
    }

    void RebuildMesh(int roadIndex)
    {
        RoadPath road = roads[roadIndex];
        if (!road.enabled || road.points == null || road.points.Count < 2)
        {
            if (road.generatedObject != null)
                road.generatedObject.SetActive(false);
            if (road.overlayObject != null)
                road.overlayObject.SetActive(false);
            if (road.paintObject != null)
                road.paintObject.SetActive(false);
            return;
        }

        GameObject roadObject = EnsureGeneratedObject(road, roadIndex);
        List<RoadSample> samples = BuildSamples(road);
        if (samples.Count < 2)
            return;

        Mesh mesh = road.geometryMode == GeometryMode.SourceMeshTiles && roadSourceObject != null
            ? BuildSourceMeshRoadMesh(road, samples)
            : BuildRoadMesh(road, samples);
        MeshFilter filter = roadObject.GetComponent<MeshFilter>();
        if (filter.sharedMesh != null)
            DestroyMesh(filter.sharedMesh);
        filter.sharedMesh = mesh;

        MeshRenderer renderer = roadObject.GetComponent<MeshRenderer>();
        Material[] materials = GetRoadMaterials(road);
        if (materials.Length > 0)
            renderer.sharedMaterials = materials;

        MeshCollider collider = roadObject.GetComponent<MeshCollider>();
        if (road.updateCollider)
        {
            if (collider == null)
                collider = roadObject.AddComponent<MeshCollider>();
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }
        else if (collider != null)
        {
            DestroyImmediate(collider);
        }

        RebuildOverlayMesh(road, roadIndex, samples);
        RebuildPaintOverlayMesh(road, roadIndex, samples);
    }

    GameObject EnsureGeneratedObject(RoadPath road, int roadIndex)
    {
        if (road.generatedObject == null)
        {
            road.generatedObject = new GameObject($"Generated_{road.name}");
            road.generatedObject.transform.SetParent(transform, false);
        }

        if (road.generatedObject.GetComponent<MeshFilter>() == null)
            road.generatedObject.AddComponent<MeshFilter>();
        if (road.generatedObject.GetComponent<MeshRenderer>() == null)
            road.generatedObject.AddComponent<MeshRenderer>();

        road.generatedObject.name = $"Generated_{roadIndex + 1}_{road.name}";
        road.generatedObject.SetActive(true);
        return road.generatedObject;
    }

    GameObject EnsureOverlayObject(RoadPath road, int roadIndex)
    {
        if (road.overlayObject == null)
        {
            road.overlayObject = new GameObject($"Overlay_{road.name}");
            road.overlayObject.transform.SetParent(transform, false);
        }

        if (road.overlayObject.GetComponent<MeshFilter>() == null)
            road.overlayObject.AddComponent<MeshFilter>();
        if (road.overlayObject.GetComponent<MeshRenderer>() == null)
            road.overlayObject.AddComponent<MeshRenderer>();

        road.overlayObject.name = $"Overlay_{roadIndex + 1}_{road.name}";
        road.overlayObject.SetActive(true);
        return road.overlayObject;
    }

    GameObject EnsurePaintObject(RoadPath road, int roadIndex)
    {
        if (road.paintObject == null)
        {
            road.paintObject = new GameObject($"PaintOverlay_{road.name}");
            road.paintObject.transform.SetParent(transform, false);
        }

        if (road.paintObject.GetComponent<MeshFilter>() == null)
            road.paintObject.AddComponent<MeshFilter>();
        if (road.paintObject.GetComponent<MeshRenderer>() == null)
            road.paintObject.AddComponent<MeshRenderer>();

        road.paintObject.name = $"PaintOverlay_{roadIndex + 1}_{road.name}";
        road.paintObject.SetActive(true);
        return road.paintObject;
    }

    Mesh BuildRoadMesh(RoadPath road, List<RoadSample> samples)
    {
        if (road.medianWidth > 0.01f && road.medianWidth < road.width - 0.1f)
            return BuildDividedRoadMesh(road, samples);

        int materialCount = Mathf.Max(1, GetRoadMaterialCount(road));
        int vertexCount = samples.Count * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        List<int>[] trianglesByMaterial = CreateTriangleBuckets(materialCount);

        for (int i = 0; i < samples.Count; i++)
        {
            Vector3 tangent = GetSampleTangent(samples, i);
            Vector3 right = new Vector3(tangent.z, 0f, -tangent.x);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            right.Normalize();

            float halfWidth = road.width * 0.5f;
            int left = i * 2;
            int rightIndex = left + 1;
            vertices[left] = BuildDeformedVertex(road, samples[i].center, right, -halfWidth, samples[i].distance, -1f, 0f);
            vertices[rightIndex] = BuildDeformedVertex(road, samples[i].center, right, halfWidth, samples[i].distance, 1f, 0f);
            normals[left] = Vector3.up;
            normals[rightIndex] = Vector3.up;

            float v = samples[i].distance / Mathf.Max(0.001f, road.uvMetersPerTile);
            uvs[left] = TransformRoadUv(road, new Vector2(0f, v));
            uvs[rightIndex] = TransformRoadUv(road, new Vector2(1f, v));
        }

        for (int i = 0; i < samples.Count - 1; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;
            List<int> bucket = trianglesByMaterial[GetMaterialSlotForDistance(road, samples[i].distance, materialCount)];

            bucket.Add(a);
            bucket.Add(c);
            bucket.Add(b);
            bucket.Add(b);
            bucket.Add(c);
            bucket.Add(d);
        }

        Mesh mesh = new Mesh { name = $"{road.name}_ProceduralRoad" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        ApplySubmeshTriangles(mesh, trianglesByMaterial);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // Divided / dual-carriageway road: two strips with a central median gap (medianWidth).
    // Only used in ProceduralStrip mode; the Divided preset switches geometry to that.
    Mesh BuildDividedRoadMesh(RoadPath road, List<RoadSample> samples)
    {
        int materialCount = Mathf.Max(1, GetRoadMaterialCount(road));
        int vertexCount = samples.Count * 4;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        List<int>[] trianglesByMaterial = CreateTriangleBuckets(materialCount);

        float halfWidth = road.width * 0.5f;
        float halfMedian = Mathf.Clamp(road.medianWidth * 0.5f, 0.05f, halfWidth - 0.2f);
        float edgeRef = Mathf.Max(0.01f, halfWidth);

        for (int i = 0; i < samples.Count; i++)
        {
            Vector3 tangent = GetSampleTangent(samples, i);
            Vector3 right = new Vector3(tangent.z, 0f, -tangent.x);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            right.Normalize();

            int b = i * 4;
            float d = samples[i].distance;
            vertices[b + 0] = BuildDeformedVertex(road, samples[i].center, right, -halfWidth, d, -1f, 0f);
            vertices[b + 1] = BuildDeformedVertex(road, samples[i].center, right, -halfMedian, d, -halfMedian / edgeRef, 0f);
            vertices[b + 2] = BuildDeformedVertex(road, samples[i].center, right, halfMedian, d, halfMedian / edgeRef, 0f);
            vertices[b + 3] = BuildDeformedVertex(road, samples[i].center, right, halfWidth, d, 1f, 0f);
            normals[b + 0] = normals[b + 1] = normals[b + 2] = normals[b + 3] = Vector3.up;

            float v = d / Mathf.Max(0.001f, road.uvMetersPerTile);
            uvs[b + 0] = TransformRoadUv(road, new Vector2(0f, v));
            uvs[b + 1] = TransformRoadUv(road, new Vector2(1f, v));
            uvs[b + 2] = TransformRoadUv(road, new Vector2(0f, v));
            uvs[b + 3] = TransformRoadUv(road, new Vector2(1f, v));
        }

        for (int i = 0; i < samples.Count - 1; i++)
        {
            int b = i * 4;
            int nb = (i + 1) * 4;
            List<int> bucket = trianglesByMaterial[GetMaterialSlotForDistance(road, samples[i].distance, materialCount)];
            AddRoadQuad(bucket, b + 0, b + 1, nb + 0, nb + 1); // left carriageway
            AddRoadQuad(bucket, b + 2, b + 3, nb + 2, nb + 3); // right carriageway
        }

        Mesh mesh = new Mesh { name = $"{road.name}_DividedRoad" };
        if (vertexCount > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        ApplySubmeshTriangles(mesh, trianglesByMaterial);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static void AddRoadQuad(List<int> bucket, int thisLeft, int thisRight, int nextLeft, int nextRight)
    {
        bucket.Add(thisLeft);
        bucket.Add(nextLeft);
        bucket.Add(thisRight);
        bucket.Add(thisRight);
        bucket.Add(nextLeft);
        bucket.Add(nextRight);
    }

    Mesh BuildSourceMeshRoadMesh(RoadPath road, List<RoadSample> samples)
    {
        SourceMeshData source = CollectSourceMeshData();
        if (!source.valid || source.bounds.length <= 0.001f || source.bounds.width <= 0.001f)
            return BuildRoadMesh(road, samples);

        float totalDistance = samples[samples.Count - 1].distance;
        float tileLength = Mathf.Max(0.25f, source.bounds.length);
        int tileCount = Mathf.Max(1, Mathf.CeilToInt(totalDistance / tileLength));
        int vertexCount = source.vertices.Count * tileCount;
        int materialCount = Mathf.Max(1, GetRoadMaterialCount(road));

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        List<int>[] trianglesByMaterial = CreateTriangleBuckets(materialCount);

        int vertexOffset = 0;
        float widthScale = road.width / source.bounds.width;

        for (int tile = 0; tile < tileCount; tile++)
        {
            float tileStart = tile * tileLength;
            float tileEnd = Mathf.Min(tileStart + tileLength, Mathf.Max(tileStart + 0.01f, totalDistance));
            float effectiveTileLength = Mathf.Max(0.01f, tileEnd - tileStart);

            for (int i = 0; i < source.vertices.Count; i++)
            {
                Vector3 local = source.vertices[i];
                float sourceForward = GetAxis(local, source.bounds.lengthAxis);
                float sourceSide = GetAxis(local, source.bounds.widthAxis);
                float forward01 = Mathf.InverseLerp(source.bounds.lengthMin, source.bounds.lengthMax, sourceForward);
                if (road.flipSourceForward)
                    forward01 = 1f - forward01;

                float distance = tileStart + forward01 * effectiveTileLength;
                RoadFrame frame = EvaluateFrame(samples, distance);
                float sideOffset = (sourceSide - source.bounds.widthCenter) * widthScale;
                float heightOffset = local.y - source.bounds.bottomY;
                float normalizedSide = Mathf.Clamp(sideOffset / Mathf.Max(0.001f, road.width * 0.5f), -1f, 1f);

                vertices[vertexOffset + i] = BuildDeformedVertex(road, frame.center, frame.right, sideOffset, distance, normalizedSide, heightOffset);
                normals[vertexOffset + i] = TransformSourceNormal(source.normals.Count > i ? source.normals[i] : Vector3.up, frame, source.bounds);
                Vector2 sourceUv = source.uvs.Count > i ? source.uvs[i] : Vector2.zero;
                Vector2 generatedUv = new Vector2(
                    Mathf.InverseLerp(source.bounds.widthCenter - source.bounds.width * 0.5f, source.bounds.widthCenter + source.bounds.width * 0.5f, sourceSide),
                    distance / Mathf.Max(0.001f, road.uvMetersPerTile));
                uvs[vertexOffset + i] = TransformRoadUv(road, road.uvMode == UvMode.SourceUv ? sourceUv : generatedUv);
            }

            List<int> bucket = trianglesByMaterial[GetMaterialSlotForDistance(road, tileStart, materialCount)];
            for (int i = 0; i < source.triangles.Count; i++)
                bucket.Add(source.triangles[i] + vertexOffset);

            vertexOffset += source.vertices.Count;
        }

        Mesh mesh = new Mesh { name = $"{road.name}_SourceMeshRoad" };
        if (vertexCount > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        ApplySubmeshTriangles(mesh, trianglesByMaterial);
        mesh.RecalculateBounds();
        if (normals.Length == 0)
            mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

    void RebuildOverlayMesh(RoadPath road, int roadIndex, List<RoadSample> samples)
    {
        if (!road.enableOverlayProjection || road.overlayDensity <= 0f || samples.Count < 2)
        {
            if (road.overlayObject != null)
                road.overlayObject.SetActive(false);
            return;
        }

        Material[] overlayMaterials = CreateOverlayMaterials(road);
        if (overlayMaterials.Length == 0)
        {
            if (road.overlayObject != null)
                road.overlayObject.SetActive(false);
            return;
        }

        GameObject overlayObject = EnsureOverlayObject(road, roadIndex);
        MeshFilter filter = overlayObject.GetComponent<MeshFilter>();
        if (filter.sharedMesh != null)
            DestroyMesh(filter.sharedMesh);

        Mesh mesh = BuildOverlayProjectionMesh(road, samples, overlayMaterials.Length);
        filter.sharedMesh = mesh;

        MeshRenderer renderer = overlayObject.GetComponent<MeshRenderer>();
        ClearGeneratedOverlayMaterials(renderer.sharedMaterials);
        renderer.sharedMaterials = overlayMaterials;
    }

    Mesh BuildOverlayProjectionMesh(RoadPath road, List<RoadSample> samples, int materialCount)
    {
        float totalDistance = samples[samples.Count - 1].distance;
        float patchStep = Mathf.Max(1f, road.overlayPatchMeters);
        int patchSlots = Mathf.Max(1, Mathf.CeilToInt(totalDistance / patchStep));

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int>[] trianglesByMaterial = CreateTriangleBuckets(materialCount);

        int layerCount = Mathf.Max(1, road.overlayLayers);
        for (int layer = 0; layer < layerCount; layer++)
        {
            float layerDensity = Mathf.Clamp01(road.overlayDensity * Mathf.Lerp(1f, 0.65f, layer / (float)Mathf.Max(1, layerCount - 1)));
            for (int patch = 0; patch < patchSlots; patch++)
            {
                int patchId = layer * 100003 + patch;
                if (Hash01(road.overlaySeed, patchId, 13) > layerDensity)
                    continue;

                float jitter = Mathf.Lerp(-0.55f, 0.55f, Hash01(road.overlaySeed, patchId, 21));
                float start = Mathf.Max(0f, patch * patchStep + jitter * patchStep);
                float length = patchStep * Mathf.Lerp(0.35f, 2.2f, Hash01(road.overlaySeed, patchId, 34));
                float end = Mathf.Min(totalDistance, start + length);
                if (end - start < 0.25f)
                    continue;

                float widthFactor = Mathf.Lerp(road.overlayMinWidth, road.overlayMaxWidth, Hash01(road.overlaySeed, patchId, 55));
                float patchWidth = Mathf.Clamp(road.width * widthFactor, 0.2f, road.width * 1.18f);
                float centerShift = Mathf.Lerp(-road.overlayCenterJitter, road.overlayCenterJitter, Hash01(road.overlaySeed, patchId, 89)) * road.width * 0.5f;
                int materialSlot = Mathf.Abs(HashInts(road.overlaySeed, patchId, 144)) % materialCount;
                int segments = Mathf.Clamp(Mathf.CeilToInt((end - start) * Mathf.Max(0.18f, road.samplesPerMeter)), 3, 28);
                float uvOffsetU = Mathf.Lerp(-6f, 6f, Hash01(road.overlaySeed, patchId, 233)) * road.overlayUvRandomness;
                float uvOffsetV = Mathf.Lerp(-6f, 6f, Hash01(road.overlaySeed, patchId, 377)) * road.overlayUvRandomness;
                float uvScale = Mathf.Lerp(0.55f, 2.4f, Hash01(road.overlaySeed, patchId, 610));
                bool patchFlipU = Hash01(road.overlaySeed, patchId, 987) > 0.5f;
                bool patchFlipV = Hash01(road.overlaySeed, patchId, 1597) > 0.5f;

                int baseVertex = vertices.Count;
                for (int s = 0; s <= segments; s++)
                {
                    float t = s / (float)segments;
                    float distance = Mathf.Lerp(start, end, t);
                    RoadFrame frame = EvaluateFrame(samples, distance);
                    float taper = Mathf.Sin(t * Mathf.PI);
                    float waviness = Mathf.Lerp(0.65f, 1.25f, Hash01(road.overlaySeed, patchId, 2000 + s));
                    float currentWidth = patchWidth * Mathf.Lerp(0.08f, 1f, taper) * waviness;
                    float edgeJitter = road.overlayEdgeJitter * patchWidth * 0.22f;
                    float leftJitter = Mathf.Lerp(-edgeJitter, edgeJitter, Hash01(road.overlaySeed, patchId, 3100 + s));
                    float rightJitter = Mathf.Lerp(-edgeJitter, edgeJitter, Hash01(road.overlaySeed, patchId, 4100 + s));
                    float centerWobble = Mathf.Lerp(-road.overlayCenterJitter, road.overlayCenterJitter, Hash01(road.overlaySeed, patchId, 5100 + s)) * road.width * 0.12f;
                    float center = centerShift + centerWobble;
                    float leftSide = Mathf.Clamp(center - currentWidth * 0.5f + leftJitter, -road.width * 0.6f, road.width * 0.6f);
                    float rightSide = Mathf.Clamp(center + currentWidth * 0.5f + rightJitter, -road.width * 0.6f, road.width * 0.6f);
                    if (rightSide < leftSide + 0.05f)
                        rightSide = leftSide + 0.05f;

                    float lift = road.overlayYOffset + 0.001f * patch + 0.004f * layer;
                    vertices.Add(frame.center + frame.right * leftSide + Vector3.up * lift);
                    vertices.Add(frame.center + frame.right * rightSide + Vector3.up * lift);
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);

                    float v = (distance - start) / Mathf.Max(0.001f, road.uvMetersPerTile);
                    float leftU = patchFlipU ? 1f : 0f;
                    float rightU = patchFlipU ? 0f : 1f;
                    float patchV = patchFlipV ? 1f - v : v;
                    uvs.Add(TransformRoadUv(road, new Vector2(leftU * uvScale + uvOffsetU, patchV * uvScale + uvOffsetV)));
                    uvs.Add(TransformRoadUv(road, new Vector2(rightU * uvScale + uvOffsetU, patchV * uvScale + uvOffsetV)));
                }

                List<int> bucket = trianglesByMaterial[materialSlot];
                for (int s = 0; s < segments; s++)
                {
                    int a = baseVertex + s * 2;
                    int b = a + 1;
                    int c = a + 2;
                    int d = a + 3;

                    bucket.Add(a);
                    bucket.Add(c);
                    bucket.Add(b);
                    bucket.Add(b);
                    bucket.Add(c);
                    bucket.Add(d);
                }
            }
        }

        Mesh mesh = new Mesh { name = $"{road.name}_OverlayProjection" };
        if (vertices.Count > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        ApplySubmeshTriangles(mesh, trianglesByMaterial);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    Material[] CreateOverlayMaterials(RoadPath road)
    {
        Material[] baseMaterials = GetRoadMaterials(road);
        List<Material> overlays = new List<Material>();
        for (int i = 0; i < baseMaterials.Length; i++)
        {
            Material source = baseMaterials[i];
            if (source == null)
                continue;

            Material overlay = new Material(source)
            {
                name = $"RoadOverlay_{source.name}",
                hideFlags = HideFlags.DontSave
            };
            ConfigureOverlayMaterial(overlay, road.overlayAlpha, i);
            overlays.Add(overlay);
        }

        return overlays.ToArray();
    }

    static void ConfigureOverlayMaterial(Material material, float alpha, int materialIndex)
    {
        alpha = Mathf.Clamp01(alpha);
        float tint = Mathf.Lerp(0.62f, 1.18f, Hash01(1947, materialIndex, 771));
        if (material.HasProperty("_BaseColor"))
        {
            Color color = material.GetColor("_BaseColor");
            color.r *= tint;
            color.g *= tint;
            color.b *= tint;
            color.a = alpha;
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            Color color = material.GetColor("_Color");
            color.r *= tint;
            color.g *= tint;
            color.b *= tint;
            color.a = alpha;
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    static void ClearGeneratedOverlayMaterials(Material[] materials)
    {
        if (materials == null)
            return;

        foreach (Material material in materials)
        {
            if (material == null || !material.name.StartsWith("RoadOverlay_"))
                continue;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }
    }

    void RebuildPaintOverlayMesh(RoadPath road, int roadIndex, List<RoadSample> samples)
    {
        if (road.paintStamps == null || road.paintStamps.Count == 0 || samples.Count < 2)
        {
            if (road.paintObject != null)
                road.paintObject.SetActive(false);
            return;
        }

        GameObject paintObject = EnsurePaintObject(road, roadIndex);
        MeshFilter filter = paintObject.GetComponent<MeshFilter>();
        if (filter.sharedMesh != null)
            DestroyMesh(filter.sharedMesh);

        MeshRenderer renderer = paintObject.GetComponent<MeshRenderer>();

        List<Material> materials = new List<Material>();
        Mesh mesh = BuildPaintOverlayMesh(road, samples, materials);
        filter.sharedMesh = mesh;
        renderer.sharedMaterials = materials.ToArray();
    }

    Mesh BuildPaintOverlayMesh(RoadPath road, List<RoadSample> samples, List<Material> materials)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs0 = new List<Vector2>();
        List<Vector2> uvs1 = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<List<int>> buckets = new List<List<int>>();
        Dictionary<(Texture2D, PaintBlendMode), int> keyToSlot = new Dictionary<(Texture2D, PaintBlendMode), int>();

        int grid = Mathf.Clamp(road.brush != null ? road.brush.gridResolution : 6, 2, 12);
        float yBase = Mathf.Max(0.001f, road.brush != null ? road.brush.yOffset : 0.028f);

        for (int s = 0; s < road.paintStamps.Count; s++)
        {
            PaintStamp stamp = road.paintStamps[s];
            Texture2D texture = ResolveStampTexture(stamp, road.brush);
            if (texture == null)
                texture = WhiteDecalTexture();

            var key = (texture, stamp.blendMode);
            if (!keyToSlot.TryGetValue(key, out int slot))
            {
                slot = buckets.Count;
                keyToSlot[key] = slot;
                buckets.Add(new List<int>());
                materials.Add(GetDecalMaterial(texture, stamp.blendMode));
            }

            AppendStampGrid(stamp, samples, grid, yBase + slot * 0.0004f,
                vertices, normals, uvs0, uvs1, colors, buckets[slot]);
        }

        Mesh mesh = new Mesh { name = $"{road.name}_PaintOverlay" };
        if (vertices.Count > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs0);
        mesh.SetUVs(1, uvs1);
        mesh.SetColors(colors);
        mesh.subMeshCount = buckets.Count;
        for (int i = 0; i < buckets.Count; i++)
            mesh.SetTriangles(buckets[i], i);
        mesh.RecalculateBounds();
        return mesh;
    }

    // One soft, rounded grid quad per stamp, conforming to the road frame. Rotation is applied
    // to the LOCAL quad (UVs stay 0..1) so the texture visibly rotates. Per-stamp falloff rides
    // in UV1.x and tint/opacity in vertex color, so many stamps share one batched material.
    void AppendStampGrid(PaintStamp stamp, List<RoadSample> samples, int grid, float yLift,
        List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs0, List<Vector2> uvs1,
        List<Color> colors, List<int> triangles)
    {
        Color tint = stamp.tint;
        if (stamp.material != null)
        {
            if (stamp.material.HasProperty("_BaseColor"))
                tint *= stamp.material.GetColor("_BaseColor");
            else if (stamp.material.HasProperty("_Color"))
                tint *= stamp.material.GetColor("_Color");
        }
        Color vertexColor = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(stamp.opacity * tint.a));
        Vector2 falloffUv = new Vector2(Mathf.Clamp01(stamp.falloff), 0f);

        float half = Mathf.Max(0.05f, stamp.radius * Mathf.Max(0.05f, stamp.scale));
        float rotationRad = stamp.rotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rotationRad);
        float sin = Mathf.Sin(rotationRad);
        int baseVertex = vertices.Count;

        for (int gy = 0; gy < grid; gy++)
        {
            for (int gx = 0; gx < grid; gx++)
            {
                float u = gx / (grid - 1f);
                float v = gy / (grid - 1f);
                float localX = (u - 0.5f) * 2f * half;
                float localY = (v - 0.5f) * 2f * half;

                if (stamp.edgeJitter > 0f && (gx == 0 || gy == 0 || gx == grid - 1 || gy == grid - 1))
                {
                    float jitter = 1f + (Hash01(stamp.seed, gy * grid + gx, 9157) - 0.5f) * 2f * stamp.edgeJitter * 0.5f;
                    localX *= jitter;
                    localY *= jitter;
                }

                float rotatedX = localX * cos - localY * sin;
                float rotatedY = localX * sin + localY * cos;
                RoadFrame frame = EvaluateFrame(samples, stamp.distance + rotatedY);
                vertices.Add(frame.center + frame.right * (stamp.sideOffset + rotatedX) + Vector3.up * yLift);
                normals.Add(Vector3.up);
                uvs0.Add(new Vector2(u, v));
                uvs1.Add(falloffUv);
                colors.Add(vertexColor);
            }
        }

        for (int gy = 0; gy < grid - 1; gy++)
        {
            for (int gx = 0; gx < grid - 1; gx++)
            {
                int i0 = baseVertex + gy * grid + gx;
                int i1 = i0 + 1;
                int i2 = i0 + grid;
                int i3 = i2 + 1;
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }
    }

    static Shader s_decalShader;
    static readonly Dictionary<(Texture2D, PaintBlendMode), Material> s_decalMaterials =
        new Dictionary<(Texture2D, PaintBlendMode), Material>();
    static Texture2D s_whiteDecal;

    static Texture2D ResolveStampTexture(PaintStamp stamp, RoadBrushSettings brush)
    {
        if (stamp.texture != null)
            return stamp.texture;

        if (stamp.material != null)
        {
            Texture t = null;
            if (stamp.material.HasProperty("_BaseMap"))
                t = stamp.material.GetTexture("_BaseMap");
            if (t == null && stamp.material.HasProperty("_MainTex"))
                t = stamp.material.GetTexture("_MainTex");
            if (t is Texture2D t2)
                return t2;
        }

        if (brush != null && brush.brushTexture != null)
            return brush.brushTexture;

        return null;
    }

    static Texture2D WhiteDecalTexture()
    {
        if (s_whiteDecal == null)
        {
            s_whiteDecal = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[16];
            for (int i = 0; i < px.Length; i++)
                px[i] = Color.white;
            s_whiteDecal.SetPixels(px);
            s_whiteDecal.Apply();
        }
        return s_whiteDecal;
    }

    // One cached material per (texture, blend) pair, so any number of stamps that share a
    // damage texture + blend mode batch into a single draw. Materials are runtime-only
    // (DontSave); BakePaintMeshes persists copies as assets.
    static Material GetDecalMaterial(Texture2D texture, PaintBlendMode blend)
    {
        var key = (texture, blend);
        if (s_decalMaterials.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        if (s_decalShader == null)
            s_decalShader = Shader.Find("RoadTool/RoadPaintDecal");

        Material material;
        if (s_decalShader != null)
        {
            material = new Material(s_decalShader);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            ApplyDecalBlend(material, blend);
        }
        else
        {
            // Decal shader not imported yet – render with a transparent Lit/Standard clone so
            // nothing is invisible; replaced automatically once the shader compiles.
            Shader fallback = Shader.Find("Universal Render Pipeline/Lit");
            if (fallback == null)
                fallback = Shader.Find("Standard");
            material = new Material(fallback);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            ConfigureLegacyTransparent(material, blend);
        }

        material.name = $"RoadPaintDecal_{(texture != null ? texture.name : "white")}_{blend}";
        material.hideFlags = HideFlags.DontSave;
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;
        s_decalMaterials[key] = material;
        return material;
    }

    static void ApplyDecalBlend(Material material, PaintBlendMode blend)
    {
        material.DisableKeyword("_BLEND_ADDITIVE");
        material.DisableKeyword("_BLEND_MULTIPLY");
        switch (blend)
        {
            case PaintBlendMode.Additive:
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.EnableKeyword("_BLEND_ADDITIVE");
                break;
            case PaintBlendMode.Multiply:
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.DstColor);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.EnableKeyword("_BLEND_MULTIPLY");
                break;
            default:
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                break;
        }
    }

    static void ConfigureLegacyTransparent(Material material, PaintBlendMode blendMode)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", blendMode == PaintBlendMode.Multiply
                ? (float)UnityEngine.Rendering.BlendMode.DstColor
                : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", blendMode == PaintBlendMode.Additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : blendMode == PaintBlendMode.Multiply
                    ? (float)UnityEngine.Rendering.BlendMode.Zero
                    : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
    }

    public static void ClearDecalMaterialCache()
    {
        foreach (var kv in s_decalMaterials)
        {
            if (kv.Value == null)
                continue;
            if (Application.isPlaying)
                Destroy(kv.Value);
            else
                DestroyImmediate(kv.Value);
        }
        s_decalMaterials.Clear();
    }

    struct SourceBounds
    {
        public int lengthAxis;
        public int widthAxis;
        public float lengthMin;
        public float lengthMax;
        public float length;
        public float widthCenter;
        public float width;
        public float bottomY;
    }

    struct SourceMeshData
    {
        public bool valid;
        public SourceBounds bounds;
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<Vector2> uvs;
        public List<int> triangles;
    }

    SourceMeshData CollectSourceMeshData()
    {
        SourceMeshData data = new SourceMeshData
        {
            vertices = new List<Vector3>(),
            normals = new List<Vector3>(),
            uvs = new List<Vector2>(),
            triangles = new List<int>()
        };

        if (roadSourceObject == null || !TryGetSourceBounds(out SourceBounds bounds))
            return data;

        MeshFilter[] filters = roadSourceObject.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            Mesh mesh = filter.sharedMesh;
            int baseVertex = data.vertices.Count;
            Vector3[] meshVertices = mesh.vertices;
            Vector3[] meshNormals = mesh.normals;
            Vector2[] meshUvs = mesh.uv;

            for (int i = 0; i < meshVertices.Length; i++)
            {
                Vector3 sourceLocal = roadSourceObject.transform.InverseTransformPoint(filter.transform.TransformPoint(meshVertices[i]));
                data.vertices.Add(sourceLocal);

                if (meshNormals != null && meshNormals.Length == meshVertices.Length)
                {
                    Vector3 worldNormal = filter.transform.TransformDirection(meshNormals[i]);
                    data.normals.Add(roadSourceObject.transform.InverseTransformDirection(worldNormal).normalized);
                }
                else
                {
                    data.normals.Add(Vector3.up);
                }

                data.uvs.Add(meshUvs != null && meshUvs.Length == meshVertices.Length ? meshUvs[i] : Vector2.zero);
            }

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] meshTriangles = mesh.GetTriangles(subMesh);
                for (int i = 0; i < meshTriangles.Length; i++)
                    data.triangles.Add(baseVertex + meshTriangles[i]);
            }
        }

        data.valid = data.vertices.Count > 0 && data.triangles.Count > 0;
        data.bounds = bounds;
        return data;
    }

    bool TryGetSourceBounds(out SourceBounds sourceBounds)
    {
        sourceBounds = default;
        if (roadSourceObject == null)
            return false;

        MeshFilter[] filters = roadSourceObject.GetComponentsInChildren<MeshFilter>();
        bool hasAny = false;
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            Vector3[] vertices = filter.sharedMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 sourceLocal = roadSourceObject.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                min = Vector3.Min(min, sourceLocal);
                max = Vector3.Max(max, sourceLocal);
                hasAny = true;
            }
        }

        if (!hasAny)
            return false;

        Vector3 size = max - min;
        int lengthAxis = Mathf.Abs(size.x) >= Mathf.Abs(size.z) ? 0 : 2;
        int widthAxis = lengthAxis == 0 ? 2 : 0;
        float lengthMin = GetAxis(min, lengthAxis);
        float lengthMax = GetAxis(max, lengthAxis);
        float widthMin = GetAxis(min, widthAxis);
        float widthMax = GetAxis(max, widthAxis);

        sourceBounds = new SourceBounds
        {
            lengthAxis = lengthAxis,
            widthAxis = widthAxis,
            lengthMin = lengthMin,
            lengthMax = lengthMax,
            length = Mathf.Max(0.001f, lengthMax - lengthMin),
            widthCenter = (widthMin + widthMax) * 0.5f,
            width = Mathf.Max(0.001f, widthMax - widthMin),
            bottomY = min.y
        };
        return true;
    }

    static RoadFrame EvaluateFrame(List<RoadSample> samples, float distance)
    {
        distance = Mathf.Clamp(distance, 0f, samples[samples.Count - 1].distance);

        for (int i = 0; i < samples.Count - 1; i++)
        {
            RoadSample a = samples[i];
            RoadSample b = samples[i + 1];
            if (distance > b.distance)
                continue;

            float t = Mathf.InverseLerp(a.distance, b.distance, distance);
            Vector3 center = Vector3.Lerp(a.center, b.center, t);
            Vector3 tangent = (b.center - a.center).normalized;
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector3.forward;

            Vector3 right = new Vector3(tangent.z, 0f, -tangent.x);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            right.Normalize();

            return new RoadFrame { center = center, tangent = tangent, right = right, distance = distance };
        }

        Vector3 endTangent = GetSampleTangent(samples, samples.Count - 1);
        Vector3 endRight = new Vector3(endTangent.z, 0f, -endTangent.x);
        if (endRight.sqrMagnitude < 0.0001f)
            endRight = Vector3.right;
        endRight.Normalize();
        return new RoadFrame { center = samples[samples.Count - 1].center, tangent = endTangent, right = endRight, distance = samples[samples.Count - 1].distance };
    }

    public bool TryProjectWorldToRoad(int roadIndex, Vector3 worldPosition, out float distance, out float sideOffset, out RoadFrame frame)
    {
        RoadPath road = ActiveRoad(roadIndex);
        List<RoadSample> samples = BuildSamples(road);
        distance = 0f;
        sideOffset = 0f;
        frame = default;

        if (samples.Count < 2)
            return false;

        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float bestSqr = float.MaxValue;
        float bestDistance = 0f;
        Vector3 bestCenter = samples[0].center;

        for (int i = 0; i < samples.Count - 1; i++)
        {
            Vector3 a = samples[i].center;
            Vector3 b = samples[i + 1].center;
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 > 0.000001f ? Mathf.Clamp01(Vector3.Dot(local - a, ab) / len2) : 0f;
            Vector3 point = Vector3.Lerp(a, b, t);
            float sqr = (local - point).sqrMagnitude;
            if (sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            bestCenter = point;
            bestDistance = Mathf.Lerp(samples[i].distance, samples[i + 1].distance, t);
        }

        frame = EvaluateFrame(samples, bestDistance);
        distance = bestDistance;
        sideOffset = Vector3.Dot(local - bestCenter, frame.right);
        return Mathf.Abs(sideOffset) <= road.width * 0.75f + road.shoulderWidth;
    }

    public bool TryEvaluateRoadFrame(int roadIndex, float distance, out RoadFrame frame)
    {
        RoadPath road = ActiveRoad(roadIndex);
        List<RoadSample> samples = BuildSamples(road);
        frame = default;
        if (samples.Count < 2)
            return false;

        frame = EvaluateFrame(samples, distance);
        return true;
    }

    // Rebuilds ONLY the active road's paint overlay (not every road's road/overlay/paint mesh).
    // Used while dragging the brush so painting stays smooth; a full RebuildAllMeshes runs only
    // on the explicit "Rebuild All" button.
    public void RebuildPaintOnly(int roadIndex)
    {
        EnsureDefaultRoad();
        if (roads.Count == 0)
            return;

        roadIndex = Mathf.Clamp(roadIndex, 0, roads.Count - 1);
        RoadPath road = roads[roadIndex];
        List<RoadSample> samples = BuildSamples(road);
        if (samples == null || samples.Count < 2)
        {
            if (road.paintObject != null)
                road.paintObject.SetActive(false);
            return;
        }

        RebuildPaintOverlayMesh(road, roadIndex, samples);
    }

    public void AddPaintStampFast(int roadIndex, PaintStamp stamp)
    {
        RoadPath road = ActiveRoad(roadIndex);
        if (road.paintStamps == null)
            road.paintStamps = new List<PaintStamp>();

        road.paintStamps.Add(stamp);
        RebuildPaintOnly(roadIndex);
    }

    public void AddPaintStamp(int roadIndex, PaintStamp stamp)
    {
        RoadPath road = ActiveRoad(roadIndex);
        if (road.paintStamps == null)
            road.paintStamps = new List<PaintStamp>();

        road.paintStamps.Add(stamp);
        RebuildPaintOnly(roadIndex);
    }

    public int RemovePaintStampsNear(int roadIndex, float distance, float sideOffset, float radius)
    {
        RoadPath road = ActiveRoad(roadIndex);
        if (road.paintStamps == null || road.paintStamps.Count == 0)
            return 0;

        int removed = 0;
        float sqrRadius = radius * radius;
        for (int i = road.paintStamps.Count - 1; i >= 0; i--)
        {
            PaintStamp stamp = road.paintStamps[i];
            float dx = stamp.distance - distance;
            float dy = stamp.sideOffset - sideOffset;
            if (dx * dx + dy * dy > sqrRadius)
                continue;

            road.paintStamps.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
            RebuildPaintOnly(roadIndex);
        return removed;
    }

    public void ClearPaint(int roadIndex)
    {
        RoadPath road = ActiveRoad(roadIndex);
        if (road.paintStamps == null)
            road.paintStamps = new List<PaintStamp>();
        else
            road.paintStamps.Clear();

        RebuildPaintOnly(roadIndex);
    }

    static Vector3 TransformSourceNormal(Vector3 sourceNormal, RoadFrame frame, SourceBounds bounds)
    {
        Vector3 forwardComponent = frame.tangent * GetAxis(sourceNormal, bounds.lengthAxis);
        Vector3 sideComponent = frame.right * GetAxis(sourceNormal, bounds.widthAxis);
        Vector3 upComponent = Vector3.up * sourceNormal.y;
        Vector3 normal = forwardComponent + sideComponent + upComponent;
        return normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
    }

    static Vector2 TransformRoadUv(RoadPath road, Vector2 uv)
    {
        if (road.swapUv)
            uv = new Vector2(uv.y, uv.x);

        if (road.flipU)
            uv.x = 1f - uv.x;
        if (road.flipV)
            uv.y = 1f - uv.y;

        uv = Vector2.Scale(uv, road.uvScale) + road.uvOffset;
        return uv;
    }

    static float GetAxis(Vector3 value, int axis)
    {
        if (axis == 0)
            return value.x;
        if (axis == 1)
            return value.y;
        return value.z;
    }

    public void AddMaterialVariantsToRoad(int roadIndex, IEnumerable<Material> materials)
    {
        RoadPath road = ActiveRoad(roadIndex);
        AddUniqueMaterials(road, materials);
        RebuildAllMeshes();
    }

    static void AddUniqueMaterials(RoadPath road, IEnumerable<Material> materials)
    {
        if (road.materialVariants == null)
            road.materialVariants = new List<Material>();

        foreach (Material material in materials)
        {
            if (material == null)
                continue;

            if (road.roadMaterial == null)
            {
                road.roadMaterial = material;
                continue;
            }

            if (road.roadMaterial == material || road.materialVariants.Contains(material))
                continue;

            road.materialVariants.Add(material);
        }
    }

    Material[] GetRoadMaterials(RoadPath road)
    {
        List<Material> materials = new List<Material>();
        if (road.roadMaterial != null)
            materials.Add(road.roadMaterial);

        if (road.materialVariants != null)
        {
            foreach (Material material in road.materialVariants)
            {
                if (material != null && !materials.Contains(material))
                    materials.Add(material);
            }
        }

        return materials.ToArray();
    }

    int GetRoadMaterialCount(RoadPath road)
    {
        return GetRoadMaterials(road).Length;
    }

    static List<int>[] CreateTriangleBuckets(int count)
    {
        List<int>[] buckets = new List<int>[Mathf.Max(1, count)];
        for (int i = 0; i < buckets.Length; i++)
            buckets[i] = new List<int>();
        return buckets;
    }

    static void ApplySubmeshTriangles(Mesh mesh, List<int>[] trianglesByMaterial)
    {
        mesh.subMeshCount = trianglesByMaterial.Length;
        for (int i = 0; i < trianglesByMaterial.Length; i++)
            mesh.SetTriangles(trianglesByMaterial[i], i);
    }

    static int GetMaterialSlotForDistance(RoadPath road, float distance, int materialCount)
    {
        if (materialCount <= 1 || !road.randomizeMaterials)
            return 0;

        float run = Mathf.Max(1f, road.materialRunMeters);
        int chunk = Mathf.FloorToInt(distance / run);
        int hash = HashInts(road.materialSeed, chunk, 31);
        return Mathf.Abs(hash) % materialCount;
    }

    static Vector3 BuildDeformedVertex(RoadPath road, Vector3 center, Vector3 right, float sideOffset, float distance, float normalizedSide, float heightOffset)
    {
        if (!road.enableDeformation || IsFlatSection(road, distance))
            return center + right * sideOffset + Vector3.up * heightOffset;

        float scale = Mathf.Max(0.1f, road.deformationScale);
        float seed = road.deformationSeed * 0.137f;
        float edgeStrength = Mathf.Pow(Mathf.Abs(normalizedSide), 1.7f);
        float edgeNoise = (Mathf.PerlinNoise(distance / scale + seed, 18.73f + seed) - 0.5f) * 2f;
        float surfaceNoise = (Mathf.PerlinNoise(distance / scale + seed, normalizedSide * 2.3f + seed) - 0.5f) * 2f;

        float deformedSide = sideOffset + Mathf.Sign(normalizedSide) * edgeNoise * road.edgeWidthNoise * edgeStrength;
        float deformedHeight = heightOffset + surfaceNoise * road.surfaceHeightNoise * Mathf.Lerp(0.35f, 1f, 1f - edgeStrength);
        return center + right * deformedSide + Vector3.up * deformedHeight;
    }

    static bool IsFlatSection(RoadPath road, float distance)
    {
        if (road.flatSectionChance <= 0f)
            return false;
        if (road.flatSectionChance >= 1f)
            return true;

        int section = Mathf.FloorToInt(distance / Mathf.Max(1f, road.flatSectionMeters));
        float value = Hash01(road.deformationSeed, section, 97);
        return value < road.flatSectionChance;
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

    bool TryGetSourceWidth(out float width)
    {
        width = 0f;
        if (roadSourceObject == null)
            return false;

        if (TryGetSourceBounds(out SourceBounds sourceBounds))
        {
            width = sourceBounds.width;
            return width > 0.001f;
        }

        Renderer[] renderers = roadSourceObject.GetComponentsInChildren<Renderer>();
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            MeshFilter[] filters = roadSourceObject.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                if (filter == null || filter.sharedMesh == null)
                    continue;

                Bounds meshBounds = filter.sharedMesh.bounds;
                if (!hasBounds)
                {
                    bounds = meshBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(meshBounds);
                }
            }
        }

        if (!hasBounds)
            return false;

        float x = Mathf.Abs(bounds.size.x);
        float z = Mathf.Abs(bounds.size.z);
        if (x <= 0.001f && z <= 0.001f)
            return false;

        if (x <= 0.001f)
            width = z;
        else if (z <= 0.001f)
            width = x;
        else
            width = Mathf.Min(x, z);

        return width > 0.001f;
    }

    List<RoadSample> BuildSamples(RoadPath road)
    {
        List<Vector3> centers = road.curveMode == CurveMode.Straight
            ? BuildLinearCenters(road)
            : BuildCatmullCenters(road);

        if (road.heightMode == HeightMode.SnapToTerrain)
        {
            for (int i = 0; i < centers.Count; i++)
                centers[i] = new Vector3(centers[i].x, SampleTerrainLocalY(centers[i]) + road.roadYOffset, centers[i].z);
        }

        SmoothSampleHeights(centers, road.smoothGradeWindow);

        List<RoadSample> samples = new List<RoadSample>(centers.Count);
        float distance = 0f;
        for (int i = 0; i < centers.Count; i++)
        {
            if (i > 0)
                distance += Vector3.Distance(centers[i - 1], centers[i]);

            samples.Add(new RoadSample { center = centers[i], distance = distance });
        }

        return samples;
    }

    List<Vector3> BuildLinearCenters(RoadPath road)
    {
        List<Vector3> centers = new List<Vector3>();
        for (int i = 0; i < road.points.Count - 1; i++)
            AppendSegmentSamples(centers, road.points[i], road.points[i + 1], road.samplesPerMeter);
        centers.Add(road.points[road.points.Count - 1]);
        return centers;
    }

    List<Vector3> BuildCatmullCenters(RoadPath road)
    {
        List<Vector3> centers = new List<Vector3>();
        for (int i = 0; i < road.points.Count - 1; i++)
        {
            Vector3 p0 = road.points[Mathf.Max(0, i - 1)];
            Vector3 p1 = road.points[i];
            Vector3 p2 = road.points[i + 1];
            Vector3 p3 = road.points[Mathf.Min(road.points.Count - 1, i + 2)];
            int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(p1, p2) * road.samplesPerMeter));

            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                centers.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        centers.Add(road.points[road.points.Count - 1]);
        return centers;
    }

    static void AppendSegmentSamples(List<Vector3> centers, Vector3 a, Vector3 b, float samplesPerMeter)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) * samplesPerMeter));
        for (int s = 0; s < steps; s++)
            centers.Add(Vector3.Lerp(a, b, s / (float)steps));
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    static Vector3 GetSampleTangent(List<RoadSample> samples, int index)
    {
        if (index == 0)
            return (samples[1].center - samples[0].center).normalized;
        if (index == samples.Count - 1)
            return (samples[index].center - samples[index - 1].center).normalized;
        return (samples[index + 1].center - samples[index - 1].center).normalized;
    }

    static void SmoothSampleHeights(List<Vector3> centers, int window)
    {
        if (window <= 1 || centers.Count < 3)
            return;

        int half = window / 2;
        float[] heights = new float[centers.Count];
        for (int i = 0; i < centers.Count; i++)
        {
            float sum = 0f;
            int count = 0;
            for (int j = -half; j <= half; j++)
            {
                int k = i + j;
                if (k < 0 || k >= centers.Count)
                    continue;
                sum += centers[k].y;
                count++;
            }
            heights[i] = sum / Mathf.Max(1, count);
        }

        for (int i = 0; i < centers.Count; i++)
            centers[i] = new Vector3(centers[i].x, heights[i], centers[i].z);
    }

    float SampleTerrainLocalY(Vector3 local)
    {
        Terrain target = terrain != null ? terrain : Terrain.activeTerrain;
        if (target == null)
            return local.y;

        Vector3 world = transform.TransformPoint(local);
        float worldY = target.SampleHeight(world) + target.transform.position.y;
        return transform.InverseTransformPoint(new Vector3(world.x, worldY, world.z)).y;
    }

    void ApplyTerrainHeights(Terrain target)
    {
        TerrainData data = target.terrainData;
        Vector3 terrainPos = target.transform.position;
        Vector3 size = data.size;
        int res = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, res, res);

        foreach (RoadPath road in roads)
        {
            if (!road.enabled || road.points.Count < 2)
                continue;

            List<RoadSample> samples = BuildSamples(road);
            float corridor = road.width * 0.5f + road.shoulderWidth;
            for (int i = 0; i < samples.Count - 1; i++)
            {
                Vector3 a = transform.TransformPoint(samples[i].center);
                Vector3 b = transform.TransformPoint(samples[i + 1].center);
                ApplyHeightSegment(heights, res, size, terrainPos, road, a, b, corridor);
            }
        }

        data.SetHeights(0, 0, heights);
    }

    void ApplyHeightSegment(float[,] heights, int res, Vector3 size, Vector3 terrainPos, RoadPath road, Vector3 a, Vector3 b, float corridor)
    {
        float cellX = (res - 1) / size.x;
        float cellZ = (res - 1) / size.z;
        float minX = Mathf.Min(a.x, b.x) - corridor - terrainPos.x;
        float maxX = Mathf.Max(a.x, b.x) + corridor - terrainPos.x;
        float minZ = Mathf.Min(a.z, b.z) - corridor - terrainPos.z;
        float maxZ = Mathf.Max(a.z, b.z) + corridor - terrainPos.z;

        int ix0 = Mathf.Clamp(Mathf.FloorToInt(minX * cellX), 0, res - 1);
        int ix1 = Mathf.Clamp(Mathf.CeilToInt(maxX * cellX), 0, res - 1);
        int iz0 = Mathf.Clamp(Mathf.FloorToInt(minZ * cellZ), 0, res - 1);
        int iz1 = Mathf.Clamp(Mathf.CeilToInt(maxZ * cellZ), 0, res - 1);

        for (int z = iz0; z <= iz1; z++)
        {
            float wz = terrainPos.z + z / (float)(res - 1) * size.z;
            for (int x = ix0; x <= ix1; x++)
            {
                float wx = terrainPos.x + x / (float)(res - 1) * size.x;
                float t;
                float d = PointSegmentDistance(wx, wz, a.x, a.z, b.x, b.z, out t);
                if (d > corridor)
                    continue;

                float roadHeight01 = Mathf.Clamp01((Mathf.Lerp(a.y, b.y, t) + terrainHeightOffset - terrainPos.y) / size.y);
                float halfWidth = road.width * 0.5f;
                float blend = d <= halfWidth ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (d - halfWidth) / Mathf.Max(0.001f, road.shoulderWidth));
                heights[z, x] = Mathf.Lerp(heights[z, x], roadHeight01, blend);
            }
        }
    }

    void PaintTerrainLayers(Terrain target)
    {
        TerrainData data = target.terrainData;
        List<TerrainLayer> layers = new List<TerrainLayer>(data.terrainLayers);
        int roadIndex = EnsureTerrainLayer(layers, roadLayer);
        int shoulderIndex = EnsureTerrainLayer(layers, shoulderLayer);
        if (roadIndex < 0 && shoulderIndex < 0)
            return;

        data.terrainLayers = layers.ToArray();
        int aRes = data.alphamapResolution;
        float[,,] maps = data.GetAlphamaps(0, 0, aRes, aRes);
        Vector3 terrainPos = target.transform.position;
        Vector3 size = data.size;

        foreach (RoadPath road in roads)
        {
            if (!road.enabled || road.points.Count < 2)
                continue;

            List<RoadSample> samples = BuildSamples(road);
            float corridor = road.width * 0.5f + road.shoulderWidth;
            for (int i = 0; i < samples.Count - 1; i++)
            {
                Vector3 a = transform.TransformPoint(samples[i].center);
                Vector3 b = transform.TransformPoint(samples[i + 1].center);
                PaintLayerSegment(maps, aRes, layers.Count, size, terrainPos, road, a, b, corridor, roadIndex, shoulderIndex);
            }
        }

        data.SetAlphamaps(0, 0, maps);
    }

    static int EnsureTerrainLayer(List<TerrainLayer> layers, TerrainLayer layer)
    {
        if (layer == null)
            return -1;

        int index = layers.IndexOf(layer);
        if (index >= 0)
            return index;

        layers.Add(layer);
        return layers.Count - 1;
    }

    void PaintLayerSegment(float[,,] maps, int res, int layerCount, Vector3 size, Vector3 terrainPos, RoadPath road, Vector3 a, Vector3 b, float corridor, int roadLayerIndex, int shoulderLayerIndex)
    {
        float cellX = (res - 1) / size.x;
        float cellZ = (res - 1) / size.z;
        int ix0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, b.x) - corridor - terrainPos.x) * cellX), 0, res - 1);
        int ix1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x, b.x) + corridor - terrainPos.x) * cellX), 0, res - 1);
        int iz0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z, b.z) - corridor - terrainPos.z) * cellZ), 0, res - 1);
        int iz1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.z, b.z) + corridor - terrainPos.z) * cellZ), 0, res - 1);

        for (int z = iz0; z <= iz1; z++)
        {
            float wz = terrainPos.z + z / (float)(res - 1) * size.z;
            for (int x = ix0; x <= ix1; x++)
            {
                float wx = terrainPos.x + x / (float)(res - 1) * size.x;
                float t;
                float d = PointSegmentDistance(wx, wz, a.x, a.z, b.x, b.z, out t);
                if (d > corridor)
                    continue;

                float halfWidth = road.width * 0.5f;
                int paintIndex = d <= halfWidth ? roadLayerIndex : shoulderLayerIndex;
                if (paintIndex < 0)
                    continue;

                float strength = d <= halfWidth ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (d - halfWidth) / Mathf.Max(0.001f, road.shoulderWidth));
                NormalizePaintCell(maps, z, x, layerCount, paintIndex, strength);
            }
        }
    }

    static void NormalizePaintCell(float[,,] maps, int z, int x, int layerCount, int paintIndex, float strength)
    {
        float keep = 1f - Mathf.Clamp01(strength);
        float sum = 0f;
        for (int layer = 0; layer < layerCount; layer++)
        {
            maps[z, x, layer] = layer == paintIndex ? Mathf.Max(maps[z, x, layer], strength) : maps[z, x, layer] * keep;
            sum += maps[z, x, layer];
        }

        if (sum <= 0.0001f)
            return;

        for (int layer = 0; layer < layerCount; layer++)
            maps[z, x, layer] /= sum;
    }

    void ClearTreesInRoadCorridors(Terrain target)
    {
        TerrainData data = target.terrainData;
        if (data.treeInstanceCount == 0)
            return;

        Vector3 terrainPos = target.transform.position;
        Vector3 size = data.size;
        TreeInstance[] source = data.treeInstances;
        List<TreeInstance> kept = new List<TreeInstance>(source.Length);

        foreach (TreeInstance tree in source)
        {
            Vector3 world = terrainPos + Vector3.Scale(tree.position, size);
            if (!IsInAnyRoadCorridor(world))
                kept.Add(tree);
        }

        data.SetTreeInstances(kept.ToArray(), true);
    }

    bool IsInAnyRoadCorridor(Vector3 world)
    {
        foreach (RoadPath road in roads)
        {
            if (!road.enabled || road.points.Count < 2)
                continue;

            List<RoadSample> samples = BuildSamples(road);
            float corridor = road.width * 0.5f + road.shoulderWidth + treeClearance;
            for (int i = 0; i < samples.Count - 1; i++)
            {
                Vector3 a = transform.TransformPoint(samples[i].center);
                Vector3 b = transform.TransformPoint(samples[i + 1].center);
                float t;
                if (PointSegmentDistance(world.x, world.z, a.x, a.z, b.x, b.z, out t) <= corridor)
                    return true;
            }
        }

        return false;
    }

    static float PointSegmentDistance(float px, float pz, float ax, float az, float bx, float bz, out float t)
    {
        float abx = bx - ax;
        float abz = bz - az;
        float len2 = abx * abx + abz * abz;
        t = len2 > 0.000001f ? Mathf.Clamp01(((px - ax) * abx + (pz - az) * abz) / len2) : 0f;
        float cx = ax + abx * t;
        float cz = az + abz * t;
        float dx = px - cx;
        float dz = pz - cz;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static void DestroyMesh(Mesh mesh)
    {
        if (mesh == null)
            return;

        if (Application.isPlaying)
            Destroy(mesh);
        else
            DestroyImmediate(mesh);
    }
}
