using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor-only geometry survey. No runtime ray tests or automatic room reassignment.</summary>
[InitializeOnLoad]
public static partial class GlassRoomSetup
{
    const string Request="Library/GlassRooms.request";
    public const string Output="Recovery/GlassRooms";
    static GlassRoomSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request))return;
        var command=File.ReadAllText(Request).Trim();File.Delete(Request);Directory.CreateDirectory(Output);
        try {if(command=="install")Install();else if(command=="verify")Verify();else if(command=="save"){if(!File.Exists(Output+"/Verification.txt")||!File.ReadAllText(Output+"/Verification.txt").Contains("PASS Automated checks complete"))throw new InvalidOperationException("Verification incomplete");if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Save failed");File.WriteAllText(Output+"/Saved.txt","PASS verified scene saved");}else Survey();}catch(Exception e){File.WriteAllText(Output+"/Error.txt",e.ToString());Debug.LogException(e);}
    }
    public static GlassSurfaceVariation[] Panes()=>UnityEngine.Object.FindObjectsByType<GlassSurfaceVariation>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.gameObject.scene==SceneManager.GetActiveScene() && GlassSurfaceVariation.UsesGlass(p.Surface)).OrderBy(p=>p.name).ToArray();
    static readonly MethodInfo Intersect=typeof(HandleUtility).GetMethod("IntersectRayMesh",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public,null,new[]{typeof(Ray),typeof(Mesh),typeof(Matrix4x4),typeof(RaycastHit).MakeByRefType()},null);
    public static float Distance(Vector3 origin,Vector3 direction,float maximum,MeshFilter[] geometry,out string hitName)
    {
        if(Intersect==null)throw new MissingMethodException("HandleUtility.IntersectRayMesh");
        var ray=new Ray(origin,direction);float best=maximum;hitName="OPEN";
        foreach(var mesh in geometry)
        {
            var renderer=mesh.GetComponent<Renderer>();
            if(!renderer || !renderer.bounds.IntersectRay(ray,out float d) || d>best)continue;
            object[] args={ray,mesh.sharedMesh,mesh.transform.localToWorldMatrix,default(RaycastHit)};
            if((bool)Intersect.Invoke(null,args)){var hit=(RaycastHit)args[3];if(hit.distance>0.02f && hit.distance<best){best=hit.distance;hitName=mesh.name;}}
        }
        return best;
    }
    public static MeshFilter[] Geometry()=>UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.gameObject.scene==SceneManager.GetActiveScene() && m.sharedMesh && m.GetComponent<Renderer>() && m.GetComponent<Renderer>().enabled && !m.GetComponent<GlassSurfaceVariation>() && !m.GetComponentInParent<ReflectionActor>() && !m.GetComponentInParent<Camera>()).ToArray();
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Survey All Window Rooms")]
    public static void Survey()
    {
        Directory.CreateDirectory(Output);var geometry=Geometry();var report=new StringBuilder();
        foreach(var pane in Panes())
        {
            var point=pane.transform.TransformPoint(pane.LocalPoint);var normal=pane.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(pane.LocalNormal).normalized;
            report.AppendLine(pane.name+" id="+pane.StableId+" center="+point+" normal="+normal);
            foreach(int side in new[]{1,-1})
            {
                var origin=point+normal*side*1.5f;report.AppendLine(" Side "+side+" sample="+origin);
                foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back,Vector3.up,Vector3.down})
                {float distance=Distance(origin,direction,60,geometry,out var hit);report.AppendLine("  "+direction+" distance="+distance.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" hit="+hit);}
            }
        }
        File.WriteAllText(Output+"/Survey.txt",report.ToString());
    }
}


