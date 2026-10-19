using System;
using UnityEngine;

/// <summary>Editor-baked structural geometry for reflection depth only. No colliders or renderers.</summary>
public sealed class GlassReflectionOcclusion:MonoBehaviour
{
    [Serializable] public struct Part {public Transform source;public Mesh mesh;}
    public Part[] parts=Array.Empty<Part>();
}
