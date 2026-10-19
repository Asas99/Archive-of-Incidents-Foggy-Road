using UnityEngine;

/// <summary>
/// Opens a hinged door from either face when the player aims at it and presses E or left click.
/// Attach this to the hinge/pivot object, not the visual door mesh.
/// </summary>
[DisallowMultipleComponent]
public sealed class TwoSidedDoor : MonoBehaviour
{
    public enum OpeningDirection
    {
        [InspectorName("Otomatik (oyuncudan uzağa)")] Automatic = 0,
        [InspectorName("İçeri")] Inward = 1,
        [InspectorName("Dışarı")] Outward = 2
    }

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool allowLeftClick = true;

    [Header("Motion")]
    [SerializeField, InspectorName("Açılma Yönü")]
    private OpeningDirection openingDirection = OpeningDirection.Automatic;
    [SerializeField, InspectorName("İç / Dış Yönünü Ters Çevir")]
    [Tooltip("İçeri yönü kapalı menteşenin yerel +Y tarafıdır. Kapı ters yerleştirildiyse bu seçeneği açın.")]
    private bool invertOpeningDirection;
    [SerializeField, Range(1f, 170f)] private float openAngle = 92f;
    [SerializeField, Min(1f)] private float degreesPerSecond = 240f;

    private Camera playerCamera;
    private Quaternion closedLocalRotation;
    private float openedAngle;
    private bool isOpen;

    private void Awake()
    {
        closedLocalRotation = transform.localRotation;
        playerCamera = FindPlayerCamera();
    }

    private void Update()
    {
        if (playerCamera == null)
            playerCamera = FindPlayerCamera();

        if (playerCamera != null && WantsToInteract() && IsAimingAtThisDoor())
            Toggle(playerCamera.transform.position);

        Quaternion targetRotation = isOpen
            ? closedLocalRotation * Quaternion.AngleAxis(openedAngle, Vector3.forward)
            : closedLocalRotation;

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            degreesPerSecond * Time.deltaTime);
    }

    private bool WantsToInteract()
    {
        return Input.GetKeyDown(interactKey) ||
               (allowLeftClick && Input.GetMouseButtonDown(0));
    }

    private bool IsAimingAtThisDoor()
    {
        Ray ray = GetInteractionRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider.GetComponentInParent<TwoSidedDoor>() == this;
    }

    private Ray GetInteractionRay()
    {
        bool cursorIsFree = Cursor.lockState == CursorLockMode.None &&
                            Input.mousePosition.x >= 0f && Input.mousePosition.x <= Screen.width &&
                            Input.mousePosition.y >= 0f && Input.mousePosition.y <= Screen.height;

        Vector3 screenPoint = cursorIsFree
            ? Input.mousePosition
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        return playerCamera.ScreenPointToRay(screenPoint);
    }

    private void Toggle(Vector3 playerPosition)
    {
        if (isOpen)
        {
            isOpen = false;
            return;
        }

        if (openingDirection != OpeningDirection.Automatic)
        {
            // The panel extends along local +X; positive Z swings toward local +Y.
            openedAngle = openingDirection == OpeningDirection.Inward ? openAngle : -openAngle;
            if (invertOpeningDirection)
                openedAngle = -openedAngle;
            isOpen = true;
            return;
        }

        // The hinge's local up axis is the door face normal. Pick the opposite
        // swing direction on each side so the panel moves away from the player.
        float playerSide = transform.InverseTransformPoint(playerPosition).y;
        if (Mathf.Approximately(playerSide, 0f))
            playerSide = transform.InverseTransformDirection(playerCamera.transform.forward).y;

        openedAngle = playerSide >= 0f ? -openAngle : openAngle;
        isOpen = true;
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
