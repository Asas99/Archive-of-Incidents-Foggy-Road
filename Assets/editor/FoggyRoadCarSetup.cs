using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FoggyRoadCarSetup
{
    private const string CarName = "Cube Car";

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

        CharacterController player = Object.FindFirstObjectByType<CharacterController>();
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

    private static Transform CreatePoint(Transform parent, string name, Vector3 localPosition)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.localPosition = localPosition;
        return point.transform;
    }
}
