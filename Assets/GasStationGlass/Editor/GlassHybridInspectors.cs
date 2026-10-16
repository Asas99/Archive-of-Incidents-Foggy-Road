using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GasStationPlanarGlass))]
public sealed class GlassHybridInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); var manager=(GasStationPlanarGlass)target;
        EditorGUILayout.Space(); EditorGUILayout.LabelField("Hybrid status",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Registered actors",manager.RegisteredActors.ToString());
        EditorGUILayout.LabelField("Live groups",manager.ActiveGroups.ToString());
        EditorGUILayout.LabelField("Planar captures",manager.TotalCaptures.ToString());
        EditorGUILayout.LabelField("Last activity",manager.LastActivityReason);
        if(Application.isPlaying) Repaint();
    }
}
[CustomEditor(typeof(GlassSurfaceVariation)),CanEditMultipleObjects]
public sealed class GlassSurfaceVariationInspector : Editor
{
    public override void OnInspectorGUI()
    {
        foreach(var item in targets)
        {
            var pane=(GlassSurfaceVariation)item;
            EditorGUILayout.LabelField(pane.name,"Variant "+pane.SurfaceVariant);
            EditorGUILayout.SelectableLabel(pane.StableId??"Unassigned",GUILayout.Height(18));
        }
        if(!Application.isPlaying && GUILayout.Button("Regenerate selected dust patterns"))
            foreach(var item in targets) { Undo.RecordObject(item,"Regenerate glass dust"); ((GlassSurfaceVariation)item).RegenerateSurfacePattern(); }
    }
}
