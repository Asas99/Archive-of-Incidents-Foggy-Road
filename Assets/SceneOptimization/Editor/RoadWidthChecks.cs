using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoadWidthChecks
{
    static MeshFilter Road()=>UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Single(f=>f.name=="NurbsPath_ROAD"&&f.gameObject.scene==SceneManager.GetActiveScene());
    public static void Show(bool curve)
    {
        var road=Road();var field=new RoadWidthGeometry(road.sharedMesh,road.transform);var view=SceneView.lastActiveSceneView;if(!view)throw new InvalidOperationException("Scene View required");
        Vector3 point=curve?new Vector3(560,0,1800):new Vector3(404,0,900);if(!field.Query(point,out var q))throw new InvalidOperationException("View location outside road");
        if(curve)
        {
            var forward=Vector3.Cross(q.across,Vector3.up).normalized;if(forward.z<0)forward=-forward;
            view.LookAt(q.center+forward*15,Quaternion.LookRotation(forward+Vector3.down*.35f),45,false,true);
        }
        else view.LookAt(q.center+new Vector3(12,0,0),Quaternion.Euler(32,35,0),65,false,true);
        Selection.activeGameObject=null;view.Repaint();
    }
    // Sweeps the actual scene vehicle collider's footprint through both lanes.
    // This is an editor physics clearance check, not a gameplay driving/FPS test.
    public static void CheckVehicleClearance()
    {
        var road=Road();var field=new RoadWidthGeometry(road.sharedMesh,road.transform);
        var cars=UnityEngine.Object.FindObjectsByType<CarController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==road.gameObject.scene).ToArray();
        if(cars.Length==0)throw new InvalidOperationException("No actual vehicle controller/collider found");
        var car=cars.OrderBy(c=>Vector3.SqrMagnitude(c.transform.position-new Vector3(405,0,900))).First();
        var collider=car.GetComponent<Collider>();if(!collider)throw new InvalidOperationException("Vehicle collider missing");
        var size=collider.bounds.size;float halfWidth=Mathf.Min(size.x,size.z)*.5f,halfLength=Mathf.Max(size.x,size.z)*.5f;
        if(collider is BoxCollider box)
        {
            var scaled=Vector3.Scale(box.size,car.transform.lossyScale);halfWidth=Mathf.Min(Mathf.Abs(scaled.x),Mathf.Abs(scaled.z))*.5f;halfLength=Mathf.Max(Mathf.Abs(scaled.x),Mathf.Abs(scaled.z))*.5f;
        }
        var hits=new Collider[96];int probes=0,blocked=0;var report=new StringBuilder();
        Physics.SyncTransforms();
        foreach(int side in new[]{-1,1})for(int i=5;i<field.sections.Length-5;i+=3)
        {
            var section=field.sections[i];var across=(section.a-section.b).normalized;var tangent=(field.sections[i+1].center-field.sections[i-1].center).normalized;var center=section.center+across*section.half*.5f*side;
            // Tires contact the road; the footprint volume starts 10 cm above it.
            center.y=section.center.y+Mathf.Max(.5f,size.y*.5f)+.10f;
            int count=Physics.OverlapBoxNonAlloc(center,new Vector3(halfWidth,Mathf.Max(.4f,size.y*.5f),halfLength),hits,Quaternion.LookRotation(tangent*side),~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)throw new InvalidOperationException("Vehicle clearance buffer full");
            for(int h=0;h<count;h++)
            {
                var hit=hits[h];if(hit is TerrainCollider||hit.gameObject==road.gameObject||hit.GetComponentInParent<CarController>()||hit.GetComponentInParent<ReflectionActor>())continue;
                string name=hit.name;
                if(name.StartsWith("GRPRO_")||name.IndexOf("tree",StringComparison.OrdinalIgnoreCase)>=0)
                {blocked++;report.AppendLine("Blocked "+i+" side="+side+" "+name);}
            }
            probes++;
        }
        report.AppendLine("Actual vehicle="+car.name+" footprint="+(halfWidth*2)+" x "+(halfLength*2)+"m; two-direction lane probes="+probes+"; guardrail/tree blockers="+blocked);
        report.AppendLine("Editor overlap checks only. No claim of manual player driving or FPS measurement.");
        File.WriteAllText(RoadWidthInstallation.State.backupDirectory+"/VehicleClearance.txt",report.ToString());
        if(blocked>0)throw new InvalidOperationException("Vehicle clearance blockers: "+blocked);
    }
}
