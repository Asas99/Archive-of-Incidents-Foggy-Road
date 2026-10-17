using UnityEngine;

/// <summary>Explicit, persistent reflection sources on each side of one pane. No runtime scans.</summary>
[DisallowMultipleComponent]
public sealed class GlassReflectionBinding : MonoBehaviour
{
    public ReflectionProbe positive, negative;
    public string positiveRegion, negativeRegion;
    [HideInInspector] public string verifiedIdentity;
    public bool Verified => TryGetComponent<GlassSurfaceVariation>(out var pane) &&
        verifiedIdentity == pane.StableId && positive && negative &&
        positive.customBakedTexture && negative.customBakedTexture;
}
