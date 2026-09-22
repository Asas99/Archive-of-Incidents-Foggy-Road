using System.Collections.Generic;
using FoggyRoad.Driving;
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
    [SerializeField] private float steeringTurnSpeed = 110f;
    [SerializeField, Range(1f, 45f)] private float maxSteeringAngle = 32f;
    [SerializeField] private float parkedTurnStrength = 35f;

    [Header("Steering Visuals")]
    [SerializeField] private Transform steeringWheelVisual;
    [SerializeField] private Transform frontLeftWheelVisual;
    [SerializeField] private Transform frontRightWheelVisual;
    [SerializeField] private float steeringWheelRotationMultiplier = 12f;

    [Header("Gear Switching")]
    [SerializeField] private KeyCode gearUpKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode gearDownKey = KeyCode.Q;
    [SerializeField] private float gearShiftDuration = 0.45f;
    [SerializeField] private float shiftTorqueMultiplier = 0.25f;
    [SerializeField] private GearSetting[] gears =
    {
        new GearSetting { name = "1", minDriveSpeed = 0f, maxSpeed = 12f, torqueMultiplier = 1.15f },
        new GearSetting { name = "2", minDriveSpeed = 9.5f, maxSpeed = 22f, torqueMultiplier = 0.95f },
        new GearSetting { name = "3", minDriveSpeed = 19f, maxSpeed = 35f, torqueMultiplier = 0.78f },
        new GearSetting { name = "4", minDriveSpeed = 30f, maxSpeed = 50f, torqueMultiplier = 0.65f }
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
    private float steeringAngle;
    private float shiftTimer;
    // -1 = reverse, 0 = neutral, 1..N = forward gears.
    private int currentGear = 1;
    private string message = "Arabaya yaklas: E ile bin.";
    private Vector3 steeringWheelBasePosition;
    private Vector3 steeringWheelPivotOffset;
    private Quaternion steeringWheelBaseRotation;
    private Quaternion frontLeftWheelBaseRotation;
    private Quaternion frontRightWheelBaseRotation;

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
        ApplyLegacyGearDefaults();
        CacheSteeringVisuals();

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

        message = $"Suruyorsun | Hiz: {SpeedKmh:0} km/h | Vites: {CurrentGearName} | Direksiyon: {steeringAngle:0}° | {DriveHint} | Fare ile bak | E - in";
    }

    private void FixedUpdate()
    {
        if (!HasDriver)
        {
            ApplyRollingFriction();
            return;
        }

        float throttle = GetThrottleInput();
        float steering = GetSteeringInput();
        bool brake = Input.GetKey(KeyCode.Space);

        shiftTimer = Mathf.Max(0f, shiftTimer - Time.fixedDeltaTime);
        smoothedThrottle = Mathf.MoveTowards(smoothedThrottle, throttle, throttleResponsiveness * Time.fixedDeltaTime);
        steeringAngle = ManualTransmissionRules.UpdateSteeringAngle(
            steeringAngle,
            steering,
            steeringTurnSpeed,
            maxSteeringAngle,
            Time.fixedDeltaTime);
        ApplySteeringVisuals();

        ApplyDrive(smoothedThrottle);
        ApplySteering();
        ApplyLateralGrip();

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, DriveForward);
        bool brakingWithReverseKey = throttle < -0.05f && currentGear != -1 && forwardSpeed > 0.2f;
        if (brake || brakingWithReverseKey)
        {
            ApplyBrake();
        }
        else if (Mathf.Abs(throttle) < 0.05f || (throttle < -0.05f && currentGear != -1))
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
        message = "Arabaya bindin. W gaz, S fren; geri icin Q ile R vitesine in. Shift/Q vites, E in.";
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
        int nextGear = Mathf.Clamp(currentGear + direction, -1, gears.Length);
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

        if (throttle > 0.05f && currentGear > 0)
        {
            GearSetting gear = gears[currentGear - 1];
            if (!ManualTransmissionRules.IsForwardGearSpeedValid(forwardSpeed, gear.minDriveSpeed, gear.maxSpeed))
            {
                return;
            }

            float speedRatio = Mathf.InverseLerp(gear.maxSpeed * 0.55f, gear.maxSpeed, Mathf.Max(0f, forwardSpeed));
            float torqueFalloff = Mathf.Lerp(1f, 0.15f, speedRatio);
            float shiftFactor = shiftTimer > 0f ? shiftTorqueMultiplier : 1f;
            float driveForce = throttle * acceleration * gear.torqueMultiplier * torqueFalloff * shiftFactor;
            rb.AddForce(DriveForward * driveForce, ForceMode.Acceleration);
        }
        else if (throttle < -0.05f && currentGear == -1 && forwardSpeed > -maxReverseSpeed)
        {
            rb.AddForce(DriveForward * throttle * reverseAcceleration, ForceMode.Acceleration);
        }
    }

    private float GetThrottleInput()
    {
        return ManualTransmissionRules.ResolveDigitalAxis(
            Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
            Input.GetAxisRaw("Vertical"));
    }

    private float GetSteeringInput()
    {
        return ManualTransmissionRules.ResolveDigitalAxis(
            Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
            Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
            Input.GetAxisRaw("Horizontal"));
    }

    private void ApplyLegacyGearDefaults()
    {
        for (int gearIndex = 1; gearIndex < gears.Length; gearIndex++)
        {
            if (gears[gearIndex].minDriveSpeed > 0.01f)
            {
                continue;
            }

            GearSetting gear = gears[gearIndex];
            gear.minDriveSpeed = ManualTransmissionRules.GetDefaultMinimumDriveSpeed(gearIndex, gear.maxSpeed);
            gears[gearIndex] = gear;
        }
    }

    private void ApplySteering()
    {
        if (Mathf.Abs(steeringAngle) < 0.01f)
        {
            return;
        }

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, DriveForward);
        float effectiveTurnStrength = ManualTransmissionRules.GetTurnStrength(forwardSpeed, parkedTurnStrength, turnStrength, 4f);
        if (effectiveTurnStrength <= 0f)
        {
            return;
        }

        float reverseSteering = ManualTransmissionRules.GetSteeringDirection(forwardSpeed);
        float steeringRatio = steeringAngle / maxSteeringAngle;
        Quaternion turn = Quaternion.Euler(0f, steeringRatio * reverseSteering * effectiveTurnStrength * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    private void CacheSteeringVisuals()
    {
        steeringWheelVisual ??= transform.Find("Araba_Sahne_Model/Mesh_0.029");
        frontLeftWheelVisual ??= transform.Find("Araba_Sahne_Model/Mesh_0.025");
        frontRightWheelVisual ??= transform.Find("Araba_Sahne_Model/Mesh_0.028");

        if (steeringWheelVisual != null)
        {
            steeringWheelBasePosition = steeringWheelVisual.localPosition;
            steeringWheelBaseRotation = steeringWheelVisual.localRotation;

            MeshFilter steeringWheelMesh = steeringWheelVisual.GetComponent<MeshFilter>();
            steeringWheelPivotOffset = steeringWheelMesh != null && steeringWheelMesh.sharedMesh != null
                ? Vector3.Scale(steeringWheelMesh.sharedMesh.bounds.center, steeringWheelVisual.localScale)
                : Vector3.zero;
        }

        if (frontLeftWheelVisual != null)
        {
            frontLeftWheelBaseRotation = frontLeftWheelVisual.localRotation;
        }

        if (frontRightWheelVisual != null)
        {
            frontRightWheelBaseRotation = frontRightWheelVisual.localRotation;
        }
    }

    private void ApplySteeringVisuals()
    {
        float steeringWheelAngle = ManualTransmissionRules.GetSteeringWheelVisualAngle(
            steeringAngle,
            steeringWheelRotationMultiplier);

        if (steeringWheelVisual != null)
        {
            // Mesh_0.029 is modeled with its steering-column axis on local Y.
            Quaternion wheelRotation = steeringWheelBaseRotation * Quaternion.AngleAxis(steeringWheelAngle, Vector3.up);
            Vector3 pivotInParent = steeringWheelBasePosition + steeringWheelBaseRotation * steeringWheelPivotOffset;

            steeringWheelVisual.localRotation = wheelRotation;
            steeringWheelVisual.localPosition = pivotInParent - wheelRotation * steeringWheelPivotOffset;
        }

        if (frontLeftWheelVisual != null)
        {
            frontLeftWheelVisual.localRotation = frontLeftWheelBaseRotation * Quaternion.Euler(0f, 0f, steeringAngle);
        }

        if (frontRightWheelVisual != null)
        {
            frontRightWheelVisual.localRotation = frontRightWheelBaseRotation * Quaternion.Euler(0f, 0f, steeringAngle);
        }
    }

    private string CurrentGearName => currentGear switch
    {
        -1 => "R",
        0 => "N",
        _ => gears[currentGear - 1].name
    };

    private string DriveHint
    {
        get
        {
            if (currentGear == -1)
            {
                return "Geri vites: S ile geri git";
            }

            if (currentGear == 0)
            {
                return "Bos vites: hareket icin Shift ile 1. vitese al";
            }

            GearSetting gear = gears[currentGear - 1];
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, DriveForward);
            if (forwardSpeed < gear.minDriveSpeed)
            {
                return $"{gear.name}. vites bu hizda cekmez - Q ile vites kucult";
            }

            return "A/D direksiyon | S fren";
        }
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

        GUI.Box(new Rect(20f, 20f, 600f, 78f), message);
        GUI.Label(new Rect(32f, 50f, 560f, 24f), "Kontrol: Mouse bakis, W gaz, S fren / R'de geri, A/D donus, Space fren, Shift/Q vites");
    }

    [System.Serializable]
    private struct GearSetting
    {
        public string name;
        [Tooltip("Bu vitesin cekis vermeye basladigi minimum ileri hiz (m/s).")]
        public float minDriveSpeed;
        public float maxSpeed;
        public float torqueMultiplier;
    }
}
