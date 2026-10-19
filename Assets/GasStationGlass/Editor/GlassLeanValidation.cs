using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class GlassLeanSetup
{
    public static void Verify()
    {
        var manager=Manager();if(manager.captureContent!=GasStationPlanarGlass.CaptureContent.DynamicObjects)throw new InvalidOperationException("Install dynamic-only mode first.");
        GlassRoomSetup.Verify();
        var report=File.ReadAllText(GlassRoomSetup.Output+"/Verification.txt");if(!report.Contains("PASS Automated checks complete"))throw new InvalidOperationException("Per-pane rendering verification incomplete");
        int images=0;foreach(var file in Directory.GetFiles(GlassRoomSetup.Output,"*_HybridActive_ReflectionOnly.png"))
        {
            var image=new Texture2D(2,2,TextureFormat.RGBA32,false);image.LoadImage(File.ReadAllBytes(file));int pink=0;foreach(var c in image.GetPixels32())if(c.r>120&&c.b>120&&c.g<60)pink++;UnityEngine.Object.DestroyImmediate(image);
            if(pink<20)throw new InvalidOperationException("Reflected actor did not survive depth occlusion: "+file+" pixels="+pink);images++;
        }
        if(images!=40)throw new InvalidOperationException("Expected forty both-side/angle dynamic images, got "+images);
        report+="\nPASS all forty dynamic images contain the reflected marker through the correct window opening.\n";
        File.WriteAllText(Output+"/VisualVerification.txt",report);
        if(!EditorSceneManager.SaveScene(manager.gameObject.scene))throw new IOException("Verified scene save failed");
        File.WriteAllText(Output+"/Saved.txt","PASS all panes verified and current scene saved. Development Build profiling is separate.\n");
    }
    public static void BuildBenchmark()
    {
        var scene=SceneManager.GetActiveScene();string path="Assets/GasStationGlass/Backups/LeanBenchmark.unity";
        if(!EditorSceneManager.SaveScene(scene,path,true))throw new IOException("Benchmark snapshot failed");
        var temporary=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var manager=temporary.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GasStationPlanarGlass>(true)).Single();
            var pane=temporary.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GlassSurfaceVariation>(true)).Single(p=>p.name=="Plane (10)");
            var car=temporary.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ReflectionActor>(true)).FirstOrDefault(a=>a.enabled&&a.gameObject.activeInHierarchy&&a.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/NPC_ARABALARI/")));
            if(!car)throw new InvalidOperationException("No real Car actor for benchmark");
            var root=new GameObject("Temporary reflection benchmark runner");SceneManager.MoveGameObjectToScene(root,temporary);var runner=root.AddComponent<GlassReflectionBenchmark>();runner.manager=manager;runner.pane=pane.transform;runner.testActor=car;runner.dynamicRenderer=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/GasStationGlass/Settings/Glass Dynamic Renderer.asset");var pipelineData=new SerializedObject(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);runner.mainRenderer=pipelineData.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue as UnityEngine.Rendering.Universal.UniversalRendererData;EditorSceneManager.SaveScene(temporary);
        }
        finally{EditorSceneManager.CloseScene(temporary,true);SceneManager.SetActiveScene(scene);}
        Directory.CreateDirectory(Output+"/Build");bool timing=PlayerSettings.enableFrameTimingStats;
        var pipelineAsset=new SerializedObject(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);var mainRenderer=pipelineAsset.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue as UnityEngine.Rendering.Universal.UniversalRendererData;var mainMode=mainRenderer.renderingMode;
        try
        {
            PlayerSettings.enableFrameTimingStats=true;
            // Keep both Forward and Forward+ shader variants for a fair runtime A/B.
            // The benchmark restores the main renderer to Forward+ before its first frame.
            mainRenderer.renderingMode=UnityEngine.Rendering.Universal.RenderingMode.Forward;
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},locationPathName=Output+"/Build/GlassBenchmark.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Output+"/Build.txt",result.summary.result+" bytes="+result.summary.totalSize+" duration="+result.summary.totalTime);
            if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Development Build failed: "+result.summary.result);
        }
        finally{mainRenderer.renderingMode=mainMode;EditorUtility.SetDirty(mainRenderer);AssetDatabase.SaveAssets();PlayerSettings.enableFrameTimingStats=timing;UnityEngine.Rendering.GPUResidentDrawer.ReinitializeIfNeeded();}
    }
}



