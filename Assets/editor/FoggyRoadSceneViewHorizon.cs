using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Keeps editor fly navigation level without touching any scene camera.</summary>
[InitializeOnLoad]
public static class FoggyRoadSceneViewHorizon
{
    const string Preference="FoggyRoad.SceneView.LockHorizon";
    const string Menu="Tools/Foggy Road/Scene View/Keep Horizon Level";
    static bool reported;
    public static bool Enabled => EditorPrefs.GetBool(Preference,true);
    static FoggyRoadSceneViewHorizon()
    {
        EditorApplication.update+=CorrectOpenViews;
        SceneView.duringSceneGui+=OnSceneGUI;
        EditorApplication.delayCall+=CorrectOpenViews;
    }
    [MenuItem(Menu)]
    static void Toggle()
    {
        EditorPrefs.SetBool(Preference,!Enabled);
        CorrectOpenViews();
    }
    [MenuItem(Menu,true)]
    static bool ValidateToggle() { UnityEditor.Menu.SetChecked(Menu,Enabled); return true; }
    [MenuItem("Tools/Foggy Road/Scene View/Level Horizon Now")]
    public static void LevelNow()
    {
        foreach(SceneView view in SceneView.sceneViews) Correct(view);
    }
    static void OnSceneGUI(SceneView view)
    {
        if(Enabled && !EditorApplication.isPlayingOrWillChangePlaymode) Correct(view);
    }
    static void CorrectOpenViews()
    {
        if(!Enabled || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        LevelNow();
    }
    public static Quaternion LevelRotation(Quaternion rotation)
    {
        var forward=rotation*Vector3.forward;
        // At a vertical view the horizon is undefined. Leave top/bottom views alone.
        if(Vector3.Cross(Vector3.up,forward).sqrMagnitude<0.000001f) return rotation;
        return Quaternion.LookRotation(forward,Vector3.up);
    }
    public static bool Correct(SceneView view)
    {
        if(!view || view.in2DMode) return false;
        var previous=view.rotation;
        var level=LevelRotation(previous);
        if(Quaternion.Angle(previous,level)<0.01f) return false;
        // Set current AND target values so the old animated rotation cannot restore the roll.
        view.LookAtDirect(view.pivot,level,view.size);
        view.Repaint();
        if(!reported)
        {
            reported=true;
            File.WriteAllText("Library/SceneViewHorizonLock.txt","PASS Scene View horizon corrected. Previous Euler="+previous.eulerAngles+"; current="+view.rotation.eulerAngles+". Editor-only; scene cameras unchanged.\n");
        }
        return true;
    }
}

