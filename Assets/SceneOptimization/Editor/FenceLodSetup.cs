using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
[InitializeOnLoad] public static class FenceLodSetup
{
 const string Request="Library/FenceLod.request";static string Backup=>File.ReadAllText("Library/FenceLodBackup.path").Trim();
 static FenceLodSetup(){EditorApplication.update+=Poll;}
 static GameObject Find(string name){var found=All().Where(t=>t.name==name).ToArray();if(found.Length!=1)throw new Exception(name+" candidates="+found.Length);return found[0].gameObject;}
 static Transform[] All()=>UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==EditorSceneManager.GetActiveScene()).ToArray();
 static Bounds BoundsOf(GameObject go){var rr=go.GetComponentsInChildren<Renderer>(true);if(rr.Length==0)throw new Exception("No renderers "+go.name);var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static int Tris(GameObject go)=>go.GetComponentsInChildren<MeshFilter>(true).Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(int)f.sharedMesh.GetIndexCount(i)/3));
 static Mesh MeshOf(GameObject go){var filters=go.GetComponentsInChildren<MeshFilter>(true);if(filters.Length!=1||filters[0].gameObject!=go)throw new Exception("Expected one root mesh: "+go.name);return filters[0].sharedMesh;}
 [Serializable] public class Entry{public string name;public Vector3 position,euler,scale,size,meshSize;public int triangles;public string[] components,shaders;public bool existingLod;}
 [Serializable] public class AuditData{public Entry[] objects;public string[] gates;}
 static string GateSnapshot()=>string.Join("\n",All().Where(t=>t.name.StartsWith("telkap",StringComparison.OrdinalIgnoreCase)).SelectMany(t=>t.GetComponentsInChildren<Component>(true)).Where(c=>c).Distinct().OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)));
 static string PhysicsSnapshot()=>string.Join("\n",UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==EditorSceneManager.GetActiveScene()).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)));
 static void Poll(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;string mode=File.ReadAllText(Request).Trim();File.Delete(Request);try{if(mode=="AUDIT")Audit();else if(mode=="APPLY")Apply();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}}
 static void Audit(){var names=new[]{"tel1","tel2","tel3","tel4","tel5","lod2tel"};var data=new AuditData{objects=names.Select(n=>{var go=Find(n);return new Entry{name=n,position=go.transform.position,euler=go.transform.eulerAngles,scale=go.transform.lossyScale,size=BoundsOf(go).size,meshSize=MeshOf(go).bounds.size,triangles=Tris(go),components=go.GetComponentsInChildren<Component>(true).Where(c=>c).Select(c=>c.GetType().Name).ToArray(),shaders=go.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.shader.name).Distinct().ToArray(),existingLod=go.GetComponentInParent<LODGroup>()||go.GetComponentInChildren<LODGroup>()};}).ToArray(),gates=All().Where(t=>t.name.StartsWith("telkap",StringComparison.OrdinalIgnoreCase)).Select(t=>t.name).ToArray()};File.WriteAllText(Backup+"/Audit.json",JsonUtility.ToJson(data,true));}
 static void Apply()
 {
  var highs=Enumerable.Range(1,5).Select(i=>Find("tel"+i)).ToArray();var template=Find("lod2tel");var scene=highs[0].scene;
  if(All().Any(t=>t.name.StartsWith("Tel LOD ")))throw new Exception("Existing fence LOD setup; no duplicate installation");
  if(highs.Any(h=>h.GetComponentInParent<LODGroup>()||h.GetComponentInChildren<LODGroup>()||h.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("telkap",StringComparison.OrdinalIgnoreCase))))throw new Exception("Existing LOD or gate inside target");
  if(template.GetComponentsInChildren<Component>(true).Any(c=>c&&!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)))throw new Exception("Low template has physics or scripts; inspect first");
  var lowMesh=MeshOf(template);foreach(var h in highs){MeshOf(h);if(Tris(template)>=Tris(h))throw new Exception("LOD2 does not reduce triangles for "+h.name);}
  if(!EditorSceneManager.SaveScene(scene,Backup+"/Before.unity",true))throw new Exception("Backup failed");
  string gates=GateSnapshot(),physics=PhysicsSnapshot();var report=new System.Text.StringBuilder();
  Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Fence LOD setup (gate excluded)");
  try
  {
   var lows=new GameObject[5];lows[0]=template;for(int i=1;i<5;i++){lows[i]=UnityEngine.Object.Instantiate(template);Undo.RegisterCreatedObjectUndo(lows[i],"Fence LOD setup");}
   for(int i=0;i<5;i++)
   {
    var high=highs[i];var low=lows[i];var highBounds=BoundsOf(high);var highMesh=MeshOf(high);var pos=high.transform.position;var rot=high.transform.rotation;var worldScale=high.transform.lossyScale;
    var holder=new GameObject("Tel LOD "+(i+1));Undo.RegisterCreatedObjectUndo(holder,"Fence LOD setup");holder.transform.SetParent(high.transform.parent,true);holder.transform.position=pos;holder.transform.rotation=Quaternion.identity;
    Undo.SetTransformParent(high.transform,holder.transform,"Fence LOD setup");Undo.SetTransformParent(low.transform,holder.transform,"Fence LOD setup");Undo.RecordObject(low.transform,"Align low fence");Undo.RecordObject(low,"Name low fence");low.name=i==0?"lod2tel":"tel"+(i+1)+" LOD2";
    low.transform.rotation=rot;low.transform.localScale=Vector3.Scale(high.transform.localScale,new Vector3(highMesh.bounds.size.x/lowMesh.bounds.size.x,highMesh.bounds.size.y/lowMesh.bounds.size.y,highMesh.bounds.size.z/lowMesh.bounds.size.z));low.transform.position+=highBounds.center-BoundsOf(low).center;
    if(Vector3.Distance(high.transform.position,pos)>.001f||Quaternion.Angle(high.transform.rotation,rot)>.01f||Vector3.Distance(high.transform.lossyScale,worldScale)>.01f)throw new Exception("High transform changed: "+high.name);
    var lowBounds=BoundsOf(low);if(Vector3.Distance(highBounds.center,lowBounds.center)>.01f)throw new Exception("Center alignment failed");for(int a=0;a<3;a++)if(Mathf.Abs(highBounds.size[a]-lowBounds.size[a])>Mathf.Max(.02f,highBounds.size[a]*.01f))throw new Exception("Size alignment failed: "+high.name);
    var group=Undo.AddComponent<LODGroup>(holder);var hr=high.GetComponentsInChildren<Renderer>(true);var lr=low.GetComponentsInChildren<Renderer>(true);group.SetLODs(new[]{new LOD(.4f,hr),new LOD(.06f,lr)});group.RecalculateBounds();
    var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c=>c.enabled&&c.targetTexture==null&&c.gameObject.scene==scene);float fov=camera?camera.fieldOfView:60;float k=group.size*Mathf.Max(holder.transform.lossyScale.x,holder.transform.lossyScale.y,holder.transform.lossyScale.z)*QualitySettings.lodBias/(2*Mathf.Tan(fov*Mathf.Deg2Rad*.5f));
    var levels=group.GetLODs();levels[0].screenRelativeTransitionHeight=Mathf.Clamp(k/20,.12f,.7f);levels[1].screenRelativeTransitionHeight=Mathf.Clamp(k/120,.005f,levels[0].screenRelativeTransitionHeight-.02f);levels[0].fadeTransitionWidth=.1f;levels[1].fadeTransitionWidth=.1f;group.SetLODs(levels);group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=false;
    if(group.GetLODs()[0].renderers.Intersect(group.GetLODs()[1].renderers).Any())throw new Exception("LOD renderer overlap");
    report.AppendLine(high.name+" high="+Tris(high)+" low="+Tris(low)+" centers="+highBounds.center+"/"+lowBounds.center+" sizes="+highBounds.size+"/"+lowBounds.size+" thresholds="+levels[0].screenRelativeTransitionHeight+"/"+levels[1].screenRelativeTransitionHeight);
   }
   if(GateSnapshot()!=gates||PhysicsSnapshot()!=physics)throw new Exception("Gate or collider serialization changed");
   Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
   report.AppendLine("PASS five independent native LODGroups, five low renderers (original plus four clones), no spare low template. Gate and all collider serialization unchanged; high transforms/materials retained. CrossFade enabled. No runtime scripts, benchmark or push.");File.WriteAllText(Backup+"/Result.txt",report.ToString());Selection.activeGameObject=highs[0].transform.parent.gameObject;
  }
  catch{Undo.RevertAllDownToGroup(undo);throw;}
 }
}
