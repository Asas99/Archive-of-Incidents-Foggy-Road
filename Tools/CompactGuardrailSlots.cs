using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CompactGuardrailSlots
{
    sealed class Entry {public MeshRenderer renderer;public MeshFilter filter;public Mesh source,compact;public Material[] materials;public string target;}
    public static string Main()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Edit Mode required");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/game prototype scene.unity"||scene.isDirty)throw new InvalidOperationException("Expected saved gameplay scene");
        var entries=new List<Entry>();
        string folder="Assets/SceneOptimization/Generated/DrawCallCompaction_20261113";
        if(AssetDatabase.IsValidFolder(folder))throw new InvalidOperationException("Output folder already exists; inspect before rerunning");
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if(r.gameObject.scene!=scene||!r.name.StartsWith("GRPRO",StringComparison.Ordinal)||r.HasPropertyBlock()||r.isPartOfStaticBatch)continue;
            var f=r.GetComponent<MeshFilter>();var mesh=f?f.sharedMesh:null;var mats=r.sharedMaterials;
            if(!mesh||!mesh.isReadable||mats.Length<2||mesh.subMeshCount!=mats.Length||!mats[0]||mats[0].renderQueue>=2500||mats.Any(m=>!ReferenceEquals(m,mats[0])))continue;
            if(Enumerable.Range(0,mesh.subMeshCount).Any(i=>mesh.GetTopology(i)!=MeshTopology.Triangles))continue;
            var indices=new List<int>();
            for(int i=0;i<mesh.subMeshCount;i++)indices.AddRange(mesh.GetIndices(i,true));
            var copy=UnityEngine.Object.Instantiate(mesh);copy.name=mesh.name+"_SingleMaterial";copy.subMeshCount=1;
            copy.SetIndices(indices.ToArray(),MeshTopology.Triangles,0,false,0);copy.bounds=mesh.bounds;
            if(copy.vertexCount!=mesh.vertexCount||copy.bounds!=mesh.bounds||!copy.GetIndices(0,true).SequenceEqual(indices))throw new InvalidOperationException("Geometry/index invariant failed: "+r.name);
            using(var data=Mesh.AcquireReadOnlyMeshData(new[]{mesh,copy}))
            {
                if(data[0].vertexBufferCount!=data[1].vertexBufferCount)throw new InvalidOperationException("Vertex stream count changed");
                for(int stream=0;stream<data[0].vertexBufferCount;stream++)
                {
                    var a=data[0].GetVertexData<byte>(stream);var b=data[1].GetVertexData<byte>(stream);
                    if(a.Length!=b.Length)throw new InvalidOperationException("Vertex stream size changed");
                    for(int k=0;k<a.Length;k++)if(a[k]!=b[k])throw new InvalidOperationException("Vertex attributes changed");
                }
            }
            entries.Add(new Entry{renderer=r,filter=f,source=mesh,compact=copy,materials=mats,target=folder+"/"+entries.Count.ToString("D3")+"_"+mesh.name+".asset"});
        }
        if(entries.Count==0)return "No duplicate opaque guardrail slots found";
        string reportRoot="Recovery/QualityTrials_20261113";
        Directory.CreateDirectory(reportRoot+"/SourceBackup");
        File.Copy(scene.path,reportRoot+"/SourceBackup/scene_before_compact.unity",true);
        var colliders=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).Where(c=>c.gameObject.scene==scene).ToArray();
        var colliderState=colliders.Select(c=>EditorJsonUtility.ToJson(c)).ToArray();
        var matrices=entries.Select(e=>e.renderer.localToWorldMatrix).ToArray();
        string parent="Assets/SceneOptimization/Generated";AssetDatabase.CreateFolder(parent,"DrawCallCompaction_20261113");
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Compact identical guardrail material slots");
        var created=new List<string>();var report=new StringBuilder("renderer_global_id\toriginal_mesh\tcompact_mesh\told_slots\tnew_slots\tvertices\tindices\n");
        try
        {
            foreach(var e in entries)
            {
                AssetDatabase.CreateAsset(e.compact,e.target);created.Add(e.target);
                Undo.RecordObjects(new UnityEngine.Object[]{e.filter,e.renderer},"Compact guardrail slots");
                e.filter.sharedMesh=e.compact;e.renderer.sharedMaterials=new[]{e.materials[0]};
                PrefabUtility.RecordPrefabInstancePropertyModifications(e.filter);PrefabUtility.RecordPrefabInstancePropertyModifications(e.renderer);
                EditorUtility.SetDirty(e.filter);EditorUtility.SetDirty(e.renderer);
                report.AppendLine(GlobalObjectId.GetGlobalObjectIdSlow(e.renderer)+"\t"+AssetDatabase.GetAssetPath(e.source)+"\t"+e.target+"\t"+e.materials.Length+"\t1\t"+e.source.vertexCount+"\t"+e.compact.GetIndexCount(0));
            }
            for(int i=0;i<entries.Count;i++)if(entries[i].renderer.localToWorldMatrix!=matrices[i])throw new InvalidOperationException("Transform changed");
            for(int i=0;i<colliders.Length;i++)if(EditorJsonUtility.ToJson(colliders[i])!=colliderState[i])throw new InvalidOperationException("Collider changed");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Scene save failed");
            Undo.CollapseUndoOperations(undo);
            File.WriteAllText(reportRoot+"/AppliedGuardrailSlots.tsv",report.ToString());
            string result="Saved "+entries.Count+" guardrail renderers with 2 -> 1 identical opaque material slots. All vertex streams, ordered triangle indices, bounds, transforms and collider serialization verified unchanged.";
            File.WriteAllText(reportRoot+"/AppliedGuardrailSlots.txt",result);
            return result;
        }
        catch
        {
            Undo.RevertAllDownToGroup(undo);
            foreach(var path in created)AssetDatabase.DeleteAsset(path);
            throw;
        }
    }
}