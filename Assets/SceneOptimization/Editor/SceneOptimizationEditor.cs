using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class SceneOptimizationEditor
{
 const string Root="Assets/SceneOptimization/Generated";
 static SceneOptimizationEditor(){EditorApplication.update+=Poll;}
 static bool sawBakeRunning;
 static string Backup=>File.ReadAllText("Library/SceneOptimizationBackup.path").Trim();
 static void Poll()
 {
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(File.Exists("Library/SceneOptimization.finalize")){File.Delete("Library/SceneOptimization.finalize");try{Finish();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}return;}
  if(File.Exists("Library/SceneOptimization.install"))
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
   File.Delete("Library/SceneOptimization.install");try{Apply();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}
  }
  if(File.Exists("Library/SceneOptimization.baking")&&StaticOcclusionCulling.isRunning)sawBakeRunning=true;
  if(File.Exists("Library/SceneOptimization.baking")&&!StaticOcclusionCulling.isRunning&&(sawBakeRunning||(DateTime.UtcNow-File.GetLastWriteTimeUtc("Library/SceneOptimization.baking")).TotalSeconds>5))
  {
   File.Delete("Library/SceneOptimization.baking");try{Finish();}catch(Exception e){File.WriteAllText(Backup+"/Error.txt",e.ToString());Debug.LogException(e);}
  }
 }
 [MenuItem("Tools/Foggy Road/Scene Optimization/Analiz")]
 public static void Analyze()=>SceneOptimizationBridge.Audit();
 static void BackupAsset(string path)
 {
  if(string.IsNullOrEmpty(path)||!File.Exists(path))return;string dest=Backup+"/AssetBackups/"+path;
  if(File.Exists(dest))return;Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(path,dest);
  if(File.Exists(path+".meta"))File.Copy(path+".meta",dest+".meta",true);
 }
 static void SaveObject(Object o){EditorUtility.SetDirty(o);}
 static void EnsureFolder(string path){path=path.Replace('\\','/');if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
 static bool IsProtected(Renderer renderer)
 {
  if(renderer.GetComponentInParent<Rigidbody>()||renderer.GetComponentInParent<Animator>()||renderer.GetComponentInParent<ReflectionActor>()||renderer.GetComponent<GlassSurfaceVariation>())return true;
  for(var t=renderer.transform;t;t=t.parent)
  {
   if(t.CompareTag("Car")||t.CompareTag("NPC")||t.gameObject.layer==LayerMask.NameToLayer("pickable"))return true;
   string n=t.name.ToLowerInvariant();
   if(n.Contains("benzinlik")||n.Contains("door")||n.Contains("kapı")||n.Contains("kapi")||n.Contains("window")||n.Contains("pencere")||n.Contains("threshold")||n.Contains("duvar")||n.Contains("wall")||n.Contains("kolon"))return true;
  }
  return false;
 }
 static string GeneratedShader(Shader shader,string category)
 {
  string source=AssetDatabase.GetAssetPath(shader);if(source.EndsWith(".shadergraph"))
  {
   string target=Root+"/Shaders/"+AssetDatabase.AssetPathToGUID(source)+"_"+category+".shader";
   if(File.Exists(target)){var previous=AssetDatabase.LoadAssetAtPath<Shader>(target);if(!previous||ShaderUtil.ShaderHasError(previous))throw new Exception("Existing generated shader has errors: "+target);return target;}
   Type importer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter")).FirstOrDefault(t=>t!=null);
   var method=importer?.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).FirstOrDefault(m=>m.Name=="GetShaderText"&&m.GetParameters().Length==4&&m.GetParameters()[3].IsOut);
   if(method==null)throw new Exception("Shader Graph source generation unavailable");
   object[] args={source,null,null,null};string text=(string)method.Invoke(null,args);
   string shaderName="FoggyRoad/Optimization/Graph/"+AssetDatabase.AssetPathToGUID(source)+"/"+category;
   // Keep the generated lighting, wind and material property layout.
   text=new System.Text.RegularExpressions.Regex("Shader \"[^\"]+\"").Replace(text,"Shader \""+shaderName+"\"",1);
   const string vary="Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
   if(!text.Contains(vary))throw new Exception("Unsupported Shader Graph varying layout: "+source);
   text=text.Replace(vary,"Assets/SceneOptimization/Shaders/"+category+"GraphVaryings.hlsl");
   EnsureFolder(Path.GetDirectoryName(target));File.WriteAllText(target,text);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
   var generated=AssetDatabase.LoadAssetAtPath<Shader>(target);
   if(!generated||ShaderUtil.ShaderHasError(generated))throw new Exception("Generated shader compilation failed: "+source);
   return target;
  }
  if(shader.name=="Universal Render Pipeline/Lit"&&(category=="Small"||category=="Medium"))return "Assets/SceneOptimization/Shaders/Lit"+category+".shader";
  throw new Exception("No verified fade adapter: "+shader.name);
 }
 static Material FadeMaterial(Material original,string category)
 {
  if(!original)throw new Exception("Missing material");
  if(AssetDatabase.GetAssetPath(original).StartsWith(Root+"/"))return original;
  string path=Root+"/Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))+"_"+category+".mat";
  var found=AssetDatabase.LoadAssetAtPath<Material>(path);if(found)return found;
  var m=new Material(original);m.name=original.name+" ("+category+" distance)";
  if(category=="Grass"&&m.HasProperty("Distance_Fade_Start")&&m.HasProperty("Distance_Fade_End")){var profile=Object.FindFirstObjectByType<SceneOptimizationController>()?.profile;m.SetFloat("Distance_Fade_Start",profile?profile.grassStart:25);m.SetFloat("Distance_Fade_End",profile?profile.grassEnd:35);}
  else m.shader=AssetDatabase.LoadAssetAtPath<Shader>(GeneratedShader(original.shader,category));
  foreach(var name in original.GetTexturePropertyNames())if(m.HasProperty(name)){m.SetTexture(name,original.GetTexture(name));m.SetTextureScale(name,original.GetTextureScale(name));m.SetTextureOffset(name,original.GetTextureOffset(name));}
  if(m.HasProperty("_Surface")&&m.GetFloat("_Surface")>.5f){Object.DestroyImmediate(m);throw new Exception("Transparent material excluded: "+original.name);}
  EnsureFolder(Path.GetDirectoryName(path));AssetDatabase.CreateAsset(m,path);return m;
 }
 static GameObject FadePrefab(GameObject source,string category,StringBuilder report)
 {
  string sourcePath=AssetDatabase.GetAssetPath(source);if(sourcePath.StartsWith(Root+"/"))return source;
  string target=Root+"/Prefabs/"+AssetDatabase.AssetPathToGUID(sourcePath)+"_"+category+".prefab";
  var exists=AssetDatabase.LoadAssetAtPath<GameObject>(target);if(exists)return exists;
  var copy=Object.Instantiate(source);copy.name=source.name;
  try
  {
   foreach(var r in copy.GetComponentsInChildren<Renderer>(true))
   {
    r.sharedMaterials=r.sharedMaterials.Select(m=>FadeMaterial(m,category)).ToArray();
    if(category=="Grass")r.shadowCastingMode=ShadowCastingMode.Off;
   }
   EnsureFolder(Path.GetDirectoryName(target));var saved=PrefabUtility.SaveAsPrefabAsset(copy,target);report.AppendLine("PROTOTYPE "+sourcePath+" -> "+target);return saved;
  }finally{Object.DestroyImmediate(copy);}
 }
 [MenuItem("Tools/Foggy Road/Scene Optimization/Dengeli Profili Uygula")]
 public static void Apply()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/game prototype scene.unity"||Application.isPlaying)throw new Exception("Open main scene in edit mode");
  if(StaticOcclusionCulling.isRunning)throw new Exception("Occlusion bake already running");
  if(!File.Exists(Backup+"/OpenSceneBefore.unity"))SceneOptimizationBridge.Audit();
  EnsureFolder(Root);var report=new StringBuilder();
  var manager=Object.FindFirstObjectByType<SceneOptimizationController>();if(!manager)manager=new GameObject("Scene Optimization").AddComponent<SceneOptimizationController>();
  string profilePath=Root+"/Balanced.asset";var profile=AssetDatabase.LoadAssetAtPath<SceneOptimizationProfile>(profilePath);if(!profile){profile=ScriptableObject.CreateInstance<SceneOptimizationProfile>();AssetDatabase.CreateAsset(profile,profilePath);}manager.profile=profile;
  manager.terrains=Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene&&!manager.smallVegetationTerrains.Contains(t)).ToArray();
  foreach(var terrain in manager.terrains)
  {
   var data=terrain.terrainData;BackupAsset(AssetDatabase.GetAssetPath(data));
   if(!AssetDatabase.GetAssetPath(data).StartsWith(Root+"/"))
   {string target=Root+"/Terrain_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data))+".asset";if(!File.Exists(target))AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data),target);data=AssetDatabase.LoadAssetAtPath<TerrainData>(target);terrain.terrainData=data;var collider=terrain.GetComponent<TerrainCollider>();if(collider)collider.terrainData=data;}
   var trees=data.treePrototypes;for(int i=0;i<trees.Length;i++)if(trees[i].prefab)trees[i].prefab=FadePrefab(trees[i].prefab,"Tree",report);data.treePrototypes=trees;
   var details=data.detailPrototypes;for(int i=0;i<details.Length;i++)if(details[i].prototype)details[i].prototype=FadePrefab(details[i].prototype,"Grass",report);data.detailPrototypes=details;SaveObject(data);
   // Do not rewrite detail maps, tree instances, scatter mode, or density.
  }
  foreach(var dependency in AssetDatabase.GetDependencies(scene.path))if(dependency.EndsWith("OcclusionCullingData.asset"))BackupAsset(dependency);
  SceneVegetationTerrain.Split(manager,Root,report);
  VegetationRangeInstaller.ConfigureMaterials(manager);
  manager.ApplyTerrain();
  var distanceGroups=new List<LODGroup>();var distanceEnds=new List<float>();var smallGroups=new List<bool>();
  for(int i=0;i<manager.distanceGroups.Length&&i<manager.distanceEnds.Length;i++)if(manager.distanceGroups[i]){distanceGroups.Add(manager.distanceGroups[i]);distanceEnds.Add(manager.distanceEnds[i]);smallGroups.Add(i<manager.smallDistanceGroups.Length?manager.smallDistanceGroups[i]:manager.distanceEnds[i]==80);}
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene))
  {
   if(IsProtected(r))continue;float size=Mathf.Max(r.bounds.size.x,r.bounds.size.y,r.bounds.size.z);string category=null;
   int layer=r.gameObject.layer;string n=r.name.ToLowerInvariant();
   if(layer==LayerMask.NameToLayer("Guardrail")&&size<40)category="Medium";
   else if(layer==LayerMask.NameToLayer("Props")&&size<12)category=size<=3?"Small":"Medium";
   else if((r.gameObject.isStatic&&size<=3)||(size<12&&(n.Contains("rock")||n.Contains("stone")||n.Contains("bush")||n.Contains("fern"))))category=size<=3?"Small":"Medium";
   if(category==null)continue;
   if(r.GetComponentInParent<LODGroup>()){report.AppendLine("EXISTING_LOD retained "+r.name);continue;}
   if(r.HasPropertyBlock()){report.AppendLine("SKIP MPB "+r.name);continue;}
   if(Vector3.Distance(r.bounds.center,r.transform.position)>r.bounds.extents.magnitude+1){report.AppendLine("SKIP remote pivot "+r.name);continue;}
   try
   {
    var mats=r.sharedMaterials.Select(m=>FadeMaterial(m,category)).ToArray();Undo.RecordObject(r,"Environment distance fade");r.sharedMaterials=mats;
    var group=Undo.AddComponent<LODGroup>(r.gameObject);group.SetLODs(new[]{new LOD(.00001f,new Renderer[]{r})});group.RecalculateBounds();group.fadeMode=LODFadeMode.None;
    distanceGroups.Add(group);distanceEnds.Add(category=="Small"?profile.smallEnd:profile.mediumEnd);smallGroups.Add(category=="Small");
    var flags=GameObjectUtility.GetStaticEditorFlags(r.gameObject);GameObjectUtility.SetStaticEditorFlags(r.gameObject,flags&~StaticEditorFlags.BatchingStatic);
    report.AppendLine("DISTANCE "+category+" "+r.name);SaveObject(r);
   }catch(Exception e){report.AppendLine("UNRESOLVED "+r.name+" "+e.Message);}
  }
  if(distanceGroups.Count>0){manager.distanceGroups=distanceGroups.ToArray();manager.distanceEnds=distanceEnds.ToArray();manager.smallDistanceGroups=smallGroups.ToArray();}
  // Every screen camera keeps native per-view culling; no renderer.enabled or SetActive toggles.
  foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene&&c.targetTexture==null))
  {camera.useOcclusionCulling=false;SaveObject(camera);}
  foreach(var lc in Object.FindObjectsByType<FoggyRoad_LayerCulling>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene))
  {lc.Uygula();SaveObject(lc);}
  foreach(var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(m=>m&&m.gameObject.scene==scene))
  {
   if(mb.GetType().Name=="DynamicWeatherSystem"){var so=new SerializedObject(mb);var p=so.FindProperty("overrideTerrainSettings");if(p!=null){p.boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}}
   if(mb.GetType().Name=="FoggyRoadFoliageOptimization"){mb.enabled=false;SaveObject(mb);}
  }
  SceneVehicleLods.Install(scene,report);
  QualitySettings.lodBias=profile.lodBias;QualitySettings.enableLODCrossFade=true;
  // Safe tree shadow distance is derived from actual instance scale and first non-shadow LOD.
  float safe=profile.shadowDistance;
  foreach(var terrain in manager.terrains)foreach(var instance in terrain.terrainData.treeInstances)
  {var prefab=terrain.terrainData.treePrototypes[instance.prototypeIndex].prefab;var g=prefab?prefab.GetComponent<LODGroup>():null;if(!g)continue;var levels=g.GetLODs();for(int i=1;i<levels.Length;i++)if(levels[i].renderers.All(x=>!x||x.shadowCastingMode==ShadowCastingMode.Off)){float h=levels[i-1].screenRelativeTransitionHeight;if(h>0)safe=Mathf.Min(safe,.9f*g.size*instance.heightScale*profile.lodBias/(2*Mathf.Tan(30*Mathf.Deg2Rad)*h));break;}}
  profile.shadowDistance=Mathf.Max(5,safe);QualitySettings.shadowDistance=profile.shadowDistance;QualitySettings.shadowCascades=2;
  var pipeline=(QualitySettings.renderPipeline?QualitySettings.renderPipeline:GraphicsSettings.defaultRenderPipeline)as UniversalRenderPipelineAsset;
  if(!pipeline)throw new Exception("URP pipeline missing");var graphicsSO=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);graphicsSO.FindProperty("m_BrgStripping").intValue=(int)BatchRendererGroupStrippingMode.KeepAll;graphicsSO.ApplyModifiedPropertiesWithoutUndo();BackupAsset(AssetDatabase.GetAssetPath(pipeline));var pipelineSO=new SerializedObject(pipeline);
  Set(pipelineSO,"m_GPUResidentDrawerEnableOcclusionCullingInCameras",true);Set(pipelineSO,"m_UseSRPBatcher",true);Set(pipelineSO,"m_EnableLODCrossFade",true);Set(pipelineSO,"m_ShadowDistance",profile.shadowDistance);Set(pipelineSO,"m_ShadowCascadeCount",2);Set(pipelineSO,"m_MainLightShadowmapResolution",2048);Set(pipelineSO,"m_SoftShadowQuality",2);Set(pipelineSO,"m_CascadeBorder",.3f);Set(pipelineSO,"m_SmallMeshScreenPercentage",0f);pipelineSO.ApplyModifiedPropertiesWithoutUndo();SaveObject(pipeline);
  // Static occluders: opaque fixed walls/large solids only. Open frames and glazing cannot seal passages.
  int occluders=0;
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene))
  {
   var flags=GameObjectUtility.GetStaticEditorFlags(r.gameObject);bool moving=r.GetComponentInParent<Rigidbody>()||r.GetComponentInParent<Animator>()||r.GetComponentInParent<ReflectionActor>();string n=r.name.ToLowerInvariant();
   bool opening=n.Contains("door")||n.Contains("kapı")||n.Contains("kapi")||n.Contains("window")||n.Contains("pencere")||n.Contains("threshold")||r.GetComponent<GlassSurfaceVariation>();
   bool foliage=r.sharedMaterials.Any(m=>m&&(m.shader.name.Contains("Tree")||m.shader.name.Contains("Grass")));
   bool wall=r.gameObject.isStatic&&!moving&&!opening&&!foliage&&(n.Contains("duvar")||n.Contains("wall")||n.Contains("concrete"))&&r.sharedMaterials.All(m=>m&&m.renderQueue<2500);
   if(wall){flags|=StaticEditorFlags.OccluderStatic;occluders++;}else flags&=~StaticEditorFlags.OccluderStatic;
   if(moving)flags&=~(StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
   else if(r.gameObject.isStatic&&!foliage)flags|=StaticEditorFlags.OccludeeStatic;
   GameObjectUtility.SetStaticEditorFlags(r.gameObject,flags);
  }
  if(occluders==0)throw new Exception("No safe occluders found");
  SaveObject(profile);SaveObject(manager);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  report.AppendLine("Distance groups="+manager.distanceGroups.Length+" safe shadow distance="+profile.shadowDistance+" occluders="+occluders);File.WriteAllText(Backup+"/Applied.txt",report.ToString());
  File.WriteAllText("Library/SceneOptimization.baking","pending");StaticOcclusionCulling.GenerateInBackground();
 }
 static void Set(SerializedObject so,string name,bool value){var p=so.FindProperty(name);if(p==null)throw new Exception("Missing setting "+name);p.boolValue=value;}
 static void Set(SerializedObject so,string name,int value){var p=so.FindProperty(name);if(p==null)throw new Exception("Missing setting "+name);p.intValue=value;}
 static void Set(SerializedObject so,string name,float value){var p=so.FindProperty(name);if(p==null)throw new Exception("Missing setting "+name);p.floatValue=value;}
 static void Finish()
 {
  var manager=Object.FindFirstObjectByType<SceneOptimizationController>();if(!manager)throw new Exception("Controller missing after bake");
  var scenePath=manager.gameObject.scene.path;var bakeStart=File.GetLastWriteTimeUtc(Backup+"/Applied.txt");
  bool fresh=AssetDatabase.GetDependencies(scenePath).Where(p=>p.EndsWith("OcclusionCullingData.asset")).Any(p=>File.GetLastWriteTimeUtc(p)>=bakeStart);
  if(!fresh)throw new Exception("No newly written occlusion asset; stale occlusion is not enabled");
  var dataPath=AssetDatabase.GetDependencies(scenePath).First(p=>p.EndsWith("OcclusionCullingData.asset"));
  var dataAsset=AssetDatabase.LoadMainAssetAtPath(dataPath);if(!dataAsset||new FileInfo(dataPath).Length<=1024)throw new Exception("Missing/empty native occlusion asset");
  var dataSO=new SerializedObject(dataAsset);var pvs=dataSO.FindProperty("m_PVSData");if(pvs!=null&&pvs.isArray&&pvs.arraySize==0)throw new Exception("Empty PVS array");
  File.WriteAllText(Backup+"/OcclusionValidation.txt","Native asset="+dataAsset.GetType().Name+" file bytes="+new FileInfo(dataPath).Length+" PVS="+(pvs!=null&&pvs.isArray?pvs.arraySize.ToString():"native serialized data")+" memory counter="+StaticOcclusionCulling.umbraDataSize+"\n");
  var report=new StringBuilder();SceneVegetationTerrain.Split(manager,Root,report);
  manager.smallDistanceGroups=manager.distanceEnds.Select(end=>Mathf.Approximately(end,80)).ToArray();
  VegetationRangeInstaller.ConfigureMaterials(manager);
  manager.ApplyTerrain();File.WriteAllText(Backup+"/Finalization.txt",report.ToString());
  manager.occlusionReady=true;foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==manager.gameObject.scene&&c.targetTexture==null)){c.useOcclusionCulling=true;SaveObject(c);}
  SaveObject(manager);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);
  File.WriteAllText(Backup+"/Installed.txt","PASS Scene saved; fresh occlusion file bytes="+new FileInfo(dataPath).Length+"; no FPS benchmark or push.\n");
 }
 [MenuItem("Tools/Foggy Road/Scene Optimization/Geri Yukle")]
 public static void Restore()
 {
  if(Application.isPlaying||StaticOcclusionCulling.isRunning)throw new Exception("Stop play/bake before restore");
  string backup=Backup;var manager=Object.FindFirstObjectByType<SceneOptimizationController>();if(manager)manager.enabled=false;
  File.Copy(backup+"/OpenSceneBefore.unity","Assets/game prototype scene.unity",true);
  foreach(var file in new[]{"ProjectSettings/QualitySettings.asset","ProjectSettings/GraphicsSettings.asset","Assets/Settings/PC_RPAsset.asset","Assets/Settings/PC_Renderer.asset"})File.Copy(backup+"/"+file,file,true);
  if(Directory.Exists(backup+"/AssetBackups"))foreach(var source in Directory.GetFiles(backup+"/AssetBackups","*",SearchOption.AllDirectories)){string dest=source.Substring((backup+"/AssetBackups/").Length);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(source,dest,true);}
  AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/game prototype scene.unity",OpenSceneMode.Single);
 }
}
