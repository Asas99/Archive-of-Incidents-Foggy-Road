using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public sealed class RoadWidthWindow : EditorWindow
{
    string message;
    [MenuItem("Tools/Foggy Road/Yolu Yuzde 50 Genislet")]
    public static void Open(){GetWindow<RoadWidthWindow>("Yol Genisletme").Show();}
    void OnGUI()
    {
        EditorGUILayout.HelpBox("NurbsPath_ROAD: merkez korunarak 1,5 kat genislik. Terrain, bitki ve mevcut bariyerler birlikte uyarlanir. Ortak FBX degismez.",MessageType.Info);
        var state=RoadWidthInstallation.State;
        EditorGUILayout.LabelField("Durum",state&&state.applied?"%50 genisletme uygulanmis":"Baslangic genisligi");
        using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode||StaticOcclusionCulling.isRunning))
        {
            if(GUILayout.Button("Onizleme: eski/yeni kenarlar")){RoadWidthInstallation.preview=!RoadWidthInstallation.preview;SceneView.RepaintAll();}
            if(GUILayout.Button("Benzinlik Girisini Goster"))RoadWidthChecks.Show(false);
            if(GUILayout.Button("Viraji Goster"))RoadWidthChecks.Show(true);
            if(GUILayout.Button("%50 Genislet, Cevreyi Uyarla ve Kaydet"))Run(RoadWidthInstallation.Apply);
            using(new EditorGUI.DisabledScope(!state||!state.applied))if(GUILayout.Button("Genisletmeyi Geri Al"))Run(RoadWidthInstallation.Revert);
        }
        if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
        if(state)EditorGUILayout.TextArea(state.report??"",GUILayout.MinHeight(100));
    }
    void Run(Action action){try{action();message="Islem baslatildi. Occlusion bake tamamlaninca sonuc raporlanir.";}catch(Exception e){message=e.Message;Debug.LogException(e);}}
}

[InitializeOnLoad] public static class RoadWidthInstallation
{
    const string Root="Assets/RoadTool/GeneratedWidth150";
    const string StatePath=Root+"/RoadWidthState.asset";
    const string Pending="Library/RoadWidening.baking";
    public static bool preview;
    static bool sawBake;
    public static RoadWidthState State=>AssetDatabase.LoadAssetAtPath<RoadWidthState>(StatePath);
    static string Backup=>State?State.backupDirectory:File.ReadAllText("Library/RoadWideningBackup.path").Trim();
    static RoadWidthInstallation(){EditorApplication.update+=Poll;SceneView.duringSceneGui+=Draw;}
    static string Id(Object obj)=>GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
    static T Resolve<T>(string id)where T:Object=>GlobalObjectId.TryParse(id,out var key)?GlobalObjectId.GlobalObjectIdentifierToObjectSlow(key) as T:null;
    static T[] SceneObjects<T>()where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).Where(x=>x.gameObject.scene==SceneManager.GetActiveScene()).ToArray();
    static MeshFilter Road()=>SceneObjects<MeshFilter>().Single(x=>x.name=="NurbsPath_ROAD");
    static string Glass()=>string.Join("\n",SceneObjects<GasStationPlanarGlass>().Select(x=>EditorJsonUtility.ToJson(x)).Concat(SceneObjects<GlassSurfaceVariation>().Select(x=>EditorJsonUtility.ToJson(x))));
    static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string request="Library/RoadWidening.request";
        if(File.Exists(request))
        {
            string mode=File.ReadAllText(request).Trim();File.Delete(request);
            try{if(mode=="APPLY")Apply();else if(mode=="REVERT")Revert();else if(mode=="VALIDATE")ValidateInstalled();else if(mode=="VIEW_STATION")RoadWidthChecks.Show(false);else if(mode=="VIEW_CURVE")RoadWidthChecks.Show(true);else if(mode=="CHECK_DRIVE")RoadWidthChecks.CheckVehicleClearance();}
            catch(Exception error){File.WriteAllText(Backup+"/InstallationError.txt",error.ToString());Debug.LogException(error);}
        }
        if(!File.Exists(Pending))return;
        if(StaticOcclusionCulling.isRunning){sawBake=true;return;}
        if(!sawBake&&(DateTime.UtcNow-File.GetLastWriteTimeUtc(Pending)).TotalSeconds<15)return;
        File.Delete(Pending);
        try
        {
            var manager=SceneObjects<SceneOptimizationController>().Single();
            var path=AssetDatabase.GetDependencies(manager.gameObject.scene.path).Single(p=>p.EndsWith("OcclusionCullingData.asset"));
            AssetDatabase.SaveAssets();
            Require(new FileInfo(path).Length>1024&&File.GetLastWriteTimeUtc(path)>=File.GetLastWriteTimeUtc(Backup+"/BakeStarted.txt"),"Fresh native occlusion data missing; culling remains disabled");
            manager.occlusionReady=true;EditorUtility.SetDirty(manager);
            ValidateInstalled();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(manager.gameObject.scene);
            File.WriteAllText(Backup+"/Complete.txt","PASS scene saved; fresh native occlusion bake completed. "+new FileInfo(path).Length+" bytes.\n"+State.report);
            Debug.Log("ROAD_WIDTH_INSTALLATION_COMPLETE");
        }
        catch(Exception e){File.WriteAllText(Backup+"/InstallationError.txt",e.ToString());Debug.LogException(e);}
    }
    static void Draw(SceneView view)
    {
        if(!preview)return;
        try
        {
            var road=Road();var field=new RoadWidthGeometry(State?State.originalRoad:road.sharedMesh,road.transform);
            for(int i=1;i<field.sections.Length;i++)
            {
                var a=field.sections[i-1];var b=field.sections[i];Handles.color=Color.cyan;Handles.DrawLine(a.a,b.a);Handles.DrawLine(a.b,b.b);
                Handles.color=Color.yellow;Handles.DrawLine(a.center+(a.a-a.center)*1.5f,b.center+(b.a-b.center)*1.5f);Handles.DrawLine(a.center+(a.b-a.center)*1.5f,b.center+(b.b-b.center)*1.5f);
                Handles.color=new Color(1,.5f,0,.5f);Handles.DrawLine(a.center+(a.a-a.center).normalized*(a.half*1.5f+6.75f),b.center+(b.a-b.center).normalized*(b.half*1.5f+6.75f));Handles.DrawLine(a.center+(a.b-a.center).normalized*(a.half*1.5f+6.75f),b.center+(b.b-b.center).normalized*(b.half*1.5f+6.75f));
            }
        }catch{preview=false;}
    }
    static void CreateAsset(Object obj,string name){AssetDatabase.CreateAsset(obj,Root+"/"+name+".asset");}
    public static void Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!StaticOcclusionCulling.isRunning,"Stop Play Mode/bake first");
        if(State&&State.applied){ValidateInstalled();return;}
        if(State&&State.widenedRoad&&State.meshes.Length==167&&State.terrains.Length==2){Reapply();return;}
        Require(!State,"An incomplete installation exists; use its recovery backup before a new installation");
        var road=Road();var scene=road.gameObject.scene;var original=road.sharedMesh;var field=new RoadWidthGeometry(original,road.transform);
        var rails=SceneObjects<MeshFilter>().Where(x=>System.Text.RegularExpressions.Regex.IsMatch(x.name,@"^GRPRO_[LR]_\d+_")).ToArray();
        Require(rails.Length==167,"Expected 167 existing guardrail modules; inspect changed scene first");
        Require(rails.All(r=>r.sharedMesh&&r.sharedMesh.isReadable),"Guardrail source not readable");
        var manager=SceneObjects<SceneOptimizationController>().Single();var terrains=manager.terrains.Concat(manager.smallVegetationTerrains).Where(t=>t).Distinct().ToArray();
        Require(terrains.Length==2,"Expected main Terrain and render-only vegetation Terrain");
        var originalTransforms=SceneObjects<Transform>().ToDictionary(t=>t,t=>(t.position,t.rotation,t.lossyScale,t.parent));
        var profileJson=EditorJsonUtility.ToJson(manager.profile);var glassJson=Glass();
        var protectedPhysics=SceneObjects<Collider>().Where(c=>!rails.Any(r=>c.transform==r.transform||c.transform.IsChildOf(r.transform))&&c.gameObject!=road.gameObject&&!(c is TerrainCollider)).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
        string backup=File.ReadAllText("Library/RoadWideningBackup.path").Trim();
        Require(EditorSceneManager.SaveScene(scene,backup+"/BeforeApply.unity",true),"Open-scene backup failed");
        foreach(var terrain in terrains){string path=AssetDatabase.GetAssetPath(terrain.terrainData);File.Copy(path,backup+"/"+Path.GetFileName(path),true);}
        foreach(var path in AssetDatabase.GetDependencies(scene.path).Where(p=>p.EndsWith("OcclusionCullingData.asset")))File.Copy(path,backup+"/Occlusion.before",true);
        var grass=manager.GetComponent<FarGrassRenderer>();if(grass)foreach(var cache in grass.caches.Where(c=>c))File.Copy(AssetDatabase.GetAssetPath(cache),backup+"/"+Path.GetFileName(AssetDatabase.GetAssetPath(cache)),true);
        Directory.CreateDirectory(Root);AssetDatabase.Refresh();
        var state=ScriptableObject.CreateInstance<RoadWidthState>();state.name="RoadWidthState";state.scenePath=scene.path;state.roadId=Id(road);state.backupDirectory=backup;state.originalRoad=original;state.originalRoadPath=AssetDatabase.GetAssetPath(original);state.profileJson=profileJson;state.glassJson=glassJson;CreateAsset(state,"RoadWidthState");
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Widen road and adapt roadside");
        try
        {
            var report=new StringBuilder();state.widenedRoad=field.Widen(original,road.transform);CreateAsset(state.widenedRoad,"NurbsPath_ROAD_Width150");
            var terrainLinks=new List<RoadWidthState.TerrainLink>();
            var primary=terrains.Single(t=>t.drawHeightmap);var primaryCopy=Object.Instantiate(primary.terrainData);primaryCopy.name=primary.terrainData.name+"_RoadWidth150";CreateAsset(primaryCopy,"MainTerrain");
            int heightCells=RoadTerrainAdapter.Shape(primaryCopy,primary.transform.position,field,state.widenedRoad,road.transform);
            report.AppendLine("Height resolution preserved="+primaryCopy.heightmapResolution+"; changed height samples="+heightCells);
            foreach(var terrain in terrains)
            {
                var copy=terrain==primary?primaryCopy:Object.Instantiate(terrain.terrainData);if(terrain!=primary){copy.name=terrain.terrainData.name+"_RoadWidth150";CreateAsset(copy,"SmallVegetation");}
                int detailCells=RoadTerrainAdapter.ClearDetails(copy,terrain.transform.position,field);
                var trees=RoadVegetationAdapter.UpdateTrees(copy,terrain.transform.position,primaryCopy,primary.transform.position,field);
                terrainLinks.Add(new RoadWidthState.TerrainLink{id=Id(terrain),original=terrain.terrainData,generated=copy});
                report.AppendLine(terrain.name+": trees="+copy.treeInstanceCount+" relocated="+trees.moved+" regrounded="+trees.grounded+" cleared detail cells="+detailCells);
            }
            state.terrains=terrainLinks.ToArray();
            var barrierAdapter=new RoadBarrierAdapter(field,rails);var meshLinks=new List<RoadWidthState.MeshLink>();var colliderLinks=new List<RoadWidthState.ColliderLink>();
            foreach(var rail in rails)
            {
                var mesh=FoggyRoadTools.MeshEdgeSnap.GuardrailMeshEdgeSnapPRO.WidenExistingModule(rail.sharedMesh,rail.transform,rail.name.StartsWith("GRPRO_L_"),barrierAdapter);CreateAsset(mesh,rail.name);
                meshLinks.Add(new RoadWidthState.MeshLink{id=Id(rail),original=rail.sharedMesh,generated=mesh});
                foreach(var collider in rail.GetComponents<Collider>())
                {
                    var link=new RoadWidthState.ColliderLink{id=Id(collider),enabled=collider.enabled};
                    if(collider is MeshCollider mc){link.originalMesh=mc.sharedMesh;link.generatedMesh=mc.sharedMesh==rail.sharedMesh?mesh:barrierAdapter.Deform(mc.sharedMesh,collider.transform,rail.name.StartsWith("GRPRO_L_"));if(link.generatedMesh!=mesh)CreateAsset(link.generatedMesh,rail.name+"_Collider");}
                    else if(collider is BoxCollider box){link.center=box.center;link.size=box.size;}
                    else if(collider is SphereCollider sphere){link.center=sphere.center;link.radius=sphere.radius;}
                    else if(collider is CapsuleCollider capsule){link.center=capsule.center;link.radius=capsule.radius;link.height=capsule.height;}
                    else throw new InvalidOperationException("Unsupported barrier collider "+collider.GetType().Name);
                    colliderLinks.Add(link);
                }
            }
            state.meshes=meshLinks.ToArray();state.colliders=colliderLinks.ToArray();
            // Assign only after terrain/tree and all mesh construction has succeeded.
            Undo.RecordObject(road,"Road mesh");road.sharedMesh=state.widenedRoad;var roadCollider=road.GetComponent<MeshCollider>();Require(roadCollider,"Road collider missing");Undo.RecordObject(roadCollider,"Road collider");roadCollider.sharedMesh=state.widenedRoad;
            foreach(var link in state.terrains){var t=Resolve<Terrain>(link.id);Undo.RecordObject(t,"Terrain reference");t.terrainData=link.generated;var c=t.GetComponent<TerrainCollider>();if(c){Undo.RecordObject(c,"Terrain collider reference");c.terrainData=link.generated;}t.Flush();EditorUtility.SetDirty(link.generated);}
            foreach(var link in state.meshes){var f=Resolve<MeshFilter>(link.id);Undo.RecordObject(f,"Barrier mesh");f.sharedMesh=link.generated;EditorUtility.SetDirty(f);}
            foreach(var link in state.colliders)
            {
                var c=Resolve<Collider>(link.id);Undo.RecordObject(c,"Barrier collider");
                if(c is MeshCollider mc)mc.sharedMesh=link.generatedMesh;
                else
                {
                    bool left=c.name.StartsWith("GRPRO_L_");Vector3 Map(Vector3 p)=>c.transform.InverseTransformPoint(barrierAdapter.Map(c.transform.TransformPoint(p),left));
                    if(c is BoxCollider box){var bounds=new Bounds(Map(link.center),Vector3.zero);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)bounds.Encapsulate(Map(link.center+Vector3.Scale(link.size*.5f,new Vector3(x,y,z))));box.center=bounds.center;box.size=bounds.size;}
                    else if(c is SphereCollider sphere)sphere.center=Map(link.center);else if(c is CapsuleCollider capsule)capsule.center=Map(link.center);
                }
                if(c is BoxCollider finalBox){link.newCenter=finalBox.center;link.newSize=finalBox.size;}else if(c is SphereCollider finalSphere)link.newCenter=finalSphere.center;else if(c is CapsuleCollider finalCapsule)link.newCenter=finalCapsule.center;
                EditorUtility.SetDirty(c);
            }
            Physics.SyncTransforms();
            foreach(var pair in originalTransforms){var t=pair.Key;Require(t.position==pair.Value.Item1&&Quaternion.Angle(t.rotation,pair.Value.Item2)<.01f&&t.lossyScale==pair.Value.Item3&&t.parent==pair.Value.Item4,"Protected transform changed: "+t.name);}
            foreach(var pair in protectedPhysics)Require(pair.Value==EditorJsonUtility.ToJson(pair.Key),"Unrelated collider changed: "+pair.Key.name);
            Require(profileJson==EditorJsonUtility.ToJson(manager.profile)&&glassJson==Glass(),"Protected glass/profile changed");
            state.applied=true;state.report=report.ToString();EditorUtility.SetDirty(state);Undo.CollapseUndoOperations(undo);
            VegetationBrushPaint.RefreshDistantGrass(manager.terrains);AssetDatabase.SaveAssets();
            ValidateInstalled();StartBake(manager);preview=false;
        }
        catch
        {
            Undo.FlushUndoRecordObjects();Undo.RevertAllDownToGroup(undo);state.applied=false;EditorUtility.SetDirty(state);VegetationBrushPaint.RefreshDistantGrass(manager.terrains);AssetDatabase.SaveAssets();throw;
        }
    }
    public static void RebuildOcclusionForGeometryChange()
    {
        Require(State && State.applied, "Widened road setup missing");
        StartBake(SceneObjects<SceneOptimizationController>().Single());
    }
    static void StartBake(SceneOptimizationController manager)
    {
        manager.occlusionReady=false;EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Backup+"/BakeStarted.txt",DateTime.UtcNow.ToString("O"));File.WriteAllText(Pending,DateTime.UtcNow.ToString("O"));sawBake=false;
        Require(StaticOcclusionCulling.GenerateInBackground(),"Could not start native occlusion bake");
    }
    public static void ValidateInstalled()
    {
        var state=State;Require(state,"No road installation");var road=Resolve<MeshFilter>(state.roadId);Require(road,"Road identity missing");
        if(!state.applied){Require(road.sharedMesh==state.originalRoad,"Original road not restored");return;}
        Require(road.sharedMesh==state.widenedRoad&&road.GetComponent<MeshCollider>().sharedMesh==state.widenedRoad,"Road renderer/collider mismatch");
        var field=new RoadWidthGeometry(state.originalRoad,road.transform);var widened=new RoadWidthGeometry(state.widenedRoad,road.transform);
        Require(field.sections.Length==widened.sections.Length&&state.originalRoad.vertexCount==state.widenedRoad.vertexCount,"Road topology changed");
        float maxError=0;
        for(int i=0;i<field.sections.Length;i++){maxError=Mathf.Max(maxError,Mathf.Abs(widened.sections[i].half/field.sections[i].half-1.5f));Require(Vector3.Distance(field.sections[i].center,widened.sections[i].center)<.002f,"Road centerline changed");}
        Require(maxError<.0002f,"Width ratio failed");
        var manager=SceneObjects<SceneOptimizationController>().Single();Require(state.profileJson==EditorJsonUtility.ToJson(manager.profile)&&state.glassJson==Glass(),"Glass or optimization profile changed");
        var main=state.terrains.Select(l=>Resolve<Terrain>(l.id)).Single(t=>t.drawHeightmap);int samples=0;
        for(int i=0;i<widened.sections.Length;i+=4)
        {
            var section=widened.sections[i];foreach(float ratio in new[]{-.95f,0,.95f})
            {
                var p=section.center+(section.a-section.center)*ratio;
                if(p.x<main.transform.position.x||p.x>main.transform.position.x+main.terrainData.size.x||p.z<main.transform.position.z||p.z>main.transform.position.z+main.terrainData.size.z)continue;
                float h=main.SampleHeight(p)+main.transform.position.y;Require(h<=p.y-.025f,"Terrain intersects road at "+p+" terrain="+h);
                Require(road.GetComponent<MeshCollider>().Raycast(new Ray(p+Vector3.up*2,Vector3.down),out var hit,4),"Road collision gap at "+p);samples++;
            }
        }
        foreach(var link in state.terrains){var t=Resolve<Terrain>(link.id);Require(t.terrainData==link.generated&&link.original.treeInstanceCount-link.approvedTreeRemovals==link.generated.treeInstanceCount,"Terrain/tree count mismatch");}
        Require(state.meshes.Length==167,"Guardrail count changed");
        float minGap=float.PositiveInfinity;
        foreach(var link in state.meshes)
        {
            var f=Resolve<MeshFilter>(link.id);Require(f&&f.sharedMesh==link.generated&&link.original.vertexCount==link.generated.vertexCount,"Guardrail mismatch");
            foreach(var v in link.generated.vertices){var p=f.transform.TransformPoint(v);Require(field.Query(p,out var q),"Guardrail outside field");minGap=Mathf.Min(minGap,Mathf.Abs(q.side)-q.half*1.5f);}
        }
        Require(minGap>.15f,"Guardrail encroaches on new road: "+minGap);
        string validation="PASS width "+(field.sections.Average(s=>s.half)*2).ToString("F3")+" -> "+(widened.sections.Average(s=>s.half)*2).ToString("F3")+"m; sections="+field.sections.Length+" ratio error="+maxError+"; collision/terrain probes="+samples+"; 167 modules, min lateral clearance="+minGap+"m; profile/glass unchanged.\n";
        File.WriteAllText(state.backupDirectory+"/Validation.txt",validation);
        if(!state.report.Contains("PASS width")){state.report+=validation;EditorUtility.SetDirty(state);}
    }
    public static void Revert()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!StaticOcclusionCulling.isRunning,"Stop Play/bake first");var state=State;Require(state&&state.applied,"No applied widening");
        var road=Resolve<MeshFilter>(state.roadId);road.sharedMesh=state.originalRoad;road.GetComponent<MeshCollider>().sharedMesh=state.originalRoad;
        foreach(var link in state.terrains){var t=Resolve<Terrain>(link.id);t.terrainData=link.original;var c=t.GetComponent<TerrainCollider>();if(c)c.terrainData=link.original;t.Flush();EditorUtility.SetDirty(t);}
        foreach(var link in state.meshes){var f=Resolve<MeshFilter>(link.id);f.sharedMesh=link.original;EditorUtility.SetDirty(f);}
        foreach(var link in state.colliders){var c=Resolve<Collider>(link.id);if(c is MeshCollider mc)mc.sharedMesh=link.originalMesh;else if(c is BoxCollider b){b.center=link.center;b.size=link.size;}else if(c is SphereCollider s)s.center=link.center;else if(c is CapsuleCollider p)p.center=link.center;EditorUtility.SetDirty(c);}
        state.applied=false;EditorUtility.SetDirty(state);Physics.SyncTransforms();var manager=SceneObjects<SceneOptimizationController>().Single();VegetationBrushPaint.RefreshDistantGrass(manager.terrains);StartBake(manager);
    }
    static void Reapply()
    {
        var state=State;var road=Resolve<MeshFilter>(state.roadId);Require(road.sharedMesh==state.originalRoad,"Road changed after reverting");
        road.sharedMesh=state.widenedRoad;road.GetComponent<MeshCollider>().sharedMesh=state.widenedRoad;
        foreach(var link in state.terrains){var t=Resolve<Terrain>(link.id);t.terrainData=link.generated;var c=t.GetComponent<TerrainCollider>();if(c)c.terrainData=link.generated;t.Flush();EditorUtility.SetDirty(t);}
        foreach(var link in state.meshes){var f=Resolve<MeshFilter>(link.id);f.sharedMesh=link.generated;EditorUtility.SetDirty(f);}
        foreach(var link in state.colliders){var c=Resolve<Collider>(link.id);if(c is MeshCollider mc)mc.sharedMesh=link.generatedMesh;else if(c is BoxCollider b){b.center=link.newCenter;b.size=link.newSize;}else if(c is SphereCollider s)s.center=link.newCenter;else if(c is CapsuleCollider p)p.center=link.newCenter;EditorUtility.SetDirty(c);}
        state.applied=true;EditorUtility.SetDirty(state);Physics.SyncTransforms();ValidateInstalled();var manager=SceneObjects<SceneOptimizationController>().Single();VegetationBrushPaint.RefreshDistantGrass(manager.terrains);StartBake(manager);
    }
}
