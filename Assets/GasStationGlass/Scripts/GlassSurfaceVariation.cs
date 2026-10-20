using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent, per-pane surface variation. Does not allocate materials or cameras.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshRenderer))]
public sealed class GlassSurfaceVariation : MonoBehaviour
{
    public const string ShaderName = "FoggyRoad/Gas Station/Dusty Window";
    [SerializeField, HideInInspector] string stableId;
    [SerializeField, HideInInspector] string editorOwner;
    [SerializeField, HideInInspector] Mesh bakedMesh;
    [SerializeField, HideInInspector] Vector4 paneU, paneV;
    [SerializeField, HideInInspector] Vector3 localNormal = Vector3.forward;
    [SerializeField, HideInInspector] bool geometryReady;
    [SerializeField, HideInInspector] float paneAspect=1;
    [SerializeField, HideInInspector] int surfaceVariant=-1;
    [SerializeField, HideInInspector] string variantIdentity;
    [SerializeField, HideInInspector] int patternRevision;
    public const int VariantCount=12;
    string PatternIdentity => (StableId??"")+(patternRevision==0 ? "" : "#"+patternRevision);
    public int SurfaceVariant => variantIdentity==PatternIdentity && surfaceVariant>=0 && surfaceVariant<VariantCount ? surfaceVariant : (int)(Hash(PatternIdentity)%VariantCount);
    [NonSerialized] string runtimeId;
    MeshRenderer surface;
    MaterialPropertyBlock block;
    static readonly Dictionary<string, GlassSurfaceVariation> Owners = new Dictionary<string, GlassSurfaceVariation>();
    static bool registryPrepared;
    static bool registryPlayMode;
    #if UNITY_EDITOR
    Matrix4x4 editorMatrix;
    bool editorMatrixKnown;
    bool editorLifetimeKnown;
    #endif
    public string StableId => Application.isPlaying && !string.IsNullOrEmpty(runtimeId) ? runtimeId : stableId;
    public Vector4 Pattern { get; private set; }
    public bool GeometryReady => geometryReady;
    public Vector3 LocalNormal => localNormal;
    public Vector3 LocalPoint => bakedMesh ? bakedMesh.bounds.center : Vector3.zero;
    public Renderer Surface => surface ? surface : (surface = GetComponent<MeshRenderer>());

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry()
    {
        Owners.Clear(); registryPrepared = false;
        foreach(var pane in FindObjectsByType<GlassSurfaceVariation>(FindObjectsInactive.Include,FindObjectsSortMode.None)) pane.runtimeId=null;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RestoreWithoutDomainReload()
    {
        foreach(var pane in FindObjectsByType<GlassSurfaceVariation>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(pane.isActiveAndEnabled) { pane.EnsureIdentity(); pane.Apply(); pane.RegisterReflection(); }
    }
    void OnEnable()
    {
        surface=GetComponent<MeshRenderer>(); block=new MaterialPropertyBlock();
        #if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            if(string.IsNullOrEmpty(runtimeId) || editorLifetimeKnown) editorLifetimeKnown=true;
            UnityEditor.EditorApplication.delayCall += EditorRefresh; return;
        }
        #endif
        EnsureIdentity();
        if(!geometryReady) BakeGeometry();
        Apply(); RegisterReflection();
    }
    void OnDestroy()
    {
        ReleaseIdentity();
        foreach(var manager in FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None)) manager.UnregisterWindow(Surface);
    }
    void PrepareRegistry()
    {
        if(registryPrepared && registryPlayMode==Application.isPlaying) return;
        Owners.Clear(); registryPlayMode=Application.isPlaying;
        registryPrepared=true;
        // Reserve disabled originals too, so cloning one cannot steal its identity.
        foreach(var pane in FindObjectsByType<GlassSurfaceVariation>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(pane==this || string.IsNullOrEmpty(pane.StableId)) continue;
            #if UNITY_EDITOR
            if(!Application.isPlaying && !pane.editorLifetimeKnown)
            {
                var token=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(pane).ToString();
                if(string.IsNullOrEmpty(pane.editorOwner) || pane.editorOwner!=token) continue;
            }
            #endif
            if(!Owners.ContainsKey(pane.StableId)) Owners.Add(pane.StableId,pane);
        }
    }
    void EnsureIdentity()
    {
        PrepareRegistry();
        string id=StableId;
        if(string.IsNullOrEmpty(id) || (Owners.TryGetValue(id,out var owner) && owner && owner!=this)) id=Guid.NewGuid().ToString("N");
        if(Application.isPlaying) runtimeId=id; else stableId=id;
        Owners[id]=this;
    }
    void ReleaseIdentity()
    {
        var id=StableId;
        if(!string.IsNullOrEmpty(id) && Owners.TryGetValue(id,out var owner) && owner==this) Owners.Remove(id);
    }
    public void SetStableId(string id)
    {
        if(string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable window identity cannot be empty.",nameof(id));
        PrepareRegistry();
        if(Owners.TryGetValue(id,out var owner) && owner && owner!=this) throw new ArgumentException("Another window already owns this identity.",nameof(id));
        ReleaseIdentity();
        if(Application.isPlaying) runtimeId=id; else stableId=id;
        Owners[id]=this; Apply();
    }
    public static GlassSurfaceVariation RegisterGlass(Renderer renderer)
    {
        if(!(renderer is MeshRenderer)) throw new ArgumentException("A glass pane must have a MeshRenderer.");
        var variation=renderer.GetComponent<GlassSurfaceVariation>();
        if(!variation) variation=renderer.gameObject.AddComponent<GlassSurfaceVariation>();
        #if UNITY_EDITOR
        if(!Application.isPlaying) variation.EditorRefresh(); else
        #endif
        { variation.EnsureIdentity(); if(!variation.geometryReady) variation.BakeGeometry(); variation.Apply(); variation.RegisterReflection(); }
        return variation;
    }
    public static bool UsesGlass(Renderer renderer)
    {
        if(!renderer) return false;
        foreach(var material in renderer.sharedMaterials) if(material && material.shader && material.shader.name==ShaderName) return true;
        return false;
    }
    public void RegisterReflection()
    {
        if(!geometryReady || !UsesGlass(Surface)) return;
        foreach(var manager in FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None))
            if(manager.gameObject.scene==gameObject.scene) { manager.RegisterWindow(Surface,LocalPoint,localNormal); return; }
    }
    public bool BakeGeometry()
    {
        var filter=GetComponent<MeshFilter>();
        if(!filter || !filter.sharedMesh) { geometryReady=false; return false; }
        var mesh=filter.sharedMesh;
        Vector3[] vertices=null, normals=null;
        #if UNITY_EDITOR
        if(!Application.isPlaying && !mesh.isReadable)
        {
            using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh))
            using(var positions=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp))
            using(var directions=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp))
            {
                data[0].GetVertices(positions); vertices=positions.ToArray();
                if(mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal)) { data[0].GetNormals(directions); normals=directions.ToArray(); }
            }
        }
        else
        #endif
        if(mesh.isReadable) { vertices=mesh.vertices; normals=mesh.normals; }
        if(vertices==null || vertices.Length<3 || normals==null || normals.Length==0) { geometryReady=false; return false; }
        localNormal=normals[0].normalized;
        float tolerance=Mathf.Max(0.002f,mesh.bounds.size.magnitude*0.0001f);
        foreach(var vertex in vertices) if(Mathf.Abs(Vector3.Dot(vertex-vertices[0],localNormal))>tolerance) { geometryReady=false; return false; }
        var normalW=transform.localToWorldMatrix.inverse.transpose.MultiplyVector(localNormal).normalized;
        var up=Vector3.ProjectOnPlane(Vector3.up,normalW);
        if(up.sqrMagnitude<0.001f) up=Vector3.ProjectOnPlane(transform.forward,normalW);
        if(up.sqrMagnitude<0.001f) up=Vector3.ProjectOnPlane(transform.right,normalW);
        up.Normalize(); var right=Vector3.Cross(up,normalW).normalized;
        var u=transform.localToWorldMatrix.transpose.MultiplyVector(right);
        var v=transform.localToWorldMatrix.transpose.MultiplyVector(up);
        float minU=float.PositiveInfinity,maxU=float.NegativeInfinity,minV=minU,maxV=maxU;
        foreach(var p in vertices) { float x=Vector3.Dot(p,u),y=Vector3.Dot(p,v); minU=Mathf.Min(minU,x); maxU=Mathf.Max(maxU,x); minV=Mathf.Min(minV,y); maxV=Mathf.Max(maxV,y); }
        if(maxU-minU<0.0001f || maxV-minV<0.0001f) { geometryReady=false; return false; }
        u/=maxU-minU; v/=maxV-minV;
        paneU=new Vector4(u.x,u.y,u.z,-minU/(maxU-minU)); paneV=new Vector4(v.x,v.y,v.z,-minV/(maxV-minV));
        paneAspect=(maxU-minU)/(maxV-minV);
        bakedMesh=mesh; geometryReady=true; return true;
    }
    static uint Hash(string id)
    {
        uint hash=2166136261;
        foreach(char c in id) { hash^=c; hash*=16777619; }
        return hash==0 ? 1 : hash;
    }
    static float Next(ref uint state) { state=state*1664525+1013904223; return (state>>8)*(1f/16777216); }
    public void Apply()
    {
        if(!UsesGlass(Surface)) return;
        if(block==null) block=new MaterialPropertyBlock();
        if(string.IsNullOrEmpty(StableId)) EnsureIdentity();
        #if UNITY_EDITOR
        if(!Application.isPlaying) EnsureVariant();
        #endif
        uint state=Hash(PatternIdentity);
        Pattern=new Vector4(Next(ref state)*97,Next(ref state)*97,Next(ref state),Next(ref state));
        float scale=0.65f+Next(ref state)*0.3f;
        var crop=new Vector4(paneAspect>=1 ? scale : scale*paneAspect,paneAspect>=1 ? scale/paneAspect : scale,Next(ref state),Next(ref state));
        crop.z=0.025f+crop.z*(0.95f-crop.x); crop.w=0.025f+crop.w*(0.95f-crop.y);
        Surface.GetPropertyBlock(block);
        block.SetFloat("_VariationEnabled",geometryReady ? 1 : 0);
        block.SetVector("_PaneU",paneU); block.SetVector("_PaneV",paneV);
        block.SetVector("_SurfaceRandom",Pattern); block.SetVector("_WearCrop",crop);
        block.SetFloat("_SurfaceVariant",SurfaceVariant);
        block.SetFloat("_SurfaceMirror",Pattern.z>0.5f ? 1 : 0);
        block.SetFloat("_DustMultiplier",0.75f+Next(ref state)*0.5f);
        block.SetFloat("_WearMultiplier",0.75f+Next(ref state)*0.5f);
        Surface.SetPropertyBlock(block);
    }
    #if UNITY_EDITOR
    void EnsureVariant()
    {
        if(variantIdentity==PatternIdentity && surfaceVariant>=0 && surfaceVariant<VariantCount) return;
        int initial=(int)(Hash(PatternIdentity)%VariantCount),index=initial;
        var peers=FindObjectsByType<GlassSurfaceVariation>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        for(int attempt=0;attempt<VariantCount;attempt++)
        {
            bool occupied=false;
            foreach(var peer in peers) if(peer!=this && peer.gameObject.scene==gameObject.scene && peer.variantIdentity==peer.PatternIdentity && peer.surfaceVariant==index) { occupied=true; break; }
            if(!occupied) break; index=(index+1)%VariantCount;
        }
        surfaceVariant=index; variantIdentity=PatternIdentity; UnityEditor.EditorUtility.SetDirty(this);
    }
    public void RegenerateSurfacePattern()
    {
        patternRevision++; variantIdentity=null; EditorRefresh();
        UnityEditor.EditorUtility.SetDirty(this); UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
    void OnValidate() { UnityEditor.EditorApplication.delayCall += EditorRefresh; }
    public void EditorRefresh()
    {
        if(!this || Application.isPlaying || UnityEditor.EditorApplication.isCompiling || !gameObject.scene.IsValid()) return;
        // Static batching rewrites object-space vertices, invalidating _PaneU/_PaneV.
        // Keep other static flags, including lighting and occlusion, intact.
        var flags=UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject);
        bool batchingChanged=UsesGlass(Surface) && (flags & UnityEditor.StaticEditorFlags.BatchingStatic)!=0;
        if(batchingChanged)
        {
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(gameObject,flags & ~UnityEditor.StaticEditorFlags.BatchingStatic);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
        }
        string oldId=stableId,oldOwner=editorOwner; int oldVariant=surfaceVariant; Vector4 oldU=paneU,oldV=paneV; var oldMesh=bakedMesh;
        var owner=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this);
        if(owner.targetObjectId!=0 && !string.IsNullOrEmpty(gameObject.scene.path))
        {
            string token=owner.ToString();
            if(!string.IsNullOrEmpty(editorOwner) && editorOwner!=token) { ReleaseIdentity(); stableId=Guid.NewGuid().ToString("N"); }
            editorOwner=token;
        }
        EnsureIdentity(); BakeGeometry();
        var matrix=transform.localToWorldMatrix;
        bool regroup=oldMesh!=bakedMesh || oldU!=paneU || oldV!=paneV || (editorMatrixKnown && editorMatrix!=matrix);
        if(regroup || !UsesGlass(Surface)) foreach(var manager in FindObjectsByType<GasStationPlanarGlass>(FindObjectsSortMode.None)) manager.UnregisterWindow(Surface);
        editorMatrix=matrix; editorMatrixKnown=true;
        Apply(); RegisterReflection();
        if(batchingChanged || oldId!=stableId || oldOwner!=editorOwner || oldVariant!=surfaceVariant || regroup)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
    #endif
}
