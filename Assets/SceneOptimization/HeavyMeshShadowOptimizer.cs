using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Removes real-time shadow casters only from the three highest-cost station props.
/// It resolves the named LOD roots once, then restores their original modes if disabled.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HeavyMeshShadowOptimizer : MonoBehaviour
{
    [SerializeField] private string[] targetRootNames =
    {
        "marketönüraf LOD",
        "plastikkasalar LOD",
        "otomat LOD"
    };

    private readonly List<MeshRenderer> targets = new List<MeshRenderer>();
    private readonly List<ShadowCastingMode> originalModes = new List<ShadowCastingMode>();
    private bool applied;

    private void Awake()
    {
        ResolveTargets();
        Apply();
    }

    private void OnEnable()
    {
        if (!applied)
        {
            ResolveTargets();
            Apply();
        }
    }

    private void OnDisable()
    {
        Restore();
    }

    public void Apply()
    {
        if (applied) return;

        for (int i = 0; i < targets.Count; i++)
        {
            MeshRenderer renderer = targets[i];
            if (renderer != null)
                renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        applied = true;
    }

    public void Restore()
    {
        if (!applied) return;

        for (int i = 0; i < targets.Count; i++)
        {
            MeshRenderer renderer = targets[i];
            if (renderer != null)
                renderer.shadowCastingMode = originalModes[i];
        }

        applied = false;
    }

    private void ResolveTargets()
    {
        if (targets.Count > 0) return;

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < targetRootNames.Length; i++)
        {
            Transform root = FindNamedTransform(roots, targetRootNames[i]);
            if (root == null)
            {
                Debug.LogWarning("[FoggyRoad] Heavy shadow target was not found: " + targetRootNames[i], this);
                continue;
            }

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                MeshRenderer renderer = renderers[r];
                if (renderer == null) continue;
                targets.Add(renderer);
                originalModes.Add(renderer.shadowCastingMode);
            }
        }
    }

    private static Transform FindNamedTransform(GameObject[] roots, string targetName)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindNamedTransform(roots[i].transform, targetName);
            if (found != null) return found;
        }

        return null;
    }

    private static Transform FindNamedTransform(Transform current, string targetName)
    {
        if (current.name == targetName) return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindNamedTransform(current.GetChild(i), targetName);
            if (found != null) return found;
        }

        return null;
    }
}
