using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Opens a hinged door from either face when the player aims at it and presses E or left click.
/// Attach this to the hinge/pivot object, not the visual door mesh.
/// </summary>
[DisallowMultipleComponent]
public sealed class TwoSidedDoor : MonoBehaviour
{
    private static readonly HashSet<TwoSidedDoor> activeDoors = new HashSet<TwoSidedDoor>();
    private Bounds interactionBounds;
    private bool hasInteractionBounds;
    public enum OpeningDirection
    {
        [InspectorName("Otomatik (oyuncudan uzağa)")] Automatic = 0,
        [InspectorName("İçeri")] Inward = 1,
        [InspectorName("Dışarı")] Outward = 2
    }

    [Header("Interaction")]
    [SerializeField] private bool allowLeftClick = true;

    [Header("Motion")]
    [SerializeField, InspectorName("Açılma Yönü")]
    private OpeningDirection openingDirection = OpeningDirection.Automatic;
    [SerializeField, InspectorName("İç / Dış Yönünü Ters Çevir")]
    [Tooltip("İçeri yönü kapalı menteşenin yerel +Y tarafıdır. Kapı ters yerleştirildiyse bu seçeneği açın.")]
    private bool invertOpeningDirection;
    [SerializeField, Range(1f, 170f)] private float openAngle = 92f;
    [SerializeField, Min(1f)] private float degreesPerSecond = 240f;

    public bool AllowLeftClick => allowLeftClick;
    private Quaternion closedLocalRotation;
    private float openedAngle;
    private bool isOpen;

    private void Awake()
    {
        closedLocalRotation = transform.localRotation;
        CacheInteractionBounds();
        if (isActiveAndEnabled) activeDoors.Add(this);
    }

    private void OnEnable() { activeDoors.Add(this); }
    private void OnDisable() { activeDoors.Remove(this); }
    private void OnDestroy() { activeDoors.Remove(this); }

    private void CacheInteractionBounds()
    {
        hasInteractionBounds = false;
        // Use the whole moving panel, including edges and handles. Some scene
        // colliders cover only part of the mesh; leave their physical shape intact.
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<Renderer>();
            if (filter.sharedMesh == null || renderer == null || !renderer.enabled ||
                renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                continue;
            Bounds bounds = filter.sharedMesh.bounds;
            Matrix4x4 toDoor = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f));
                point = toDoor.MultiplyPoint3x4(point);
                if (!hasInteractionBounds) { interactionBounds = new Bounds(point, Vector3.zero); hasInteractionBounds = true; }
                else interactionBounds.Encapsulate(point);
            }
        }
    }

    public static TwoSidedDoor FindInteractionTarget(Ray worldRay, float unobstructedDistance)
    {
        TwoSidedDoor nearest = null;
        float nearestDistance = unobstructedDistance;
        foreach (TwoSidedDoor door in activeDoors)
        {
            if (door == null || !door.isActiveAndEnabled || !door.hasInteractionBounds) continue;
            Ray localRay = new Ray(door.transform.InverseTransformPoint(worldRay.origin),
                door.transform.InverseTransformVector(worldRay.direction));
            if (!door.interactionBounds.IntersectRay(localRay, out float localDistance)) continue;
            Vector3 worldPoint = door.transform.TransformPoint(localRay.GetPoint(localDistance));
            float distance = Vector3.Distance(worldRay.origin, worldPoint);
            // A wall/other physical target in front of the panel still blocks it.
            if (distance > nearestDistance) continue;
            nearestDistance = distance;
            nearest = door;
        }
        return nearest;
    }

    private void Update()
    {
        Quaternion targetRotation = isOpen
            ? closedLocalRotation * Quaternion.AngleAxis(openedAngle, Vector3.forward)
            : closedLocalRotation;

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            degreesPerSecond * Time.deltaTime);
    }

    public void Interact(Vector3 playerPosition, Vector3 viewDirection)
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
            playerSide = transform.InverseTransformDirection(viewDirection).y;

        openedAngle = playerSide >= 0f ? -openAngle : openAngle;
        isOpen = true;
    }

}
