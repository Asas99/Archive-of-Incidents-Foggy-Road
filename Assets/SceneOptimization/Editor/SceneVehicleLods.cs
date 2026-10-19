using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class SceneVehicleLods
{
 public static void Install(UnityEngine.SceneManagement.Scene scene,StringBuilder report)
 {
  var catalog=Resources.Load<GlassVehicleLodCatalog>("GlassVehicleLodCatalog");if(!catalog){report.AppendLine("VEHICLE_UNRESOLVED No existing LOD2 catalogue");return;}
  foreach(var actor in UnityEngine.Object.FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.gameObject.scene==scene))
  {
   if(actor.GetComponentInParent<LODGroup>()||actor.GetComponentInChildren<LODGroup>(true)){report.AppendLine("VEHICLE_LOD retained "+actor.name);continue;}
   var sources=actor.GetComponentsInChildren<MeshRenderer>(true);
   foreach(var entry in catalog.vehicles)
   {
    if(!sources.Any(r=>entry.high.Any(p=>p.mesh==r.GetComponent<MeshFilter>()?.sharedMesh)))continue;
    try{Configure(actor,entry);report.AppendLine("VEHICLE_LOD configured "+actor.name+" "+entry.name);}catch(Exception e){report.AppendLine("VEHICLE_UNRESOLVED "+actor.name+" "+e.Message);}break;
   }
  }
 }
 public static void Configure(ReflectionActor actor,GlassVehicleLodCatalog.Vehicle entry)
 {
  if(actor.GetComponent<LODGroup>())return;
  var sources=actor.GetComponentsInChildren<MeshRenderer>(true);var high=sources.Where(r=>entry.high.Any(p=>p.mesh==r.GetComponent<MeshFilter>()?.sharedMesh)).ToArray();
  if(entry.high.Any(p=>!high.Any(r=>r.GetComponent<MeshFilter>().sharedMesh==p.mesh)))throw new Exception("Incomplete original high part set");
  var existingLow=sources.Where(r=>entry.low.Any(p=>p.mesh==r.GetComponent<MeshFilter>()?.sharedMesh)).ToArray();
  bool anyLow=existingLow.Length>0;if(anyLow&&entry.low.Any(p=>!existingLow.Any(r=>r.GetComponent<MeshFilter>().sharedMesh==p.mesh)))throw new Exception("Partial existing low part set");
  var plans=new List<(Transform anchor,Matrix4x4 offset,GlassVehicleLodCatalog.Part part)>();
  if(!anyLow)
  {
   if(entry.high.GroupBy(p=>p.name).Any(g=>g.Count()>1))throw new Exception("Ambiguous repeated moving part names; existing vehicle representation retained");
   foreach(var low in entry.low)
   {
    var bound=entry.high.FirstOrDefault(p=>p.name==low.name);var source=high.FirstOrDefault(r=>r.GetComponent<MeshFilter>().sharedMesh==bound.mesh);
    if(!source){bound=entry.high[0];source=high.First(r=>r.GetComponent<MeshFilter>().sharedMesh==bound.mesh);}
    var offset=bound.rootMatrix.inverse*low.rootMatrix;
    var recomposed=Matrix4x4.TRS(offset.GetColumn(3),offset.rotation,offset.lossyScale);
    for(int i=0;i<16;i++)if(Mathf.Abs(offset[i]-recomposed[i])>.001f)throw new Exception("Non-TRS part offset "+low.name);
    if(!low.mesh||low.materials.Length==0||low.materials.Any(m=>!m))throw new Exception("Missing low geometry/material "+low.name);
    plans.Add((source.transform,offset,low));
   }
  }
  var lows=new List<Renderer>(existingLow);
  foreach(var plan in plans)
  {
   var go=new GameObject("Existing LOD2 - "+plan.part.name);Undo.RegisterCreatedObjectUndo(go,"Existing vehicle LOD2");go.layer=plan.anchor.gameObject.layer;go.transform.SetParent(plan.anchor,false);
   go.transform.localPosition=plan.offset.GetColumn(3);go.transform.localRotation=plan.offset.rotation;go.transform.localScale=plan.offset.lossyScale;
   go.AddComponent<MeshFilter>().sharedMesh=plan.part.mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=plan.part.materials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;lows.Add(renderer);
  }
  var group=Undo.AddComponent<LODGroup>(actor.gameObject);group.SetLODs(new[]{new LOD(.2f,high),new LOD(.00001f,lows.ToArray())});group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.RecalculateBounds();EditorUtility.SetDirty(group);
  actor.RefreshRenderers();
 }
}
