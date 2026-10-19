using UnityEngine;

public sealed partial class GasStationPlanarGlass
{
    public enum TransmissionQuality { Off, Balanced, High }
    public TransmissionQuality transmissionQuality=TransmissionQuality.Balanced;
    [Min(0)] public float actorEnterDistance=20;
    [Min(0)] public float actorExitDistance=22;
    [Min(1)] public float reflectionSceneDepth=25;
    public string SelectedLiveGroup { get; private set; }="None";
    public string LiveActorSummary { get; private set; }="No actors";
    public bool TransmissionCopyRequested => transmissionCamera && transmissionReady;
    MaterialPropertyBlock transmissionProperties;

    bool NeedsTransmission(Renderer window)
    {
        if(transmissionQuality==TransmissionQuality.Off)return false;
        if(transmissionProperties==null)transmissionProperties=new MaterialPropertyBlock();
        window.GetPropertyBlock(transmissionProperties);
        if(transmissionProperties.HasFloat("_TransmissionBlur"))return transmissionProperties.GetFloat("_TransmissionBlur")>.01f;
        var material=window.sharedMaterial;
        return material&&material.HasProperty("_TransmissionBlur")&&material.GetFloat("_TransmissionBlur")>.01f;
    }

    bool ActorInRange(WindowGroup group,ReflectionActor actor,Bounds bounds)
    {
        if(!Lean(this))return true;
        bool wasInside=group.nearbyActors.Contains(actor);
        float limit=wasInside?Mathf.Max(actorEnterDistance,actorExitDistance):actorEnterDistance;
        float nearest=float.PositiveInfinity;
        foreach(var window in group.windows)if(window&&window.enabled&&window.gameObject.activeInHierarchy)
        {
            var b=window.bounds;
            var gap=Vector3.Max(Vector3.zero,Vector3.Max(b.min-bounds.max,bounds.min-b.max));
            nearest=Mathf.Min(nearest,gap.sqrMagnitude);
        }
        bool inside=nearest<=limit*limit;
        if(inside)group.nearbyActors.Add(actor);else group.nearbyActors.Remove(actor);
        return inside;
    }

    // Build one projection for activity, model selection and capture. In particular,
    // do not calculate the oblique near plane from the source's unbounded projection.
    public Matrix4x4 ReflectionProjection(Camera source,Vector3 point)
    {
        var projection=source.projectionMatrix;
        if(!Lean(this))return projection;
        float near=source.nearClipPlane;
        float far=Mathf.Max(near+.01f,Mathf.Min(source.farClipPlane,Vector3.Distance(source.transform.position,point)+Mathf.Max(1,reflectionSceneDepth)));
        if(source.orthographic){projection.m22=-2/(far-near);projection.m23=-(far+near)/(far-near);}
        else {projection.m22=-(far+near)/(far-near);projection.m23=-(2*far*near)/(far-near);}
        return projection;
    }
    // Oblique clipping also changes the far plane. A short far distance can
    // therefore cut nearby actors when the viewer approaches or turns sideways.
    // Keep the raster projection stable; explicit bounded actor culling below
    // still limits the work to the configured local reflection range.
    Matrix4x4 CaptureProjection(Camera source,Vector3 point,Vector3 normal,Matrix4x4 view)
    {
        var cp=view.MultiplyPoint(point+normal*clipOffset);
        var cn=view.MultiplyVector(normal).normalized;
        return ObliqueProjection(source.projectionMatrix,new Vector4(cn.x,cn.y,cn.z,-Vector3.Dot(cp,cn)));
    }
    static bool SharesCapture(WindowGroup a,WindowGroup b)
    {
        if(a==b)return true;
        if(a==null||b==null||!a.plane||!b.plane)return false;
        var an=a.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(a.localNormal).normalized;
        var bn=b.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(b.localNormal).normalized;
        return Mathf.Abs(Vector3.Dot(an,bn))>.99999f&&
            Mathf.Abs(Vector3.Dot(a.plane.TransformPoint(a.localPoint)-b.plane.TransformPoint(b.localPoint),an))<.01f;
    }
    bool CaptureActorInRange(WindowGroup group,ReflectionActor actor,Bounds bounds)
    {
        foreach(var member in groups)if(SharesCapture(group,member)&&ActorInRange(member,actor,bounds))return true;
        return false;
    }
    static Matrix4x4 ObliqueProjection(Matrix4x4 projection,Vector4 clip)
    {
        var q=projection.inverse*new Vector4(clip.x>=0?1:-1,clip.y>=0?1:-1,1,1);
        float denominator=Vector4.Dot(clip,q);
        if(Mathf.Abs(denominator)<1e-6f)return projection;
        projection.SetRow(2,clip*(2/denominator)-projection.GetRow(3));
        return projection;
    }
}
