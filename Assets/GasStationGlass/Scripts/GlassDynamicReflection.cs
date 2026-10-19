using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class GasStationPlanarGlass
{
    public enum CaptureContent { FullScene, DynamicObjects }
    public CaptureContent captureContent=CaptureContent.FullScene;
    public int dynamicRendererIndex=-1;
    public GlassReflectionOcclusion reflectionOcclusion;
    public Material occlusionMaterial;
    public enum DynamicDebug { Composite, Coverage, DynamicColour, OcclusionDepth }
    public DynamicDebug dynamicDebug;
    public int DynamicModelCount{get;private set;}
    public long DynamicTriangles{get;private set;}
    public string LastUpdateReason{get;private set;}="Baked";
    public static GasStationPlanarGlass DynamicCapture{get;private set;}
    public Camera CaptureCamera{get;private set;}
    sealed class DrawMaterials {public int revision;public Material[] materials;public int[] passes;}
    readonly Dictionary<Renderer,DrawMaterials> preparedMaterials=new Dictionary<Renderer,DrawMaterials>();
    readonly List<ReflectionActor> captureActors=new List<ReflectionActor>();
    readonly Dictionary<Renderer,Mesh> dynamicMeshes=new Dictionary<Renderer,Mesh>();
    readonly List<Renderer> dynamicModels=new List<Renderer>();
    sealed class DynamicHistory
    {
        public readonly List<Matrix4x4> matrices=new List<Matrix4x4>();
        public readonly List<Renderer> models=new List<Renderer>();
        public readonly List<Matrix4x4> occluders=new List<Matrix4x4>();
        public Vector3 position;public Quaternion rotation;public Matrix4x4 projection;
        public int lights;public bool valid;
    }
    readonly Dictionary<WindowGroup,DynamicHistory> histories=new Dictionary<WindowGroup,DynamicHistory>();
    DynamicHistory History(WindowGroup group)
    {
        if(!histories.TryGetValue(group,out var state)){state=new DynamicHistory();histories.Add(group,state);}
        return state;
    }
    readonly Dictionary<Material,Material> opaqueMaterials=new Dictionary<Material,Material>();
    readonly Plane[] dynamicFrustum=new Plane[6];
    WindowGroup preferredGroup;
    bool motionValid;
    static bool Lean(GasStationPlanarGlass manager)=>manager.captureContent==CaptureContent.DynamicObjects;

    void GatherDynamicModels(WindowGroup group,Camera source)
    {
        dynamicModels.Clear();dynamicMeshes.Clear();captureActors.Clear();
        var point=group.plane.TransformPoint(group.localPoint);var normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
        var mirror=ReflectionMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,point)));
        GeometryUtility.CalculateFrustumPlanes(ReflectionProjection(source,point)*source.worldToCameraMatrix*mirror,dynamicFrustum);
        foreach(var actor in ReflectionActor.Registered)
        {
            if(!actor||!actor.isActiveAndEnabled||actor.gameObject.scene!=gameObject.scene||!actor.TryGetBounds(reflectionLayers.value,Time.realtimeSinceStartupAsDouble,out var actorBounds,out _,false)||!(nearProfileEnabled&&UsesAtlas?CaptureVisibility(group,actor,actorBounds,source)>0:CaptureActorInRange(group,actor,actorBounds)))continue;
            if(UsesAtlas&&actor.LowDrawParts.Count>0)
            {
                bool visible=false;
                foreach(var part in actor.LowDrawParts)if(part.anchor&&part.anchor.gameObject.activeInHierarchy&&part.mesh&&GeometryUtility.TestPlanesAABB(dynamicFrustum,TransformBounds(part.mesh.bounds,part.Matrix)))
                {
                    var marker=part.anchor.GetComponent<Renderer>();if(marker&&!dynamicModels.Contains(marker))dynamicModels.Add(marker);visible=true;
                }
                if(visible)captureActors.Add(actor);
                continue;
            }
            foreach(var r in actor.ReflectionDrawSources)
            {
                if(!r||!r.gameObject.activeInHierarchy||(reflectionLayers.value&(1<<r.gameObject.layer))==0||r.forceRenderingOff||r.shadowCastingMode==ShadowCastingMode.ShadowsOnly||r.GetComponent<GlassSurfaceVariation>()||r.GetComponentInParent<Camera>()||!GeometryUtility.TestPlanesAABB(dynamicFrustum,r.bounds))continue;
                if(!dynamicModels.Contains(r)){dynamicModels.Add(r);var mesh=actor.ReflectionMesh(r);if(mesh)dynamicMeshes[r]=mesh;if(!captureActors.Contains(actor))captureActors.Add(actor);PrepareMaterials(r,actor.RenderRevision);}
            }
        }
        dynamicModels.Sort((a,b)=>a.GetInstanceID().CompareTo(b.GetInstanceID()));DynamicModelCount=dynamicModels.Count;
        int cars=0,npcs=0,doors=0,other=0;foreach(var a in captureActors){if(a.GetComponent<TwoSidedDoor>())doors++;else if(a.CompareTag("NPC"))npcs++;else if(a.CompareTag("Car"))cars++;else other++;}
        LiveActorSummary=$"Car={cars}, NPC={npcs}, Door={doors}, Other={other}";
    }
    bool ReflectionViewChanged(WindowGroup group,Camera source)
    {
        var state=History(group);
        return !group.ready||!state.valid||
            (source.transform.position-state.position).sqrMagnitude>0.000001f||
            Quaternion.Angle(source.transform.rotation,state.rotation)>.01f||source.projectionMatrix!=state.projection;
    }
    bool DynamicNeedsCapture(WindowGroup group,Camera source)
    {
        var state=History(group);
        GatherDynamicModels(group,source);
        if(!motionValid||!state.valid||!group.ready){LastUpdateReason="Initial image / group / side";return true;}
        if((source.transform.position-state.position).sqrMagnitude>0.000001f||Quaternion.Angle(source.transform.rotation,state.rotation)>0.01f||source.projectionMatrix!=state.projection){LastUpdateReason="Camera movement";return true;}
        if(state.models.Count!=dynamicModels.Count){LastUpdateReason="Actor spawn / despawn / visibility";return true;}
        for(int i=0;i<dynamicModels.Count;i++)
        {
            var r=dynamicModels[i];if(r!=state.models[i]||!PoseEqual(r.localToWorldMatrix,state.matrices[i])){LastUpdateReason="Actor / door movement";return true;}
            if(r is SkinnedMeshRenderer){var animator=r.GetComponentInParent<Animator>();if(animator&&animator.isActiveAndEnabled&&animator.speed!=0){LastUpdateReason="Visible skinned animation";return true;}}
        }
        // Moving structural sources (notably door pivots) invalidate cached occlusion.
        if(reflectionOcclusion && state.occluders.Count!=reflectionOcclusion.parts.Length)return true;
        if(reflectionOcclusion)for(int i=0;i<reflectionOcclusion.parts.Length;i++)if(reflectionOcclusion.parts[i].source&&!PoseEqual(reflectionOcclusion.parts[i].source.localToWorldMatrix,state.occluders[i]))return true;
        int lights=LightSignature();if(lights!=state.lights){LastUpdateReason="Actor light changed";return true;}
        LastUpdateReason="Cached: camera and reflected content stationary";return false;
    }
    int LightSignature()
    {
        unchecked{int hash=17;if(UsesAtlas){hash=hash*31+RenderSettings.ambientLight.GetHashCode();foreach(var l in atlasLights)if(l)hash=hash*31+l.enabled.GetHashCode()+l.color.GetHashCode()+l.intensity.GetHashCode()+l.transform.localToWorldMatrix.GetHashCode();}foreach(var a in captureActors)if(a&&a.isActiveAndEnabled)hash=hash*31+a.ReflectionLightSignature();return hash;}
    }
    static bool PoseEqual(Matrix4x4 a,Matrix4x4 b)
    {
        if(((Vector3)a.GetColumn(3)-(Vector3)b.GetColumn(3)).sqrMagnitude>0.000001f)return false;
        return Quaternion.Angle(a.rotation,b.rotation)<=0.01f&&((Vector3)a.lossyScale-(Vector3)b.lossyScale).sqrMagnitude<0.000001f;
    }
    void RememberDynamic(WindowGroup group,Camera source)
    {
        var state=History(group);
        state.models.Clear();state.matrices.Clear();foreach(var r in dynamicModels){state.models.Add(r);state.matrices.Add(r.localToWorldMatrix);}
        state.occluders.Clear();if(reflectionOcclusion)foreach(var p in reflectionOcclusion.parts)state.occluders.Add(p.source?p.source.localToWorldMatrix:Matrix4x4.identity);
        state.lights=LightSignature();state.valid=true;state.position=source.transform.position;state.rotation=source.transform.rotation;state.projection=source.projectionMatrix;motionValid=true;
    }
    void PrepareMaterials(Renderer renderer,int revision)
    {
        if(preparedMaterials.TryGetValue(renderer,out var found)&&found.revision==revision)return;
        var sources=renderer.sharedMaterials;var materials=new Material[sources.Length];var passes=new int[sources.Length];
        for(int i=0;i<sources.Length;i++){materials[i]=ActorMaterial(sources[i]);if(materials[i])passes[i]=ForwardPass(materials[i]);}
        preparedMaterials[renderer]=new DrawMaterials{revision=revision,materials=materials,passes=passes};
    }
    Material ActorMaterial(Material original)
    {
        if(!original)return null;
        if(original.renderQueue<3000)return original;
        if(opaqueMaterials.TryGetValue(original,out var found)&&found)return found;
        // Transparent vehicle glazing is an opaque tinted part in the lightweight capture,
        // so coverage remains unambiguous; the player's original material is untouched.
        var copy=new Material(Shader.Find("Universal Render Pipeline/Lit")){hideFlags=HideFlags.HideAndDontSave};
        if(original.HasProperty("_BaseMap"))copy.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));
        copy.SetColor("_BaseColor",original.HasProperty("_BaseColor")?new Color(original.GetColor("_BaseColor").r,original.GetColor("_BaseColor").g,original.GetColor("_BaseColor").b,1):Color.gray);
        opaqueMaterials[original]=copy;return copy;
    }
    int ForwardPass(Material material){int p=material.FindPass("ForwardLit");if(p<0)p=material.FindPass("Unlit");if(p<0)p=material.FindPass("UniversalForward");return Mathf.Max(0,p);}
    public void DrawDynamicReflection(RasterCommandBuffer command)
    {
        DynamicTriangles=0;
        if(reflectionOcclusion&&occlusionMaterial)foreach(var p in reflectionOcclusion.parts)if(p.source&&p.mesh){command.SetGlobalMatrix("_GlassOcclusionMVP",GL.GetGPUProjectionMatrix(CaptureCamera.projectionMatrix,true)*CaptureCamera.worldToCameraMatrix*p.source.localToWorldMatrix);command.DrawMesh(p.mesh,Matrix4x4.identity,occlusionMaterial,0,dynamicDebug==DynamicDebug.OcclusionDepth?1:0);DynamicTriangles+=p.mesh.GetIndexCount(0)/3;}
        if(dynamicDebug==DynamicDebug.OcclusionDepth)return;
        foreach(var r in dynamicModels){var prepared=preparedMaterials[r];var materials=prepared.materials;for(int sub=0;sub<materials.Length;sub++){var m=materials[sub];if(m){if(dynamicMeshes.TryGetValue(r,out var proxy)&&proxy)command.DrawMesh(proxy,r.localToWorldMatrix,m,Mathf.Min(sub,proxy.subMeshCount-1),prepared.passes[sub]);else command.DrawRenderer(r,m,sub,prepared.passes[sub]);}}var mesh=dynamicMeshes.TryGetValue(r,out var low)?low:r is SkinnedMeshRenderer sk?sk.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh)for(int sub=0;sub<mesh.subMeshCount;sub++)DynamicTriangles+=(long)mesh.GetIndexCount(sub)/3;}
    }
    public void DrawDynamicReflection(CommandBuffer command)
    {
        DynamicTriangles=0;
        if(reflectionOcclusion&&occlusionMaterial)foreach(var p in reflectionOcclusion.parts)if(p.source&&p.mesh){command.SetGlobalMatrix("_GlassOcclusionMVP",GL.GetGPUProjectionMatrix(CaptureCamera.projectionMatrix,true)*CaptureCamera.worldToCameraMatrix*p.source.localToWorldMatrix);command.DrawMesh(p.mesh,Matrix4x4.identity,occlusionMaterial,0,dynamicDebug==DynamicDebug.OcclusionDepth?1:0);DynamicTriangles+=p.mesh.GetIndexCount(0)/3;}
        if(dynamicDebug==DynamicDebug.OcclusionDepth)return;
        foreach(var r in dynamicModels){var prepared=preparedMaterials[r];var materials=prepared.materials;for(int sub=0;sub<materials.Length;sub++){var m=materials[sub];if(m){if(dynamicMeshes.TryGetValue(r,out var proxy)&&proxy)command.DrawMesh(proxy,r.localToWorldMatrix,m,Mathf.Min(sub,proxy.subMeshCount-1),prepared.passes[sub]);else command.DrawRenderer(r,m,sub,prepared.passes[sub]);}}var mesh=dynamicMeshes.TryGetValue(r,out var low)?low:r is SkinnedMeshRenderer sk?sk.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh)for(int sub=0;sub<mesh.subMeshCount;sub++)DynamicTriangles+=(long)mesh.GetIndexCount(sub)/3;}
    }
    void ReleaseDynamicMaterials(){foreach(var m in opaqueMaterials.Values)if(m)DestroyResource(m);opaqueMaterials.Clear();preparedMaterials.Clear();histories.Clear();motionValid=false;}
}




