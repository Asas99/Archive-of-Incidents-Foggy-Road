using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class StationColliderSetup
{
    const string Request="Library/StationCollider.request";
    const string ReportPath="Recovery/StationColliders";
    [Serializable] public class Row
    {
        public string path,mesh,action,reason; public int triangles; public bool active,moving;
        public Vector3 size,worldSize; public int existing;
    }
    [Serializable] public class Inventory { public string root,scene; public bool dirty; public Row[] rows; }
    sealed class Item { public MeshFilter filter; public Row row; public Bounds bounds; }
    static readonly Dictionary<Mesh,bool> boxes=new Dictionary<Mesh,bool>();
    static StationColliderSetup() { EditorApplication.update+=Requests; }
    static void Requests()
    {
        if(!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string command=File.ReadAllText(Request).Trim(); File.Delete(Request);
        try { if(command=="apply") Apply(); else Audit(); }
        catch(Exception e) { Directory.CreateDirectory(ReportPath); File.WriteAllText(ReportPath+"/Error.txt",e.ToString()); Debug.LogException(e); }
    }
    static GameObject Root()
    {
        var selected=Selection.activeGameObject;
        if(selected && selected.name=="benzinlik" && selected.scene.IsValid()) return selected;
        var roots=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(t=>t.name=="benzinlik" && t.gameObject.scene==SceneManager.GetActiveScene()).ToArray();
        if(roots.Length!=1) throw new InvalidOperationException("Select the single benzinlik root in the active scene. Found "+roots.Length);
        return roots[0].gameObject;
    }
    static string PathOf(Transform t)
    {
        string path=t.name; while(t.parent) {t=t.parent;path=t.name+"/"+path;} return path;
    }
    static bool Contains(BoxCollider box,Bounds localBounds,Transform meshTransform)
    {
        if(!box.enabled || box.isTrigger) return false;
        var matrix=box.transform.worldToLocalMatrix*meshTransform.localToWorldMatrix;
        var bounds=new Bounds(box.center,box.size+Vector3.one*0.0001f);
        for(int i=0;i<8;i++)
            if(!bounds.Contains(matrix.MultiplyPoint3x4(localBounds.center+Vector3.Scale(localBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))))) return false;
        return true;
    }
    static Collider CoveringCollider(MeshFilter mesh)
    {
        var own=mesh.GetComponents<Collider>(); if(own.Length>0) return own[0];
        foreach(var box in mesh.GetComponentsInParent<BoxCollider>(true)) if(Contains(box,mesh.sharedMesh.bounds,mesh.transform)) return box;
        // A child/parent mesh collider counts only when it represents exactly the same geometry and transform.
        foreach(var c in mesh.GetComponentsInChildren<MeshCollider>(true).Concat(mesh.GetComponentsInParent<MeshCollider>(true)))
        {
            if(!c.enabled || c.isTrigger || c.sharedMesh!=mesh.sharedMesh) continue;
            var delta=c.transform.worldToLocalMatrix*mesh.transform.localToWorldMatrix; bool equal=true;
            for(int i=0;i<16;i++) if(Mathf.Abs(delta[i]-Matrix4x4.identity[i])>0.00001f) {equal=false;break;}
            if(equal) return c;
        }
        return null;
    }
    static bool Moving(Transform t)
    {
        if(t.GetComponentInParent<Rigidbody>() || t.GetComponentInParent<Animator>() || t.GetComponentInParent<Animation>()) return true;
        for(var a=t;a;a=a.parent)
            foreach(var behaviour in a.GetComponents<MonoBehaviour>())
                if(behaviour && (behaviour.GetType().Name.IndexOf("Door",StringComparison.OrdinalIgnoreCase)>=0 || behaviour.GetType().Name.IndexOf("Kapi",StringComparison.OrdinalIgnoreCase)>=0)) return true;
        return false;
    }
    static int Triangles(Mesh m)
    { long count=0; for(int i=0;i<m.subMeshCount;i++) if(m.GetTopology(i)==MeshTopology.Triangles) count+=(long)m.GetIndexCount(i)/3; return (int)Math.Min(int.MaxValue,count); }
    static bool BoxLike(Mesh mesh)
    {
        if(boxes.TryGetValue(mesh,out bool result)) return result;
        var b=mesh.bounds; var size=b.size;
        if(size.x<=0 || size.y<=0 || size.z<=0) return boxes[mesh]=false;
        using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
        using(var verts=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
        {
            var d=data[0]; d.GetVertices(verts); var normalized=new Vector3[verts.Length]; int onSurface=0;
            for(int i=0;i<verts.Length;i++)
            {
                var p=verts[i]-b.center; p=new Vector3(p.x/b.extents.x,p.y/b.extents.y,p.z/b.extents.z);normalized[i]=p;
                if(Mathf.Max(Mathf.Abs(p.x),Mathf.Max(Mathf.Abs(p.y),Mathf.Abs(p.z)))>0.97f) onSurface++;
            }
            double signed=0; bool wide=d.indexFormat==IndexFormat.UInt32;
            var indices16=wide ? default : d.GetIndexData<ushort>(); var indices32=wide ? d.GetIndexData<uint>() : default;
            for(int sub=0;sub<d.subMeshCount;sub++)
            {
                var part=d.GetSubMesh(sub); if(part.topology!=MeshTopology.Triangles) return boxes[mesh]=false;
                for(int j=part.indexStart;j<part.indexStart+part.indexCount;j+=3)
                {
                    int a=(wide?(int)indices32[j]:indices16[j])+part.baseVertex;
                    int c=(wide?(int)indices32[j+1]:indices16[j+1])+part.baseVertex;
                    int e=(wide?(int)indices32[j+2]:indices16[j+2])+part.baseVertex;
                    signed+=Vector3.Dot(normalized[a],Vector3.Cross(normalized[c],normalized[e]))/6.0;
                }
            }
            double fill=Math.Abs(signed)/8;
            result=fill>=0.95 && fill<=1.05 && onSurface>verts.Length*0.9;
        }
        return boxes[mesh]=result;
    }
    static List<Item> Inspect(GameObject root)
    {
        boxes.Clear();var items=new List<Item>();
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.GetComponent<MeshRenderer>()) continue;
            var r=new Row {path=PathOf(mf.transform),active=mf.gameObject.activeInHierarchy,moving=Moving(mf.transform),existing=mf.GetComponents<Collider>().Length};
            var item=new Item{filter=mf,row=r}; items.Add(item);
            var mesh=mf.sharedMesh;
            if(!mesh) {r.action="Unresolved";r.reason="Missing mesh";continue;}
            r.mesh=AssetDatabase.GetAssetPath(mesh);r.triangles=Triangles(mesh);r.size=mesh.bounds.size;
            r.worldSize=Vector3.Scale(r.size,new Vector3(Mathf.Abs(mf.transform.lossyScale.x),Mathf.Abs(mf.transform.lossyScale.y),Mathf.Abs(mf.transform.lossyScale.z)));item.bounds=mesh.bounds;
            var covered=CoveringCollider(mf);
            if(covered) {r.action="Keep";r.reason="Existing "+covered.GetType().Name+" at "+PathOf(covered.transform)+(covered.enabled&&!covered.isTrigger?"":" [WARNING disabled/trigger]");continue;}
            if(r.triangles==0) {r.action="Unresolved";r.reason="No mesh triangles";continue;}
            bool glass=mf.GetComponent<GlassSurfaceVariation>() || mf.name.StartsWith("Plane",StringComparison.OrdinalIgnoreCase) && mf.GetComponent<Renderer>().sharedMaterials.Any(m=>m&&m.shader&&m.shader.name==GlassSurfaceVariation.ShaderName);
            string label=(mf.name+" "+r.mesh).ToLowerInvariant();
            bool opening=new[]{"threshold","frame","window","portal","arch","kapı kas","kapi kas","pencer"}.Any(label.Contains);
            bool simple=glass || (!opening && BoxLike(mesh));
            if(simple) {r.action="Box";r.reason=glass?"Flat glass, minimum 1cm world thickness":"Closed box geometry verified by volume and surface occupancy";}
            else if(r.moving) {r.action="Unresolved";r.reason="Complex moving geometry requires a fitted compound collider";}
            else {r.action="Mesh";r.reason=opening?"Preserve openings":"Complex static geometry";}
        }
        return items;
    }
    static void Write(GameObject root,List<Item> items,string phase)
    {
        Directory.CreateDirectory(ReportPath);
        var inventory=new Inventory{root=PathOf(root.transform),scene=root.scene.path,dirty=root.scene.isDirty,rows=items.Select(i=>i.row).ToArray()};
        File.WriteAllText(ReportPath+"/"+phase+".json",JsonUtility.ToJson(inventory,true));
        var text=new StringBuilder(phase+" "+DateTime.Now.ToString("s")+" root="+inventory.root+"\n");
        foreach(var g in items.GroupBy(i=>i.row.action)) text.AppendLine(g.Key+"="+g.Count());
        foreach(var i in items) text.AppendLine(i.row.action+" | "+i.row.path+" | tris="+i.row.triangles+" | "+i.row.reason);
        foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) text.AppendLine("Unresolved skinned mesh | "+PathOf(skin.transform));
        File.WriteAllText(ReportPath+"/"+phase+".txt",text.ToString());
    }
    [MenuItem("Tools/Foggy Road/Station Colliders/Audit Selected Station")]
    public static void Audit() {var root=Root();Write(root,Inspect(root),"Audit");}
    [MenuItem("Tools/Foggy Road/Station Colliders/Add Missing Colliders")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var root=Root(); var items=Inspect(root);Write(root,items,"Before");
        Directory.CreateDirectory("Assets/GasStationGlass/Backups");
        string backup="Assets/GasStationGlass/Backups/BeforeStationColliders_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(root.scene,backup,true)) throw new IOException("Scene backup failed.");
        var existing=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
        var transforms=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));
        var outside=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>!c.transform.IsChildOf(root.transform)).Select(c=>c.GetInstanceID()).OrderBy(id=>id).ToArray();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Add missing station colliders");
        try
        {
            foreach(var item in items)
            {
                Collider added=null;
                if(item.row.action=="Box")
                {
                    var box=Undo.AddComponent<BoxCollider>(item.filter.gameObject);box.center=item.bounds.center;var size=item.bounds.size;
                    if(item.filter.GetComponent<GlassSurfaceVariation>())
                    {
                        var scale=item.filter.transform.lossyScale;
                        for(int i=0;i<3;i++) size[i]=Mathf.Max(size[i],0.01f/Mathf.Max(0.000001f,Mathf.Abs(scale[i])));
                    }
                    box.size=size;added=box;
                }
                else if(item.row.action=="Mesh")
                {
                    var mesh=Undo.AddComponent<MeshCollider>(item.filter.gameObject);mesh.convex=false;mesh.sharedMesh=item.filter.sharedMesh;added=mesh;
                }
                if(added) {added.enabled=true;added.isTrigger=false;PrefabUtility.RecordPrefabInstancePropertyModifications(added);EditorUtility.SetDirty(added);}
            }
            Physics.SyncTransforms();
            foreach(var pair in existing) if(!pair.Key || EditorJsonUtility.ToJson(pair.Key)!=pair.Value) throw new InvalidOperationException("Existing collider changed");
            foreach(var pair in transforms) if(!pair.Key || EditorJsonUtility.ToJson(pair.Key)!=pair.Value) throw new InvalidOperationException("Scene transform changed");
            var afterOutside=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>!c.transform.IsChildOf(root.transform)).Select(c=>c.GetInstanceID()).OrderBy(id=>id);
            if(!outside.SequenceEqual(afterOutside)) throw new InvalidOperationException("Collider outside station changed");
            var after=Inspect(root);Write(root,after,"After");
            if(after.Any(i=>i.row.action=="Box" || i.row.action=="Mesh")) throw new InvalidOperationException("Idempotence check failed");
            EditorSceneManager.MarkSceneDirty(root.scene);
            if(!EditorSceneManager.SaveScene(root.scene)) throw new IOException("Scene save failed");
            File.WriteAllText(ReportPath+"/Applied.txt","PASS colliders added; existing colliders and all scene transforms preserved; outside station unchanged; second run adds zero.\nBackup="+backup+"\nUnresolved="+after.Count(i=>i.row.action=="Unresolved"));
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
        finally {Undo.CollapseUndoOperations(undo);}
    }
}

