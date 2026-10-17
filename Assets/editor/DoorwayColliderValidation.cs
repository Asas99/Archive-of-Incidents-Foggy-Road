using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DoorwayColliderValidation
{
    public static void Validate(bool runtime)
    {
        var report=new StringBuilder("Doorway validation "+DateTime.Now.ToString("s")+" runtime="+runtime+"\n");
        void Check(bool ok,string message){report.AppendLine((ok?"PASS ":"FAIL ")+message);if(!ok)throw new InvalidOperationException(message);}
        try
        {
            var owners=DoorwayColliderTool.Owners();Check(owners.Length==5,"Exactly five physical doorway sets");
            Check(owners.SelectMany(o=>new[]{o.first,o.second}).Distinct().Count()==10,"Exactly ten distinct source models");
            Check(owners.Sum(o=>o.generated.Count(c=>c))==15,"Exactly fifteen generated BoxColliders");
            foreach(var owner in owners)
            {
                Check(owner.Built,owner.name+" has three enabled non-trigger boxes");
                Check(owner.replaced.All(c=>!c||!c.enabled),owner.name+" source collision is replaced");
                Check(!owner.GetComponent<Rigidbody>(),owner.name+" has no new Rigidbody");
                var center=new Vector3((owner.openingMin.x+owner.openingMax.x)*0.5f,(owner.openingMin.y+owner.openingMax.y)*0.5f,0);
                bool Clear(Vector3 p)
                {
                    foreach(int side in new[]{-1,1})
                    {
                        Vector3 origin=owner.transform.TransformPoint(p+Vector3.forward*side*(owner.wall.size.z+1));Vector3 dir=owner.transform.TransformDirection(Vector3.forward)*-side;
                        foreach(var box in owner.generated)if(box.Raycast(new Ray(origin,dir),out _,owner.wall.size.z*2+2))return false;
                    }
                    return true;
                }
                Check(Clear(center),owner.name+" center is open in both directions");
                Check(Clear(new Vector3(center.x,owner.openingMin.y+0.03f,0)),owner.name+" has no bottom crossbar");
                foreach(var box in owner.generated)
                {
                    Vector3 p=owner.transform.TransformPoint(box.center);Vector3 n=owner.transform.forward;
                    Check(box.Raycast(new Ray(p-n*(owner.wall.size.z+1),n),out _,owner.wall.size.z*2+2),owner.name+" "+box.name+" blocks passage through wall material");
                }
            }
            if(runtime){int failures=0;Runtime(owners,report,(ok,message)=>{report.AppendLine((ok?"PASS ":"FAIL ")+message);if(!ok)failures++;});Check(failures==0,"All real-player traversal and closed-leaf checks passed (failures="+failures+")");}
            Directory.CreateDirectory(DoorwayColliderTool.Output);File.WriteAllText(DoorwayColliderTool.Output+(runtime?"/PlayMode.txt":"/Geometry.txt"),report+"PASS\n");
        }
        catch(Exception e){Directory.CreateDirectory(DoorwayColliderTool.Output);File.WriteAllText(DoorwayColliderTool.Output+(runtime?"/PlayMode.txt":"/Geometry.txt"),report+"FAIL "+e);throw;}
    }
    static void Runtime(DoorwayColliderAuthoring[] owners,StringBuilder report,Action<bool,string> check)
    {
        var player=UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None).FirstOrDefault(c=>c.GetComponent<MonoBehaviour>());
        if(!player)throw new InvalidOperationException("No gameplay CharacterController");
        Vector3 oldPosition=player.transform.position;Quaternion oldRotation=player.transform.rotation;bool oldEnabled=player.enabled;
        var movement=player.GetComponents<MonoBehaviour>().Where(b=>b&&b.GetType().Name=="KarakterHareket").ToArray();var movementStates=movement.Select(b=>b.enabled).ToArray();
        var hinges=owners.Select(o=>o.doorHinge).Where(h=>h).Distinct().ToArray();var rotations=hinges.ToDictionary(h=>h,h=>h.localRotation);
        var scripts=hinges.Select(h=>h.GetComponent<TwoSidedDoor>()).Where(b=>b).ToArray();var enabled=scripts.Select(b=>b.enabled).ToArray();
        var originals=owners.SelectMany(o=>o.replaced).Where(c=>c).Distinct().ToArray();var originalStates=originals.Select(c=>c.enabled).ToArray();
        try
        {
            foreach(var b in movement)b.enabled=false;foreach(var b in scripts)b.enabled=false;
            foreach(var hinge in hinges)hinge.localRotation=rotations[hinge]*Quaternion.AngleAxis(92,Vector3.forward);
            Physics.SyncTransforms();
            float radius=player.radius*Mathf.Max(Mathf.Abs(player.transform.lossyScale.x),Mathf.Abs(player.transform.lossyScale.z));float height=player.height*Mathf.Abs(player.transform.lossyScale.y);
            report.AppendLine("Player world radius="+radius+" height="+height+"; measurements below are synchronous CharacterController.Move wall times, not FPS or GPU times.");
            void Teleport(Vector3 p){player.enabled=false;player.transform.position=p-player.transform.TransformVector(player.center);player.enabled=true;Physics.SyncTransforms();}
            foreach(var owner in owners)
            {
                float width=owner.openingMax.x-owner.openingMin.x,clearHeight=owner.openingMax.y-owner.openingMin.y;
                check(width>radius*2+0.02f,owner.name+" fits actual player width ("+width.ToString("F2")+"m)");
                check(clearHeight>height+0.02f,owner.name+" fits actual player height ("+clearHeight.ToString("F2")+"m)");
                check(owner.doorHinge,owner.name+" has matched moving door hinge");
                float center=(owner.openingMin.x+owner.openingMax.x)*0.5f;float offset=Mathf.Min(0.10f,(width-radius*2-0.02f)*0.20f);
                var after=new List<double>();
                foreach(int side in new[]{-1,1}) foreach(float diagonal in new[]{0f,offset,-offset})
                {
                    float z0=(side<0?owner.wall.min.z-radius-0.5f:owner.wall.max.z+radius+0.5f),z1=(side<0?owner.wall.max.z+radius+0.5f:owner.wall.min.z-radius-0.5f);
                    var start=owner.transform.TransformPoint(new Vector3(center-diagonal,owner.openingMin.y+height*0.5f+0.04f,z0));var end=owner.transform.TransformPoint(new Vector3(center+diagonal,owner.openingMin.y+height*0.5f+0.04f,z1));Teleport(start);
                    owner.doorHinge.localRotation=rotations[owner.doorHinge];float playerSide=owner.doorHinge.InverseTransformPoint(start).y;owner.doorHinge.localRotation=rotations[owner.doorHinge]*Quaternion.AngleAxis(playerSide>=0?-92:92,Vector3.forward);Physics.SyncTransforms();var step=(end-start)/24;
                    for(int i=0;i<24;i++){var sw=Stopwatch.StartNew();player.Move(step);sw.Stop();after.Add(sw.Elapsed.TotalMilliseconds);}
                    Vector3 actual=player.transform.TransformPoint(player.center);
                    if((actual-end).magnitude>0.25f)
                    {
                        var hits=Physics.CapsuleCastAll(start+Vector3.up*(height*0.5f-radius),start-Vector3.up*(height*0.5f-radius),radius,(end-start).normalized,(end-start).magnitude,~0,QueryTriggerInteraction.Ignore);
                        report.AppendLine("Travel start="+start+" end="+end+" actual="+actual+" delta="+(actual-end)+". Blockers: "+string.Join("; ",hits.Where(h=>!h.collider.transform.IsChildOf(player.transform)).Select(h=>DoorwayColliderTool.PathOf(h.collider.transform)).Distinct()));
                    }
                    check((actual-end).magnitude<0.25f,owner.name+" real player crosses side="+side+" diagonal="+diagonal.ToString("F2"));
                }
                var sorted=after.OrderBy(t=>t).ToArray();report.AppendLine(owner.name+" new Move mean="+after.Average().ToString("F4")+"ms p95="+sorted[(int)(sorted.Length*0.95)].ToString("F4")+"ms worst="+sorted.Last().ToString("F4")+"ms");
                // Record the same center route with the original source collider configuration.
                foreach(var c in owner.generated)c.enabled=false;
                for(int i=0;i<owner.replaced.Length;i++)if(owner.replaced[i])owner.replaced[i].enabled=owner.previousEnabled[i];
                Physics.SyncTransforms();var before=new List<double>();
                var a=owner.transform.TransformPoint(new Vector3(center,owner.openingMin.y+height*0.5f+0.04f,owner.wall.min.z-radius-0.5f));var bpoint=owner.transform.TransformPoint(new Vector3(center,owner.openingMin.y+height*0.5f+0.04f,owner.wall.max.z+radius+0.5f));Teleport(a);
                for(int i=0;i<24;i++){var sw=Stopwatch.StartNew();player.Move((bpoint-a)/24);sw.Stop();before.Add(sw.Elapsed.TotalMilliseconds);}
                report.AppendLine(owner.name+" original source settings Move mean="+before.Average().ToString("F4")+"ms worst="+before.Max().ToString("F4")+"ms; original route may have been obstructed/missing colliders.");
                foreach(var c in owner.replaced)if(c)c.enabled=false;foreach(var c in owner.generated)c.enabled=true;
                owner.doorHinge.localRotation=rotations[owner.doorHinge];Physics.SyncTransforms();
                var rayOrigin=owner.transform.TransformPoint(new Vector3(center,owner.openingMin.y+height*0.55f,owner.wall.min.z-1));
                var rayHits=Physics.RaycastAll(rayOrigin,owner.transform.forward,owner.wall.size.z+2,~0,QueryTriggerInteraction.Ignore);
                check(rayHits.Any(h=>h.collider.transform.IsChildOf(owner.doorHinge)),owner.name+" closed door leaf still has collision");
                owner.doorHinge.localRotation=rotations[owner.doorHinge]*Quaternion.AngleAxis(92,Vector3.forward);Physics.SyncTransforms();
            }
        }
        finally
        {
            foreach(var pair in rotations)if(pair.Key)pair.Key.localRotation=pair.Value;
            for(int i=0;i<scripts.Length;i++)if(scripts[i])scripts[i].enabled=enabled[i];
            for(int i=0;i<originals.Length;i++)if(originals[i])originals[i].enabled=originalStates[i];
            foreach(var owner in owners)foreach(var c in owner.generated)if(c)c.enabled=true;
            player.enabled=false;player.transform.SetPositionAndRotation(oldPosition,oldRotation);player.enabled=oldEnabled;
            for(int i=0;i<movement.Length;i++)if(movement[i])movement[i].enabled=movementStates[i];Physics.SyncTransforms();
        }
    }
}

