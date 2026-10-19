using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class SceneOptimizationBridge
{
 static SceneOptimizationBridge(){EditorApplication.update+=Poll;}
 static void Poll(){if(!File.Exists("Library/SceneOptimization.audit")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/SceneOptimization.audit");try{Audit();}catch(Exception e){File.WriteAllText("Recovery/SceneOptimizationError.txt",e.ToString());}}
 public static void Audit()
 {
  string backup=File.ReadAllText("Library/SceneOptimizationBackup.path").Trim();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/game prototype scene.unity")throw new Exception("Wrong scene "+scene.path);
  if(!File.Exists(backup+"/OpenSceneBefore.unity")&&!Application.isPlaying&&!EditorSceneManager.SaveScene(scene,backup+"/OpenSceneBefore.unity",true))throw new Exception("Scene backup failed");
  var sb=new StringBuilder();sb.AppendLine("Playing="+Application.isPlaying+" scene="+scene.path);
  foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.None))
  {
   var d=t.terrainData;sb.AppendLine("TERRAIN "+t.name+" data="+AssetDatabase.GetAssetPath(d)+" trees="+d.treeInstanceCount+" detailDistance="+t.detailObjectDistance);
   int i=0;foreach(var x in d.treePrototypes){sb.AppendLine("TREE "+i+" count="+d.treeInstances.Count(t=>t.prototypeIndex==i)+" "+AssetDatabase.GetAssetPath(x.prefab));foreach(var r in x.prefab.GetComponentsInChildren<Renderer>(true))sb.AppendLine("  "+r.name+" "+string.Join(";",r.sharedMaterials.Select(m=>m?m.shader.name+"|"+AssetDatabase.GetAssetPath(m):"null")));i++;}
   i=0;foreach(var x in d.detailPrototypes){sb.AppendLine("DETAIL "+i+++" "+AssetDatabase.GetAssetPath(x.prototype)+" inst="+x.useInstancing+" mode="+x.renderMode);if(x.prototype)foreach(var r in x.prototype.GetComponentsInChildren<Renderer>(true))sb.AppendLine("  "+r.name+" "+string.Join(";",r.sharedMaterials.Select(m=>m?m.shader.name+"|"+AssetDatabase.GetAssetPath(m):"null")));}
  }
  foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene))
  {var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;long tris=0;if(mesh)for(int i=0;i<mesh.subMeshCount;i++)tris+=(long)mesh.GetIndexCount(i)/3;sb.AppendLine("RENDERER|"+GlobalObjectId.GetGlobalObjectIdSlow(r)+"|"+r.name+"|"+LayerMask.LayerToName(r.gameObject.layer)+"|"+tris+"|"+r.bounds.size+"|"+r.HasPropertyBlock()+"|"+GameObjectUtility.GetStaticEditorFlags(r.gameObject)+"|"+string.Join(";",r.sharedMaterials.Select(m=>m?m.shader.name:"null")));}
  File.WriteAllText(backup+"/Inventory.txt",sb.ToString());
 }
}
