using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class FoggyRoadFoliageOptimization : MonoBehaviour
{
    [Min(1f)] public float foliageShadowDistance = 28f;
    [Min(1f)] public float rockShadowDistance = 32f;
    [Min(1f)] public float fogCullDistance = 235f;
    [Min(1f)] public float smallFoliageCullDistance = 105f;
    [Min(0f)] public float fogCullHysteresis = 16f;
    [Min(0f)] public float shadowHysteresis = 10f;
    [Min(0.05f)] public float refreshInterval = 0.2f;
    public bool limitDistantFoliageShadows;
    public bool limitDistantRockShadows;
    public bool cullFogHiddenFoliage;
    public bool cullSmallFoliageEarlier;

    private readonly List<Entry> entries = new List<Entry>();
    private float nextRefreshTime;

    private sealed class Entry
    {
        public Renderer renderer;
        public ShadowCastingMode originalShadowCastingMode;
        public bool originalForceRenderingOff;
        public bool isFoliage;
        public bool isRock;
        public bool isSmallFoliage;
        public bool canCullInFog;
        public bool cullApplied;
        public bool shadowTrimApplied;
        public ShadowCastingMode appliedShadowCastingMode;
        public bool appliedForceRenderingOff;
    }

    private void OnEnable()
    {
        BuildCache();
        nextRefreshTime = 0f;
    }

    private void Update()
    {
        if (!Application.isPlaying || Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + refreshInterval;
        Refresh();
    }

    private void OnDisable()
    {
        RestoreOriginalState();
    }

    private void BuildCache()
    {
        RestoreOriginalState();
        entries.Clear();

        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
            bool foliage = IsFoliage(renderer);
            bool rock = IsRock(renderer);
            bool fogCullCandidate = foliage || rock;
            if (!foliage && !fogCullCandidate) continue;

            entries.Add(new Entry
            {
                renderer = renderer,
                originalShadowCastingMode = renderer.shadowCastingMode,
                originalForceRenderingOff = renderer.forceRenderingOff,
                isFoliage = foliage,
                isRock = rock,
                isSmallFoliage = IsSmallFoliage(renderer),
                canCullInFog = fogCullCandidate,
                appliedShadowCastingMode = renderer.shadowCastingMode,
                appliedForceRenderingOff = renderer.forceRenderingOff
            });
        }
    }

    private void Refresh()
    {
        Vector3 cameraPosition = transform.position;

        foreach (Entry entry in entries)
        {
            if (entry.renderer == null) continue;
            float distanceSqr = (entry.renderer.bounds.center - cameraPosition).sqrMagnitude;

            // Golgeyi acip kapatirken tek bir esik kullanmak "bir anda golge olustu" seklinde
            // gorunur pop yaratiyordu. Kapatma esigi uzakta, tekrar acma esigi daha yakinda
            // olacak sekilde histerezis uyguluyoruz; boylece sinirin uzerinde gidip gelirken
            // golge yanip sonmuyor.
            float shadowLimit = entry.isRock ? rockShadowDistance : foliageShadowDistance;
            bool shadowLimitEnabled = (limitDistantFoliageShadows && entry.isFoliage) ||
                                      (limitDistantRockShadows && entry.isRock);

            if (!shadowLimitEnabled)
            {
                entry.shadowTrimApplied = false;
            }
            else
            {
                float offDistanceSqr = shadowLimit * shadowLimit;
                float onDistance = Mathf.Max(1f, shadowLimit - shadowHysteresis);
                float onDistanceSqr = onDistance * onDistance;

                if (!entry.shadowTrimApplied && distanceSqr > offDistanceSqr)
                    entry.shadowTrimApplied = true;
                else if (entry.shadowTrimApplied && distanceSqr < onDistanceSqr)
                    entry.shadowTrimApplied = false;
            }

            ShadowCastingMode desiredShadowMode = entry.shadowTrimApplied
                ? ShadowCastingMode.Off
                : entry.originalShadowCastingMode;
            if (entry.appliedShadowCastingMode != desiredShadowMode)
            {
                entry.renderer.shadowCastingMode = desiredShadowMode;
                entry.appliedShadowCastingMode = desiredShadowMode;
            }

            float cullDistance = float.PositiveInfinity;
            if (cullFogHiddenFoliage && entry.canCullInFog)
                cullDistance = fogCullDistance;
            if (cullSmallFoliageEarlier && entry.isSmallFoliage)
                cullDistance = Mathf.Min(cullDistance, smallFoliageCullDistance);

            if (float.IsPositiveInfinity(cullDistance))
            {
                entry.cullApplied = false;
                SetForceRenderingOff(entry, entry.originalForceRenderingOff);
                continue;
            }

            float targetCullDistanceSqr = cullDistance * cullDistance;
            float targetRestoreDistance = Mathf.Max(1f, cullDistance - fogCullHysteresis);
            float targetRestoreDistanceSqr = targetRestoreDistance * targetRestoreDistance;

            if (!entry.cullApplied && distanceSqr > targetCullDistanceSqr)
                entry.cullApplied = true;
            else if (entry.cullApplied && distanceSqr < targetRestoreDistanceSqr)
                entry.cullApplied = false;

            SetForceRenderingOff(entry, entry.originalForceRenderingOff || entry.cullApplied);
        }
    }

    private static void SetForceRenderingOff(Entry entry, bool value)
    {
        if (entry.appliedForceRenderingOff == value) return;
        entry.renderer.forceRenderingOff = value;
        entry.appliedForceRenderingOff = value;
    }

    private void RestoreOriginalState()
    {
        foreach (Entry entry in entries)
        {
            if (entry.renderer == null) continue;
            if (entry.appliedShadowCastingMode != entry.originalShadowCastingMode)
                entry.renderer.shadowCastingMode = entry.originalShadowCastingMode;
            if (entry.appliedForceRenderingOff != entry.originalForceRenderingOff)
                entry.renderer.forceRenderingOff = entry.originalForceRenderingOff;
        }
    }

    private static bool IsFoliage(Renderer renderer)
    {
        string name = renderer.name.ToLowerInvariant();
        if (name.Contains("pine") || name.Contains("fern") || name.Contains("bush") ||
            name.Contains("grass") || name.Contains("meadow"))
            return true;

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material != null && material.shader != null &&
                material.shader.name.IndexOf("TerrainGrass", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static bool IsRock(Renderer renderer)
    {
        return renderer.name.IndexOf("rock", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsSmallFoliage(Renderer renderer)
    {
        string name = renderer.name.ToLowerInvariant();
        return name.Contains("fern") || name.Contains("bush") || name.Contains("grass") ||
               name.Contains("meadow");
    }
}
