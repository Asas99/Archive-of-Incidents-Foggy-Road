using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

/// <summary>Fixed-road CPU/GPU sanity check. Creates no persistent scene objects.</summary>
public sealed class RoadCpuBenchmark : MonoBehaviour
{
    [Serializable] public sealed class Report
    {
        public string view, gpu;
        public int frames, glassIdleSkips;
        public float meanFrameMs, medianFrameMs, meanMainThreadMs, meanGpuMs, meanGcBytes;
    }

    Camera camera;
    GasStationPlanarGlass glass;
    KarakterHareket movement;
    bool movementWasEnabled, restored;
    Vector3 originalPosition;
    Quaternion originalRotation;
    int oldVsync, oldTarget, startSkips;
    bool oldBackground;
    ProfilerRecorder main, gpu, gc;

    public static void Begin()
    {
        if (FindFirstObjectByType<RoadCpuBenchmark>() != null) return;
        var camera = FindObjectsByType<Camera>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c.isActiveAndEnabled && c.targetTexture == null && c.GetComponent<DistantMountainBackdrop>() == null);
        if (!camera) throw new InvalidOperationException("Gameplay camera missing.");
        var host = new GameObject("Temporary Road CPU Benchmark");
        host.AddComponent<RoadCpuBenchmark>().Initialize(camera, FindFirstObjectByType<GasStationPlanarGlass>());
    }

    void Initialize(Camera source, GasStationPlanarGlass manager)
    {
        camera = source; glass = manager;
        originalPosition = camera.transform.position; originalRotation = camera.transform.rotation;
        movement = camera.GetComponentInParent<KarakterHareket>();
        if (movement) { movementWasEnabled = movement.enabled; movement.enabled = false; }
        camera.transform.SetPositionAndRotation(new Vector3(509.513f, 2.235f, 1248.762f), Quaternion.Euler(0f, 180f, 0f));
        oldVsync = QualitySettings.vSyncCount; oldTarget = Application.targetFrameRate; oldBackground = Application.runInBackground;
        QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; Application.runInBackground = true;
        main = Start(ProfilerCategory.Internal, "Main Thread");
        gpu = Start(ProfilerCategory.Render, "GPU Frame Time");
        gc = Start(ProfilerCategory.Memory, "GC Allocated In Frame");
        startSkips = glass ? glass.DistantIdleSkipCount : 0;
        StartCoroutine(Measure());
    }
    static ProfilerRecorder Start(ProfilerCategory category, string name) { try { return ProfilerRecorder.StartNew(category, name, 1); } catch { return default; } }
    static float Ms(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue / 1000000f : 0;

    IEnumerator Measure()
    {
        yield return new WaitForSecondsRealtime(8);
        var frames = new System.Collections.Generic.List<float>();
        double totalMain = 0, totalGpu = 0, totalGc = 0;
        double until = Time.realtimeSinceStartupAsDouble + 10;
        while (Time.realtimeSinceStartupAsDouble < until)
        {
            yield return null; frames.Add(Time.unscaledDeltaTime * 1000);
            totalMain += Ms(main); totalGpu += Ms(gpu); if (gc.Valid) totalGc += gc.LastValue;
        }
        frames.Sort();
        var report = new Report { view = "Road (509.513, 2.235, 1248.762), yaw 180", gpu = SystemInfo.graphicsDeviceName,
            frames = frames.Count, glassIdleSkips = glass ? glass.DistantIdleSkipCount - startSkips : 0,
            meanFrameMs = frames.Average(), medianFrameMs = frames[frames.Count / 2],
            meanMainThreadMs = (float)(totalMain / frames.Count), meanGpuMs = (float)(totalGpu / frames.Count), meanGcBytes = (float)(totalGc / frames.Count) };
        File.WriteAllText("Library/RoadCpuBenchmark.json", JsonUtility.ToJson(report, true));
        Restore(); Destroy(gameObject);
    }
    void Restore()
    {
        if (restored) return;
        restored = true;
        if (camera) camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
        if (movement) movement.enabled = movementWasEnabled;
        QualitySettings.vSyncCount = oldVsync; Application.targetFrameRate = oldTarget; Application.runInBackground = oldBackground;
        main.Dispose(); gpu.Dispose(); gc.Dispose();
    }
    void OnDisable() => Restore();
}
