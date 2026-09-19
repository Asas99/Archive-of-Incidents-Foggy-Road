using UnityEngine;

/// <summary>
/// Lets a first-person player pick up a rigidbody in front of the camera and drop or throw it.
/// Attach this component to the player, then assign its camera in the Inspector.
/// </summary>
public class PickupDrop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;

    [Header("Pickup")]
    [SerializeField, Min(0.1f)] private float pickupRange = 3f;
    [SerializeField] private LayerMask pickupLayers = ~0;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool useMouseCursor = true;

    [Header("Holding")]
    [SerializeField, Min(0.1f)] private float holdDistance = 2f;
    [SerializeField, Min(0.1f)] private float followSpeed = 14f;
    [SerializeField] private KeyCode throwKey = KeyCode.Mouse0;
    [SerializeField, Min(0f)] private float throwForce = 8f;
    [SerializeField] private bool showDebugText = true;

    private Rigidbody heldBody;
    private Transform originalParent;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private string status = "Aim at an object and press E.";

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (heldBody == null)
        {
            UpdateTargetStatus();

            if (Input.GetKeyDown(interactKey))
                TryPickup();

            return;
        }

        MoveHeldObject();

        if (Input.GetKeyDown(throwKey))
            Release(throwForce);
        else if (Input.GetKeyDown(interactKey))
            Release(0f);
    }

    private void TryPickup()
    {
        if (playerCamera == null)
        {
            status = "PickupDrop: Assign Player Camera in the Inspector.";
            return;
        }

        Ray ray = GetPickupRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayers, QueryTriggerInteraction.Ignore))
        {
            status = "No object in range at the centre of the camera.";
            return;
        }

        Rigidbody body = hit.rigidbody != null
            ? hit.rigidbody
            : hit.collider.GetComponentInParent<Rigidbody>();
        print(hit.rigidbody.transform.gameObject.name);
        if (body == null)
        {
            status = $"{hit.collider.name} has no Rigidbody.";
            return;
        }

        heldBody = body;
        originalParent = heldBody.transform.parent;
        originalUseGravity = heldBody.useGravity;
        originalIsKinematic = heldBody.isKinematic;

        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.useGravity = false;
        heldBody.isKinematic = true;
        heldBody.transform.SetParent(holdPoint != null ? holdPoint : playerCamera.transform, true);
        MoveHeldObject(true);
        status = $"Holding {body.name}. E drops; left mouse throws.";
    }

    private void MoveHeldObject(bool snap = false)
    {
        Transform anchor = holdPoint != null ? holdPoint : playerCamera.transform;
        Vector3 targetPosition = anchor.position + anchor.forward * holdDistance;

        heldBody.transform.position = snap
            ? targetPosition
            : Vector3.Lerp(heldBody.transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    private void Release(float force)
    {
        if (heldBody == null)
            return;

        Rigidbody body = heldBody;
        heldBody = null;

        body.transform.SetParent(originalParent, true);
        body.isKinematic = originalIsKinematic;
        body.useGravity = originalUseGravity;

        if (force > 0f && playerCamera != null)
            body.AddForce(playerCamera.transform.forward * force, ForceMode.Impulse);

        status = $"Dropped {body.name}.";
    }

    private void UpdateTargetStatus()
    {
        if (playerCamera == null)
        {
            status = "PickupDrop: Assign Player Camera in the Inspector.";
            return;
        }

        Ray ray = GetPickupRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayers, QueryTriggerInteraction.Ignore))
        {
            status = useMouseCursor
                ? "Move the mouse cursor over an object."
                : "Aim the centre of the camera at an object.";
            return;
        }

        Rigidbody body = hit.rigidbody != null
            ? hit.rigidbody
            : hit.collider.GetComponentInParent<Rigidbody>();
        status = body == null
            ? $"Aiming at {hit.collider.name}: it needs a Rigidbody."
            : $"Aiming at {body.name}: press {interactKey} to pick up.";
    }

    private Ray GetPickupRay()
    {
        return useMouseCursor
            ? playerCamera.ScreenPointToRay(Input.mousePosition)
            : new Ray(playerCamera.transform.position, playerCamera.transform.forward);
    }

    private void OnGUI()
    {
        if (!showDebugText)
            return;

        GUI.Box(new Rect(20f, Screen.height - 58f, 520f, 38f), status);
    }

    private void OnDisable()
    {
        Release(0f);
    }
}
