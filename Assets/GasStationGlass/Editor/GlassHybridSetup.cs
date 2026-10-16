using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class GlassHybridSetup
{
    static GlassHybridSetup() { EditorApplication.update+=Requests; }
    static void Requests()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Library/GlassHybridInstall.request")) return;
        File.Delete("Library/GlassHybridInstall.request");
        try { Install(); } catch(Exception e) { File.WriteAllText("Library/GlassHybridInstallResults.txt","FAIL: "+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Install Hybrid on All Scene Windows")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before installing hybrid glass.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/game prototype scene.unity") throw new InvalidOperationException("Select the prototype scene before installing all gas station windows.");
        Directory.CreateDirectory("Assets/GasStationGlass/Backups"); AssetDatabase.Refresh();
        string backup="Assets/GasStationGlass/Backups/BeforeHybrid_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new IOException("Could not back up the current open scene.");
        GlassSurfaceVariantBaker.Bake(); GlassSurfaceVariationEditor.Refresh();
        var manager=UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None).First(m=>m.gameObject.scene==scene);
        var surfaces=UnityEngine.Object.FindObjectsByType<GlassSurfaceVariation>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.gameObject.scene==scene && GlassSurfaceVariation.UsesGlass(p.Surface)).ToArray();
        Undo.RecordObject(manager,"Install balanced hybrid glass");
        bool wasEnabled=manager.enabled; manager.enabled=false;
        manager.groups=Array.Empty<GasStationPlanarGlass.WindowGroup>();
        manager.enabled=wasEnabled;
        foreach(var surface in surfaces) { surface.EditorRefresh(); surface.RegisterReflection(); }
        manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Hybrid;
        manager.previewInSceneView=false; manager.maxActiveGroups=2; manager.liveUpdatesPerSecond=30;
        manager.activityInterval=0.1f; manager.activityHold=1.5f; manager.transitionSeconds=0.4f;
        manager.playerCamera=manager.ResolvePlayerCamera();
        RegisterSceneActors(scene);
        BakeSharedProbes(manager,surfaces);
        EditorUtility.SetDirty(manager); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Hybrid scene could not be saved.");
        var report=new StringBuilder("PASS hybrid installed and open scene saved.\nBackup: "+backup+"\n");
        report.AppendLine("Panes="+surfaces.Length+" groups="+manager.groups.Length+" scene actor components="+UnityEngine.Object.FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(a=>a.gameObject.scene==scene));
        foreach(var surface in surfaces) report.AppendLine(surface.name+" id="+surface.StableId+" variant="+surface.SurfaceVariant+" geometry="+surface.GeometryReady);
        foreach(var probe in manager.GetComponentsInChildren<ReflectionProbe>(true))
            if(probe.name.StartsWith("Hybrid Region ")) report.AppendLine("Glass-only probe="+probe.name+" enabled="+probe.enabled+" mode="+probe.mode+" cube="+AssetDatabase.GetAssetPath(probe.customBakedTexture)+" bounds="+probe.bounds);
        File.WriteAllText("Library/GlassHybridInstallResults.txt",report.ToString()); Debug.Log(report.ToString());
    }
    public static void RegisterSceneActors(UnityEngine.SceneManagement.Scene scene)
    {
        var roots=new HashSet<GameObject>();
        foreach(var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(behaviour && behaviour.gameObject.scene==scene && behaviour.GetType().Name=="CarController") roots.Add(behaviour.gameObject);
        foreach(var character in UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(character.gameObject.scene==scene && character.GetComponentsInChildren<Renderer>(true).Any(r=>!(r is ParticleSystemRenderer) && !r.GetComponentInParent<Camera>())) roots.Add(character.gameObject);
        foreach(var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(animator.gameObject.scene==scene && animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length>0 && !roots.Any(r=>animator.transform.IsChildOf(r.transform))) roots.Add(animator.gameObject);
        foreach(var root in roots)
        {
            if(!root.GetComponent<ReflectionActor>()) Undo.AddComponent<ReflectionActor>(root);
            root.GetComponent<ReflectionActor>().RefreshRenderers();
            EditorUtility.SetDirty(root); PrefabUtility.RecordPrefabInstancePropertyModifications(root.GetComponent<ReflectionActor>());
        }
    }
    public static void BakeSharedProbes(GasStationPlanarGlass manager,GlassSurfaceVariation[] panes)
    {
        if(panes.Length==0) throw new InvalidOperationException("No glass panes to bake.");
        Directory.CreateDirectory("Assets/GasStationGlass/Reflections/Hybrid"); AssetDatabase.Refresh();
        Bounds all=panes[0].Surface.bounds; foreach(var pane in panes) all.Encapsulate(pane.Surface.bounds);
        Vector3 Normal(GlassSurfaceVariation p) => p.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(p.LocalNormal).normalized;
        var dominant=panes.OrderByDescending(p=>panes.Count(q=>Mathf.Abs(Vector3.Dot(Normal(p),Normal(q)))>0.98f)).First();
        Vector3 normal=Normal(dominant),tangent=Vector3.Cross(Vector3.up,normal).normalized;
        var frontage=panes.Where(p=>Mathf.Abs(Vector3.Dot(Normal(p),normal))>0.98f).ToArray();
        Bounds facade=frontage[0].Surface.bounds; foreach(var pane in frontage) facade.Encapsulate(pane.Surface.bounds);
        var positions=new List<Vector3>{facade.center+normal*3,facade.center-normal*3};
        foreach(int end in new[]{-1,1})
        {
            var others=panes.Where(p=>Mathf.Abs(Vector3.Dot(Normal(p),normal))<=0.98f && Vector3.Dot(p.Surface.bounds.center-facade.center,tangent)*end>=0).ToArray();
            if(others.Length==0) continue;
            Vector3 centre=Vector3.zero,outward=Normal(others[0]); foreach(var pane in others) centre+=pane.Surface.bounds.center; centre/=others.Length;
            if(Vector3.Dot(outward,centre-all.center)<0) outward=-outward;
            positions.Add(centre+outward*3);
        }
        var oldMode=manager.reflectionMode; manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Baked;
        var flags=new Dictionary<GameObject,StaticEditorFlags>(); var enabled=new Dictionary<Renderer,bool>();
        var lightEnabled=new Dictionary<Light,bool>();
        float oldReflection=Shader.GetGlobalFloat("_GasGlassRenderingReflection");
        try
        {
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(renderer.gameObject.scene!=manager.gameObject.scene) continue;
                bool exclude=renderer.GetComponentInParent<ReflectionActor>() || renderer.GetComponentInParent<Camera>() || renderer.GetComponent<GlassSurfaceVariation>() || renderer is ParticleSystemRenderer || renderer is TrailRenderer;
                if(exclude) { enabled[renderer]=renderer.enabled; renderer.enabled=false; continue; }
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var go=renderer.gameObject;
                if(!flags.ContainsKey(go)) { flags.Add(go,GameObjectUtility.GetStaticEditorFlags(go)); GameObjectUtility.SetStaticEditorFlags(go,flags[go]|StaticEditorFlags.ReflectionProbeStatic); }
            }
            Shader.SetGlobalFloat("_GasGlassRenderingReflection",1);
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(light.gameObject.scene==manager.gameObject.scene && light.GetComponentInParent<ReflectionActor>()) { lightEnabled[light]=light.enabled; light.enabled=false; }
            foreach(var actor in ReflectionActor.Registered) if(actor && actor.gameObject.scene==manager.gameObject.scene)
                foreach(var light in actor.activityLights) if(light && !lightEnabled.ContainsKey(light)) { lightEnabled[light]=light.enabled; light.enabled=false; }
            for(int region=0;region<positions.Count;region++)
            {
                string name="Hybrid Region "+region;
                var child=manager.transform.Find(name);
                var probe=child ? child.GetComponent<ReflectionProbe>() : new GameObject(name).AddComponent<ReflectionProbe>();
                probe.transform.SetParent(manager.transform); probe.enabled=true;
                Vector3 size=all.size+new Vector3(6,4,6)+new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z))*10;
                probe.transform.position=positions[region]; probe.center=all.center-positions[region]; probe.size=size;
                probe.mode=ReflectionProbeMode.Baked; probe.resolution=256; probe.hdr=true; probe.boxProjection=true;
                probe.importance=3; probe.blendDistance=0.5f; probe.cullingMask=manager.reflectionLayers; probe.farClipPlane=manager.playerCamera ? manager.playerCamera.farClipPlane : 500;
                string path="Assets/GasStationGlass/Reflections/Hybrid/SharedRegion_"+region+".exr";
                if(!Lightmapping.BakeReflectionProbe(probe,path)) throw new InvalidOperationException("Probe capture failed: "+name);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                if(!cube) throw new InvalidOperationException("Baked output is not a cubemap: "+path);
                // Explicit asset reference survives builds; never refresh this probe at runtime.
                probe.mode=ReflectionProbeMode.Custom; probe.customBakedTexture=cube; probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
                probe.enabled=false;
                EditorUtility.SetDirty(probe);
            }
            foreach(var probe in manager.GetComponentsInChildren<ReflectionProbe>(true))
                if(probe.name.StartsWith("Glass Probe ") || probe.name.StartsWith("Hybrid Baked ")) { probe.enabled=false; EditorUtility.SetDirty(probe); }
            manager.RefreshBakedBindings();
        }
        finally
        {
            foreach(var pair in flags) if(pair.Key) GameObjectUtility.SetStaticEditorFlags(pair.Key,pair.Value);
            foreach(var pair in enabled) if(pair.Key) pair.Key.enabled=pair.Value;
            foreach(var pair in lightEnabled) if(pair.Key) pair.Key.enabled=pair.Value;
            Shader.SetGlobalFloat("_GasGlassRenderingReflection",oldReflection); manager.reflectionMode=oldMode;
        }
    }
}
