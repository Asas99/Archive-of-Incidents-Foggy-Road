using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Deliberately tests real tree triangles near pole hardware, not a cleared strip
// along the power line or the full bounding box of a tree crown.
[InitializeOnLoad]
public static class PowerPoleTreeCleanup
{
    const string Request = "Library/PowerPoleTreeCleanup.request";
    const string Audit = "Recovery/PowerPoleTreeAudit.txt";
    struct Capsule { public Vector3 a, b; public float radius; public string pole; }
    sealed class Part { public Vector3[] vertices; public int[] indices; public Matrix4x4 local; public Bounds bounds; }
    sealed class Hit { public int index; public string pole, reason; public Vector3 position; public TreeInstance tree; }
    sealed class Work { public Terrain terrain; public TreeInstance[] original; public List<Hit> hits = new List<Hit>(); }
    static PowerPoleTreeCleanup() { EditorApplication.update += Poll; }
    static void Require(bool b, string s) { if (!b) throw new InvalidOperationException(s); }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isUpdating || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || StaticOcclusionCulling.isRunning) return;
        var command = File.ReadAllText(Request).Trim(); File.Delete(Request);
        try { Run(command == "APPLY"); }
        catch (Exception e) { File.WriteAllText("Recovery/PowerPoleTreeError.txt", e.ToString()); Debug.LogException(e); }
    }
    static Bounds WorldBounds(Bounds b, Matrix4x4 m)
    {
        var result = new Bounds(m.MultiplyPoint3x4(b.center), Vector3.zero);
        for (int i = 0; i < 8; i++) result.Encapsulate(m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
        return result;
    }
    static Part ReadPart(MeshFilter filter, Transform root)
    {
        var mesh = filter.sharedMesh; Require(mesh, "Missing tree mesh: " + filter.name);
        using (var data = MeshUtility.AcquireReadOnlyMeshData(mesh))
        {
            var md = data[0]; using (var vertices = new NativeArray<Vector3>(md.vertexCount, Allocator.Temp))
            {
                md.GetVertices(vertices); var triangles = new List<int>();
                for (int s = 0; s < md.subMeshCount; s++)
                {
                    if (md.GetSubMesh(s).topology != MeshTopology.Triangles) continue;
                    using (var indices = new NativeArray<int>(md.GetSubMesh(s).indexCount, Allocator.Temp)) { md.GetIndices(indices, s); triangles.AddRange(indices.ToArray()); }
                }
                return new Part { vertices = vertices.ToArray(), indices = triangles.ToArray(), local = root.worldToLocalMatrix * filter.transform.localToWorldMatrix, bounds = mesh.bounds };
            }
        }
    }
    static Part[] Prototype(GameObject prefab)
    {
        var lod = prefab.GetComponentInChildren<LODGroup>(true);
        var renderers = lod && lod.GetLODs().Length > 0 ? lod.GetLODs()[0].renderers : prefab.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray();
        return renderers.Where(r => r && r.GetComponent<MeshFilter>()).Select(r => ReadPart(r.GetComponent<MeshFilter>(), prefab.transform)).ToArray();
    }
    static float PointSegment(Vector3 p, Vector3 a, Vector3 b) { var d=b-a; float t=d.sqrMagnitude>1e-10f?Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude):0; return (p-a-d*t).sqrMagnitude; }
    static float Segments(Vector3 p, Vector3 q, Vector3 a, Vector3 b)
    {
        var d=q-p;var e=b-a;var r=p-a;float dd=Vector3.Dot(d,d),ee=Vector3.Dot(e,e),de=Vector3.Dot(d,e),dr=Vector3.Dot(d,r),er=Vector3.Dot(e,r),s,t;
        if(dd<1e-10f)return PointSegment(p,a,b); if(ee<1e-10f)return PointSegment(a,p,q);
        float den=dd*ee-de*de;s=den>1e-10f?Mathf.Clamp01((de*er-dr*ee)/den):0;t=(de*s+er)/ee;
        if(t<0){t=0;s=Mathf.Clamp01(-dr/dd);}else if(t>1){t=1;s=Mathf.Clamp01((de-dr)/dd);}
        return (p+d*s-a-e*t).sqrMagnitude;
    }
    static float PointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        var ab=b-a;var ac=c-a;var normal=Vector3.Cross(ab,ac);float nn=normal.sqrMagnitude;
        if(nn>1e-12f)
        {
            var projected=p-normal*(Vector3.Dot(p-a,normal)/nn);
            if(Vector3.Dot(Vector3.Cross(b-a,projected-a),normal)>=0 && Vector3.Dot(Vector3.Cross(c-b,projected-b),normal)>=0 && Vector3.Dot(Vector3.Cross(a-c,projected-c),normal)>=0)
                return (p-projected).sqrMagnitude;
        }
        return Mathf.Min(PointSegment(p,a,b),Mathf.Min(PointSegment(p,b,c),PointSegment(p,c,a)));
    }
    static float TriangleSegment(Vector3 a, Vector3 b, Vector3 c, Vector3 p, Vector3 q)
    {
        var n=Vector3.Cross(b-a,c-a);float den=Vector3.Dot(n,q-p);
        if(Mathf.Abs(den)>1e-8f)
        {
            float t=Vector3.Dot(n,a-p)/den;
            if(t>=0&&t<=1&&PointTriangle(Vector3.Lerp(p,q,t),a,b,c)<1e-8f)return 0;
        }
        return Mathf.Min(Mathf.Min(PointTriangle(p,a,b,c),PointTriangle(q,a,b,c)),Mathf.Min(Segments(p,q,a,b),Mathf.Min(Segments(p,q,b,c),Segments(p,q,c,a))));
    }
    static bool Intersects(Part part, Matrix4x4 tree, Capsule capsule)
    {
        var matrix=tree*part.local;var bound=WorldBounds(part.bounds,matrix);bound.Expand(capsule.radius*2);
        var cb=new Bounds(capsule.a,Vector3.zero);cb.Encapsulate(capsule.b);
        if(!bound.Intersects(cb))return false;
        float rr=capsule.radius*capsule.radius;
        var v=part.vertices;var ids=part.indices;
        for(int i=0;i+2<ids.Length;i+=3)
        {
            var a=matrix.MultiplyPoint3x4(v[ids[i]]);var b=matrix.MultiplyPoint3x4(v[ids[i+1]]);var c=matrix.MultiplyPoint3x4(v[ids[i+2]]);
            var tri=new Bounds(a,Vector3.zero);tri.Encapsulate(b);tri.Encapsulate(c);tri.Expand(capsule.radius*2);
            if(tri.Intersects(cb)&&TriangleSegment(a,b,c,capsule.a,capsule.b)<=rr)return true;
        }
        return false;
    }
    public static void Run(bool apply)
    {
        var scene=SceneManager.GetActiveScene();
        var line=Object.FindObjectsByType<FoggyRoad_PowerLine>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(l=>l.gameObject.scene==scene&&l.transform.parent&&l.transform.parent.name=="ElektrikDirekleri_Auto");
        Require(line.poles.Count==98&&line.poles.All(p=>p),"Expected existing 98-pole line");
        var caps=new List<Capsule>();
        foreach(var p in line.poles)
        {
            var shaft=p.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Cylinder").bounds;
            caps.Add(new Capsule{a=new Vector3(shaft.center.x,shaft.min.y,shaft.center.z),b=new Vector3(shaft.center.x,shaft.max.y,shaft.center.z),radius=Mathf.Max(shaft.extents.x,shaft.extents.z)+.35f,pole=p.name});
            var anchors=line.CollectAnchors(p);Require(anchors.Count>=2,"Pole anchors missing");
            caps.Add(new Capsule{a=anchors[0],b=anchors[anchors.Count-1],radius=.45f,pole=p.name});
        }
        var works=new List<Work>();var parts=new Dictionary<GameObject,Part[]>();var log=new StringBuilder();
        log.AppendLine("Tree audit: 1.6m trunk-center clearance; actual foliage triangles within 0.35m of shaft / 0.45m of crossarm. No cable-span clearing.");
        foreach(var terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).Where(t=>t.gameObject.scene==scene&&t.terrainData).GroupBy(t=>t.terrainData).Select(g=>g.First()))
        {
            var data=terrain.terrainData;var work=new Work{terrain=terrain,original=data.treeInstances};var prototypes=data.treePrototypes;
            for(int i=0;i<work.original.Length;i++)
            {
                var instance=work.original[i];var prefab=prototypes[instance.prototypeIndex].prefab;if(!prefab)continue;
                // Only actual tall tree prototypes, never fern/grass/shrub layers.
                if(!parts.TryGetValue(prefab,out var meshParts))
                {
                    meshParts=Prototype(prefab);parts[prefab]=meshParts;
                    log.AppendLine($"Prototype {prefab.name}, root scale {prefab.transform.localScale}, parts {meshParts.Length}, triangles {meshParts.Sum(m=>m.indices.Length/3)}");
                }
                var pos=terrain.transform.position+Vector3.Scale(instance.position,data.size);
                var matrix=Matrix4x4.TRS(pos,Quaternion.Euler(0,instance.rotation*Mathf.Rad2Deg,0),new Vector3(instance.widthScale,instance.heightScale,instance.widthScale));
                if(meshParts.Length==0)continue;
                var bounds=WorldBounds(meshParts[0].bounds,matrix*meshParts[0].local);foreach(var part in meshParts.Skip(1))bounds.Encapsulate(WorldBounds(part.bounds,matrix*part.local));
                if(bounds.size.y<5)continue;
                var nearby=caps.Where(c=>bounds.SqrDistance(c.a)<1||bounds.SqrDistance(c.b)<1||bounds.Intersects(new Bounds((c.a+c.b)*.5f,new Vector3(Mathf.Abs(c.b.x-c.a.x)+2,Mathf.Abs(c.b.y-c.a.y)+2,Mathf.Abs(c.b.z-c.a.z)+2)))).ToArray();
                foreach(var c in nearby)
                {
                    bool trunk=new Vector2(pos.x-c.a.x,pos.z-c.a.z).sqrMagnitude<1.6f*1.6f&&bounds.max.y>c.a.y&&bounds.min.y<c.b.y;
                    if(!trunk&&!meshParts.Any(part=>Intersects(part,matrix,c)))continue;
                    work.hits.Add(new Hit{index=i,pole=c.pole,reason=trunk?"trunk too close":"foliage/hardware intersection",position=pos,tree=instance});break;
                }
            }
            works.Add(work);log.AppendLine($"{terrain.name}: {work.original.Length} trees; remove candidates {work.hits.Count}");
            foreach(var hit in work.hits)log.AppendLine($"  tree {hit.index}, {hit.position:F3}, {hit.pole}: {hit.reason}");
        }
        int total=works.Sum(w=>w.original.Length),count=works.Sum(w=>w.hits.Count);log.AppendLine($"TOTAL {count}/{total} ({100f*count/Mathf.Max(1,total):F2}%)");
        File.WriteAllText(Audit,log.ToString());if(!apply)return;
        Require(count<=Mathf.Max(1,total*.03f)&&count<=250,"Cleanup too broad; inspect audit before applying");
        if(count==0){Debug.Log("No pole/tree conflicts found");return;}
        RoadWidthInstallation.ValidateInstalled();
        string backup="Recovery/PowerPoleTrees_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
        Require(EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true),"Scene backup failed");
        File.WriteAllText("Library/PowerPoleTreesBackup.path",backup);File.WriteAllText(backup+"/Report.txt",log.ToString());
        var roadState=RoadWidthInstallation.State;var oldRemovals=roadState.terrains.Select(t=>t.approvedTreeRemovals).ToArray();
        File.Copy(AssetDatabase.GetAssetPath(roadState),backup+"/RoadWidthState.before.asset");
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Remove only trees intersecting power poles");
        Undo.RegisterCompleteObjectUndo(roadState,"Record approved pole/tree cleanup");
        try
        {
            foreach(var w in works.Where(w=>w.hits.Count>0))
            {
                var data=w.terrain.terrainData;string path=AssetDatabase.GetAssetPath(data);
                File.Copy(path,backup+"/"+data.GetInstanceID()+"_"+Path.GetFileName(path));
                Undo.RegisterCompleteObjectUndo(data,"Remove pole-conflicting trees");
                var remove=new HashSet<int>(w.hits.Select(h=>h.index));var keep=w.original.Where((t,i)=>!remove.Contains(i)).ToArray();
                data.SetTreeInstances(keep,false);EditorUtility.SetDirty(data);w.terrain.Flush();
                Require(data.treeInstances.SequenceEqual(keep),"Retained tree data changed");
                roadState.terrains.Single(t=>t.generated==data).approvedTreeRemovals+=w.hits.Count;
            }
            EditorUtility.SetDirty(roadState);
            RoadWidthInstallation.ValidateInstalled();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(undo);
            File.WriteAllText(backup+"/Applied.txt",$"Removed {count} trees; retained {total-count}. All retained tree records unchanged. Scene saved.\n");
            RoadWidthInstallation.RebuildOcclusionForGeometryChange();Debug.Log("POWER_POLE_TREES_CLEANED "+backup);
        }
        catch{for(int i=0;i<oldRemovals.Length;i++)roadState.terrains[i].approvedTreeRemovals=oldRemovals[i];EditorUtility.SetDirty(roadState);foreach(var w in works.Where(w=>w.hits.Count>0)){w.terrain.terrainData.SetTreeInstances(w.original,false);EditorUtility.SetDirty(w.terrain.terrainData);w.terrain.Flush();}AssetDatabase.SaveAssets();throw;}
    }
}
