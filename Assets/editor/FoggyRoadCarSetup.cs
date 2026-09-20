using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FoggyRoadCarSetup
{
    private const string CarName = "Cube Car";
    private const string ExistingVehicleName = "Araba_Sahne";
    private const string VehiclePrefabPath = "Assets/araba/Araba_Unity/Araba_Sahne.prefab";

    [MenuItem("Tools/Foggy Road/Create Cube Car")]
    public static void CreateCubeCar()
    {
        GameObject existing = GameObject.Find(CarName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log("Cube Car already exists.");
            return;
        }

        CharacterController player = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        Vector3 spawnPosition = player != null
            ? player.transform.position + player.transform.right * 3f + Vector3.up * 0.5f
            : new Vector3(0f, 1f, 0f);

        GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
        car.name = CarName;
        car.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        car.transform.localScale = new Vector3(2.2f, 1.1f, 4f);

        Rigidbody rb = car.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 1.8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Transform seat = CreatePoint(car.transform, "Seat Point", new Vector3(0f, 0.75f, 0.2f));
        Transform exit = CreatePoint(car.transform, "Exit Point", new Vector3(2.2f, 0.25f, 0f));

        CarController controller = car.AddComponent<CarController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("seatPoint").objectReferenceValue = seat;
        serialized.FindProperty("exitPoint").objectReferenceValue = exit;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Selection.activeGameObject = car;
        Debug.Log("Created Cube Car near the player.");
    }

    [MenuItem("Tools/Foggy Road/Replace Placeholder With Araba Sahne")]
    public static void ReplacePlaceholderWithArabaSahne()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No active scene is available for the vehicle replacement.");
            return;
        }

        GameObject placeholder = GameObject.Find(CarName);
        if (placeholder == null)
        {
            Debug.LogError($"Could not find the placeholder vehicle '{CarName}'.");
            return;
        }

        GameObject vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePrefabPath);
        if (vehiclePrefab == null)
        {
            Debug.LogError($"Could not load vehicle prefab at '{VehiclePrefabPath}'.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Replace Cube Car with Araba Sahne");

        Vector3 oldPosition = placeholder.transform.position;
        Quaternion oldRotation = placeholder.transform.rotation;
        Bounds oldBounds = GetWorldBounds(placeholder);

        GameObject vehicle = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab, scene);
        Undo.RegisterCreatedObjectUndo(vehicle, "Create Araba Sahne vehicle");
        vehicle.name = "Araba Sahne Vehicle";
        vehicle.transform.SetPositionAndRotation(oldPosition, oldRotation);

        // The supplied prefab includes a presentation ground mesh.  It must not become part of
        // the drivable vehicle or its physics bounds.
        Transform presentationGround = FindChildRecursive(vehicle.transform, "PNG_Ground");
        if (presentationGround != null)
        {
            Undo.DestroyObjectImmediate(presentationGround.gameObject);
        }

        Bounds sourceBounds = GetWorldBounds(vehicle);
        if (sourceBounds.size.x <= 0f || sourceBounds.size.z <= 0f)
        {
            Debug.LogError("Araba Sahne has no renderable vehicle bounds after removing its presentation ground.");
            Undo.DestroyObjectImmediate(vehicle);
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        float footprintScale = Mathf.Min(oldBounds.size.x / sourceBounds.size.x, oldBounds.size.z / sourceBounds.size.z);
        vehicle.transform.localScale = Vector3.one * footprintScale;

        Bounds scaledBounds = GetWorldBounds(vehicle);
        vehicle.transform.position += Vector3.up * (oldBounds.min.y + 0.06f - scaledBounds.min.y);
        Bounds vehicleBounds = GetWorldBounds(vehicle);

        BoxCollider bodyCollider = vehicle.AddComponent<BoxCollider>();
        bodyCollider.center = vehicle.transform.InverseTransformPoint(vehicleBounds.center);
        bodyCollider.size = vehicle.transform.InverseTransformVector(vehicleBounds.size);

        Rigidbody body = vehicle.AddComponent<Rigidbody>();
        body.mass = 1200f;
        body.linearDamping = 0.1f;
        body.angularDamping = 1.8f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.centerOfMass = new Vector3(0f, -vehicleBounds.size.y * 0.18f, 0f);
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.angularDamping = 5f;

        Transform seat = CreatePoint(vehicle.transform, "Seat Point", new Vector3(0f, vehicleBounds.size.y * 0.35f, 0.15f));
        Transform exit = CreatePoint(vehicle.transform, "Exit Point", new Vector3(vehicleBounds.size.x * 0.65f, vehicleBounds.size.y * 0.2f, 0f));

        CarController controller = vehicle.AddComponent<CarController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("seatPoint").objectReferenceValue = seat;
        serialized.FindProperty("exitPoint").objectReferenceValue = exit;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Undo.DestroyObjectImmediate(placeholder);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = vehicle;
        Debug.Log($"Replaced '{CarName}' with '{vehicle.name}' in {scene.name}.");
    }

    [MenuItem("Tools/Foggy Road/Configure Existing Araba Sahne")]
    public static void ConfigureExistingArabaSahne()
    {
        GameObject vehicle = GameObject.Find(ExistingVehicleName);
        if (vehicle == null)
        {
            Debug.LogError($"Could not find '{ExistingVehicleName}' in the active scene.");
            return;
        }

        // Deliberately do not change vehicle.transform: the scene-authored position,
        // rotation and scale remain exactly as placed by the level designer.
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure existing Araba Sahne vehicle");

        GameObject previousReplacement = GameObject.Find("Araba Sahne Vehicle");
        if (previousReplacement != null && previousReplacement != vehicle)
        {
            Undo.DestroyObjectImmediate(previousReplacement);
        }

        Bounds vehicleBounds = GetVehicleBounds(vehicle);
        BoxCollider bodyCollider = vehicle.GetComponent<BoxCollider>();
        if (bodyCollider == null)
        {
            bodyCollider = Undo.AddComponent<BoxCollider>(vehicle);
        }

        bodyCollider.center = vehicle.transform.InverseTransformPoint(vehicleBounds.center);
        Vector3 localColliderSize = vehicle.transform.InverseTransformVector(vehicleBounds.size);
        bodyCollider.size = new Vector3(
            Mathf.Abs(localColliderSize.x),
            Mathf.Abs(localColliderSize.y),
            Mathf.Abs(localColliderSize.z));

        Rigidbody body = vehicle.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = Undo.AddComponent<Rigidbody>(vehicle);
        }

        body.mass = 1200f;
        body.linearDamping = 0.1f;
        body.angularDamping = 5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.centerOfMass = new Vector3(0f, -0.55f, 0f);
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        Transform seat = FindChildRecursive(vehicle.transform, "Seat Point");
        if (seat == null)
        {
            seat = CreatePoint(vehicle.transform, "Seat Point", bodyCollider.center + new Vector3(0f, bodyCollider.size.y * 0.35f, 0f));
        }

        Transform exit = FindChildRecursive(vehicle.transform, "Exit Point");
        if (exit == null)
        {
            exit = CreatePoint(vehicle.transform, "Exit Point", bodyCollider.center + new Vector3(bodyCollider.size.x * 0.6f, bodyCollider.size.y * 0.2f, 0f));
        }

        CarController controller = vehicle.GetComponent<CarController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<CarController>(vehicle);
        }

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("seatPoint").objectReferenceValue = seat;
        serialized.FindProperty("exitPoint").objectReferenceValue = exit;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = vehicle;
        Debug.Log($"Configured '{ExistingVehicleName}' without changing its transform.");
    }

    [MenuItem("Tools/Foggy Road/Validate Araba Sahne Enter Exit")]
    public static void ValidateExistingArabaSahneEnterExit()
    {
        GameObject vehicle = GameObject.Find(ExistingVehicleName);
        CarController controller = vehicle != null ? vehicle.GetComponent<CarController>() : null;
        if (controller == null || vehicle.GetComponent<Rigidbody>() == null || vehicle.GetComponent<Collider>() == null)
        {
            throw new InvalidOperationException("Araba_Sahne is missing CarController, Rigidbody, or Collider.");
        }

        MethodInfo enterMethod = typeof(CarController).GetMethod("TryEnterCar", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo exitMethod = typeof(CarController).GetMethod("ExitCar", BindingFlags.Instance | BindingFlags.NonPublic);
        if (enterMethod == null || exitMethod == null)
        {
            throw new MissingMethodException("CarController enter/exit methods could not be found.");
        }

        GameObject testDriver = new GameObject("Temporary Vehicle Entry Test Driver");
        try
        {
            testDriver.transform.position = vehicle.transform.position + vehicle.transform.right;
            CharacterController testController = testDriver.AddComponent<CharacterController>();

            enterMethod.Invoke(controller, null);
            if (testDriver.transform.parent != vehicle.transform || testController.enabled)
            {
                throw new InvalidOperationException("Entry flow did not attach and disable the driver.");
            }

            exitMethod.Invoke(controller, null);
            if (testDriver.transform.parent != null || !testController.enabled)
            {
                throw new InvalidOperationException("Exit flow did not restore the driver.");
            }

            Debug.Log("Araba_Sahne entry/exit validation passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testDriver);
        }
    }

    private static Bounds GetWorldBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        return bounds;
    }

    private static Bounds GetVehicleBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (FindNamedAncestor(renderer.transform, root.transform, "PNG_Ground") != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds : GetWorldBounds(root);
    }

    private static Transform FindNamedAncestor(Transform current, Transform root, string name)
    {
        while (current != null && current != root)
        {
            if (current.name == name)
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static Transform CreatePoint(Transform parent, string name, Vector3 localPosition)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.localPosition = localPosition;
        return point.transform;
    }
}
