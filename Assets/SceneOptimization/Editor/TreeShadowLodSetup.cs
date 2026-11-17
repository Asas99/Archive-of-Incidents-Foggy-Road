using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Keep visual LODs intact. Only LOD1 uses the cheaper LOD2 shadow mesh.
public static class TreeShadowLodSetup
{
    const string ProxyName = "__FoggyRoad_LOD1_ShadowProxy";
    static readonly string[] Paths = {
        "Assets/SceneOptimization/Generated/Prefabs/34776a9356ed54141bf7fed4fded28a0_Tree.prefab",
        "Assets/SceneOptimization/Generated/Prefabs/451d860cead90b64bbbab1639909594c_Tree.prefab"
    };

    [MenuItem("Tools/Foggy Road/Tree Shadow LOD/Apply")]
    public static void Apply() { Debug.Log(Configure(true)); }

    [MenuItem("Tools/Foggy Road/Tree Shadow LOD/Revert")]
    public static void Revert() { Debug.Log(Configure(false)); }

    public static string Configure(bool apply)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before changing tree prefabs.");
        var report = new StringBuilder();
        foreach (string path in Paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var group = root.GetComponent<LODGroup>();
                var levels = group.GetLODs();
                var proxy = root.GetComponentsInChildren<MeshRenderer>(true)
                    .FirstOrDefault(r => r.name == ProxyName);
                if (apply && proxy) { report.AppendLine(path + ": already applied"); continue; }
                if (!apply && !proxy) continue;
                var visuals = levels[1].renderers.Where(r => r && r != proxy).ToArray();
                // Validate the known prefab layout before changing anything.
                if (visuals.Length != 1 || !(visuals[0] is MeshRenderer))
                    throw new InvalidOperationException("Unexpected LOD1 layout: " + path);
                if (apply)
                {
                    var source = levels[2].renderers.Single() as MeshRenderer;
                    var mesh = source ? source.GetComponent<MeshFilter>() : null;
                    if (!mesh || !mesh.sharedMesh || visuals[0].shadowCastingMode != ShadowCastingMode.On)
                        throw new InvalidOperationException("Unexpected shadow source: " + path);
                    var go = new GameObject(ProxyName);
                    go.layer = source.gameObject.layer;
                    go.transform.SetParent(source.transform.parent, false);
                    go.transform.localPosition = source.transform.localPosition;
                    go.transform.localRotation = source.transform.localRotation;
                    go.transform.localScale = source.transform.localScale;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                    proxy = go.AddComponent<MeshRenderer>();
                    EditorUtility.CopySerialized(source, proxy);
                    proxy.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    proxy.receiveShadows = false;
                    visuals[0].shadowCastingMode = ShadowCastingMode.Off;
                    levels[1].renderers = visuals.Concat(new Renderer[] { proxy }).ToArray();
                    report.AppendLine(path + ": LOD1 shadow vertices " +
                        visuals[0].GetComponent<MeshFilter>().sharedMesh.vertexCount + " -> " + mesh.sharedMesh.vertexCount);
                }
                else
                {
                    visuals[0].shadowCastingMode = ShadowCastingMode.On;
                    levels[1].renderers = visuals;
                    UnityEngine.Object.DestroyImmediate(proxy.gameObject);
                    report.AppendLine(path + ": original LOD1 shadows restored");
                }
                group.SetLODs(levels);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return report.ToString();
    }
}
