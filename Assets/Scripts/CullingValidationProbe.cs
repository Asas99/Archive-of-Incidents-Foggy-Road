using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

/// <summary>Temporary, unsaved fixed-view culling measurements; restores gameplay and camera state.</summary>
[DefaultExecutionOrder(21000)]
public sealed class CullingValidationProbe : MonoBehaviour
{
    [Serializable] public sealed class View { public string name; public Vector3 position; public float yaw; }
    [Serializable] public sealed class Views { public List<View> items = new List<View>(); }
    [Serializable] public sealed class Sample
    {
        public string view, mode; public Vector3 position; public float yaw, medianMs, p95Ms, triangles, batches;
        public int frames, grassVisible, grassOccluded, grassFrustumRejected; public bool gpuCountersValid, rearBoundsRejected;
        public float grassDensityFactor = 1, treeDensityFactor = 1;
        public int treeInstances;
        public float gpuMedianMs, mainThreadMedianMs;
        public bool gpuTimingAvailable;
    }
    [Serializable] public sealed class Report
    {
        public string phase, environment = "Unity Editor Play Mode; fixed 1280x720 camera; uncapped; native shadows/reflections retained", gpu;
        public int width, height; public List<Sample> samples = new List<Sample>();
    }
    Camera camera; SceneOptimizationController controller; FarGrassRenderer[] grass;
    MonoBehaviour movement; bool oldMovement, oldReady, oldOcclusion, oldBackground, initialized, restored;
    int oldTarget, oldVsync; Vector3 originalPosition, pose; Quaternion originalRotation, rotation;
    ProfilerRecorder triangles, batches, gpuTime, mainTime; Report report; Views views; string folder;
    sealed class DensityTerrain
    {
        public Terrain terrain; public TerrainData original, temporary;
        public TreeInstance[] full, threeQuarters, half; public float density;
    }
    readonly List<DensityTerrain> densityTerrains = new List<DensityTerrain>();
    readonly Dictionary<SceneOptimizationProfile, SceneOptimizationProfile> densityProfiles = new Dictionary<SceneOptimizationProfile, SceneOptimizationProfile>();
    SceneOptimizationProfile originalProfile;
    SceneOptimizationProfile[] originalGrassProfiles;
    float grassFactor = 1, treeFactor = 1, appliedTreeFactor = 1;
    public void Begin(Camera source, string phase)
    {
        camera = source; controller = FindFirstObjectByType<SceneOptimizationController>();
        grass = FindObjectsByType<FarGrassRenderer>(FindObjectsSortMode.None);
        movement = camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "KarakterHareket");
        oldMovement = movement && movement.enabled; oldReady = controller.occlusionReady; oldOcclusion = camera.useOcclusionCulling;
        originalPosition = camera.transform.position; originalRotation = camera.transform.rotation; pose = originalPosition; rotation = originalRotation;
        oldTarget = Application.targetFrameRate; oldVsync = QualitySettings.vSyncCount; oldBackground = Application.runInBackground;
        initialized = true; if (movement) movement.enabled = false;
        Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0; Application.runInBackground = true;
        triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
        batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
        try { gpuTime = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1); } catch { gpuTime = default; }
        try { mainTime = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1); } catch { mainTime = default; }
        folder = "Library/CullingValidation_" + phase; Directory.CreateDirectory(folder);
        report = new Report { phase = phase, gpu = SystemInfo.graphicsDeviceName, width = Screen.width, height = Screen.height };
        const string path = "Library/CullingValidationViews.json";
        if (phase == "Before" || !File.Exists(path)) { views = CreateViews(); File.WriteAllText(path, JsonUtility.ToJson(views, true)); }
        else views = JsonUtility.FromJson<Views>(File.ReadAllText(path));
        if (phase.StartsWith("Density", StringComparison.Ordinal)) PrepareDensity();
        StartCoroutine(Measure());
    }
    void PrepareDensity()
    {
        // All tree edits are made on unsaved clones; source TerrainData and colliders stay intact.
        originalProfile = controller.profile;
        originalGrassProfiles = grass.Select(g => g.profile).ToArray();
        foreach (var profile in originalGrassProfiles.Concat(new[] { originalProfile }).Where(p => p).Distinct())
        {
            var copy = Instantiate(profile); copy.hideFlags = HideFlags.HideAndDontSave;
            densityProfiles.Add(profile, copy);
        }
        controller.profile = densityProfiles[originalProfile];
        for (int i = 0; i < grass.Length; i++) if (originalGrassProfiles[i]) grass[i].profile = densityProfiles[originalGrassProfiles[i]];
        foreach (var terrain in FindObjectsByType<Terrain>(FindObjectsSortMode.None).Where(t => t.terrainData))
        {
            var item = new DensityTerrain { terrain = terrain, original = terrain.terrainData, density = terrain.detailObjectDensity };
            if (terrain.drawHeightmap && item.original.treeInstanceCount > 0)
            {
                item.full = item.original.treeInstances;
                int[] order = Enumerable.Range(0, item.full.Length).OrderBy(TreeRank).ToArray();
                item.threeQuarters = order.Take((int)(item.full.Length * .75f)).Select(i => item.full[i]).ToArray();
                item.half = order.Take(item.full.Length / 2).Select(i => item.full[i]).ToArray();
                item.temporary = Instantiate(item.original); item.temporary.hideFlags = HideFlags.HideAndDontSave;
                terrain.terrainData = item.temporary;
            }
            densityTerrains.Add(item);
        }
        string[] names = report.phase == "DensityConfirm" ? new[] { "RoadForest_0", "RoadForest_90" }
            : new[] { "RoadForest_0", "RoadForest_90", "StationExterior_180", "StationExterior_0" };
        views.items = names.Select(name => views.items.Single(v => v.name == name)).ToList();
        report.environment += "; density uses temporary TerrainData/profile clones; tree colliders and culling settings unchanged";
    }
    static uint TreeRank(int index)
    {
        unchecked { uint n = (uint)index + 0x9e3779b9u; n ^= n >> 16; n *= 0x7feb352du; n ^= n >> 15; n *= 0x846ca68bu; return n ^ (n >> 16); }
    }
    void ApplyDensity(string mode)
    {
        grassFactor = mode.StartsWith("Grass") || mode.StartsWith("Both") ? (mode.EndsWith("75") ? .75f : .5f) : 1;
        treeFactor = mode.StartsWith("Trees") || mode.StartsWith("Both") ? (mode.EndsWith("75") ? .75f : .5f) : 1;
        foreach (var pair in densityProfiles) pair.Value.farGrassDensity = pair.Key.farGrassDensity * grassFactor;
        foreach (var item in densityTerrains)
        {
            if (!item.terrain) continue;
            item.terrain.detailObjectDensity = item.density * grassFactor;
            if (item.temporary && treeFactor != appliedTreeFactor)
                item.temporary.SetTreeInstances(treeFactor == 1 ? item.full : treeFactor == .75f ? item.threeQuarters : item.half, false);
        }
        appliedTreeFactor = treeFactor;
    }
    Views CreateViews()
    {
        var nearby = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.enabled && !r.GetComponentInParent<Rigidbody>() &&
            (r.name.ToLowerInvariant().Contains("duvar") || r.name.ToLowerInvariant().Contains("wall")) && Vector3.Distance(r.bounds.center, originalPosition) < 90 && r.bounds.size.magnitude < 100).ToArray();
        Bounds station = new Bounds(originalPosition, Vector3.one * 8);
        if (nearby.Length > 0) { station = nearby[0].bounds; foreach (var r in nearby.Skip(1)) station.Encapsulate(r.bounds); }
        Vector3 interior = station.center; interior.y = originalPosition.y;
        Vector3 back = station.center + Vector3.forward * (station.extents.z + 6); back.y = GroundHeight(back);
        Vector3 road = originalPosition + camera.transform.forward * 45 + camera.transform.right * 10; road.y = GroundHeight(road);
        var result = new Views(); Vector3[] points = { originalPosition, interior, back, road };
        string[] labels = { "StationExterior", "StationInterior", "BuildingBack", "RoadForest" };
        for (int p = 0; p < points.Length; p++) for (int yaw = 0; yaw < 360; yaw += 90)
            result.items.Add(new View { name = labels[p] + "_" + yaw, position = points[p], yaw = yaw });
        return result;
    }
    float GroundHeight(Vector3 position)
    {
        foreach (var terrain in Terrain.activeTerrains.Where(t => t.drawHeightmap))
        {
            Vector3 local = position - terrain.transform.position; Vector3 size = terrain.terrainData.size;
            if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z) return terrain.SampleHeight(position) + terrain.transform.position.y + 1.7f;
        }
        return originalPosition.y;
    }
    IEnumerator Measure()
    {
        yield return new WaitForSecondsRealtime(8);
        report.width = Screen.width; report.height = Screen.height;
        foreach (var point in views.items)
        {
            pose = point.position; rotation = Quaternion.Euler(0, point.yaw, 0);
            bool density = report.phase.StartsWith("Density", StringComparison.Ordinal);
            bool confirm = report.phase == "DensityConfirm";
            string[] modes = confirm ? new[] { "Baseline", "Grass50", "Trees50", "Both50", "BaselineRepeat" }
                : density ? new[] { "Baseline", "Grass75", "Grass50", "Trees75", "Trees50", "Both75", "Both50", "BaselineRepeat" }
                : report.phase == "Before" ? new[] { "Current" } : new[] { "Enabled", "Disabled", "EnabledRepeat" };
            foreach (string mode in modes)
            {
                if (density) ApplyDensity(mode);
                else { controller.occlusionReady = mode == "Current" ? oldReady : mode != "Disabled"; camera.useOcclusionCulling = controller.occlusionReady; }
                File.WriteAllText(folder + "/status.txt", point.name + " / " + mode);
                yield return new WaitForSecondsRealtime(confirm ? 5f : density ? 3f : 1.5f);
                foreach (var g in grass) g.RequestGpuCounters();
                double until = Time.realtimeSinceStartupAsDouble + (confirm ? 5 : density ? 3 : 2);
                var times = new List<float>(); var gpuTimes = new List<float>(); var mainTimes = new List<float>(); double tris = 0, calls = 0;
                while (Time.realtimeSinceStartupAsDouble < until)
                {
                    yield return null; times.Add(Time.unscaledDeltaTime * 1000);
                    if (triangles.Valid) tris += triangles.LastValue; if (batches.Valid) calls += batches.LastValue;
                    if (gpuTime.Valid && gpuTime.LastValue > 0) gpuTimes.Add(gpuTime.LastValue / 1000000f);
                    if (mainTime.Valid && mainTime.LastValue > 0) mainTimes.Add(mainTime.LastValue / 1000000f);
                }
                times.Sort(); int n = times.Count;
                gpuTimes.Sort(); mainTimes.Sort();
                var sample = new Sample { view = point.name, mode = mode, position = pose, yaw = point.yaw, frames = n,
                    medianMs = times[n / 2], p95Ms = times[Math.Min(n - 1, (int)(n * .95f))], triangles = (float)(tris / n), batches = (float)(calls / n),
                    grassVisible = grass.Sum(g => g.gpuActive ? g.gpuVisible : g.visibleInstances), grassOccluded = grass.Sum(g => g.gpuOccluded),
                    grassFrustumRejected = grass.Sum(g => g.gpuActive ? g.gpuFrustumRejected : g.frustumRejected),
                    gpuCountersValid = grass.All(g => !g.gpuActive || g.gpuCountersValid),
                    grassDensityFactor = grassFactor, treeDensityFactor = treeFactor,
                    treeInstances = densityTerrains.Where(t => t.terrain && t.temporary).Sum(t => t.terrain.terrainData.treeInstanceCount),
                    gpuTimingAvailable = gpuTimes.Count > 0, gpuMedianMs = gpuTimes.Count > 0 ? gpuTimes[gpuTimes.Count / 2] : 0,
                    mainThreadMedianMs = mainTimes.Count > 0 ? mainTimes[mainTimes.Count / 2] : 0,
                    rearBoundsRejected = !GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), new Bounds(pose - camera.transform.forward * 100, Vector3.one * 2)) };
                report.samples.Add(sample); File.WriteAllText(folder + "/report.json", JsonUtility.ToJson(report, true));
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder + "/" + point.name + "_" + mode + ".png"));
                yield return null;
            }
        }
        Restore(); File.WriteAllText(folder + "/status.txt", "COMPLETE; original settings restored"); Destroy(gameObject);
    }
    void LateUpdate() { if (initialized && !restored) camera.transform.SetPositionAndRotation(pose, rotation); }
    void Restore()
    {
        if (!initialized || restored) return;
        foreach (var item in densityTerrains)
        {
            if (item.terrain) { item.terrain.terrainData = item.original; item.terrain.detailObjectDensity = item.density; }
            if (item.temporary) { if (Application.isPlaying) Destroy(item.temporary); else DestroyImmediate(item.temporary); }
        }
        if (originalProfile && controller) controller.profile = originalProfile;
        if (originalGrassProfiles != null)
            for (int i = 0; i < grass.Length; i++) if (grass[i]) grass[i].profile = originalGrassProfiles[i];
        foreach (var copy in densityProfiles.Values) if (copy) { if (Application.isPlaying) Destroy(copy); else DestroyImmediate(copy); }
        if (controller) controller.occlusionReady = oldReady;
        if (camera) { camera.useOcclusionCulling = oldOcclusion; camera.transform.SetPositionAndRotation(originalPosition, originalRotation); }
        if (movement) movement.enabled = oldMovement;
        Application.targetFrameRate = oldTarget; QualitySettings.vSyncCount = oldVsync; Application.runInBackground = oldBackground;
        triangles.Dispose(); batches.Dispose(); gpuTime.Dispose(); mainTime.Dispose(); restored = true;
    }
    void OnDisable() { Restore(); }
}
