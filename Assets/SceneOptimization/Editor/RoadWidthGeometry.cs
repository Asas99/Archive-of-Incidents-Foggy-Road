using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Editor-only cross sections use the imported road's continuous longitudinal UVs.
public sealed class RoadWidthGeometry
{
    public struct Section { public Vector3 a,b; public Vector3 center => (a+b)*.5f; public float half => Flat(a-b).magnitude*.5f; }
    public struct Sample { public Vector3 center,across; public float half,side,station,end; public float Outside(float factor) => Mathf.Sqrt(Mathf.Pow(Mathf.Max(0,Mathf.Abs(side)-half*factor),2)+end*end); }
    public readonly Section[] sections;
    public readonly float[] distances;
    public readonly float[] vertexStations;
    readonly Dictionary<Vector2Int,List<int>> grid = new Dictionary<Vector2Int,List<int>>();
    const float Cell=32;
    static Vector3 Flat(Vector3 v) => new Vector3(v.x,0,v.z);
    public RoadWidthGeometry(Mesh mesh,Transform transform)
    {
        var uv=mesh.uv;var vertices=mesh.vertices;
        if(uv.Length!=vertices.Length)throw new InvalidOperationException("Road requires longitudinal UVs");
        var rows=Enumerable.Range(0,vertices.Length).GroupBy(i=>uv[i].y).OrderBy(g=>g.Key).ToArray();
        sections=new Section[rows.Length]; distances=new float[rows.Length];vertexStations=new float[vertices.Length];
        for(int r=0;r<rows.Length;r++)
        {
            var ids=rows[r].ToArray();var a=ids.Where(i=>uv[i].x<.001f).OrderByDescending(i=>transform.TransformPoint(vertices[i]).y).First();var b=ids.Where(i=>uv[i].x>.999f).OrderByDescending(i=>transform.TransformPoint(vertices[i]).y).First();
            sections[r]=new Section{a=transform.TransformPoint(vertices[a]),b=transform.TransformPoint(vertices[b])};
            if(sections[r].half<.5f)throw new InvalidOperationException("Invalid road section");
            if(r>0)distances[r]=distances[r-1]+Flat(sections[r].center-sections[r-1].center).magnitude;
            foreach(int i in ids)vertexStations[i]=r;
        }
        for(int i=0;i<sections.Length-1;i++)
        {
            var a=sections[i].center;var b=sections[i+1].center;
            for(int z=Mathf.FloorToInt((Mathf.Min(a.z,b.z)-40)/Cell);z<=Mathf.FloorToInt((Mathf.Max(a.z,b.z)+40)/Cell);z++)
            for(int x=Mathf.FloorToInt((Mathf.Min(a.x,b.x)-40)/Cell);x<=Mathf.FloorToInt((Mathf.Max(a.x,b.x)+40)/Cell);x++)
            {var key=new Vector2Int(x,z);if(!grid.TryGetValue(key,out var list))grid[key]=list=new List<int>();list.Add(i);}
        }
    }
    public bool Query(Vector3 point,out Sample result)
    {
        result=default; if(!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(point.x/Cell),Mathf.FloorToInt(point.z/Cell)),out var list))return false;
        float best=float.PositiveInfinity;
        foreach(int i in list)
        {
            var a=sections[i].center;var b=sections[i+1].center;var tangent=Flat(b-a);float raw=Vector3.Dot(Flat(point-a),tangent)/tangent.sqrMagnitude;float t=Mathf.Clamp01(raw);var c=Vector3.Lerp(a,b,t);float d=Flat(point-c).sqrMagnitude;if(d>=best)continue;
            best=d;var across=Flat(Vector3.Lerp(sections[i].a-sections[i].b,sections[i+1].a-sections[i+1].b,t)).normalized;
            result=new Sample{center=c,across=across,half=Mathf.Lerp(sections[i].half,sections[i+1].half,t),side=Vector3.Dot(Flat(point-c),across),station=Mathf.Lerp(distances[i],distances[i+1],t),end=(i==0&&raw<0||i==sections.Length-2&&raw>1)?Mathf.Abs(Vector3.Dot(Flat(point-c),tangent.normalized)):0};
        }
        return !float.IsPositiveInfinity(best);
    }
    public Mesh Widen(Mesh original,Transform transform)
    {
        var mesh=UnityEngine.Object.Instantiate(original);mesh.name=original.name+"_Width150";var vertices=original.vertices;
        for(int i=0;i<vertices.Length;i++){var c=sections[(int)vertexStations[i]].center;var p=transform.TransformPoint(vertices[i]);p+=Flat(p-c)*.5f;vertices[i]=transform.InverseTransformPoint(p);}
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
    }
    public float SurfaceHeight(Vector3 point)
    {
        if(!Query(point,out var q))throw new InvalidOperationException("Point outside road field");return q.center.y;
    }
    public static float Smooth(float v){v=Mathf.Clamp01(v);return v*v*v*(v*(v*6-15)+10);}
}
