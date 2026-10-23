using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Diagnoses the gameplay camera's terrain clipping without changing foliage distances.</summary>
[InitializeOnLoad]
public static class MountainVisibilityRepair
{
    private const string Request = "Library/MountainVisibility.request";
    private const string Report = "Library/MountainVisibility.txt";

    static MountainVisibilityRepair() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        string command = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try { Run(command); }
        catch (Exception ex) { File.WriteAllText(Report, "FAIL: " + ex); Debug.LogException(ex); }
    }

    [MenuItem("Tools/Foggy Road/Repair Gameplay Mountain Visibility")]
    public static void Repair() { Run("FIX"); }

    private const string MountainLayer = "DistantMountains";

    private static MeshRenderer[] Mountains(Camera camera)
    {
        var result = new System.Collections.Generic.List<MeshRenderer>();
        foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (renderer.gameObject.scene != camera.gameObject.scene) continue;
            if (renderer.name.StartsWith("SkyboxMountains", StringComparison.OrdinalIgnoreCase) ||
                (mesh != null && mesh.name.StartsWith("SkyboxMountains", StringComparison.OrdinalIgnoreCase)))
                result.Add(renderer);
        }
        return result.ToArray();
    }

    private static int EnsureMountainLayer()
    {
        int layer = LayerMask.NameToLayer(MountainLayer);
        if (layer >= 0) return layer;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = settings.FindProperty("layers");
        for (int i = 8; i < 32; i++)
        {
            if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
            layers.GetArrayElementAtIndex(i).stringValue = MountainLayer;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return i;
        }
        throw new InvalidOperationException("No free layer for distant mountains.");
    }

    private static void Run(string command)
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
        if (player == null) throw new InvalidOperationException("No gameplay camera found.");
        Camera source = player.GetComponent<Camera>();
        MeshRenderer[] mountains = Mountains(source);
        if (mountains.Length == 0) throw new InvalidOperationException("No SkyboxMountains renderers found.");
        var report = new StringBuilder();
        report.AppendLine($"Gameplay camera={source.name}; far clip={source.farClipPlane}; mountains={mountains.Length}");
        float farDistance = 2000f;
        foreach (MeshRenderer renderer in mountains)
        {
            Bounds b = renderer.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = b.center + Vector3.Scale(b.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                farDistance = Mathf.Max(farDistance, Vector3.Distance(source.transform.position, point) + 100f);
            }
            report.AppendLine($"Mountain: {renderer.name}; layer={renderer.gameObject.layer}; centre distance={Vector3.Distance(source.transform.position, b.center)}");
        }
        farDistance = Mathf.Ceil(farDistance / 1000f) * 1000f;
        if (command == "FIX")
        {
            int layer = EnsureMountainLayer();
            foreach (MeshRenderer renderer in mountains)
            {
                Undo.RecordObject(renderer.gameObject, "Set mountain-only layer");
                renderer.gameObject.layer = layer;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);
            }
            Undo.RecordObject(source, "Keep normal gameplay view at 500 metres");
            source.farClipPlane = 500f;
            source.cullingMask &= ~(1 << layer);
            Transform existing = source.transform.Find("Distant Mountains Camera");
            GameObject go = existing != null ? existing.gameObject : new GameObject("Distant Mountains Camera");
            go.transform.SetParent(source.transform, false);
            Camera background = go.GetComponent<Camera>();
            if (background == null) background = go.AddComponent<Camera>();
            background.CopyFrom(source);
            background.cullingMask = 1 << layer;
            background.farClipPlane = farDistance;
            background.clearFlags = CameraClearFlags.Skybox;
            background.useOcclusionCulling = false;
            var sourceData = source.GetUniversalAdditionalCameraData();
            Undo.RecordObject(sourceData, "Stack gameplay over distant mountains");
            var sourceSettings = new SerializedObject(sourceData);
            sourceSettings.FindProperty("m_ClearDepth").boolValue = true;
            sourceSettings.ApplyModifiedProperties();
            var data = background.GetUniversalAdditionalCameraData();
            // Keep the dedicated mountain renderer when repairing the camera stack.
            int rendererIndex = new SerializedObject(sourceData).FindProperty("m_RendererIndex").intValue;
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var rendererList = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            var mountainRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Distant Mountains Renderer.asset");
            if (mountainRenderer != null)
                for (int i = 0; i < rendererList.arraySize; i++)
                    if (rendererList.GetArrayElementAtIndex(i).objectReferenceValue == mountainRenderer) rendererIndex = i;
            data.SetRenderer(rendererIndex);
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            var backdrop = go.GetComponent<DistantMountainBackdrop>();
            if (backdrop == null) backdrop = go.AddComponent<DistantMountainBackdrop>();
            backdrop.Configure(source);
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(sourceData);
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(backdrop);
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
            if (!EditorSceneManager.SaveScene(source.gameObject.scene)) throw new IOException("Scene save failed.");
            report.AppendLine($"PASS: Saved normal camera=500 m; mountain-only camera={farDistance} m; layer={layer}.");
        }
        else if (command == "OPTIMIZE")
        {
            var backdrop = source.GetComponentInChildren<DistantMountainBackdrop>();
            if (backdrop == null) throw new InvalidOperationException("Mountain camera is missing.");
            backdrop.Configure(source);
            EditorUtility.SetDirty(backdrop.GetComponent<Camera>());
            EditorUtility.SetDirty(backdrop.GetComponent<UniversalAdditionalCameraData>());
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
            if (!EditorSceneManager.SaveScene(source.gameObject.scene)) throw new IOException("Scene save failed.");
            Verify(source, mountains, report);
        }
        else if (command == "VERIFY") Verify(source, mountains, report);
        else if (command != "AUDIT") throw new InvalidOperationException("Unknown mountain visibility command.");
        File.WriteAllText(Report, report.ToString());
    }

    private static void Verify(Camera source, MeshRenderer[] mountains, StringBuilder report)
    {
        var backdrop = source.GetComponentInChildren<DistantMountainBackdrop>();
        if (backdrop == null) throw new InvalidOperationException("Mountain-only camera is missing.");
        Camera background = backdrop.GetComponent<Camera>();
        int layer = LayerMask.NameToLayer(MountainLayer);
        if (source.farClipPlane != 500f || background.cullingMask != (1 << layer) ||
            (source.cullingMask & (1 << layer)) != 0)
            throw new InvalidOperationException("Normal and mountain-only view limits are not isolated.");
        foreach (MeshRenderer renderer in mountains)
            if (renderer.gameObject.layer != layer) throw new InvalidOperationException("Mountain outside dedicated layer.");
        var foregroundData = source.GetUniversalAdditionalCameraData();
        var backgroundData = background.GetUniversalAdditionalCameraData();
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (renderer.gameObject.scene == source.gameObject.scene && renderer.gameObject.layer == layer &&
                Array.IndexOf(mountains, renderer as MeshRenderer) < 0)
                throw new InvalidOperationException("Non-mountain renderer on mountain layer: " + renderer.name);
        if (backgroundData.renderShadows || backgroundData.renderPostProcessing ||
            backgroundData.requiresDepthOption != CameraOverrideOption.Off || backgroundData.requiresColorOption != CameraOverrideOption.Off)
            throw new InvalidOperationException("Unnecessary mountain camera rendering enabled.");
        var pipeline = new SerializedObject(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
        int rendererIndex = new SerializedObject(backgroundData).FindProperty("m_RendererIndex").intValue;
        var rendererAsset = pipeline.FindProperty("m_RendererDataList").GetArrayElementAtIndex(rendererIndex).objectReferenceValue;
        if (new SerializedObject(rendererAsset).FindProperty("m_RendererFeatures").arraySize != 0)
            throw new InvalidOperationException("Mountain renderer still contains gameplay features.");
        backdrop.Apply();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(background);
        Bounds rear = new Bounds(source.transform.position - source.transform.forward * 2000f, Vector3.one * 100f);
        if (GeometryUtility.TestPlanesAABB(planes, rear)) throw new InvalidOperationException("Behind-camera bounds accepted.");
        report.AppendLine("PASS: mountain layer contains only mountain renderers; no gameplay renderer features, shadows, post FX or depth/color copies; rear bounds rejected by camera frustum.");
        if (foregroundData.renderType != CameraRenderType.Overlay || !foregroundData.clearDepth ||
            backgroundData.renderType != CameraRenderType.Base || !backgroundData.cameraStack.Contains(source))
            throw new InvalidOperationException("Camera stack does not preserve foreground occlusion.");
        MeshRenderer sample = Array.Find(mountains, r => r.name == "SkyboxMountains03 (2)") ?? mountains[0];
        var texture = new RenderTexture(960, 540, 24);
        Quaternion previousRotation = source.transform.rotation;
        int previousMask = background.cullingMask;
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            source.transform.LookAt(sample.bounds.center);
            backdrop.Apply();
            background.cullingMask = 0;
            Color32[] before = Capture(background, texture, "Library/MountainOnlyBefore.png");
            background.cullingMask = previousMask;
            Color32[] after = Capture(background, texture, "Library/MountainOnlyAfter.png");
            int changed = 0;
            for (int i = 0; i < before.Length; i++)
                if (Mathf.Abs(before[i].r - after[i].r) + Mathf.Abs(before[i].g - after[i].g) + Mathf.Abs(before[i].b - after[i].b) > 30) changed++;
            if (changed < 100) throw new InvalidOperationException("Mountain-only render did not visibly add mountains.");
            report.AppendLine($"PASS: camera=500 m; background mask=mountains only; depth cleared before foreground; changed pixels={changed}/{before.Length}.");
            foreach (GasStationPlanarGlass glass in UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None))
                if (glass.ResolvePlayerCamera() != source) throw new InvalidOperationException("Glass no longer resolves the gameplay camera.");
            report.AppendLine("PASS: Glass reflections still resolve the original gameplay camera.");
        }
        finally
        {
            source.transform.rotation = previousRotation;
            background.cullingMask = previousMask;
            RenderTexture.active = previousActive;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static Color32[] Capture(Camera camera, RenderTexture target, string path)
    {
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
            new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            return image.GetPixels32();
        }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
}
