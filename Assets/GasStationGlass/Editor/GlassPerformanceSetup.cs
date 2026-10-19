using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class GlassLeanSetup
{
    public const string PerformanceOutput="Recovery/GlassPerformance";
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Apply Performance Settings")]
    public static void InstallPerformance()
    {
        Directory.CreateDirectory(PerformanceOutput);var manager=Manager();var scene=manager.gameObject.scene;
        if(!EditorSceneManager.SaveScene(scene,Root+"/Backups/BeforePerformance_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true))throw new IOException("Scene backup failed");
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var so=new SerializedObject(pipeline);var renderers=so.FindProperty("m_RendererDataList");
        for(int i=0;i<renderers.arraySize;i++)
        {
            var data=renderers.GetArrayElementAtIndex(i).objectReferenceValue as UniversalRendererData;
            if(!data)throw new InvalidOperationException("Unsupported renderer in active pipeline at "+i);
            // Main and legacy renderers are already Forward+. Only change our dedicated one.
            if(i==manager.dynamicRendererIndex){Undo.RecordObject(data,"Glass Forward+ compatibility");data.renderingMode=RenderingMode.ForwardPlus;EditorUtility.SetDirty(data);}
            else if(data.renderingMode!=RenderingMode.ForwardPlus)throw new InvalidOperationException("Another renderer blocks GPU Resident Drawer: "+data.name);
        }
        Undo.RecordObject(manager,"Glass performance settings");manager.transmissionQuality=GasStationPlanarGlass.TransmissionQuality.Balanced;
        manager.actorEnterDistance=20;manager.actorExitDistance=22;manager.reflectionSceneDepth=25;manager.liveUpdatesPerSecond=60;
        manager.maxActiveGroups=1;manager.previewInSceneView=false;manager.InvalidateReflections();EditorUtility.SetDirty(manager);
        // Recover the temporary profiling switch left by the previous interrupted build.
        PlayerSettings.enableFrameTimingStats=false;
        // GRD cannot run if its DOTS shader variants are stripped, even with Forward+.
        var graphicsObject=UnityEditor.Unsupported.GetSerializedAssetInterfaceSingleton("GraphicsSettings");
        var graphicsSettings=new SerializedObject(graphicsObject);var stripping=graphicsSettings.FindProperty("m_BrgStripping");
        if(stripping==null)throw new InvalidOperationException("GraphicsSettings BRG stripping property not found");
        stripping.intValue=(int)BatchRendererGroupStrippingMode.KeepAll;graphicsSettings.ApplyModifiedPropertiesWithoutUndo();
        for(int i=0;i<renderers.arraySize;i++)pipeline.GetRenderer(i);
        GPUResidentDrawer.ReinitializeIfNeeded();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(PerformanceOutput+"/Installed.txt","All registered panes configured. Balanced blur, actor range 20/22m, capture scene depth 25m, live 60Hz max.\n");
    }
    public static bool ResidentDrawerRunning()=>typeof(GPUResidentDrawer).GetProperty("instance",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null)!=null;
    public static void FrameDebuggerAPI()
    {
        Directory.CreateDirectory(PerformanceOutput);
        EditorApplication.ExecuteMenuItem("Window/Analysis/Frame Debugger");
        var types=AppDomain.CurrentDomain.GetAssemblies().SelectMany(a=>{try{return a.GetTypes();}catch{return Array.Empty<Type>();}}).Where(t=>t.Name.Contains("FrameDebugger")).ToArray();
        File.WriteAllText(PerformanceOutput+"/FrameDebuggerTypes.txt",string.Join("\n",types.Select(t=>t.FullName)));
        var type=types.FirstOrDefault(t=>t.Name=="FrameDebuggerUtility");
        File.WriteAllText(PerformanceOutput+"/FrameDebuggerAPI.txt",type==null?"Unavailable":string.Join("\n",type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Select(m=>m.ToString())));
    }
    public static void VerifyPerformance()
    {
        Directory.CreateDirectory(PerformanceOutput);var manager=Manager();var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var so=new SerializedObject(pipeline);var list=so.FindProperty("m_RendererDataList");
        for(int i=0;i<list.arraySize;i++){var renderer=list.GetArrayElementAtIndex(i).objectReferenceValue as UniversalRendererData;if(!renderer||renderer.renderingMode!=RenderingMode.ForwardPlus)throw new InvalidOperationException("Renderer not Forward+: "+i);}
        GPUResidentDrawer.ReinitializeIfNeeded();
        if(!ResidentDrawerRunning())throw new InvalidOperationException("GPU Resident Drawer has no live instance; configuration alone is not sufficient");
        Verify();File.Copy(Output+"/VisualVerification.txt",PerformanceOutput+"/VisualVerification.txt",true);
        File.WriteAllText(PerformanceOutput+"/GPU.txt","PASS GPU Resident Drawer live instance exists; all configured renderers Forward+.\n");
    }
    public static void FrameProof()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility")).FirstOrDefault(t=>t!=null);
        if(type==null)throw new InvalidOperationException("Open Frame Debugger first");
        var flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        var set=type.GetMethod("SetEnabled",flags);set.Invoke(null,new object[]{true,0});
        double until=EditorApplication.timeSinceStartup+8;
        EditorApplication.CallbackFunction poll=null;poll=()=>
        {
            int count=(int)type.GetProperty("count",flags).GetValue(null);
            if(count==0&&EditorApplication.timeSinceStartup<until){SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();return;}
            EditorApplication.update-=poll;
            try
            {
                var events=(Array)type.GetMethod("GetFrameEvents",flags).Invoke(null,null);var text=new System.Text.StringBuilder();int hybrid=0;
                for(int i=0;i<events.Length;i++)
                {
                    var item=events.GetValue(i);string fields=string.Join(" ",item.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Select(f=>f.Name+"="+f.GetValue(item)));
                    string name=(string)type.GetMethod("GetFrameEventInfoName",flags).Invoke(null,new object[]{i});string line=i+" "+fields+" "+name;text.AppendLine(line);if(line.Contains("BatchRendererGroup")||line.Contains("Hybrid Batch Group")||line.Contains("m_Type=HybridBatch"))hybrid++;
                }
                File.WriteAllText(PerformanceOutput+"/FrameEvents.txt","GRD="+ResidentDrawerRunning()+" hybrid events="+hybrid+"\n"+text);
            }
            finally{set.Invoke(null,new object[]{false,0});}
        };
        EditorApplication.update+=poll;SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
    }
}
