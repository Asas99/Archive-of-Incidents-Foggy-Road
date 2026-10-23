using UnityEngine;
using UnityEngine.UI;

/// <summary>Inspector controls for the dot's native UI color, alpha and size.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
[AddComponentMenu("UI/Reticle Appearance")]
public sealed class ReticleAppearance : MonoBehaviour
{
    // Appearance stays on RawImage/RectTransform so native Inspector edits also persist.
}
