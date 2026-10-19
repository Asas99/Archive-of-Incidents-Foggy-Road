using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class GlassRoomSetup
{
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Verify All Room Reflections")]
    public static void Verify()
    {
        Directory.CreateDirectory(Output);var panes=Panes();var manager=UnityEngine.Object.FindFirstObjectByType<GasStationPlanarGlass>();var report=new StringBuilder();
        void Require(bool ok,string message){report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Output+"/Verification.txt",report.ToString());if(!ok)throw new InvalidOperationException(message);}
        Require(panes.Length==10,"All ten panes inventoried");
        foreach(var pane in panes)
        {
            var binding=pane.GetComponent<GlassReflectionBinding>();Require(binding&&binding.Verified,pane.name+" persistent identity and both explicit sources valid");
            Require(!binding.positive.enabled&&!binding.negative.enabled,pane.name+" probes isolated from ordinary environment assignment");
            var p=pane.transform.TransformPoint(pane.LocalPoint);var n=pane.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(pane.LocalNormal).normalized;
            Require(Vector3.Dot(binding.positive.transform.position-p,n)>0&&Vector3.Dot(binding.negative.transform.position-p,n)<0,pane.name+" captures lie on their assigned sides");
            report.AppendLine("BIND "+pane.name+" +="+binding.positiveRegion+" -="+binding.negativeRegion+" position="+p+" normal="+n);
        }
        var actors=UnityEngine.Object.FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None);var states=actors.Select(a=>a.enabled).ToArray();
        var oldMode=manager.reflectionMode;bool oldOcclusion=manager.testActorOcclusion;
        var root=new GameObject("Glass Room Verification"){hideFlags=HideFlags.HideAndDontSave};var camera=root.AddComponent<Camera>();var original=manager.ResolvePlayerCamera();if(original)camera.CopyFrom(original);
        camera.enabled=false;camera.targetTexture=null;camera.cullingMask&=~(1<<30);camera.nearClipPlane=0.05f;camera.aspect=16f/9;camera.fieldOfView=55;camera.GetUniversalAdditionalCameraData().SetRenderer(0);
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf);target.Create();camera.targetTexture=target;
        var pixels=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
        var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="Reflection validation marker";marker.hideFlags=HideFlags.HideInHierarchy;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker,manager.gameObject.scene);marker.layer=30;marker.transform.localScale=Vector3.one*0.7f;
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",Color.magenta*3);marker.GetComponent<Renderer>().sharedMaterial=material;
        var actor=ReflectionActor.Register(marker);var tick=typeof(GasStationPlanarGlass).GetMethod("TickWithClock",BindingFlags.Instance|BindingFlags.NonPublic);double now=Time.realtimeSinceStartupAsDouble+100;int frame=100000;
        void Step(){now+=1.0/60;tick.Invoke(manager,new object[]{camera,true,now,frame++});Require(manager.CapturesThisTick<=1&&manager.ActiveGroups<=2,"Hybrid global capture/group budget");}
        void Capture(string name)
        {
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});var prior=RenderTexture.active;RenderTexture.active=target;
            try{pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Output+"/"+name+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=prior;}
        }
        try
        {
            foreach(var a in actors)a.enabled=false;
            foreach(var pane in panes)
            {
                var point=pane.transform.TransformPoint(pane.LocalPoint);var normal=pane.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(pane.LocalNormal).normalized;
                var tangent=Vector3.Cross(Vector3.up,normal).normalized;
                foreach(int side in new[]{1,-1})foreach(bool oblique in new[]{false,true})
                {
                    camera.transform.SetPositionAndRotation(point+normal*side*4+tangent*(oblique?1.8f:0),Quaternion.LookRotation(-normal*side*4-tangent*(oblique?1.8f:0),Vector3.up));
                    // Reconstruct the previous nearest-side selection for a same-camera visual baseline.
                    marker.SetActive(false);actor.enabled=false;manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Baked;
                    var previous=manager.groups.Select(g=>(g,g.bakedPositive,g.bakedNegative)).ToArray();
                    var legacy=manager.GetComponentsInChildren<ReflectionProbe>(true).Where(p=>p.name.StartsWith("Hybrid Region ")&&p.customBakedTexture).ToArray();
                    if(legacy.Length>0)
                    {
                        foreach(var group in manager.groups)
                        {
                            var gp=group.plane.TransformPoint(group.localPoint);var gn=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
                            ReflectionProbe Pick(int sign)=>legacy.OrderBy(p=>(p.transform.position-gp).sqrMagnitude+(Vector3.Dot(p.transform.position-gp,gn)*sign<0.05f?1000000:0)).First();
                            group.bakedPositive=Pick(1);group.bakedNegative=Pick(-1);
                        }
                        manager.InvalidateReflections();Step();Capture(pane.name.Replace(' ','_')+"_"+side+"_"+(oblique?"Oblique":"Straight")+"_Before");
                    }
                    foreach(var old in previous){old.g.bakedPositive=old.bakedPositive;old.g.bakedNegative=old.bakedNegative;}
                    foreach(string label in new[]{"Baked","HybridEmpty","HybridActive"})
                    {
                        marker.SetActive(label=="HybridActive");actor.enabled=label=="HybridActive";
                        marker.transform.position=point+normal*side*2.5f;actor.RefreshRenderers();
                        if(label=="HybridActive")ReflectionActor.Register(marker);
                        manager.testActorOcclusion=false;manager.reflectionMode=label=="Baked"?GasStationPlanarGlass.ReflectionMode.Baked:GasStationPlanarGlass.ReflectionMode.Hybrid;manager.InvalidateReflections();
                        long before=manager.TotalCaptures;var sw=Stopwatch.StartNew();for(int i=0;i<32;i++)Step();
                        Capture(pane.name.Replace(' ','_')+"_"+side+"_"+(oblique?"Oblique":"Straight")+"_"+label);                        if(label=="Baked"||label=="HybridActive")
                        {
                            var savedBlock=new MaterialPropertyBlock();pane.Surface.GetPropertyBlock(savedBlock);var debugBlock=new MaterialPropertyBlock();pane.Surface.GetPropertyBlock(debugBlock);
                            debugBlock.SetFloat("_F0",1);debugBlock.SetFloat("_Haze",0);debugBlock.SetFloat("_DustAmount",0);debugBlock.SetFloat("_WearAmount",0);debugBlock.SetFloat("_TransmissionBlur",0);pane.Surface.SetPropertyBlock(debugBlock);
                            try{Capture(pane.name.Replace(' ','_')+"_"+side+"_"+(oblique?"Oblique":"Straight")+"_"+label+"_ReflectionOnly");}finally{pane.Surface.SetPropertyBlock(savedBlock);}
                        }sw.Stop();long count=manager.TotalCaptures-before;
                        if(label=="HybridActive"&&count==0)
                        {
                            var gp=manager.groups.First(g=>g.windows.Contains(pane.Surface));var pt=gp.plane.TransformPoint(gp.localPoint);var nm=gp.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(gp.localNormal).normalized;nm*=Vector3.Dot(nm,camera.transform.position-pt)>=0?1:-1;var view=camera.worldToCameraMatrix*GasStationPlanarGlass.ReflectionMatrix(new Vector4(nm.x,nm.y,nm.z,-Vector3.Dot(nm,pt)));var cp=view.MultiplyPoint(pt+nm*manager.clipOffset);var cn=view.MultiplyVector(nm).normalized;var proj=camera.CalculateObliqueMatrix(new Vector4(cn.x,cn.y,cn.z,-Vector3.Dot(cp,cn)));var vp=proj*view;actor.TryGetBounds(manager.reflectionLayers,now,out var bb,out _);var ar=typeof(GasStationPlanarGlass).GetMethod("ProjectedRect",BindingFlags.Static|BindingFlags.NonPublic);
                            report.AppendLine("MATH actorFrustum="+GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(vp),bb)+" cameraFrustum="+GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),pane.Surface.bounds)+" actorRect="+ar.Invoke(null,new object[]{vp,bb})+" windowRect="+ar.Invoke(null,new object[]{camera.projectionMatrix*camera.worldToCameraMatrix,pane.Surface.bounds})+" actorScene="+actor.gameObject.scene.name+" managerScene="+manager.gameObject.scene.name+" groups="+manager.groups.Length);
                            report.AppendLine("DIAGNOSTIC mode="+manager.reflectionMode+" reason="+manager.LastActivityReason+" registered="+manager.RegisteredActors+" sourceMask="+camera.cullingMask+" reflectionMask="+manager.reflectionLayers.value+" pipeline="+GraphicsSettings.currentRenderPipeline+" configured="+manager.configuredPipeline+" marker="+actor.TryGetBounds(manager.reflectionLayers.value,now,out var ab,out var le)+" bounds="+ab+" windowEnabled="+pane.Surface.enabled+" windowActive="+pane.gameObject.activeInHierarchy+" windowBounds="+pane.Surface.bounds+" camera="+camera.transform.position+" shaderGlobal="+Shader.GetGlobalFloat("_GasGlassRenderingReflection"));}
                        Require(label=="HybridActive"?count>0&&count<=32:count==0,pane.name+" side="+side+" "+label+" captures="+count);
                        var mpb=new MaterialPropertyBlock();pane.Surface.GetPropertyBlock(mpb);Require(mpb.GetFloat("_UseLocalBaked")==1&&mpb.GetFloat("_VariationEnabled")==1,pane.name+" correct local reflection and dirt MPB survive transitions");
                        if(label=="HybridActive")
                        {
                            report.AppendLine("LIVE "+pane.name+" side="+side+" oblique="+oblique+" weight="+mpb.GetFloat("_PlanarWeight")+" models="+manager.DynamicModelCount+" tris="+manager.DynamicTriangles+" reason="+manager.LastActivityReason+" update="+manager.LastUpdateReason);
                            var raw=mpb.GetTexture("_PlanarTexture") as RenderTexture;
                            if(raw){var oldTarget=RenderTexture.active;RenderTexture.active=raw;var read=new Texture2D(raw.width,raw.height,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,raw.width,raw.height),0,0);read.Apply();File.WriteAllBytes(Output+"/Raw_"+pane.name.Replace(' ','_')+"_"+side+"_"+oblique+".png",read.EncodeToPNG());UnityEngine.Object.DestroyImmediate(read);RenderTexture.active=oldTarget;}
                        }
                        report.AppendLine("SAMPLE "+pane.name+" side="+side+" oblique="+oblique+" "+label+" sync render/readback batch="+sw.Elapsed.TotalMilliseconds.ToString("F2")+"ms (not runtime FPS)");
                    }
                }
            }
            var moving=panes.FirstOrDefault(p=>p.GetComponentInParent<TwoSidedDoor>());
            Require(moving,"Moving door glass is included in the inventory");
            var hinge=moving.GetComponentInParent<TwoSidedDoor>().transform;var pose=hinge.localRotation;
            try
            {
                foreach(float angle in new[]{-92f,92f})
                {
                    hinge.localRotation=pose*Quaternion.AngleAxis(angle,Vector3.forward);Physics.SyncTransforms();
                    var p=moving.transform.TransformPoint(moving.LocalPoint);var n=moving.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(moving.LocalNormal).normalized;
                    foreach(int side in new[]{-1,1})
                    {
                        camera.transform.SetPositionAndRotation(p+n*side*4,Quaternion.LookRotation(-n*side,Vector3.up));marker.SetActive(true);actor.enabled=true;marker.transform.position=p+n*side*2.5f;ReflectionActor.Register(marker);
                        manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Hybrid;manager.InvalidateReflections();long start=manager.TotalCaptures;for(int i=0;i<32;i++)Step();
                        Require(manager.TotalCaptures>start,"Moving door angle="+angle+" side="+side+" uses current reflection plane");Capture("Door_"+angle+"_"+side);
                    }
                }
            }
            finally{hinge.localRotation=pose;Physics.SyncTransforms();}
            manager.RefreshBakedBindings();Require(Panes().All(p=>p.GetComponent<GlassReflectionBinding>().Verified),"Refreshing keeps all explicit bindings");
            Require(ShaderUtil.GetShaderMessages(panes[0].Surface.sharedMaterial.shader).All(m=>m.severity.ToString()!="Error"),"No glass shader compilation errors");
            report.AppendLine("PASS Automated checks complete. Inspect exported images before final scene save. CPU/GPU runtime profiling not inferred from synchronous render timings.");File.WriteAllText(Output+"/Verification.txt",report.ToString());
        }
        finally
        {
            manager.reflectionMode=oldMode;manager.testActorOcclusion=oldOcclusion;for(int i=0;i<actors.Length;i++)if(actors[i])actors[i].enabled=states[i];
            UnityEngine.Object.DestroyImmediate(marker);UnityEngine.Object.DestroyImmediate(material);camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(root);manager.InvalidateReflections();
        }
    }
}
