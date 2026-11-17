using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Profiling;

// Cost isolation only: disabling a visible effect is never retained as an optimization.
public static class OtherCostIsolation
{
    static readonly string root="Recovery/OtherCosts_20261114";
    static readonly string[] modes={"Baseline","NoFogDiagnostic","NoFarGrassDiagnostic","NoNativeDetailsDiagnostic","BaselineRepeat","NoFogRepeat","NoFarGrassRepeat"};
    static readonly Vector3[] positions={new Vector3(509.513f,2.235f,1248.762f),new Vector3(440,2.25f,902)};
    static readonly float[] yaw={180,0};
    static readonly Dictionary<ScriptableRendererFeature,string> snapshots=new Dictionary<ScriptableRendererFeature,string>();
    static readonly Dictionary<Terrain,float> details=new Dictionary<Terrain,float>();
    static ScriptableRendererFeature fog,grass;static bool fogActive,grassActive;
    static Camera camera;static MonoBehaviour movement;static Vector3 oldPosition;static Quaternion oldRotation;
    static bool moving,background,running;static int rate,vsync,size,phase=-1,last=-1;static PropertyInfo sizeProperty;static EditorWindow game;
    static ProfilerRecorder gpu,batch,tri,main,gc;static double began,batches,triangles,mains,allocations;static int mainSamples;
    static readonly List<double> frames=new List<double>(),gpus=new List<double>();
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");fog=data.rendererFeatures.OfType<ReducedResolutionFogFeature>().Single();grass=data.rendererFeatures.OfType<FarGrassFeature>().Single();
        foreach(var f in new[]{fog,grass}){if(EditorUtility.IsDirty(f))throw new Exception("Dirty feature "+f.name);snapshots.Add(f,EditorJsonUtility.ToJson(f));}fogActive=fog.isActive;grassActive=grass.isActive;
        foreach(var t in Terrain.activeTerrains)details.Add(t,t.detailObjectDistance);
        camera=UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(x=>x.GetComponent<Camera>()).First();oldPosition=camera.transform.position;oldRotation=camera.transform.rotation;
        movement=camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x.GetType().Name=="KarakterHareket");moving=movement&&movement.enabled;if(movement)movement.enabled=false;
        background=Application.runInBackground;rate=Application.targetFrameRate;vsync=QualitySettings.vSyncCount;Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
        var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");game=EditorWindow.GetWindow(type);sizeProperty=type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);size=(int)sizeProperty.GetValue(game);sizeProperty.SetValue(game,3);game.Focus();
        gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time",1);batch=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
        Directory.CreateDirectory(root);File.WriteAllText(root+"/Isolation.csv","view,mode,frames,median_ms,p95_ms,gpu_median_ms,main_mean_ms,batches,triangles,gc_bytes\n");
        running=true;RenderPipelineManager.beginFrameRendering+=Before;RenderPipelineManager.endFrameRendering+=After;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Abort;EditorApplication.update+=Watchdog;Next();return "Started 2-view fog/far-grass/native-detail cost isolation, repeated baselines; auto-restore enabled";
    }
    static void Before(ScriptableRenderContext c,Camera[] cameras)
    {
        if(!running)return;int view=phase/modes.Length;camera.transform.SetPositionAndRotation(positions[view],Quaternion.Euler(0,yaw[view],0));
        foreach(var t in details)if(t.Key)t.Key.detailObjectDistance=modes[phase%modes.Length].StartsWith("NoNative")?0:t.Value;
    }
    static void After(ScriptableRenderContext c,Camera[] cameras)
    {
        if(!running||last==Time.frameCount||!cameras.Contains(camera))return;last=Time.frameCount;
        try{
            double elapsed=Time.realtimeSinceStartupAsDouble-began;if(elapsed<5)return;
            frames.Add(Time.unscaledDeltaTime*1000);if(gpu.Valid&&gpu.LastValue>0)gpus.Add(gpu.LastValue/1e6);batches+=batch.LastValue;triangles+=tri.LastValue;allocations+=gc.LastValue;if(main.Valid&&main.LastValue>0){mains+=main.LastValue/1e6;mainSamples++;}
            if(elapsed<10||frames.Count<100)return;
            frames.Sort();gpus.Sort();int n=frames.Count;string N(double v)=>v.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            File.AppendAllText(root+"/Isolation.csv",string.Join(",",phase/modes.Length,modes[phase%modes.Length],n,N(frames[n/2]),N(frames[Math.Min(n-1,(int)(n*.95))]),N(gpus.Count>0?gpus[gpus.Count/2]:-1),N(mainSamples>0?mains/mainSamples:-1),N(batches/n),N(triangles/n),N(allocations/n))+"\n");Next();
        }catch(Exception e){File.WriteAllText(root+"/error.txt",e.ToString());Stop();}
    }
    static void Next()
    {
        phase++;if(phase>=modes.Length*positions.Length){Stop();return;}string mode=modes[phase%modes.Length];fog.SetActive(mode.StartsWith("NoFog")?false:fogActive);grass.SetActive(mode.StartsWith("NoFarGrass")?false:grassActive);
        frames.Clear();gpus.Clear();batches=triangles=mains=allocations=0;mainSamples=0;began=Time.realtimeSinceStartupAsDouble;File.WriteAllText(root+"/status.txt","view="+(phase/modes.Length)+" "+mode);
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode)Stop();}
    static void Abort(){Stop();}
    static void Watchdog(){if(running&&(!EditorApplication.isPlaying||EditorApplication.isPaused||Time.realtimeSinceStartupAsDouble-began>60))Stop();}
    public static string Stop()
    {
        running=false;RenderPipelineManager.beginFrameRendering-=Before;RenderPipelineManager.endFrameRendering-=After;EditorApplication.playModeStateChanged-=State;AssemblyReloadEvents.beforeAssemblyReload-=Abort;EditorApplication.update-=Watchdog;
        foreach(var f in snapshots){EditorJsonUtility.FromJsonOverwrite(f.Value,f.Key);EditorUtility.ClearDirty(f.Key);}foreach(var t in details)if(t.Key)t.Key.detailObjectDistance=t.Value;
        if(camera)camera.transform.SetPositionAndRotation(oldPosition,oldRotation);if(movement)movement.enabled=moving;Application.runInBackground=background;Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;if(game)sizeProperty.SetValue(game,size);
        gpu.Dispose();batch.Dispose();tri.Dispose();main.Dispose();gc.Dispose();bool exact=snapshots.All(f=>EditorJsonUtility.ToJson(f.Key)==f.Value&&!EditorUtility.IsDirty(f.Key));string r=(phase>=modes.Length*positions.Length?"COMPLETE":"STOPPED")+"; features restored="+exact+"; terrain/camera/settings restored";File.WriteAllText(root+"/status.txt",r);return r;
    }
}
