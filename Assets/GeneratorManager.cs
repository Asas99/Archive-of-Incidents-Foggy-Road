using UnityEngine;

/// <summary>Controls the generator with its physical switch lever.</summary>
[DisallowMultipleComponent]
public sealed class GeneratorManager : MonoBehaviour
{
    [Header("Generator Controls")]
    [SerializeField] private Transform lever;
    [SerializeField] private Light greenSpotlight;
    [SerializeField] private Light redSpotlight;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool allowLeftClick = true;
    [SerializeField, Min(1f)] private float degreesPerSecond = 180f;

    private Camera playerCamera;
    private Quaternion leverDownRotation;
    private Quaternion leverUpRotation;
    public bool isOn;

    private void Awake()
    {
        if (lever != null)
        {
            leverDownRotation = lever.localRotation;
            // The lever mesh extends along its local Z axis. Rotate around its
            // local Y hinge axis so it swings through the vertical plane.
            leverUpRotation = leverDownRotation * Quaternion.AngleAxis(-90f, Vector3.up);
        }

        playerCamera = FindPlayerCamera();
        SetGeneratorState(false);
    }

    private void Update()
    {
        if (playerCamera == null)
            playerCamera = FindPlayerCamera();

        if (playerCamera != null && WantsToInteract() && IsAimingAtLever())
            SetGeneratorState(!isOn);

        if (lever != null)
        {
            Quaternion target = isOn ? leverUpRotation : leverDownRotation;
            lever.localRotation = Quaternion.RotateTowards(
                lever.localRotation, target, degreesPerSecond * Time.deltaTime);
        }
    }

    private bool WantsToInteract()
    {
        return Input.GetKeyDown(interactKey) ||
               (allowLeftClick && Input.GetMouseButtonDown(0));
    }

    private bool IsAimingAtLever()
    {
        bool cursorIsFree = Cursor.lockState == CursorLockMode.None &&
                            Input.mousePosition.x >= 0f && Input.mousePosition.x <= Screen.width &&
                            Input.mousePosition.y >= 0f && Input.mousePosition.y <= Screen.height;
        Vector3 point = cursorIsFree
            ? Input.mousePosition
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Ray ray = playerCamera.ScreenPointToRay(point);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        return lever != null && (hit.transform == lever || hit.transform.IsChildOf(lever));
    }

    private void SetGeneratorState(bool turnOn)
    {
        isOn = turnOn;
        if (greenSpotlight != null)
            greenSpotlight.enabled = isOn;
        if (redSpotlight != null)
            redSpotlight.enabled = !isOn;
    }

    private static Camera FindPlayerCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (camera.isActiveAndEnabled && camera.cameraType == CameraType.Game && camera.targetTexture == null)
                return camera;

        return null;
    }
}
