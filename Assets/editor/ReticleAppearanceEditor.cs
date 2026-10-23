using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ReticleAppearance))]
public sealed class ReticleAppearanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var appearance = (ReticleAppearance)target;
        var imageData = new SerializedObject(appearance.GetComponent<RawImage>());
        var rectData = new SerializedObject(appearance.GetComponent<RectTransform>());
        imageData.Update();
        rectData.Update();
        SerializedProperty colorProperty = imageData.FindProperty("m_Color");
        SerializedProperty sizeProperty = rectData.FindProperty("m_SizeDelta");
        Color color = colorProperty.colorValue;

        EditorGUI.BeginChangeCheck();
        color = EditorGUILayout.ColorField("Renk", color);
        color.a = EditorGUILayout.Slider(new GUIContent("Opaklık", "0: görünmez, 1: tamamen görünür."), color.a, 0f, 1f);
        float diameter = EditorGUILayout.Slider(new GUIContent("Boyut (piksel)", "Noktanın ekrandaki çapı."),
            sizeProperty.vector2Value.x, 0.5f, 20f);
        if (EditorGUI.EndChangeCheck())
        {
            colorProperty.colorValue = color;
            sizeProperty.vector2Value = Vector2.one * diameter;
            imageData.ApplyModifiedProperties();
            rectData.ApplyModifiedProperties();
        }
    }
}
