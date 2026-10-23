using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class FrameCostProbeSetup
{
    private const string Request = "Library/FrameCostProbe.request";
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static EditorWindow gameView;
    private static PropertyInfo selectedSize;
    private static int previousSize = -1;
    private static double readyAt;
    private static double missingCameraAt;
    static FrameCostProbeSetup() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isPlaying && EditorApplication.isPaused && UnityEngine.Object.FindFirstObjectByType<FrameCostProbe>() != null) EditorApplication.isPaused = false;
        if (previousSize >= 0 && UnityEngine.Object.FindFirstObjectByType<FrameCostProbe>() == null && !File.Exists(Request))
        {
            selectedSize.SetValue(gameView, previousSize);
            previousSize = -1;
        }
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string phase = File.ReadAllText(Request).Trim();
        if (phase == "INSTALL")
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try { InstallMountainRenderer(); }
            catch (Exception ex) { File.WriteAllText("Library/FrameCostProbe.error", ex.ToString()); Debug.LogException(ex); }
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            if (phase == "Profiler" && !EditorApplication.isPlayingOrWillChangePlaymode && EditorSceneManager.GetActiveScene().path != "Assets/game prototype scene.unity")
            {
                string backup = "Recovery/ProfilerSceneRecovery_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Directory.CreateDirectory(backup);
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.isDirty && !EditorSceneManager.SaveScene(scene, backup + "/OpenScene_" + i + ".unity", true))
                        throw new IOException("Could not preserve the open scene before loading gameplay.");
                }
                EditorSceneManager.OpenScene("Assets/game prototype scene.unity");
                return;
            }
            if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = true;
            return;
        }
        if (UnityEngine.Object.FindFirstObjectByType<FrameCostProbe>() != null) return;
        if (previousSize < 0)
        {
            try { PrepareResolution(); }
            catch (Exception ex) { File.Delete(Request); File.WriteAllText("Library/FrameCostProbe.error", ex.ToString()); Debug.LogException(ex); }
            return;
        }
        if (EditorApplication.timeSinceStartup < readyAt) return;
        GameObject temporary = null;
        try
        {
            if (phase != "Before" && phase != "After" && phase != "Profiler") throw new InvalidOperationException("Invalid probe phase.");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
            if (player == null)
            {
                if (missingCameraAt == 0) missingCameraAt = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - missingCameraAt < 15) return;
                throw new InvalidOperationException("Gameplay camera missing.");
            }
            missingCameraAt = 0; File.Delete(Request);
            if (File.Exists("Library/FrameCostProbe.error")) File.Delete("Library/FrameCostProbe.error");
            var gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameView != null) EditorWindow.GetWindow(gameView, false, null, false).Focus();
            temporary = new GameObject("Temporary frame cost measurement");
            var probe = temporary.AddComponent<FrameCostProbe>();
            probe.Begin(player.GetComponent<Camera>(), phase);
        }
        catch (Exception ex)
        {
            if (File.Exists(Request)) File.Delete(Request);
            if (temporary != null) UnityEngine.Object.Destroy(temporary);
            File.WriteAllText("Library/FrameCostProbe.error", ex.ToString()); Debug.LogException(ex);
        }
    }

    private static void PrepareResolution()
    {
        Type viewType = Type.GetType("UnityEditor.GameView,UnityEditor", true);
        gameView = EditorWindow.GetWindow(viewType, false, null, false);
        selectedSize = viewType.GetProperty("selectedSizeIndex", All);
        Type sizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor", true);
        object sizes = sizesType.BaseType.GetProperty("instance", All).GetValue(null);
        MethodInfo getGroup = sizesType.GetMethod("GetGroup", All);
        object groupId = Enum.Parse(getGroup.GetParameters()[0].ParameterType, "Standalone");
        object group = getGroup.Invoke(sizes, new[] { groupId });
        Type groupType = group.GetType();
        int index = (int)groupType.GetMethod("GetTotalCount", All).Invoke(group, null);
        Type sizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor", true);
        ConstructorInfo ctor = Array.Find(sizeType.GetConstructors(All), c => c.GetParameters().Length == 4);
        object fixedKind = Enum.Parse(ctor.GetParameters()[0].ParameterType, "FixedResolution");
        object size = ctor.Invoke(new[] { fixedKind, (object)1280, 720, "FPS Probe 1280x720" });
        groupType.GetMethod("AddCustomSize", All).Invoke(group, new[] { size });
        previousSize = (int)selectedSize.GetValue(gameView);
        selectedSize.SetValue(gameView, index);
        gameView.Focus();
        readyAt = EditorApplication.timeSinceStartup + 1;
    }

    private static void InstallMountainRenderer()
    {
        const string path = "Assets/Settings/Distant Mountains Renderer.asset";
        var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var settings = new SerializedObject(pipeline);
        var list = settings.FindProperty("m_RendererDataList");
        var renderer = UnityEditor.AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if (renderer == null)
        {
            renderer = UnityEngine.Object.Instantiate((UniversalRendererData)list.GetArrayElementAtIndex(0).objectReferenceValue);
            renderer.name = "Distant Mountains Renderer";
            renderer.rendererFeatures.Clear();
            renderer.renderingMode = RenderingMode.ForwardPlus;
            var data = new SerializedObject(renderer);
            data.FindProperty("m_RendererFeatureMap").ClearArray();
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(renderer, path);
        }
        int index = -1;
        for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) index = i;
        if (index < 0)
        {
            index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            settings.ApplyModifiedProperties();
        }
        var backdrop = UnityEngine.Object.FindFirstObjectByType<DistantMountainBackdrop>();
        if (backdrop == null) throw new InvalidOperationException("Mountain camera missing.");
        var cameraData = backdrop.GetComponent<Camera>().GetUniversalAdditionalCameraData();
        Undo.RecordObject(cameraData, "Remove gameplay effects from mountain camera");
        cameraData.SetRenderer(index);
        EditorUtility.SetDirty(cameraData);
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(backdrop.gameObject.scene);
        EditorSceneManager.SaveScene(backdrop.gameObject.scene);
        File.WriteAllText("Library/FrameCostFix.txt", "PASS: Mountain-only renderer has zero gameplay features; renderer index=" + index + ". Main renderer unchanged.");
    }
}
