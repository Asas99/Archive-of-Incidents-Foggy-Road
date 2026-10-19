using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class SceneVegetationTerrain
{
 public static void Split(SceneOptimizationController manager,string root,StringBuilder report)
 {
  var helpers=new List<Terrain>(manager.smallVegetationTerrains.Where(t=>t));
  foreach(var terrain in manager.terrains)
  {
   var data=terrain.terrainData;var prototypes=data.treePrototypes;var small=new HashSet<int>();
   for(int i=0;i<prototypes.Length;i++)
   {
    var p=prototypes[i].prefab;if(!p||p.GetComponentsInChildren<Collider>(true).Length>0)continue;
    if(p.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m&&m.HasProperty("Distance_Fade_Start")&&m.HasProperty("Distance_Fade_End"))))small.Add(i);
   }
   var instances=data.treeInstances;var close=instances.Where(t=>small.Contains(t.prototypeIndex)).ToArray();
   report.AppendLine("TERRAIN_INSTANCE_COUNTS trees="+instances.Count(t=>!small.Contains(t.prototypeIndex))+" small="+close.Length);
   if(close.Length==0)continue;
   // These small prototypes have no colliders. Keep all existing physical tree instances and height data unchanged in shape.
   string path=root+"/SmallVegetation_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data))+".asset";
   var childData=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
   if(!childData){childData=new TerrainData();childData.heightmapResolution=33;childData.size=data.size;AssetDatabase.CreateAsset(childData,path);}
   childData.treePrototypes=prototypes;childData.SetTreeInstances(close,false);
   data.SetTreeInstances(instances.Where(t=>!small.Contains(t.prototypeIndex)).ToArray(),false);EditorUtility.SetDirty(data);EditorUtility.SetDirty(childData);
   var go=new GameObject("Small Vegetation (render only)");Undo.RegisterCreatedObjectUndo(go,"Small vegetation native distance");go.transform.SetParent(terrain.transform,false);
   var helper=go.AddComponent<Terrain>();helper.terrainData=childData;helper.materialTemplate=terrain.materialTemplate;helper.drawHeightmap=false;helper.drawTreesAndFoliage=true;helper.drawInstanced=true;helper.detailObjectDensity=0;helper.detailObjectDistance=0;helper.treeDistance=manager.profile.smallVegetationEnd;helper.treeBillboardDistance=manager.profile.smallVegetationEnd;helper.treeMaximumFullLODCount=400;helper.allowAutoConnect=false;helper.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;helpers.Add(helper);
  }
  manager.smallVegetationTerrains=helpers.ToArray();
 }
}
