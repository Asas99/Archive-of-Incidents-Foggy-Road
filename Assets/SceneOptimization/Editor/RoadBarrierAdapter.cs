using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class RoadBarrierAdapter
{
    readonly RoadWidthGeometry field;
    readonly float[][] envelope;
    const float Bin=1.5f;
    public RoadBarrierAdapter(RoadWidthGeometry field,IEnumerable<MeshFilter> filters)
    {
        this.field=field;int count=Mathf.CeilToInt(field.distances.Last()/Bin)+2;envelope=new[]{new float[count],new float[count]};
        foreach(var e in envelope)for(int i=0;i<count;i++)e[i]=float.PositiveInfinity;
        foreach(var f in filters)
        {
            int side=f.name.StartsWith("GRPRO_L_")?0:1;float sign=side==0?-1:1;
            foreach(var vertex in f.sharedMesh.vertices)
            {
                var p=f.transform.TransformPoint(vertex);if(!field.Query(p,out var q))throw new InvalidOperationException("Barrier outside road: "+f.name);
                int bin=Mathf.Clamp(Mathf.RoundToInt(q.station/Bin),0,count-1);envelope[side][bin]=Mathf.Min(envelope[side][bin],sign*q.side);
            }
        }
        foreach(var e in envelope)
        {
            int first=Array.FindIndex(e,x=>!float.IsPositiveInfinity(x));if(first<0)throw new InvalidOperationException("Missing barrier side");
            for(int i=0;i<first;i++)e[i]=e[first];int previous=first;
            for(int i=first+1;i<count;i++)if(!float.IsPositiveInfinity(e[i])){for(int j=previous+1;j<i;j++)e[j]=Mathf.Lerp(e[previous],e[i],(j-previous)/(float)(i-previous));previous=i;}
            for(int i=previous+1;i<count;i++)e[i]=e[previous];
        }
    }
    public Vector3 Map(Vector3 point,bool left)
    {
        if(!field.Query(point,out var q))throw new InvalidOperationException("Barrier vertex outside field");
        int side=left?0:1;float sign=left?-1:1;float f=q.station/Bin;int i=Mathf.Clamp((int)f,0,envelope[side].Length-2);float inner=Mathf.Lerp(envelope[side][i],envelope[side][i+1],f-i);
        return point+q.across*sign*(q.half*1.5f+.30f-inner);
    }
    public Mesh Deform(Mesh original,Transform transform,bool left)
    {
        var mesh=UnityEngine.Object.Instantiate(original);mesh.name=original.name+"_RoadWidth150";var vertices=original.vertices;
        for(int i=0;i<vertices.Length;i++)vertices[i]=transform.InverseTransformPoint(Map(transform.TransformPoint(vertices[i]),left));
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
    }
}
