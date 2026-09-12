using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReferencePhotoMatchV2
{
    private const string RootName = "__ReferencePhotoMatchV2";
    private const string BackupPath = "Assets/FoggyRoad/Looks/ReferencePhotoMatchV2Backup.asset";
    private const string ProfilePath = "Assets/FoggyRoad/Looks/ReferencePhotoMatchV2.asset";
    private const string OriginalProfilePath = "Assets/FoggyRoad/Looks/NightWetLook.asset";
    private const string FogMaterialPath = "Assets/FoggyRoad/Looks/ReferencePhotoFogV2.mat";
    private const string RoadMaterialPath = "Assets/FoggyRoad/Looks/ReferencePhotoRoadV2.mat";
    private const string OriginalRoadMaterialPath = "Assets/yol 1/RoadWet 1.mat";
    private const string SkyMaterialPath = "Assets/FoggyRoad/Looks/ReferencePhotoSkyV2.mat";
    private const string GuardrailMaterialPath = "Assets/FoggyRoad/Looks/ReferencePhotoGuardrailV2.mat";
    private const string EdgeLineMaterialPath = "Assets/FoggyRoad/Looks/ReferencePhotoEdgeLineV2.mat";

    [MenuItem("Tools/Foggy Road/Apply Reference Photo Match V2")]
    public static void Apply()
    {
        ReferencePhotoMatchV2Backup backup = LoadBackup();
        if (backup != null && backup.hasBackup) Revert();

        backup = CaptureBackup();
        try
        {
            ConfigureEnvironment();
            ConfigureVolume();
            ConfigureFogPass();
            ConfigureRoad();
            ConfigureCamera();
            ConfigureLights();
            EnsureRoadEdgesAndGuardrails();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Reference Photo Match V2 applied: dark blue fog, low camera, wet road, edge lines and guardrails.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogWarning("Reference Photo Match V2 stopped. Use Tools/Foggy Road/Revert Reference Photo Match V2 to restore the captured state.");
        }
    }

    [MenuItem("Tools/Foggy Road/Revert Reference Photo Match V2")]
    public static void Revert()
    {
        ReferencePhotoMatchV2Backup backup = LoadBackup();
        if (backup == null || !backup.hasBackup)
        {
            RemoveGeneratedRoot();
            return;
        }

        RestoreEnvironment(backup);
        RestoreVolume(backup);
        RestoreFogPass(backup);
        RestoreRoad(backup);
        RestoreCamera(backup);
        RestoreLights(backup);
        RemoveGeneratedRoot();
        DeleteGeneratedAssets(backup);

        backup.hasBackup = false;
        EditorUtility.SetDirty(backup);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Reference Photo Match V2 reverted.");
    }

    [MenuItem("Tools/Foggy Road/Reference Photo Match V2 Panel")]
    public static void OpenPanel()
    {
        ReferencePhotoMatchV2Window.ShowWindow();
    }

    private static ReferencePhotoMatchV2Backup LoadBackup()
    {
        return AssetDatabase.LoadAssetAtPath<ReferencePhotoMatchV2Backup>(BackupPath);
    }

    private static ReferencePhotoMatchV2Backup CaptureBackup()
    {
        ReferencePhotoMatchV2Backup backup = LoadBackup();
        if (backup == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(BackupPath) != null)
                AssetDatabase.DeleteAsset(BackupPath);
            backup = ScriptableObject.CreateInstance<ReferencePhotoMatchV2Backup>();
            AssetDatabase.CreateAsset(backup, BackupPath);
        }

        backup.hasBackup = true;
        backup.fog = RenderSettings.fog;
        backup.fogMode = RenderSettings.fogMode;
        backup.fogColor = RenderSettings.fogColor;
        backup.fogDensity = RenderSettings.fogDensity;
        backup.ambientMode = RenderSettings.ambientMode;
        backup.ambientLight = RenderSettings.ambientLight;
        backup.ambientSkyColor = RenderSettings.ambientSkyColor;
        backup.ambientEquatorColor = RenderSettings.ambientEquatorColor;
        backup.ambientGroundColor = RenderSettings.ambientGroundColor;
        backup.ambientIntensity = RenderSettings.ambientIntensity;
        backup.reflectionIntensity = RenderSettings.reflectionIntensity;
        backup.defaultReflectionMode = RenderSettings.defaultReflectionMode;
        backup.skybox = RenderSettings.skybox;
        backup.sun = RenderSettings.sun;
        backup.volume = FindVolume("Night Wet Look Volume");
        if (backup.volume != null)
        {
            backup.volumeEnabled = backup.volume.enabled;
            backup.volumeGlobal = backup.volume.isGlobal;
            backup.volumePriority = backup.volume.priority;
            backup.volumeWeight = backup.volume.weight;
            backup.volumeProfile = backup.volume.sharedProfile;
            string volumeProfilePath = backup.volumeProfile != null ? AssetDatabase.GetAssetPath(backup.volumeProfile) : string.Empty;
            if (backup.volumeProfile == null || volumeProfilePath == ProfilePath)
                backup.volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(OriginalProfilePath);
        }

        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        backup.camera = camera;
        if (camera != null) backup.cameraSnapshot = ReferencePhotoMatchV2CameraSnapshot.From(camera);

        backup.lights.Clear();
        CaptureLight(backup, "Rainy Overcast Key");
        CaptureLight(backup, "__FoggyRoadReferenceLook/ColdSkyFill");
        CaptureLight(backup, "__FoggyRoadReferenceLook/SoftFrontFill");
        CaptureLight(backup, "oyuncu/Camera/FogHeadlight_Left");
        CaptureLight(backup, "oyuncu/Camera/FogHeadlight_Right");

        backup.roadRenderer = FindRoadRenderer();
        backup.roadMaterials = backup.roadRenderer != null ? backup.roadRenderer.sharedMaterials : null;
        if (FirstMaterial(backup.roadMaterials) == null)
            backup.roadMaterials = new[] { AssetDatabase.LoadAssetAtPath<Material>(OriginalRoadMaterialPath) };
        backup.rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");
        backup.rendererPassMaterial = GetPassMaterial(backup.rendererData);
        backup.profileExisted = false;
        backup.fogMaterialExisted = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath) != null;
        backup.roadMaterialExisted = AssetDatabase.LoadAssetAtPath<Material>(RoadMaterialPath) != null;
        backup.skyMaterialExisted = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath) != null;
        backup.guardrailMaterialExisted = AssetDatabase.LoadAssetAtPath<Material>(GuardrailMaterialPath) != null;
        backup.edgeLineMaterialExisted = AssetDatabase.LoadAssetAtPath<Material>(EdgeLineMaterialPath) != null;
        EditorUtility.SetDirty(backup);
        AssetDatabase.SaveAssets();
        return backup;
    }

    private static void ConfigureEnvironment()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.19f, 0.205f, 0.23f, 1f);
        RenderSettings.reflectionIntensity = 0.30f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

        Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/FoggyRoad/Looks/Sky_NightBlue.mat");
        Material sky = CloneMaterial(source, SkyMaterialPath, "Sky_NightBlue_ReferenceV2");
        if (sky != null)
        {
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.90f);
            if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", new Color(0.46f, 0.49f, 0.55f, 1f));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }
    }

    private static void ConfigureVolume()
    {
        Volume volume = FindVolume("Night Wet Look Volume");
        if (volume == null) return;

        VolumeProfile source = volume.sharedProfile;
        if (source == null)
            source = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/FoggyRoad/Looks/NightWetLook.asset");
        VolumeProfile profile = CloneAsset(source, ProfilePath, "ReferencePhotoMatchV2");
        if (profile == null)
        {
            AssetDatabase.DeleteAsset(ProfilePath);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "ReferencePhotoMatchV2";
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ProfilePath, ImportAssetOptions.ForceSynchronousImport);
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        }
        if (profile == null) return;

        SetVolume(profile);
        volume.sharedProfile = profile;
        volume.enabled = true;
        volume.isGlobal = true;
        volume.priority = 70f;
        volume.weight = 1f;
        EditorUtility.SetDirty(volume);
    }

    private static void SetVolume(VolumeProfile profile)
    {
        Tonemapping tone = GetOrAdd<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.ACES);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.postExposure.Override(0.42f);
        color.contrast.Override(2.5f);
        color.colorFilter.Override(new Color(0.94f, 0.95f, 0.98f, 1f));
        color.saturation.Override(-14f);

        WhiteBalance white = GetOrAdd<WhiteBalance>(profile);
        white.temperature.Override(-10f);
        white.tint.Override(-4f);

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.Override(1.15f);
        bloom.intensity.Override(0.16f);
        bloom.scatter.Override(0.5f);
        bloom.highQualityFiltering.Override(true);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.color.Override(new Color(0.002f, 0.004f, 0.01f, 1f));
        vignette.center.Override(new Vector2(0.5f, 0.48f));
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.62f);

        FilmGrain grain = GetOrAdd<FilmGrain>(profile);
        grain.intensity.Override(0.05f);
        grain.response.Override(0.78f);
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component)) component = profile.Add<T>(true);
        component.active = true;
        return component;
    }

    private static void ConfigureFogPass()
    {
        Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Kullanıcı içeriği/materyaller/VolumetricFog_Night.mat");
        Material fog = CloneMaterial(source, FogMaterialPath, "ReferencePhotoFogV2");
        if (fog == null) return;

        SetFloat(fog, "_AmbientStrength", 0.64f);
        SetFloat(fog, "_BaseHeight", 4.2f);
        SetFloat(fog, "_Coverage", 0.58f);
        SetFloat(fog, "_Density", 0.025f);
        SetFloat(fog, "_GroundDensity", 0.060f);
        SetFloat(fog, "_GroundThickness", 2.6f);
        SetFloat(fog, "_FogZoneBreakup", 0.92f);
        SetFloat(fog, "_FogZoneScale", 0.012f);
        SetFloat(fog, "_HeightFalloff", 0.1f);
        SetFloat(fog, "_MaxDistance", 220f);
        SetFloat(fog, "_MaxOpacity", 0.58f);
        SetFloat(fog, "_NoiseScale", 0.03f);
        SetFloat(fog, "_NoiseStrength", 0.62f);
        SetFloat(fog, "_ShadowStrength", 0.9f);
        SetFloat(fog, "_StartDistance", 24f);
        SetFloat(fog, "_Steps", 48f);
        SetFloat(fog, "_SunScattering", 0.28f);
        SetFloat(fog, "_VolumeContrast", 1.5f);
        SetVector(fog, "_FogAreaCenter", new Vector4(420f, 0f, -35f, 0f));
        SetVector(fog, "_FogAreaSize", new Vector4(100f, 140f, 0f, 0f));
        SetFloat(fog, "_FogAreaSoftness", 18f);
        SetVector(fog, "_AreaWindDirection", new Vector4(0.72f, 0.38f, 0f, 0f));
        SetFloat(fog, "_AreaWindSpeed", 0.12f);
        SetFloat(fog, "_AreaWaveScale", 0.035f);
        SetFloat(fog, "_AreaWaveStrength", 0.82f);
        SetFloat(fog, "_AreaEdgeBreakup", 0.78f);
        SetFloat(fog, "_AreaInfluence", 0.88f);
        SetColor(fog, "_FogColor", new Color(0.24f, 0.32f, 0.40f, 1f));
        EditorUtility.SetDirty(fog);
        SetPassMaterial(backupRendererData: LoadRendererData(), material: fog);
    }

    private static void ConfigureRoad()
    {
        Renderer renderer = FindRoadRenderer();
        if (renderer == null) return;
        Material source = FirstMaterial(renderer.sharedMaterials);
        if (source == null)
            source = AssetDatabase.LoadAssetAtPath<Material>("Assets/yol 1/RoadWet 1.mat");
        Material road = CloneMaterial(source, RoadMaterialPath, "ReferencePhotoRoadV2");
        if (road == null) return;

        SetColor(road, "_BaseColor", new Color(0.0035f, 0.0042f, 0.0055f, 1f));
        SetColor(road, "_Color", new Color(0.0035f, 0.0042f, 0.0055f, 1f));
        SetFloat(road, "_Metallic", 0.02f);
        SetFloat(road, "_Smoothness", 0.86f);
        SetFloat(road, "_Glossiness", 0.86f);
        SetFloat(road, "_EnvironmentReflections", 1f);
        SetFloat(road, "_SpecularHighlights", 1f);
        SetFloat(road, "_Wetness", 0.86f);
        SetFloat(road, "_DampFilm", 0.10f);
        SetFloat(road, "_WetRoughness", 0.08f);
        SetFloat(road, "_PuddleRoughness", 0.035f);
        SetFloat(road, "_PuddleLevel", 0.70f);
        SetFloat(road, "_PuddleCoverage", 0.72f);
        SetFloat(road, "_PuddleEdge", 0.32f);
        SetFloat(road, "_PuddleRandomness", 0.88f);
        SetFloat(road, "_PuddleNoiseScale", 0.08f);
        SetFloat(road, "_PuddleFromHeight", 0.8f);
        SetFloat(road, "_WetDarkening", 0.34f);
        SetFloat(road, "_WaterFilm", 0.70f);
        SetFloat(road, "_WetNormalFlatten", 0.8f);
        SetFloat(road, "_PuddleNormalFlatten", 0.95f);
        SetColor(road, "_WetTint", new Color(0.34f, 0.36f, 0.39f, 1f));
        SetColor(road, "_ReflectionTint", new Color(0.48f, 0.52f, 0.58f, 1f));
        SetFloat(road, "_PuddleReflection", 0.58f);
        SetFloat(road, "_DarkPuddleReadability", 0.20f);
        SetFloat(road, "_ReflectionGain", 1.10f);
        SetFloat(road, "_ReflectionLimit", 0.30f);
        SetFloat(road, "_MacroStrength", 0.38f);
        SetFloat(road, "_MicroAsphaltContrast", 0.32f);
        SetFloat(road, "_FresnelBoost", 0.3f);
        EditorUtility.SetDirty(road);

        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            string name = materials[i] != null ? materials[i].name.ToLowerInvariant() : string.Empty;
            if (!name.Contains("line") && !name.Contains("paint") && !name.Contains("yellow")) materials[i] = road;
        }
        renderer.sharedMaterials = materials;
        EditorUtility.SetDirty(renderer);
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
        Vector3 position = camera.transform.position - right * 2.2f;
        position.y = 2.20f;
        camera.transform.position = position;
        Vector3 target = position + forward * 82f - right * 6.5f;
        target.y = position.y + 5.20f;
        camera.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);
        camera.fieldOfView = 54f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 520f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        camera.allowMSAA = true;

        UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
        if (data != null) data.renderPostProcessing = true;
        EditorUtility.SetDirty(camera);
    }

    private static void ConfigureLights()
    {
        SetLight("Rainy Overcast Key", 1.30f, new Color(0.62f, 0.66f, 0.72f, 1f), new Vector3(38f, -25f, 0f));
        SetLight("__FoggyRoadReferenceLook/ColdSkyFill", 0.70f, new Color(0.55f, 0.59f, 0.66f, 1f), null);
        SetLight("__FoggyRoadReferenceLook/SoftFrontFill", 0.42f, new Color(0.50f, 0.54f, 0.61f, 1f), null);
        SetLight("oyuncu/Camera/FogHeadlight_Left", 92f, new Color(0.72f, 0.82f, 1f, 1f), null);
        SetLight("oyuncu/Camera/FogHeadlight_Right", 108f, new Color(0.72f, 0.82f, 1f, 1f), null);

        Light sun = FindLight("Rainy Overcast Key");
        if (sun != null) RenderSettings.sun = sun;
    }

    private static void EnsureRoadEdgesAndGuardrails()
    {
        RemoveGeneratedRoot();
        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null) return;

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create reference photo dressing");
        Material railMaterial = EnsureMaterial(GuardrailMaterialPath, new Color(0.34f, 0.40f, 0.46f, 1f), 0.72f, 0.82f);
        Material lineMaterial = EnsureMaterial(EdgeLineMaterialPath, new Color(0.78f, 0.82f, 0.84f, 1f), 0f, 0.76f);

        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
        Vector3 start = camera.transform.position + forward * 10f;
        start.y = 0.03f;
        const int segmentCount = 12;
        const float length = 180f;
        const float roadHalfWidth = 6.5f;
        Vector3 previousCenter = start;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 center = start + forward * (length * t) + right * (18f * t * t);
            CreateSegment(root.transform, previousCenter + right * roadHalfWidth, center + right * roadHalfWidth, railMaterial, 0.95f, 0.16f, 0.18f, $"Rail_R_{i:00}");
            CreateSegment(root.transform, previousCenter - right * roadHalfWidth, center - right * roadHalfWidth, railMaterial, 0.95f, 0.16f, 0.18f, $"Rail_L_{i:00}");
            CreateSegment(root.transform, previousCenter + right * (roadHalfWidth - 0.35f), center + right * (roadHalfWidth - 0.35f), lineMaterial, 0.07f, 0.06f, 0.11f, $"Edge_R_{i:00}");
            CreateSegment(root.transform, previousCenter - right * (roadHalfWidth - 0.35f), center - right * (roadHalfWidth - 0.35f), lineMaterial, 0.07f, 0.06f, 0.11f, $"Edge_L_{i:00}");

            if (i < segmentCount)
            {
                CreatePost(root.transform, center + right * roadHalfWidth, railMaterial, $"Post_R_{i:00}");
                CreatePost(root.transform, center - right * roadHalfWidth, railMaterial, $"Post_L_{i:00}");
            }
            previousCenter = center;
        }
    }

    private static void CreateSegment(Transform parent, Vector3 from, Vector3 to, Material material, float height, float thickness, float width, string name)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(bar, "Create rail segment");
        bar.name = name;
        bar.transform.SetParent(parent);
        Vector3 delta = to - from;
        bar.transform.position = (from + to) * 0.5f + Vector3.up * height;
        bar.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        bar.transform.localScale = new Vector3(thickness, width, delta.magnitude);
        UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
        bar.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreatePost(Transform parent, Vector3 position, Material material, string name)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(post, "Create guardrail post");
        post.name = name;
        post.transform.SetParent(parent);
        post.transform.position = position + Vector3.up * 0.38f;
        post.transform.localScale = new Vector3(0.18f, 0.78f, 0.18f);
        UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
        post.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void RestoreEnvironment(ReferencePhotoMatchV2Backup backup)
    {
        RenderSettings.fog = backup.fog;
        RenderSettings.fogMode = backup.fogMode;
        RenderSettings.fogColor = backup.fogColor;
        RenderSettings.fogDensity = backup.fogDensity;
        RenderSettings.ambientMode = backup.ambientMode;
        RenderSettings.ambientLight = backup.ambientLight;
        RenderSettings.ambientSkyColor = backup.ambientSkyColor;
        RenderSettings.ambientEquatorColor = backup.ambientEquatorColor;
        RenderSettings.ambientGroundColor = backup.ambientGroundColor;
        RenderSettings.ambientIntensity = backup.ambientIntensity;
        RenderSettings.reflectionIntensity = backup.reflectionIntensity;
        RenderSettings.defaultReflectionMode = backup.defaultReflectionMode;
        RenderSettings.skybox = backup.skybox != null
            ? backup.skybox
            : AssetDatabase.LoadAssetAtPath<Material>("Assets/FoggyRoad/Looks/Sky_NightBlue.mat");
        RenderSettings.sun = backup.sun != null
            ? backup.sun
            : FindLight("Rainy Overcast Key");
    }

    private static void RestoreVolume(ReferencePhotoMatchV2Backup backup)
    {
        Volume volume = FindVolume("Night Wet Look Volume");
        if (volume == null) return;
        volume.enabled = backup.volumeEnabled;
        volume.isGlobal = backup.volumeGlobal;
        volume.priority = backup.volumePriority;
        volume.weight = backup.volumeWeight;
        VolumeProfile profile = backup.volumeProfile;
        if (profile == null || AssetDatabase.GetAssetPath(profile) == ProfilePath)
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(OriginalProfilePath);
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);
    }

    private static void RestoreFogPass(ReferencePhotoMatchV2Backup backup)
    {
        SetPassMaterial(backup.rendererData, backup.rendererPassMaterial);
    }

    private static void RestoreRoad(ReferencePhotoMatchV2Backup backup)
    {
        Renderer renderer = FindRoadRenderer();
        if (renderer == null) return;
        Material[] materials = backup.roadMaterials;
        if (FirstMaterial(materials) == null)
            materials = new[] { AssetDatabase.LoadAssetAtPath<Material>(OriginalRoadMaterialPath) };
        if (materials == null || FirstMaterial(materials) == null) return;
        renderer.sharedMaterials = materials;
        EditorUtility.SetDirty(renderer);
    }

    private static void RestoreCamera(ReferencePhotoMatchV2Backup backup)
    {
        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera != null) backup.cameraSnapshot.Apply(camera);
    }

    private static void RestoreLights(ReferencePhotoMatchV2Backup backup)
    {
        foreach (ReferencePhotoMatchV2LightSnapshot snapshot in backup.lights)
        {
            Light light = FindLight(snapshot.path);
            if (light != null) snapshot.Apply(light);
        }
    }

    private static void DeleteGeneratedAssets(ReferencePhotoMatchV2Backup backup)
    {
        if (!backup.profileExisted) AssetDatabase.DeleteAsset(ProfilePath);
        if (!backup.fogMaterialExisted) AssetDatabase.DeleteAsset(FogMaterialPath);
        if (!backup.roadMaterialExisted) AssetDatabase.DeleteAsset(RoadMaterialPath);
        if (!backup.skyMaterialExisted) AssetDatabase.DeleteAsset(SkyMaterialPath);
        if (!backup.guardrailMaterialExisted) AssetDatabase.DeleteAsset(GuardrailMaterialPath);
        if (!backup.edgeLineMaterialExisted) AssetDatabase.DeleteAsset(EdgeLineMaterialPath);
    }

    private static void RemoveGeneratedRoot()
    {
        GameObject root = GameObject.Find(RootName);
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CaptureLight(ReferencePhotoMatchV2Backup backup, string path)
    {
        Light light = FindLight(path);
        if (light != null) backup.lights.Add(ReferencePhotoMatchV2LightSnapshot.From(path, light));
    }

    private static Light FindLight(string path)
    {
        GameObject target = GameObject.Find(path);
        return target != null ? target.GetComponent<Light>() : null;
    }

    private static Volume FindVolume(string name)
    {
        GameObject target = GameObject.Find(name);
        return target != null ? target.GetComponent<Volume>() : null;
    }

    private static Renderer FindRoadRenderer()
    {
        GameObject road = GameObject.Find("yol2");
        return road != null ? road.GetComponent<Renderer>() : null;
    }

    private static ScriptableRendererData LoadRendererData()
    {
        return AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/PC_Renderer.asset");
    }

    private static Material GetPassMaterial(ScriptableRendererData data)
    {
        if (data == null) return null;
        foreach (ScriptableRendererFeature feature in data.rendererFeatures)
        {
            if (feature == null) continue;
            SerializedObject serialized = new SerializedObject(feature);
            SerializedProperty property = serialized.FindProperty("passMaterial");
            if (property != null && property.objectReferenceValue is Material material) return material;
        }
        return null;
    }

    private static void SetPassMaterial(ScriptableRendererData backupRendererData, Material material)
    {
        if (backupRendererData == null) return;
        foreach (ScriptableRendererFeature feature in backupRendererData.rendererFeatures)
        {
            if (feature == null) continue;
            SerializedObject serialized = new SerializedObject(feature);
            SerializedProperty property = serialized.FindProperty("passMaterial");
            if (property == null) continue;
            property.objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feature);
            return;
        }
    }

    private static Material FirstMaterial(Material[] materials)
    {
        if (materials == null) return null;
        foreach (Material material in materials) if (material != null) return material;
        return null;
    }

    private static Material CloneMaterial(Material source, string path, string name)
    {
        if (source == null) return null;
        AssetDatabase.DeleteAsset(path);
        Material clone = UnityEngine.Object.Instantiate(source);
        clone.name = name;
        AssetDatabase.CreateAsset(clone, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static T CloneAsset<T>(T source, string path, string name) where T : UnityEngine.Object
    {
        if (source == null) return null;
        AssetDatabase.DeleteAsset(path);
        T clone = UnityEngine.Object.Instantiate(source);
        clone.name = name;
        AssetDatabase.CreateAsset(clone, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static Material EnsureMaterial(string path, Color color, float metallic, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }
        SetColor(material, "_BaseColor", color);
        SetColor(material, "_Color", color);
        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material != null && material.HasProperty(property)) material.SetColor(property, value);
    }

    private static void SetVector(Material material, string property, Vector4 value)
    {
        if (material != null && material.HasProperty(property)) material.SetVector(property, value);
    }

    private static void SetLight(string path, float intensity, Color color, Vector3? eulerAngles)
    {
        Light light = FindLight(path);
        if (light == null) return;
        light.enabled = true;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.6f;
        if (eulerAngles.HasValue) light.transform.rotation = Quaternion.Euler(eulerAngles.Value);
        EditorUtility.SetDirty(light);
    }
}

public sealed class ReferencePhotoMatchV2Window : EditorWindow
{
    public static void ShowWindow()
    {
        ReferencePhotoMatchV2Window window = GetWindow<ReferencePhotoMatchV2Window>("Reference Match V2");
        window.minSize = new Vector2(300f, 150f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Reference Photo Match V2", EditorStyles.boldLabel);
        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Apply Reference Match", GUILayout.Height(34f))) ReferencePhotoMatchV2.Apply();
        if (GUILayout.Button("Revert / Geri Al", GUILayout.Height(34f))) ReferencePhotoMatchV2.Revert();
    }
}

[Serializable]
public sealed class ReferencePhotoMatchV2LightSnapshot
{
    public string path;
    public bool enabled;
    public Vector3 position;
    public Quaternion rotation;
    public Color color;
    public float intensity;
    public LightShadows shadows;
    public float shadowStrength;

    public static ReferencePhotoMatchV2LightSnapshot From(string path, Light light)
    {
        return new ReferencePhotoMatchV2LightSnapshot
        {
            path = path,
            enabled = light.enabled,
            position = light.transform.position,
            rotation = light.transform.rotation,
            color = light.color,
            intensity = light.intensity,
            shadows = light.shadows,
            shadowStrength = light.shadowStrength
        };
    }

    public void Apply(Light light)
    {
        light.enabled = enabled;
        light.transform.position = position;
        light.transform.rotation = rotation;
        light.color = color;
        light.intensity = intensity;
        light.shadows = shadows;
        light.shadowStrength = shadowStrength;
        EditorUtility.SetDirty(light);
    }
}

[Serializable]
public sealed class ReferencePhotoMatchV2CameraSnapshot
{
    public Vector3 position;
    public Quaternion rotation;
    public float fieldOfView;
    public float nearClipPlane;
    public float farClipPlane;
    public CameraClearFlags clearFlags;
    public bool allowHDR;
    public bool allowMSAA;
    public bool hadUrpData;
    public bool renderPostProcessing;

    public static ReferencePhotoMatchV2CameraSnapshot From(Camera camera)
    {
        UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
        return new ReferencePhotoMatchV2CameraSnapshot
        {
            position = camera.transform.position,
            rotation = camera.transform.rotation,
            fieldOfView = camera.fieldOfView,
            nearClipPlane = camera.nearClipPlane,
            farClipPlane = camera.farClipPlane,
            clearFlags = camera.clearFlags,
            allowHDR = camera.allowHDR,
            allowMSAA = camera.allowMSAA,
            hadUrpData = data != null,
            renderPostProcessing = data != null && data.renderPostProcessing
        };
    }

    public void Apply(Camera camera)
    {
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        camera.fieldOfView = fieldOfView;
        camera.nearClipPlane = nearClipPlane;
        camera.farClipPlane = farClipPlane;
        camera.clearFlags = clearFlags;
        camera.allowHDR = allowHDR;
        camera.allowMSAA = allowMSAA;
        UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
        if (data != null) data.renderPostProcessing = renderPostProcessing;
        EditorUtility.SetDirty(camera);
    }
}
