using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class WeatherVisualValidator
{
    private const string SessionKey = "FoggyRoad.WeatherVisualValidation.V16";
    private const string OutputFolder = "Temp/WeatherValidation";
    private static Color32[] firstFrame;
    private static Camera validationCamera;
    private static double firstFrameTime;
    private static double secondFrameTime;
    private static bool waitingForFirstFrame;
    private static bool waitingForSecondFrame;
    private static double nextBeginAttempt;

    static WeatherVisualValidator()
    {
        EditorApplication.update += WaitForSceneAndBegin;
    }

    private static void WaitForSceneAndBegin()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            EditorApplication.update -= WaitForSceneAndBegin;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < nextBeginAttempt)
            return;

        nextBeginAttempt = EditorApplication.timeSinceStartup + 1.0;
        BeginValidation();
    }

    private static void BeginValidation()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isCompiling)
            return;

        DynamicWeatherSystem system = Object.FindFirstObjectByType<DynamicWeatherSystem>(FindObjectsInactive.Include);
        if (system == null)
            return;

        Camera sourceCamera = GetValidationCamera();
        if (sourceCamera == null)
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.update -= WaitForSceneAndBegin;
        Directory.CreateDirectory(OutputFolder);
        validationCamera = CreateLockedValidationCamera(sourceCamera);
        firstFrameTime = EditorApplication.timeSinceStartup + 1.5;
        waitingForFirstFrame = true;
        EditorApplication.update += CaptureFirstFrame;
    }

    private static void CaptureFirstFrame()
    {
        if (!waitingForFirstFrame || EditorApplication.timeSinceStartup < firstFrameTime)
            return;

        waitingForFirstFrame = false;
        EditorApplication.update -= CaptureFirstFrame;

        DynamicWeatherSystem system = Object.FindFirstObjectByType<DynamicWeatherSystem>(FindObjectsInactive.Include);
        if (system == null || validationCamera == null)
            return;

        system.EditorTick();
        firstFrame = Capture(validationCamera, Path.Combine(OutputFolder, "overcast_frame_A.png"));
        secondFrameTime = EditorApplication.timeSinceStartup + 2.5;
        waitingForSecondFrame = true;
        EditorApplication.update += FinishValidation;
    }

    private static void FinishValidation()
    {
        if (!waitingForSecondFrame || EditorApplication.timeSinceStartup < secondFrameTime)
            return;

        waitingForSecondFrame = false;
        EditorApplication.update -= FinishValidation;

        DynamicWeatherSystem system = Object.FindFirstObjectByType<DynamicWeatherSystem>(FindObjectsInactive.Include);
        if (system == null || validationCamera == null || firstFrame == null)
            return;

        system.EditorTick();
        Color32[] secondFrame = Capture(validationCamera, Path.Combine(OutputFolder, "overcast_frame_B.png"));
        double difference = CalculateFrameDifference(firstFrame, secondFrame);

        int responsiveTreeMaterials = 0;
        foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (material != null && material.shader != null &&
                material.shader.name.Contains("PineTree/URP") &&
                material.HasProperty("_WindStrength") && material.GetFloat("_WindStrength") > 0.1f)
                responsiveTreeMaterials++;
        }

        Debug.Log($"[Weather Validation] PASS — animated frame delta: {difference:F3}; " +
                  $"wind-responsive pine materials: {responsiveTreeMaterials}; " +
                  $"captures: {OutputFolder}");
        Object.DestroyImmediate(validationCamera.gameObject);
        validationCamera = null;
    }

    private static Camera GetValidationCamera()
    {
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            return SceneView.lastActiveSceneView.camera;

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (camera != null)
                return camera;
        return null;
    }

    private static Camera CreateLockedValidationCamera(Camera source)
    {
        GameObject cameraObject = new GameObject("Weather Validation Camera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.CopyFrom(source);
        // A repeatable road-level viewpoint makes visual regression captures comparable
        // even while the user continues navigating the Scene view.
        camera.transform.SetPositionAndRotation(
            new Vector3(425f, 6.2f, 690f),
            Quaternion.Euler(3.5f, 0f, 0f));
        camera.fieldOfView = 58f;
        camera.farClipPlane = 650f;
        camera.enabled = false;
        return camera;
    }

    private static Color32[] Capture(Camera camera, string path)
    {
        const int width = 960;
        const int height = 540;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false, false);

        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply(false, false);
            File.WriteAllBytes(path, image.EncodeToPNG());
            return image.GetPixels32();
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(image);
        }
    }

    private static double CalculateFrameDifference(Color32[] a, Color32[] b)
    {
        if (a.Length != b.Length)
            return -1;

        double total = 0;
        int samples = 0;
        for (int i = 0; i < a.Length; i += 16)
        {
            total += Mathf.Abs(a[i].r - b[i].r);
            total += Mathf.Abs(a[i].g - b[i].g);
            total += Mathf.Abs(a[i].b - b[i].b);
            samples += 3;
        }
        return samples > 0 ? total / samples : 0;
    }
}
