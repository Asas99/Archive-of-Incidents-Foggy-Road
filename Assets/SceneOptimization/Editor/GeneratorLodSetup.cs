using System;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
[InitializeOnLoad] public static class GeneratorLodSetup
{
 const string Request="Library/GeneratorLod.request";static string Backup=>File.ReadAllText("Library/GeneratorLodBackup.path").Trim();
 [Serializable] public class Entry{public string name,path;public Vector3 position,euler,scale,size,center;public string[] components,children;public Part[] parts;}
 [Serializable] public class Part{public string name,mesh,shader;public int triangles;public Bounds meshBounds;public Vector3 position,euler,scale;}
 [Serializable] public class AuditData{public string scene;public Entry[] entries;}
 [Serializable] public class Config{public float lowScale=1;public Vector3 rotationDelta;public float highThreshold=.15f,lowThreshold=.02f;public bool crossFade=true;}
 static GeneratorLodSetup(){EditorApplication.update+=Poll;}
 static GameObject Find(string name){var found=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==EditorSceneManager.GetActiveScene()&&t.name==name).ToArray();if(found.Length!=1)throw new Exception(name+" candidates="+found.Length);return found[0].gameObject;}
 static Bounds BoundsOf(GameObject go){var rr=go.GetComponentsInChildren<Renderer>(true);if(rr.Length==0)throw new Exception("No renderers "+go.name);var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static void Poll(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;string mode=File.ReadAllText(Request).Trim();File.Delete(Request);try{if(mode=="AUDIT")Audit();else if(mode=="APPLY")Apply();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}}
 static void Audit(){var scene=EditorSceneManager.GetActiveScene();var entries=new[]{Find("jeneratörlod1"),Find("jeneratörlod2")}.Select(go=>{var b=BoundsOf(go);return new Entry{name=go.name,path=PathOf(go.transform),position=go.transform.position,euler=go.transform.eulerAngles,scale=go.transform.lossyScale,size=b.size,center=b.center,components=go.GetComponents<Component>().Select(c=>c?c.GetType().Name:"Missing").ToArray(),children=go.GetComponentsInChildren<Transform>(true).Select(t=>PathOf(t)).ToArray(),parts=go.GetComponentsInChildren<MeshFilter>(true).Select(f=>new Part{name=f.name,mesh=AssetDatabase.GetAssetPath(f.sharedMesh),triangles=f.sharedMesh?(int)f.sharedMesh.GetIndexCount(0)/3:0,meshBounds=f.sharedMesh?f.sharedMesh.bounds:default,shader=f.GetComponent<Renderer>()?string.Join(",",f.GetComponent<Renderer>().sharedMaterials.Where(m=>m).Select(m=>m.shader.name)):"",position=f.transform.position,euler=f.transform.eulerAngles,scale=f.transform.lossyScale}).ToArray()};}).ToArray();File.WriteAllText(Backup+"/Audit.json",JsonUtility.ToJson(new AuditData{scene=scene.path,entries=entries},true));}
 static void Apply()
 {
  var high=Find("jeneratörlod1");var low=Find("jeneratörlod2");var scene=high.scene;
  if(high.GetComponentInParent<LODGroup>()||low.GetComponentInParent<LODGroup>()||high.GetComponentInChildren<LODGroup>()||low.GetComponentInChildren<LODGroup>())throw new Exception("Existing LODGroup: inspect before replacing");
  if(high.transform.parent!=low.transform.parent)throw new Exception("LOD sources have different parents");
  if(low.GetComponentsInChildren<Collider>(true).Length>0||low.GetComponentsInChildren<MonoBehaviour>(true).Length>0||low.GetComponentsInChildren<Rigidbody>(true).Length>0)throw new Exception("Low source has physics/scripts: explicit review required");
  var cfg=JsonUtility.FromJson<Config>(File.ReadAllText(Backup+"/Config.json"));
  var hr=high.GetComponentsInChildren<Renderer>(true);var lr=low.GetComponentsInChildren<Renderer>(true);
  int ht=high.GetComponentsInChildren<MeshFilter>(true).Sum(f=>(int)f.sharedMesh.GetIndexCount(0)/3),lt=low.GetComponentsInChildren<MeshFilter>(true).Sum(f=>(int)f.sharedMesh.GetIndexCount(0)/3);
  if(lt>=ht||lt==0)throw new Exception("LOD2 does not reduce triangles");
  if(!EditorSceneManager.SaveScene(scene,Backup+"/Before.unity",true))throw new Exception("Scene backup failed");
  string protectedBefore=string.Join("\n",UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)));
  var position=high.transform.position;var rotation=high.transform.rotation;var scale=high.transform.lossyScale;var highBounds=BoundsOf(high);
  int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Generator LOD setup");
  var holder=new GameObject("Jeneratör LOD");Undo.RegisterCreatedObjectUndo(holder,"Generator LOD setup");holder.transform.SetParent(high.transform.parent,false);holder.transform.position=position;holder.transform.rotation=Quaternion.identity;holder.transform.localScale=Vector3.one;
  Undo.SetTransformParent(high.transform,holder.transform,"Generator LOD setup");Undo.SetTransformParent(low.transform,holder.transform,"Generator LOD setup");Undo.RecordObject(low.transform,"Align generator low detail");low.transform.rotation=Quaternion.Euler(cfg.rotationDelta)*low.transform.rotation;low.transform.localScale*=cfg.lowScale;low.transform.position+=highBounds.center-BoundsOf(low).center;
  if(Vector3.Distance(high.transform.position,position)>.0001f||Quaternion.Angle(high.transform.rotation,rotation)>.001f||Vector3.Distance(high.transform.lossyScale,scale)>.001f)throw new Exception("High detail transform changed");
  var lowBounds=BoundsOf(low);if(Vector3.Distance(lowBounds.center,highBounds.center)>.01f)throw new Exception("Bounds not aligned");
  for(int i=0;i<3;i++)if(Mathf.Abs(lowBounds.size[i]-highBounds.size[i])/Mathf.Max(.001f,highBounds.size[i])>.2f)throw new Exception("LOD silhouette size mismatch");
  var group=Undo.AddComponent<LODGroup>(holder);var levels=new[]{new LOD(cfg.highThreshold,hr),new LOD(cfg.lowThreshold,lr)};levels[0].fadeTransitionWidth=.15f;levels[1].fadeTransitionWidth=.15f;group.SetLODs(levels);group.fadeMode=cfg.crossFade?LODFadeMode.CrossFade:LODFadeMode.None;group.animateCrossFading=false;group.RecalculateBounds();
  string after=string.Join("\n",UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).OrderBy(c=>c.GetInstanceID()).Select(c=>EditorJsonUtility.ToJson(c)));
  if(after!=protectedBefore)throw new Exception("Collider serialization changed");
  if(group.GetLODs().Length!=2||group.GetLODs()[0].renderers.Intersect(group.GetLODs()[1].renderers).Any())throw new Exception("LOD renderer overlap");
  Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
  Selection.activeGameObject=holder;File.WriteAllText(Backup+"/Result.txt","PASS saved "+scene.path+"\nLOD0 triangles="+ht+"; LOD1 triangles="+lt+"; reduction="+(100f*(1-(float)lt/ht)).ToString("0.0")+"%\nHigh/low threshold="+cfg.highThreshold+"/"+cfg.lowThreshold+"; CrossFade="+cfg.crossFade+"\nCenters="+highBounds.center+"/"+lowBounds.center+"; sizes="+highBounds.size+"/"+lowBounds.size+"\nHigh transform and collider serialization preserved.\n");
 }
}
