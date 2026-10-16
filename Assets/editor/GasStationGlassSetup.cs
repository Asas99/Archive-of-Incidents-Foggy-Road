using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class GasStationGlassSetup
{
    const string Root="Assets/GasStationGlass";
    static GasStationGlassSetup()
    {
        EditorApplication.update += ProcessRequests;
    }
    static double nextRequestCheck;
    static void ProcessRequests()
    {
        if(EditorApplication.timeSinceStartup<nextRequestCheck) return;
        nextRequestCheck=EditorApplication.timeSinceStartup+0.5;
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) return;
        if(EditorApplication.isPlaying)
        {
            if(File.Exists("Library/GasStationGlassRuntimeCapture.request"))
            {
                File.Delete("Library/GasStationGlassRuntimeCapture.request");
                try { CaptureRuntime(); }
                catch(Exception e) { File.WriteAllText("Library/GasStationGlassRuntime.txt",e.ToString()); Debug.LogException(e); }
            }
            return;
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(File.Exists("Library/GasStationGlassBind.request"))
        {
            File.Delete("Library/GasStationGlassBind.request");
            var manager=UnityEngine.Object.FindFirstObjectByType<GasStationPlanarGlass>();
            if(manager)
            {
                Undo.RecordObject(manager,"Bind real gameplay camera to glass");
                manager.playerCamera=manager.ResolvePlayerCamera();
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                EditorSceneManager.SaveScene(manager.gameObject.scene);
                File.WriteAllText("Library/GasStationGlassCameraBinding.txt",manager.playerCamera ? "Bound "+manager.playerCamera.name+" tag="+manager.playerCamera.tag : "ERROR: no gameplay camera found");
            }
        }
        if(File.Exists("Library/GasStationGlassInstall.request"))
        {
            File.Delete("Library/GasStationGlassInstall.request");
            try
            {
                InspectActive();
                var candidates=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
                    .Where(r=>r.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene() && r.sharedMaterials.Any(m=>m && AssetDatabase.GetAssetPath(m)=="Assets/Kullanıcı içeriği/materyaller/glass mat.mat") && IsPlanar(r)).ToArray();
                if(candidates.Length>0) { Install(candidates); BakeLocalProbes(); }
                else
                {
                    var installed=UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None).FirstOrDefault(m=>m.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    File.WriteAllText("Library/GasStationGlassInstallResult.txt",installed ? "Already installed: "+installed.groups.Sum(g=>g.windows.Length)+" panes. No duplicate controller created." : "No windows using glass mat.mat found. Select window planes and use the installation menu. Nothing in the scene was replaced.");
                }
            }
            catch(Exception exception) { File.WriteAllText("Library/GasStationGlassInstallResult.txt",exception.ToString()); Debug.LogException(exception); }
        }
        if(File.Exists("Library/GasStationGlassTest.request") || File.Exists("Library/GasStationGlassSave.request"))
        {
            File.Delete("Library/GasStationGlassTest.request");
            try { TestActiveWindows(); }
            catch(Exception exception)
            {
                File.Delete("Library/GasStationGlassSave.request");
                File.WriteAllText("Library/GasStationGlassSceneTest.txt",exception.ToString()); Debug.LogException(exception);
            }
        }
    }

    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/game prototype scene.unity");
        InspectActive();
    }

    [MenuItem("Tools/Foggy Road/Gas Station Glass/Inspect Active Windows")]
    public static void InspectActive()
    {
        var report = new StringBuilder();
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh) continue;
            string materials = string.Join(" | ", renderer.sharedMaterials.Select(m => m ? AssetDatabase.GetAssetPath(m) + " [" + m.shader.name + "]" : "null"));
            if (filter.sharedMesh.vertexCount > 32 && !materials.ToLowerInvariant().Contains("glass") && !materials.ToLowerInvariant().Contains("cam")) continue;
            string hierarchy = renderer.name;
            for (var t = renderer.transform.parent; t; t = t.parent) hierarchy = t.name + "/" + hierarchy;
            report.AppendLine(hierarchy + "\n id=" + GlobalObjectId.GetGlobalObjectIdSlow(renderer) + " mesh=" + filter.sharedMesh.name + " vertices=" + filter.sharedMesh.vertexCount + " bounds=" + renderer.bounds + " local=" + filter.sharedMesh.bounds + "\n materials=" + materials);
        }
        File.WriteAllText("Library/GasStationGlassInspection.txt", report.ToString());
    }

    [MenuItem("Tools/Foggy Road/Gas Station Glass/Install on Selected Windows")]
    public static void InstallSelected()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before installing glass.");
        var windows=Selection.gameObjects.SelectMany(o=>o.GetComponentsInChildren<MeshRenderer>(true)).Distinct().ToArray();
        if(windows.Length==0) throw new InvalidOperationException("Select the glass planes in the hierarchy first.");
        if(windows.Any(r=>!IsPlanar(r))) throw new InvalidOperationException("Selection contains non-planar geometry. Select only the glass panes, excluding frames and walls.");
        Install(windows);
    }

    static bool IsPlanar(MeshRenderer renderer)
    {
        var filter=renderer.GetComponent<MeshFilter>();
        if(!filter || !filter.sharedMesh || filter.sharedMesh.subMeshCount!=1 || !filter.sharedMesh.isReadable) return false;
        var vertices=filter.sharedMesh.vertices;
        var normals=filter.sharedMesh.normals;
        if(vertices.Length<3 || normals.Length!=vertices.Length || filter.sharedMesh.uv.Length!=vertices.Length) return false;
        Vector3 normal=normals[0].normalized;
        if(normal.sqrMagnitude<0.99f) return false;
        return vertices.All(v=>Mathf.Abs(Vector3.Dot(v-vertices[0],normal))<0.002f);
    }

    public static void Install(MeshRenderer[] windows)
    {
        if(windows.Length==0 || windows.Any(w=>!w || w.gameObject.scene!=windows[0].gameObject.scene)) throw new InvalidOperationException("Select glass panes from one scene at a time.");
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(!pipeline) pipeline=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if(!pipeline) throw new InvalidOperationException("An active Universal Render Pipeline asset is required.");
        Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Settings");
        AssetDatabase.Refresh();
        var shader=Shader.Find("FoggyRoad/Gas Station/Dusty Window");
        if(!shader || !shader.isSupported) throw new InvalidOperationException("The window shader is not available or is not supported.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Station Dusty Glass.mat");
        if(!material) { material=new Material(shader); AssetDatabase.CreateAsset(material,Root+"/Materials/Station Dusty Glass.mat"); }
        int rendererIndex=ConfigureRenderer(pipeline);
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install Gas Station Glass");
        var manager=UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None).FirstOrDefault(m=>m.gameObject.scene==windows[0].gameObject.scene);
        if(!manager)
        {
            var go=new GameObject("Gas Station Dynamic Glass"); Undo.RegisterCreatedObjectUndo(go,"Create glass controller");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,windows[0].gameObject.scene);
            manager=Undo.AddComponent<GasStationPlanarGlass>(go);
        }
        Undo.RecordObject(manager,"Configure reflection groups");
        var all=manager.groups.SelectMany(g=>g.windows).OfType<MeshRenderer>().Concat(windows).Distinct().Where(r=>r).ToArray();
        var groups=new System.Collections.Generic.List<GasStationPlanarGlass.WindowGroup>();
        foreach(var window in all)
        {
            var mesh=window.GetComponent<MeshFilter>().sharedMesh;
            var point=window.transform.TransformPoint(mesh.bounds.center);
            var normal=window.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(mesh.normals[0]).normalized;
            // Moving panes receive their own group so a door cannot invalidate a fixed facade.
            bool moving=window.GetComponentsInParent<Animator>().Length>0 || window.GetComponentsInParent<Rigidbody>().Length>0 || window.name.ToLowerInvariant().Contains("door") || window.name.ToLowerInvariant().Contains("kapı");
            var group=moving ? null : groups.FirstOrDefault(g=>Mathf.Abs(Vector3.Dot(g.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(g.localNormal).normalized,normal))>0.9999f && Mathf.Abs(Vector3.Dot(point-g.plane.TransformPoint(g.localPoint),normal))<0.01f && !g.name.StartsWith("Moving"));
            if(group==null)
            {
                group=new GasStationPlanarGlass.WindowGroup { name=(moving?"Moving ":"Facade ")+groups.Count,plane=window.transform,localPoint=mesh.bounds.center,localNormal=mesh.normals[0],windows=Array.Empty<Renderer>() };
                groups.Add(group);
            }
            group.windows=group.windows.Concat(new Renderer[]{window}).ToArray();
            Undo.RecordObject(window,"Assign glass material");
            window.sharedMaterial=material; window.shadowCastingMode=ShadowCastingMode.Off;
            window.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
            PrefabUtility.RecordPrefabInstancePropertyModifications(window);
        }
        manager.enabled=false;
        manager.groups=groups.ToArray(); manager.configuredPipeline=pipeline; manager.reflectionRendererIndex=rendererIndex;
        manager.reflectionLayers=~LayerMask.GetMask("UI","TransparentFX");
        manager.playerCamera=manager.ResolvePlayerCamera(); manager.enabled=true;
        EditorUtility.SetDirty(manager); EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Undo.CollapseUndoOperations(undo);
        AssetDatabase.SaveAssets();
        // Leave the existing open scene's unsaved work under the user's control.
        File.WriteAllText("Library/GasStationGlassInstallResult.txt","Installed "+all.Length+" panes in "+groups.Count+" groups. Scene marked dirty; save normally. Renderer index: "+rendererIndex);
        Selection.activeGameObject=manager.gameObject;
        Debug.Log("Gas station dynamic glass installed on "+all.Length+" windows. Save the scene normally.",manager);
    }

    public static int ConfigureRenderer(UniversalRenderPipelineAsset pipeline)
    {
        string path=Root+"/Settings/Glass Reflection Renderer.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        var data=new SerializedObject(pipeline);
        var list=data.FindProperty("m_RendererDataList");
        if(!renderer)
        {
            var source=list.GetArrayElementAtIndex(data.FindProperty("m_DefaultRendererIndex").intValue).objectReferenceValue as UniversalRendererData;
            renderer=source ? UnityEngine.Object.Instantiate(source) : ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name="Glass Reflection Renderer";
            renderer.rendererFeatures.Clear();
            var settings=new SerializedObject(renderer);
            settings.FindProperty("m_RenderingMode").intValue=2;
            settings.FindProperty("m_RendererFeatureMap").ClearArray();
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(renderer,path);
        }
        var existingSettings=new SerializedObject(renderer);
        if(existingSettings.FindProperty("m_RenderingMode").intValue!=2)
        {
            existingSettings.FindProperty("m_RenderingMode").intValue=2;
            existingSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }
        for(int i=0;i<list.arraySize;i++) if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer) return i;
        Undo.RecordObject(pipeline,"Register glass reflection renderer");
        int index=list.arraySize; list.arraySize++;
        list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;
        data.ApplyModifiedProperties(); EditorUtility.SetDirty(pipeline);
        return index;
    }

    [MenuItem("Tools/Foggy Road/Gas Station Glass/Bake Local Reflection Probes")]
    public static void BakeLocalProbes()
    {
        var manager=Selection.activeGameObject ? Selection.activeGameObject.GetComponent<GasStationPlanarGlass>() : null;
        if(!manager) throw new InvalidOperationException("Select Gas Station Dynamic Glass first.");
        Directory.CreateDirectory(Root+"/Reflections"); AssetDatabase.Refresh();
        for(int i=0;i<manager.groups.Length;i++)
        {
            var group=manager.groups[i]; if(!group.plane) continue;
            Bounds bounds=group.windows[0].bounds; foreach(var window in group.windows) bounds.Encapsulate(window.bounds);
            Vector3 normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
            for(int side=-1;side<=1;side+=2)
            {
                string name="Glass Probe "+i+" "+side;
                var child=manager.transform.Find(name);
                ReflectionProbe probe;
                if(child) probe=child.GetComponent<ReflectionProbe>();
                else { var go=new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Create local glass probe"); go.transform.SetParent(manager.transform); probe=go.AddComponent<ReflectionProbe>(); }
                Undo.RecordObject(probe,"Configure local glass probe");
                probe.transform.position=bounds.center+normal*side*2;
                probe.mode=ReflectionProbeMode.Baked; probe.resolution=256; probe.hdr=true; probe.boxProjection=true;
                probe.size=bounds.size+Vector3.one*2+new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z))*8;
                probe.center=normal*side*2.75f; probe.blendDistance=0.5f; probe.importance=2;
                probe.cullingMask=manager.reflectionLayers;
                float oldReflection=Shader.GetGlobalFloat("_GasGlassRenderingReflection");
                try
                {
                    Shader.SetGlobalFloat("_GasGlassRenderingReflection",1);
                    Lightmapping.BakeReflectionProbe(probe,Root+"/Reflections/Window_"+i+"_"+side+".exr");
                }
                finally { Shader.SetGlobalFloat("_GasGlassRenderingReflection",oldReflection); }
            }
        }
        AssetDatabase.Refresh(); EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }

    [MenuItem("Tools/Foggy Road/Gas Station Glass/Test Installed Windows From Both Sides")]
    public static void TestActiveWindows()
    {
        var manager=UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None)
            .FirstOrDefault(m=>m.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene() && m.groups.Length>0);
        if(!manager) throw new InvalidOperationException("No installed window controller in the active scene.");
        var report=new StringBuilder();
        var go=new GameObject("Glass Verification Camera") { hideFlags=HideFlags.HideAndDontSave };
        var camera=go.AddComponent<Camera>(); camera.enabled=false;
        camera.clearFlags=CameraClearFlags.Skybox; camera.backgroundColor=new Color(0.08f,0.12f,0.18f);
        camera.nearClipPlane=0.05f; camera.farClipPlane=500; camera.fieldOfView=55; camera.aspect=16f/9;
        var data=camera.GetUniversalAdditionalCameraData(); data.SetRenderer(manager.reflectionRendererIndex);
        data.renderPostProcessing=false; data.renderShadows=true; data.requiresColorTexture=false; data.requiresDepthTexture=false;
        var target=new RenderTexture(960,540,24,RenderTextureFormat.ARGBHalf); target.Create(); camera.targetTexture=target;
        var block=new MaterialPropertyBlock(); var originalBlock=new MaterialPropertyBlock();
        Renderer currentWindow=null; bool originalEnabled=true;
        try
        {
            for(int i=0;i<manager.groups.Length;i++)
            {
                var group=manager.groups[i]; currentWindow=group.windows.FirstOrDefault(r=>r && r.gameObject.activeInHierarchy);
                if(!currentWindow) continue;
                originalEnabled=currentWindow.enabled; currentWindow.GetPropertyBlock(originalBlock);
                if(!currentWindow.sharedMaterial || currentWindow.sharedMaterial.shader.name!="FoggyRoad/Gas Station/Dusty Window") throw new InvalidOperationException("A window still uses the old shader.");
                report.AppendLine("MATERIAL "+AssetDatabase.GetAssetPath(currentWindow.sharedMaterial)+" dust="+currentWindow.sharedMaterial.GetFloat("_DustAmount")+" wear="+currentWindow.sharedMaterial.GetFloat("_WearAmount")+" blur="+currentWindow.sharedMaterial.GetFloat("_TransmissionBlur")+" packed="+currentWindow.sharedMaterial.GetFloat("_UsePackedMasks"));
                var point=group.plane.TransformPoint(group.localPoint);
                var normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
                for(int side=-1;side<=1;side+=2)
                {
                    camera.transform.position=point+normal*side*6;
                    camera.transform.rotation=Quaternion.LookRotation(point-camera.transform.position,Vector3.up);
                    manager.RenderForCamera(camera);
                    currentWindow.GetPropertyBlock(block);
                    if(block.GetFloat("_PlanarWeight")<0.95f) throw new InvalidOperationException("No dynamic reflection for group "+i+", side "+side);
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                    var image=ReadImage(target);
                    File.WriteAllBytes("Library/GasStationGlass_"+i+"_"+(side<0?"Outside":"Inside")+".png",image.EncodeToPNG());
                    var visible=image.GetPixels(); UnityEngine.Object.DestroyImmediate(image);
                    currentWindow.enabled=false;
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                    var background=ReadImage(target); var behind=background.GetPixels(); UnityEngine.Object.DestroyImmediate(background);
                    currentWindow.enabled=originalEnabled;
                    int changed=0;
                    for(int p=0;p<visible.Length;p++) if(Mathf.Abs(visible[p].r-behind[p].r)+Mathf.Abs(visible[p].g-behind[p].g)+Mathf.Abs(visible[p].b-behind[p].b)>0.01f) changed++;
                    if(changed<100) throw new InvalidOperationException("Glass makes no visible contribution for group "+i+", side "+side);
                    report.AppendLine("PASS group="+i+" side="+side+" changedPixels="+changed+" shader="+currentWindow.sharedMaterial.shader.name);
                }
                currentWindow.SetPropertyBlock(originalBlock); currentWindow=null;
            }
            var errors=ShaderUtil.GetShaderMessages(Shader.Find("FoggyRoad/Gas Station/Dusty Window")).Where(m=>m.severity.ToString()=="Error").ToArray();
            if(errors.Length>0) throw new InvalidOperationException(string.Join("; ",errors.Select(e=>e.message)));
            report.AppendLine("PASS shader compilation; verified in current scene with actual geometry and lighting.");
            if(File.Exists("Library/GasStationGlassSave.request"))
            {
                if(manager.gameObject.scene.path!="Assets/game prototype scene.unity") throw new InvalidOperationException("Only the requested prototype scene can be saved by this verification request.");
                if(!EditorSceneManager.SaveScene(manager.gameObject.scene)) throw new InvalidOperationException("The verified scene could not be saved.");
                File.Delete("Library/GasStationGlassSave.request");
                report.AppendLine("PASS verified prototype scene saved.");
            }
            File.WriteAllText("Library/GasStationGlassSceneTest.txt",report.ToString());
            Debug.Log(report.ToString(),manager);
        }
        finally
        {
            if(currentWindow) { currentWindow.enabled=originalEnabled; currentWindow.SetPropertyBlock(originalBlock); }
            camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go);
            manager.SendMessage("LateUpdate");
        }
    }
    static Texture2D ReadImage(RenderTexture target)
    {
        var old=RenderTexture.active;
        try
        {
            RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); return image;
        }
        finally { RenderTexture.active=old; }
    }

    public static void CaptureRuntime()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<GasStationPlanarGlass>();
        if(!manager) throw new InvalidOperationException("Glass controller missing.");
        var camera=manager.ResolvePlayerCamera();
        var report=new StringBuilder("Playing="+Application.isPlaying+"\n");
        foreach(var c in Camera.allCameras) report.AppendLine("CAMERA "+c.name+" tag="+c.tag+" type="+c.cameraType+" target="+c.targetTexture+" position="+c.transform.position);
        if(!camera) throw new InvalidOperationException("No active gameplay camera resolved.");
        manager.RenderForCamera(camera);
        report.AppendLine("SOURCE "+camera.name+" tag="+camera.tag+" activeGroups="+manager.ActiveGroups);
        var block=new MaterialPropertyBlock();
        for(int i=0;i<manager.groups.Length;i++)
        {
            var group=manager.groups[i]; var window=group.windows[0]; window.GetPropertyBlock(block);
            var texture=block.GetTexture("_PlanarTexture") as RenderTexture;
            report.AppendLine("GROUP "+i+" weight="+block.GetFloat("_PlanarWeight")+" texture="+texture+" bounds="+window.bounds);
            if(texture)
            {
                var image=ReadImage(texture);
                File.WriteAllBytes("Library/GasStationGlassRuntimeReflection_"+i+".png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
            }
        }
        File.WriteAllText("Library/GasStationGlassRuntime.txt",report.ToString());
    }
}
