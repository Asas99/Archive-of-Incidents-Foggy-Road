using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed partial class GasStationPlanarGlass
{
    public enum ReflectionMode { Baked, Hybrid, Realtime }
    public ReflectionMode reflectionMode = ReflectionMode.Hybrid;
    // Keep moving reflected actors responsive. 0.1 seconds made cars visibly trail
    // behind their gameplay position before the live reflection was even requested.
    [Min(0.01f)] public float activityInterval=0.02f;
    [Min(0)] public float activityHold=1.5f;
    [Min(0.01f)] public float transitionSeconds=0.4f;
    [Range(1,60)] public float liveUpdatesPerSecond=60;
    [Min(0)] public float idleResourceSeconds=3;
    public bool testActorOcclusion=true;
    public int RegisteredActors => ReflectionActor.RegisteredCount;
    public long TotalCaptures { get; private set; }
    public int CapturesThisTick { get; private set; }
    public string LastActivityReason { get; private set; }="Baked: no activity";
    readonly Plane[] actorFrustum=new Plane[6];
    readonly RaycastHit[] obstructionHits=new RaycastHit[16];
    double previousTick=-1;
    int lastCaptureFrame=-1;
    public void InvalidateReflections()
    {
        previousTick=-1;
        foreach(var group in groups)
        {
            group.ready=false; group.liveWeight=0; group.activity=false; group.lastActivity=-100;
            group.lastVisible=-100; group.nextActivityCheck=0; group.lastUpdate=-100; group.sourceId=0;
            Apply(group,0);
        }
    }
    static readonly int[] PositiveProbeIds=ProbeIds("Positive"),NegativeProbeIds=ProbeIds("Negative");
    static int[] ProbeIds(string side) => new[]{Shader.PropertyToID("_Baked"+side),Shader.PropertyToID("_BakedPosition"+side),Shader.PropertyToID("_BakedMin"+side),Shader.PropertyToID("_BakedMax"+side),Shader.PropertyToID("_BakedHDR"+side)};
    public void RefreshBakedBindings()
    {
        var probes=GetComponentsInChildren<ReflectionProbe>(true);
        // These components store cubemaps and box projection metadata for our shader.
        // They must not participate in Unity's automatic probe assignment to other renderers.
        foreach(var probe in probes)
            if(IsGlassBakedSource(probe) && probe.enabled)
            {
                probe.enabled=false;
                #if UNITY_EDITOR
                if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(probe);
                #endif
            }
        foreach(var group in groups)
        {
            var binding=group.plane ? group.plane.GetComponent<GlassReflectionBinding>() : null;
            group.bakedPositive=binding && binding.Verified ? binding.positive : null;
            group.bakedNegative=binding && binding.Verified ? binding.negative : null;
        }
    }
    static bool IsGlassBakedSource(ReflectionProbe probe)
    {
        return probe && probe.mode==ReflectionProbeMode.Custom && probe.customBakedTexture &&
            !probe.name.StartsWith("Glass Probe ",StringComparison.Ordinal) &&
            !probe.name.StartsWith("Hybrid Baked ",StringComparison.Ordinal);
    }
    static void SetBakedProperties(MaterialPropertyBlock properties,WindowGroup group)
    {
        bool valid=group.plane && group.bakedPositive && group.bakedNegative && group.bakedPositive.customBakedTexture && group.bakedNegative.customBakedTexture;
        properties.SetFloat("_UseLocalBaked",valid ? 1 : -1);
        if(!valid) return;
        properties.SetVector("_BakedNormal",group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized);
        SetProbe(properties,group.bakedPositive,PositiveProbeIds); SetProbe(properties,group.bakedNegative,NegativeProbeIds);
    }
    static void SetProbe(MaterialPropertyBlock properties,ReflectionProbe probe,int[] ids)
    {
        properties.SetTexture(ids[0],probe.customBakedTexture);
        var p=probe.transform.position;
        properties.SetVector(ids[1],new Vector4(p.x,p.y,p.z,probe.boxProjection ? 1 : 0));
        properties.SetVector(ids[2],probe.bounds.min); properties.SetVector(ids[3],probe.bounds.max);
        properties.SetVector(ids[4],probe.textureHDRDecodeValues);
    }

    void TickReflections(Camera source,bool allowLive)
    {
        if(Application.isPlaying && allowLive && reflectionMode==ReflectionMode.Hybrid)
            ReflectionActor.DiscoverTaggedActors(Time.realtimeSinceStartupAsDouble);
        TickWithClock(source,allowLive,Time.realtimeSinceStartupAsDouble,Time.frameCount);
    }

    // A clock seam also lets edit-mode validation exercise real rendering and scheduling deterministically.
    void TickWithClock(Camera source,bool allowLive,double now,int frame)
    {
        CapturesThisTick=0; LastSourceCamera=source; ActiveGroups=0;
        float dt=previousTick<0 ? 0 : Mathf.Clamp((float)(now-previousTick),0,0.1f); previousTick=now;
        if(!source || !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) { ResetWeights(); return; }
        var effective=allowLive ? reflectionMode : ReflectionMode.Baked;
        bool configured=reflectionRendererIndex>=0 && (!configuredPipeline || configuredPipeline==GraphicsSettings.currentRenderPipeline);
        if(!configured && effective!=ReflectionMode.Baked)
        {
            effective=ReflectionMode.Baked;
            if(!warned) { Debug.LogWarning("Glass is using baked probes: its planar renderer is not configured for this pipeline.",this); warned=true; }
        }
        GeometryUtility.CalculateFrustumPlanes(source,frustum);
        candidates.Clear(); bool anyVisible=false;
        foreach(var group in groups)
        {
            if(!group.plane) { ReleaseIdle(group,now); continue; }
            float distance=float.PositiveInfinity,area=0;
            foreach(var window in group.windows)
            {
                if(!window || !window.enabled || !window.gameObject.activeInHierarchy || (source.cullingMask&(1<<window.gameObject.layer))==0 || !GeometryUtility.TestPlanesAABB(frustum,window.bounds)) continue;
                float windowArea=ScreenArea(source,window.bounds);
                if(windowArea<minimumScreenArea) continue;
                area+=windowArea; distance=Mathf.Min(distance,Mathf.Sqrt(window.bounds.SqrDistance(source.transform.position)));
            }
            bool visible=area>0 && distance<fadeEndDistance;
            if(!visible)
            {
                group.liveWeight=0; group.activity=false; group.nextActivityCheck=0; group.lastActivity=-100;
                ReleaseIdle(group,now); continue;
            }
            anyVisible=true;
            Vector3 point=group.plane.TransformPoint(group.localPoint);
            Vector3 normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
            int side=Vector3.Dot(normal,source.transform.position-point)>=0 ? 1 : -1;
            bool changed=group.sourceId!=source.GetInstanceID() || side!=group.side ||
                (point-group.lastPoint).sqrMagnitude>0.000001f || (normal*side-group.lastNormal).sqrMagnitude>0.000001f;
            if(changed) { group.ready=false; group.liveWeight=0; group.nextActivityCheck=0; }
            if(effective==ReflectionMode.Hybrid && now>=group.nextActivityCheck)
            {
                group.activity=ActorNeeded(group,source,point,normal*side,now,out group.reason);
                group.nextActivityCheck=now+activityInterval;
                if(group.activity) group.lastActivity=now;
            }
            bool needed=effective==ReflectionMode.Realtime || (effective==ReflectionMode.Hybrid && (group.activity || now-group.lastActivity<activityHold || group.liveWeight>0));
            if(needed) candidates.Add(new Candidate{group=group,distance=distance,area=area});
            else { group.liveWeight=0; ReleaseIdle(group,now); }
        }
        ConfigureTransmission(source,anyVisible);
        // Preserve transmission even when no planar candidate exists.
        foreach(var group in groups) Apply(group,0);
        candidates.Sort((a,b)=> { int area=b.area.CompareTo(a.area); return area!=0 ? area : a.distance.CompareTo(b.distance); });
        int selected=Mathf.Min(maxActiveGroups,candidates.Count);
        WindowGroup scheduled=null; double oldest=double.PositiveInfinity;
        for(int i=0;i<selected;i++)
        {
            var group=candidates[i].group;
            bool held=effective==ReflectionMode.Realtime || group.activity || now-group.lastActivity<activityHold;
            if(!held && group.liveWeight<=0) continue;
            float hz=effective==ReflectionMode.Realtime && candidates[i].distance<=fullRateDistance ? float.PositiveInfinity :
                (effective==ReflectionMode.Realtime ? distantUpdatesPerSecond : liveUpdatesPerSecond);
            bool due=!group.ready || now-group.lastUpdate>=1/Mathf.Max(1,hz);
            if(due && group.lastUpdate<oldest) { scheduled=group; oldest=group.lastUpdate; }
        }
        LastActivityReason="Baked: no reflected activity";
        for(int i=0;i<candidates.Count;i++)
        {
            var candidate=candidates[i]; var group=candidate.group;
            if(i>=selected) { group.liveWeight=0; group.reason="Baked: live group budget"; ReleaseIdle(group,now); continue; }
            bool held=effective==ReflectionMode.Realtime || group.activity || now-group.lastActivity<activityHold;
            Vector3 point=group.plane.TransformPoint(group.localPoint);
            Vector3 normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
            int side=Vector3.Dot(normal,source.transform.position-point)>=0 ? 1 : -1; normal*=side;
            bool capture=effective==ReflectionMode.Realtime ? (!group.ready || candidate.distance<=fullRateDistance || now-group.lastUpdate>=1/Mathf.Max(1,distantUpdatesPerSecond)) :
                group==scheduled && lastCaptureFrame!=frame;
            if(capture && (held || group.liveWeight>0))
            {
                long before=TotalCaptures; Render(group,source,point,normal);
                if(TotalCaptures>before) { lastCaptureFrame=frame; group.lastUpdate=now; }
                group.lastPoint=point; group.lastNormal=normal; group.side=side; group.sourceId=source.GetInstanceID();
            }
            if(effective==ReflectionMode.Realtime) group.liveWeight=group.ready ? 1 : 0;
            else group.liveWeight=group.ready ? Mathf.MoveTowards(group.liveWeight,held ? 1 : 0,dt/Mathf.Max(0.01f,transitionSeconds)) : 0;
            if(group.ready) group.lastVisible=now;
            float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(reducedRateDistance,fadeEndDistance,candidate.distance));
            Apply(group,group.liveWeight*fade);
            if(group.ready && (held || group.liveWeight>0)) { ActiveGroups++; LastActivityReason=effective==ReflectionMode.Realtime ? "Realtime mode" : group.reason; }
            if(!held && group.liveWeight<=0) ReleaseIdle(group,now);
        }
    }

    bool ActorNeeded(WindowGroup group,Camera source,Vector3 point,Vector3 normal,double now,out string reason)
    {
        reason="Baked: no reflected actor";
        var mirror=ReflectionMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,point)));
        var view=source.worldToCameraMatrix*mirror;
        Vector3 cp=view.MultiplyPoint(point+normal*clipOffset),cn=view.MultiplyVector(normal).normalized;
        var projection=source.CalculateObliqueMatrix(new Vector4(cn.x,cn.y,cn.z,-Vector3.Dot(cp,cn)));
        var vp=projection*view; GeometryUtility.CalculateFrustumPlanes(vp,actorFrustum);
        foreach(var actor in ReflectionActor.Registered)
        {
            if(!actor || actor.gameObject.scene!=gameObject.scene || !actor.TryGetBounds(reflectionLayers.value,now,out var bounds,out bool lights)) continue;
            if(!GeometryUtility.TestPlanesAABB(actorFrustum,bounds)) continue;
            Rect actorRect=ProjectedRect(vp,bounds);
            foreach(var window in group.windows)
            {
                if(!window || !window.enabled || !window.gameObject.activeInHierarchy || !GeometryUtility.TestPlanesAABB(frustum,window.bounds)) continue;
                if(!actorRect.Overlaps(ProjectedRect(source.projectionMatrix*source.worldToCameraMatrix,window.bounds))) continue;
                if(!lights && testActorOcclusion && Blocked(window.bounds.center+normal*0.05f,bounds,actor)) continue;
                reason=lights ? "Changing light: "+actor.name : actor.Reason; return true;
            }
        }
        return false;
    }
    static Rect ProjectedRect(Matrix4x4 vp,Bounds bounds)
    {
        Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;
        for(int i=0;i<8;i++)
        {
            var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            var clip=vp*new Vector4(p.x,p.y,p.z,1);
            if(clip.w<=0.001f) return new Rect(-2,-2,4,4);
            Vector2 uv=new Vector2(clip.x,clip.y)/clip.w*0.5f+Vector2.one*0.5f;
            min=Vector2.Min(min,uv); max=Vector2.Max(max,uv);
        }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    bool Blocked(Vector3 origin,Bounds bounds,ReflectionActor actor)
    {
        // Conservative: keep the reflection if any of three representative rays is unobstructed.
        for(int sample=0;sample<3;sample++)
        {
            Vector3 target=bounds.center+(sample==0 ? Vector3.zero : bounds.extents*(sample==1 ? 0.7f : -0.7f));
            Vector3 direction=target-origin; float distance=direction.magnitude;
            if(distance<0.1f) return false;
            int hits=Physics.RaycastNonAlloc(origin,direction/distance,obstructionHits,distance,reflectionLayers.value,QueryTriggerInteraction.Ignore);
            if(hits==obstructionHits.Length) return false;
            bool blocked=false;
            for(int i=0;i<hits;i++)
            {
                var collider=obstructionHits[i].collider;
                if(!collider || collider.transform.IsChildOf(actor.transform) || collider.GetComponent<GlassSurfaceVariation>()) continue;
                if(obstructionHits[i].distance<distance-0.1f) { blocked=true; break; }
            }
            if(!blocked) return false;
        }
        return true;
    }
    void ReleaseIdle(WindowGroup group,double now)
    {
        if(now-group.lastVisible<idleResourceSeconds) return;
        if(group.camera) { group.camera.targetTexture=null; DestroyResource(group.camera.gameObject); }
        if(group.texture) { group.texture.Release(); DestroyResource(group.texture); }
        group.camera=null; group.texture=null; group.ready=false; group.liveWeight=0;
    }
}
