using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class GlassLeanSetup
{
    public static void QualityScreens()
    {
        Directory.CreateDirectory(PerformanceOutput+"/Quality");var manager=Manager();var old=manager.reflectionMode;var quality=manager.transmissionQuality;
        var root=new GameObject("Temporary glass quality camera");var camera=root.AddComponent<Camera>();var source=manager.ResolvePlayerCamera();if(source)camera.CopyFrom(source);camera.enabled=false;camera.nearClipPlane=.05f;camera.aspect=16f/9;camera.fieldOfView=55;camera.GetUniversalAdditionalCameraData().SetRenderer(0);
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf);target.Create();camera.targetTexture=target;var pixels=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
        try
        {
            manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Baked;
            foreach(var pane in Object.FindObjectsByType<GlassSurfaceVariation>(FindObjectsSortMode.None).Where(p=>new[]{"Plane (9)","Plane (10)","Plane (11)"}.Contains(p.name)))
            {
                var point=pane.transform.TransformPoint(pane.LocalPoint);var normal=pane.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(pane.LocalNormal).normalized;
                foreach(int side in new[]{-1,1})foreach(var mode in new[]{GasStationPlanarGlass.TransmissionQuality.High,GasStationPlanarGlass.TransmissionQuality.Balanced,GasStationPlanarGlass.TransmissionQuality.Off})
                {
                    camera.transform.SetPositionAndRotation(point+normal*side*1.3f,Quaternion.LookRotation(-normal*side,Vector3.up));manager.transmissionQuality=mode;manager.RenderForCamera(camera);
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});var previous=RenderTexture.active;RenderTexture.active=target;
                    try{pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(PerformanceOutput+"/Quality/"+pane.name.Replace(' ','_')+"_"+side+"_"+mode+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                }
            }
        }
        finally
        {
            typeof(GasStationPlanarGlass).GetMethod("ConfigureTransmission",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{null,false});manager.reflectionMode=old;manager.transmissionQuality=quality;manager.InvalidateReflections();camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);Object.DestroyImmediate(root);
        }
    }
}
