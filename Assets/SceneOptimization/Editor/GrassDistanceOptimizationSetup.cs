using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GrassDistanceOptimizationSetup
{
 static GrassDistanceOptimizationSetup(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  const string request="Library/GrassDistanceOptimization.install";
  if(!File.Exists(request)||EditorApplication.isUpdating||EditorApplication.isCompiling)return;
  if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
  File.Delete(request);string backup=File.ReadAllText("Library/GrassDistanceOptimizationBackup.path").Trim();
  try
  {
   var manager=UnityEngine.Object.FindFirstObjectByType<SceneOptimizationController>();
   if(!manager||!manager.profile||manager.gameObject.scene.path!="Assets/game prototype scene.unity")throw new Exception("Open the optimized main scene");
   var p=manager.profile;Undo.RecordObject(p,"Optimize dense grass distance");
   p.grassStart=25;p.grassEnd=35;p.farGrassStart=0;p.farGrassFull=0;p.farGrassFade=130;p.farGrassEnd=150;p.farGrassDensity=1;
   p.farGrassDistanceThinning=true;p.farDensityFullDistance=60;p.farDensityMidDistance=100;p.farDensityLowDistance=130;p.farDensityMid=.7f;p.farDensityLow=.45f;p.farDensityFadeHalfWidth=.04f;
   EditorUtility.SetDirty(p);
   // This existing path backs up the open scene, rebuilds the current paint cache,
   // verifies TerrainData byte equality, applies native detail distance and saves.
   GrassContinuitySetup.Install(backup);
   File.AppendAllText(backup+"/Installed.txt","PASS optimized profile: full detail0-25 / fade25-35; continuous base0-150; density1 at60m,0.7 at100m,0.45 at130m. No terrain paint, height or tree edits.\n");
  }
  catch(Exception e){File.WriteAllText(backup+"/Error.txt",e.ToString());Debug.LogException(e);}
 }
}
