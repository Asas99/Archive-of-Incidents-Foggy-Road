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

// Fixed-scene diagnostic prototype, not a production registry for spawned decals.
public static class DecalVisibilityTrial
{
    static readonly string root="Recovery/OptimizationSearch_20261113/Visibility";
    static readonly string[] modes={"Baseline","VisibilityGate","BaselineRepeat","VisibilityGateRepeat"};
    static readonly float[] yaws={0,180};
    static DecalProjector[] projectors;static ScriptableRendererFeature feature;static string featureJson;static bool featureActive;
    static Camera camera;static MonoBehaviour movement;static Vector3 position;static Quaternion rotation;static bool moving,background,running,captureOnly;
    static int phase=-1,last=-1,rate,vsync,oldSize,captureFrame=-1;static float timeScale;
    static EditorWindow game;static PropertyInfo sizeProperty;
    static readonly Plane[] planes=new Plane[6];static readonly Stack<bool> cameraStates=new Stack<bool>();
    static readonly List<double> frames=new List<double>(),gpus=new List<double>();static ProfilerRecorder gpu,batch,tri;static double began,batches,triangles;static int visible;
    public static string Main(){return Start(false);}
    public static string Capture(){return Start(true);}
    static string Start(bool captures)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play required");captureOnly=captures;
        feature=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset").rendererFeatures.First(f=>f is DecalRendererFeature);
        if(EditorUtility.IsDirty(feature))throw new Exception("Dirty feature");featureJson=EditorJsonUtility.ToJson(feature);featureActive=feature.isActive;
        projectors=UnityEngine.Object.FindObjectsByType<DecalProjector>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        camera=UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(p=>p.GetComponent<Camera>()).First();position=camera.transform.position;rotation=camera.transform.rotation;
        movement=camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x.GetType().Name=="KarakterHareket");moving=movement&&movement.enabled;if(movement)movement.enabled=false;
        background=Application.runInBackground;rate=Application.targetFrameRate;vsync=QualitySettings.vSyncCount;timeScale=Time.timeScale;
        Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;if(captureOnly)Time.timeScale=0;
        var t=typeof(Editor).Assembly.GetType("UnityEditor.GameView");game=EditorWindow.GetWindow(t);sizeProperty=t.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);oldSize=(int)sizeProperty.GetValue(game);sizeProperty.SetValue(game,3);game.Focus();
        gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time",1);batch=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);tri=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);
        Directory.CreateDirectory(root);File.WriteAllText(root+(captures?"/Capture.csv":"/Timing.csv"),"view,mode,visible_decals,frames,width,height,median_ms,gpu_median_ms,batches,triangles\n");
        running=true;RenderPipelineManager.beginFrameRendering+=Before;RenderPipelineManager.beginCameraRendering+=BeginCamera;RenderPipelineManager.endCameraRendering+=EndCamera;RenderPipelineManager.endFrameRendering+=After;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Abort;EditorApplication.update+=Watchdog;Next();return "Started conservative decal visibility "+(captures?"frozen-time image capture":"timing trial");
    }
    static Bounds BoundsOf(DecalProjector p)
    {
        var m=Matrix4x4.TRS(p.transform.position,p.transform.rotation,p.scaleMode==DecalScaleMode.InheritFromHierarchy?p.transform.lossyScale:Vector3.one);var e=p.size*.5f;
        var x=m.MultiplyVector(new Vector3(e.x,0,0));var y=m.MultiplyVector(new Vector3(0,e.y,0));var z=m.MultiplyVector(new Vector3(0,0,e.z));
        return new Bounds(m.MultiplyPoint3x4(p.pivot),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2+Vector3.one);
    }
    static void Before(ScriptableRenderContext c,Camera[] cameras){if(running)camera.transform.SetPositionAndRotation(new Vector3(509.513f,2.235f,1248.762f),Quaternion.Euler(0,yaws[phase/4],0));}
    static void BeginCamera(ScriptableRenderContext c,Camera cam)
    {
        if(!running)return;cameraStates.Push(feature.isActive);
        if(cam!=camera)return;
        GeometryUtility.CalculateFrustumPlanes(cam,planes);visible=0;
        foreach(var p in projectors)if(p&&p.isActiveAndEnabled&&p.material&&GeometryUtility.TestPlanesAABB(planes,BoundsOf(p)))visible++;
        feature.SetActive(featureActive&&(!(phase%2==1)||visible>0));
    }
    static void EndCamera(ScriptableRenderContext c,Camera cam){if(running&&cameraStates.Count>0)feature.SetActive(cameraStates.Pop());}
    static void After(ScriptableRenderContext c,Camera[] cameras)
    {
        if(!running||last==Time.frameCount||!cameras.Contains(camera))return;last=Time.frameCount;
        try{
            double elapsed=Time.realtimeSinceStartupAsDouble-began;if(elapsed<(captureOnly?2:4))return;
            frames.Add(Time.unscaledDeltaTime*1000);if(gpu.Valid&&gpu.LastValue>0)gpus.Add(gpu.LastValue/1e6);batches+=batch.LastValue;triangles+=tri.LastValue;
            if(captureOnly){if(captureFrame<0){ScreenCapture.CaptureScreenshot(Path.GetFullPath(root+"/View"+(phase/4)+"_"+modes[phase%4]+".png"));captureFrame=Time.frameCount;return;}if(captureFrame+2>Time.frameCount)return;}
            else if(elapsed<10||frames.Count<80)return;
            frames.Sort();gpus.Sort();int n=frames.Count;string N(double x)=>x.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            File.AppendAllText(root+(captureOnly?"/Capture.csv":"/Timing.csv"),string.Join(",",phase/4,modes[phase%4],visible,n,Screen.width,Screen.height,N(frames[n/2]),N(gpus.Count>0?gpus[gpus.Count/2]:-1),N(batches/n),N(triangles/n))+"\n");Next();
        }catch(Exception e){File.WriteAllText(root+"/error.txt",e.ToString());Stop();}
    }
    static void Next(){phase++;if(phase>=8){Stop();return;}feature.SetActive(featureActive);frames.Clear();gpus.Clear();batches=triangles=0;captureFrame=-1;began=Time.realtimeSinceStartupAsDouble;File.WriteAllText(root+"/status.txt",(captureOnly?"Capture":"Timing")+" view="+(phase/4)+" "+modes[phase%4]);}
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode)Stop();}
    static void Abort(){Stop();}
    static void Watchdog(){if(running&&(!EditorApplication.isPlaying||EditorApplication.isPaused||Time.realtimeSinceStartupAsDouble-began>60))Stop();}
    public static string Stop()
    {
        running=false;RenderPipelineManager.beginFrameRendering-=Before;RenderPipelineManager.beginCameraRendering-=BeginCamera;RenderPipelineManager.endCameraRendering-=EndCamera;RenderPipelineManager.endFrameRendering-=After;EditorApplication.playModeStateChanged-=State;AssemblyReloadEvents.beforeAssemblyReload-=Abort;EditorApplication.update-=Watchdog;cameraStates.Clear();
        feature.SetActive(featureActive);EditorJsonUtility.FromJsonOverwrite(featureJson,feature);EditorUtility.ClearDirty(feature);if(camera)camera.transform.SetPositionAndRotation(position,rotation);if(movement)movement.enabled=moving;
        Application.runInBackground=background;Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;Time.timeScale=timeScale;if(game)sizeProperty.SetValue(game,oldSize);gpu.Dispose();batch.Dispose();tri.Dispose();
        string result=(phase>=8?"COMPLETE":"STOPPED")+"; feature restored="+(EditorJsonUtility.ToJson(feature)==featureJson&&!EditorUtility.IsDirty(feature))+"; camera/settings/timeScale restored";File.WriteAllText(root+"/status.txt",result);return result;
    }
}
