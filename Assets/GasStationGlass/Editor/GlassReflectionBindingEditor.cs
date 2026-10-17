using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlassReflectionBinding))]
public sealed class GlassReflectionBindingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();var binding=(GlassReflectionBinding)target;
        EditorGUILayout.HelpBox(binding.Verified?"Both reflection sources are saved for this pane.":"Assign the actual room/exterior sources on both sides, then verify. A copied pane requires its own approval.",binding.Verified?MessageType.Info:MessageType.Warning);
        if(GUILayout.Button("Verify assigned sources for this pane"))
        {
            var pane=binding.GetComponent<GlassSurfaceVariation>();
            if(!pane||!binding.positive||!binding.negative||!binding.positive.customBakedTexture||!binding.negative.customBakedTexture){EditorGUILayout.HelpBox("Both baked cubemaps are required.",MessageType.Error);return;}
            var p=pane.transform.TransformPoint(pane.LocalPoint);var n=pane.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(pane.LocalNormal).normalized;
            if(Vector3.Dot(binding.positive.transform.position-p,n)<=0||Vector3.Dot(binding.negative.transform.position-p,n)>=0){Debug.LogWarning("Probe captures are not on the assigned sides. Check the closed/rest pose and region selection.",binding);return;}
            Undo.RecordObject(binding,"Verify pane reflection sources");binding.verifiedIdentity=pane.StableId;EditorUtility.SetDirty(binding);
            foreach(var manager in Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None))if(manager.gameObject.scene==pane.gameObject.scene){manager.UnregisterWindow(pane.Surface);pane.RegisterReflection();manager.RefreshBakedBindings();manager.InvalidateReflections();EditorUtility.SetDirty(manager);}
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);
        }
    }
}
