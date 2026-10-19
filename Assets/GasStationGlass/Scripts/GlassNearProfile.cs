using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class GasStationPlanarGlass
{
    public bool nearProfileEnabled;
    [Range(0,1)] public float dynamicReflectionStrength=1;
    public Vector2Int atlasResolutionLimit=new Vector2Int(640,360);
    [Range(0,2)] public float dynamicMipLimit=2;
    [Range(0,1)] public float actorAreaEnter=.0025f,actorAreaExit=.0015f;
    public bool useExistingSunShadows=true;
    public bool contactShadows=true;
    [Range(0,1)] public float contactShadowOpacity=.12f;
    public Shader contactShadowShader;
    public bool localReflectionLights;
    public bool MainShadowAvailable{get;private set;}
    public int ContactShadowDraws{get;private set;}
    [SerializeField,HideInInspector] string previousAtlasQualityJson;
    [Serializable] sealed class AtlasQuality
    {
        public bool near,contacts,local,shadows;public float strength,scale,mip,enter,exit,depth,full,reduced,end,hz,areaEnter,areaExit,contactOpacity;
        public Vector2Int limit;public TransmissionQuality transmission;
    }
    public void SavePreviousAtlasQuality()
    {
        previousAtlasQualityJson=JsonUtility.ToJson(new AtlasQuality{near=nearProfileEnabled,contacts=contactShadows,local=localReflectionLights,shadows=useExistingSunShadows,strength=dynamicReflectionStrength,scale=resolutionScale,mip=dynamicMipLimit,enter=actorEnterDistance,exit=actorExitDistance,depth=reflectionSceneDepth,full=fullRateDistance,reduced=reducedRateDistance,end=fadeEndDistance,hz=liveUpdatesPerSecond,areaEnter=actorAreaEnter,areaExit=actorAreaExit,contactOpacity=contactShadowOpacity,limit=atlasResolutionLimit,transmission=transmissionQuality});
    }
    public void RestorePreviousAtlasQuality()
    {
        if(string.IsNullOrEmpty(previousAtlasQualityJson))return;var q=JsonUtility.FromJson<AtlasQuality>(previousAtlasQualityJson);
        nearProfileEnabled=q.near;contactShadows=q.contacts;localReflectionLights=q.local;useExistingSunShadows=q.shadows;dynamicReflectionStrength=q.strength;resolutionScale=q.scale;dynamicMipLimit=q.mip;actorEnterDistance=q.enter;actorExitDistance=q.exit;reflectionSceneDepth=q.depth;fullRateDistance=q.full;reducedRateDistance=q.reduced;fadeEndDistance=q.end;liveUpdatesPerSecond=q.hz;actorAreaEnter=q.areaEnter;actorAreaExit=q.areaExit;contactShadowOpacity=q.contactOpacity;atlasResolutionLimit=q.limit;transmissionQuality=q.transmission;InvalidateReflections();
    }
    public void ApplyNearDefaults()
    {
        nearProfileEnabled=true;dynamicReflectionStrength=.25f;actorEnterDistance=8;actorExitDistance=10;reflectionSceneDepth=12;
        fullRateDistance=4;reducedRateDistance=8;fadeEndDistance=12;resolutionScale=.25f;atlasResolutionLimit=new Vector2Int(480,270);dynamicMipLimit=1.5f;
        actorAreaEnter=.0025f;actorAreaExit=.0015f;liveUpdatesPerSecond=30;transmissionQuality=TransmissionQuality.Off;localReflectionLights=false;useExistingSunShadows=true;contactShadows=true;contactShadowOpacity=.12f;InvalidateReflections();
    }
    double nearClock;
    readonly Dictionary<WindowGroup,HashSet<ReflectionActor>> areaActors=new Dictionary<WindowGroup,HashSet<ReflectionActor>>();
    readonly Plane[] visibilityPlanes=new Plane[6];
    readonly Dictionary<ReflectionActor,GroundContact> groundContacts=new Dictionary<ReflectionActor,GroundContact>();
    readonly RaycastHit[] groundHits=new RaycastHit[32];
    sealed class GroundContact {public double next;public bool valid;public Vector3 point,normal;public Matrix4x4 matrix;}
    struct ContactDraw {public Matrix4x4 matrix;public float opacity;}
    Material contactMaterial;Mesh contactQuad;
    float ActorVisibility(WindowGroup group,ReflectionActor actor,Bounds bounds,Camera source)
    {
        if(!nearProfileEnabled||!UsesAtlas)return ActorInRange(group,actor,bounds)?1:0;
        if(!ActorInRange(group,actor,bounds)||!source)return 0;
        float nearest=float.PositiveInfinity;
        foreach(var window in group.windows)if(window&&window.enabled&&window.gameObject.activeInHierarchy)
        {
            var b=window.bounds;var gap=Vector3.Max(Vector3.zero,Vector3.Max(b.min-bounds.max,bounds.min-b.max));nearest=Mathf.Min(nearest,gap.magnitude);
        }
        float distanceFade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(actorEnterDistance,Mathf.Max(actorEnterDistance+.01f,actorExitDistance),nearest));
        if(distanceFade<=0)return 0;
        var normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
        var point=group.plane.TransformPoint(group.localPoint);var mirror=ReflectionMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,point)));
        var vp=ReflectionProjection(source,point)*source.worldToCameraMatrix*mirror;GeometryUtility.CalculateFrustumPlanes(vp,visibilityPlanes);
        if(!GeometryUtility.TestPlanesAABB(visibilityPlanes,bounds))return 0;
        var actorRect=ProjectedRect(vp,bounds);float area=0;
        foreach(var member in groups)if(SharesCapture(group,member))foreach(var window in member.windows)
        {
            if(!window||!window.enabled||!window.gameObject.activeInHierarchy)continue;
            var rect=ProjectedRect(source.projectionMatrix*source.worldToCameraMatrix,window.bounds);
            float w=Mathf.Max(0,Mathf.Min(1,Mathf.Min(rect.xMax,actorRect.xMax))-Mathf.Max(0,Mathf.Max(rect.xMin,actorRect.xMin)));
            float h=Mathf.Max(0,Mathf.Min(1,Mathf.Min(rect.yMax,actorRect.yMax))-Mathf.Max(0,Mathf.Max(rect.yMin,actorRect.yMin)));area+=w*h;
        }
        if(!areaActors.TryGetValue(group,out var inside)){inside=new HashSet<ReflectionActor>();areaActors.Add(group,inside);}
        float exit=Mathf.Min(actorAreaExit,actorAreaEnter),enter=Mathf.Max(exit+.000001f,actorAreaEnter);
        bool eligible=area>=(inside.Contains(actor)?exit:enter);
        if(!eligible){inside.Remove(actor);return 0;}inside.Add(actor);
        return distanceFade*Mathf.SmoothStep(0,1,Mathf.InverseLerp(exit,enter,area));
    }
    float CaptureVisibility(WindowGroup group,ReflectionActor actor,Bounds bounds,Camera source)
    {
        float value=0;foreach(var member in groups)if(SharesCapture(group,member))value=Mathf.Max(value,ActorVisibility(member,actor,bounds,source));return value;
    }
    bool TryContact(ReflectionActor actor,Bounds bounds,out Matrix4x4 matrix)
    {
        matrix=Matrix4x4.identity;
        if(!nearProfileEnabled||!contactShadows||contactShadowOpacity<=0||(!actor.CompareTag("Car")&&actor.LowDrawParts.Count==0))return false;
        if(!groundContacts.TryGetValue(actor,out var state)){state=new GroundContact();groundContacts.Add(actor,state);}
        if(nearClock>=state.next)
        {
            state.next=nearClock+.1;state.valid=false;
            var origin=bounds.center+Vector3.up*.1f;int count=Physics.RaycastNonAlloc(origin,Vector3.down,groundHits,bounds.extents.y+3.1f,reflectionLayers.value,QueryTriggerInteraction.Ignore);
            if(count<groundHits.Length)
            {
                float closest=float.PositiveInfinity;RaycastHit chosen=default;
                for(int i=0;i<count;i++)
                {
                    var hit=groundHits[i];if(!hit.collider||hit.collider.transform.IsChildOf(actor.transform)||Vector3.Dot(hit.normal,Vector3.up)<.5f||hit.distance>=closest)continue;
                    chosen=hit;closest=hit.distance;
                }
                if(closest<float.PositiveInfinity)
                {
                    state.point=chosen.point;state.normal=chosen.normal;state.valid=true;
                }
            }
        }
        if(state.valid)
        {
            // Ground is queried at 10 Hz, but the footprint follows the vehicle every draw.
            var normal=state.normal;Vector3 forward=Vector3.ProjectOnPlane(actor.transform.forward,normal).normalized;if(forward.sqrMagnitude<.01f)forward=Vector3.ProjectOnPlane(Vector3.forward,normal).normalized;
            var rotation=Quaternion.LookRotation(normal,forward);var right=rotation*Vector3.right;var up=rotation*Vector3.up;
            float rx=Vector3.Dot(new Vector3(Mathf.Abs(right.x),Mathf.Abs(right.y),Mathf.Abs(right.z)),bounds.extents);
            float ry=Vector3.Dot(new Vector3(Mathf.Abs(up.x),Mathf.Abs(up.y),Mathf.Abs(up.z)),bounds.extents);
            var point=bounds.center-normal*Vector3.Dot(bounds.center-state.point,normal);
            state.matrix=Matrix4x4.TRS(point+normal*.02f,rotation,new Vector3(Mathf.Max(.1f,rx*2),Mathf.Max(.1f,ry*2),1));
        }
        matrix=state.matrix;return state.valid;
    }
    public Light AtlasSun
    {
        get {if(RenderSettings.sun&&RenderSettings.sun.isActiveAndEnabled)return RenderSettings.sun;Light best=null;foreach(var light in atlasLights)if(light&&light.isActiveAndEnabled&&light.type==LightType.Directional&&(!best||light.intensity>best.intensity))best=light;return best;}
    }
    void ReleaseNearProfile()
    {
        areaActors.Clear();groundContacts.Clear();if(contactMaterial)DestroyResource(contactMaterial);contactMaterial=null;contactQuad=null;
    }
}
