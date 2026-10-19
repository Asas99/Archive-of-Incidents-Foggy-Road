using System;
using System.Collections.Generic;
using UnityEngine;

// References existing imported FBXs only. No generated or modified geometry.
public sealed class GlassVehicleLodCatalog : ScriptableObject
{
    [Serializable] public struct Part { public Mesh mesh; public Material[] materials; public Matrix4x4 rootMatrix; public string name; }
    [Serializable] public sealed class Vehicle { public string name; public Part[] high,low; }
    public Vehicle[] vehicles=Array.Empty<Vehicle>();
    public sealed class DrawPart
    {
        public Mesh mesh;public Material[] materials;public Transform anchor;public Matrix4x4 offset;
        public Matrix4x4 Matrix=>anchor.localToWorldMatrix*offset;
    }
    static GlassVehicleLodCatalog cached;
    public static bool TryBind(Renderer[] sources,List<DrawPart> result,out string vehicle)
    {
        result.Clear();vehicle=null;
        if(!cached)cached=Resources.Load<GlassVehicleLodCatalog>("GlassVehicleLodCatalog");
        if(!cached)return false;
        foreach(var entry in cached.vehicles)
        {
            Renderer rootAnchor=null;Part rootPart=default;
            foreach(var source in sources)
            {
                if(!source||!source.gameObject.activeInHierarchy||source is SkinnedMeshRenderer)continue;
                var mesh=source.GetComponent<MeshFilter>()?.sharedMesh;if(!mesh)continue;
                foreach(var p in entry.high)if(p.mesh==mesh){rootAnchor=source;rootPart=p;break;}
                if(rootAnchor)break;
                foreach(var p in entry.low)if(p.mesh==mesh){rootAnchor=source;rootPart=p;break;}
                if(rootAnchor)break;
            }
            if(!rootAnchor)continue;
            foreach(var low in entry.low)
            {
                if(!low.mesh)continue;
                var anchor=rootAnchor;var bind=rootPart;
                // Match the moving original part by its imported part identity.
                foreach(var source in sources)
                {
                    if(!source||!source.gameObject.activeInHierarchy)continue;var mesh=source.GetComponent<MeshFilter>()?.sharedMesh;
                    bool found=false;
                    foreach(var high in entry.high)if(high.mesh==mesh&&high.name==low.name){anchor=source;bind=high;found=true;break;}
                    if(!found&&mesh==low.mesh){anchor=source;bind=low;found=true;}
                    if(found)break;
                }
                result.Add(new DrawPart{mesh=low.mesh,materials=low.materials,anchor=anchor.transform,offset=bind.rootMatrix.inverse*low.rootMatrix});
            }
            vehicle=entry.name;return result.Count>0;
        }
        return false;
    }
}
