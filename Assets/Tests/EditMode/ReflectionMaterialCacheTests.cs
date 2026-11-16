using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ReflectionMaterialCacheTests
{
    GameObject owner;
    MeshRenderer renderer;
    Material first, second;
    object cache;
    MethodInfo get, clear;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject("Reflection material cache test");
        renderer = owner.AddComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader, Is.Not.Null);
        first = new Material(shader);
        second = new Material(shader);
        var type = Type.GetType("ReflectionMaterialCache, Assembly-CSharp", true);
        cache = Activator.CreateInstance(type, true);
        get = type.GetMethod("Get");
        clear = type.GetMethod("Clear");
    }

    Material[] Capture(Renderer target) => (Material[])get.Invoke(cache, new object[] { target });

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(owner);
        UnityEngine.Object.DestroyImmediate(first);
        UnityEngine.Object.DestroyImmediate(second);
    }

    [Test]
    public void UnchangedSlotsReuseArrayAndKeepOriginalMaterials()
    {
        renderer.sharedMaterials = new[] { first, second };
        var original = Capture(renderer);
        Assert.That(Capture(renderer), Is.SameAs(original));
        Assert.That(original, Is.EqualTo(new[] { first, second }));
        first.color = Color.red;
        Assert.That(Capture(renderer), Is.SameAs(original));
        Assert.That(original[0].color, Is.EqualTo(Color.red));
    }

    [Test]
    public void RuntimeReorderingCreatesNewSnapshotWithoutChangingQueuedCapture()
    {
        renderer.sharedMaterials = new[] { first, second };
        var queued = Capture(renderer);
        renderer.sharedMaterials = new[] { second, first };
        var changed = Capture(renderer);
        Assert.That(changed, Is.Not.SameAs(queued));
        Assert.That(changed, Is.EqualTo(new[] { second, first }));
        Assert.That(queued, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void SlotRemovalAndNullSlotsArePreserved()
    {
        renderer.sharedMaterials = new Material[] { first, null, second };
        Assert.That(Capture(renderer), Is.EqualTo(new Material[] { first, null, second }));
        renderer.sharedMaterials = new[] { second };
        Assert.That(Capture(renderer), Is.EqualTo(new[] { second }));
        renderer.sharedMaterials = Array.Empty<Material>();
        Assert.That(Capture(renderer), Is.Empty);
    }

    [Test]
    public void DifferentRenderersDoNotShareMutableSnapshots()
    {
        var child = new GameObject("Other reflection renderer");
        child.transform.SetParent(owner.transform, false);
        var other = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[] { first };
        other.sharedMaterials = new[] { second };
        Assert.That(Capture(renderer), Is.EqualTo(new[] { first }));
        Assert.That(Capture(other), Is.EqualTo(new[] { second }));
    }

    [Test]
    public void ClearDiscardsCachedArraysAndDestroyedRenderersAreSafe()
    {
        renderer.sharedMaterials = new[] { first };
        var original = Capture(renderer);
        clear.Invoke(cache, null);
        Assert.That(Capture(renderer), Is.Not.SameAs(original));
        UnityEngine.Object.DestroyImmediate(owner);
        Assert.That(Capture(renderer), Is.Empty);
        Assert.That(Capture(null), Is.Empty);
    }
}
