using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static partial class GlassRoomSetup
{
    public sealed class Region
    {
        public string name;public Vector3 capture;public Bounds bounds;public ReflectionProbe probe;
        public Region(string name,Vector3 capture,Vector3 min,Vector3 max){this.name=name;this.capture=capture;bounds=new Bounds((min+max)*0.5f,max-min);}
    }
    // Surveyed prototype room envelopes in world metres, not an automatic nearest-probe heuristic.
    static Region[] Layout()=>new[]{
        new Region("Salon",new Vector3(450,4.5f,906),new Vector3(440.8f,0.7f,894.5f),new Vector3(462.1f,8.12f,920.7f)),
        new Region("Arka Oda",new Vector3(451,4.5f,889.5f),new Vector3(442.7f,0.7f,884.2f),new Vector3(460.1f,8.12f,894.3f)),
        new Region("Koridor",new Vector3(466.5f,4.5f,905),new Vector3(462.7f,0.7f,888.1f),new Vector3(471.2f,8.05f,922)),
        new Region("Dis On Cephe",new Vector3(438,4.5f,907),new Vector3(410,0.3f,887),new Vector3(442.5f,14,930)),
        new Region("Dis Arka Oda",new Vector3(450.5f,5,881),new Vector3(440,0.3f,861),new Vector3(461,14,885.2f)),
        new Region("Dis Koridor Guney",new Vector3(467.3f,5,885),new Vector3(460,0.3f,866),new Vector3(480,14,889)),
        new Region("Dis Koridor Kuzey",new Vector3(465.7f,5,924.5f),new Vector3(459,0.3f,921),new Vector3(480,14,945))};
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Install Verified Room Reflections")]
    public static void Install()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/game prototype scene.unity")throw new InvalidOperationException("Room layout belongs to the prototype scene only.");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        var panes=Panes();var expected=Enumerable.Range(2,10).Select(i=>"Plane ("+i+")").ToArray();
        if(panes.Length!=10 || !expected.All(n=>panes.Count(p=>p.name==n)==1))throw new InvalidOperationException("Survey changed: expected the ten audited panes Plane (2)..Plane (11). Resurvey before assigning new rooms.");
        var manager=UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);
        Directory.CreateDirectory("Assets/GasStationGlass/Backups");Directory.CreateDirectory("Assets/GasStationGlass/Reflections/Rooms");Directory.CreateDirectory(Output);
        string backup="Assets/GasStationGlass/Backups/BeforeRoomReflections_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Open scene backup failed.");
        var regions=Layout();var flags=new Dictionary<GameObject,StaticEditorFlags>();var hidden=new Dictionary<Renderer,bool>();var lights=new Dictionary<Light,bool>();float global=Shader.GetGlobalFloat("_GasGlassRenderingReflection");
        var oldMode=manager.reflectionMode;manager.reflectionMode=GasStationPlanarGlass.ReflectionMode.Baked;
        try
        {
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(renderer.gameObject.scene!=scene)continue;
                if(GlassSurfaceVariation.UsesGlass(renderer)||renderer.GetComponentInParent<ReflectionActor>()||renderer.GetComponentInParent<Camera>()||renderer is ParticleSystemRenderer||renderer is TrailRenderer){hidden[renderer]=renderer.enabled;renderer.enabled=false;continue;}
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                if(!flags.ContainsKey(renderer.gameObject)){flags[renderer.gameObject]=GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,flags[renderer.gameObject]|StaticEditorFlags.ReflectionProbeStatic);}
            }
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(light.gameObject.scene==scene&&light.GetComponentInParent<ReflectionActor>()){lights[light]=light.enabled;light.enabled=false;}
            Shader.SetGlobalFloat("_GasGlassRenderingReflection",1);
            AssetDatabase.Refresh();
            foreach(var region in regions)
            {
                var child=manager.transform.Find("Room Reflection - "+region.name);var probe=child?child.GetComponent<ReflectionProbe>():new GameObject("Room Reflection - "+region.name).AddComponent<ReflectionProbe>();
                probe.transform.SetParent(manager.transform,true);probe.transform.position=region.capture;probe.center=region.bounds.center-region.capture;probe.size=region.bounds.size;
                probe.enabled=true;probe.mode=ReflectionProbeMode.Baked;probe.resolution=256;probe.hdr=true;probe.boxProjection=true;probe.nearClipPlane=0.05f;probe.farClipPlane=500;probe.cullingMask=manager.reflectionLayers;
                string path="Assets/GasStationGlass/Reflections/Rooms/"+region.name.Replace(' ','_')+".exr";
                if(!Lightmapping.BakeReflectionProbe(probe,path))throw new InvalidOperationException("Bake failed: "+region.name);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                if(!probe.customBakedTexture)throw new IOException("Missing cubemap: "+path);
                probe.mode=ReflectionProbeMode.Custom;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.enabled=false;region.probe=probe;EditorUtility.SetDirty(probe);
            }
            foreach(var pane in panes)
            {
                var binding=pane.GetComponent<GlassReflectionBinding>();if(!binding)binding=Undo.AddComponent<GlassReflectionBinding>(pane.gameObject);else Undo.RecordObject(binding,"Assign verified reflection room");
                int inside=pane.name=="Plane (9)"?1:pane.name=="Plane (10)"||pane.name=="Plane (11)"?2:0;
                int outside=inside==0?3:inside==1?4:pane.name=="Plane (10)"?5:6;
                var point=pane.transform.TransformPoint(pane.LocalPoint);var normal=pane.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(pane.LocalNormal).normalized;
                bool positiveInside=Vector3.Dot(regions[inside].capture-point,normal)>0;
                var pos=regions[positiveInside?inside:outside];var neg=regions[positiveInside?outside:inside];
                if(Vector3.Dot(pos.capture-point,normal)<=0 || Vector3.Dot(neg.capture-point,normal)>=0)throw new InvalidOperationException("Region lies on wrong side: "+pane.name);
                binding.positive=pos.probe;binding.negative=neg.probe;binding.positiveRegion=pos.name;binding.negativeRegion=neg.name;binding.verifiedIdentity=pane.StableId;EditorUtility.SetDirty(binding);
            }
            foreach(var probe in manager.GetComponentsInChildren<ReflectionProbe>(true))probe.enabled=false;
            bool wasEnabled=manager.enabled;manager.enabled=false;manager.groups=Array.Empty<GasStationPlanarGlass.WindowGroup>();manager.enabled=wasEnabled;
            foreach(var pane in panes)pane.RegisterReflection();manager.RefreshBakedBindings();manager.InvalidateReflections();EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Output+"/Installed.txt","Backup="+backup+"\nTen pane bindings installed; seven 256 HDR shared room/exterior cubes. Scene save pending visual verification.\n");
            Survey();
        }
        finally
        {
            foreach(var pair in hidden)if(pair.Key)pair.Key.enabled=pair.Value;
            foreach(var pair in flags)if(pair.Key)GameObjectUtility.SetStaticEditorFlags(pair.Key,pair.Value);
            foreach(var pair in lights)if(pair.Key)pair.Key.enabled=pair.Value;
            foreach(var region in regions)if(region.probe)region.probe.enabled=false;
            Shader.SetGlobalFloat("_GasGlassRenderingReflection",global);manager.reflectionMode=oldMode;
        }
    }
}
