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
    [SerializeField] private LayerMask pickupLayers = ~0;

    [Header("Holding")]
    [SerializeField, Min(0.1f)] private float holdDistance = 2f;
    [SerializeField, Min(0.1f)] private float followSpeed = 14f;
    [SerializeField, Min(0f)] private float throwForce = 8f;
    [SerializeField] private bool showDebugText = true;

    private Rigidbody heldBody;
    private Transform originalParent;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private string status = "Ortadaki noktayla nesneye bak ve E tuşuna bas.";

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    public bool IsHolding => heldBody != null;

    private void LateUpdate()
    {
        if (heldBody != null) MoveHeldObject();
    }

    public bool CanPickup(RaycastHit hit)
    {
        if (hit.collider == null || heldBody != null || playerCamera == null) return false;
        if ((pickupLayers.value & (1 << hit.collider.gameObject.layer)) == 0) return false;
        Rigidbody body = hit.rigidbody;
        return body != null && body.GetComponentInParent<CarController>() == null &&
               hit.collider.GetComponentInParent<TwoSidedDoor>() == null &&
               GeneratorManager.FindForTarget(hit.transform) == null &&
               body.GetComponentInParent<CharacterController>() == null;
    }

    public bool TryPickup(RaycastHit hit)
    {
        if (!CanPickup(hit)) return false;
        Rigidbody body = hit.rigidbody;
        heldBody = body;
        originalParent = heldBody.transform.parent;
        originalUseGravity = heldBody.useGravity;
        originalIsKinematic = heldBody.isKinematic;

        if (!heldBody.isKinematic)
        {
            heldBody.linearVelocity = Vector3.zero;
            heldBody.angularVelocity = Vector3.zero;
        }
        heldBody.useGravity = false;
        heldBody.isKinematic = true;
        heldBody.transform.SetParent(holdPoint != null ? holdPoint : playerCamera.transform, true);
        MoveHeldObject(true);
        status = $"{body.name} elde. E - Bırak | Sol tık - Fırlat";
        return true;
    }

    private void MoveHeldObject(bool snap = false)
    {
        Transform anchor = holdPoint != null ? holdPoint : playerCamera.transform;
        Vector3 targetPosition = anchor.position + anchor.forward * holdDistance;

        heldBody.transform.position = snap
            ? targetPosition
            : Vector3.Lerp(heldBody.transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    public void ReleaseHeld(bool throwObject)
    {
        Release(throwObject ? throwForce : 0f);
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

        if (force > 0f && playerCamera != null && !body.isKinematic)
            body.AddForce(playerCamera.transform.forward * force, ForceMode.Impulse);

        status = $"{body.name} bırakıldı.";
    }

    public void SetTargetStatus(string text)
    {
        if (!IsHolding) status = text;
    }

    private void OnGUI()
    {
        if (!showDebugText || string.IsNullOrEmpty(status))
            return;

        GUI.Box(new Rect(20f, Screen.height - 58f, 520f, 38f), status);
    }

    private void OnDisable()
    {
        Release(0f);
    }
}
