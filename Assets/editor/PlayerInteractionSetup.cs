using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Installs the interaction component without replacing the player's existing setup.</summary>
[InitializeOnLoad]
public static class PlayerInteractionSetup
{
    private const string Request = "Library/PlayerInteractionSetup.request";
    private const string Result = "Library/PlayerInteractionSetup.result";
    private static TestRunnerApi testRunner;

    static PlayerInteractionSetup() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string command = File.ReadAllText(Request).Trim();
        if (EditorApplication.isPlayingOrWillChangePlaymode && command != "CAPTURE" && command != "STOP") return;
        File.Delete(Request);
        try
        {
            if (command == "STOP")
            {
                EditorApplication.isPlaying = false;
                File.WriteAllText(Result, "Stopped Play Mode.");
            }
            else if (command == "PLAY")
            {
                EditorApplication.isPlaying = true;
                File.WriteAllText(Result, "Started Play Mode.");
            }
            else if (command == "CAPTURE")
            {
                CaptureReticle();
            }
            else if (command == "INSTALL")
            {
                Install();
                File.WriteAllText(Result, "PASS: Installed and saved player interaction.");
            }
            else if (command == "TEST")
            {
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.isDirty && (string.IsNullOrEmpty(scene.path) || !EditorSceneManager.SaveScene(scene)))
                        throw new InvalidOperationException("Could not save the open scene before tests.");
                }
                testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
                testRunner.RegisterCallbacks(new TestResults());
                testRunner.Execute(new ExecutionSettings(new Filter
                {
                    testMode = TestMode.EditMode,
                    assemblyNames = new[] { "FoggyRoad.Tests" }
                }));
            }
            else throw new InvalidOperationException("Unknown interaction setup command.");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Result, "FAIL: " + ex);
            Debug.LogException(ex);
        }
    }

    private static void CaptureReticle()
    {
        var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        var view = EditorWindow.GetWindow(gameViewType, false, null, false);
        var texture = gameViewType.GetField("m_RenderTexture", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).GetValue(view) as RenderTexture;
        if (texture == null) throw new InvalidOperationException("Game View has no rendered frame.");
        var previous = RenderTexture.active;
        var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels = image.GetPixels32();
                var flipped = new Color32[pixels.Length];
                for (int y = 0; y < texture.height; y++)
                    Array.Copy(pixels, y * texture.width, flipped, (texture.height - 1 - y) * texture.width, texture.width);
                image.SetPixels32(flipped);
            }
            image.Apply();
            File.WriteAllBytes("Library/PlayerInteractionPreview.png", image.EncodeToPNG());
            var interaction = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
            File.WriteAllText(Result, "CAPTURE: " + texture.width + "x" + texture.height +
                "; playing=" + EditorApplication.isPlaying +
                (interaction == null ? "; missing interaction" : "; dot visible=" + interaction.IsReticleVisible +
                 "; enabled=" + interaction.enabled + "; camera=" + interaction.GetComponent<Camera>().isActiveAndEnabled +
                 "; rect=" + interaction.GetReticleRect(texture.width, texture.height)));

        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    [MenuItem("Tools/Foggy Road/Install Player Interaction")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install interaction outside Play Mode.");
        int installed = 0;
        foreach (PickupDrop pickup in UnityEngine.Object.FindObjectsByType<PickupDrop>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var pickupData = new SerializedObject(pickup);
            Camera camera = pickupData.FindProperty("playerCamera").objectReferenceValue as Camera;
            if (camera == null) camera = pickup.GetComponentInChildren<Camera>(true);
            CharacterController player = pickup.GetComponent<CharacterController>();
            if (camera == null || player == null)
                throw new InvalidOperationException("Player camera or CharacterController is missing: " + pickup.name);
            PlayerInteraction interaction = camera.GetComponent<PlayerInteraction>();
            if (interaction == null) interaction = Undo.AddComponent<PlayerInteraction>(camera.gameObject);
            var data = new SerializedObject(interaction);
            data.FindProperty("player").objectReferenceValue = player;
            data.FindProperty("pickup").objectReferenceValue = pickup;
            data.FindProperty("interactionRange").floatValue = 4f;
            data.FindProperty("dotDiameter").floatValue = 3f;
            data.FindProperty("playerCamera").objectReferenceValue = camera;
            data.ApplyModifiedProperties();
            InstallReticle(camera, interaction);
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            if (!EditorSceneManager.SaveScene(camera.gameObject.scene))
                throw new IOException("Could not save the player scene.");
            installed++;
        }
        if (installed == 0) throw new InvalidOperationException("No PickupDrop player was found in the open scene.");
        Debug.Log("PlayerInteraction installed on " + installed + " player camera(s), range 4 m.");
    }

    private static void InstallReticle(Camera camera, PlayerInteraction interaction)
    {
        const string texturePath = "Assets/UI/InteractionDot.png";
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        Transform existing = camera.transform.Find("Interaction Reticle");
        GameObject overlay = existing != null ? existing.gameObject :
            new GameObject("Interaction Reticle", typeof(RectTransform), typeof(Canvas));
        overlay.layer = 5;
        overlay.transform.SetParent(camera.transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = camera.nearClipPlane + 0.1f;
        canvas.sortingOrder = 32767;
        camera.cullingMask |= 1 << 5;
        Transform existingDot = overlay.transform.Find("Dot");
        GameObject dotObject = existingDot != null ? existingDot.gameObject :
            new GameObject("Dot", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
        dotObject.layer = 5;
        dotObject.transform.SetParent(overlay.transform, false);
        var dot = dotObject.GetComponent<UnityEngine.UI.RawImage>();
        dot.texture = texture;
        if (existingDot == null) dot.color = new Color(1f, 1f, 1f, 0.75f);
        dot.raycastTarget = false;
        dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = Vector2.one * 0.5f;
        dot.rectTransform.pivot = Vector2.one * 0.5f;
        dot.rectTransform.anchoredPosition = Vector2.zero;
        if (existingDot == null) dot.rectTransform.sizeDelta = Vector2.one * 2f;
        if (dotObject.GetComponent<ReticleAppearance>() == null)
            Undo.AddComponent<ReticleAppearance>(dotObject);
        var data = new SerializedObject(interaction);
        data.FindProperty("reticleCanvas").objectReferenceValue = canvas;
        data.FindProperty("reticleDot").objectReferenceValue = dot;
        data.ApplyModifiedProperties();
    }

    private sealed class TestResults : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, "Library/PlayerInteractionTests.xml");
            File.WriteAllText(Result, $"{result.TestStatus}: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.\n{result.Message}");
            UnityEngine.Object.DestroyImmediate(testRunner);
        }
    }
}
