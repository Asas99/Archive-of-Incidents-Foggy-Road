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

// Diagnostic only. Execute in memory; all material/feature changes are restored.
public static class ForestCostCandidates
{
    static readonly string root="Recovery/OptimizationSearch_20261113";
    static readonly string[] modes={"Baseline","NoDecalsDiagnostic","TreeInstanced","BaselineRepeat","TreeInstancedRepeat","NoDecalsDiagnosticRepeat","BaselineFinal"};
    static readonly Dictionary<Material,Shader> originals=new Dictionary<Material,Shader>(), variants=new Dictionary<Material,Shader>();
    static readonly Dictionary<Material,string> snapshots=new Dictionary<Material,string>();
    static readonly List<double> frames=new List<double>(),gpus=new List<double>();
    static ScriptableRendererFeature decal;static string decalJson;static bool decalActive;
    static Camera camera;static MonoBehaviour movement;static bool moving,background,running;static int rate,vsync,oldSize,phase=-1,last=-1;
    static Vector3 position;static Quaternion rotation;static EditorWindow game;static PropertyInfo sizeProperty;
    static ProfilerRecorder gpu,batch,tri,main;static double began,batches,triangles,mains;static int mainCount;
    public static string Main()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Ready Play Mode required");
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");decal=data.rendererFeatures.First(f=>f is DecalRendererFeature);
        if(EditorUtility.IsDirty(decal))throw new Exception("Decal feature already dirty");decalActive=decal.isActive;decalJson=EditorJsonUtility.ToJson(decal);
        foreach(var row in File.ReadAllLines("Recovery/DrawCalls_20261107/InstalledMaterialShaders.tsv").Skip(1).Where(l=>!string.IsNullOrWhiteSpace(l)).Select(l=>l.Split('\t')))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(row[0]);var s=AssetDatabase.LoadAssetAtPath<Shader>(row[2]);
            if(!m||!s||EditorUtility.IsDirty(m))throw new Exception("Unavailable or dirty pine material: "+row[0]);
            originals.Add(m,m.shader);variants.Add(m,s);snapshots.Add(m,EditorJsonUtility.ToJson(m));
        }
        camera=UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(p=>p.GetComponent<Camera>()).First();position=camera.transform.position;rotation=camera.transform.rotation;
        movement=camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x.GetType().Name=="KarakterHareket");moving=movement&&movement.enabled;
        background=Application.runInBackground;rate=Application.targetFrameRate;vsync=QualitySettings.vSyncCount;
        var gt=typeof(Editor).Assembly.GetType("UnityEditor.GameView");game=EditorWindow.GetWindow(gt);sizeProperty=gt.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);oldSize=(int)sizeProperty.GetValue(game);
        if(movement)movement.enabled=false;Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;sizeProperty.SetValue(game,3);game.Focus();
        gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time",1);batch=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
        Directory.CreateDirectory(root);File.WriteAllText(root+"/Candidates.csv","mode,frames,width,height,median_ms,p95_ms,gpu_median_ms,main_mean_ms,batches,triangles\n");
        running=true;RenderPipelineManager.beginFrameRendering+=Before;RenderPipelineManager.endFrameRendering+=After;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Abort;EditorApplication.update+=Watchdog;Next();
        return "Started 7-phase forest A/B: DBuffer diagnostic and pine instancing; no assets saved; auto-restore enabled";
    }
    static void Before(ScriptableRenderContext c,Camera[] cameras){if(running)camera.transform.SetPositionAndRotation(new Vector3(509.513f,2.235f,1248.762f),Quaternion.Euler(0,180,0));}
    static void After(ScriptableRenderContext c,Camera[] cameras)
    {
        if(!running||last==Time.frameCount||!cameras.Contains(camera))return;last=Time.frameCount;
        try{
            double elapsed=Time.realtimeSinceStartupAsDouble-began;if(elapsed<5)return;
            frames.Add(Time.unscaledDeltaTime*1000);if(gpu.Valid&&gpu.LastValue>0)gpus.Add(gpu.LastValue/1e6);batches+=batch.LastValue;triangles+=tri.LastValue;if(main.Valid&&main.LastValue>0){mains+=main.LastValue/1e6;mainCount++;}
            if(elapsed<11||frames.Count<80)return;
            frames.Sort();gpus.Sort();int n=frames.Count;string N(double x)=>x.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            File.AppendAllText(root+"/Candidates.csv",string.Join(",",modes[phase],n,Screen.width,Screen.height,N(frames[n/2]),N(frames[Math.Min(n-1,(int)(n*.95))]),N(gpus.Count>0?gpus[gpus.Count/2]:-1),N(mainCount>0?mains/mainCount:-1),N(batches/n),N(triangles/n))+"\n");Next();
        }catch(Exception e){File.WriteAllText(root+"/error.txt",e.ToString());Stop();}
    }
    static void Next()
    {
        phase++;if(phase>=modes.Length){Stop();return;}
        bool instance=modes[phase].StartsWith("TreeInstanced");foreach(var m in originals)m.Key.shader=instance?variants[m.Key]:m.Value;
        decal.SetActive(modes[phase].StartsWith("NoDecals")?false:decalActive);
        frames.Clear();gpus.Clear();batches=triangles=mains=0;mainCount=0;began=Time.realtimeSinceStartupAsDouble;File.WriteAllText(root+"/status.txt",modes[phase]);
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode)Stop();}
    static void Abort(){Stop();}
    static void Watchdog(){if(running&&(!EditorApplication.isPlaying||EditorApplication.isPaused||Time.realtimeSinceStartupAsDouble-began>60))Stop();}
    public static string Stop()
    {
        running=false;RenderPipelineManager.beginFrameRendering-=Before;RenderPipelineManager.endFrameRendering-=After;EditorApplication.playModeStateChanged-=State;AssemblyReloadEvents.beforeAssemblyReload-=Abort;EditorApplication.update-=Watchdog;
        foreach(var s in snapshots)if(s.Key){EditorJsonUtility.FromJsonOverwrite(s.Value,s.Key);EditorUtility.ClearDirty(s.Key);}
        if(decal){decal.SetActive(decalActive);EditorJsonUtility.FromJsonOverwrite(decalJson,decal);EditorUtility.ClearDirty(decal);}
        if(camera)camera.transform.SetPositionAndRotation(position,rotation);if(movement)movement.enabled=moving;Application.runInBackground=background;Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;
        gpu.Dispose();batch.Dispose();tri.Dispose();main.Dispose();if(game)sizeProperty.SetValue(game,oldSize);
        bool exact=snapshots.All(s=>s.Key&&EditorJsonUtility.ToJson(s.Key)==s.Value&&!EditorUtility.IsDirty(s.Key))&&decal&&EditorJsonUtility.ToJson(decal)==decalJson&&!EditorUtility.IsDirty(decal);
        string result=(phase>=modes.Length?"COMPLETE":"STOPPED")+"; materials/feature serialization restored="+exact+"; camera/control/settings restored; no saves";File.WriteAllText(root+"/status.txt",result);return result;
    }
}
