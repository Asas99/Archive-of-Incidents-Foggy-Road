using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static partial class GlassLeanSetup
{
    public const string Output="Recovery/GlassLean";
    const string Root="Assets/GasStationGlass";
    static GlassLeanSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string file="Library/GlassLean.request";
        if(File.Exists(file)&&File.ReadAllText(file).Trim()=="atlas"&&EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        if(!File.Exists(file)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        string action=File.ReadAllText(file).Trim();File.Delete(file);Directory.CreateDirectory(Output);
        try{if(action=="atlas")InstallAtlas();else if(action=="install")Install();else if(action=="finalize")InstallCore(false);else if(action=="verify")Verify();else if(action=="diagnose")Diagnose();else if(action=="build")BuildBenchmark();else if(action=="performance")InstallPerformance();else if(action=="performance-verify")VerifyPerformance();else if(action=="frame-api")FrameDebuggerAPI();else if(action=="frame-proof")FrameProof();else if(action=="quality")QualityScreens();else Audit();}
        catch(Exception e){File.WriteAllText(Output+"/Error.txt",e.ToString());Debug.LogException(e);}
    }
    static GasStationPlanarGlass Manager()=>UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==SceneManager.GetActiveScene());
    static long Triangles(Mesh mesh){long count=0;if(mesh)for(int i=0;i<mesh.subMeshCount;i++)count+=(long)mesh.GetIndexCount(i)/3;return count;}
    public static void Audit()
    {
        var text=new StringBuilder();var scene=SceneManager.GetActiveScene();
        foreach(var actor in UnityEngine.Object.FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.gameObject.scene==scene))
        {
            actor.RefreshRenderers();text.AppendLine("ACTOR "+actor.name+" tag="+actor.tag+" position="+actor.transform.position);
            foreach(var renderer in actor.ReflectionDrawSources){if(!renderer)continue;var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;text.AppendLine("  "+renderer.name+" enabled="+renderer.enabled+" tris="+Triangles(mesh)+" asset="+AssetDatabase.GetAssetPath(mesh));}
        }
        File.WriteAllText(Output+"/Actors.txt",text.ToString());
    }
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Install Dynamic-Only Reflections On All Windows")]
    public static void Install()=>InstallCore(true);
    static void InstallCore(bool bake)
    {
        var manager=Manager();var scene=manager.gameObject.scene;
        if(scene.path!="Assets/game prototype scene.unity")throw new InvalidOperationException("Select the prototype scene.");
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Root+"/Backups");Directory.CreateDirectory(Root+"/Geometry");
        string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss"),backup=Root+"/Backups/BeforeDynamicOnly_"+stamp+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Open scene backup failed.");
        File.WriteAllText(Output+"/BeforeSettings.json",EditorJsonUtility.ToJson(manager,true));
        string cubeBackup=Output+"/PreviousCubes_"+stamp;Directory.CreateDirectory(cubeBackup);
        foreach(var file in Directory.GetFiles(Root+"/Reflections/Rooms"))File.Copy(file,cubeBackup+"/"+Path.GetFileName(file),true);
        var rendererPath=Root+"/Settings/Glass Dynamic Renderer.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if(!renderer)
        {
            AssetDatabase.CopyAsset(Root+"/Settings/Glass Reflection Renderer.asset",rendererPath);renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            var feature=ScriptableObject.CreateInstance<GlassDynamicReflectionFeature>();feature.name="Glass dynamic objects and depth";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Clear();renderer.rendererFeatures.Add(feature);
        }
        var serialized=new SerializedObject(renderer);serialized.FindProperty("m_OpaqueLayerMask").intValue=0;serialized.FindProperty("m_TransparentLayerMask").intValue=0;serialized.FindProperty("m_RenderingMode").intValue=(int)RenderingMode.ForwardPlus;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(!pipeline)throw new InvalidOperationException("URP required");
        var pipelineObject=new SerializedObject(pipeline);var list=pipelineObject.FindProperty("m_RendererDataList");int index=-1;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)index=i;
        if(index<0){index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;pipelineObject.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);}
        manager.dynamicRendererIndex=index;
        foreach(var tag in new[]{"Car","NPC"})foreach(var actor in GameObject.FindGameObjectsWithTag(tag))if(actor.scene==scene)ReflectionActor.Register(actor);
        // Existing imported traffic models are known assets even when the user has not tagged them yet.
        foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(filter.gameObject.scene!=scene||!filter.sharedMesh||!AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith("Assets/NPC_ARABALARI/",StringComparison.Ordinal))continue;
            var lod=filter.GetComponentInParent<LODGroup>();
            var actorRoot=lod?lod.gameObject:PrefabUtility.GetOutermostPrefabInstanceRoot(filter.gameObject);
            if(!actorRoot){var t=filter.transform;while(t.parent && t.parent.name!="benzinlik")t=t.parent;actorRoot=t.gameObject;}
            var parentActor=ReflectionActor.Register(actorRoot);
            if(lod)foreach(var nested in actorRoot.GetComponentsInChildren<ReflectionActor>(true))if(nested!=parentActor){nested.enabled=false;EditorUtility.SetDirty(nested);}
        }
        foreach(var door in UnityEngine.Object.FindObjectsByType<TwoSidedDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(door.gameObject.scene==scene)ReflectionActor.Register(door.gameObject);
        var text=new StringBuilder();int overrides=0;
        foreach(var actor in UnityEngine.Object.FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.gameObject.scene==scene))
        {
            actor.RefreshRenderers();var meshes=new List<ReflectionActor.MeshOverride>();
            foreach(var r in actor.ReflectionDrawSources)
            {
                if(!r||r is SkinnedMeshRenderer)continue;var original=r.GetComponent<MeshFilter>()?.sharedMesh;if(!original)continue;
                string path=AssetDatabase.GetAssetPath(original);Mesh best=null;
                var files=new List<string>();
                string directory=Path.GetDirectoryName(path)?.Replace('\\','/');
                if(!string.IsNullOrEmpty(directory))
                {
                    foreach(string lod in new[]{"lod2","lod1"})
                    {
                        string folder=directory.Replace("/lod0","/"+lod).Replace("/lod1","/"+lod);
                        if(Directory.Exists(folder))files.AddRange(Directory.GetFiles(folder,"*.fbx").Select(f=>f.Replace('\\','/')));
                    }
                    if(path.Contains("arabablendlod"))files.AddRange(Directory.GetFiles(directory,"arabablendlod*.fbx").Select(f=>f.Replace('\\','/')));
                }
                foreach(var candidate in files.Distinct())
                {
                    var candidates=AssetDatabase.LoadAllAssetsAtPath(candidate).OfType<Mesh>().Where(m=>m.name==original.name&&m.subMeshCount==original.subMeshCount);
                    foreach(var m in candidates){var size=m.bounds.size;var expected=original.bounds.size;if((size-expected).magnitude/Mathf.Max(0.001f,expected.magnitude)<0.15f&&Triangles(m)<Triangles(original)&&(!best||Triangles(m)<Triangles(best))&&(m.bounds.center-original.bounds.center).magnitude<expected.magnitude*0.1f)best=m;}
                }
                if(best){meshes.Add(new ReflectionActor.MeshOverride{renderer=r,mesh=best});overrides++;text.AppendLine(actor.name+" / "+r.name+" "+Triangles(original)+" -> "+Triangles(best));}
                else text.AppendLine(actor.name+" / "+r.name+": existing geometry "+Triangles(original)+" triangles; no compatible lower mesh found");
            }
            actor.reflectionMeshes=meshes.ToArray();EditorUtility.SetDirty(actor);PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
        }
        var occlusion=manager.GetComponent<GlassReflectionOcclusion>();if(!occlusion)occlusion=Undo.AddComponent<GlassReflectionOcclusion>(manager.gameObject);
        var depth=BuildOcclusion(manager);string meshPath=Root+"/Geometry/Station Reflection Occlusion.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(old){old.Clear();old.indexFormat=depth.indexFormat;old.vertices=depth.vertices;old.triangles=depth.triangles;old.RecalculateBounds();old.UploadMeshData(false);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(depth);depth=old;}else AssetDatabase.CreateAsset(depth,meshPath);
        occlusion.parts=new[]{new GlassReflectionOcclusion.Part{source=manager.transform,mesh=depth}};manager.reflectionOcclusion=occlusion;
        string materialPath=Root+"/Materials/Reflection Occlusion.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("Hidden/FoggyRoad/Glass Occlusion"));AssetDatabase.CreateAsset(material,materialPath);}manager.occlusionMaterial=material;
        manager.captureContent=GasStationPlanarGlass.CaptureContent.DynamicObjects;manager.maxActiveGroups=1;manager.liveUpdatesPerSecond=60;manager.resolutionScale=0.5f;
        EditorUtility.SetDirty(manager);EditorUtility.SetDirty(occlusion);AssetDatabase.SaveAssets();
        if(bake)RebakeWithoutDynamicDoors(manager);manager.RefreshBakedBindings();manager.InvalidateReflections();EditorSceneManager.MarkSceneDirty(scene);
        File.WriteAllText(Output+"/Installed.txt","Backup="+backup+"\nAll panes="+manager.groups.Sum(g=>g.windows.Length)+" groups="+manager.groups.Length+"\nOcclusion triangles="+Triangles(depth)+" lower meshes="+overrides+"\n"+text+"\nVerification and save pending.");Audit();
    }
    static void RebakeWithoutDynamicDoors(GasStationPlanarGlass manager)
    {
        var probes=manager.groups.SelectMany(g=>new[]{g.bakedPositive,g.bakedNegative}).Where(p=>p).Distinct().ToArray();
        var states=new Dictionary<Renderer,bool>();var flags=new Dictionary<GameObject,StaticEditorFlags>();float global=Shader.GetGlobalFloat("_GasGlassRenderingReflection");
        try
        {
            foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(r.gameObject.scene==manager.gameObject.scene)
            {
                if(r.GetComponentInParent<ReflectionActor>()||GlassSurfaceVariation.UsesGlass(r)||r.GetComponentInParent<Camera>()||r is ParticleSystemRenderer||r is TrailRenderer){states[r]=r.enabled;r.enabled=false;}
                else if(r.enabled&&r.gameObject.activeInHierarchy&&!flags.ContainsKey(r.gameObject)){flags[r.gameObject]=GameObjectUtility.GetStaticEditorFlags(r.gameObject);GameObjectUtility.SetStaticEditorFlags(r.gameObject,flags[r.gameObject]|StaticEditorFlags.ReflectionProbeStatic);}
            }
            Shader.SetGlobalFloat("_GasGlassRenderingReflection",1);
            foreach(var probe in probes)
            {
                var texture=probe.customBakedTexture;string path=AssetDatabase.GetAssetPath(texture);probe.mode=ReflectionProbeMode.Baked;probe.enabled=true;
                if(!Lightmapping.BakeReflectionProbe(probe,path))throw new InvalidOperationException("Bake failed: "+probe.name);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);probe.mode=ReflectionProbeMode.Custom;probe.enabled=false;EditorUtility.SetDirty(probe);
            }
        }
        finally{foreach(var pair in states)if(pair.Key)pair.Key.enabled=pair.Value;foreach(var pair in flags)if(pair.Key)GameObjectUtility.SetStaticEditorFlags(pair.Key,pair.Value);foreach(var p in probes)if(p){p.mode=ReflectionProbeMode.Custom;p.enabled=false;}Shader.SetGlobalFloat("_GasGlassRenderingReflection",global);}
    }
}



