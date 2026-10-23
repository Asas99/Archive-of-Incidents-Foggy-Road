using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns the centre target, interaction input and walking reticle.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private CharacterController player;
    [SerializeField] private PickupDrop pickup;
    [SerializeField, Min(0.1f)] private float interactionRange = 4f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    // Fallback defaults only. The saved Dot UI owns its appearance.
    [SerializeField, HideInInspector] private float dotDiameter = 3f;
    [SerializeField, HideInInspector] private float dotOpacity = 0.75f;

    [SerializeField, HideInInspector] private Camera playerCamera;
    [SerializeField] private Canvas reticleCanvas;
    [SerializeField] private RawImage reticleDot;
    private readonly RaycastHit[] hits = new RaycastHit[16];
    private Texture2D dotTexture;

    public CarController DrivingVehicle { get; private set; }
    public bool IsDriving => DrivingVehicle != null && DrivingVehicle.HasDriver;
    public float InteractionRange => interactionRange;
    public bool IsReticleVisible => isActiveAndEnabled && !IsDriving &&
        playerCamera != null && playerCamera.isActiveAndEnabled;

    public Rect GetReticleRect(int width, int height)
    {
        // Keep the dot visible even in the small Editor Game View.
        float diameter = Mathf.Max(2f, dotDiameter * height / 1080f);
        return new Rect((width - diameter) * 0.5f, (height - diameter) * 0.5f, diameter, diameter);
    }

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        if (player == null) player = GetComponentInParent<CharacterController>();
        if (pickup == null && player != null) pickup = player.GetComponent<PickupDrop>();
        CreateReticle();
    }

    private void LateUpdate()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (!playerCamera.isActiveAndEnabled) { RefreshReticle(); return; }
        ProcessInteraction(Input.GetKeyDown(interactKey), Input.GetMouseButtonDown(0));
        RefreshReticle();
    }

    // Also usable by deterministic gameplay checks without synthesizing keyboard events.
    public void ProcessInteraction(bool useKey, bool leftClick)
    {
        if (IsDriving)
        {
            if (useKey) DrivingVehicle.ExitCar();
            return;
        }

        if (pickup != null && pickup.isActiveAndEnabled && pickup.IsHolding)
        {
            if (leftClick || useKey) pickup.ReleaseHeld(leftClick);
            return;
        }

        bool hasHit = TryGetTarget(out RaycastHit hit);
        TwoSidedDoor door = hasHit ? hit.collider.GetComponentInParent<TwoSidedDoor>() : null;
        if (door == null)
            door = TwoSidedDoor.FindInteractionTarget(
                new Ray(playerCamera.transform.position, playerCamera.transform.forward),
                hasHit ? Mathf.Min(interactionRange, hit.distance) : interactionRange);
        if (door != null)
        {
            SetStatus(door.isActiveAndEnabled ? "E - Kapıyı aç / kapat" : "");
            if (door.isActiveAndEnabled && (useKey || (leftClick && door.AllowLeftClick)))
                door.Interact(playerCamera.transform.position, playerCamera.transform.forward);
            return;
        }

        if (!hasHit)
        {
            SetStatus("Ortadaki noktayla bir nesneye bak (en fazla " + interactionRange.ToString("0.#") + " m).");
            return;
        }

        GeneratorManager generator = GeneratorManager.FindForTarget(hit.transform);
        if (generator != null)
        {
            bool isLever = generator.isActiveAndEnabled && generator.IsLever(hit.transform);
            SetStatus(isLever ? "E - Jeneratörü aç / kapat" : "Jeneratörün kolunu hedefle.");
            if (isLever && (useKey || (leftClick && generator.AllowLeftClick))) generator.Interact();
            return;
        }

        CarController car = hit.collider.GetComponentInParent<CarController>();
        if (car != null)
        {
            SetStatus(car.isActiveAndEnabled && !car.HasDriver ? "E - Arabaya bin" : "");
            if (useKey && car.isActiveAndEnabled && !car.HasDriver && player != null)
            {
                if (car.TryEnterCar(player)) DrivingVehicle = car;
            }
            return;
        }

        bool canPickup = pickup != null && pickup.isActiveAndEnabled && pickup.CanPickup(hit);
        SetStatus(canPickup ? "E - " + hit.rigidbody.name + " al" : "");
        if (useKey && canPickup) pickup.TryPickup(hit);
    }

    public bool TryGetTarget(out RaycastHit target)
    {
        target = default;
        if (playerCamera == null || IsDriving) return false;
        // Start at the camera, not its near plane, so the range is exactly camera-to-surface.
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        int count = Physics.RaycastNonAlloc(ray, hits, interactionRange,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit[] candidates = hits;
        if (count == hits.Length)
        {
            candidates = Physics.RaycastAll(ray, interactionRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            count = candidates.Length;
        }
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = candidates[i];
            if (player != null && hit.transform.IsChildOf(player.transform)) continue;
            if (hit.distance >= nearest) continue;
            target = hit;
            nearest = hit.distance;
        }
        return target.collider != null;
    }

    private void SetStatus(string text)
    {
        if (pickup != null && pickup.isActiveAndEnabled) pickup.SetTargetStatus(text);
    }

    public void RefreshReticle()
    {
        if (reticleCanvas == null) return;
        reticleCanvas.enabled = IsReticleVisible;
    }

    private void CreateReticle()
    {
        if (reticleDot != null) return;
        const int size = 32;
        dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        dotTexture.name = "Soft centre dot";
        dotTexture.wrapMode = TextureWrapMode.Clamp;
        dotTexture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude;
                float alpha = 1f - Mathf.SmoothStep(size / 2f - 5f, size / 2f, radius);
                dotTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        dotTexture.Apply(false, true);
        GameObject overlay = new GameObject("Interaction Reticle", typeof(RectTransform), typeof(Canvas));
        overlay.layer = 5;
        overlay.transform.SetParent(transform, false);
        reticleCanvas = overlay.GetComponent<Canvas>();
        reticleCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        reticleCanvas.worldCamera = playerCamera;
        reticleCanvas.planeDistance = playerCamera.nearClipPlane + 0.1f;
        reticleCanvas.sortingOrder = 32767;
        playerCamera.cullingMask |= 1 << 5;
        GameObject image = new GameObject("Dot", typeof(RectTransform), typeof(RawImage));
        image.layer = 5;
        image.transform.SetParent(overlay.transform, false);
        reticleDot = image.GetComponent<RawImage>();
        reticleDot.texture = dotTexture;
        reticleDot.color = new Color(1f, 1f, 1f, dotOpacity);
        reticleDot.rectTransform.sizeDelta = Vector2.one * GetReticleRect(Screen.width, Screen.height).width;
        image.AddComponent<ReticleAppearance>();
        reticleDot.raycastTarget = false;
        reticleDot.rectTransform.anchorMin = reticleDot.rectTransform.anchorMax = Vector2.one * 0.5f;
        reticleDot.rectTransform.pivot = Vector2.one * 0.5f;
        reticleDot.rectTransform.anchoredPosition = Vector2.zero;
        RefreshReticle();
    }

    private void OnDisable()
    {
        if (reticleCanvas != null) reticleCanvas.enabled = false;
    }

    private void OnDestroy()
    {
        if (dotTexture != null && reticleCanvas != null) DestroyOwned(reticleCanvas.gameObject);
        DestroyOwned(dotTexture);
    }

    private static void DestroyOwned(Object owned)
    {
        if (owned == null) return;
        if (Application.isPlaying) Destroy(owned);
        else DestroyImmediate(owned);
    }
}
