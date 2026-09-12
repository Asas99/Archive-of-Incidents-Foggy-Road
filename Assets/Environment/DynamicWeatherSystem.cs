using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(WindZone))]
public sealed class DynamicWeatherSystem : MonoBehaviour
{
    [Header("Wind")]
    [SerializeField] private Vector2 windDirection = new Vector2(0.93f, 0.36f);
    [SerializeField, Range(0f, 2f)] private float steadyWind = 0.72f;
    [SerializeField, Range(0f, 2f)] private float gustStrength = 0.78f;
    [SerializeField, Range(0.01f, 1f)] private float gustFrequency = 0.10f;
    [SerializeField, Range(0f, 2f)] private float turbulence = 0.72f;

    [Header("Atmosphere")]
    [Tooltip("Multiplier for the already slow fog-bank advection. Keep this low so banks feel anchored to the landscape.")]
    [SerializeField, Range(0f, 0.25f)] private float fogFlowSpeed = 0.06f;
    [Tooltip("Target time for a fog bank to noticeably cross the scene. Values below 480 seconds look artificial.")]
    [SerializeField, Range(480f, 900f)] private float fogBankCycleSeconds = 720f;
    [SerializeField, Range(0f, 2f)] private float skyFlowSpeed = 0.18f;

    [Header("Intermittent drizzle")]
    [Tooltip("Time between the starts of two drizzle windows.")]
    [SerializeField, Range(300f, 1200f)] private float drizzleCycleSeconds = 720f;
    [SerializeField, Range(30f, 240f)] private float drizzleDurationSeconds = 105f;
    [SerializeField, Range(10f, 220f)] private float drizzleEmissionRate = 90f;
    [SerializeField, Range(0.5f, 8f)] private float drizzleFallSpeed = 3.2f;

    // Trees are drawn as swaying meshes out to this distance and not submitted past it.
    private const float TreeViewDistance = 350f;

    private static readonly int WeatherWindVector = Shader.PropertyToID("_WeatherWindVector");
    private static readonly int WeatherDynamics = Shader.PropertyToID("_WeatherDynamics");
    private static readonly int WeatherFogSky = Shader.PropertyToID("_WeatherFogSky");
    private WindZone windZone;
    private double editorStartTime;
    private ParticleSystem drizzleParticles;
    private Camera cachedCamera;
    private Material drizzleMaterial;
#if UNITY_EDITOR
    private double nextEditorWindPreview;
#endif

    // Shader globals survive the transition from edit mode into play mode. If this
    // component is missing or disabled in the running scene, the trees, fog and sky
    // would otherwise be driven by frozen editor values. Clearing them on load makes
    // every weather-aware shader fall back to its own material settings instead.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetWeatherGlobals()
    {
        Shader.SetGlobalVector(WeatherWindVector, Vector4.zero);
        Shader.SetGlobalVector(WeatherDynamics, Vector4.zero);
        Shader.SetGlobalVector(WeatherFogSky, Vector4.zero);
    }

    private void OnEnable()
    {
        editorStartTime = Time.realtimeSinceStartupAsDouble;
        EnsureSceneComponents();
        DisableLegacyScreenRain();
        UpdateWeather(true);
#if UNITY_EDITOR
        EditorApplication.update -= EditorWindPreview;
        EditorApplication.update += EditorWindPreview;
#endif
    }

    private void OnValidate()
    {
        windDirection = windDirection.sqrMagnitude < 0.001f ? Vector2.right : windDirection.normalized;
        UpdateWeather(true, false);
    }

    private void Update()
    {
        UpdateWeather(false);

    }

    public void EditorTick()
    {
        if (!Application.isPlaying)
            UpdateWeather(false);
    }

#if UNITY_EDITOR
    private void EditorWindPreview()
    {
        if (Application.isPlaying || EditorApplication.timeSinceStartup < nextEditorWindPreview)
            return;

        nextEditorWindPreview = EditorApplication.timeSinceStartup + 0.05;
        UpdateWeather(false);
        SceneView.RepaintAll();
    }
#endif

    private void EnsureSceneComponents()
    {
        windZone = GetComponent<WindZone>();
        if (windZone == null)
            windZone = gameObject.AddComponent<WindZone>();

        windZone.mode = WindZoneMode.Directional;

        // Two things are tuned together here.
        // 1) Billboarded terrain trees never run the SpeedTree wind vertex path, so the
        //    billboard threshold must sit beyond the visible range or the canopy freezes.
        // 2) The visible range is set by the fog, which saturates around 110 m, so drawing
        //    trees out to kilometres only burns CPU on geometry nobody can see.
        // Keeping both at the same distance means every tree you can actually see is a real
        // mesh that sways, and nothing past that is submitted at all.
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            terrain.treeDistance = TreeViewDistance;
            terrain.treeBillboardDistance = TreeViewDistance;
            terrain.treeMaximumFullLODCount = 2000;
            terrain.detailObjectDistance = 110f;
            terrain.heightmapPixelError = 10f;
        }
    }

    private void UpdateWeather(bool force, bool updateRuntimeEffects = true)
    {
        float time = Application.isPlaying
            ? Time.time
            : (float)(Time.realtimeSinceStartupAsDouble - editorStartTime);

        Vector2 direction = windDirection.normalized;
        float slowGust = Mathf.PerlinNoise(time * gustFrequency, 13.71f);
        float fastGust = Mathf.PerlinNoise(time * gustFrequency * 3.17f, 47.29f);
        float gust = Mathf.SmoothStep(0f, 1f, slowGust) * 0.72f + fastGust * 0.28f;
        float currentWind = steadyWind + gust * gustStrength;

        Shader.SetGlobalVector(WeatherWindVector,
            new Vector4(direction.x, 0f, direction.y, currentWind));
        Shader.SetGlobalVector(WeatherDynamics,
            new Vector4(currentWind, gust, time, turbulence));
        Shader.SetGlobalVector(WeatherFogSky,
            new Vector4(GetFogFlowMultiplier(), skyFlowSpeed, fogBankCycleSeconds, 0f));


        if (windZone != null)
        {
            windZone.windMain = currentWind;
            windZone.windTurbulence = turbulence;
            windZone.windPulseMagnitude = gustStrength;
            windZone.windPulseFrequency = gustFrequency;
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
        }

        if (updateRuntimeEffects)
            UpdateIntermittentDrizzle(time, direction, currentWind);

        if (force)
            DynamicGI.UpdateEnvironment();
    }

    private float GetFogFlowMultiplier()
    {
        // Metres per second: at the default 0.06 a broad bank drifts ~54 metres in
        // 15 minutes. It is intentionally independent from foliage gust strength.
        return fogFlowSpeed;
    }

    private void UpdateIntermittentDrizzle(float time, Vector2 direction, float windStrength)
    {
        if (!Application.isPlaying)
            return;

        float cycle = Mathf.Max(drizzleCycleSeconds, drizzleDurationSeconds + 1f);
        float windowTime = Mathf.Repeat(time, cycle);
        float fadeDuration = Mathf.Min(20f, drizzleDurationSeconds * 0.22f);
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, fadeDuration, windowTime));
        float fadeOut = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(drizzleDurationSeconds - fadeDuration, drizzleDurationSeconds, windowTime));
        float intensity = windowTime < drizzleDurationSeconds ? fadeIn * fadeOut : 0f;

        EnsureDrizzleParticles();
        if (drizzleParticles == null)
            return;

        // Kamerayi bir kez bul ve sakla. Main Camera pasif oldugu icin Camera.main
        // null donuyordu ve her kare tum sahne taraniyordu.
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
            if (cachedCamera == null)
                cachedCamera = FindFirstObjectByType<Camera>();
        }
        Camera camera = cachedCamera;
        if (camera == null)
            return;

        Transform particleTransform = drizzleParticles.transform;
        particleTransform.position = camera.transform.position + new Vector3(0f, 5.5f, 0f);
        particleTransform.rotation = Quaternion.identity;

        ParticleSystem.EmissionModule emission = drizzleParticles.emission;
        emission.rateOverTime = drizzleEmissionRate * intensity;
        ParticleSystem.VelocityOverLifetimeModule velocity = drizzleParticles.velocityOverLifetime;
        velocity.x = direction.x * Mathf.Min(windStrength, 1.1f) * 0.35f;
        velocity.y = -drizzleFallSpeed;
        velocity.z = direction.y * Mathf.Min(windStrength, 1.1f) * 0.35f;

        if (intensity > 0.001f && !drizzleParticles.isPlaying)
            drizzleParticles.Play();
    }

    private void EnsureDrizzleParticles()
    {
        if (drizzleParticles != null)
            return;

        Shader shader = Shader.Find("Environment/Thin Drizzle Particle");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            return;

        GameObject drizzle = new GameObject("Intermittent Exterior Drizzle")
        {
            hideFlags = HideFlags.DontSave
        };
        drizzle.transform.SetParent(transform, false);
        drizzleParticles = drizzle.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = drizzleParticles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 450;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.25f, 2.0f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.0035f, 0.007f);
        main.startColor = new Color(0.72f, 0.79f, 0.84f, 0.16f);

        ParticleSystem.EmissionModule emission = drizzleParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = drizzleParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(18f, 0.25f, 15f);

        ParticleSystem.VelocityOverLifetimeModule velocity = drizzleParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        ParticleSystemRenderer renderer = drizzleParticles.GetComponent<ParticleSystemRenderer>();
        drizzleMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        renderer.sharedMaterial = drizzleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 0.075f;
        renderer.velocityScale = 0.035f;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 0.007f;
    }

    private void DisableLegacyScreenRain()
    {
        foreach (Transform child in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (child.name == "Window raindrops")
                child.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorWindPreview;
#endif
        if (drizzleParticles != null)
            DestroyImmediate(drizzleParticles.gameObject);
        if (drizzleMaterial != null)
            DestroyImmediate(drizzleMaterial);
        drizzleParticles = null;
        drizzleMaterial = null;

    }

}
