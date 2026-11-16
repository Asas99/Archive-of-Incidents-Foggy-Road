using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class ValidateDecalVisibility
{
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        var type=typeof(VisibilityAwareDecalFeature).Assembly.GetType("DecalProjectorRegistry",true);
        var canSkip=type.GetMethod("CanSkip",flags);var count=type.GetProperty("Count",flags);var available=type.GetProperty("Available",flags);
        if(!(bool)available.GetValue(null))throw new Exception("Lifecycle tracking unavailable");
        int initial=(int)count.GetValue(null);var report=new StringBuilder();int passed=0;
        var scene=SceneManager.CreateScene("Temporary decal visibility validation");
        var cameraObject=new GameObject("Test camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,1000,0);camera.nearClipPlane=.1f;camera.farClipPlane=100;
        var planes=new Plane[6];
        bool Skip(){GeometryUtility.CalculateFrustumPlanes(camera,planes);return (bool)canSkip.Invoke(null,new object[]{planes});}
        void Check(bool condition,string label){if(!condition)throw new Exception("FAIL "+label);passed++;report.AppendLine("PASS "+label);}
        var material=UnityEngine.Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None).First(p=>p.material).material;
        GameObject owner=null;
        try
        {
            Check(Skip(),"Existing offscreen projectors allow skipping");
            owner=new GameObject("New runtime decal");SceneManager.MoveGameObjectToScene(owner,scene);owner.transform.position=new Vector3(0,1000,10);var p=owner.AddComponent<DecalProjector>();p.material=material;
            Check((int)count.GetValue(null)==initial+1&&!Skip(),"Runtime AddComponent is registered immediately");
            p.enabled=false;Check((int)count.GetValue(null)==initial&&Skip(),"Disable removes projector immediately");
            p.enabled=true;Check(!Skip(),"Enable restores visible projector immediately");
            owner.transform.position=new Vector3(0,1000,-20);Check(Skip(),"Moving behind camera is detected without a refresh delay");
            owner.transform.position=new Vector3(50,1000,10);p.size=Vector3.one;Check(Skip(),"Small offscreen projector skips");
            p.size=new Vector3(120,2,2);Check(!Skip(),"Growing an offscreen projector into the frustum is detected");
            p.size=Vector3.one;owner.transform.localScale=new Vector3(120,1,1);p.scaleMode=DecalScaleMode.ScaleInvariant;Check(Skip(),"Scale-invariant mode ignores transform scale");
            p.scaleMode=DecalScaleMode.InheritFromHierarchy;Check(!Skip(),"Inherited scale expands conservative bounds");
            p.material=null;Check(Skip(),"Null material contributes no visible decal");p.material=material;Check(!Skip(),"Runtime material replacement restores contribution");
            UnityEngine.Object.DestroyImmediate(owner);owner=null;Check((int)count.GetValue(null)==initial&&Skip(),"Destroy removes projector immediately");
            owner=new GameObject("Initially inactive decal");SceneManager.MoveGameObjectToScene(owner,scene);owner.SetActive(false);owner.transform.position=new Vector3(0,1000,10);p=owner.AddComponent<DecalProjector>();p.material=material;
            Check((int)count.GetValue(null)==initial&&Skip(),"Inactive creation stays absent");owner.SetActive(true);Check((int)count.GetValue(null)==initial+1&&!Skip(),"Later activation registers the projector");
            var clone=UnityEngine.Object.Instantiate(owner);SceneManager.MoveGameObjectToScene(clone,scene);Check((int)count.GetValue(null)==initial+2,"Instantiate registers copied projectors");UnityEngine.Object.DestroyImmediate(clone);
            report.AppendLine("Immediate checks passed="+passed+"; unload check pending");
        }
        catch(Exception exception){report.AppendLine(exception.ToString());throw;}
        finally
        {
            string path="Recovery/DecalVisibilityPermanent_20261114/LifecycleValidation.txt";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,report.ToString());
            var unload=SceneManager.UnloadSceneAsync(scene);
            unload.completed+=op=>{bool restored=(int)count.GetValue(null)==initial;File.AppendAllText(path,(restored?"PASS":"FAIL")+" Additive scene unload restores original registry count="+(int)count.GetValue(null)+"\n");};
        }
        return report.ToString();
    }
}
