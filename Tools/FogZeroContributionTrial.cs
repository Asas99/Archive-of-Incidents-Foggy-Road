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

public static class FogZeroContributionTrial
{
    const string Root="Recovery/FogZeroContribution_20261114";
    static readonly string[] Modes={"Original","Optimized","OriginalRepeat","OptimizedRepeat"};
    static readonly Vector3[] Positions={new Vector3(509.513f,2.235f,1248.762f),new Vector3(440,2.25f,902)};
    static Material source;static Shader original,optimized;static string snapshot,originalSource,optimizedSource,sourcePath;
    static Camera camera;static MonoBehaviour movement;static Vector3 position;static Quaternion rotation;static bool moving,background,asyncCompilation,running,capture;
    static int phase=-1,last=-1,rate,vsync,size,captureFrame=-1;static float timeScale;static PropertyInfo sizeProperty;static EditorWindow game;
    static ProfilerRecorder gpu;static double began;static readonly List<double> times=new List<double>(),gpuTimes=new List<double>();
    public static string Main(){return Start(false);}
    public static string Capture(){return Start(true);}
    static string GuardNoise(string code,string startToken,string endToken,string fieldToken,string fadeName,string maskName,string strength)
    {
        int start=code.IndexOf(startToken,StringComparison.Ordinal),end=code.IndexOf(endToken,start,StringComparison.Ordinal);
        if(start<0||end<0)throw new Exception("Fog source guard anchors changed");
        string block=code.Substring(start,end-start);int fadeAt=block.IndexOf("                float "+fadeName+" =",StringComparison.Ordinal);int fieldAt=block.IndexOf(fieldToken,StringComparison.Ordinal);
        string fade,body;
        if(fadeAt==0){fade=block.Substring(0,fieldAt);body=block.Substring(fieldAt);}
        else{int maskAt=block.IndexOf("                float "+maskName+" =",fadeAt,StringComparison.Ordinal);fade=block.Substring(fadeAt,maskAt-fadeAt);body=block.Remove(fadeAt,maskAt-fadeAt);}
        body=body.Replace("float "+maskName+" =",maskName+" =").TrimEnd();
        string guarded=fade+"                float "+maskName+" = 1.0;\n"+
            "                // This noise cannot contribute when its exact blend weight is zero.\n"+
            "                [branch] if ("+fadeName+" * "+strength+" != 0.0)\n                {\n"+
            string.Join("\n",body.Split('\n').Select(line=>"    "+line))+"\n                }\n\n";
        return code.Remove(start,end-start).Insert(start,guarded);
    }
    static string Start(bool screenshots)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");capture=screenshots;
        source=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset").rendererFeatures.OfType<ReducedResolutionFogFeature>().Single().fogMaterial;
        if(EditorUtility.IsDirty(source))throw new Exception("Fog material has unsaved changes");
        snapshot=EditorJsonUtility.ToJson(source);original=source.shader;sourcePath=AssetDatabase.GetAssetPath(original);originalSource=File.ReadAllText(sourcePath).Replace("\r\n","\n");
        optimizedSource=GuardNoise(originalSource,"                float3 zonePosition =","                // Keep the far road","unused","zoneFade","zoneMask","_FogZoneBreakup");
        optimizedSource=GuardNoise(optimizedSource,"                float distantFade =","                // Keep only a faint","                float distantField =","distantFade","distantBreakup","_DistantBreakup");
        Directory.CreateDirectory(Root);File.WriteAllText(Root+"/Original.shader.txt",originalSource);File.WriteAllText(Root+"/Optimized.shader.txt",optimizedSource);
        asyncCompilation=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
        try{optimized=ShaderUtil.CreateShaderAsset(optimizedSource.Replace("Shader \""+original.name+"\"","Shader \""+original.name+" Exact Noise Trial\""),false);optimized.hideFlags=HideFlags.HideAndDontSave;if(ShaderUtil.ShaderHasError(optimized))throw new Exception("Optimized fog shader did not compile");}
        catch{ShaderUtil.allowAsyncCompilation=asyncCompilation;if(optimized)UnityEngine.Object.DestroyImmediate(optimized);throw;}
        camera=UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(x=>x.GetComponent<Camera>()).First();position=camera.transform.position;rotation=camera.transform.rotation;
        movement=camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x.GetType().Name=="KarakterHareket");moving=movement&&movement.enabled;if(movement)movement.enabled=false;
        background=Application.runInBackground;rate=Application.targetFrameRate;vsync=QualitySettings.vSyncCount;timeScale=Time.timeScale;Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;if(capture)Time.timeScale=0;
        var t=typeof(Editor).Assembly.GetType("UnityEditor.GameView");game=EditorWindow.GetWindow(t);sizeProperty=t.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);size=(int)sizeProperty.GetValue(game);sizeProperty.SetValue(game,3);game.Focus();
        gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time",1);File.WriteAllText(Root+(capture?"/Capture.csv":"/Timing.csv"),"view,mode,frames,median_ms,p95_ms,gpu_median_ms\n");
        running=true;RenderPipelineManager.beginFrameRendering+=Before;RenderPipelineManager.endFrameRendering+=After;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Abort;EditorApplication.update+=Watchdog;Next();return "Started exact zero-weight fog-noise "+(capture?"image comparison":"timing comparison")+"; original material and settings auto-restored";
    }
    static void Before(ScriptableRenderContext c,Camera[] cameras){if(running)camera.transform.SetPositionAndRotation(Positions[phase/4],Quaternion.Euler(0,phase/4==0?180:0,0));}
    static void After(ScriptableRenderContext c,Camera[] cameras)
    {
        if(!running||last==Time.frameCount||!cameras.Contains(camera))return;last=Time.frameCount;
        try{
            double elapsed=Time.realtimeSinceStartupAsDouble-began;if(elapsed<(capture?2:5))return;
            times.Add(Time.unscaledDeltaTime*1000);if(gpu.Valid&&gpu.LastValue>0)gpuTimes.Add(gpu.LastValue/1e6);
            if(capture){if(captureFrame<0){ScreenCapture.CaptureScreenshot(Path.GetFullPath(Root+"/View"+(phase/4)+"_"+Modes[phase%4]+".png"));captureFrame=Time.frameCount;return;}if(Time.frameCount<captureFrame+2)return;}
            else if(elapsed<10||times.Count<100)return;
            times.Sort();gpuTimes.Sort();int n=times.Count;string N(double v)=>v.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            File.AppendAllText(Root+(capture?"/Capture.csv":"/Timing.csv"),string.Join(",",phase/4,Modes[phase%4],n,N(times[n/2]),N(times[Math.Min(n-1,(int)(n*.95))]),N(gpuTimes.Count>0?gpuTimes[gpuTimes.Count/2]:-1))+"\n");Next();
        }catch(Exception e){File.WriteAllText(Root+"/error.txt",e.ToString());Stop();}
    }
    static void Next(){phase++;if(phase>=8){Stop();return;}source.shader=phase%2==1?optimized:original;times.Clear();gpuTimes.Clear();captureFrame=-1;began=Time.realtimeSinceStartupAsDouble;File.WriteAllText(Root+"/status.txt",(capture?"Capture":"Timing")+" view="+(phase/4)+" "+Modes[phase%4]);}
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode)Stop();}
    static void Abort(){Stop();}
    static void Watchdog(){if(running&&(!EditorApplication.isPlaying||EditorApplication.isPaused||Time.realtimeSinceStartupAsDouble-began>90))Stop();}
    public static string Stop()
    {
        running=false;RenderPipelineManager.beginFrameRendering-=Before;RenderPipelineManager.endFrameRendering-=After;EditorApplication.playModeStateChanged-=State;AssemblyReloadEvents.beforeAssemblyReload-=Abort;EditorApplication.update-=Watchdog;
        EditorJsonUtility.FromJsonOverwrite(snapshot,source);EditorUtility.ClearDirty(source);if(camera)camera.transform.SetPositionAndRotation(position,rotation);if(movement)movement.enabled=moving;Application.runInBackground=background;Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;Time.timeScale=timeScale;ShaderUtil.allowAsyncCompilation=asyncCompilation;if(game)sizeProperty.SetValue(game,size);gpu.Dispose();if(optimized)UnityEngine.Object.DestroyImmediate(optimized);
        string r=(phase>=8?"COMPLETE":"STOPPED")+"; original material restored="+(EditorJsonUtility.ToJson(source)==snapshot&&!EditorUtility.IsDirty(source))+"; settings restored";File.WriteAllText(Root+"/status.txt",r);return r;
    }
}
