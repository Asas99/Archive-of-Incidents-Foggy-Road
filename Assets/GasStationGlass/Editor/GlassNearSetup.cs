using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GlassNearSetup
{
    static GlassNearSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string request="Library/GlassNear.install";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        File.Delete(request);
        try
        {
            var manager=UnityEngine.Object.FindFirstObjectByType<GasStationPlanarGlass>();
            if(!manager||!manager.UsesAtlas)throw new InvalidOperationException("An installed shared atlas manager is required.");
            var backup=File.ReadAllText("Library/GlassNearBackup.path").Trim().Replace('\\','/');var scene=manager.gameObject.scene;
            if(scene.path!="Assets/game prototype scene.unity")throw new InvalidOperationException("Wrong scene: "+scene.path);
            if(!EditorSceneManager.SaveScene(scene,backup+"/OpenSceneBefore.unity",true))throw new IOException("Scene backup failed");
            File.WriteAllText(backup+"/PreviousQuality.json",EditorJsonUtility.ToJson(manager,true));
            Undo.RecordObject(manager,"Apply near glass performance profile");manager.SavePreviousAtlasQuality();manager.ApplyNearDefaults();
            manager.contactShadowShader=Shader.Find("Hidden/FoggyRoad/Glass Contact Shadow");if(!manager.contactShadowShader)throw new InvalidOperationException("Contact shader missing");
            EditorUtility.SetDirty(manager);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
            File.WriteAllText(backup+"/Installed.txt","PASS Near profile saved. Atlas backend preserved. Dynamic=.25, range8/10, camera8/12, depth12, scale.25 cap480x270, mip1.5, transmissionOff, localLightsOff, actorHz30, contacts.12.\n");
        }
        catch(Exception e){File.WriteAllText("Recovery/GlassNearInstallError.txt",e.ToString());Debug.LogException(e);}
    }
}
