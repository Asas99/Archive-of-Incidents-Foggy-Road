using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class GlassAtlasInstallRequest
{
    static GlassAtlasInstallRequest(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string request="Library/GlassAtlas.install";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        File.Delete(request);try{GlassLeanSetup.InstallAtlas();}catch(Exception e){File.WriteAllText("Recovery/GlassAtlasInstallError.txt",e.ToString());Debug.LogException(e);}
    }
}

public static partial class GlassLeanSetup
{
    public static void InstallAtlas()
    {
        var manager=Manager();var scene=manager.gameObject.scene;
        if(scene.path!="Assets/game prototype scene.unity")throw new InvalidOperationException("Wrong scene: "+scene.path);
        string backup=File.ReadAllText("Library/GlassAtlasBackup.path").Trim().Replace('\\','/');
        if(!EditorSceneManager.SaveScene(scene,backup+"/OpenSceneBefore.unity",true))throw new IOException("Open scene backup failed");
        var pipeline=(UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        var pipelineSo=new SerializedObject(pipeline);var renderers=pipelineSo.FindProperty("m_RendererDataList");int mainIndex=pipelineSo.FindProperty("m_DefaultRendererIndex").intValue;
        var player=manager.ResolvePlayerCamera();if(player){var cameraSo=new SerializedObject(player.GetUniversalAdditionalCameraData());int configured=cameraSo.FindProperty("m_RendererIndex").intValue;if(configured>=0)mainIndex=configured;}
        var main=renderers.GetArrayElementAtIndex(mainIndex).objectReferenceValue as UniversalRendererData;
        var feature=main.rendererFeatures.OfType<GlassAtlasFeature>().FirstOrDefault();
        if(!feature){feature=ScriptableObject.CreateInstance<GlassAtlasFeature>();feature.name="Glass shared atlas";AssetDatabase.AddObjectToAsset(feature,main);main.rendererFeatures.Add(feature);}
        feature.SetActive(true);EditorUtility.SetDirty(main);EditorUtility.SetDirty(feature);
        string resources=Root+"/Resources";Directory.CreateDirectory(resources);AssetDatabase.Refresh();
        string path=resources+"/GlassVehicleLodCatalog.asset";
        var catalog=AssetDatabase.LoadAssetAtPath<GlassVehicleLodCatalog>(path);if(!catalog){catalog=ScriptableObject.CreateInstance<GlassVehicleLodCatalog>();AssetDatabase.CreateAsset(catalog,path);}
        var entries=new System.Collections.Generic.List<GlassVehicleLodCatalog.Vehicle>();var report=new StringBuilder();
        for(int n=1;n<=17;n++)
        {
            string folder="Assets/NPC_ARABALARI/npc"+n;
            string[] files=Directory.GetFiles(folder,"*.fbx",SearchOption.AllDirectories).Select(f=>f.Replace('\\','/')).ToArray();
            string low=files.Single(f=>f.Contains("lod2/")),high=files.Single(f=>f.Contains("lod1/"));
            var highParts=AtlasParts(AssetDatabase.LoadAssetAtPath<GameObject>(high));var lowParts=AtlasParts(AssetDatabase.LoadAssetAtPath<GameObject>(low));
            if(highParts.Length==0||lowParts.Length==0)throw new InvalidOperationException("No meshes in "+folder);
            entries.Add(new GlassVehicleLodCatalog.Vehicle{name="npc"+n,high=highParts,low=lowParts});
            long highTris=highParts.Sum(p=>Triangles(p.mesh)),lowTris=lowParts.Sum(p=>Triangles(p.mesh));
            report.AppendLine("npc"+n+" | "+high+" | "+low+" | parts="+lowParts.Length+" | triangles="+highTris+" -> "+lowTris);
        }
        catalog.vehicles=entries.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        foreach(var actor in UnityEngine.Object.FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(actor.gameObject.scene==scene)
        {
            actor.RefreshRenderers();
            if(actor.VehicleLodName!=null)report.AppendLine("BOUND "+actor.name+" -> "+actor.VehicleLodName+" parts="+actor.LowDrawParts.Count);
        }
        manager.dynamicBackend=GasStationPlanarGlass.DynamicBackend.SharedAtlas;manager.atlasFeature=feature;
        manager.atlasActorShader=Shader.Find("Hidden/FoggyRoad/Glass Atlas Actor");manager.atlasClearShader=Shader.Find("Hidden/FoggyRoad/Glass Atlas Clear");
        if(!manager.atlasActorShader||!manager.atlasClearShader)throw new InvalidOperationException("Atlas shaders missing");
        manager.resolutionScale=.375f;manager.transmissionQuality=GasStationPlanarGlass.TransmissionQuality.Balanced;manager.previewInSceneView=false;
        foreach(var material in manager.groups.SelectMany(g=>g.windows).Where(r=>r).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader.name==GlassSurfaceVariation.ShaderName).Distinct())
        {
            material.SetFloat("_TransmissionBlur",1.75f);material.SetFloat("_TransmissionScatter",.3f);material.SetFloat("_DustSmoothness",.75f);EditorUtility.SetDirty(material);
        }
        manager.InvalidateReflections();EditorUtility.SetDirty(manager);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(backup+"/Lod2Mappings.txt",report.ToString());File.WriteAllText(backup+"/Installed.txt","Atlas installed; panes="+manager.groups.Sum(g=>g.windows.Length)+" groups="+manager.groups.Length+"; catalogue=17; resolution=.375 cap640x360; blur1.75 scatter.3 dirt smoothness.75; no FPS benchmark.\n");
    }
    static GlassVehicleLodCatalog.Part[] AtlasParts(GameObject root)
    {
        return root.GetComponentsInChildren<MeshRenderer>(true).Select(r=>new GlassVehicleLodCatalog.Part{mesh=r.GetComponent<MeshFilter>()?.sharedMesh,materials=r.sharedMaterials,rootMatrix=root.transform.worldToLocalMatrix*r.localToWorldMatrix,name=r.GetComponent<MeshFilter>()?.sharedMesh?.name??r.name}).Where(p=>p.mesh).ToArray();
    }
}
