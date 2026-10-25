using System;
using System.Collections.Generic;
using UnityEngine;

// Coarse bounds only reject entire off-screen/distant branches. Accepted leaves
// run the original per-patch tests, so the visible candidate set is unchanged.
public sealed class VegetationPatchHierarchy
{
    struct Node
    {
        public Bounds bounds;
        public int first, length, left, right, instances;
    }
    public struct Stats
    {
        public int nodeTests, patchTests, rangeRejected, frustumRejected;
    }
    readonly FarGrassCache.Patch[] patches;
    readonly int[] order;
    readonly Node[] nodes;
    public int InstanceCount { get; }

    public VegetationPatchHierarchy(FarGrassCache.Patch[] patches)
    {
        this.patches = patches;
        order = new int[patches.Length];
        for (int i = 0; i < order.Length; i++) { order[i] = i; InstanceCount += patches[i].matrices.Length; }
        var building = new List<Node>();
        if (order.Length > 0) Build(building, 0, order.Length);
        nodes = building.ToArray();
    }

    int Build(List<Node> building, int first, int length)
    {
        Bounds bounds = patches[order[first]].bounds;
        int instances = 0;
        for (int i = first; i < first + length; i++)
        {
            bounds.Encapsulate(patches[order[i]].bounds);
            instances += patches[order[i]].matrices.Length;
        }
        int index = building.Count;
        var node = new Node { bounds = bounds, first = first, length = length, left = -1, right = -1, instances = instances };
        building.Add(node);
        if (length > 8)
        {
            Vector3 size = bounds.size;
            int axis = size.x > size.z ? 0 : 2;
            Array.Sort(order, first, length, Comparer<int>.Create((a, b) => patches[a].bounds.center[axis].CompareTo(patches[b].bounds.center[axis])));
            int half = length / 2;
            node.left = Build(building, first, half);
            node.right = Build(building, first + half, length - half);
        }
        building[index] = node;
        return index;
    }

    public int Query(Plane[] planes, Vector3 eye, float farSquared, float windPadding, int[] accepted, out Stats stats)
    {
        stats = default;
        int count = 0;
        if (nodes.Length > 0) Visit(0, planes, eye, farSquared, windPadding, accepted, ref count, ref stats);
        // Keep the original submission order. The hierarchy changes only how
        // candidates are found, not ordering, placement, density or thresholds.
        Array.Sort(accepted, 0, count);
        return count;
    }

    void Visit(int index, Plane[] planes, Vector3 eye, float farSquared, float padding, int[] accepted, ref int count, ref Stats stats)
    {
        Node node = nodes[index];
        stats.nodeTests++;
        var expanded = node.bounds;
        // Rounding of union min/max must never make a parent smaller than a
        // child. The extra centimetre affects only coarse rejection, never leaves.
        expanded.Expand(padding * 2 + .02f);
        if (expanded.SqrDistance(eye) > farSquared) { stats.rangeRejected += node.instances; return; }
        if (!GeometryUtility.TestPlanesAABB(planes, expanded)) { stats.frustumRejected += node.instances; return; }
        if (node.left >= 0)
        {
            Visit(node.left, planes, eye, farSquared, padding, accepted, ref count, ref stats);
            Visit(node.right, planes, eye, farSquared, padding, accepted, ref count, ref stats);
            return;
        }
        for (int i = node.first; i < node.first + node.length; i++)
        {
            int p = order[i];
            stats.patchTests++;
            var patch = patches[p];
            expanded = patch.bounds;
            expanded.Expand(padding * 2);
            if (expanded.SqrDistance(eye) > farSquared) { stats.rangeRejected += patch.matrices.Length; continue; }
            if (!GeometryUtility.TestPlanesAABB(planes, expanded)) { stats.frustumRejected += patch.matrices.Length; continue; }
            accepted[count++] = p;
        }
    }
}
