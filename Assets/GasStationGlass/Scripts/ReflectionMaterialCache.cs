using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Reuse capture material arrays, while detecting slot changes on every capture.</summary>
internal sealed class ReflectionMaterialCache
{
    sealed class Entry
    {
        public readonly List<Material> current = new List<Material>();
        public Material[] snapshot = Array.Empty<Material>();
    }

    readonly Dictionary<Renderer, Entry> entries = new Dictionary<Renderer, Entry>();

    public Material[] Get(Renderer renderer)
    {
        if (!renderer) return Array.Empty<Material>();
        if (!entries.TryGetValue(renderer, out var entry))
        {
            entry = new Entry();
            entries.Add(renderer, entry);
        }

        // sharedMaterials creates a fresh array, including for stationary actors.
        // Reading into a retained list also detects runtime replacements and reordering.
        renderer.GetSharedMaterials(entry.current);
        bool changed = entry.current.Count != entry.snapshot.Length;
        for (int i = 0; !changed && i < entry.current.Count; i++)
            changed = !ReferenceEquals(entry.current[i], entry.snapshot[i]);
        if (changed) entry.snapshot = entry.current.ToArray();
        return entry.snapshot;
    }

    public void Clear() => entries.Clear();
}
