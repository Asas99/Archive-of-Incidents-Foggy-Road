using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class StationPropsLodSetup
{
    const string Request="Library/StationPropsLod.request";
    static string[] Keys={"copkovasi","icebox","elektriktesisataksamlari"};
    static float[] Distances={10,15,18};
    static string[] LowKeys;
    [Serializable] sealed class RequestData { public string mode; public string[] keys; public string[] lowKeys; public float[] distances; }
    static StationPropsLodSetup(){EditorApplication.update+=Poll;}
    static string Normalize(string s)=>new string(s.Replace('ı','i').Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
    static GameObject Find(string key)
    {
        var candidates=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==EditorSceneManager.GetActiveScene()&&Normalize(t.name)==key).ToArray();
        Require(candidates.Length==1,key+" candidates="+candidates.Length);return candidates[0].gameObject;
    }
    static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
    static MeshFilter Mesh(GameObject o){var all=o.GetComponentsInChildren<MeshFilter>(true);Require(all.Length==1&&all[0].gameObject==o&&all[0].sharedMesh,"Expected one root mesh: "+o.name);return all[0];}
    static long Tris(GameObject o)=>o.GetComponentsInChildren<MeshFilter>(true).Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(s=>(long)f.sharedMesh.GetIndexCount(s)/3));
    static Bounds Bounds(GameObject o){var rr=o.GetComponentsInChildren<Renderer>(true);Require(rr.Length>0,"No renderers: "+o.name);var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
    static string Physics()=>string.Join("\n",Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).Where(c=>c.gameObject.scene==EditorSceneManager.GetActiveScene()).Select(c=>EditorJsonUtility.ToJson(c)));
    static void Poll()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||StaticOcclusionCulling.isRunning)return;
        var mode=File.ReadAllText(Request).Trim();File.Delete(Request);
        try
        {
            if(mode.StartsWith("{"))
            {
                var request=JsonUtility.FromJson<RequestData>(mode);
                Require(request.keys!=null&&request.keys.Length>=1&&request.keys.Length<=32&&request.keys.All(k=>!string.IsNullOrWhiteSpace(k))&&request.keys.Select(Normalize).Distinct().Count()==request.keys.Length,"Expected distinct target names");
                Require(request.distances!=null&&request.distances.Length==request.keys.Length&&request.distances.All(d=>d>=5&&d<=50),"Invalid LOD distances");
                Require(request.lowKeys==null||(request.lowKeys.Length==request.keys.Length&&request.lowKeys.All(k=>!string.IsNullOrWhiteSpace(k))),"Invalid low-detail model names");
                Keys=request.keys.Select(Normalize).ToArray();LowKeys=request.lowKeys?.Select(Normalize).ToArray();Distances=request.distances;mode=request.mode;
            }
            if(mode=="AUDIT")Audit();else if(mode=="APPLY")Apply();
        }
        catch(Exception e){File.WriteAllText("Recovery/StationPropsLodError.txt",e.ToString());Debug.LogException(e);}
    }
    static void Audit()
    {
        var text=new StringBuilder();
        for(int i=0;i<Keys.Length;i++)foreach(var key in new[]{Keys[i],LowKeys==null?Keys[i]+"lod2":LowKeys[i]})
        {
            var go=Find(key);var mf=Mesh(go);
            text.AppendLine($"{go.name}: triangles={Tris(go)} position={go.transform.position:F4} rotation={go.transform.eulerAngles:F3} scale={go.transform.lossyScale:F4}; bounds={Bounds(go)}; meshBounds={mf.sharedMesh.bounds}; staticFlags={GameObjectUtility.GetStaticEditorFlags(go)}; components={string.Join(",",go.GetComponentsInChildren<Component>(true).Where(c=>c).Select(c=>c.GetType().Name))}; mesh={AssetDatabase.GetAssetPath(mf.sharedMesh)}");
        }
        File.WriteAllText("Recovery/StationPropsLodAudit.txt",text.ToString());
    }
    static void Apply()
    {
        var highs=Keys.Select(Find).ToArray();var templates=Keys.Select((k,i)=>Find(LowKeys==null?k+"lod2":LowKeys[i])).ToArray();var lows=(GameObject[])templates.Clone();var scene=highs[0].scene;
        if(highs.All(h=>h.GetComponentInParent<LODGroup>())){for(int i=0;i<highs.Length;i++)lows[i]=highs[i].GetComponentInParent<LODGroup>().GetLODs()[1].renderers.Single().gameObject;Verify(highs,lows);return;}
        Require(!highs.Intersect(templates).Any(),"High and low model sets overlap");
        for(int i=0;i<highs.Length;i++)
        {
            Mesh(highs[i]);Mesh(lows[i]);
            Require(!highs[i].GetComponentInParent<LODGroup>()&&!lows[i].GetComponentInParent<LODGroup>(),"Existing partial LOD setup");
            Require(!highs[i].GetComponentInChildren<LODGroup>()&&!lows[i].GetComponentInChildren<LODGroup>(),"Nested LODGroup");
            Require(Tris(lows[i])>0&&Tris(lows[i])<Tris(highs[i]),"Low model is not lighter: "+Keys[i]);
            Require(lows[i].GetComponentsInChildren<Component>(true).All(c=>c is Transform||c is MeshRenderer||c is MeshFilter),"Low model has physics/scripts; inspect: "+lows[i].name);
        }
        string backup="Recovery/StationPropsLod_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);File.WriteAllText("Library/StationPropsLodBackup.path",backup);
        Require(EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true),"Scene backup failed");
        Audit();File.Copy("Recovery/StationPropsLodAudit.txt",backup+"/Audit.txt");
        var physics=Physics();var highPos=highs.Select(h=>h.transform.position).ToArray();var highRot=highs.Select(h=>h.transform.rotation).ToArray();var highScale=highs.Select(h=>h.transform.lossyScale).ToArray();
        var highMaterials=highs.Select(h=>h.GetComponent<Renderer>().sharedMaterials).ToArray();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Station props LOD2 integration");var report=new StringBuilder();
        try
        {
            // Clone shared low sources before moving any source into its LOD holder.
            for(int i=0;i<highs.Length;i++)if(templates.Take(i).Contains(templates[i]))
            {
                lows[i]=Object.Instantiate(templates[i]);Undo.RegisterCreatedObjectUndo(lows[i],"Shared low-detail instance");lows[i].name=highs[i].name+"lod2";
            }
            for(int i=0;i<highs.Length;i++)
            {
                var high=highs[i];var low=lows[i];var hb=Bounds(high);var hm=Mesh(high).sharedMesh;var lm=Mesh(low).sharedMesh;
                var holder=new GameObject(high.name+" LOD");Undo.RegisterCreatedObjectUndo(holder,"Prop LOD group");holder.transform.SetParent(high.transform.parent,true);holder.transform.position=highPos[i];holder.transform.rotation=Quaternion.identity;
                Undo.SetTransformParent(high.transform,holder.transform,"Prop LOD group");Undo.SetTransformParent(low.transform,holder.transform,"Prop LOD group");Undo.RecordObject(low.transform,"Align low detail prop");
                low.transform.rotation=highRot[i];
                var ratio=new Vector3(hm.bounds.size.x/lm.bounds.size.x,hm.bounds.size.y/lm.bounds.size.y,hm.bounds.size.z/lm.bounds.size.z);
                Require(float.IsFinite(ratio.x)&&float.IsFinite(ratio.y)&&float.IsFinite(ratio.z),"Degenerate low mesh bounds");
                low.transform.localScale=Vector3.Scale(high.transform.localScale,ratio);low.transform.position+=hb.center-Bounds(low).center;
                PrefabUtility.RecordPrefabInstancePropertyModifications(low.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(high.transform);
                var group=Undo.AddComponent<LODGroup>(holder);group.SetLODs(new[]{new LOD(.25f,high.GetComponentsInChildren<Renderer>(true)),new LOD(.01f,low.GetComponentsInChildren<Renderer>(true))});group.RecalculateBounds();
                var camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c=>c.gameObject.scene==scene&&c.enabled&&c.targetTexture==null);
                float k=group.size*Mathf.Max(holder.transform.lossyScale.x,holder.transform.lossyScale.y,holder.transform.lossyScale.z)*QualitySettings.lodBias/(2*Mathf.Tan((camera?camera.fieldOfView:60)*Mathf.Deg2Rad*.5f));
                float transition=Distances[i];
                var levels=group.GetLODs();levels[0].screenRelativeTransitionHeight=Mathf.Clamp(k/transition,.02f,.8f);levels[1].screenRelativeTransitionHeight=Mathf.Clamp(k/120,.001f,levels[0].screenRelativeTransitionHeight*.5f);group.SetLODs(levels);
                // Single active level: avoids overlapping surfaces and requires no material edits.
                group.fadeMode=LODFadeMode.None;group.animateCrossFading=false;
                Require(Vector3.Distance(high.transform.position,highPos[i])<.001f&&Quaternion.Angle(high.transform.rotation,highRot[i])<.01f&&Vector3.Distance(high.transform.lossyScale,highScale[i])<.01f,"High model transform changed");
                Require(high.GetComponent<Renderer>().sharedMaterials.SequenceEqual(highMaterials[i]),"High materials changed");
                var lb=Bounds(low);Require(Vector3.Distance(hb.center,lb.center)<.01f&&Vector3.Distance(hb.size,lb.size)<.03f,"Low model silhouette bounds mismatch");
                report.AppendLine($"{high.name}: {Tris(high)} -> {Tris(low)} triangles ({100*(1-(double)Tris(low)/Tris(high)):F2}% reduction); aligned bounds {hb.size:F3}; approximate LOD switch {transition}m, cull 120m at current main FOV/LOD bias; thresholds {levels[0].screenRelativeTransitionHeight:F5}/{levels[1].screenRelativeTransitionHeight:F5}");
            }
            Require(Physics()==physics,"Collider serialization changed");Verify(highs,lows);Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Scene save failed");
            report.AppendLine($"PASS: {highs.Length} native LODGroups; only one level active, no runtime update script. Original transforms/materials and all colliders retained. Existing low templates reused, {lows.Count(l=>!templates.Contains(l))} required low instances cloned. Scene saved. No benchmark/commit/push.");File.WriteAllText(backup+"/Result.txt",report.ToString());Selection.objects=highs.Select(h=>(Object)h.transform.parent.gameObject).ToArray();Debug.Log("STATION_PROPS_LOD_COMPLETE "+backup);
        }
        catch{Undo.RevertAllDownToGroup(undo);throw;}
    }
    static void Verify(GameObject[] highs,GameObject[] lows)
    {
        for(int i=0;i<highs.Length;i++)
        {
            var group=highs[i].GetComponentInParent<LODGroup>();Require(group&&lows[i].GetComponentInParent<LODGroup>()==group,"LOD group references invalid");
            var levels=group.GetLODs();Require(levels.Length==2&&!levels[0].renderers.Intersect(levels[1].renderers).Any()&&levels[0].renderers.Length>0&&levels[1].renderers.Length>0,"LOD renderer overlap/empty level");
            Require(levels[0].renderers.All(r=>r.transform.IsChildOf(highs[i].transform)||r.gameObject==highs[i])&&levels[1].renderers.All(r=>r.transform.IsChildOf(lows[i].transform)||r.gameObject==lows[i]),"Wrong LOD renderer assignment");
        }
    }
}
