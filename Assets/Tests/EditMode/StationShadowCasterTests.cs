using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class StationShadowCasterTests
{
    const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    readonly List<UnityEngine.Object> cleanup = new List<UnityEngine.Object>();
    MeshRenderer source, proxy;
    MeshFilter sourceFilter;
    Behaviour caster;
    Mesh mesh;

    GameObject NewObject(string name)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        cleanup.Add(go);
        return go;
    }

    Mesh NewMesh()
    {
        var result = new Mesh
        {
            vertices = new[] { Vector3.zero, new Vector3(.02f, 0, 0), new Vector3(0, .02f, .02f) },
            triangles = new[] { 0, 1, 2 }
        };
        result.RecalculateBounds();
        cleanup.Add(result);
        return result;
    }

    void Synchronize() => caster.GetType().GetMethod("Synchronize", Members).Invoke(caster, null);
    void Set(string field, object value) => caster.GetType().GetField(field, Members).SetValue(caster, value);

    [SetUp]
    public void SetUp()
    {
        var original = NewObject("Shadow source test");
        sourceFilter = original.AddComponent<MeshFilter>();
        mesh = NewMesh();
        sourceFilter.sharedMesh = mesh;
        source = original.AddComponent<MeshRenderer>();
        source.shadowCastingMode = ShadowCastingMode.On;
        var child = NewObject("Shadow proxy test");
        child.transform.SetParent(original.transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        proxy = child.AddComponent<MeshRenderer>();
        caster = (Behaviour)child.AddComponent(Type.GetType("StationShadowCaster, Assembly-CSharp", true));
        caster.GetType().GetMethod("Configure", Members).Invoke(caster, new object[] { source });
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = cleanup.Count - 1; i >= 0; i--)
            if (cleanup[i]) UnityEngine.Object.DestroyImmediate(cleanup[i]);
        cleanup.Clear();
    }

    [Test]
    public void SerializedBoundsRoundingKeepsProxyActive()
    {
        // Reproduces the ~3e-8 local-space error observed in the saved station scene.
        var rounded = mesh.bounds;
        rounded.extents += new Vector3(3.2e-8f, 0, 0);
        Set("bakedBounds", rounded);
        Synchronize();
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
        Assert.That(proxy.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));
        Assert.That(proxy.enabled, Is.True);
    }

    [Test]
    public void ActualBoundsChangeRestoresOriginalShadow()
    {
        var changed = mesh.bounds;
        changed.extents += new Vector3(.001f, 0, 0);
        mesh.bounds = changed;
        Synchronize();
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        Assert.That(proxy.enabled, Is.False);
    }

    [Test]
    public void ReplacedSourceMeshRestoresOriginalShadow()
    {
        sourceFilter.sharedMesh = NewMesh();
        Synchronize();
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        Assert.That(proxy.enabled, Is.False);
    }

    [Test]
    public void DisablingOptimizationRestoresOriginalShadow()
    {
        caster.enabled = false;
        Assert.That(source.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        Assert.That(proxy.enabled, Is.False);
    }
}
