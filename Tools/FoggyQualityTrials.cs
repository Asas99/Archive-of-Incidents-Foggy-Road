using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Unity.Profiling;

// Temporary in-memory trials; no scene or asset is saved by this runner.
public static class FoggyQualityTrials
{
    class SlotEdit { public MeshFilter filter; public MeshRenderer renderer; public Mesh original, merged; public Material[] materials, compact; }
    static readonly List<SlotEdit> slots=new List<SlotEdit>();
    static readonly Dictionary<Material,bool> instancing=new Dictionary<Material,bool>();
    static readonly List<MeshRenderer> originals=new List<MeshRenderer>(), clones=new List<MeshRenderer>();
    static readonly List<GameObject> cloneObjects=new List<GameObject>();
    static readonly List<double> frame=new List<double>(), cpu=new List<double>(), render=new List<double>(), gpu=new List<double>(), batches=new List<double>(), triangles=new List<double>(), gc=new List<double>();
    static readonly string[] modes={"Baseline","CompactSlots","Instancing","StaticBatch","Combined","BaselineRepeat","CombinedRepeat"};
    static readonly Vector3[] positions={new Vector3(440,2.25f,902),new Vector3(418,2.25f,902),new Vector3(509.513f,2.235f,1248.762f)};
    static readonly float[] yaw={0,90,180};
    static readonly string[] names={"Interior","Pumps","ForestRoad"};
    static Camera camera;
    static MonoBehaviour movement;
    static bool movementEnabled;
    static Vector3 oldPosition;static Quaternion oldRotation;
    static ProfilerRecorder mainRec,renderRec,gpuRec,batchRec,triRec,gcRec;
    static int phase=-1, view, lastFrame=-1, oldSize;
    static double began;
    static bool running;
    public static string folder="Recovery/QualityTrials_20261113";
    static string status;
    static ProfilerRecorder Rec(ProfilerCategory cat,string name){try{return ProfilerRecorder.StartNew(cat,name,1);}catch{return default;}}
    public static string Main()
    {
        if(!EditorApplication.isPlaying)return "Requires Play Mode";
        if(running)return status;
        Directory.CreateDirectory(folder);
        camera=UnityEngine.Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(p=>p.GetComponent<Camera>()).First();
        oldPosition=camera.transform.position;oldRotation=camera.transform.rotation;
        movement=camera.GetComponentInParent<CharacterController>()?.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x.GetType().Name=="KarakterHareket");
        movementEnabled=movement&&movement.enabled;if(movement)movement.enabled=false;
        var gt=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var win=EditorWindow.GetWindow(gt);
        var prop=gt.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        oldSize=(int)prop.GetValue(win);prop.SetValue(win,3);win.Focus();
        Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
        PrepareSlots();
        var rr=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.scene==camera.gameObject.scene).ToArray();
        foreach(var g in rr.Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh).GroupBy(r=>r.GetComponent<MeshFilter>().sharedMesh).Where(g=>g.Count()>1))
            foreach(var r in g)foreach(var mat in r.sharedMaterials)
                if(mat && mat.shader.name=="Universal Render Pipeline/Lit"&&!instancing.ContainsKey(mat))instancing.Add(mat,mat.enableInstancing);
        PrepareBatches();
        mainRec=Rec(ProfilerCategory.Internal,"Main Thread");renderRec=Rec(ProfilerCategory.Internal,"Render Thread");
        gpuRec=Rec(ProfilerCategory.Render,"GPU Frame Time");batchRec=Rec(ProfilerCategory.Render,"Batches Count");triRec=Rec(ProfilerCategory.Render,"Triangles Count");gcRec=Rec(ProfilerCategory.Memory,"GC Allocated In Frame");
        File.WriteAllText(folder+"/results.csv","view,mode,width,height,frames,mean_ms,median_ms,p95_ms,cpu_ms,render_ms,gpu_ms,batches,triangles,gc_bytes\n");
        File.WriteAllText(folder+"/inventory.txt","slotRenderers="+slots.Count+"; instancingMaterials="+instancing.Count+"; staticBatchRenderers="+clones.Count+"; GPU="+SystemInfo.graphicsDeviceName+"; Editor Play Mode; original density/LOD/shadows/resolution scale retained.\n");
        running=true;phase=-1;view=0;
        RenderPipelineManager.beginFrameRendering+=Before;
        RenderPipelineManager.endFrameRendering+=After;
        EditorApplication.playModeStateChanged+=PlayChanged;
        Next();
        return status+"; slot candidates="+slots.Count+"; instancing materials="+instancing.Count+"; batch candidates="+clones.Count;
    }
    static void PrepareSlots()
    {
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if(!r.name.StartsWith("GRPRO",StringComparison.Ordinal)||r.HasPropertyBlock())continue;
            var f=r.GetComponent<MeshFilter>();var mesh=f?f.sharedMesh:null;var mats=r.sharedMaterials;
            if(!mesh||mats.Length!=mesh.subMeshCount||mats.Length<2||mats.Any(m=>!m||m.renderQueue>=2500)||mats.Distinct().Count()==mats.Length)continue;
            var unique=new List<Material>();var indices=new List<List<int>>();bool valid=true;
            for(int i=0;i<mats.Length;i++)
            {
                if(mesh.GetTopology(i)!=MeshTopology.Triangles){valid=false;break;}
                int at=unique.IndexOf(mats[i]);if(at<0){at=unique.Count;unique.Add(mats[i]);indices.Add(new List<int>());}
                indices[at].AddRange(mesh.GetIndices(i,true));
            }
            if(!valid)continue;
            var copy=UnityEngine.Object.Instantiate(mesh);copy.name=mesh.name+"_CompactSlots_Trial";copy.subMeshCount=unique.Count;
            for(int i=0;i<indices.Count;i++)copy.SetIndices(indices[i].ToArray(),MeshTopology.Triangles,i,false,0);
            copy.bounds=mesh.bounds;
            if(copy.vertexCount!=mesh.vertexCount||Enumerable.Range(0,copy.subMeshCount).Sum(i=>(long)copy.GetIndexCount(i))!=Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(long)mesh.GetIndexCount(i)))throw new Exception("Geometry invariant failed");
            slots.Add(new SlotEdit{filter=f,renderer=r,original=mesh,merged=copy,materials=mats,compact=unique.ToArray()});
        }
    }
    static void PrepareBatches()
    {
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if(!r.name.StartsWith("GRPRO",StringComparison.Ordinal)||r.HasPropertyBlock()||!r.enabled||r.isPartOfStaticBatch)continue;
            var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh||!f.sharedMesh.isReadable||r.GetComponentInParent<LODGroup>()||r.GetComponentInParent<Rigidbody>())continue;
            var go=new GameObject(r.name+"_StaticBatch_Trial"){hideFlags=HideFlags.DontSave,layer=r.gameObject.layer};
            go.transform.SetParent(r.transform.parent,false);go.transform.localPosition=r.transform.localPosition;go.transform.localRotation=r.transform.localRotation;go.transform.localScale=r.transform.localScale;
            go.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;
            var c=go.AddComponent<MeshRenderer>();
            c.sharedMaterials=r.sharedMaterials;c.shadowCastingMode=r.shadowCastingMode;c.receiveShadows=r.receiveShadows;c.lightProbeUsage=r.lightProbeUsage;c.reflectionProbeUsage=r.reflectionProbeUsage;c.probeAnchor=r.probeAnchor;c.lightProbeProxyVolumeOverride=r.lightProbeProxyVolumeOverride;
            c.lightmapIndex=r.lightmapIndex;c.lightmapScaleOffset=r.lightmapScaleOffset;c.realtimeLightmapIndex=r.realtimeLightmapIndex;c.realtimeLightmapScaleOffset=r.realtimeLightmapScaleOffset;
            c.motionVectorGenerationMode=r.motionVectorGenerationMode;c.renderingLayerMask=r.renderingLayerMask;c.sortingLayerID=r.sortingLayerID;c.sortingOrder=r.sortingOrder;c.allowOcclusionWhenDynamic=r.allowOcclusionWhenDynamic;
            c.additionalVertexStreams=r.additionalVertexStreams;
            originals.Add(r);clones.Add(c);cloneObjects.Add(go);
        }
        foreach(var region in cloneObjects.GroupBy(o=>new Vector2Int(Mathf.FloorToInt(o.transform.position.x/40),Mathf.FloorToInt(o.transform.position.z/40))))
            StaticBatchingUtility.Combine(region.ToArray(),null);
        foreach(var c in clones)c.enabled=false;
    }
    static void SetMode(string mode)
    {
        bool compact=mode=="CompactSlots"||mode.StartsWith("Combined");
        bool instance=mode=="Instancing"||mode.StartsWith("Combined");
        bool batch=mode=="StaticBatch"||mode.StartsWith("Combined");
        foreach(var s in slots){s.filter.sharedMesh=compact?s.merged:s.original;s.renderer.sharedMaterials=compact?s.compact:s.materials;}
        foreach(var m in instancing)m.Key.enableInstancing=instance||m.Value;
        for(int i=0;i<clones.Count;i++){originals[i].enabled=!batch;clones[i].enabled=batch;}
    }
    static void Before(ScriptableRenderContext context,Camera[] cameras)
    {
        if(running&&camera)camera.transform.SetPositionAndRotation(positions[view],Quaternion.Euler(0,yaw[view],0));
    }
    static double Last(ProfilerRecorder r,double divisor=1)=>r.Valid?r.LastValue/divisor:-1;
    static void After(ScriptableRenderContext context,Camera[] cameras)
    {
        if(!running||!camera||Time.frameCount==lastFrame||!cameras.Contains(camera)&&!cameras.Any(c=>c.GetComponent<DistantMountainBackdrop>()))return;
        lastFrame=Time.frameCount;
        try{
            double elapsed=Time.realtimeSinceStartupAsDouble-began;
            if(elapsed>=3)
            {
                frame.Add(Time.unscaledDeltaTime*1000);cpu.Add(Last(mainRec,1e6));render.Add(Last(renderRec,1e6));gpu.Add(Last(gpuRec,1e6));
                batches.Add(Last(batchRec));triangles.Add(Last(triRec));gc.Add(Last(gcRec));
            }
            if(elapsed>=8&&frame.Count>=20)Next();
        }catch(Exception e){File.WriteAllText(folder+"/error.txt",e.ToString());Stop();}
    }
    static double Avg(List<double> a)=>a.Count>0?a.Average():-1;
    static void Next()
    {
        if(phase>=0)
        {
            frame.Sort();string row=string.Join(",",new[]{names[view],modes[phase],Screen.width.ToString(),Screen.height.ToString(),frame.Count.ToString(),
                Avg(frame).ToString("F4",System.Globalization.CultureInfo.InvariantCulture),frame[frame.Count/2].ToString("F4",System.Globalization.CultureInfo.InvariantCulture),
                frame[Math.Min(frame.Count-1,(int)(frame.Count*.95))].ToString("F4",System.Globalization.CultureInfo.InvariantCulture),
                Avg(cpu).ToString("F4",System.Globalization.CultureInfo.InvariantCulture),Avg(render).ToString("F4",System.Globalization.CultureInfo.InvariantCulture),
                Avg(gpu).ToString("F4",System.Globalization.CultureInfo.InvariantCulture),Avg(batches).ToString("F1",System.Globalization.CultureInfo.InvariantCulture),
                Avg(triangles).ToString("F1",System.Globalization.CultureInfo.InvariantCulture),Avg(gc).ToString("F1",System.Globalization.CultureInfo.InvariantCulture)});
            File.AppendAllText(folder+"/results.csv",row+"\n");
        }
        phase++;if(phase>=modes.Length){phase=0;view++;}
        if(view>=positions.Length){status="COMPLETE; restored";Stop();return;}
        SetMode(modes[phase]);camera.transform.SetPositionAndRotation(positions[view],Quaternion.Euler(0,yaw[view],0));
        foreach(var g in UnityEngine.Object.FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None))g.InvalidateReflections();
        frame.Clear();cpu.Clear();render.Clear();gpu.Clear();batches.Clear();triangles.Clear();gc.Clear();began=Time.realtimeSinceStartupAsDouble;
        status=names[view]+" / "+modes[phase];File.WriteAllText(folder+"/status.txt",status);
    }
    static void PlayChanged(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Stop();}
    public static string Stop()
    {
        RenderPipelineManager.beginFrameRendering-=Before;RenderPipelineManager.endFrameRendering-=After;EditorApplication.playModeStateChanged-=PlayChanged;
        SetMode("Baseline");foreach(var go in cloneObjects)if(go)UnityEngine.Object.DestroyImmediate(go);
        foreach(var s in slots)if(s.merged)UnityEngine.Object.DestroyImmediate(s.merged);
        if(camera)camera.transform.SetPositionAndRotation(oldPosition,oldRotation);if(movement)movement.enabled=movementEnabled;
        mainRec.Dispose();renderRec.Dispose();gpuRec.Dispose();batchRec.Dispose();triRec.Dispose();gcRec.Dispose();running=false;
        if(status==null||!status.StartsWith("COMPLETE"))status="STOPPED; restored";File.WriteAllText(folder+"/status.txt",status);return status;
    }
}
