using System;
using UnityEngine;

public static class RoadTerrainAdapter
{
    public static int Shape(TerrainData data,Vector3 origin,RoadWidthGeometry field,Mesh road,Transform roadTransform)
    {
        int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);var before=(float[,])heights.Clone();var size=data.size;var hits=new RaycastHit[48];
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {
            var p=new Vector3(origin.x+size.x*x/(n-1),0,origin.z+size.z*z/(n-1));
            if(!field.Query(p,out var q))continue;float d=q.Outside(1.5f);if(d>=6.75f)continue;
            float target=q.center.y-.05f-Mathf.Min(.75f,d)*.03f;
            float blend=RoadWidthGeometry.Smooth((d-.75f)/6f);
            float adjusted=Mathf.Lerp(target,origin.y+before[z,x]*size.y,blend);
            if(d>.01f)
            {
                int count=Physics.RaycastNonAlloc(new Vector3(p.x,q.center.y+2,p.z),Vector3.down,hits,4,~0,QueryTriggerInteraction.Ignore);
                for(int h=0;h<count;h++)
                {
                    var hit=hits[h];var collider=hit.collider;
                    if(collider is TerrainCollider||collider.attachedRigidbody||collider.GetComponentInParent<ReflectionActor>()||hit.normal.y<.8f||hit.point.y>q.center.y+.75f||hit.point.y<q.center.y-1)continue;
                    string name=collider.name.ToLowerInvariant();
                    if(name.Contains("platform")||name.Contains("pavement")||name.Contains("zemin")||name.Contains("concrete")||name.Contains("parking"))adjusted=Mathf.Min(adjusted,hit.point.y-.05f);
                }
            }
            heights[z,x]=(adjusted-origin.y)/size.y;
        }
        // Rasterize actual top triangles against heightmap cells. Capping all four
        // corners prevents interpolated Terrain poking through the carriageway.
        var vertices=road.vertices;var triangles=road.triangles;
        for(int i=0;i<triangles.Length;i+=3)
        {
            var wa=roadTransform.TransformPoint(vertices[triangles[i]]);var wb=roadTransform.TransformPoint(vertices[triangles[i+1]]);var wc=roadTransform.TransformPoint(vertices[triangles[i+2]]);
            if(Vector3.Cross(wb-wa,wc-wa).normalized.y<.8f)continue;
            var a=Grid(wa,origin,size,n);var b=Grid(wb,origin,size,n);var c=Grid(wc,origin,size,n);
            int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))),0,n-2),x1=Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))),0,n-2);
            int z0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))),0,n-2),z1=Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))),0,n-2);
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)if(Intersects(a,b,c,x,z))
            {
                float cap=(Mathf.Min(wa.y,Mathf.Min(wb.y,wc.y))-.05f-origin.y)/size.y;
                for(int dz=0;dz<=1;dz++)for(int dx=0;dx<=1;dx++)heights[z+dz,x+dx]=Mathf.Min(heights[z+dz,x+dx],cap);
            }
        }
        int changed=0;
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(heights[z,x]!=before[z,x])
        {
            var p=new Vector3(origin.x+size.x*x/(n-1),0,origin.z+size.z*z/(n-1));
            if(!field.Query(p,out var q)||q.Outside(1.5f)>6.751f)throw new InvalidOperationException("Terrain support exceeds protected corridor");
            changed++;
        }
        data.SetHeightsDelayLOD(0,0,heights);data.SyncHeightmap();return changed;
    }
    static Vector2 Grid(Vector3 p,Vector3 origin,Vector3 size,int n)=>new Vector2((p.x-origin.x)/size.x*(n-1),(p.z-origin.z)/size.z*(n-1));
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static bool In(Vector2 p,Vector2 a,Vector2 b,Vector2 c){float s=Cross(b-a,p-a),t=Cross(c-b,p-b),u=Cross(a-c,p-c);return s>=-1e-5f&&t>=-1e-5f&&u>=-1e-5f||s<=1e-5f&&t<=1e-5f&&u<=1e-5f;}
    static bool Edge(Vector2 a,Vector2 b,Vector2 c,Vector2 d){var ab=b-a;var cd=d-c;float div=Cross(ab,cd);if(Mathf.Abs(div)<1e-8)return false;float t=Cross(c-a,cd)/div,u=Cross(c-a,ab)/div;return t>=0&&t<=1&&u>=0&&u<=1;}
    static bool Intersects(Vector2 a,Vector2 b,Vector2 c,int x,int z)
    {
        var p=new Vector2(x,z);var q=p+Vector2.right;var r=p+Vector2.one;var s=p+Vector2.up;
        if(In(p,a,b,c)||In(q,a,b,c)||In(r,a,b,c)||In(s,a,b,c))return true;
        foreach(var v in new[]{a,b,c})if(v.x>=x&&v.x<=x+1&&v.y>=z&&v.y<=z+1)return true;
        foreach(var e in new[]{(a,b),(b,c),(c,a)})if(Edge(e.Item1,e.Item2,p,q)||Edge(e.Item1,e.Item2,q,r)||Edge(e.Item1,e.Item2,r,s)||Edge(e.Item1,e.Item2,s,p))return true;
        return false;
    }
    public static int ClearDetails(TerrainData data,Vector3 origin,RoadWidthGeometry field)
    {
        int n=data.detailResolution;if(n==0)return 0;var clear=new bool[n,n];var size=data.size;
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {var p=new Vector3(origin.x+size.x*(x+.5f)/n,0,origin.z+size.z*(z+.5f)/n);clear[z,x]=field.Query(p,out var q)&&q.Outside(1.5f)<=.75f;}
        int changed=0;
        for(int layer=0;layer<data.detailPrototypes.Length;layer++)
        {
            var map=data.GetDetailLayer(0,0,n,n,layer);bool dirty=false;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(clear[z,x]&&map[z,x]!=0){map[z,x]=0;changed++;dirty=true;}
            if(dirty)data.SetDetailLayer(0,0,layer,map);
        }
        return changed;
    }
}
