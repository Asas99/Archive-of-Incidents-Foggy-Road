using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class GrassContinuitySetup
{
 const string Request="Library/GrassContinuity.install";
 static GrassContinuitySetup(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  if(!File.Exists(Request)||EditorApplication.isUpdating||EditorApplication.isCompiling)return;
  if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
  File.Delete(Request);string backup=File.ReadAllText("Library/GrassContinuityBackup.path").Trim();
  try{Install(backup);}catch(Exception e){File.WriteAllText(backup+"/Error.txt",e.ToString());Debug.LogException(e);}
 }
 static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
 public static void Install(string backup)
 {
  var manager=Object.FindFirstObjectByType<SceneOptimizationController>();
  if(!manager||!manager.profile||manager.gameObject.scene.path!="Assets/game prototype scene.unity"||Application.isPlaying)throw new Exception("Open the optimized main scene in edit mode");
  var scene=manager.gameObject.scene;EditorSceneManager.SaveScene(scene,backup+"/OpenSceneBefore.unity",true);
  var profile=manager.profile;Undo.RecordObject(profile,"Continuous 150 metre grass");
  profile.farGrassStart=0;profile.farGrassFull=0;profile.farGrassFade=130;profile.farGrassEnd=150;
  var grass=manager.GetComponent<FarGrassRenderer>();if(!grass)throw new Exception("Existing grass renderer missing");
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainDemoScene_URP/Prefabs/Details/Grass_A.prefab");var mesh=prefab.GetComponent<MeshFilter>().sharedMesh;
  if(mesh.GetIndexCount(0)/3!=128)throw new Exception("Approved lightweight grass mesh changed");
  var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/SceneOptimization/Generated/Materials/FarGrass.mat");
  if(!material||ShaderUtil.ShaderHasError(material.shader)||material.FindPass("Universal Forward")<0)throw new Exception("Grass shader invalid");
  material.SetFloat("Distance_Fade_Start",10000);material.SetFloat("Distance_Fade_End",11000);material.enableInstancing=true;EditorUtility.SetDirty(material);
  var report=new StringBuilder();var caches=new System.Collections.Generic.List<FarGrassCache>();Physics.SyncTransforms();
  foreach(var terrain in manager.terrains)
  {
   var data=terrain.terrainData;string dataPath=AssetDatabase.GetAssetPath(data);AssetDatabase.SaveAssetIfDirty(data);string before=Hash(dataPath);
   File.Copy(dataPath,backup+"/"+Path.GetFileName(dataPath)+".before",true);
   var prototype=data.detailPrototypes.First(p=>p.prototype&&p.prototype.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==mesh));
   var built=VegetationRangeInstaller.Build(terrain,mesh,material,prototype);
   string path="Assets/SceneOptimization/Generated/FarGrass_"+AssetDatabase.AssetPathToGUID(dataPath)+".asset";
   var cache=AssetDatabase.LoadAssetAtPath<FarGrassCache>(path);if(!cache){cache=built;AssetDatabase.CreateAsset(cache,path);}else{EditorUtility.CopySerialized(built,cache);Object.DestroyImmediate(built);}
   EditorUtility.SetDirty(cache);caches.Add(cache);
   if(Hash(dataPath)!=before)throw new Exception("TerrainData unexpectedly changed");
   report.AppendLine("TerrainData unchanged (trees/heights/detail maps): "+before);
   report.AppendLine("Cache clumps="+cache.instanceCount+" patches="+cache.patches.Length+" triangles/clump=128; source detail hash="+cache.sourceDetailHash);
  }
  Undo.RecordObject(grass,"Continuous grass cache");grass.caches=caches.ToArray();grass.profile=profile;
  VegetationRangeInstaller.ConfigureMaterials(manager);manager.ApplyTerrain();
  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
  var features=renderer.rendererFeatures.OfType<FarGrassFeature>().ToArray();if(features.Length!=1)throw new Exception("Expected one far grass feature");features[0].SetActive(true);
  EditorUtility.SetDirty(profile);EditorUtility.SetDirty(grass);EditorUtility.SetDirty(manager);foreach(var t in manager.terrains)EditorUtility.SetDirty(t);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
  report.AppendLine("PASS continuous base0-150m; outer fade130-150m; same painted species dimensions. Near details follow the saved profile. No scene planting or height changes. No FPS benchmark or push.");
  File.WriteAllText(backup+"/Installed.txt",report.ToString());
 }
}
