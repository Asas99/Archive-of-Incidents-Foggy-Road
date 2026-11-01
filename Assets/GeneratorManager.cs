using System.Collections.Generic;
using UnityEngine;

/// <summary>Controls the generator with its physical switch lever.</summary>
[DisallowMultipleComponent]
public sealed class GeneratorManager : MonoBehaviour
{
    private static readonly HashSet<GeneratorManager> activeGenerators = new HashSet<GeneratorManager>();
    [Header("Generator Controls")]
    [SerializeField] private Transform lever;
    [SerializeField] private Light greenSpotlight;
    [SerializeField] private Light redSpotlight;

    [Header("Interaction")]
    [SerializeField] private bool allowLeftClick = true;
    [SerializeField] private Collider interactionOccluder;
    [SerializeField, Min(1f)] private float degreesPerSecond = 180f;

    public bool AllowLeftClick => allowLeftClick;
    private Collider[] leverColliders;
    private Quaternion leverDownRotation;
    private Quaternion leverUpRotation;
    public bool isOn;

    private void OnEnable() { activeGenerators.Add(this); }
    private void OnDisable() { activeGenerators.Remove(this); }

    // The scene's switch lever is a separate root object, not a child of this manager.
    public static GeneratorManager FindForTarget(Transform target)
    {
        foreach (GeneratorManager generator in activeGenerators)
            if (generator != null && generator.IsLever(target)) return generator;
        return target.GetComponentInParent<GeneratorManager>();
    }

    // A simplified generator body collider can overlap the visible lever.
    // Skip only that explicitly assigned collider; closed doors and other objects
    // continue to block the player's line of sight.
    public static GeneratorManager FindLeverBehindAssignedOccluder(Ray ray, float range, Collider blocker, Transform player)
    {
        if (blocker == null) return null;
        GeneratorManager nearest = null;
        float nearestDistance = range;
        foreach (GeneratorManager generator in activeGenerators)
        {
            if (generator == null || !generator.isActiveAndEnabled || generator.interactionOccluder != blocker || generator.leverColliders == null) continue;
            foreach (Collider leverCollider in generator.leverColliders)
            {
                if (leverCollider == null || !leverCollider.enabled || leverCollider.isTrigger ||
                    !leverCollider.Raycast(ray, out RaycastHit leverHit, nearestDistance)) continue;
                bool obstructed = false;
                foreach (RaycastHit hit in Physics.RaycastAll(ray, leverHit.distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == blocker || generator.IsLever(hit.transform) ||
                        (player != null && hit.transform.IsChildOf(player))) continue;
                    obstructed = true;
                    break;
                }
                if (obstructed) continue;
                nearest = generator;
                nearestDistance = leverHit.distance;
            }
        }
        return nearest;
    }

    private void Awake()
    {
        if (lever != null)
        {
            leverColliders = lever.GetComponentsInChildren<Collider>();
            leverDownRotation = lever.localRotation;
            // The lever mesh extends along its local Z axis. Rotate around its
            // local Y hinge axis so it swings through the vertical plane.
            leverUpRotation = leverDownRotation * Quaternion.AngleAxis(-90f, Vector3.up);
        }

        SetGeneratorState(false);
    }

    private void Update()
    {
        if (lever != null)
        {
            Quaternion target = isOn ? leverUpRotation : leverDownRotation;
            lever.localRotation = Quaternion.RotateTowards(
                lever.localRotation, target, degreesPerSecond * Time.deltaTime);
        }
    }

    public bool IsLever(Transform target)
    {
        return lever != null && (target == lever || target.IsChildOf(lever));
    }

    public void Interact()
    {
        SetGeneratorState(!isOn);
    }

    private void SetGeneratorState(bool turnOn)
    {
        isOn = turnOn;
        if (greenSpotlight != null)
            greenSpotlight.enabled = isOn;
        if (redSpotlight != null)
            redSpotlight.enabled = !isOn;
    }

}
