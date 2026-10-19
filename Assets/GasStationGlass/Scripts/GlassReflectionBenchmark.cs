using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Only attached to a temporary Development Build snapshot.</summary>
public sealed class GlassReflectionBenchmark:MonoBehaviour
{
    public GasStationPlanarGlass manager;
    public Transform pane;
    public ReflectionActor testActor;
    public UniversalRendererData dynamicRenderer,mainRenderer;
    [Serializable] public class Row
    {
        public string stage;public int repeat,samples;public double meanFrameMs,p95FrameMs,meanMainThreadMs,meanRenderThreadMs,meanGpuMs,triangles,batches;
        public long captures;public bool gpuResidentRunning;public List<Segment> segments=new List<Segment>();
    }
    [Serializable] public class Segment {public string scenario;public int samples;public double meanFrameMs,p95FrameMs;public long captures;}
    [Serializable] public class Results {public string device;public int width,height;public string[] unavailable;public string methodology="10 seconds warmup and 30 seconds measured per run; identical time-based approach route, four actor scenarios; three repeats per stage";public List<Row> rows=new List<Row>();}
    readonly Results results=new Results();readonly List<double> frameTimes=new List<double>(),mainTimes=new List<double>(),renderTimes=new List<double>(),gpuTimes=new List<double>(),tris=new List<double>(),batches=new List<double>();
    readonly List<double>[] segmentTimes={new List<double>(),new List<double>(),new List<double>(),new List<double>()};readonly long[] segmentCaptures=new long[4];
    ProfilerRecorder main,render,tri,batch;readonly FrameTiming[] timing=new FrameTiming[1];
    Camera camera;Vector3 point,normal,actorPosition;ReflectionActor[] actors;bool[] actorStates;int phase=-1,scenario=-1;double started;long captures,segmentStart;bool measuring;
    static readonly string[] scenarios={"NoActors","DoorsOnly","ParkedCar","MovingCar"};
    ProfilerRecorder Recorder(ProfilerCategory category,string name){try{return ProfilerRecorder.StartNew(category,name,1);}catch{return default;}}
    static bool ResidentRunning()=>typeof(GPUResidentDrawer).GetProperty("instance",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null)!=null;
    void Start()
    {
        Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Screen.SetResolution(1920,1080,false);
        foreach(var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(behaviour!=this&&!(behaviour is GasStationPlanarGlass)&&!(behaviour is ReflectionActor)&&!(behaviour is GlassSurfaceVariation))behaviour.enabled=false;
        foreach(var body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))body.isKinematic=true;
        camera=manager.ResolvePlayerCamera();if(!camera||!testActor||!dynamicRenderer){Fail("Missing camera / real car / renderer");return;}
        foreach(var other in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(other!=camera)other.enabled=false;
        ReflectionActor.DiscoverTaggedActors(Time.realtimeSinceStartupAsDouble);
        if(mainRenderer)mainRenderer.renderingMode=RenderingMode.ForwardPlus;
        actors=FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None);actorStates=actors.Select(a=>a.enabled).ToArray();
        var variation=pane.GetComponent<GlassSurfaceVariation>();point=pane.TransformPoint(variation.LocalPoint);normal=-pane.localToWorldMatrix.inverse.transpose.MultiplyVector(variation.LocalNormal).normalized;
        testActor.gameObject.SetActive(true);var renderers=testActor.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);testActor.transform.position+=point+normal*18-bounds.center;testActor.RefreshRenderers();actorPosition=testActor.transform.position;
        manager.testActorOcclusion=false;manager.previewInSceneView=false;manager.captureContent=GasStationPlanarGlass.CaptureContent.DynamicObjects;manager.maxActiveGroups=1;manager.liveUpdatesPerSecond=60;
        main=Recorder(ProfilerCategory.Internal,"Main Thread");render=Recorder(ProfilerCategory.Internal,"Render Thread");tri=Recorder(ProfilerCategory.Render,"Triangles Count");batch=Recorder(ProfilerCategory.Render,"Batches Count");results.device=SystemInfo.graphicsDeviceName;Next();
    }
    static double Mean(List<double> a)=>a.Count==0?-1:a.Average();
    static double P95(List<double> a){if(a.Count==0)return -1;var sorted=a.OrderBy(v=>v).ToArray();return sorted[Math.Min(sorted.Length-1,(int)(sorted.Length*.95))];}
    void Next()
    {
        if(phase>=0)
        {
            if(scenario>=0)segmentCaptures[scenario]+=manager.TotalCaptures-segmentStart;
            var row=new Row{stage=new[]{"Current","ForwardPlusOnly","Optimized"}[phase%3],repeat=phase/3,samples=frameTimes.Count,meanFrameMs=Mean(frameTimes),p95FrameMs=P95(frameTimes),meanMainThreadMs=Mean(mainTimes),meanRenderThreadMs=Mean(renderTimes),meanGpuMs=Mean(gpuTimes),triangles=Mean(tris),batches=Mean(batches),captures=manager.TotalCaptures-captures,gpuResidentRunning=ResidentRunning()};
            for(int i=0;i<4;i++)row.segments.Add(new Segment{scenario=scenarios[i],samples=segmentTimes[i].Count,meanFrameMs=Mean(segmentTimes[i]),p95FrameMs=P95(segmentTimes[i]),captures=segmentCaptures[i]});results.rows.Add(row);Save();
        }
        phase++;if(phase>=9){Application.Quit();return;}
        dynamicRenderer.renderingMode=phase%3==0?RenderingMode.Forward:RenderingMode.ForwardPlus;typeof(GPUResidentDrawer).GetMethod("Reinitialize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        manager.transmissionQuality=phase%3==2?GasStationPlanarGlass.TransmissionQuality.Balanced:GasStationPlanarGlass.TransmissionQuality.High;
        manager.actorEnterDistance=phase%3==2?20:100000;manager.actorExitDistance=phase%3==2?22:100000;manager.reflectionSceneDepth=phase%3==2?25:100000;
        manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Hybrid;manager.InvalidateReflections();scenario=-1;measuring=false;started=Time.realtimeSinceStartupAsDouble;
        frameTimes.Clear();mainTimes.Clear();renderTimes.Clear();gpuTimes.Clear();tris.Clear();batches.Clear();for(int i=0;i<4;i++){segmentTimes[i].Clear();segmentCaptures[i]=0;}
        SetScenario(0);Debug.Log("Glass benchmark stage "+phase+" started");
    }
    void SetScenario(int next)
    {
        if(next==scenario)return;if(measuring&&scenario>=0)segmentCaptures[scenario]+=manager.TotalCaptures-segmentStart;scenario=next;
        for(int i=0;i<actors.Length;i++)if(actors[i])actors[i].enabled=actorStates[i]&&next>0&&actors[i].GetComponent<TwoSidedDoor>();
        testActor.enabled=next>=2;if(testActor.enabled)ReflectionActor.Register(testActor.gameObject);
        manager.InvalidateReflections();segmentStart=manager.TotalCaptures;
    }
    void Update()
    {
        if(phase>=9||!camera)return;double elapsed=Time.realtimeSinceStartupAsDouble-started;
        if(elapsed>=40){Next();return;}
        if(elapsed>=10&&!measuring){measuring=true;captures=manager.TotalCaptures;segmentStart=captures;}
        float route=(float)(Math.Max(0,elapsed-10)%30);SetScenario(measuring?Math.Min(3,(int)(route/7.5f)):0);
        if(measuring)
        {
            double ms=Time.unscaledDeltaTime*1000;frameTimes.Add(ms);segmentTimes[scenario].Add(ms);if(main.Valid)mainTimes.Add(main.LastValue/1000000.0);if(render.Valid)renderTimes.Add(render.LastValue/1000000.0);if(tri.Valid)tris.Add(tri.LastValue);if(batch.Valid)batches.Add(batch.LastValue);
            if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0)gpuTimes.Add(timing[0].gpuFrameTime);
        }
        FrameTimingManager.CaptureFrameTimings();
        float local=route%7.5f;var tangent=Vector3.Cross(Vector3.up,normal);float distance=Mathf.Lerp(8,1.2f,(Mathf.Sin(local/7.5f*Mathf.PI*2-Mathf.PI*.5f)+1)*.5f);var position=point+normal*distance+tangent*(Mathf.Sin(local)*.3f);
        if(scenario!=2||local<5)camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(point-position,Vector3.up));
        testActor.transform.position=actorPosition+(scenario==3?tangent*(Mathf.Sin(local*1.2f)*4):Vector3.zero);
    }
    void Save()
    {
        results.width=Screen.width;results.height=Screen.height;var unavailable=new List<string>();if(!main.Valid)unavailable.Add("Main Thread");if(!render.Valid)unavailable.Add("Render Thread");if(!tri.Valid)unavailable.Add("Triangles Count");if(!batch.Valid)unavailable.Add("Batches Count");if(results.rows.All(r=>r.meanGpuMs<=0))unavailable.Add("GPU FrameTiming");results.unavailable=unavailable.ToArray();File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"Results.json"),JsonUtility.ToJson(results,true));
    }
    void Fail(string message){File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"Error.txt"),message);Application.Quit(1);}
    void OnDestroy(){main.Dispose();render.Dispose();tri.Dispose();batch.Dispose();}
}
