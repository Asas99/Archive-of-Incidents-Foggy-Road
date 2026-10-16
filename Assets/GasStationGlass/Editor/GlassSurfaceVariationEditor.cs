using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GlassSurfaceVariationEditor
{
    const string Root="Assets/GasStationGlass/Textures/";
    static bool scheduled,processing;
    static GlassSurfaceVariationEditor()
    {
        EditorApplication.hierarchyChanged+=Schedule;
        EditorApplication.projectChanged+=Schedule;
        Undo.undoRedoPerformed+=Schedule;
        EditorApplication.playModeStateChanged+=state=> { if(state==PlayModeStateChange.EnteredEditMode) Schedule(); };
        ObjectChangeEvents.changesPublished+=OnObjectChanges;
        EditorApplication.update+=Requests;
        Schedule();
    }
    static void OnObjectChanges(ref ObjectChangeEventStream stream) { Schedule(); }
    static void Schedule()
    {
        if(scheduled || processing) return;
        scheduled=true; EditorApplication.delayCall+=Refresh;
    }
    static void Requests()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(!File.Exists("Library/GlassVariationInstall.request")) return;
        File.Delete("Library/GlassVariationInstall.request");
        try
        {
            PackMasks(); Refresh();
            foreach(var manager in UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None))
                if(manager.gameObject.scene.path=="Assets/game prototype scene.unity") EditorSceneManager.SaveScene(manager.gameObject.scene);
            File.WriteAllText("Library/GlassVariationInstallResult.txt","PASS masks packed, glass variations assigned and prototype scene saved.");
        }
        catch(Exception e) { File.WriteAllText("Library/GlassVariationInstallResult.txt",e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Refresh Automatic Surface Variations")]
    public static void Refresh()
    {
        scheduled=false;
        if(processing || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        processing=true;
        try
        {
            foreach(var existing in UnityEngine.Object.FindObjectsByType<GlassSurfaceVariation>(FindObjectsInactive.Include,FindObjectsSortMode.None)) existing.EditorRefresh();
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!renderer.gameObject.scene.IsValid() || !GlassSurfaceVariation.UsesGlass(renderer)) continue;
                var variation=renderer.GetComponent<GlassSurfaceVariation>();
                if(!variation) variation=Undo.AddComponent<GlassSurfaceVariation>(renderer.gameObject);
                variation.EditorRefresh();
            }
        }
        finally { processing=false; }
    }
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Pack Dust and Wear Masks")]
    public static void PackMasks()
    {
        var dust=LoadSource("DustSource.png"); var wear=LoadSource("WearSource.png");
        if(dust.width!=wear.width || dust.height!=wear.height) throw new InvalidOperationException("Dust and wear sources must have matching dimensions.");
        var a=dust.GetPixels32(); var b=wear.GetPixels32(); var packed=new Color32[a.Length];
        for(int i=0;i<packed.Length;i++) packed[i]=new Color32(a[i].r,b[i].r,0,255);
        var texture=new Texture2D(dust.width,dust.height,TextureFormat.RGBA32,false,true);
        texture.SetPixels32(packed); texture.Apply();
        File.WriteAllBytes(Root+"GlassSurfaceMasks.png",texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(Root+"GlassSurfaceMasks.png",ImportAssetOptions.ForceSynchronousImport);
        foreach(string name in new[]{"DustSource.png","WearSource.png","GlassSurfaceMasks.png"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+name);
            importer.isReadable=false; importer.sRGBTexture=false; importer.mipmapEnabled=true;
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Clamp; importer.filterMode=FilterMode.Trilinear; importer.anisoLevel=2;
            if(name=="GlassSurfaceMasks.png")
            {
                importer.alphaSource=TextureImporterAlphaSource.None;
                var pc=importer.GetPlatformTextureSettings("Standalone"); pc.overridden=true; pc.maxTextureSize=2048; pc.format=TextureImporterFormat.BC7; pc.compressionQuality=100; importer.SetPlatformTextureSettings(pc);
            }
            importer.SaveAndReimport();
        }
        var masks=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"GlassSurfaceMasks.png");
        foreach(string path in new[]{"Assets/GasStationGlass/Materials/Station Dusty Glass.mat","Assets/Kullanıcı içeriği/materyaller/glass mat.mat"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material || material.shader.name!=GlassSurfaceVariation.ShaderName) continue;
            material.SetTexture("_PackedMasks",masks); material.SetFloat("_UsePackedMasks",1);
            // Repacking sources must preserve the user's visual tuning.
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
    }
    static Texture2D LoadSource(string name)
    {
        var path=Root+name; AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(!importer) throw new FileNotFoundException("Surface mask source missing",path);
        importer.isReadable=true; importer.sRGBTexture=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
