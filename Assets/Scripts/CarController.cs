using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class CarController : MonoBehaviour
{
    [Header("Enter / Exit")]
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float enterDistance = 5f;
    [SerializeField] private KeyCode enterExitKey = KeyCode.E;
    [SerializeField] private bool showHelpText = true;

    [Header("Drive")]
    [SerializeField] private float acceleration = 45f;
    [SerializeField] private float reverseAcceleration = 22f;
    [SerializeField] private float turnStrength = 95f;
    [SerializeField] private float brakeStrength = 45f;
    [SerializeField] private float rollingFriction = 0.8f;
    [SerializeField] private float lateralGrip = 5f;
    [SerializeField] private float maxReverseSpeed = 12f;

    [Header("Gear Switching")]
    [SerializeField] private KeyCode gearUpKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode gearDownKey = KeyCode.Q;
    [SerializeField] private GearSetting[] gears =
    {
        new GearSetting { name = "1", maxSpeed = 11f, torqueMultiplier = 2f },
        new GearSetting { name = "2", maxSpeed = 22f, torqueMultiplier = 1.5f },
        new GearSetting { name = "3", maxSpeed = 34f, torqueMultiplier = 1.15f },
        new GearSetting { name = "4", maxSpeed = 50f, torqueMultiplier = 0.95f }
    };

    private readonly List<MonoBehaviour> disabledDriverScripts = new List<MonoBehaviour>();
    private readonly List<Collider> disabledDriverColliders = new List<Collider>();
    private Rigidbody rb;
    private CharacterController driverController;
    private Transform driverTransform;
    private Transform originalDriverParent;
    private int currentGear;
    private string message = "Arabaya yaklas: E ile bin.";

    private bool HasDriver => driverController != null;
    private float SpeedKmh => rb.linearVelocity.magnitude * 3.6f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass = new Vector3(0f, -0.55f, 0f);

        if (seatPoint == null)
        {
            GameObject seat = new GameObject("Seat Point");
            seat.transform.SetParent(transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.75f, 0.2f);
            seatPoint = seat.transform;
        }

        if (exitPoint == null)
        {
            GameObject exit = new GameObject("Exit Point");
            exit.transform.SetParent(transform, false);
            exit.transform.localPosition = new Vector3(2.2f, 0.25f, 0f);
            exitPoint = exit.transform;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(enterExitKey))
        {
            if (HasDriver)
            {
                ExitCar();
            }
            else
            {
                TryEnterCar();
            }
        }

        if (!HasDriver)
        {
            CharacterController nearest = FindNearestCharacter(out float distance);
            message = nearest == null
                ? "Sahnede CharacterController olan karakter bulunamadi."
                : distance <= enterDistance
                    ? "E - Arabaya bin"
                    : "Arabaya yaklas: E ile bin.";
            return;
        }

        if (Input.GetKeyDown(gearUpKey))
        {
            currentGear = Mathf.Min(currentGear + 1, gears.Length - 1);
        }

        if (Input.GetKeyDown(gearDownKey))
        {
            currentGear = Mathf.Max(currentGear - 1, 0);
        }

        message = $"Suruyorsun | Hiz: {SpeedKmh:0} km/h | Vites: {gears[currentGear].name} | E - in";
    }

    private void FixedUpdate()
    {
        if (!HasDriver)
        {
            ApplyRollingFriction();
            return;
        }

        float throttle = Input.GetAxis("Vertical");
        float steering = Input.GetAxis("Horizontal");
        bool brake = Input.GetKey(KeyCode.Space);

        ApplyDrive(throttle);
        ApplySteering(steering);
        ApplyLateralGrip();

        if (brake)
        {
            ApplyBrake();
        }
        else if (Mathf.Abs(throttle) < 0.05f)
        {
            ApplyRollingFriction();
        }
    }

    private void TryEnterCar()
    {
        CharacterController nearest = FindNearestCharacter(out float distance);
        if (nearest == null)
        {
            message = "Binilemedi: CharacterController olan karakter yok.";
            return;
        }

        if (distance > enterDistance)
        {
            message = $"Cok uzaksin ({distance:0.0}m). Arabaya yaklas.";
            return;
        }

        EnterCar(nearest);
    }

    private CharacterController FindNearestCharacter(out float nearestDistance)
    {
        CharacterController[] controllers = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
        CharacterController nearest = null;
        nearestDistance = float.MaxValue;

        foreach (CharacterController controller in controllers)
        {
            if (!controller.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, controller.transform.position);
            if (distance < nearestDistance)
            {
                nearest = controller;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void EnterCar(CharacterController character)
    {
        driverController = character;
        driverTransform = character.transform;
        originalDriverParent = driverTransform.parent;

        disabledDriverScripts.Clear();
        disabledDriverColliders.Clear();
        foreach (MonoBehaviour script in driverTransform.GetComponents<MonoBehaviour>())
        {
            if (script.enabled)
            {
                script.enabled = false;
                disabledDriverScripts.Add(script);
            }
        }

        foreach (Collider driverCollider in driverTransform.GetComponentsInChildren<Collider>())
        {
            if (driverCollider != driverController && driverCollider.enabled)
            {
                driverCollider.enabled = false;
                disabledDriverColliders.Add(driverCollider);
            }
        }

        driverController.enabled = false;
        driverTransform.SetParent(transform, true);
        driverTransform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
        Physics.SyncTransforms();
        message = "Arabaya bindin. WASD sur, Space fren, Shift/Q vites, E in.";
    }

    private void ExitCar()
    {
        driverTransform.SetParent(originalDriverParent, true);
        driverTransform.SetPositionAndRotation(exitPoint.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        Physics.SyncTransforms();

        foreach (Collider driverCollider in disabledDriverColliders)
        {
            if (driverCollider != null)
            {
                driverCollider.enabled = true;
            }
        }

        foreach (MonoBehaviour script in disabledDriverScripts)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }

        driverController.enabled = true;
        disabledDriverScripts.Clear();
        disabledDriverColliders.Clear();
        driverController = null;
        driverTransform = null;
        originalDriverParent = null;
        message = "Arabadan indin.";
    }

    private void ApplyDrive(float throttle)
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        GearSetting gear = gears[currentGear];

        if (throttle > 0f && forwardSpeed < gear.maxSpeed)
        {
            rb.AddForce(transform.forward * throttle * acceleration * gear.torqueMultiplier, ForceMode.Acceleration);
        }
        else if (throttle < 0f && forwardSpeed > -maxReverseSpeed)
        {
            rb.AddForce(transform.forward * throttle * reverseAcceleration, ForceMode.Acceleration);
        }
    }

    private void ApplySteering(float steering)
    {
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / 2f);
        Quaternion turn = Quaternion.Euler(0f, steering * turnStrength * speedFactor * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    private void ApplyBrake()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(-horizontalVelocity * brakeStrength, ForceMode.Acceleration);
    }

    private void ApplyRollingFriction()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(-horizontalVelocity * rollingFriction, ForceMode.Acceleration);
    }

    private void ApplyLateralGrip()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 sideVelocity = transform.right * localVelocity.x;
        rb.AddForce(-sideVelocity * lateralGrip, ForceMode.Acceleration);
    }

    private void OnGUI()
    {
        if (!showHelpText)
        {
            return;
        }

        GUI.Box(new Rect(20f, 20f, 470f, 78f), message);
        GUI.Label(new Rect(32f, 50f, 440f, 24f), "Kontrol: W/S gaz-geri, A/D donus, Space fren, Shift/Q vites");
    }

    [System.Serializable]
    private struct GearSetting
    {
        public string name;
        public float maxSpeed;
        public float torqueMultiplier;
    }
}
