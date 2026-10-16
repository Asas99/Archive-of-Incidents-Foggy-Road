using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class FoggyRoadEditorRecovery
{
    const string Request="Library/EditorRecoverySaveAndExit.request";
    static FoggyRoadEditorRecovery() { EditorApplication.update+=Poll; }
    static void Poll()
    {
        if(!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying=false;
            return;
        }
        bool saveOnly=File.ReadAllText(Request).Trim()=="SAVE_ONLY";
        File.Delete(Request);
        try
        {
            Directory.CreateDirectory("Assets/GasStationGlass/Backups");
            var result=new StringBuilder();
            for(int i=0;i<EditorSceneManager.sceneCount;i++)
            {
                var scene=EditorSceneManager.GetSceneAt(i);
                if(!scene.isLoaded) continue;
                if(string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("An untitled scene must be named before restart.");
                string backup="Assets/GasStationGlass/Backups/BeforeRestart_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+i+".unity";
                if(!EditorSceneManager.SaveScene(scene,backup,true) || !EditorSceneManager.SaveScene(scene))
                    throw new IOException("Scene save failed: "+scene.path);
                result.AppendLine("Saved "+scene.path+"; backup "+backup);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Library/EditorRecoverySaveAndExit.txt","PASS\n"+result);
            if(!saveOnly) EditorApplication.Exit(0);
        }
        catch(Exception e) { File.WriteAllText("Library/EditorRecoverySaveAndExit.txt","FAIL\n"+e); UnityEngine.Debug.LogException(e); }
    }
}

