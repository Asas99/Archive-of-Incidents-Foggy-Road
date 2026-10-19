using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// One-shot local recovery. Only runs when an explicit request file exists.
[InitializeOnLoad]
public static class TerrainSelectiveRestore
{
 const string Request="Library/TerrainSelectiveRestore.request";
 const string Target="Assets/SceneOptimization/Generated/Terrain_65edf7f5a6f7b584fad05ff484e8ecc8.asset";
 const string Temporary="Assets/SceneOptimization/Generated/__TerrainRestoreSource.asset";
 static TerrainSelectiveRestore(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
  File.Delete(Request);string backup=File.ReadAllText("Library/TerrainUndoBackup.path").Trim();
  try{Restore(backup);}catch(Exception e){File.WriteAllText(backup+"/RestoreError.txt",e.ToString());Debug.LogException(e);}
 }
 static string PaintedHash(TerrainData data)
 {
  // Hash all non-height paint, including holes and detail density, to verify preservation.
  using(var hash=System.Security.Cryptography.SHA256.Create())
  using(var memory=new MemoryStream())
  using(var writer=new BinaryWriter(memory))
  {
   writer.Write(data.detailWidth);writer.Write(data.detailHeight);writer.Write(data.detailPrototypes.Length);
   for(int layer=0;layer<data.detailPrototypes.Length;layer++)foreach(int value in data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer))writer.Write(value);
   foreach(float value in data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight))writer.Write(value);
   foreach(bool value in data.GetHoles(0,0,data.holesResolution,data.holesResolution))writer.Write(value);
   writer.Flush();return Convert.ToBase64String(hash.ComputeHash(memory.ToArray()));
  }
 }
 public static void Restore(string backup)
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/game prototype scene.unity"||Application.isPlaying)throw new Exception("Main scene must be open in edit mode");
  var data=AssetDatabase.LoadAssetAtPath<TerrainData>(Target);if(!data)throw new Exception("Target TerrainData missing");
  var linked=Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene&&t.terrainData==data).ToArray();
  if(linked.Length!=1)throw new Exception("Expected exactly one scene terrain using target");
  string source=File.ReadAllText("Library/VegetationRangeBackup.path").Trim()+"/"+Target;
  if(!File.Exists(source))throw new Exception("Approved terrain backup missing");
  if(File.Exists(Temporary))throw new Exception("Temporary recovery asset already exists; refusing overwrite");
  EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
  File.Copy(Target,backup+"/TerrainBeforeRestore.asset",true);File.Copy(Target+".meta",backup+"/TerrainBeforeRestore.asset.meta",true);
  if(!EditorSceneManager.SaveScene(scene,backup+"/OpenSceneBeforeRestore.unity",true))throw new Exception("Open scene backup failed");
  File.Copy(source,Temporary);AssetDatabase.ImportAsset(Temporary,ImportAssetOptions.ForceSynchronousImport);
  try
  {
   var old=AssetDatabase.LoadAssetAtPath<TerrainData>(Temporary);
   if(!old||old.heightmapResolution!=data.heightmapResolution||old.size!=data.size)throw new Exception("Heightmap layout differs; restore cancelled");
   string paintedBefore=PaintedHash(data);int countBefore=data.treeInstanceCount;
   var currentPrototypes=data.treePrototypes.ToList();var previousPrototypes=old.treePrototypes;var indexMap=new int[previousPrototypes.Length];
   for(int i=0;i<previousPrototypes.Length;i++)
   {
    var previous=previousPrototypes[i];int match=currentPrototypes.FindIndex(p=>p.prefab==previous.prefab);
    if(match<0){match=currentPrototypes.Count;currentPrototypes.Add(previous);}indexMap[i]=match;
   }
   var trees=old.treeInstances;
   for(int i=0;i<trees.Length;i++)trees[i].prototypeIndex=indexMap[trees[i].prototypeIndex];
   int resolution=old.heightmapResolution;var heights=old.GetHeights(0,0,resolution,resolution);
   // Store an object snapshot for a single Undo; restore only the requested fields.
   Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Restore terrain trees and heights to 22:07");Undo.RegisterCompleteObjectUndo(data,"Restore terrain trees and heights to 22:07");
   data.SetHeights(0,0,heights);data.treePrototypes=currentPrototypes.ToArray();data.SetTreeInstances(trees,false);
   foreach(var terrain in linked)terrain.Flush();
   string paintedAfter=PaintedHash(data);
   if(paintedBefore!=paintedAfter)throw new Exception("Unexpected paint change detected");
   var restored=data.GetHeights(0,0,resolution,resolution);int mismatch=0;float maxError=0;
   for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){float error=Mathf.Abs(restored[z,x]-heights[z,x]);maxError=Mathf.Max(maxError,error);if(error>.000001f)mismatch++;}
   if(mismatch!=0||!data.treeInstances.SequenceEqual(trees))throw new Exception("Restored height/tree verification failed");
   EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(undo);
   var report=new StringBuilder();report.AppendLine("PASS trees and heights restored to the approved 22:07 state.");
   report.AppendLine("Source="+source);report.AppendLine("Target="+Target);report.AppendLine("Trees before="+countBefore+" after="+trees.Length);
   report.AppendLine("Height samples="+(resolution*resolution)+" mismatches="+mismatch+" maxError="+maxError);
   report.AppendLine("Grass/alphamap/holes SHA256 unchanged="+paintedBefore);report.AppendLine("Profile, glass, scene objects and renderer settings were not edited. Scene and TerrainData saved.");
   File.WriteAllText(backup+"/Restored.txt",report.ToString());
  }
  finally{AssetDatabase.DeleteAsset(Temporary);}
 }
}
