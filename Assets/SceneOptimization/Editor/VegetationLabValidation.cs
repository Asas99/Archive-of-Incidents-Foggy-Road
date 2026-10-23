using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Explicit local requests only. Normal editor startup never changes scenes or settings.
[InitializeOnLoad]
public static class VegetationLabValidation
{
 const string Request="Library/VegetationLab.request",Result="Library/VegetationLabValidation.txt",Pending="FoggyRoad.VegetationLabValidation.Pending";
 static VegetationLabValidation(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
  if(SessionState.GetBool(Pending,false))
  {
   if(!EditorApplication.isPlaying)return;
   if(!Object.FindFirstObjectByType<SceneOptimizationController>())return;
   SessionState.SetBool(Pending,false);
   try{ValidatePreview();}catch(Exception e){File.WriteAllText(Result,"FAIL "+e);Debug.LogException(e);}
   finally{EditorApplication.isPlaying=false;}
   return;
  }
  if(!File.Exists(Request))return;
  string command=File.ReadAllText(Request).Trim();File.Delete(Request);
  try
  {
   if(command=="OPEN"){VegetationLabWindow.Open();File.WriteAllText(Result,"OPEN: tool compiled and opened; no settings applied.");}
   else if(command=="PREVIEW_TEST")
   {
    if(EditorApplication.isPlaying)
    {
     try{ValidatePreview();}finally{EditorApplication.isPlaying=false;}
     return;
    }
    if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start validation from Edit Mode.");
    var controller=Object.FindFirstObjectByType<SceneOptimizationController>();if(!controller)throw new InvalidOperationException("Open the gameplay scene first.");
    if(controller.gameObject.scene.isDirty && !EditorSceneManager.SaveScene(controller.gameObject.scene))throw new IOException("Scene save failed.");
    File.WriteAllText(Result,"RUNNING: Play Mode preview restoration test.");SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
   }
   else throw new ArgumentException(command);
  }
  catch(Exception e){File.WriteAllText(Result,"FAIL "+e);Debug.LogException(e);}
 }
 static void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);}
 static void ValidatePreview()
 {
  var c=Object.FindFirstObjectByType<SceneOptimizationController>();
  var source=c.profile;string json=JsonUtility.ToJson(source);
  var terrains=c.terrains.Where(t=>t&&t.terrainData).ToArray();
  var data=terrains.Select(t=>t.terrainData).ToArray();var counts=data.Select(d=>d.treeInstanceCount).ToArray();
  var colliders=terrains.Select(t=>t.GetComponent<TerrainCollider>()?.terrainData).ToArray();
  var densities=terrains.Select(t=>t.detailObjectDensity).ToArray();
  var window=ScriptableObject.CreateInstance<VegetationLabWindow>();
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  try
  {
   typeof(VegetationLabWindow).GetField("controller",flags).SetValue(window,c);
   var settings=VegetationOptimizationSettings.Read(source);settings.treeDensity=.5f;settings.nativeGrassDensity=.6f;settings.treeLodBias=.7f;
   typeof(VegetationLabWindow).GetField("draft",flags).SetValue(window,settings);
   window.BeginPreview();
   Require(c.profile!=source && !AssetDatabase.Contains(c.profile),"Preview profile must be transient.");
   for(int i=0;i<terrains.Length;i++)
   {
    Require(terrains[i].terrainData.treeInstanceCount==Mathf.FloorToInt(counts[i]*.5f),"50% tree subset count.");
    Require(data[i].treeInstanceCount==counts[i],"Painted trees were modified.");
    Require(terrains[i].GetComponent<TerrainCollider>()?.terrainData==colliders[i],"Collider changed.");
   }
   settings.treeDensity=.75f;window.BeginPreview();
   for(int i=0;i<terrains.Length;i++)
   {
    Require(terrains[i].terrainData.treeInstanceCount==Mathf.FloorToInt(counts[i]*.75f),"75% tree subset count.");
    Require(Mathf.Abs(terrains[i].detailObjectDensity-densities[i]*.6f)<.001f,"Grass density compounded.");
   }
   window.EndPreview();
   Require(c.profile==source && JsonUtility.ToJson(source)==json,"Source profile was modified.");
   for(int i=0;i<terrains.Length;i++)
   {
    Require(terrains[i].terrainData==data[i],"Original TerrainData was not restored.");
    Require(terrains[i].GetComponent<TerrainCollider>()?.terrainData==colliders[i],"Original collider was not preserved.");
    Require(Mathf.Abs(terrains[i].detailObjectDensity-densities[i])<.001f,"Original density was not restored.");
   }
   File.WriteAllText(Result,"PASS: 50% / 75% runtime subsets; no compounded grass density; source profile and painted data unchanged; TerrainData, colliders and density restored. Trees: "+string.Join(",",counts));
  }
  finally{window.EndPreview();Object.DestroyImmediate(window);}
 }
}
