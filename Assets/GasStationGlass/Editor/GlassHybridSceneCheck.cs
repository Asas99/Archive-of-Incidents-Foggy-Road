using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class GlassHybridSceneCheck
{
    static GlassHybridSceneCheck() { EditorApplication.update+=Requests; }
    static void Requests()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Library/GlassHybridSceneCheck.request") || File.Exists("Library/GlassHybridInstall.request")) return;
        File.Delete("Library/GlassHybridSceneCheck.request");
        try { CheckScene(); } catch(Exception e) { File.AppendAllText("Library/GlassHybridSceneCheck.txt","\nFAIL: "+e); UnityEngine.Debug.LogException(e); }
    }
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Validate Hybrid and Compare 1080p")]
    public static void CheckScene()
    {
        File.WriteAllText("Library/GlassHybridSceneCheck.txt","RUNNING\n");
        var manager=UnityEngine.Object.FindFirstObjectByType<GasStationPlanarGlass>();
        var surfaces=UnityEngine.Object.FindObjectsByType<GlassSurfaceVariation>(FindObjectsSortMode.None).Where(p=>p.gameObject.scene==manager.gameObject.scene).ToArray();
        var report=new StringBuilder();
        void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); report.AppendLine("PASS "+message); }
        Require(surfaces.Length>0,"Scene glass inventory: "+surfaces.Length);
        Require(surfaces.Select(p=>p.StableId).Distinct().Count()==surfaces.Length,"All glass identities are unique");
        Require(surfaces.Select(p=>p.SurfaceVariant).Distinct().Count()==surfaces.Length,"All current panes use different base surface variants");
        var array=surfaces[0].Surface.sharedMaterial.GetTexture("_SurfaceVariants") as Texture2DArray;
        Require(array && array.depth==12 && array.width==1024 && array.format==TextureFormat.BC7 && !array.isReadable,"Shared BC7 array is correctly bound, 12x1024 and CPU-unreadable");
        var actors=UnityEngine.Object.FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.gameObject.scene==manager.gameObject.scene).ToArray();
        report.AppendLine("Scene actor components: "+actors.Length);
        foreach(var actor in actors) report.AppendLine("Actor "+actor.name+" active="+actor.gameObject.activeInHierarchy);
        foreach(var group in manager.groups) Require(group.bakedPositive && group.bakedNegative && group.bakedPositive.customBakedTexture && group.bakedNegative.customBakedTexture,"Both-side baked bindings: "+group.name);
        var modes=manager.reflectionMode; bool occlusion=manager.testActorOcclusion;
        var activeActorStates=actors.Select(a=>a.enabled).ToArray();
        var root=new GameObject("Hybrid Scene Verification") {hideFlags=HideFlags.HideAndDontSave};
        var camera=root.AddComponent<Camera>(); camera.enabled=false;
        var original=manager.ResolvePlayerCamera(); if(original) camera.CopyFrom(original);
        camera.enabled=false; camera.fieldOfView=55; camera.aspect=16f/9; camera.nearClipPlane=0.05f;
        var data=camera.GetUniversalAdditionalCameraData(); data.SetRenderer(0); data.renderPostProcessing=false;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf); target.Create(); camera.targetTexture=target;
        var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
        var marker=GameObject.CreatePrimitive(PrimitiveType.Cube); marker.name="Hybrid verification actor"; marker.hideFlags=HideFlags.HideAndDontSave;
        marker.transform.localScale=Vector3.one; var reflectedActor=ReflectionActor.Register(marker);
        var tick=typeof(GasStationPlanarGlass).GetMethod("TickWithClock",BindingFlags.Instance|BindingFlags.NonPublic);
        double now=Time.realtimeSinceStartupAsDouble+100; int frame=100000;
        void Step()
        { now+=1.0/60; tick.Invoke(manager,new object[]{camera,true,now,frame++}); }
        void Render()
        {
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            var previous=RenderTexture.active; RenderTexture.active=target;
            pixel.ReadPixels(new Rect(0,0,1,1),0,0); pixel.Apply(); RenderTexture.active=previous;
        }
        try
        {
            foreach(var actor in actors) actor.enabled=false;
            var group=manager.groups.OrderByDescending(g=>g.windows.Where(w=>w).Sum(w=>w.bounds.size.magnitude)).First();
            var point=group.plane.TransformPoint(group.localPoint);
            var normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
            camera.transform.SetPositionAndRotation(point+normal*6,Quaternion.LookRotation(-normal,Vector3.up));
            marker.transform.position=point+normal*10; manager.testActorOcclusion=false;
            foreach(var label in new[]{"Realtime","Baked","HybridEmpty","HybridActive"})
            {
                manager.reflectionMode=label=="Realtime" ? GasStationPlanarGlass.ReflectionMode.Realtime : label=="Baked" ? GasStationPlanarGlass.ReflectionMode.Baked : GasStationPlanarGlass.ReflectionMode.Hybrid;
                marker.SetActive(label=="HybridActive"); reflectedActor.enabled=label=="HybridActive";
                if(label=="HybridActive") ReflectionActor.Register(marker); // Explicit edit-mode lifecycle.
                manager.InvalidateReflections();
                // Let state transitions settle. Two rendered warmup frames populate renderer caches.
                for(int i=0;i<130;i++) Step(); Render(); Step(); Render();
                long captures=manager.TotalCaptures; var samples=new List<double>();
                for(int sample=0;sample<24;sample++)
                {
                    var timer=Stopwatch.StartNew(); Step(); Render(); timer.Stop(); samples.Add(timer.Elapsed.TotalMilliseconds);
                }
                samples.Sort(); long added=manager.TotalCaptures-captures;
                report.AppendLine(label+": synchronous render + 1px GPU readback ms mean="+samples.Average().ToString("F2")+" p95="+samples[(int)(samples.Count*0.95)].ToString("F2")+" worst="+samples.Last().ToString("F2")+" planar captures in 24 simulated 60Hz frames="+added);
                if(label=="Baked" || label=="HybridEmpty") Require(added==0,label+" submits zero planar captures");
                if(label=="HybridActive")
                {
                    report.AppendLine("Activity diagnostic: registered="+manager.RegisteredActors+" activeGroups="+manager.ActiveGroups+" reason="+manager.LastActivityReason+" actor bounds="+reflectedActor.TryGetBounds(manager.reflectionLayers.value,now,out var actorBounds,out var lightEffect)+" "+actorBounds);
                    File.WriteAllText("Library/GlassHybridSceneCheck.txt",report.ToString());
                    Require(added>0 && added<=24,"Hybrid active capture budget is respected");
                }
                var image=new Texture2D(1920,1080,TextureFormat.RGBA32,false); var previous=RenderTexture.active; RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply(); RenderTexture.active=previous;
                File.WriteAllBytes("Library/GlassHybridScene_"+label+".png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
            }
            // Check all current pane MPBs after the transitions, rather than just serialized identities.
            foreach(var pane in surfaces)
            {
                var block=new MaterialPropertyBlock(); pane.Surface.GetPropertyBlock(block);
                Require(block.GetFloat("_VariationEnabled")==1 && Mathf.RoundToInt(block.GetFloat("_SurfaceVariant"))==pane.SurfaceVariant,"Runtime surface parameters: "+pane.name);
            }
            var errors=ShaderUtil.GetShaderMessages(surfaces[0].Surface.sharedMaterial.shader).Where(m=>m.severity.ToString()=="Error").ToArray();
            Require(errors.Length==0,"No shader compilation errors");
            report.AppendLine("Timings above are offscreen synchronous throughput including GPU readback, not separate CPU/GPU timings or measured Game View FPS. Synthetic 60Hz time is used only for deterministic scheduling.");
            FrameTimingManager.CaptureFrameTimings(); var timings=new FrameTiming[1]; uint available=FrameTimingManager.GetLatestTimings(1,timings);
            report.AppendLine("Separate frame timing availability: "+available+"; GPU ms="+(available>0 ? timings[0].gpuFrameTime : 0)+". Editor aggregate triangle/batch counters are not attributed to these samples; use the Unity Profiler for a same-route runtime CPU/GPU/triangles comparison.");
            File.WriteAllText("Library/GlassHybridSceneCheck.txt",report.ToString());
        }
        finally
        {
            manager.reflectionMode=modes; manager.testActorOcclusion=occlusion;
            for(int i=0;i<actors.Length;i++) if(actors[i]) actors[i].enabled=activeActorStates[i];
            UnityEngine.Object.DestroyImmediate(marker); camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(pixel);
            manager.InvalidateReflections();
            manager.SendMessage("LateUpdate");
        }
    }
}
