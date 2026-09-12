using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal static class DynamicWeatherInstaller
{
    private const string TargetScene = "Assets/game prototype scene.unity";
    private const string ProfilePath = "Assets/Environment/CinematicStormProfile.asset";
    private const string LegacyPostProcessProfilePath =
        "Assets/TerrainDemoScene_URP/Prefabs/Volumes/PostProcess Profile.asset";
    private const string SkyMaterialPath = "Assets/Environment/DynamicStormSkybox.mat";
    private const string SessionKey = "FoggyRoad.DynamicWeather.Installed.V5";
    private static DynamicWeatherSystem activeSystem;
    private static double nextPreviewRepaint;

    static DynamicWeatherInstaller()
    {
        // Scene lighting, Volume profiles and camera post-processing are authored
        // settings. This installer used to rewrite them after every reload, making
        // Inspector edits impossible. Weather now operates only through the existing
        // DynamicWeatherSystem and never mutates those authored settings automatically.
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Intentionally no automatic scene mutation.
    }

    private static void InstallIntoActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false))
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TargetScene)
            return;

        SessionState.SetBool(SessionKey, true);

        activeSystem = Object.FindFirstObjectByType<DynamicWeatherSystem>(FindObjectsInactive.Include);
        if (activeSystem == null)
        {
            GameObject weatherRoot = new GameObject("Dynamic Weather & Atmosphere");
            activeSystem = weatherRoot.AddComponent<DynamicWeatherSystem>();
        }

        VolumeProfile profile = LoadPreferredPostProcessProfile();
        SerializedObject weather = new SerializedObject(activeSystem);
        weather.FindProperty("postProcessProfile").objectReferenceValue = profile;
        weather.FindProperty("steadyWind").floatValue = 0.72f;
        weather.FindProperty("gustStrength").floatValue = 0.78f;
        weather.FindProperty("gustFrequency").floatValue = 0.10f;
        weather.FindProperty("turbulence").floatValue = 0.72f;
        weather.FindProperty("fogFlowSpeed").floatValue = 0.06f;
        weather.FindProperty("fogBankCycleSeconds").floatValue = 720f;
        weather.FindProperty("drizzleCycleSeconds").floatValue = 720f;
        weather.FindProperty("drizzleDurationSeconds").floatValue = 105f;
        weather.FindProperty("drizzleEmissionRate").floatValue = 28f;
        weather.FindProperty("drizzleFallSpeed").floatValue = 3.2f;
        weather.FindProperty("skyFlowSpeed").floatValue = 0.18f;
        weather.ApplyModifiedPropertiesWithoutUndo();

        Volume volume = activeSystem.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50f;
        volume.sharedProfile = profile;

        InstallDynamicSkybox();
        ConfigureEnvironmentMaterials();
        UpgradeTerrainVegetation();
        UpgradeLighting();
        EnablePostProcessingOnAllCameras();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EnsureSceneIsInBuildSettings();
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();

        Debug.Log("[Dynamic Weather] Installed: gusting wind, animated volumetric fog, " +
                  "moving storm sky, terrain wind response and cinematic URP post-processing.");
    }

    private static VolumeProfile LoadPreferredPostProcessProfile()
    {
        // Preserve the authored look of the original scene: bloom, colour curves,
        // vignette, grading and subtle chromatic aberration live in this profile.
        // Weather controls only the atmosphere; it must not overwrite this grade.
        VolumeProfile legacyProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LegacyPostProcessProfilePath);
        if (legacyProfile != null)
            return legacyProfile;

        // Retain a safe fallback for projects where the demo profile was removed.
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.92f);
        bloom.intensity.Override(0.08f);
        bloom.scatter.Override(0.55f);
        bloom.highQualityFiltering.Override(true);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(0.22f);
        color.contrast.Override(7f);
        color.saturation.Override(-11f);
        color.colorFilter.Override(new Color(0.91f, 0.95f, 1f, 1f));

        WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
        whiteBalance.active = true;
        whiteBalance.temperature.Override(-2f);
        whiteBalance.tint.Override(0f);

        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.Neutral);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        vignette.intensity.Override(0.14f);
        vignette.smoothness.Override(0.38f);
        vignette.rounded.Override(true);

        FilmGrain grain = GetOrAdd<FilmGrain>(profile);
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.035f);
        grain.response.Override(0.72f);

        LensDistortion distortion = GetOrAdd<LensDistortion>(profile);
        distortion.active = true;
        distortion.intensity.Override(-0.01f);
        distortion.scale.Override(1.004f);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
            return component;

        component = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void InstallDynamicSkybox()
    {
        Shader skyShader = Shader.Find("Environment/Dynamic Overcast Sky");
        if (skyShader == null)
        {
            Debug.LogWarning("[Dynamic Weather] Dynamic sky shader is still importing; sky setup deferred.");
            return;
        }

        Material existing = RenderSettings.skybox;
        Material dynamicSky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        if (dynamicSky == null)
        {
            dynamicSky = new Material(skyShader) { name = "DynamicStormSkybox" };
            if (existing != null && existing.HasProperty("_MainTex"))
                dynamicSky.SetTexture("_MainTex", existing.GetTexture("_MainTex"));
            dynamicSky.SetColor("_Tint", new Color(0.78f, 0.83f, 0.88f, 1f));
            dynamicSky.SetFloat("_Exposure", 0.92f);
            dynamicSky.SetFloat("_CloudMotion", 0.38f);
            dynamicSky.SetFloat("_CloudParallax", 0.025f);
            dynamicSky.SetFloat("_CloudContrast", 0.88f);
            AssetDatabase.CreateAsset(dynamicSky, SkyMaterialPath);
        }
        else if (dynamicSky.shader != skyShader)
        {
            dynamicSky.shader = skyShader;
            EditorUtility.SetDirty(dynamicSky);
        }

        dynamicSky.SetColor("_Tint", new Color(0.78f, 0.83f, 0.88f, 1f));
        dynamicSky.SetFloat("_Exposure", 0.92f);
        dynamicSky.SetFloat("_CloudMotion", 0.38f);
        dynamicSky.SetFloat("_CloudParallax", 0.025f);
        dynamicSky.SetFloat("_CloudContrast", 0.88f);
        EditorUtility.SetDirty(dynamicSky);

        Material requestedSkybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/skybox/skyboxnight.mat");
        if (requestedSkybox != null && requestedSkybox.HasProperty("_Rotation"))
        {
            // Keep the panorama seam on the back side of the road view. The texture
            // itself is imported with horizontal repeat + trilinear filtering.
            requestedSkybox.SetFloat("_Rotation", 180f);
            EditorUtility.SetDirty(requestedSkybox);
        }
        RenderSettings.skybox = requestedSkybox != null ? requestedSkybox : dynamicSky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.31f, 0.34f, 0.37f);
        RenderSettings.ambientEquatorColor = new Color(0.21f, 0.23f, 0.25f);
        RenderSettings.ambientGroundColor = new Color(0.095f, 0.10f, 0.105f);
        RenderSettings.ambientIntensity = 1.02f;
        RenderSettings.reflectionIntensity = 0.82f;
        RenderSettings.defaultReflectionResolution = 256;
        DynamicGI.UpdateEnvironment();
    }

    private static void ConfigureEnvironmentMaterials()
    {
        Material fog = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Kullanıcı içeriği/materyaller/sismat.mat");
        SetFloat(fog, "_Density", 0.00135f);
        SetFloat(fog, "_GroundDensity", 0.009f);
        SetFloat(fog, "_Coverage", 0.48f);
        SetFloat(fog, "_Erosion", 0.40f);
        SetFloat(fog, "_NoiseStrength", 0.48f);
        SetFloat(fog, "_WeatherScale", 0.0024f);
        SetFloat(fog, "_NoiseScale", 0.012f);
        SetFloat(fog, "_WindSpeed", 0.06f);
        SetFloat(fog, "_AmbientStrength", 0.66f);
        SetFloat(fog, "_SunScattering", 0.38f);
        SetFloat(fog, "_ShadowStrength", 0.55f);
        SetFloat(fog, "_MaxOpacity", 0.58f);
        SetFloat(fog, "_MaxDistance", 190f);
        SetColor(fog, "_FogColor", new Color(0.43f, 0.47f, 0.50f, 1f));

        Material road = AssetDatabase.LoadAssetAtPath<Material>("Assets/yol 1/RoadWet 1.mat");
        SetFloat(road, "_Wetness", 0.58f);
        SetFloat(road, "_DampFilm", 0.22f);
        SetFloat(road, "_WetDarkening", 0.26f);
        SetFloat(road, "_WaterFilm", 0.48f);
        SetFloat(road, "_WetRoughness", 0.18f);
        SetFloat(road, "_PuddleCoverage", 0.18f);
        SetFloat(road, "_PuddleEdge", 0.18f);
        SetFloat(road, "_PuddleFromHeight", 0.45f);
        SetFloat(road, "_PuddleRandomness", 0.35f);
        SetFloat(road, "_PuddleRoughness", 0.12f);
        SetFloat(road, "_Roughness", 0.72f);

        ConfigurePineMaterial("Assets/ağaç/M_Pine_Bark.mat", 0.30f, 0.34f, 0.075f, 0f);
        ConfigurePineMaterial("Assets/ağaç/M_Pine_Leaves.mat", 0.34f, 0.38f, 0.09f, 0.014f);
    }

    private static void ConfigurePineMaterial(string path, float wind, float gust, float branch, float flutter)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        SetFloat(material, "_WindStrength", wind);
        SetFloat(material, "_WindSpeed", path.Contains("Leaves") ? 1.10f : 1.05f);
        SetFloat(material, "_GustStrength", gust);
        SetFloat(material, "_BranchFlex", branch);
        SetFloat(material, "_LeafFlutter", flutter);
        if (material != null)
        {
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property))
        {
            material.SetFloat(property, value);
            EditorUtility.SetDirty(material);
        }
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material != null && material.HasProperty(property))
        {
            material.SetColor(property, value);
            EditorUtility.SetDirty(material);
        }
    }

    private static void UpgradeTerrainVegetation()
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            terrain.heightmapPixelError = 5f;
            terrain.detailObjectDistance = 220f;
            terrain.treeDistance = Mathf.Max(terrain.treeDistance, 1800f);
            terrain.treeBillboardDistance = 260f;
            terrain.treeCrossFadeLength = 18f;
            terrain.treeMaximumFullLODCount = 600;
            terrain.drawInstanced = true;

            TerrainData data = terrain.terrainData;
            TreePrototype[] trees = data.treePrototypes;
            for (int i = 0; i < trees.Length; i++)
            {
                trees[i].bendFactor = Mathf.Max(trees[i].bendFactor, 0.32f);
                if (trees[i].prefab == null)
                    continue;

                foreach (Renderer renderer in trees[i].prefab.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.sharedMaterials.Where(material => material != null))
                {
                    material.enableInstancing = true;
                    Debug.Log($"[Dynamic Weather] Tree prototype '{trees[i].prefab.name}' uses '{material.shader?.name}'.");
                }
            }

            data.treePrototypes = trees;
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(terrain);
        }
    }

    private static void UpgradeLighting()
    {
        Light sun = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(light => light.type == LightType.Directional && light.isActiveAndEnabled)
            .OrderByDescending(light => light.intensity)
            .FirstOrDefault();

        if (sun == null)
            return;

        RenderSettings.sun = sun;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.88f;
        sun.shadowBias = 0.035f;
        sun.shadowNormalBias = 0.28f;
        sun.color = new Color(0.72f, 0.77f, 0.82f);
        sun.intensity = 0.62f;
        EditorUtility.SetDirty(sun);
    }

    private static void EnablePostProcessingOnAllCameras()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.stopNaN = true;
            data.dithering = true;
            camera.allowHDR = true;
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(data);
        }
    }

    private static void EnsureSceneIsInBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(item => item.path == TargetScene && item.enabled))
            return;

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TargetScene, true) }
            .Concat(scenes.Where(item => item.path != TargetScene))
            .ToArray();
    }

    private static void TickScenePreview()
    {
        if (EditorApplication.isPlaying)
            return;

        if (EditorApplication.timeSinceStartup < nextPreviewRepaint)
            return;
        nextPreviewRepaint = EditorApplication.timeSinceStartup + (1.0 / 30.0);

        if (activeSystem == null)
            activeSystem = Object.FindFirstObjectByType<DynamicWeatherSystem>(FindObjectsInactive.Include);

        if (activeSystem == null)
            return;

        activeSystem.EditorTick();
        SceneView.RepaintAll();
    }
}
