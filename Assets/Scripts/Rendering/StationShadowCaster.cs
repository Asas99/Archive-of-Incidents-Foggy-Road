using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class StationShadowCaster : MonoBehaviour
{
    [SerializeField] private MeshRenderer sourceRenderer;
    [SerializeField] private ShadowCastingMode originalShadowMode = ShadowCastingMode.On;
    [SerializeField] private Mesh bakedSourceMesh;
    [SerializeField] private int bakedVertexCount;
    [SerializeField] private Bounds bakedBounds;
    private MeshRenderer shadowRenderer;
    private MeshFilter sourceFilter;
    private MeshFilter shadowFilter;

    public MeshRenderer SourceRenderer => sourceRenderer;

    public void Configure(MeshRenderer source)
    {
        sourceRenderer = source;
        originalShadowMode = source.shadowCastingMode;
        sourceFilter = source.GetComponent<MeshFilter>();
        bakedSourceMesh = sourceFilter.sharedMesh;
        bakedVertexCount = bakedSourceMesh.vertexCount;
        bakedBounds = bakedSourceMesh.bounds;
        Synchronize();
    }

    private void OnEnable() => Synchronize();
    private void LateUpdate() => Synchronize();

    private void Synchronize()
    {
        if (shadowRenderer == null) shadowRenderer = GetComponent<MeshRenderer>();
        if (shadowFilter == null) shadowFilter = GetComponent<MeshFilter>();
        if (sourceRenderer == null)
        {
            if (shadowRenderer.enabled) shadowRenderer.enabled = false;
            return;
        }

        if (sourceFilter == null) sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
        // Fall back to the original caster when its mesh is replaced or its geometry bounds change.
        if (sourceFilter == null || sourceFilter.sharedMesh != bakedSourceMesh ||
            bakedSourceMesh == null || bakedSourceMesh.vertexCount != bakedVertexCount ||
            !bakedSourceMesh.bounds.Equals(bakedBounds) || shadowFilter.sharedMesh == null)
        {
            RestoreSource();
            return;
        }

        if (sourceRenderer.shadowCastingMode != ShadowCastingMode.Off)
            sourceRenderer.shadowCastingMode = ShadowCastingMode.Off;
        if (shadowRenderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
            shadowRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        if (shadowRenderer.enabled != sourceRenderer.enabled)
            shadowRenderer.enabled = sourceRenderer.enabled;
        if (shadowRenderer.forceRenderingOff != sourceRenderer.forceRenderingOff)
            shadowRenderer.forceRenderingOff = sourceRenderer.forceRenderingOff;
        if (shadowRenderer.sharedMaterial != sourceRenderer.sharedMaterial)
            shadowRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        if (shadowRenderer.renderingLayerMask != sourceRenderer.renderingLayerMask)
            shadowRenderer.renderingLayerMask = sourceRenderer.renderingLayerMask;
        if (gameObject.layer != sourceRenderer.gameObject.layer)
            gameObject.layer = sourceRenderer.gameObject.layer;
    }

    private void OnDisable() => RestoreSource();

    private void RestoreSource()
    {
        if (sourceRenderer != null && sourceRenderer.shadowCastingMode == ShadowCastingMode.Off)
            sourceRenderer.shadowCastingMode = originalShadowMode;
        if (shadowRenderer != null && shadowRenderer.enabled)
            shadowRenderer.enabled = false;
    }
}

