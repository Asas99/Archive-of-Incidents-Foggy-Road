using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Upgrade existing scenes without rebaking or changing their materials/lighting.
[InitializeOnLoad]
public static class GlassProbeIsolationRepair
{
    static GlassProbeIsolationRepair()
    {
        EditorApplication.delayCall+=Repair;
        EditorSceneManager.sceneOpened+=(scene,mode)=>EditorApplication.delayCall+=Repair;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall+=Repair;
        };
    }
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Isolate Glass Probes From Environment")]
    public static void Repair()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall+=Repair; return; }
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        int changed=0;
        foreach(var manager in Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var scene=manager.gameObject.scene;
            if(scene.path!="Assets/game prototype scene.unity") continue;
            bool needsRepair=false;
            foreach(var probe in manager.GetComponentsInChildren<ReflectionProbe>(true))
                needsRepair|=(probe.enabled || EditorUtility.IsDirty(probe)) && probe.mode==ReflectionProbeMode.Custom && probe.customBakedTexture;
            if(!needsRepair) continue;
            Directory.CreateDirectory("Assets/GasStationGlass/Backups");
            string backup="Assets/GasStationGlass/Backups/BeforeProbeIsolation_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
            if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new IOException("Glass probe isolation backup failed.");
            foreach(var probe in manager.GetComponentsInChildren<ReflectionProbe>(true))
            {
                if(!probe.enabled || probe.mode!=ReflectionProbeMode.Custom || !probe.customBakedTexture) continue;
                Undo.RecordObject(probe,"Isolate glass reflection probe");
                probe.enabled=false; EditorUtility.SetDirty(probe); changed++;
            }
            manager.RefreshBakedBindings(); EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Glass probe isolation could not be saved.");
        }
        File.WriteAllText("Library/GlassProbeIsolationRepair.txt","Glass-only probe repair complete. Disabled automatic environment assignment on "+changed+" probes. Materials and lighting unchanged.\n");
    }
}
