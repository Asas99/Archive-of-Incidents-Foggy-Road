using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class GlassSideSetup
{
    static GlassSideSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string request="Library/GlassSide.install";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        File.Delete(request);
        try
        {
            var manager=UnityEngine.Object.FindFirstObjectByType<GasStationPlanarGlass>();
            if(!manager||manager.gameObject.scene.path!="Assets/game prototype scene.unity")throw new Exception("Expected glass scene not open");
            int front=0,side=0;string report="";
            // Validate every binding before making any changes. Never infer facade from a group index.
            foreach(var g in manager.groups)
            {
                bool? isFront=null;
                foreach(var pane in g.windows)
                {
                    var binding=pane?pane.GetComponent<GlassReflectionBinding>():null;
                    if(!binding||!binding.Verified)throw new Exception("Unverified pane in "+g.name);
                    bool f=binding.positiveRegion=="Dis On Cephe"||binding.negativeRegion=="Dis On Cephe";
                    if(isFront.HasValue&&isFront.Value!=f)throw new Exception("Mixed front/side group "+g.name);
                    isFront=f;report+=pane.name+": "+(f?"Front: unchanged hybrid":"Side: 2s arrival burst + .4s fade, otherwise baked")+"\n";
                    if(f)front++;else side++;
                }
            }
            if(front!=7||side!=3)throw new Exception("Unexpected pane counts "+front+" / "+side);
            string backup=File.ReadAllText("Library/GlassSideBackup.path").Trim();
            if(!EditorSceneManager.SaveScene(manager.gameObject.scene,backup+"/OpenSceneBefore.unity",true))throw new Exception("Backup failed");
            Undo.RecordObject(manager,"Side glass arrival bursts");
            foreach(var g in manager.groups)
            {
                var b=g.windows[0].GetComponent<GlassReflectionBinding>();
                g.arrivalBurstOnly=b.positiveRegion!="Dis On Cephe"&&b.negativeRegion!="Dis On Cephe";g.arrivalBurstSeconds=2;
            }
            manager.InvalidateReflections();EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            if(!EditorSceneManager.SaveScene(manager.gameObject.scene))throw new Exception("Save failed");
            File.WriteAllText(backup+"/Installed.txt",report);Debug.Log("Side glass burst profile saved.");
        }
        catch(Exception e){File.WriteAllText("Recovery/GlassSideInstallError.txt",e.ToString());Debug.LogException(e);}
    }
}
