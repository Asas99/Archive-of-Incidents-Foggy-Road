using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Writes only small detail-map patches. Never snapshots the entire TerrainData to Undo.
public static class VegetationBrushPaint
{
 public sealed class Choice {public int layer;public float weight;}
 public sealed class Patch {public TerrainData data;public int layer,x,z;public int[,] before;}
 public sealed class Result {public readonly List<Patch> patches=new List<Patch>();public int cells;}
 public static string Name(DetailPrototype p)
 {
  if(!p.prototype)return p.prototypeTexture?p.prototypeTexture.name:"Missing plant";
  string path=AssetDatabase.GetAssetPath(p.prototype);string name=Path.GetFileNameWithoutExtension(path);
  string first=name.Split('_')[0];if(first.Length==32){var original=AssetDatabase.GUIDToAssetPath(first);if(!string.IsNullOrEmpty(original))return Path.GetFileNameWithoutExtension(original);}
  return p.prototype.name;
 }
 public static bool IsPlant(DetailPrototype p)
 {
  if(!p.prototype&&!p.prototypeTexture)return false;
  string name=Name(p).ToLowerInvariant();return name.Contains("grass")||name.Contains("fern")||name.Contains("egrelti")||name.Contains("eğrelti");
 }
 public static string Key(DetailPrototype p)=>p.prototype?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.prototype)):p.prototypeTexture?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.prototypeTexture)):"";
 public static bool IsGroundTerrain(Terrain t)=>t&&t.terrainData&&t.drawHeightmap&&t.terrainData.detailWidth>0;
 public static Result Paint(Terrain terrain,Vector3 center,float radius,float density,float softness,float maxSlope,bool avoidSolids,IList<Choice> palette,HashSet<int> visited,System.Random random)
 {
  var result=new Result();if(!IsGroundTerrain(terrain)||radius<=0||palette.Count==0)return result;
  var data=terrain.terrainData;int w=data.detailWidth,h=data.detailHeight;Vector3 start=terrain.transform.position,size=data.size;
  if(center.x+radius<start.x||center.x-radius>start.x+size.x||center.z+radius<start.z||center.z-radius>start.z+size.z)return result;
  int x0=Mathf.Clamp(Mathf.FloorToInt((center.x-radius-start.x)/size.x*w),0,w-1),x1=Mathf.Clamp(Mathf.FloorToInt((center.x+radius-start.x)/size.x*w),0,w-1);
  int z0=Mathf.Clamp(Mathf.FloorToInt((center.z-radius-start.z)/size.z*h),0,h-1),z1=Mathf.Clamp(Mathf.FloorToInt((center.z+radius-start.z)/size.z*h),0,h-1);
  int width=x1-x0+1,height=z1-z0+1;var choices=palette.Where(c=>c.layer>=0&&c.layer<data.detailPrototypes.Length&&c.weight>0&&IsPlant(data.detailPrototypes[c.layer])).ToArray();
  if(choices.Length==0)return result;float sum=choices.Sum(c=>c.weight),maxWeight=choices.Max(c=>c.weight);
  var maps=new Dictionary<int,int[,]>();var previous=new Dictionary<int,int[,]>();
  foreach(var c in choices.Distinct())if(!maps.ContainsKey(c.layer))maps[c.layer]=data.GetDetailLayer(x0,z0,width,height,c.layer);
  bool coverage=data.detailScatterMode==DetailScatterMode.CoverageMode;
  var hits=new RaycastHit[32];var own=terrain.GetComponent<TerrainCollider>();
  for(int z=0;z<height;z++)for(int x=0;x<width;x++)
  {
   int cell=(z0+z)*w+x0+x;
   float u=(x0+x+.5f)/w,v=(z0+z+.5f)/h;float px=start.x+u*size.x,pz=start.z+v*size.z;
   float distance=new Vector2(px-center.x,pz-center.z).magnitude;if(distance>radius)continue;
   if(data.GetSteepness(u,v)>maxSlope||data.IsHole(Mathf.Min(data.holesResolution-1,(int)(u*data.holesResolution)),Mathf.Min(data.holesResolution-1,(int)(v*data.holesResolution))))continue;
   if(avoidSolids)
   {
    Vector3 point=new Vector3(px,start.y+data.GetInterpolatedHeight(u,v),pz);if(BlockedAtGround(point,own,hits))continue;
   }
   float falloff=softness<=.001f?1:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1-softness,1,distance/radius));
   // Fill multiple species in every eligible core cell. Stable per-cell targets let
   // a soft edge strengthen as the brush passes over it, without unlimited stacking.
   visited.Add(cell);bool changed=false;
   foreach(var c in choices)
   {
    float noise=Noise(cell,c.layer);int target;
    if(coverage)target=Mathf.RoundToInt(255*Mathf.Clamp01(density*c.weight/maxWeight*falloff*(.8f+.2f*noise)));
    else
    {
     float expected=Mathf.Min(24,12*Mathf.Max(0,density)*falloff)*c.weight/sum;
     target=Mathf.Min(16,Mathf.FloorToInt(expected)+(noise<expected-Mathf.Floor(expected)?1:0));
    }
    if(target<=maps[c.layer][z,x])continue;
    if(!previous.ContainsKey(c.layer))previous[c.layer]=(int[,])maps[c.layer].Clone();
    maps[c.layer][z,x]=target;changed=true;
   }
   if(changed)result.cells++;
  }
  foreach(var entry in previous){data.SetDetailLayer(x0,z0,entry.Key,maps[entry.Key]);result.patches.Add(new Patch{data=data,layer=entry.Key,x=x0,z=z0,before=entry.Value});}
  if(result.cells>0)EditorUtility.SetDirty(data);return result;
 }
 static float Noise(int cell,int layer){unchecked{uint h=(uint)cell*747796405u+(uint)(layer+1)*2891336453u;h=(h^(h>>16))*2246822519u;return (h&65535)/65535f;}}
 public static bool BlockedAtGround(Vector3 point,TerrainCollider own,RaycastHit[] hits)
 {
  int count=Physics.RaycastNonAlloc(point+Vector3.up*5,Vector3.down,hits,5.1f,~0,QueryTriggerInteraction.Ignore);
  if(count==hits.Length)return true;
  // Leaves/branches overhead are not a road. Only near-ground supporting surfaces block grass.
  for(int i=0;i<count;i++)if(hits[i].collider!=own&&hits[i].point.y>point.y-.03f&&hits[i].point.y<=point.y+.75f)return true;
  return false;
 }
 public static void Restore(IEnumerable<Patch> patches){foreach(var p in patches.Reverse()){if(!p.data)continue;p.data.SetDetailLayer(p.x,p.z,p.layer,p.before);EditorUtility.SetDirty(p.data);}}
 public static void RefreshDistantGrass(IEnumerable<Terrain> terrains)
 {
  foreach(var manager in UnityEngine.Object.FindObjectsByType<SceneOptimizationController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
  {
   var renderer=manager.GetComponent<FarGrassRenderer>();if(!renderer)continue;
   foreach(var terrain in terrains.Where(t=>t&&manager.terrains.Contains(t)).Distinct())
   {
    var cache=renderer.caches.FirstOrDefault(c=>c&&Vector3.SqrMagnitude(c.sourcePosition-terrain.transform.position)<.001f);if(!cache)continue;
    var prototype=terrain.terrainData.detailPrototypes.FirstOrDefault(p=>p.prototype&&p.prototype.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==cache.mesh));
    var built=VegetationRangeInstaller.Build(terrain,cache.mesh,cache.material,prototype);EditorUtility.CopySerialized(built,cache);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(cache);
   }
  }
 }
}
