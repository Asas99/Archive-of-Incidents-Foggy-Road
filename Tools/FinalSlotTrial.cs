using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Unity.Profiling;
public static class FinalSlotTrial
{
    class Entry{public MeshFilter f;public MeshRenderer r;public Mesh compact,original;public Material[] one,two;}
    static readonly List<Entry> entries=new List<Entry>();
    static readonly List<double> times=new List<double>(), gpuTimes=new List<double>();
    static readonly Vector3[] positions={new Vector3(440,2.25f,902),new Vector3(418,2.25f,902),new Vector3(509.513f,2.235f,1248.762f)};
    static readonly float[] yaws={0,90,180};static readonly string[] names={"Interior","Pumps","ForestRoad"},modes={"Original","Compact","OriginalRepeat","CompactRepeat"};
    static ProfilerRecorder batch,tri,gpu,gc;
    static double batches,triangles,allocated,start;
    static int phase=-1,last=-1,oldSize,captureFrame=-1;
    static Camera camera;static Vector3 position;static Quaternion rotation;
    static MonoBehaviour movement;static bool moving,background,running;static int rate,vsync;
    static string root="Recovery/QualityTrials_20261113";
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        var paths=File.ReadAllLines(root+"/AppliedGuardrailSlots.tsv").Skip(1).Where(l=>!string.IsNullOrWhiteSpace(l)).Select(l=>l.Split('\t')).ToDictionary(p=>p[2],p=>p[1]);
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("GRPRO")))
        {var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;var path=AssetDatabase.GetAssetPath(f.sharedMesh);if(!paths.TryGetValue(path,out var original))continue;var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(original);entries.Add(new Entry{f=f,r=r,compact=f.sharedMesh,original=mesh,one=r.sharedMaterials,two=Enumerable.Repeat(r.sharedMaterial,mesh.subMeshCount).ToArray()});}
        if(entries.Count!=167)throw new Exception("Expected 167 saved optimized renderers, got "+entries.Count);
        camera=UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(x=>x.GetComponent<Camera>()).First();
        position=camera.transform.position;rotation=camera.transform.rotation;
        movement=camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x.GetType().Name=="KarakterHareket");
        moving=movement&&movement.enabled;if(movement)movement.enabled=false;
        background=Application.runInBackground;rate=Application.targetFrameRate;vsync=QualitySettings.vSyncCount;
        Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
        var gt=typeof(Editor).Assembly.GetType("UnityEditor.GameView");var win=EditorWindow.GetWindow(gt);var p=gt.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);oldSize=(int)p.GetValue(win);p.SetValue(win,3);win.Focus();
        batch=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time",1);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
        File.WriteAllText(root+"/FinalSlots.csv","view,mode,width,height,frames,median_ms,p95_ms,mean_ms,gpu_ms,batches,triangles,gc_bytes\n");
        running=true;RenderPipelineManager.beginFrameRendering+=Before;RenderPipelineManager.endFrameRendering+=After;EditorApplication.playModeStateChanged+=State;Next();
        return "Started final slot A/B: 3 views x 4 stages; 1920x1080; original quality; all production changes restored at completion";
    }
    static void Mode(bool compact){foreach(var e in entries){if(!e.f||!e.r)continue;e.f.sharedMesh=compact?e.compact:e.original;e.r.sharedMaterials=compact?e.one:e.two;}}
    static void Before(ScriptableRenderContext c,Camera[] cams){if(running&&camera)camera.transform.SetPositionAndRotation(positions[phase/4],Quaternion.Euler(0,yaws[phase/4],0));}
    static void After(ScriptableRenderContext c,Camera[] cams)
    {
        if(!running||Time.frameCount==last)return;last=Time.frameCount;
        try
        {
            double elapsed=Time.realtimeSinceStartupAsDouble-start;
            if(elapsed<2.5)return;
            times.Add(Time.unscaledDeltaTime*1000);if(gpu.Valid&&gpu.LastValue>0)gpuTimes.Add(gpu.LastValue/1e6);batches+=batch.LastValue;triangles+=tri.LastValue;allocated+=gc.LastValue;
            if(elapsed<7||times.Count<20)return;
            if(phase%4<2&&captureFrame<0){ScreenCapture.CaptureScreenshot(Path.GetFullPath(root+"/"+names[phase/4]+"_"+modes[phase%4]+".png"));captureFrame=Time.frameCount;return;}
            if(captureFrame==Time.frameCount)return;
            times.Sort();gpuTimes.Sort();int n=times.Count;
            string N(double x)=>x.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            File.AppendAllText(root+"/FinalSlots.csv",string.Join(",",names[phase/4],modes[phase%4],Screen.width,Screen.height,n,N(times[n/2]),N(times[Math.Min(n-1,(int)(n*.95))]),N(times.Average()),N(gpuTimes.Count>0?gpuTimes.Average():-1),N(batches/n),N(triangles/n),N(allocated/n))+"\n");
            Next();
        }catch(Exception e){File.WriteAllText(root+"/FinalSlots.error",e.ToString());Stop();}
    }
    static void Next(){phase++;if(phase>=12){Stop();return;}Mode(phase%2==1);times.Clear();gpuTimes.Clear();batches=triangles=allocated=0;captureFrame=-1;camera.transform.SetPositionAndRotation(positions[phase/4],Quaternion.Euler(0,yaws[phase/4],0));start=Time.realtimeSinceStartupAsDouble;File.WriteAllText(root+"/FinalSlots.status",names[phase/4]+" / "+modes[phase%4]);}
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode)Stop();}
    public static string Stop()
    {
        RenderPipelineManager.beginFrameRendering-=Before;RenderPipelineManager.endFrameRendering-=After;EditorApplication.playModeStateChanged-=State;running=false;Mode(true);
        if(camera)camera.transform.SetPositionAndRotation(position,rotation);if(movement)movement.enabled=moving;
        Application.runInBackground=background;Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;batch.Dispose();tri.Dispose();gpu.Dispose();gc.Dispose();
        var gt=typeof(Editor).Assembly.GetType("UnityEditor.GameView");gt.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(EditorWindow.GetWindow(gt),oldSize);
        File.WriteAllText(root+"/FinalSlots.status","COMPLETE; production compact meshes and camera/control/settings restored");
        return "Restored";
    }
}