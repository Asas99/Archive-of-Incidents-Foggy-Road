#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

// Explicit, temporary Play Mode A/B. Source scene and player pose are restored
// by leaving Play Mode; only benchmark counters and screenshots are written.
[InitializeOnLoad]
public static class PoleCableOptimizationBenchmark
{
    [Serializable] public class Sample
    {
        public string mode;
        public int frames, width, height, cableRenderers, cableFrustumCandidates, poleBatched;
        public float meanMs, medianMs, p95Ms, batches, triangles, cableCandidateTriangles;
    }
    [Serializable] public class Report
    {
        public string environment = "Unity Editor Play Mode; uncapped; fixed road camera; full scene",
            gpu, position;
        public List<Sample> samples = new List<Sample>();
    }
    static Camera camera;
    static FoggyRoad_PowerLine line;
    static FoggyRoadPoleBatching batching;
    static ProfilerRecorder batches, triangles;
    static Report report;
    static readonly List<float> times = new List<float>();
    static double deadline, sumBatches, sumTriangles;
    static int stage = -1, frame = -1;
    static bool measuring;
    static Vector3 position;
    static Quaternion rotation;
    static string output;
    static int oldVsync, oldTarget;
    static bool oldBackground, settingsSaved;
    static readonly string[] Modes = { "Original", "PartitionedCables", "CablesAndPoleBatching", "OptimizedRepeat" };

    static PoleCableOptimizationBenchmark()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            if (SessionState.GetBool("PoleCableRestoreBatch", false))
            {
                var root = GameObject.Find("ElektrikDirekleri_Auto");
                var batch = root ? root.GetComponent<FoggyRoadPoleBatching>() : null;
                if (batch) batch.batchOnStart = SessionState.GetBool("PoleCableOldBatchOnStart", true);
                SessionState.SetBool("PoleCableRestoreBatch", false);
            }
            if (settingsSaved)
            {
                QualitySettings.vSyncCount = oldVsync;
                Application.targetFrameRate = oldTarget;
                Application.runInBackground = oldBackground;
                settingsSaved = false;
            }
            batches.Dispose(); triangles.Dispose(); stage = -1;
        };
    }

    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Benchmark requires Edit Mode.");
        var root = GameObject.Find("ElektrikDirekleri_Auto");
        var batch = root.GetComponent<FoggyRoadPoleBatching>();
        SessionState.SetBool("PoleCableOldBatchOnStart", batch.batchOnStart);
        SessionState.SetBool("PoleCableRestoreBatch", true);
        batch.batchOnStart = false;
        stage = -1;
        measuring = false;
        var viewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (viewType != null) EditorWindow.GetWindow(viewType).Focus();
        SessionState.SetBool("PoleCableBenchmarkPending", true);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!Application.isPlaying) return;
        try
        {
            if (stage < 0)
            {
                if (!SessionState.GetBool("PoleCableBenchmarkPending", false)) return;
                line = UnityEngine.Object.FindFirstObjectByType<FoggyRoad_PowerLine>();
                camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.isActiveAndEnabled && !c.targetTexture && !c.GetComponent<DistantMountainBackdrop>());
                if (!line || !camera) return;
                SessionState.SetBool("PoleCableBenchmarkPending", false);
                batching = line.transform.parent.GetComponent<FoggyRoadPoleBatching>();
                var pole = line.poles[line.poles.Count / 2];
                // Pick the closest point on the road spine, not a station or a
                // point behind a tree. Its tangent supplies the repeatable view.
                float best = float.PositiveInfinity;
                Vector3 road = pole.position, tangent = Vector3.forward;
                for (int i = 1; i < line.spine.Count; i++)
                {
                    Vector3 a = line.spine[i - 1], delta = line.spine[i] - a;
                    Vector3 p = a + delta * Mathf.Clamp01(Vector3.Dot(pole.position - a, delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                    float d = (p - pole.position).sqrMagnitude;
                    if (d < best) { best = d; road = p; tangent = delta; }
                }
                position = road + Vector3.up * 1.8f;
                tangent.y = 0;
                rotation = Quaternion.LookRotation(tangent.normalized);
                foreach (var controller in camera.GetComponentsInParent<MonoBehaviour>())
                    if (controller.GetType().Name == "KarakterHareket") controller.enabled = false;
                oldBackground = Application.runInBackground;
                oldTarget = Application.targetFrameRate;
                oldVsync = QualitySettings.vSyncCount;
                settingsSaved = true;
                Application.runInBackground = true;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
                triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
                report = new Report { gpu = SystemInfo.graphicsDeviceName, position = position.ToString("F3") };
                output = Path.Combine(SessionState.GetString("PoleCableOptimizationBackup", "Library"), "RoadBenchmark.json");
                stage = 0;
                SetStage();
            }
            camera.transform.SetPositionAndRotation(position, rotation);
            if (Time.frameCount == frame) return;
            frame = Time.frameCount;
            if (Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (measuring)
                {
                    times.Add(Time.unscaledDeltaTime * 1000);
                    if (batches.Valid) sumBatches += batches.LastValue;
                    if (triangles.Valid) sumTriangles += triangles.LastValue;
                }
                return;
            }
            if (!measuring)
            {
                times.Clear(); sumBatches = sumTriangles = 0;
                measuring = true; deadline = Time.realtimeSinceStartupAsDouble + 8;
                return;
            }
            if (times.Count < 5) throw new Exception("Insufficient rendered frames.");
            times.Sort();
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers = line.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && !r.forceRenderingOff).ToArray();
            var visible = renderers.Where(r => GeometryUtility.TestPlanesAABB(planes, r.bounds)).ToArray();
            report.samples.Add(new Sample {
                mode = Modes[stage], frames = times.Count, width = Screen.width, height = Screen.height,
                meanMs = times.Average(), medianMs = times[times.Count / 2], p95Ms = times[(int)((times.Count - 1) * .95f)],
                batches = (float)(sumBatches / times.Count), triangles = (float)(sumTriangles / times.Count),
                cableRenderers = renderers.Length, cableFrustumCandidates = visible.Length,
                cableCandidateTriangles = visible.Sum(r => (long)r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0) / 3),
                poleBatched = batching.BatchedCount
            });
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            if (++stage < Modes.Length) { SetStage(); return; }
            batches.Dispose(); triangles.Dispose();
            stage = -1;
            EditorApplication.isPlaying = false;
            Debug.Log("[FoggyRoad] Road pole/cable benchmark complete: " + output);
        }
        catch (Exception e)
        {
            SessionState.SetBool("PoleCableBenchmarkPending", false);
            batches.Dispose(); triangles.Dispose();
            stage = -1;
            Debug.LogException(e);
            EditorApplication.isPlaying = false;
        }
    }

    static void SetStage()
    {
        line.partitionRendering = stage != 0;
        line.Rebuild();
        if (stage == 2) batching.Apply();
        measuring = false;
        deadline = Time.realtimeSinceStartupAsDouble + 6;
        Debug.Log("[FoggyRoad] Road benchmark: " + Modes[stage]);
    }
}
#endif
