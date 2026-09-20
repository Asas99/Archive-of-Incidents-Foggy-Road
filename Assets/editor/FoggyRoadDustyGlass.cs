using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FoggyRoadDustyGlass
{
    private const string Request = "Library/DustyGlassRequest.txt";
    static FoggyRoadDustyGlass() { EditorApplication.delayCall += RunRequested; }
    private static void RunRequested()
    {
        if (!File.Exists(Request)) return;
        string action = File.ReadAllText(Request).Trim();
        if (action.Length == 0) return;
        File.WriteAllText(Request, "");
        try { Inspect(); }
        catch (Exception e) { File.WriteAllText("Library/DustyGlassError.txt", e.ToString()); Debug.LogException(e); }
    }

    private static bool IsGlass(Material m)
    {
        if (!m) return false;
        string n = m.name.ToLowerInvariant();
        return n == "cam" || n.StartsWith("cam.") || n.Contains("glass");
    }

    [MenuItem("Tools/Foggy Road/Dusty Glass/Inspect Scene Glass")]
    public static void Inspect()
    {
        var s = new StringBuilder();
        s.AppendLine("Playing: " + EditorApplication.isPlaying);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            s.AppendLine("SCENE " + scene.path + " dirty=" + scene.isDirty);
            foreach (var r in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Renderer>(true)))
            {
                if (!r.sharedMaterials.Any(IsGlass)) continue;
                s.AppendLine("RENDERER " + r.name + " id=" + r.GetInstanceID() + " parent=" + r.transform.parent.name);
                s.AppendLine("materials: " + string.Join(" | ", r.sharedMaterials.Select(m => m ? m.name + " => " + AssetDatabase.GetAssetPath(m) + " shader=" + m.shader.name : "null")));
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                var mesh = mf.sharedMesh;
                s.AppendLine("mesh=" + AssetDatabase.GetAssetPath(mesh) + " / " + mesh.name + " vertices=" + mesh.vertexCount + " submeshes=" + mesh.subMeshCount + " bounds=" + mesh.bounds);
                var uv = mesh.uv;
                if (uv.Length > 0) s.AppendLine("uv=" + uv.Min(v => v.x) + "," + uv.Min(v => v.y) + " -> " + uv.Max(v => v.x) + "," + uv.Max(v => v.y));
                s.AppendLine("world=" + r.bounds + " localRotation=" + r.transform.localEulerAngles + " localScale=" + r.transform.localScale);
            }
        }
        foreach (var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
            s.AppendLine("PROBE " + probe.name + " bounds=" + probe.bounds + " mode=" + probe.mode);
        var view = SceneView.lastActiveSceneView;
        if (view) s.AppendLine("VIEW " + view.camera.transform.position + " / " + view.camera.transform.eulerAngles);
        File.WriteAllText("Library/DustyGlassInspection.txt", s.ToString());
        Debug.Log("Dusty glass inspection written to Library/DustyGlassInspection.txt");
    }
}
