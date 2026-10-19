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
        EditorGUILayout.LabelField("Selected live group",manager.SelectedLiveGroup);
        EditorGUILayout.LabelField("Reflected actor types",manager.LiveActorSummary);
        EditorGUILayout.LabelField("Transmission copy",manager.TransmissionCopyRequested.ToString());
        EditorGUILayout.LabelField("Atlas regions updated",manager.UpdatedAtlasRegions.ToString());
        EditorGUILayout.LabelField("Atlas draw models / triangles",manager.AtlasModels+" / "+manager.AtlasTriangles);
        EditorGUILayout.LabelField("Extra camera render requests",manager.CameraRenderRequests.ToString());
        EditorGUILayout.LabelField("Dynamic models",manager.DynamicModelCount.ToString());
        EditorGUILayout.LabelField("Dynamic capture triangles",manager.DynamicTriangles.ToString());
        EditorGUILayout.LabelField("Last update",manager.LastUpdateReason);
        EditorGUILayout.LabelField("Existing sun shadow usable",manager.MainShadowAvailable.ToString());
        EditorGUILayout.LabelField("Contact shadow draws",manager.ContactShadowDraws.ToString());
        if(!Application.isPlaying&&GUILayout.Button("Restore previous atlas quality")){Undo.RecordObject(manager,"Restore atlas quality");manager.RestorePreviousAtlasQuality();EditorUtility.SetDirty(manager);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);}
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
            var reflection=pane.GetComponent<GlassReflectionBinding>();
            if(!reflection || !reflection.Verified) EditorGUILayout.HelpBox("Reflection regions are unverified. Assign and verify both sides before using this pane.",MessageType.Warning);
            else EditorGUILayout.LabelField("Reflection regions",reflection.positiveRegion+" / "+reflection.negativeRegion);
            EditorGUILayout.SelectableLabel(pane.StableId??"Unassigned",GUILayout.Height(18));
        }
        if(!Application.isPlaying && GUILayout.Button("Regenerate selected dust patterns"))
            foreach(var item in targets) { Undo.RecordObject(item,"Regenerate glass dust"); ((GlassSurfaceVariation)item).RegenerateSurfacePattern(); }
    }
}

