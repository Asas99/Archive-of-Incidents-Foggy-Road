using System;
using UnityEditor;
using UnityEngine;

public static class TreeLeafAlphaSetup
{
    const string Original = "FoggyRoad/Optimization/Graph/d4fdc026464a42041be7fe785babd90e/Tree";
    const string Optimized = "FoggyRoad/Optimization/Tree Leaves Early Alpha";

    [MenuItem("Tools/Foggy Road/Tree Leaf Alpha/Apply")]
    public static void Apply() { Debug.Log(SetShader(Original, Optimized)); }

    [MenuItem("Tools/Foggy Road/Tree Leaf Alpha/Revert")]
    public static void Revert() { Debug.Log(SetShader(Optimized, Original)); }

    static string SetShader(string from, string to)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before saving tree shaders.");
        var shader = Shader.Find(to);
        if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Tree shader unavailable or has compilation errors: " + to);
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/SceneOptimization/Generated/Materials" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (!m || !m.shader || m.shader.name != from) continue;
            Undo.RecordObject(m, "Change tree leaf alpha shader");
            m.shader = shader;
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssetIfDirty(m);
            changed++;
        }
        return "Updated " + changed + " leaf materials to " + to;
    }
}
