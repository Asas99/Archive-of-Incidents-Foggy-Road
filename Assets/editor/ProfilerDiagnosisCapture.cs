using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>Captures real Unity Profiler frames, separately from unprofiled A/B timing.</summary>
[InitializeOnLoad]
public static class ProfilerDiagnosisCapture
{
    [Serializable] public sealed class Marker
    {
        public string thread, name; public double inclusiveMeanMs, selfMeanMs, maxInclusiveMs; public int calls;
    }
    [Serializable] public sealed class Capture
    {
        public string view, mode, source = "Unity Profiler RawFrameDataView";
        public int firstFrame, lastFrame, frames; public bool deepProfiling;
        public List<Marker> markers = new List<Marker>();
    }
    static string root, activeLabel;
    static readonly HashSet<string> completed = new HashSet<string>();
    static bool saved, oldEnabled, oldEditor, oldGpu, oldCpu;
    static int state;
    static double deadline;
    static ProfilerDiagnosisCapture() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var probe = UnityEngine.Object.FindFirstObjectByType<FrameCostProbe>();
        if (probe == null)
        {
            if (saved) { Restore(); completed.Clear(); root = null; state = 0; }
            return;
        }
        if (probe.CurrentStage != "ProfilerCapture" || completed.Contains(probe.CaptureLabel)) return;
        try
        {
            if (!saved)
            {
                root = "Recovery/ProfilerDiagnosis_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"); Directory.CreateDirectory(root);
                File.WriteAllText("Library/ProfilerDiagnosis.root", root);
                oldEnabled = ProfilerDriver.enabled; oldEditor = ProfilerDriver.profileEditor; oldGpu = ProfilerDriver.profileGPU;
                oldCpu = ProfilerDriver.IsAreaEnabled(ProfilerArea.CPU); saved = true;
                if (ProfilerDriver.lastFrameIndex >= 0) ProfilerDriver.SaveProfile(root + "/PreviousCapture.data");
                if (ProfilerDriver.deepProfiling) throw new InvalidOperationException("Deep Profiling is enabled; capture aborted to avoid distorted timing.");
            }
            if (state == 0)
            {
                activeLabel = probe.CaptureLabel; state = 1; StartRecording(false); return;
            }
            if (EditorApplication.timeSinceStartup < deadline) return;
            if (state == 1)
            {
                StopAndExport("Gameplay"); state = 2; StartRecording(true); return;
            }
            StopAndExport("IncludingEditor"); state = 0; completed.Add(activeLabel); probe.ProfilerCaptureComplete = true;
        }
        catch (Exception ex)
        {
            ProfilerDriver.enabled = false; File.WriteAllText((root ?? "Library") + "/ProfilerCapture.error", ex.ToString());
            completed.Add(probe.CaptureLabel); state = 0; probe.ProfilerCaptureComplete = true; Debug.LogException(ex);
        }
    }
    static void StartRecording(bool editor)
    {
        ProfilerDriver.enabled = false; ProfilerDriver.ClearAllFrames();
        // GPU module can serialize D3D11 work and distort CPU waits. GPU timing is
        // collected separately by the unprofiled A/B probe's GPU Frame Time counter.
        ProfilerDriver.profileEditor = editor; ProfilerDriver.profileGPU = false;
        ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, true); ProfilerDriver.enabled = true;
        deadline = EditorApplication.timeSinceStartup + 8;
        File.WriteAllText("Library/ProfilerDiagnosis.status", activeLabel + " / recording " + (editor ? "Editor + Gameplay" : "Gameplay"));
    }
    static void StopAndExport(string mode)
    {
        ProfilerDriver.enabled = false;
        string path = root + "/" + activeLabel + "_" + mode;
        ProfilerDriver.SaveProfile(path + ".data");
        if (!File.Exists(path + ".data") || new FileInfo(path + ".data").Length == 0) throw new IOException("Native Profiler capture save failed.");
        var capture = new Capture { view = activeLabel, mode = mode, deepProfiling = ProfilerDriver.deepProfiling,
            firstFrame = Math.Max(ProfilerDriver.firstFrameIndex + 5, ProfilerDriver.lastFrameIndex - 90), lastFrame = ProfilerDriver.lastFrameIndex - 2 };
        var totals = new Dictionary<string, Marker>(); var threadFrames = new Dictionary<string, int>();
        for (int frame = capture.firstFrame; frame <= capture.lastFrame; frame++)
        {
            bool mainFound = false;
            for (int thread = 0; thread < 128; thread++)
            {
                using (RawFrameDataView data = ProfilerDriver.GetRawFrameDataView(frame, thread))
                {
                    if (!data.valid) break;
                    string name = data.threadName;
                    if (name != "Main Thread" && name != "Render Thread" && name.IndexOf("GPU", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (name == "Main Thread") mainFound = true;
                    if (!threadFrames.ContainsKey(name)) threadFrames[name] = 0; threadFrames[name]++;
                    for (int i = 0; i < data.sampleCount; i++)
                    {
                        double inclusive = data.GetSampleTimeMs(i); if (inclusive <= 0) continue;
                        double self = inclusive;
                        int child = i + 1;
                        for (int c = 0; c < data.GetSampleChildrenCount(i) && child < data.sampleCount; c++)
                        { self -= data.GetSampleTimeMs(child); child += 1 + data.GetSampleChildrenCountRecursive(child); }
                        string markerName = data.GetSampleName(i), key = name + "|" + markerName;
                        if (!totals.TryGetValue(key, out var marker)) { marker = new Marker { thread = name, name = markerName }; totals.Add(key, marker); }
                        marker.inclusiveMeanMs += inclusive; marker.selfMeanMs += Math.Max(0, self); marker.calls++;
                        marker.maxInclusiveMs = Math.Max(marker.maxInclusiveMs, inclusive);
                    }
                }
            }
            if (mainFound) capture.frames++;
        }
        foreach (var marker in totals.Values)
        { int count = threadFrames[marker.thread]; marker.inclusiveMeanMs /= count; marker.selfMeanMs /= count; }
        capture.markers = totals.Values.OrderByDescending(m => m.selfMeanMs).ToList();
        File.WriteAllText(path + ".json", JsonUtility.ToJson(capture, true));
        File.WriteAllText("Library/ProfilerDiagnosis.status", activeLabel + " / saved " + mode + "; analyzed frames=" + capture.frames);
    }
    static void Restore()
    {
        ProfilerDriver.enabled = false; ProfilerDriver.profileEditor = oldEditor; ProfilerDriver.profileGPU = oldGpu;
        ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, oldCpu); ProfilerDriver.enabled = oldEnabled; saved = false;
        File.WriteAllText("Library/ProfilerDiagnosis.status", "COMPLETE; Profiler settings restored; captures=" + root);
    }
}
