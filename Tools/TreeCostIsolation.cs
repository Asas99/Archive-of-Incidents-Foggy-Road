using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Unity.Profiling;

// Run in-memory through script_execute. Never saves scene or material assets.
public static class TreeCostIsolation
{
    static readonly string root="Recovery/TreeCost_20261113";
    static readonly string[] modes={"Baseline","NoTreeShadows","NoTrees","BaselineRepeat","NoTreesRepeat","NoTreeShadowsRepeat","BaselineFinal"};
    static readonly Dictionary<Material,bool> passes=new Dictionary<Material,bool>();
    static readonly Dictionary<Material,string> materialJson=new Dictionary<Material,string>();
    static readonly List<double> frame=new List<double>(), gpuFrame=new List<double>();
    static ProfilerRecorder batches,triangles,gpu,main;
    static Camera camera;static Terrain terrain;static MonoBehaviour movement;
    static Vector3 position;static Quaternion rotation;static bool moving,background,paused,running;
    static int rate,vsync,oldSize,phase=-1,lastFrame=-1;static float distance;
    static double began,batchSum,triangleSum,mainSum;static int mainSamples;
    static PropertyInfo sizeProperty;static EditorWindow game;
    public static string Main()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Ready Play Mode required");
        terrain=Terrain.activeTerrains.OrderByDescending(t=>t.terrainData.treeInstanceCount).First();
        var prototypes=terrain.terrainData.treePrototypes;
        var distribution=terrain.terrainData.treeInstances.GroupBy(i=>i.prototypeIndex).Select(g=>new {index=g.Key,count=g.Count(),pine=prototypes[g.Key].prefab.GetComponentsInChildren<Renderer>(true).Any(r=>r.name.StartsWith("Pine_"))}).ToArray();
        if(distribution.Any(d=>!d.pine))throw new Exception("Main Terrain contains non-pine instances; treeDistance isolation would be mixed");
        camera=UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(p=>p.GetComponent<Camera>()).First();
        position=camera.transform.position;rotation=camera.transform.rotation;distance=terrain.treeDistance;
        movement=camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x.GetType().Name=="KarakterHareket");
        moving=movement&&movement.enabled;
        background=Application.runInBackground;rate=Application.targetFrameRate;vsync=QualitySettings.vSyncCount;paused=EditorApplication.isPaused;
        foreach(var m in distribution.SelectMany(d=>prototypes[d.index].prefab.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct())
        {if(EditorUtility.IsDirty(m))throw new Exception("Tree material already dirty: "+m.name);passes.Add(m,m.GetShaderPassEnabled("ShadowCaster"));materialJson.Add(m,EditorJsonUtility.ToJson(m));}
        Directory.CreateDirectory(root);
        File.WriteAllText(root+"/inventory.txt","Terrain="+terrain.name+" trees="+terrain.terrainData.treeInstanceCount+" originalDistance="+distance+" materials="+passes.Count+" GPU="+SystemInfo.graphicsDeviceName+"\n"+string.Join("\n",distribution.Select(d=>"prototype="+d.index+" count="+d.count)));
        var gt=typeof(Editor).Assembly.GetType("UnityEditor.GameView");game=EditorWindow.GetWindow(gt);sizeProperty=gt.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);oldSize=(int)sizeProperty.GetValue(game);
        if(movement)movement.enabled=false;Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;EditorApplication.isPaused=false;
        sizeProperty.SetValue(game,3);game.Focus();
        batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time",1);main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
        File.WriteAllText(root+"/results.csv","mode,width,height,frames,median_ms,p95_ms,gpu_median_ms,main_mean_ms,batches,triangles\n");
        running=true;RenderPipelineManager.beginFrameRendering+=Before;RenderPipelineManager.endFrameRendering+=After;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Abort;EditorApplication.update+=Watchdog;
        Next();return "Started pine isolation: 7 phases, 3s warmup + 6s samples each; tree shadows, trees, repeated baselines. Auto restore enabled.";
    }
    static void Before(ScriptableRenderContext context,Camera[] cameras)
    {
        if(!running)return;
        camera.transform.SetPositionAndRotation(new Vector3(509.513f,2.235f,1248.762f),Quaternion.Euler(0,180,0));
        terrain.treeDistance=modes[phase].StartsWith("NoTrees")?0:distance;
    }
    static void After(ScriptableRenderContext context,Camera[] cameras)
    {
        if(!running||lastFrame==Time.frameCount||!cameras.Contains(camera))return;lastFrame=Time.frameCount;
        try{
            double elapsed=Time.realtimeSinceStartupAsDouble-began;if(elapsed<3)return;
            frame.Add(Time.unscaledDeltaTime*1000);if(gpu.Valid&&gpu.LastValue>0)gpuFrame.Add(gpu.LastValue/1e6);
            batchSum+=batches.LastValue;triangleSum+=triangles.LastValue;if(main.Valid&&main.LastValue>0){mainSum+=main.LastValue/1e6;mainSamples++;}
            if(elapsed<9||frame.Count<80)return;
            frame.Sort();gpuFrame.Sort();int n=frame.Count;
            string N(double v)=>v.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            File.AppendAllText(root+"/results.csv",string.Join(",",modes[phase],Screen.width,Screen.height,n,N(frame[n/2]),N(frame[Math.Min(n-1,(int)(n*.95))]),N(gpuFrame.Count>0?gpuFrame[gpuFrame.Count/2]:-1),N(mainSamples>0?mainSum/mainSamples:-1),N(batchSum/n),N(triangleSum/n))+"\n");Next();
        }catch(Exception e){File.WriteAllText(root+"/error.txt",e.ToString());Stop();}
    }
    static void Next()
    {
        phase++;if(phase>=modes.Length){Stop();return;}
        bool noShadows=modes[phase].StartsWith("NoTreeShadows");
        foreach(var p in passes)p.Key.SetShaderPassEnabled("ShadowCaster",noShadows?false:p.Value);
        terrain.treeDistance=modes[phase].StartsWith("NoTrees")?0:distance;
        frame.Clear();gpuFrame.Clear();batchSum=triangleSum=mainSum=0;mainSamples=0;began=Time.realtimeSinceStartupAsDouble;
        File.WriteAllText(root+"/status.txt",modes[phase]);
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode)Stop();}
    static void Abort(){Stop();}
    static void Watchdog(){if(running&&(!EditorApplication.isPlaying||EditorApplication.isPaused||Time.realtimeSinceStartupAsDouble-began>45))Stop();}
    public static string Stop()
    {
        running=false;RenderPipelineManager.beginFrameRendering-=Before;RenderPipelineManager.endFrameRendering-=After;EditorApplication.playModeStateChanged-=State;AssemblyReloadEvents.beforeAssemblyReload-=Abort;EditorApplication.update-=Watchdog;
        foreach(var p in passes)if(p.Key)p.Key.SetShaderPassEnabled("ShadowCaster",p.Value);
        if(terrain)terrain.treeDistance=distance;if(camera)camera.transform.SetPositionAndRotation(position,rotation);if(movement)movement.enabled=moving;
        Application.runInBackground=background;Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;EditorApplication.isPaused=paused;
        batches.Dispose();triangles.Dispose();gpu.Dispose();main.Dispose();if(game&&sizeProperty!=null)sizeProperty.SetValue(game,oldSize);
        bool exact=materialJson.All(p=>p.Key&&EditorJsonUtility.ToJson(p.Key)==p.Value&&!EditorUtility.IsDirty(p.Key));
        string result=(phase>=modes.Length?"COMPLETE":"STOPPED")+"; material serialization restored="+exact+"; camera/control/treeDistance/settings restored";
        File.WriteAllText(root+"/status.txt",result);return result;
    }
}
