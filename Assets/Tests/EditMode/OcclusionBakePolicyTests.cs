using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OcclusionBakePolicyTests
{
    readonly List<UnityEngine.Object> cleanup = new List<UnityEngine.Object>();
    static Type Policy => Type.GetType("OcclusionBakeOnly, Assembly-CSharp-Editor", true);
    static bool Check(string method, object value) => (bool)Policy.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { value });
    GameObject New(string name, Transform parent = null)
    { var go = new GameObject(name); cleanup.Add(go); if (parent) go.transform.SetParent(parent, false); return go; }
    [TearDown] public void Clean() { for (int i = cleanup.Count - 1; i >= 0; i--) if (cleanup[i]) UnityEngine.Object.DestroyImmediate(cleanup[i]); cleanup.Clear(); }
    [Test] public void DoorPanelIsDynamicButSeparateFrameIsNot()
    {
        var building = New("Building"); var hinge = New("Hinge", building.transform);
        hinge.AddComponent(Type.GetType("TwoSidedDoor, Assembly-CSharp", true));
        var panel = New("Door panel", hinge.transform); var frame = New("Door frame", building.transform);
        Assert.That(Check("IsMoving", panel.transform), Is.True); Assert.That(Check("IsMoving", frame.transform), Is.False);
    }
    [Test] public void SeparateRootGeneratorLeverIsDynamicAndBodyIsNot()
    {
        var body = New("Generator"); var lever = New("Separate lever"); var mesh = New("Lever mesh", lever.transform);
        Component manager = body.AddComponent(Type.GetType("GeneratorManager, Assembly-CSharp", true));
        manager.GetType().GetField("lever", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, lever.transform);
        Assert.That(Check("IsMoving", mesh.transform), Is.True); Assert.That(Check("IsMoving", body.transform), Is.False);
    }
    [Test] public void RigidbodyAndPickableDescendantsCannotBecomeStaticOccluders()
    {
        var physics = New("Vehicle"); physics.AddComponent<Rigidbody>().isKinematic = true;
        Assert.That(Check("IsMoving", New("Vehicle mesh", physics.transform).transform), Is.True);
        var pickup = New("Pickable"); pickup.layer = LayerMask.NameToLayer("pickable");
        Assert.That(Check("IsMoving", New("Pickable mesh", pickup.transform).transform), Is.True);
    }
    [Test] public void OpaqueWallCanOccludeButGlassAndDistantMountainCannot()
    {
        var go = New("Concrete wall"); var renderer = go.AddComponent<MeshRenderer>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); cleanup.Add(material); renderer.sharedMaterial = material;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic);
        Assert.That(Check("IsWall", renderer), Is.True);
        material.renderQueue = 3000; Assert.That(Check("IsWall", renderer), Is.False);
        material.renderQueue = 2000; go.layer = LayerMask.NameToLayer("DistantMountains");
        Assert.That(Check("IsWall", renderer), Is.False);
    }
}
