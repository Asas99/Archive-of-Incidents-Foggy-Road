using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReferencePhotoLookPass
{
    private const string MenuPath = "Tools/Foggy Road/Push Look Toward Reference Photo";
    private const string RevertMenuPath = "Tools/Foggy Road/Revert Reference Photo Look";
    private const string BackupPath = "Assets/FoggyRoad/Looks/ReferencePhotoLookBackup.asset";
    private const string ReferenceProfilePath = "Assets/FoggyRoad/Looks/ReferencePhotoLook.asset";
    private const string GuardrailMaterialPath = "Assets/FoggyRoad/Looks/ReferenceGuardrail.mat";
    private const string FogMaterialPath = "Assets/FoggyRoad/Looks/ReferenceFogVeil.mat";
    private static readonly Color FogBlue = new Color(0.075f, 0.105f, 0.145f, 1f);
    private static readonly Color AmbientBlue = new Color(0.018f, 0.026f, 0.04f, 1f);

    [MenuItem(MenuPath)]
    public static void Apply()
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Reference Photo Look Pass");

        Bounds roadBounds;
        List<Renderer> roadRenderers = FindRoadRenderers(out roadBounds);
        Vector3 roadAxis = GetDominantRoadAxis(roadBounds);
        Vector3 sideAxis = new Vector3(-roadAxis.z, 0f, roadAxis.x).normalized;
        CaptureBackup(roadRenderers);

        ConfigureEnvironment();
        ConfigureLookVolume();
        ConfigureRoadMaterials(roadRenderers);
        Camera camera = ConfigureCamera(roadBounds, roadAxis);
        ConfigureMoonLight();
        EnsureGuardrails(roadBounds, roadAxis, sideAxis);
        EnsureLayeredFog(roadBounds, roadAxis, sideAxis, camera);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(group);
        Debug.Log("Reference photo look pass applied: darker blue grade, dense road fog, wet asphalt response, low road camera, and guardrail silhouettes.");
    }

    [MenuItem(RevertMenuPath)]
    public static void Revert()
    {
        ReferencePhotoLookBackup backup = AssetDatabase.LoadAssetAtPath<ReferencePhotoLookBackup>(BackupPath);
        if (backup == null || !backup.hasBackup)
        {
            RemoveGeneratedObjects();
            Debug.LogWarning("No reference photo look backup was found. Removed generated fog/guardrail objects only.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Revert Reference Photo Look Pass");

        RestoreRenderSettings(backup);
        RestoreCamera(backup);
        RestoreSun(backup);
        RestoreVolume(backup);
        RestoreRoadMaterials(backup);
        RemoveGeneratedObjects();
        DeleteGeneratedAssets(backup);

        backup.hasBackup = false;
        EditorUtility.SetDirty(backup);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(group);
        Debug.Log("Reference photo look pass reverted.");
    }

    [MenuItem("Tools/Foggy Road/Reference Photo Look Panel")]
    public static void OpenPanel()
    {
        ReferencePhotoLookWindow.ShowWindow();
    }

    private static void CaptureBackup(List<Renderer> roadRenderers)
    {
        ReferencePhotoLookBackup backup = AssetDatabase.LoadAssetAtPath<ReferencePhotoLookBackup>(BackupPath);
        if (backup == null)
        {
            backup = ScriptableObject.CreateInstance<ReferencePhotoLookBackup>();
            AssetDatabase.CreateAsset(backup, BackupPath);
        }

        backup.hasBackup = true;
        backup.fog = RenderSettings.fog;
        backup.fogMode = RenderSettings.fogMode;
        backup.fogColor = RenderSettings.fogColor;
        backup.fogDensity = RenderSettings.fogDensity;
        backup.ambientMode = RenderSettings.ambientMode;
        backup.ambientLight = RenderSettings.ambientLight;
        backup.reflectionIntensity = RenderSettings.reflectionIntensity;
        backup.defaultReflectionMode = RenderSettings.defaultReflectionMode;
        backup.skybox = RenderSettings.skybox;
        backup.sun = RenderSettings.sun;
        backup.sunSnapshot = RenderSettings.sun != null ? ReferencePhotoLightSnapshot.From(RenderSettings.sun) : new ReferencePhotoLightSnapshot();

        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        backup.cameraExisted = camera != null;
        backup.camera = camera;
        backup.cameraSnapshot = camera != null ? ReferencePhotoCameraSnapshot.From(camera) : new ReferencePhotoCameraSnapshot();

        Volume volume = GetReferenceVolume();
        backup.referenceVolumeExisted = volume != null;
        backup.referenceVolume = volume;
        backup.volumeSnapshot = volume != null ? ReferencePhotoVolumeSnapshot.From(volume) : new ReferencePhotoVolumeSnapshot();

        backup.referenceProfileExisted = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ReferenceProfilePath) != null;
        backup.guardrailMaterialExisted = AssetDatabase.LoadAssetAtPath<Material>(GuardrailMaterialPath) != null;
        backup.fogMaterialExisted = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath) != null;

        backup.roadMaterials.Clear();
        foreach (Renderer renderer in roadRenderers)
        {
            if (renderer == null) continue;
            backup.roadMaterials.Add(new ReferencePhotoRendererMaterials
            {
                renderer = renderer,
                materials = renderer.sharedMaterials
            });
        }

        EditorUtility.SetDirty(backup);
        AssetDatabase.SaveAssets();
    }

    private static void RestoreRenderSettings(ReferencePhotoLookBackup backup)
    {
        RenderSettings.fog = backup.fog;
        RenderSettings.fogMode = backup.fogMode;
        RenderSettings.fogColor = backup.fogColor;
        RenderSettings.fogDensity = backup.fogDensity;
        RenderSettings.ambientMode = backup.ambientMode;
        RenderSettings.ambientLight = backup.ambientLight;
        RenderSettings.reflectionIntensity = backup.reflectionIntensity;
        RenderSettings.defaultReflectionMode = backup.defaultReflectionMode;
        RenderSettings.skybox = backup.skybox;
        RenderSettings.sun = backup.sun;
    }

    private static void RestoreCamera(ReferencePhotoLookBackup backup)
    {
        if (!backup.cameraExisted)
        {
            Camera current = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (current != null && current.name == "Main Camera") Undo.DestroyObjectImmediate(current.gameObject);
            return;
        }

        if (backup.camera == null) return;
        backup.cameraSnapshot.Apply(backup.camera);
    }

    private static void RestoreSun(ReferencePhotoLookBackup backup)
    {
        if (backup.sun != null)
        {
            backup.sunSnapshot.Apply(backup.sun);
            return;
        }

        GameObject created = GameObject.Find("Cold Moon Key");
        if (created != null) Undo.DestroyObjectImmediate(created);
    }

    private static void RestoreVolume(ReferencePhotoLookBackup backup)
    {
        Volume volume = backup.referenceVolume != null ? backup.referenceVolume : GetReferenceVolume();
        if (backup.referenceVolumeExisted)
        {
            if (volume != null) backup.volumeSnapshot.Apply(volume);
            return;
        }

        if (volume != null) Undo.DestroyObjectImmediate(volume.gameObject);
    }

    private static void RestoreRoadMaterials(ReferencePhotoLookBackup backup)
    {
        foreach (ReferencePhotoRendererMaterials item in backup.roadMaterials)
        {
            if (item == null || item.renderer == null || item.materials == null) continue;
            Undo.RecordObject(item.renderer, "Restore road materials");
            item.renderer.sharedMaterials = item.materials;
            EditorUtility.SetDirty(item.renderer);
        }
    }

    private static void RemoveGeneratedObjects()
    {
        DestroyIfFound("Reference Photo Guardrails");
        DestroyIfFound("Reference Photo Fog Layers");
    }

    private static void DestroyIfFound(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null) Undo.DestroyObjectImmediate(obj);
    }

    private static void DeleteGeneratedAssets(ReferencePhotoLookBackup backup)
    {
        if (!backup.referenceProfileExisted) AssetDatabase.DeleteAsset(ReferenceProfilePath);
        if (!backup.guardrailMaterialExisted) AssetDatabase.DeleteAsset(GuardrailMaterialPath);
        if (!backup.fogMaterialExisted) AssetDatabase.DeleteAsset(FogMaterialPath);
    }

    private static Volume GetReferenceVolume()
    {
        GameObject volumeObject = GameObject.Find("Reference Photo Look Volume");
        return volumeObject != null ? volumeObject.GetComponent<Volume>() : null;
    }

    private static void ConfigureEnvironment()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = FogBlue;
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientBlue;
        RenderSettings.reflectionIntensity = 0.18f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

        Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/FoggyRoad/Looks/Sky_NightBlue.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/skybox/skyboxnight.mat");
        if (sky != null)
        {
            RenderSettings.skybox = sky;
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.28f);
            if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", new Color(0.36f, 0.45f, 0.6f, 1f));
            EditorUtility.SetDirty(sky);
        }
    }

    private static void ConfigureLookVolume()
    {
        GameObject volumeObject = GameObject.Find("Reference Photo Look Volume");
        if (volumeObject == null)
        {
            volumeObject = new GameObject("Reference Photo Look Volume");
            Undo.RegisterCreatedObjectUndo(volumeObject, "Create reference look volume");
        }

        Volume volume = volumeObject.GetComponent<Volume>();
        if (volume == null) volume = Undo.AddComponent<Volume>(volumeObject);
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 1f;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ReferenceProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ReferenceProfilePath);
        }

        volume.sharedProfile = profile;
        SetVolume(profile);
        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(volume);
    }

    private static void SetVolume(VolumeProfile profile)
    {
        Tonemapping tone = GetOrAdd<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.ACES);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.postExposure.Override(-0.85f);
        color.contrast.Override(28f);
        color.colorFilter.Override(new Color(0.62f, 0.75f, 0.98f, 1f));
        color.saturation.Override(-22f);

        WhiteBalance white = GetOrAdd<WhiteBalance>(profile);
        white.temperature.Override(-48f);
        white.tint.Override(-8f);

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.Override(0.72f);
        bloom.intensity.Override(0.42f);
        bloom.scatter.Override(0.58f);
        bloom.highQualityFiltering.Override(true);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.color.Override(new Color(0.002f, 0.004f, 0.01f, 1f));
        vignette.center.Override(new Vector2(0.5f, 0.48f));
        vignette.intensity.Override(0.34f);
        vignette.smoothness.Override(0.58f);

        FilmGrain grain = GetOrAdd<FilmGrain>(profile);
        grain.intensity.Override(0.22f);
        grain.response.Override(0.72f);

        DepthOfField depth = GetOrAdd<DepthOfField>(profile);
        depth.mode.Override(DepthOfFieldMode.Gaussian);
        depth.gaussianStart.Override(45f);
        depth.gaussianEnd.Override(135f);
        depth.gaussianMaxRadius.Override(0.85f);
        depth.highQualitySampling.Override(true);
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }

        component.active = true;
        return component;
    }

    private static Camera ConfigureCamera(Bounds roadBounds, Vector3 roadAxis)
    {
        Camera camera = Camera.main;
        if (camera == null) camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        Undo.RecordObject(camera.transform, "Frame reference road view");
        Undo.RecordObject(camera, "Tune reference camera");

        float length = RoadLength(roadBounds);
        Vector3 start = roadBounds.center - roadAxis * (length * 0.42f);
        Vector3 look = roadBounds.center + roadAxis * (length * 0.18f);
        camera.transform.position = start + Vector3.up * 1.28f;
        camera.transform.rotation = Quaternion.LookRotation((look + Vector3.up * 1.55f - camera.transform.position).normalized, Vector3.up);
        camera.fieldOfView = 61f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = Mathf.Max(350f, length * 1.8f);
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        camera.allowMSAA = true;

        UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        data.dithering = true;

        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(data);
        return camera;
    }

    private static void ConfigureMoonLight()
    {
        Light key = RenderSettings.sun;
        if (key == null)
        {
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    key = light;
                    break;
                }
            }
        }

        if (key == null)
        {
            GameObject lightObject = new GameObject("Cold Moon Key");
            Undo.RegisterCreatedObjectUndo(lightObject, "Create moon key");
            key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
        }

        Undo.RecordObject(key.transform, "Aim moon key");
        Undo.RecordObject(key, "Tune moon key");
        key.name = "Cold Moon Key";
        key.transform.rotation = Quaternion.Euler(34f, -26f, 0f);
        key.color = new Color(0.48f, 0.62f, 0.9f, 1f);
        key.intensity = 0.34f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.62f;
        RenderSettings.sun = key;
        EditorUtility.SetDirty(key);
    }

    private static void ConfigureRoadMaterials(List<Renderer> roadRenderers)
    {
        Material nightWet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Kullanıcı içeriği/materyaller/Road_NightWet.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/yol 1/RoadWet.mat");
        Material centerLine = AssetDatabase.LoadAssetAtPath<Material>("Assets/FoggyRoad/Looks/CenterLine_Reflective.mat");

        foreach (Renderer renderer in roadRenderers)
        {
            Undo.RecordObject(renderer, "Tune wet road material");
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                string name = (material != null ? material.name : string.Empty).ToLowerInvariant();
                bool line = name.Contains("line") || name.Contains("paint") || name.Contains("yellow") || name.Contains("center");
                if (line && centerLine != null)
                {
                    materials[i] = centerLine;
                    continue;
                }

                if (nightWet != null && LooksLikeRoadMaterial(material, renderer))
                {
                    materials[i] = nightWet;
                    continue;
                }

                TuneMaterial(material);
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void TuneMaterial(Material material)
    {
        if (material == null) return;
        Undo.RecordObject(material, "Tune reference material");
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.018f, 0.025f, 0.031f, 1f));
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.018f, 0.025f, 0.031f, 1f));
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.95f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.95f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.04f);
        if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 1f);
        if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 1f);
        EditorUtility.SetDirty(material);
    }

    private static void EnsureGuardrails(Bounds roadBounds, Vector3 roadAxis, Vector3 sideAxis)
    {
        if (SceneHasName("guard") || SceneHasName("bariyer") || SceneHasName("barrier")) return;

        GameObject root = new GameObject("Reference Photo Guardrails");
        Undo.RegisterCreatedObjectUndo(root, "Create guardrails");
        Material material = EnsureMaterial(GuardrailMaterialPath, new Color(0.24f, 0.3f, 0.36f, 1f), 0.68f, 0.65f);
        float length = Mathf.Min(RoadLength(roadBounds) * 0.86f, 170f);
        float shortSize = Mathf.Min(roadBounds.size.x, roadBounds.size.z);
        float sideOffset = Mathf.Clamp(shortSize * 0.42f, 4.7f, 8.4f);
        Vector3 center = roadBounds.center + Vector3.up * 0.92f;

        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 sideCenter = center + sideAxis * (sideOffset * side);
            CreateBar(root.transform, $"Rail_{side}_upper", sideCenter + Vector3.up * 0.18f, roadAxis, new Vector3(0.16f, 0.18f, length), material);
            CreateBar(root.transform, $"Rail_{side}_lower", sideCenter + Vector3.down * 0.28f, roadAxis, new Vector3(0.12f, 0.14f, length), material);

            int postCount = Mathf.Clamp(Mathf.RoundToInt(length / 7.5f), 10, 28);
            for (int i = 0; i <= postCount; i++)
            {
                float t = i / (float)postCount - 0.5f;
                Vector3 p = sideCenter + roadAxis * (t * length) + Vector3.down * 0.54f;
                CreateBar(root.transform, $"Post_{side}_{i:00}", p, roadAxis, new Vector3(0.18f, 1.08f, 0.18f), material, true);
            }
        }
    }

    private static void CreateBar(Transform parent, string name, Vector3 position, Vector3 axis, Vector3 scale, Material material, bool upright = false)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(bar, "Create guardrail segment");
        bar.name = name;
        bar.transform.SetParent(parent);
        bar.transform.position = position;
        bar.transform.rotation = upright ? Quaternion.identity : Quaternion.LookRotation(axis, Vector3.up);
        bar.transform.localScale = scale;
        Renderer renderer = bar.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
    }

    private static void EnsureLayeredFog(Bounds roadBounds, Vector3 roadAxis, Vector3 sideAxis, Camera camera)
    {
        GameObject existing = GameObject.Find("Reference Photo Fog Layers");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject root = new GameObject("Reference Photo Fog Layers");
        Undo.RegisterCreatedObjectUndo(root, "Create fog layers");
        Material material = EnsureTransparentFogMaterial();
        float length = Mathf.Min(RoadLength(roadBounds), 190f);
        float width = Mathf.Clamp(Mathf.Min(roadBounds.size.x, roadBounds.size.z) * 2.8f, 22f, 58f);
        Vector3 start = roadBounds.center - roadAxis * (length * 0.18f);

        for (int i = 0; i < 10; i++)
        {
            float t = i / 9f;
            Vector3 p = start + roadAxis * Mathf.Lerp(14f, length * 0.62f, t) + Vector3.up * Mathf.Lerp(2.0f, 5.8f, t);
            p += sideAxis * Mathf.Sin(i * 1.77f) * Mathf.Lerp(1.5f, 6.5f, t);

            GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(layer, "Create fog veil");
            layer.name = $"FogVeil_{i:00}";
            layer.transform.SetParent(root.transform);
            layer.transform.position = p;
            if (camera != null)
            {
                Vector3 toCamera = camera.transform.position - p;
                layer.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            }
            else
            {
                layer.transform.rotation = Quaternion.LookRotation(roadAxis, Vector3.up);
            }
            layer.transform.localScale = new Vector3(width * Mathf.Lerp(0.75f, 1.35f, t), Mathf.Lerp(5f, 13f, t), 1f);

            Renderer renderer = layer.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
        }
    }

    private static Material EnsureTransparentFogMaterial()
    {
        string path = FogMaterialPath;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            material = new Material(shader) { name = "ReferenceFogVeil" };
            AssetDatabase.CreateAsset(material, path);
        }

        Undo.RecordObject(material, "Tune fog veil material");
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.23f, 0.33f, 0.46f, 0.18f));
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.23f, 0.33f, 0.46f, 0.18f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.renderQueue = (int)RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material EnsureMaterial(string path, Color color, float metallic, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        Undo.RecordObject(material, "Tune reference material");
        material.name = System.IO.Path.GetFileNameWithoutExtension(path);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static List<Renderer> FindRoadRenderers(out Bounds bounds)
    {
        List<Renderer> road = new List<Renderer>();
        bounds = new Bounds(Vector3.zero, new Vector3(12f, 1f, 90f));
        bool initialized = false;

        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!LooksLikeRoadRenderer(renderer)) continue;
            road.Add(renderer);
            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!initialized)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                Vector3 size = terrain.terrainData.size;
                bounds = new Bounds(terrain.transform.position + size * 0.5f, size);
            }
        }

        return road;
    }

    private static bool LooksLikeRoadRenderer(Renderer renderer)
    {
        string n = FullName(renderer.transform).ToLowerInvariant();
        if (ContainsAny(n, "road", "yol", "asphalt", "generated_main", "paintoverlay", "centerline")) return true;
        foreach (Material material in renderer.sharedMaterials)
        {
            if (LooksLikeRoadMaterial(material, renderer)) return true;
        }
        return false;
    }

    private static bool LooksLikeRoadMaterial(Material material, Renderer renderer)
    {
        string materialName = material != null ? material.name.ToLowerInvariant() : string.Empty;
        string rendererName = renderer != null ? FullName(renderer.transform).ToLowerInvariant() : string.Empty;
        return ContainsAny(materialName + " " + rendererName, "road", "yol", "asphalt", "wetasphalt", "lane", "line", "paint");
    }

    private static bool SceneHasName(string token)
    {
        token = token.ToLowerInvariant();
        foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform.name.ToLowerInvariant().Contains(token)) return true;
        }
        return false;
    }

    private static string FullName(Transform transform)
    {
        string name = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            name = transform.name + "/" + name;
        }
        return name;
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (text.Contains(token)) return true;
        }
        return false;
    }

    private static Vector3 GetDominantRoadAxis(Bounds bounds)
    {
        return bounds.size.z >= bounds.size.x ? Vector3.forward : Vector3.right;
    }

    private static float RoadLength(Bounds bounds)
    {
        return Mathf.Max(bounds.size.x, bounds.size.z, 80f);
    }
}

public sealed class ReferencePhotoLookWindow : EditorWindow
{
    public static void ShowWindow()
    {
        ReferencePhotoLookWindow window = GetWindow<ReferencePhotoLookWindow>("Reference Look");
        window.minSize = new Vector2(260f, 110f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Reference Photo Look", EditorStyles.boldLabel);
        EditorGUILayout.Space(6f);

        if (GUILayout.Button("Apply Reference Look", GUILayout.Height(32f)))
        {
            ReferencePhotoLookPass.Apply();
        }

        if (GUILayout.Button("Revert / Geri Al", GUILayout.Height(32f)))
        {
            ReferencePhotoLookPass.Revert();
        }
    }
}

public sealed class ReferencePhotoLookBackup : ScriptableObject
{
    public bool hasBackup;
    public bool fog;
    public FogMode fogMode;
    public Color fogColor;
    public float fogDensity;
    public AmbientMode ambientMode;
    public Color ambientLight;
    public float reflectionIntensity;
    public DefaultReflectionMode defaultReflectionMode;
    public Material skybox;
    public Light sun;
    public ReferencePhotoLightSnapshot sunSnapshot = new ReferencePhotoLightSnapshot();
    public bool cameraExisted;
    public Camera camera;
    public ReferencePhotoCameraSnapshot cameraSnapshot = new ReferencePhotoCameraSnapshot();
    public bool referenceVolumeExisted;
    public Volume referenceVolume;
    public ReferencePhotoVolumeSnapshot volumeSnapshot = new ReferencePhotoVolumeSnapshot();
    public bool referenceProfileExisted;
    public bool guardrailMaterialExisted;
    public bool fogMaterialExisted;
    public List<ReferencePhotoRendererMaterials> roadMaterials = new List<ReferencePhotoRendererMaterials>();
}

[Serializable]
public sealed class ReferencePhotoRendererMaterials
{
    public Renderer renderer;
    public Material[] materials;
}

[Serializable]
public sealed class ReferencePhotoVolumeSnapshot
{
    public bool enabled;
    public bool isGlobal;
    public float priority;
    public float weight;
    public VolumeProfile profile;

    public static ReferencePhotoVolumeSnapshot From(Volume volume)
    {
        return new ReferencePhotoVolumeSnapshot
        {
            enabled = volume.enabled,
            isGlobal = volume.isGlobal,
            priority = volume.priority,
            weight = volume.weight,
            profile = volume.sharedProfile
        };
    }

    public void Apply(Volume volume)
    {
        Undo.RecordObject(volume, "Restore reference volume");
        volume.enabled = enabled;
        volume.isGlobal = isGlobal;
        volume.priority = priority;
        volume.weight = weight;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);
    }
}

[Serializable]
public sealed class ReferencePhotoLightSnapshot
{
    public string name;
    public LightType type;
    public Vector3 eulerAngles;
    public Color color;
    public float intensity;
    public LightShadows shadows;
    public float shadowStrength;

    public static ReferencePhotoLightSnapshot From(Light light)
    {
        return new ReferencePhotoLightSnapshot
        {
            name = light.name,
            type = light.type,
            eulerAngles = light.transform.eulerAngles,
            color = light.color,
            intensity = light.intensity,
            shadows = light.shadows,
            shadowStrength = light.shadowStrength
        };
    }

    public void Apply(Light light)
    {
        Undo.RecordObject(light, "Restore reference light");
        Undo.RecordObject(light.transform, "Restore reference light transform");
        light.name = name;
        light.type = type;
        light.transform.rotation = Quaternion.Euler(eulerAngles);
        light.color = color;
        light.intensity = intensity;
        light.shadows = shadows;
        light.shadowStrength = shadowStrength;
        EditorUtility.SetDirty(light);
    }
}

[Serializable]
public sealed class ReferencePhotoCameraSnapshot
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
    public AntialiasingMode antialiasing;
    public AntialiasingQuality antialiasingQuality;
    public bool dithering;

    public static ReferencePhotoCameraSnapshot From(Camera camera)
    {
        UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
        return new ReferencePhotoCameraSnapshot
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
            renderPostProcessing = data != null && data.renderPostProcessing,
            antialiasing = data != null ? data.antialiasing : AntialiasingMode.None,
            antialiasingQuality = data != null ? data.antialiasingQuality : AntialiasingQuality.High,
            dithering = data != null && data.dithering
        };
    }

    public void Apply(Camera camera)
    {
        Undo.RecordObject(camera, "Restore reference camera");
        Undo.RecordObject(camera.transform, "Restore reference camera transform");
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        camera.fieldOfView = fieldOfView;
        camera.nearClipPlane = nearClipPlane;
        camera.farClipPlane = farClipPlane;
        camera.clearFlags = clearFlags;
        camera.allowHDR = allowHDR;
        camera.allowMSAA = allowMSAA;

        UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
        if (hadUrpData)
        {
            if (data == null) data = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            data.renderPostProcessing = renderPostProcessing;
            data.antialiasing = antialiasing;
            data.antialiasingQuality = antialiasingQuality;
            data.dithering = dithering;
            EditorUtility.SetDirty(data);
        }
        else if (data != null)
        {
            Undo.DestroyObjectImmediate(data);
        }

        EditorUtility.SetDirty(camera);
    }
}
