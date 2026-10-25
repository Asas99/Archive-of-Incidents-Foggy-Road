using System.Collections.Generic;
using UnityEngine;

// Build static vertex buffers once after the scene loads. Unity still culls the
// individual Renderers and honours their LODGroup membership, layers and shadows.
// Pole geometry is stationary; the procedural wires are deliberately excluded.
[DisallowMultipleComponent]
public sealed class FoggyRoadPoleBatching : MonoBehaviour
{
    public bool batchOnStart = true;
    public int CandidateCount { get; private set; }
    public int BatchedCount { get; private set; }
    bool applied;

    void Start()
    {
        if (batchOnStart) Apply();
    }

    public void Apply()
    {
        if (!Application.isPlaying || applied) return;
        var line = GetComponentInChildren<FoggyRoad_PowerLine>();
        if (!line) return;
        var objects = new List<GameObject>();
        foreach (Transform pole in line.poles)
        {
            if (!pole || pole.GetComponentInParent<Rigidbody>() || pole.GetComponentInChildren<Animator>()) continue;
            foreach (MeshRenderer renderer in pole.GetComponentsInChildren<MeshRenderer>())
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (!renderer.enabled || renderer.isPartOfStaticBatch || !filter || !filter.sharedMesh) continue;
                if (!filter.sharedMesh.isReadable) continue;
                bool supported = renderer.sharedMaterials.Length > 0;
                foreach (Material material in renderer.sharedMaterials)
                    supported &= material && material.GetTag("DisableBatching", false, "False") != "True";
                if (supported) objects.Add(renderer.gameObject);
            }
        }
        CandidateCount = objects.Count;
        if (CandidateCount == 0) return;
        // No per-frame loops, new rendering passes, material variants or changed
        // visibility distances. Runtime buffers disappear when the scene unloads.
        StaticBatchingUtility.Combine(objects.ToArray(), gameObject);
        foreach (GameObject target in objects)
            if (target.GetComponent<MeshRenderer>().isPartOfStaticBatch) BatchedCount++;
        applied = true;
        Debug.Log("[FoggyRoad] Pole static batching: " + BatchedCount + "/" + CandidateCount);
    }
}
