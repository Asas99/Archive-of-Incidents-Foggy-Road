using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public static partial class GlassLeanSetup
{
    static void Diagnose()
    {
        var manager=Manager();var pane=Object.FindObjectsByType<GlassSurfaceVariation>(FindObjectsSortMode.None).Single(p=>p.name=="Plane (10)");
        var root=new GameObject("Temporary diagnostic");var camera=root.AddComponent<Camera>();camera.enabled=false;camera.aspect=16f/9;camera.fieldOfView=55;camera.nearClipPlane=.05f;camera.targetTexture=new RenderTexture(1920,1080,24);camera.targetTexture.Create();camera.GetUniversalAdditionalCameraData().SetRenderer(0);
        var point=pane.transform.TransformPoint(pane.LocalPoint);var normal=pane.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(pane.LocalNormal).normalized;
        camera.transform.SetPositionAndRotation(point-normal*4,Quaternion.LookRotation(normal,Vector3.up));
        var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.layer=30;marker.transform.position=point-normal*2.5f;marker.transform.localScale=Vector3.one*.7f;var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",Color.magenta*3);marker.GetComponent<Renderer>().sharedMaterial=material;ReflectionActor.Register(marker);
        foreach(var part in manager.reflectionOcclusion.parts){var mesh=part.mesh;var v=mesh.vertices;var t=mesh.triangles;mesh.Clear();mesh.vertices=v;mesh.triangles=t;mesh.RecalculateBounds();mesh.UploadMeshData(false);}
        var old=manager.reflectionOcclusion;var mode=manager.reflectionMode;var debug=manager.dynamicDebug;bool test=manager.testActorOcclusion;var tick=typeof(GasStationPlanarGlass).GetMethod("TickWithClock",BindingFlags.NonPublic|BindingFlags.Instance);double now=Time.realtimeSinceStartupAsDouble+100;int frame=300000;
        try
        {
            manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Realtime;manager.testActorOcclusion=false;
            foreach(string label in new[]{"NoDepth","Depth","DepthView"})
            {
                manager.reflectionOcclusion=label=="NoDepth"?null:old;manager.dynamicDebug=label=="DepthView"?GasStationPlanarGlass.DynamicDebug.OcclusionDepth:GasStationPlanarGlass.DynamicDebug.Composite;manager.InvalidateReflections();
                for(int i=0;i<40;i++){now+=1.0/60;tick.Invoke(manager,new object[]{camera,true,now,frame++});}
                var block=new MaterialPropertyBlock();pane.Surface.GetPropertyBlock(block);var raw=block.GetTexture("_PlanarTexture") as RenderTexture;if(!raw)continue;var previous=RenderTexture.active;RenderTexture.active=raw;var read=new Texture2D(raw.width,raw.height,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,raw.width,raw.height),0,0);read.Apply();File.WriteAllBytes(Output+"/Diagnostic_"+label+".png",read.EncodeToPNG());Object.DestroyImmediate(read);RenderTexture.active=previous;
            }
        }
        finally{manager.reflectionOcclusion=old;manager.reflectionMode=mode;manager.dynamicDebug=debug;manager.testActorOcclusion=test;manager.InvalidateReflections();Object.DestroyImmediate(marker);Object.DestroyImmediate(material);camera.targetTexture.Release();Object.DestroyImmediate(camera.targetTexture);Object.DestroyImmediate(root);}
    }
}
