using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Temporary fixed-view A/B measurements. Restores all changed settings.</summary>
[DefaultExecutionOrder(20000)]
public sealed class FrameCostProbe : MonoBehaviour
{
    [Serializable] public sealed class Sample
    {
        public string stage;
        public int frames;
        public float meanMs, medianMs, p95Ms, meanMainThreadMs, meanRenderThreadMs, meanGpuMs;
        public float triangles, batches;
        public string view;
    }
    [Serializable] public sealed class Report
    {
        public string environment = "Unity Editor Play Mode; fixed camera; uncapped; temporary A/B changes";
        public string cpu, gpu, pipeline, position, rotation, grassStatus, inventory;
        public int width, height;
        public bool grassAcceptsMountainCamera;
        public List<Sample> samples = new List<Sample>();
    }
    public string CurrentStage { get; private set; }
    public string CaptureLabel { get; private set; }
    public bool ProfilerCaptureComplete { get; set; }
    Camera source, mountain;
    DistantMountainBackdrop backdrop;
    UniversalAdditionalCameraData sourceData;
    UniversalRenderPipelineAsset pipeline;
    FarGrassRenderer[] grass;
    GasStationPlanarGlass[] glass;
    Terrain[] terrains;
    bool[] grassEnabled, glassEnabled;
    float[] detailDistance, detailDensity;
    float[] treeDistance;
    MeshRenderer[] heavyMeshes;
    bool[] heavyEnabled;
    ShadowCastingMode[] heavyShadows;
    string phaseName, viewName = "Current";
    Vector3 originalPosition;
    Quaternion originalRotation;
    MonoBehaviour movement;
    bool movementEnabled, originalPost, originalBackdrop, originalBackground;
    int mountainMask, oldTargetRate, oldVsync;
    CameraClearFlags clearFlags;
    float oldShadowDistance, oldScale;
    bool oldBackground;
    Vector3 position;
    Quaternion rotation;
    bool initialized, restored;
    int originalMountainRenderer = -1;
    ProfilerRecorder mainRecorder, renderRecorder, gpuRecorder, triangleRecorder, batchRecorder;
    string outputPath;
    Report report;

    public void Begin(Camera camera, string phase)
    {
        source = camera;
        phaseName = phase;
        position = camera.transform.position;
        rotation = camera.transform.rotation;
        originalPosition = position; originalRotation = rotation;
        sourceData = source.GetUniversalAdditionalCameraData();
        backdrop = source.GetComponentInChildren<DistantMountainBackdrop>();
        mountain = backdrop != null ? backdrop.GetComponent<Camera>() : null;
        originalBackdrop = backdrop != null && backdrop.enabled;
        originalBackground = mountain != null && mountain.enabled;
        mountainMask = mountain != null ? mountain.cullingMask : 0;
        clearFlags = source.clearFlags;
        originalPost = sourceData.renderPostProcessing;
        pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        if (mountain != null)
        {
            var list = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pipeline);
            var mountainData = mountain.GetUniversalAdditionalCameraData();
            for (int i = 0; i < list.Length; i++)
                if (ReferenceEquals(pipeline.GetRenderer(i), mountainData.scriptableRenderer)) originalMountainRenderer = i;
            if (phase == "Before")
                for (int i = 0; i < list.Length; i++)
                    if (ReferenceEquals(pipeline.GetRenderer(i), sourceData.scriptableRenderer)) { mountainData.SetRenderer(i); break; }
        }
        FarGrassRenderer.ProfileIncludeMountainCameras = phase == "Before";
        oldShadowDistance = pipeline.shadowDistance;
        oldScale = pipeline.renderScale;
        grass = FindObjectsByType<FarGrassRenderer>(FindObjectsSortMode.None);
        glass = FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None);
        terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        grassEnabled = grass.Select(g => g.enabled).ToArray();
        glassEnabled = glass.Select(g => g.enabled).ToArray();
        detailDistance = terrains.Select(t => t.detailObjectDistance).ToArray();
        detailDensity = terrains.Select(t => t.detailObjectDensity).ToArray();
        treeDistance = terrains.Select(t => t.treeDistance).ToArray();
        heavyMeshes = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.scene == source.gameObject.scene &&
            r.gameObject.layer != LayerMask.NameToLayer("DistantMountains") && r.GetComponent<MeshFilter>()?.sharedMesh != null &&
            TriangleCount(r.GetComponent<MeshFilter>().sharedMesh) >= 200000 && Vector3.Distance(r.bounds.center, position) < 120).ToArray();
        heavyEnabled = heavyMeshes.Select(r => r.enabled).ToArray(); heavyShadows = heavyMeshes.Select(r => r.shadowCastingMode).ToArray();
        var character = source.GetComponentInParent<CharacterController>();
        if (character != null) movement = character.GetComponents<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "KarakterHareket");
        movementEnabled = movement != null && movement.enabled;
        if (movement != null) movement.enabled = false;
        oldTargetRate = Application.targetFrameRate;
        oldVsync = QualitySettings.vSyncCount;
        oldBackground = Application.runInBackground;
        initialized = true;
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        Application.runInBackground = true;
        mainRecorder = Recorder(ProfilerCategory.Internal, "Main Thread");
        renderRecorder = Recorder(ProfilerCategory.Internal, "Render Thread");
        gpuRecorder = Recorder(ProfilerCategory.Render, "GPU Frame Time");
        triangleRecorder = Recorder(ProfilerCategory.Render, "Triangles Count");
        batchRecorder = Recorder(ProfilerCategory.Render, "Batches Count");
        report = new Report
        {
            cpu = SystemInfo.processorType,
            gpu = SystemInfo.graphicsDeviceName,
            pipeline = pipeline.name,
            position = position.ToString(), rotation = rotation.eulerAngles.ToString(),
            width = Screen.width, height = Screen.height,
            grassAcceptsMountainCamera = mountain != null && grass.Any(g => g.Accepts(mountain)),
            grassStatus = string.Join("; ", grass.Select(g => g.gpuStatus)),
            inventory = Inventory()
        };
        outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/FrameCostProbe_" + phase + ".json"));
        initialized = true;
        StartCoroutine(Measure());
    }

    static ProfilerRecorder Recorder(ProfilerCategory category, string name)
    { try { return ProfilerRecorder.StartNew(category, name, 1); } catch { return default; } }
    static double Last(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue : 0;

    private IEnumerator Measure()
    {
        CurrentStage = "Warmup";
        double warmup = Time.realtimeSinceStartupAsDouble + 8;
        while (Time.realtimeSinceStartupAsDouble < warmup) yield return null;
        report.width = Screen.width; report.height = Screen.height;
        int views = phaseName == "Profiler" ? 2 : 1;
        for (int viewIndex = 0; viewIndex < views; viewIndex++)
        {
        if (viewIndex == 1)
        {
            var savedViews = JsonUtility.FromJson<CullingValidationProbe.Views>(File.ReadAllText("Library/CullingValidationViews.json"));
            var point = savedViews.items.First(v => v.name == "RoadForest_0");
            position = point.position; rotation = Quaternion.Euler(0, point.yaw, 0); viewName = "Forest";
        }
        if (phaseName == "Profiler")
        {
            CurrentStage = "ProfilerCapture"; CaptureLabel = viewName; ProfilerCaptureComplete = false;
            double captureDeadline = Time.realtimeSinceStartupAsDouble + 90;
            while (!ProfilerCaptureComplete && Time.realtimeSinceStartupAsDouble < captureDeadline) yield return null;
        }
        string[] stages = { "Baseline", "NoMountainGeometry", "Baseline", "NoMountainCameraStack", "Baseline", "NoFarGrass", "Baseline", "NoShadows", "Baseline", "NoPostProcessing", "BaselineRepeat" };
        if (phaseName == "Profiler") stages = new[] { "Baseline", "NoShadows", "Baseline", "NoHeavyMeshShadows", "NoHeavyMeshes", "Baseline", "NoAllGrass", "NoTrees", "Baseline", "NoMountainCameraStack", "NoPlanarGlass", "NoPostProcessing", "RenderScale70Percent", "BaselineRepeat" };
        foreach (string stage in stages)
        {
            RestoreSettings();
            CurrentStage = stage;
            switch (stage)
            {
                case "NoMountainGeometry": if (mountain != null) mountain.cullingMask = 0; break;
                case "NoMountainCameraStack":
                    if (backdrop != null) backdrop.enabled = false;
                    source.clearFlags = CameraClearFlags.Skybox;
                    break;
                case "NoAllGrass":
                    foreach (Terrain terrain in terrains) { terrain.detailObjectDistance = 0; terrain.detailObjectDensity = 0; }
                    foreach (FarGrassRenderer renderer in grass) renderer.enabled = false;
                    break;
                case "NoFarGrass": foreach (FarGrassRenderer renderer in grass) renderer.enabled = false; break;
                case "NoTrees": foreach (Terrain terrain in terrains.Where(t => t.drawHeightmap)) terrain.treeDistance = 0; break;
                case "NoHeavyMeshes": foreach (MeshRenderer renderer in heavyMeshes) if (renderer) renderer.enabled = false; break;
                case "NoHeavyMeshShadows":
                    for (int i = 0; i < heavyMeshes.Length; i++) if (heavyMeshes[i])
                    {
                        if (heavyShadows[i] == ShadowCastingMode.ShadowsOnly) heavyMeshes[i].enabled = false;
                        else heavyMeshes[i].shadowCastingMode = ShadowCastingMode.Off;
                    }
                    break;
                case "NoShadows": pipeline.shadowDistance = 0; break;
                case "NoPlanarGlass": foreach (GasStationPlanarGlass manager in glass) manager.enabled = false; break;
                case "NoPostProcessing": sourceData.renderPostProcessing = false; break;
                case "RenderScale70Percent": pipeline.renderScale = oldScale * 0.7f; break;
            }
            File.WriteAllText(Path.ChangeExtension(outputPath, ".status"), viewName + " / " + stage);
            double until = Time.realtimeSinceStartupAsDouble + 3;
            while (Time.realtimeSinceStartupAsDouble < until) yield return null;
            var times = new List<float>();
            double main = 0, render = 0, gpu = 0, tris = 0, batches = 0;
            until = Time.realtimeSinceStartupAsDouble + 4;
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                yield return null;
                times.Add(Time.unscaledDeltaTime * 1000f);
                main += Last(mainRecorder) / 1000000;
                render += Last(renderRecorder) / 1000000;
                gpu += Last(gpuRecorder) / 1000000;
                tris += Last(triangleRecorder);
                batches += Last(batchRecorder);
            }
            times.Sort();
            int n = times.Count;
            if (n == 0) continue;
            report.samples.Add(new Sample
            {
                stage = stage, view = viewName, frames = n, meanMs = times.Average(), medianMs = times[n / 2], p95Ms = times[Math.Min(n - 1, (int)(n * 0.95f))],
                meanMainThreadMs = (float)(main / n), meanRenderThreadMs = (float)(render / n), meanGpuMs = (float)(gpu / n),
                triangles = (float)(tris / n), batches = (float)(batches / n)
            });
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
        }
        }
        RestoreAll();
        File.WriteAllText(Path.ChangeExtension(outputPath, ".status"), "COMPLETE; original settings restored");
        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (initialized && !restored && source != null) source.transform.SetPositionAndRotation(position, rotation);
    }

    private void RestoreSettings()
    {
        if (!initialized) return;
        if (pipeline) { pipeline.shadowDistance = oldShadowDistance; pipeline.renderScale = oldScale; }
        if (sourceData) sourceData.renderPostProcessing = originalPost;
        if (backdrop != null) { backdrop.enabled = originalBackdrop; if (originalBackdrop) backdrop.Apply(); }
        if (mountain != null) { mountain.cullingMask = mountainMask; mountain.enabled = originalBackground; }
        if (source) source.clearFlags = clearFlags;
        for (int i = 0; i < grass.Length; i++) if (grass[i] != null) grass[i].enabled = grassEnabled[i];
        for (int i = 0; i < glass.Length; i++) if (glass[i] != null) glass[i].enabled = glassEnabled[i];
        for (int i = 0; i < terrains.Length; i++) if (terrains[i] != null)
        { terrains[i].detailObjectDistance = detailDistance[i]; terrains[i].detailObjectDensity = detailDensity[i]; terrains[i].treeDistance = treeDistance[i]; }
        for (int i = 0; i < heavyMeshes.Length; i++) if (heavyMeshes[i]) { heavyMeshes[i].enabled = heavyEnabled[i]; heavyMeshes[i].shadowCastingMode = heavyShadows[i]; }
    }

    private void RestoreAll()
    {
        if (!initialized || restored) return;
        RestoreSettings();
        if (source) source.transform.SetPositionAndRotation(originalPosition, originalRotation);
        FarGrassRenderer.ProfileIncludeMountainCameras = false;
        if (mountain != null && originalMountainRenderer >= 0)
            mountain.GetUniversalAdditionalCameraData().SetRenderer(originalMountainRenderer);
        if (movement != null) movement.enabled = movementEnabled;
        Application.targetFrameRate = oldTargetRate;
        QualitySettings.vSyncCount = oldVsync;
        Application.runInBackground = oldBackground;
        mainRecorder.Dispose(); renderRecorder.Dispose(); gpuRecorder.Dispose(); triangleRecorder.Dispose(); batchRecorder.Dispose();
        restored = true;
    }
    private void OnDisable() { RestoreAll(); }

    private string Inventory()
    {
        var text = new System.Text.StringBuilder();
        text.AppendLine($"Shadows={pipeline.shadowDistance} m, cascades={pipeline.shadowCascadeCount}, atlas={pipeline.mainLightShadowmapResolution}, scale={pipeline.renderScale}");
        foreach (Terrain t in terrains) text.AppendLine($"Terrain {t.name}: trees={t.terrainData.treeInstanceCount}, detail={t.detailObjectDistance} m, density={t.detailObjectDensity}, tree distance={t.treeDistance}");
        foreach (FarGrassRenderer g in grass) text.AppendLine($"Far grass: caches={g.caches.Length}, density={g.profile?.farGrassDensity}, end={g.profile?.farGrassEnd}, GPU={g.gpuStatus}");
        foreach (GasStationPlanarGlass g in glass) text.AppendLine($"Glass: mode={g.reflectionMode}, groups={g.groups.Length}, active={g.ActiveGroups}, transmission={g.transmissionQuality}");
        var planes = GeometryUtility.CalculateFrustumPlanes(source);
        var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.enabled && r.GetComponent<MeshFilter>()?.sharedMesh != null && GeometryUtility.TestPlanesAABB(planes, r.bounds))
            .Select(r => new { renderer = r, mesh = r.GetComponent<MeshFilter>().sharedMesh })
            .OrderByDescending(r => TriangleCount(r.mesh)).Take(15);
        foreach (var item in renderers)
            text.AppendLine($"Potential visible mesh: {item.renderer.name}; triangles={TriangleCount(item.mesh)}; shader={item.renderer.sharedMaterial?.shader.name}");
        foreach (var renderer in heavyMeshes) text.AppendLine($"Heavy mesh A/B candidate: {renderer.name}; triangles={TriangleCount(renderer.GetComponent<MeshFilter>().sharedMesh)}; shadows={renderer.shadowCastingMode}");
        return text.ToString();
    }

    private static ulong TriangleCount(Mesh mesh)
    {
        ulong count = 0;
        for (int i = 0; i < mesh.subMeshCount; i++) count += mesh.GetIndexCount(i) / 3;
        return count;
    }
}
