using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Runtime gameplay is in Assembly-CSharp; a named test assembly accesses it by reflection.
public sealed class PlayerInteractionTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> objects = new List<GameObject>();
    private GameObject rig;
    private Camera camera;
    private CharacterController character;
    private Component interaction;
    private Component pickup;

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Members).Invoke(target, args);
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Members).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Members).GetValue(target);
    private static bool Flag(object target, string property) =>
        (bool)target.GetType().GetProperty(property, Members).GetValue(target);

    private GameObject NewObject(string name)
    {
        var go = new GameObject(name);
        objects.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        rig = NewObject("Interaction test player");
        rig.transform.position = new Vector3(1000f, 1000f, 1000f);
        character = rig.AddComponent<CharacterController>();
        GameObject view = NewObject("Interaction test camera");
        view.transform.SetParent(rig.transform, false);
        view.transform.localPosition = Vector3.up * 1.6f;
        camera = view.AddComponent<Camera>();
        pickup = rig.AddComponent(RuntimeType("PickupDrop"));
        Set(pickup, "playerCamera", camera);
        Set(pickup, "pickupLayers", (LayerMask)(1 << 10));
        Call(pickup, "Awake");
        interaction = view.AddComponent(RuntimeType("PlayerInteraction"));
        Set(interaction, "player", character);
        Set(interaction, "pickup", pickup);
        Call(interaction, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (interaction != null && Flag(interaction, "IsDriving"))
        {
            object vehicle = interaction.GetType().GetProperty("DrivingVehicle").GetValue(interaction);
            Call(vehicle, "ExitCar");
        }
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Physics.SyncTransforms();
    }

    private GameObject Box(float surfaceDistance = 2f, int layer = 0, float x = 0f)
    {
        var go = NewObject("Interaction test target");
        go.layer = layer;
        go.transform.position = camera.transform.position + new Vector3(x, 0f, surfaceDistance + 0.25f);
        go.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
        Physics.SyncTransforms();
        return go;
    }

    private Component Door(GameObject go)
    {
        Component door = go.AddComponent(RuntimeType("TwoSidedDoor"));
        Call(door, "Awake");
        return door;
    }

    private void Input(bool e = true, bool click = false)
    {
        Physics.SyncTransforms();
        Call(interaction, "ProcessInteraction", e, click);
    }

    [TestCase(3.9f, true)]
    [TestCase(4f, true)]
    [TestCase(4.1f, false)]
    public void Door_UsesFourMetresFromCameraToSurface(float distance, bool opens)
    {
        Component door = Door(Box(distance));
        Input();
        Assert.That(Get<bool>(door, "isOpen"), Is.EqualTo(opens));
    }

    [Test]
    public void OffCentreDoor_DoesNotOpen()
    {
        Component door = Door(Box(2f, 0, 1f));
        Input();
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
    }

    [Test]
    public void Wall_BlocksDoorEvenOnNonPickupLayer()
    {
        Component door = Door(Box(3f, 10));
        Box(1f, 0);
        Input();
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
        Assert.That(Flag(pickup, "IsHolding"), Is.False);
    }

    [Test]
    public void Door_SupportsLeftClickAndOnlyTogglesOnceWithBothInputs()
    {
        Component door = Door(Box());
        Input(false, true);
        Assert.That(Get<bool>(door, "isOpen"), Is.True);
        Input(true, true);
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
    }

    [TestCase(1f, -1f)]
    [TestCase(-1f, 1f)]
    public void Door_OpensAwayFromEitherSide(float side, float expectedSign)
    {
        GameObject go = Box();
        Component door = Door(go);
        Call(door, "Interact", go.transform.position + go.transform.up * side, camera.transform.forward);
        Assert.That(Mathf.Sign(Get<float>(door, "openedAngle")), Is.EqualTo(expectedSign));
        Call(door, "Interact", go.transform.position, camera.transform.forward);
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
    }

    [Test]
    public void Generator_RecognizesSeparateLeverAndUpdatesLights()
    {
        GameObject lever = Box();
        GameObject root = NewObject("Separate generator manager");
        Component generator = root.AddComponent(RuntimeType("GeneratorManager"));
        Light green = NewObject("green").AddComponent<Light>();
        Light red = NewObject("red").AddComponent<Light>();
        Set(generator, "lever", lever.transform);
        Set(generator, "greenSpotlight", green);
        Set(generator, "redSpotlight", red);
        Call(generator, "Awake");
        Call(generator, "OnEnable");
        Input(false, true);
        Assert.That(Get<bool>(generator, "isOn"), Is.True);
        Assert.That(green.enabled && !red.enabled, Is.True);
        Input();
        Assert.That(Get<bool>(generator, "isOn"), Is.False);
        Assert.That(!green.enabled && red.enabled, Is.True);
    }

    [Test]
    public void Pickup_RespectsLayerAndPhysicalOcclusion()
    {
        GameObject item = Box(3f, 10);
        item.AddComponent<Rigidbody>();
        GameObject wall = Box(1f, 0);
        Input();
        Assert.That(Flag(pickup, "IsHolding"), Is.False);
        UnityEngine.Object.DestroyImmediate(wall);
        Input();
        Assert.That(Flag(pickup, "IsHolding"), Is.True);
        Input();
        Assert.That(Flag(pickup, "IsHolding"), Is.False);
        Assert.That(item.GetComponent<Rigidbody>().useGravity, Is.True);
        Assert.That(item.GetComponent<Rigidbody>().isKinematic, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HeldItem_DropOrThrowDoesNotAlsoOpenDoor(bool throwItem)
    {
        GameObject item = Box(1f, 10);
        item.AddComponent<Rigidbody>();
        Input();
        Component door = Door(Box(3f));
        Input(!throwItem, throwItem);
        Assert.That(Flag(pickup, "IsHolding"), Is.False);
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
    }

    [Test]
    public void DoorWithRigidbody_IsNotPickedUp()
    {
        GameObject target = Box(2f, 10);
        target.AddComponent<Rigidbody>();
        Component door = Door(target);
        Input();
        Assert.That(Get<bool>(door, "isOpen"), Is.True);
        Assert.That(Flag(pickup, "IsHolding"), Is.False);
    }

    private Component DoorWithPartialCollider(float surfaceDistance)
    {
        GameObject target = Box(surfaceDistance, 0, 0.8f);
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objects.Add(panel);
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.transform.SetParent(target.transform, false);
        panel.transform.localScale = new Vector3(2f, 0.5f, 0.5f);
        return Door(target);
    }

    [TestCase(2f, true)]
    [TestCase(4f, true)]
    [TestCase(4.1f, false)]
    public void VisibleDoorEdge_OutsidePhysicalCollider_UsesSameRange(float distance, bool opens)
    {
        Component door = DoorWithPartialCollider(distance);
        Input();
        Assert.That(Get<bool>(door, "isOpen"), Is.EqualTo(opens));
        Assert.That(Flag(pickup, "IsHolding"), Is.False);
    }

    [Test]
    public void VisibleDoorEdge_IsStillBlockedByWall()
    {
        Component door = DoorWithPartialCollider(3f);
        Box(1f);
        Input();
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
    }

    [Test]
    public void VisibleDoorEdge_TracksRotatingPanelAndCanCloseWithLeftClick()
    {
        Component door = DoorWithPartialCollider(2f);
        Input();
        Assert.That(Get<bool>(door, "isOpen"), Is.True);
        door.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        camera.transform.LookAt(door.transform.TransformPoint(new Vector3(-0.8f, 0f, 0f)));
        Input(false, true);
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
    }

    [Test]
    public void TargetedCar_EntersAndExitsWithoutActivatingDoorOrOtherCar()
    {
        GameObject target = Box(2f, 10);
        target.AddComponent<Rigidbody>();
        Component car = target.AddComponent(RuntimeType("CarController"));
        Call(car, "Awake");
        GameObject other = Box(2f, 10, 2f);
        other.AddComponent<Rigidbody>();
        Component otherCar = other.AddComponent(RuntimeType("CarController"));
        Call(otherCar, "Awake");
        Input();
        Assert.That(Flag(car, "HasDriver"), Is.True);
        Assert.That(Flag(otherCar, "HasDriver"), Is.False);
        Assert.That(Flag(pickup, "IsHolding"), Is.False);
        Assert.That(character.enabled, Is.False);
        Assert.That(Flag(interaction, "IsReticleVisible"), Is.False);
        Component door = Door(Box(2f));
        Input(false, true);
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
        Input();
        Assert.That(Flag(car, "HasDriver"), Is.False);
        Assert.That(character.enabled, Is.True);
        Assert.That(Get<bool>(door, "isOpen"), Is.False);
        Assert.That(Flag(interaction, "IsReticleVisible"), Is.True);
    }

    [TestCase(689, 387)]
    [TestCase(1920, 1080)]
    [TestCase(2560, 1440)]
    public void Dot_RemainsCentredAndVisibleInSmallAndLargeViews(int width, int height)
    {
        Rect rect = (Rect)Call(interaction, "GetReticleRect", width, height);
        Assert.That(rect.center.x, Is.EqualTo(width * 0.5f));
        Assert.That(rect.center.y, Is.EqualTo(height * 0.5f));
        Assert.That(rect.width, Is.GreaterThanOrEqualTo(2f));
        if (height == 1080) Assert.That(rect.width, Is.EqualTo(3f));
        Assert.That(rect.width, Is.EqualTo(rect.height));
        Assert.That(Get<float>(interaction, "dotOpacity"), Is.EqualTo(0.75f));
        Assert.That(Flag(interaction, "IsReticleVisible"), Is.True);
        Assert.That(Get<Texture2D>(interaction, "dotTexture"), Is.Not.Null);
    }

    [Test]
    public void NativeDotColorOpacityAndSize_AreNotOverwrittenByGameplay()
    {
        Component dot = Get<Component>(interaction, "reticleDot");
        PropertyInfo color = dot.GetType().GetProperty("color");
        Color customColor = new Color(0.2f, 0.8f, 0.4f, 0.35f);
        color.SetValue(dot, customColor);
        RectTransform rect = dot.GetComponent<RectTransform>();
        rect.sizeDelta = Vector2.one * 5f;
        Call(interaction, "RefreshReticle");
        Assert.That((Color)color.GetValue(dot), Is.EqualTo(customColor));
        Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.one * 5f));
    }

    [Test]
    public void MountainCamera_OnlyIncludesMountainsAndTracksTheForwardFrustum()
    {
        var sky = NewObject("Mountain frustum test");
        sky.transform.SetParent(camera.transform, false);
        Camera skyCamera = sky.AddComponent<Camera>();
        skyCamera.farClipPlane = 10000f;
        Component backdrop = sky.AddComponent(RuntimeType("DistantMountainBackdrop"));
        Call(backdrop, "Configure", camera);
        int mask = 1 << LayerMask.NameToLayer("DistantMountains");
        Assert.That(skyCamera.cullingMask, Is.EqualTo(mask));
        Assert.That(camera.cullingMask & mask, Is.Zero);
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        {
            camera.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Call(backdrop, "Apply");
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(skyCamera);
            Vector3 direction = camera.transform.forward * 2000f;
            Assert.That(GeometryUtility.TestPlanesAABB(planes, new Bounds(camera.transform.position + direction, Vector3.one * 100f)), Is.True);
            Assert.That(GeometryUtility.TestPlanesAABB(planes, new Bounds(camera.transform.position - direction, Vector3.one * 100f)), Is.False);
        }
    }

    [Test]
    public void FarGrass_DoesNotRenderForTheMountainOnlyCamera()
    {
        var profile = ScriptableObject.CreateInstance(RuntimeType("SceneOptimizationProfile"));
        try
        {
            var go = NewObject("Grass camera filter test");
            Component grass = go.AddComponent(RuntimeType("FarGrassRenderer"));
            Set(grass, "profile", profile);
            Assert.That((bool)Call(grass, "Accepts", camera), Is.True);
            var sky = NewObject("Mountain-only camera");
            Camera skyCamera = sky.AddComponent<Camera>();
            sky.AddComponent(RuntimeType("DistantMountainBackdrop"));
            Assert.That((bool)Call(grass, "Accepts", skyCamera), Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(profile); }
    }
}
