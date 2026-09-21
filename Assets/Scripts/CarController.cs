using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class CarController : MonoBehaviour
{
    [Header("Enter / Exit")]
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float enterDistance = 10f;
    [SerializeField] private KeyCode enterExitKey = KeyCode.E;
    [SerializeField] private bool showHelpText = true;

    [Header("Camera Look")]
    [SerializeField] private bool allowMouseLookInCar = true;
    [SerializeField] private float mouseSensitivity = 120f;
    [SerializeField] private float minLookPitch = -75f;
    [SerializeField] private float maxLookPitch = 75f;

    [Header("Drive")]
    [Tooltip("Araba modeli yerel X ekseni boyunca uzanıyorsa W/S bu eksende ilerler.")]
    [SerializeField] private bool useLocalXAxisForForward = false;
    [SerializeField] private float acceleration = 28f;
    [SerializeField] private float reverseAcceleration = 14f;
    [SerializeField] private float turnStrength = 95f;
    [SerializeField] private float brakeStrength = 20f;
    [SerializeField] private float rollingFriction = 1.2f;
    [SerializeField] private float lateralGrip = 5f;
    [SerializeField] private float maxReverseSpeed = 12f;
    [SerializeField] private float throttleResponsiveness = 2.5f;

    [Header("Gear Switching")]
    [SerializeField] private KeyCode gearUpKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode gearDownKey = KeyCode.Q;
    [SerializeField] private float gearShiftDuration = 0.45f;
    [SerializeField] private float shiftTorqueMultiplier = 0.25f;
    [SerializeField] private GearSetting[] gears =
    {
        new GearSetting { name = "1", maxSpeed = 12f, torqueMultiplier = 1.15f },
        new GearSetting { name = "2", maxSpeed = 22f, torqueMultiplier = 0.95f },
        new GearSetting { name = "3", maxSpeed = 35f, torqueMultiplier = 0.78f },
        new GearSetting { name = "4", maxSpeed = 50f, torqueMultiplier = 0.65f }
    };

    private readonly List<MonoBehaviour> disabledDriverScripts = new List<MonoBehaviour>();
    private readonly List<Collider> disabledDriverColliders = new List<Collider>();
    private Rigidbody rb;
    private Collider vehicleCollider;
    private CharacterController driverController;
    private Transform driverTransform;
    private Transform driverCamera;
    private Transform originalDriverParent;
    private Quaternion originalDriverLocalRotation;
    private Quaternion originalCameraLocalRotation;
    private float driverLookYaw;
    private float driverLookPitch;
    private float smoothedThrottle;
    private float shiftTimer;
    private int currentGear;
    private string message = "Arabaya yaklas: E ile bin.";

    private bool HasDriver => driverController != null;
    private float SpeedKmh => rb.linearVelocity.magnitude * 3.6f;
    private Vector3 DriveForward => useLocalXAxisForForward ? transform.right : transform.forward;
    private Vector3 DriveLateral => useLocalXAxisForForward ? transform.forward : transform.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        vehicleCollider = GetComponent<Collider>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass = new Vector3(0f, -0.55f, 0f);
        // This controller steers with MoveRotation around Y.  Lock pitch and roll so a
        // road contact cannot tip the long visual model backwards or sideways.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.angularDamping = Mathf.Max(rb.angularDamping, 5f);

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

        UpdateDriverLook();

        if (Input.GetKeyDown(gearUpKey))
        {
            ShiftGear(1);
        }

        if (Input.GetKeyDown(gearDownKey))
        {
            ShiftGear(-1);
        }

        message = $"Suruyorsun | Hiz: {SpeedKmh:0} km/h | Vites: {gears[currentGear].name} | Fare ile bak | E - in";
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

        shiftTimer = Mathf.Max(0f, shiftTimer - Time.fixedDeltaTime);
        smoothedThrottle = Mathf.MoveTowards(smoothedThrottle, throttle, throttleResponsiveness * Time.fixedDeltaTime);

        ApplyDrive(smoothedThrottle);
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
        if (vehicleCollider == null)
        {
            vehicleCollider = GetComponent<Collider>();
        }

        CharacterController[] controllers = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
        CharacterController nearest = null;
        nearestDistance = float.MaxValue;

        foreach (CharacterController controller in controllers)
        {
            if (!controller.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 nearestVehiclePoint = vehicleCollider != null
                ? vehicleCollider.ClosestPoint(controller.transform.position)
                : transform.position;
            float distance = Vector3.Distance(nearestVehiclePoint, controller.transform.position);
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
        originalDriverLocalRotation = driverTransform.localRotation;
        driverCamera = FindDriverCamera(driverTransform);
        originalCameraLocalRotation = driverCamera != null ? driverCamera.localRotation : Quaternion.identity;
        driverLookYaw = 0f;
        driverLookPitch = 0f;

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
        message = "Arabaya bindin. Fare ile bak, WASD sur, Space fren, Shift/Q vites, E in.";
    }

    private void ExitCar()
    {
        if (driverCamera != null)
        {
            driverCamera.localRotation = originalCameraLocalRotation;
        }

        driverTransform.SetParent(originalDriverParent, true);
        driverTransform.SetPositionAndRotation(exitPoint.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        driverTransform.localRotation = originalDriverLocalRotation;
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
        driverCamera = null;
        originalDriverParent = null;
        smoothedThrottle = 0f;
        shiftTimer = 0f;
        message = "Arabadan indin.";
    }

    private void ShiftGear(int direction)
    {
        int nextGear = Mathf.Clamp(currentGear + direction, 0, gears.Length - 1);
        if (nextGear == currentGear)
        {
            return;
        }

        currentGear = nextGear;
        shiftTimer = gearShiftDuration;
        smoothedThrottle = Mathf.Min(smoothedThrottle, 0.25f);
    }

    private Transform FindDriverCamera(Transform driver)
    {
        Camera cameraInDriver = driver.GetComponentInChildren<Camera>(true);
        if (cameraInDriver != null)
        {
            return cameraInDriver.transform;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    private void UpdateDriverLook()
    {
        if (!allowMouseLookInCar || driverTransform == null)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        driverLookYaw += mouseX;
        driverLookPitch = Mathf.Clamp(driverLookPitch - mouseY, minLookPitch, maxLookPitch);

        driverTransform.localRotation = Quaternion.Euler(0f, driverLookYaw, 0f);

        if (driverCamera != null)
        {
            driverCamera.localRotation = Quaternion.Euler(driverLookPitch, 0f, 0f);
        }
    }

    private void ApplyDrive(float throttle)
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, DriveForward);
        GearSetting gear = gears[currentGear];

        if (throttle > 0f && forwardSpeed < gear.maxSpeed)
        {
            float speedRatio = Mathf.InverseLerp(gear.maxSpeed * 0.55f, gear.maxSpeed, Mathf.Max(0f, forwardSpeed));
            float torqueFalloff = Mathf.Lerp(1f, 0.15f, speedRatio);
            float shiftFactor = shiftTimer > 0f ? shiftTorqueMultiplier : 1f;
            float driveForce = throttle * acceleration * gear.torqueMultiplier * torqueFalloff * shiftFactor;
            rb.AddForce(DriveForward * driveForce, ForceMode.Acceleration);
        }
        else if (throttle < 0f && forwardSpeed > -maxReverseSpeed)
        {
            rb.AddForce(DriveForward * throttle * reverseAcceleration, ForceMode.Acceleration);
        }
    }

    private void ApplySteering(float steering)
    {
        // A small minimum lets A/D visibly steer the parked vehicle too.
        float speedFactor = Mathf.Max(0.15f, Mathf.Clamp01(rb.linearVelocity.magnitude / 2f));
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
        float lateralSpeed = Vector3.Dot(rb.linearVelocity, DriveLateral);
        rb.AddForce(-DriveLateral * lateralSpeed * lateralGrip, ForceMode.Acceleration);
    }

    private void OnGUI()
    {
        if (!showHelpText)
        {
            return;
        }

        GUI.Box(new Rect(20f, 20f, 470f, 78f), message);
        GUI.Label(new Rect(32f, 50f, 440f, 24f), "Kontrol: Mouse bakis, W/S gaz-geri, A/D donus, Space fren, Shift/Q vites");
    }

    [System.Serializable]
    private struct GearSetting
    {
        public string name;
        public float maxSpeed;
        public float torqueMultiplier;
    }
}
