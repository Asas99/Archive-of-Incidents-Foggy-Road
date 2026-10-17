using System;
using UnityEngine;

/// <summary>Saved doorway measurements. No runtime update, mesh cooking or scene scans.</summary>
[DisallowMultipleComponent]
public sealed class DoorwayColliderAuthoring : MonoBehaviour
{
    public MeshFilter first,second;
    public Transform doorHinge;
    public Bounds wall;
    public Vector2 openingMin,openingMax;
    public BoxCollider[] generated=Array.Empty<BoxCollider>();
    public Collider[] replaced=Array.Empty<Collider>();
    public bool[] previousEnabled=Array.Empty<bool>();
    [TextArea] public string measurement;
    public bool Built => generated!=null && generated.Length==3 && Array.TrueForAll(generated,c=>c && c.enabled && !c.isTrigger);
    public Bounds[] Shapes()
    {
        Vector3 lo=wall.min,hi=wall.max;
        if(openingMin.x<=lo.x || openingMax.x>=hi.x || openingMax.y>=hi.y || openingMax.x<=openingMin.x || openingMax.y<=openingMin.y)
            throw new InvalidOperationException("Opening must lie inside the wall and leave side/header material.");
        return new[]{ Make(lo,new Vector3(openingMin.x,hi.y,hi.z)),Make(new Vector3(openingMax.x,lo.y,lo.z),hi),Make(new Vector3(openingMin.x,openingMax.y,lo.z),new Vector3(openingMax.x,hi.y,hi.z)) };
    }
    static Bounds Make(Vector3 lo,Vector3 hi) => new Bounds((lo+hi)*0.5f,hi-lo);
}
