using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>A mountain-only base camera behind the 500 m gameplay camera.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class DistantMountainBackdrop : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    private Camera mountainCamera;
    private UniversalAdditionalCameraData backgroundData;
    private UniversalAdditionalCameraData foregroundData;

    public Camera GameplayCamera => gameplayCamera;

    public void Configure(Camera gameplay)
    {
        gameplayCamera = gameplay;
        ConfigureRendering();
        Apply();
    }

    private void OnEnable() { ConfigureRendering(); Apply(); }
    private void LateUpdate() { Apply(); }

    private void ConfigureRendering()
    {
        if (mountainCamera == null) mountainCamera = GetComponent<Camera>();
        int layer = LayerMask.NameToLayer("DistantMountains");
        // Fail closed if the dedicated layer is missing; never render the whole scene.
        mountainCamera.cullingMask = layer >= 0 ? 1 << layer : 0;
        mountainCamera.clearFlags = CameraClearFlags.Skybox;
        mountainCamera.useOcclusionCulling = false;
        backgroundData = mountainCamera.GetUniversalAdditionalCameraData();
        backgroundData.renderShadows = false;
        backgroundData.renderPostProcessing = false;
        backgroundData.requiresDepthOption = CameraOverrideOption.Off;
        backgroundData.requiresColorOption = CameraOverrideOption.Off;
        backgroundData.volumeLayerMask = 0;
        mountainCamera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.ViaScripting);
        if (gameplayCamera != null)
        {
            foregroundData = gameplayCamera.GetUniversalAdditionalCameraData();
            if (layer >= 0) gameplayCamera.cullingMask &= ~(1 << layer);
        }
    }

    public void Apply()
    {
        if (gameplayCamera == null) return;
        if (mountainCamera == null) mountainCamera = GetComponent<Camera>();
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        mountainCamera.fieldOfView = gameplayCamera.fieldOfView;
        mountainCamera.aspect = gameplayCamera.aspect;
        mountainCamera.orthographic = gameplayCamera.orthographic;
        mountainCamera.orthographicSize = gameplayCamera.orthographicSize;
        mountainCamera.lensShift = gameplayCamera.lensShift;
        mountainCamera.rect = gameplayCamera.rect;
        mountainCamera.nearClipPlane = gameplayCamera.nearClipPlane;
        mountainCamera.targetDisplay = gameplayCamera.targetDisplay;
        mountainCamera.depth = gameplayCamera.depth - 1f;
        mountainCamera.enabled = gameplayCamera.isActiveAndEnabled;
        // Use the same forward-facing frustum. Unity rejects renderer bounds fully
        // outside it, including mountains behind the player, without deactivating objects.
        mountainCamera.ResetCullingMatrix();
        var background = backgroundData != null ? backgroundData : (backgroundData = mountainCamera.GetUniversalAdditionalCameraData());
        var foreground = foregroundData != null ? foregroundData : (foregroundData = gameplayCamera.GetUniversalAdditionalCameraData());
        if (background.renderType != CameraRenderType.Base) background.renderType = CameraRenderType.Base;
        if (foreground.renderType != CameraRenderType.Overlay) foreground.renderType = CameraRenderType.Overlay;
        if (!background.cameraStack.Contains(gameplayCamera)) background.cameraStack.Add(gameplayCamera);
    }

    private void OnDisable()
    {
        if (mountainCamera != null)
        {
            mountainCamera.enabled = false;
            mountainCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(gameplayCamera);
        }
        if (gameplayCamera != null)
            gameplayCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
    }
}
