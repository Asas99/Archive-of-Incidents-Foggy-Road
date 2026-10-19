using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class VegetationRangeInstaller
{
 const string Root="Assets/SceneOptimization/Generated";
 static string Backup=>File.ReadAllText("Library/VegetationRangeBackup.path").Trim();
 static VegetationRangeInstaller(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  if(!File.Exists("Library/VegetationRange.install")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
  File.Delete("Library/VegetationRange.install");
  try{Install();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}
 }
 public static Shader FarShader()
 {
  const string path=Root+"/Shaders/FarGrass.shader";
  if(File.Exists(path))return AssetDatabase.LoadAssetAtPath<Shader>(path);
  var importer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter")).First(t=>t!=null);
  var method=importer.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).First(m=>m.Name=="GetShaderText"&&m.GetParameters().Length==4&&m.GetParameters()[3].IsOut);
  object[] args={"Assets/TerrainDemoScene_URP/ShaderGraphs/TerrainGrass.shadergraph",null,null,null};
  string text=(string)method.Invoke(null,args);
  text=new System.Text.RegularExpressions.Regex("Shader \"[^\"]+\"").Replace(text,"Shader \"FoggyRoad/Optimization/Far Grass\"",1);
  text=text.Replace("Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl","Assets/SceneOptimization/Shaders/FarGrassGraphVaryings.hlsl");
  File.WriteAllText(path,text);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  return AssetDatabase.LoadAssetAtPath<Shader>(path);
 }
 public static void ConfigureMaterials(SceneOptimizationController manager)
 {
  var p=manager.profile;
  // Only touch materials actually assigned to these vegetation prototypes, never glass or far grass.
  foreach(var t in manager.terrains.Where(t=>t))foreach(var prototype in t.terrainData.detailPrototypes)
   if(prototype.prototype)foreach(var r in prototype.prototype.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)SetFade(m,p.grassStart,p.grassEnd);
  foreach(var t in manager.smallVegetationTerrains.Where(t=>t))foreach(var prototype in t.terrainData.treePrototypes)
   if(prototype.prefab)foreach(var r in prototype.prefab.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)SetFade(m,p.smallVegetationStart,p.smallVegetationEnd);
  manager.nearGrassMaterials=manager.terrains.Where(t=>t).SelectMany(t=>t.terrainData.detailPrototypes).Where(d=>d.prototype).SelectMany(d=>d.prototype.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.HasProperty("Distance_Fade_Start")).Distinct().ToArray();
  manager.smallVegetationMaterials=manager.smallVegetationTerrains.Where(t=>t).SelectMany(t=>t.terrainData.treePrototypes).Where(d=>d.prefab).SelectMany(d=>d.prefab.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.HasProperty("Distance_Fade_Start")).Distinct().ToArray();
 }
 static void SetFade(Material m,float start,float end)
 {
  if(!m||!AssetDatabase.GetAssetPath(m).StartsWith(Root+"/")||!m.HasProperty("Distance_Fade_Start"))return;
  m.SetFloat("Distance_Fade_Start",start);m.SetFloat("Distance_Fade_End",end);EditorUtility.SetDirty(m);
 }
 public static int DetailHash(TerrainData data)
 {
  unchecked{int hash=17;for(int layer=0;layer<data.detailPrototypes.Length;layer++){var map=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer);foreach(int n in map)hash=hash*31+n;}return hash;}
 }
 static bool GrassPrototype(GameObject prefab)
 {
  if(!prefab)return false;string path=AssetDatabase.GetAssetPath(prefab);
  if(path.StartsWith(Root+"/Prefabs/")){string guid=Path.GetFileName(path).Split('_')[0];string original=AssetDatabase.GUIDToAssetPath(guid);if(!string.IsNullOrEmpty(original))path=original;}
  string name=Path.GetFileNameWithoutExtension(path);return name.IndexOf("Grass",StringComparison.OrdinalIgnoreCase)>=0||name.IndexOf("Fern",StringComparison.OrdinalIgnoreCase)>=0;
 }
 sealed class GrassLayer {public int[,] map;public DetailPrototype prototype;public Bounds bounds;}
 public static FarGrassCache Build(Terrain terrain,Mesh mesh,Material material,DetailPrototype scalePrototype)
 {
  var data=terrain.terrainData;var maps=new List<GrassLayer>();
  for(int layer=0;layer<data.detailPrototypes.Length;layer++)
  {
   var prototype=data.detailPrototypes[layer];
   if(GrassPrototype(prototype.prototype))
   {
    var sourceMesh=prototype.prototype.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
    maps.Add(new GrassLayer{map=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,layer),prototype=prototype,bounds=sourceMesh?sourceMesh.bounds:mesh.bounds});
   }
  }
  if(maps.Count==0)throw new Exception("No painted grass layers on "+terrain.name);
  var cache=ScriptableObject.CreateInstance<FarGrassCache>();cache.mesh=mesh;cache.material=material;cache.sourcePosition=terrain.transform.position;cache.sourceDetailHash=DetailHash(data);
  var patches=new Dictionary<Vector2Int,List<Matrix4x4>>();var random=new System.Random(194722);
  var hits=new RaycastHit[32];var ownCollider=terrain.GetComponent<TerrainCollider>();
  // One stable candidate per square metre, selected only from painted grass pixels.
  for(int z=0;z<Mathf.FloorToInt(data.size.z);z++)for(int x=0;x<Mathf.FloorToInt(data.size.x);x++)
  {
   float px=x+(float)random.NextDouble(),pz=z+(float)random.NextDouble();
   int ix=Mathf.Min(data.detailWidth-1,(int)(px/data.size.x*data.detailWidth)),iz=Mathf.Min(data.detailHeight-1,(int)(pz/data.size.z*data.detailHeight));
   int total=0;foreach(var painted in maps)total+=painted.map[iz,ix];if(total==0)continue;
   float u=px/data.size.x,v=pz/data.size.z;if(data.IsHole(Mathf.Min(data.holesResolution-1,(int)(u*data.holesResolution)),Mathf.Min(data.holesResolution-1,(int)(v*data.holesResolution))))continue;
   Vector3 pos=terrain.transform.position+new Vector3(px,data.GetInterpolatedHeight(u,v),pz);
   if(VegetationBrushPaint.BlockedAtGround(pos,ownCollider,hits))continue;
   // Keep the silhouette size of the painted source species, not Grass_A's small raw mesh.
   int choice=random.Next(total);var selected=maps[0];foreach(var painted in maps){choice-=painted.map[iz,ix];if(choice<0){selected=painted;break;}}
   var dimensions=selected.prototype;var sourceBounds=selected.bounds;var lowBounds=mesh.bounds;
   float sourceWidth=Mathf.Lerp(dimensions.minWidth,dimensions.maxWidth,(float)random.NextDouble());
   float sourceHeight=Mathf.Lerp(dimensions.minHeight,dimensions.maxHeight,(float)random.NextDouble());
   float width=sourceWidth*Mathf.Max(sourceBounds.size.x,sourceBounds.size.z)/Mathf.Max(.001f,Mathf.Max(lowBounds.size.x,lowBounds.size.z));
   float height=sourceHeight*sourceBounds.size.y/Mathf.Max(.001f,lowBounds.size.y);
   pos.y+=sourceBounds.min.y*sourceHeight-lowBounds.min.y*height;
   var key=new Vector2Int(x/16,z/16);if(!patches.TryGetValue(key,out var list))patches.Add(key,list=new List<Matrix4x4>());
   var normal=data.GetInterpolatedNormal(u,v);var rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,(float)random.NextDouble()*360,0);
   list.Add(Matrix4x4.TRS(pos,rotation,new Vector3(width,height,width)));
  }
  cache.patches=patches.OrderBy(p=>p.Key.y).ThenBy(p=>p.Key.x).Select(pair=>
  {
   var patch=new FarGrassCache.Patch{matrices=pair.Value.ToArray(),centers=new Vector3[pair.Value.Count],radii=new float[pair.Value.Count]};
   for(int i=0;i<patch.matrices.Length;i++)
   {
    var m=patch.matrices[i];patch.centers[i]=m.MultiplyPoint3x4(mesh.bounds.center);
    // Include the original shader's wind and tip bending in conservative culling bounds.
    patch.radii[i]=mesh.bounds.extents.magnitude*Mathf.Max(m.GetColumn(0).magnitude,m.GetColumn(1).magnitude,m.GetColumn(2).magnitude)+1;
    var bounds=new Bounds(patch.centers[i],Vector3.one*patch.radii[i]*2);if(i==0)patch.bounds=bounds;else patch.bounds.Encapsulate(bounds);
   }
   cache.instanceCount+=patch.matrices.Length;return patch;
  }).ToArray();return cache;
 }
 [MenuItem("Tools/Foggy Road/Scene Optimization/300m Trees + 150m Grass")]
 public static void Install()
 {
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/game prototype scene.unity"||Application.isPlaying)throw new Exception("Main scene must be open in edit mode");
  if(!File.Exists(Backup+"/OpenSceneBefore.unity"))EditorSceneManager.SaveScene(scene,Backup+"/OpenSceneBefore.unity",true);
  var manager=Object.FindFirstObjectByType<SceneOptimizationController>();if(!manager||!manager.profile)throw new Exception("Existing optimized controller/profile missing");
  var report=new StringBuilder();var p=manager.profile;p.grassStart=25;p.grassEnd=35;p.treeStart=270;p.treeEnd=300;p.treeBillboardDistance=200;p.treeCrossFadeLength=40;p.smallVegetationStart=80;p.smallVegetationEnd=100;p.farGrassStart=0;p.farGrassFull=0;p.farGrassFade=130;p.farGrassEnd=150;p.farGrassDensity=1;p.farGrassDistanceThinning=true;p.farDensityFullDistance=60;p.farDensityMidDistance=100;p.farDensityLowDistance=130;p.farDensityMid=.7f;p.farDensityLow=.45f;p.farDensityFadeHalfWidth=.04f;
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainDemoScene_URP/Prefabs/Details/Grass_A.prefab");var filter=prefab.GetComponent<MeshFilter>();var original=prefab.GetComponent<Renderer>().sharedMaterial;var mesh=filter.sharedMesh;
  if(mesh.subMeshCount!=1||mesh.GetIndexCount(0)/3>256)throw new Exception("Grass_A mesh differs from approved light model");
  var shader=FarShader();if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Far grass shader errors");
  string materialPath=Root+"/Materials/FarGrass.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
  if(!material){material=new Material(original);AssetDatabase.CreateAsset(material,materialPath);}material.shader=shader;
  foreach(var name in original.GetTexturePropertyNames())if(material.HasProperty(name)){material.SetTexture(name,original.GetTexture(name));material.SetTextureScale(name,original.GetTextureScale(name));material.SetTextureOffset(name,original.GetTextureOffset(name));}
  material.enableInstancing=true;material.SetFloat("Distance_Fade_Start",10000);material.SetFloat("Distance_Fade_End",11000);material.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(material);
  int forward=material.FindPass("Universal Forward");if(forward<0)throw new Exception("Missing forward pass in far grass shader");
  var renderer=manager.GetComponent<FarGrassRenderer>();if(!renderer)renderer=Undo.AddComponent<FarGrassRenderer>(manager.gameObject);renderer.profile=p;var caches=new List<FarGrassCache>();
  Physics.SyncTransforms();
  foreach(var terrain in manager.terrains)
  {
   var data=terrain.terrainData;int before=DetailHash(data);
   var scale=data.detailPrototypes.First(d=>d.prototype&&d.prototype.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==mesh));
   string path=Root+"/FarGrass_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data))+".asset";
   var built=Build(terrain,mesh,material,scale);var cache=AssetDatabase.LoadAssetAtPath<FarGrassCache>(path);
   if(cache){EditorUtility.CopySerialized(built,cache);Object.DestroyImmediate(built);}else {cache=built;AssetDatabase.CreateAsset(cache,path);}
   caches.Add(cache);EditorUtility.SetDirty(cache);
   if(before!=DetailHash(data))throw new Exception("Original detail maps changed");
   report.AppendLine("GRASS "+terrain.name+" patches="+cache.patches.Length+" clumps="+cache.instanceCount+" triangles/clump="+(mesh.GetIndexCount(0)/3)+" detail hash unchanged="+before);
   foreach(var prototype in data.treePrototypes)
   {
    var tree=prototype.prefab;if(!tree||!AssetDatabase.GetAssetPath(tree).StartsWith(Root+"/"))continue;var lod=tree.GetComponent<LODGroup>();if(!lod)continue;
    var levels=lod.GetLODs();if(levels.Length==0)continue;levels[levels.Length-1].screenRelativeTransitionHeight=.00001f;lod.SetLODs(levels);EditorUtility.SetDirty(lod);EditorUtility.SetDirty(tree);PrefabUtility.SavePrefabAsset(tree);
   }
  }
  renderer.caches=caches.ToArray();ConfigureMaterials(manager);manager.ApplyTerrain();
  var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
  var feature=rendererData.rendererFeatures.OfType<FarGrassFeature>().FirstOrDefault();if(!feature){feature=ScriptableObject.CreateInstance<FarGrassFeature>();feature.name="Visible far grass";AssetDatabase.AddObjectToAsset(feature,rendererData);rendererData.rendererFeatures.Add(feature);}feature.SetActive(true);EditorUtility.SetDirty(rendererData);
  EditorUtility.SetDirty(p);EditorUtility.SetDirty(manager);EditorUtility.SetDirty(renderer);foreach(var terrain in manager.terrains.Concat(manager.smallVegetationTerrains))EditorUtility.SetDirty(terrain);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  report.AppendLine("PASS saved main scene; trees300m/fade270-300, near details25-35, continuous grass0-150/fade130-150. No FPS measurement, push or new occlusion bake.");
  File.WriteAllText(Backup+"/Installed.txt",report.ToString());
 }
}
