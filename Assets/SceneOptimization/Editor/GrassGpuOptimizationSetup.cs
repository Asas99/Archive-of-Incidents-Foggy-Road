using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

// Explicit request only; normal domain reload/brush refresh never installs or resets a profile.
[InitializeOnLoad]
public static class GrassGpuOptimizationSetup
{
 const string Request="Library/GrassGpuOptimization.install";
 const string Pending="Library/GrassGpuOptimization.baking";
 static readonly Dictionary<string,string> terrainHashes=new Dictionary<string,string>();
 static string transformsBefore,physicsBefore,glassBefore;
 static bool sawBake;
 static string Backup=>File.ReadAllText("Library/GrassGpuOptimizationBackup.path").Trim();
 static GrassGpuOptimizationSetup(){EditorApplication.update+=Poll;}
 static string Hash(string path){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
 static SceneOptimizationController Manager()=>Object.FindObjectsByType<SceneOptimizationController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>c.gameObject.scene.path=="Assets/game prototype scene.unity");
 static IEnumerable<T> SceneObjects<T>(SceneOptimizationController c) where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.gameObject.scene==c.gameObject.scene).OrderBy(x=>x.GetInstanceID());
 static string Capture<T>(SceneOptimizationController c) where T:Component=>string.Join("\n",SceneObjects<T>(c).Select(x=>EditorJsonUtility.ToJson(x)));
 static string Glass(SceneOptimizationController c)=>string.Join("\n",SceneObjects<MonoBehaviour>(c).Where(x=>x&&x.GetType().Name.StartsWith("GasStationPlanarGlass",StringComparison.Ordinal)).Select(x=>EditorJsonUtility.ToJson(x)));
 static void Poll()
 {
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(File.Exists(Request)&&!File.Exists(Pending))
  {
   // Never interrupt a player's test or discard Play Mode state.
   if(EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(Request);try{Install();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}return;
  }
  if(!File.Exists(Pending))return;
  if(StaticOcclusionCulling.isRunning){sawBake=true;return;}
  if(!sawBake&&(DateTime.UtcNow-File.GetLastWriteTimeUtc(Pending)).TotalSeconds<10)return;
  File.Delete(Pending);
  try{Finish();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}
 }
 static void Install()
 {
  var c=Manager();VegetationOptimizationStorage.RequireEditable(c);
  Directory.CreateDirectory(Backup);
  // Keep the actual open scene, including unsaved object edits, as a separate copy.
  if(!EditorSceneManager.SaveScene(c.gameObject.scene,Backup+"/OpenSceneBefore.unity",true))throw new IOException("Open scene backup failed");
  transformsBefore=Capture<Transform>(c);physicsBefore=Capture<Collider>(c);glassBefore=Glass(c);terrainHashes.Clear();
  foreach(var t in c.terrains.Concat(c.smallVegetationTerrains).Where(t=>t&&t.terrainData))
  {
   AssetDatabase.SaveAssetIfDirty(t.terrainData);string path=AssetDatabase.GetAssetPath(t.terrainData);
   terrainHashes[path]=Hash(path);File.Copy(path,Backup+"/"+Path.GetFileName(path)+".before",true);
  }
  File.WriteAllText(Backup+"/ProfileBefore.json",EditorJsonUtility.ToJson(c.profile,true));
  string settingsBackup=VegetationOptimizationStorage.Backup(c);
  EditorPrefs.SetString(VegetationOptimizationStorage.LastKey(c),settingsBackup);
  var p=c.profile;
  // These are the only profile values changed by this installation. Tree/LOD/shadow settings stay as saved.
  p.grassStart=25;p.grassEnd=35;p.farGrassStart=p.farGrassFull=0;p.farGrassFade=78;p.farGrassEnd=90;p.farGrassDensity=1;
  p.farGrassDistanceThinning=true;p.farDensityFullDistance=25;p.farDensityMidDistance=55;p.farDensityLowDistance=78;p.farDensityMid=p.farDensityLow=.5f;p.farGrassGpuOcclusion=true;
  var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
  var features=data.rendererFeatures.OfType<FarGrassFeature>().ToArray();if(features.Length!=1)throw new Exception("Expected one existing grass renderer feature");
  var feature=features[0];feature.grassCulling=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/SceneOptimization/Shaders/GrassOcclusion.compute");
  feature.indirectGrassShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SceneOptimization/Generated/Shaders/FarGrassGpu.shader");
  if(!feature.grassCulling||!feature.indirectGrassShader||ShaderUtil.ShaderHasError(feature.indirectGrassShader))throw new Exception("GPU grass sources did not compile");
  feature.SetActive(true);EditorUtility.SetDirty(feature);EditorUtility.SetDirty(data);
  VegetationRangeInstaller.ConfigureMaterials(c);c.ApplyTerrain();
  var report=new StringBuilder();int changed=0,walls=0;
  foreach(var r in SceneObjects<MeshRenderer>(c))
  {
   var flags=GameObjectUtility.GetStaticEditorFlags(r.gameObject);var next=flags;
   bool moving=r.GetComponentInParent<Rigidbody>()||r.GetComponentInParent<Animator>()||r.GetComponentInParent<ReflectionActor>();
   bool opening=false;
   for(var t=r.transform;t;t=t.parent)
   {string n=t.name.ToLowerInvariant();if(n.Contains("door")||n.Contains("kapı")||n.Contains("kapi")||n.Contains("window")||n.Contains("pencere")||n.Contains("threshold"))opening=true;}
   bool glazing=r.GetComponent<GlassSurfaceVariation>()||r.sharedMaterials.Any(m=>m&&m.renderQueue>=2500);
   bool foliage=r.sharedMaterials.Any(m=>m&&(m.shader.name.IndexOf("Grass",StringComparison.OrdinalIgnoreCase)>=0||m.shader.name.IndexOf("Tree",StringComparison.OrdinalIgnoreCase)>=0));
   string name=r.name.ToLowerInvariant();bool wall=r.gameObject.isStatic&&!moving&&!opening&&!glazing&&!foliage&&(name.Contains("wall")||name.Contains("duvar")||name.Contains("concrete"))&&r.sharedMaterials.Length>0&&r.sharedMaterials.All(m=>m&&m.renderQueue<2450);
   if(moving||opening||glazing||foliage)next&=~StaticEditorFlags.OccluderStatic;
   else if(wall){next|=StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic;walls++;}
   if(next!=flags){GameObjectUtility.SetStaticEditorFlags(r.gameObject,next);changed++;report.AppendLine(r.name+": "+flags+" -> "+next);}
  }
  if(walls==0)throw new Exception("No safe opaque static walls found");
  c.occlusionReady=false;EditorUtility.SetDirty(c);EditorUtility.SetDirty(p);
  foreach(var t in c.terrains.Concat(c.smallVegetationTerrains).Where(t=>t))EditorUtility.SetDirty(t);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
  // The previous bake predates Terrain/object edits and has no current geometry fingerprint.
  // Back up its native data before replacing it with a bake from the actual open scene.
  foreach(var path in AssetDatabase.GetDependencies(c.gameObject.scene.path).Where(x=>x.EndsWith("OcclusionCullingData.asset")))File.Copy(path,Backup+"/OcclusionCullingData.before",true);
  report.AppendLine("Safe wall renderers="+walls+"; changed static flags="+changed+". Trees remain native Terrain/LOD; no custom tree occlusion.");
  File.WriteAllText(Backup+"/OcclusionAudit.txt",report.ToString());
  File.WriteAllText(Pending,"Fresh bake requested "+DateTime.UtcNow.ToString("O"));sawBake=false;
  StaticOcclusionCulling.GenerateInBackground();
 }
 static void Finish()
 {
  var c=Manager();string path=AssetDatabase.GetDependencies(c.gameObject.scene.path).Single(x=>x.EndsWith("OcclusionCullingData.asset"));
  if(new FileInfo(path).Length<1024||File.GetLastWriteTimeUtc(path)<File.GetLastWriteTimeUtc(Backup+"/OcclusionAudit.txt"))throw new Exception("Fresh occlusion bake missing; native culling left disabled");
  var report=new StringBuilder();
  foreach(var pair in terrainHashes){if(Hash(pair.Key)!=pair.Value)throw new Exception("TerrainData changed: "+pair.Key);report.AppendLine("TerrainData byte-identical: "+pair.Key+" "+pair.Value);}
  if(Capture<Transform>(c)!=transformsBefore||Capture<Collider>(c)!=physicsBefore||Glass(c)!=glassBefore)throw new Exception("Protected transforms, colliders or glass serialization changed");
  report.AppendLine("PASS transforms/colliders/glass unchanged; tree profile retained. Native occlusion bytes="+new FileInfo(path).Length);
  c.occlusionReady=true;foreach(var camera in SceneObjects<Camera>(c).Where(camera=>camera.targetTexture==null)){camera.useOcclusionCulling=camera.GetComponent<DistantMountainBackdrop>()==null;EditorUtility.SetDirty(camera);}
  EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);
  File.WriteAllText(Backup+"/Integrity.txt",report.ToString());
  File.WriteAllText(Backup+"/Installed.txt","PASS open scene saved. GPU far grass enabled, near25-35m retained, density1 at25m /0.5 at55m /0.5 at78m /fade78-90m. Fresh static occlusion bake verified. No FPS benchmark, computer use, build or push.\n");
 }
}
