using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting;

/// <summary>Keep URP's normal decal rendering, but omit its passes when no projector can affect this camera.</summary>
public sealed class VisibilityAwareDecalFeature : DecalRendererFeature
{
    public bool skipInvisibleDecals = true;
    readonly Plane[] frustum = new Plane[6];

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (Application.isPlaying && skipInvisibleDecals && camera &&
            camera.cameraType == CameraType.Game && !camera.stereoEnabled)
        {
            GeometryUtility.CalculateFrustumPlanes(
                renderingData.cameraData.GetProjectionMatrix() * renderingData.cameraData.GetViewMatrix(), frustum);
            if (DecalProjectorRegistry.CanSkip(frustum)) return;
        }

        base.AddRenderPasses(renderer, ref renderingData);
    }
}

/// <summary>
/// Subscribe once to the installed URP projector lifecycle. Read transforms and materials live,
/// so spawn, enable, move, resize, material replacement, destruction and additive scenes need no polling.
/// URP exposes these events internally; link.xml preserves them in players. Fail open if that API changes.
/// </summary>
internal static class DecalProjectorRegistry
{
    static readonly List<DecalProjector> projectors = new List<DecalProjector>();
    static EventInfo addEvent, removeEvent;
    static Delegate addHandler, removeHandler;
    static bool attempted, available;

    internal static int Count => projectors.Count;
    internal static bool Available { get { EnsureInitialized(); return available; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Detach();
        projectors.Clear();
        attempted = available = false;
        Application.quitting -= Reset;
    }

    static void EnsureInitialized()
    {
        if (attempted) return;
        attempted = true;
        try
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            addEvent = typeof(DecalProjector).GetEvent("onDecalAdd", flags);
            removeEvent = typeof(DecalProjector).GetEvent("onDecalRemove", flags);
            if (addEvent == null || removeEvent == null)
                throw new MissingMemberException("URP decal lifecycle events are unavailable");

            addHandler = Delegate.CreateDelegate(addEvent.EventHandlerType,
                typeof(DecalProjectorRegistry).GetMethod(nameof(Register), flags));
            removeHandler = Delegate.CreateDelegate(removeEvent.EventHandlerType,
                typeof(DecalProjectorRegistry).GetMethod(nameof(Unregister), flags));
            addEvent.GetAddMethod(true).Invoke(null, new object[] { addHandler });
            removeEvent.GetAddMethod(true).Invoke(null, new object[] { removeHandler });

            // Subscribe before the initial scan; subsequently URP supplies every lifecycle change.
            foreach (var projector in UnityEngine.Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None))
                Register(projector);
            available = true;
            Application.quitting += Reset;
        }
        catch (Exception exception)
        {
            Detach();
            projectors.Clear();
            available = false;
            Debug.LogWarning("Decal visibility optimization unavailable; keeping standard URP rendering. " + exception.Message);
        }
    }

    static void Detach()
    {
        if (addEvent != null && addHandler != null)
            addEvent.GetRemoveMethod(true)?.Invoke(null, new object[] { addHandler });
        if (removeEvent != null && removeHandler != null)
            removeEvent.GetRemoveMethod(true)?.Invoke(null, new object[] { removeHandler });
        addEvent = removeEvent = null;
        addHandler = removeHandler = null;
    }

    [Preserve]
    static void Register(DecalProjector projector)
    {
        if (projector && !projectors.Contains(projector)) projectors.Add(projector);
    }

    [Preserve]
    static void Unregister(DecalProjector projector) => projectors.Remove(projector);

    internal static bool CanSkip(Plane[] frustum)
    {
        EnsureInitialized();
        if (!available) return false;

        for (int i = projectors.Count - 1; i >= 0; i--)
        {
            var projector = projectors[i];
            if (!projector) { projectors.RemoveAt(i); continue; }
            if (!projector.isActiveAndEnabled || !projector.material) continue;
            var bounds = WorldBounds(projector);
            // Invalid transforms must keep rendering rather than produce a false negative.
            if (!Finite(bounds.center) || !Finite(bounds.extents) || GeometryUtility.TestPlanesAABB(frustum, bounds))
                return false;
        }
        return true;
    }

    internal static Bounds WorldBounds(DecalProjector projector)
    {
        // Match DecalProjector.effectiveScale, including scale-invariant projectors on scaled parents.
        var transform = projector.transform;
        var scale = projector.scaleMode == DecalScaleMode.InheritFromHierarchy ? transform.lossyScale : Vector3.one;
        var matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
        var extents = projector.size * .5f;
        var x = matrix.MultiplyVector(new Vector3(extents.x, 0, 0));
        var y = matrix.MultiplyVector(new Vector3(0, extents.y, 0));
        var z = matrix.MultiplyVector(new Vector3(0, 0, extents.z));
        var worldExtents = new Vector3(
            Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
            Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
        // Half a metre on every side keeps the optimization conservative around frustum edges.
        return new Bounds(matrix.MultiplyPoint3x4(projector.pivot), worldExtents * 2 + Vector3.one);
    }

    static bool Finite(Vector3 value) =>
        !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
        !float.IsNaN(value.z) && !float.IsInfinity(value.z);
}
