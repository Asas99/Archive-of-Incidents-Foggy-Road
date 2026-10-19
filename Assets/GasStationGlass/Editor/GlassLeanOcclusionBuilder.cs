using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class GlassLeanSetup
{
    static Mesh BuildOcclusion(GasStationPlanarGlass manager)
    {
        var cube=new Mesh();cube.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
        cube.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
        var combined=new List<CombineInstance>();var labels=new List<string>();var temporary=new List<Mesh>();var inverse=manager.transform.worldToLocalMatrix;
        void Box(Transform t,Vector3 centre,Vector3 size){if(size.x>0&&size.y>0&&size.z>0){combined.Add(new CombineInstance{mesh=cube,transform=inverse*t.localToWorldMatrix*Matrix4x4.TRS(centre,Quaternion.identity,size)});labels.Add(t.name);}}
        var doors=UnityEngine.Object.FindObjectsByType<DoorwayColliderAuthoring>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(d=>d.gameObject.scene==manager.gameObject.scene).ToArray();
        var managed=new HashSet<MeshFilter>(doors.SelectMany(d=>new[]{d.first,d.second}).Where(m=>m));
        foreach(var door in doors)foreach(var box in door.generated)if(box&&box.enabled)Box(box.transform,box.center,box.size);
        var station=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="benzinlik"&&t.gameObject.scene==manager.gameObject.scene);
        foreach(var filter in station.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<Renderer>();if(!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy||!filter.sharedMesh||managed.Contains(filter)||filter.GetComponentInParent<ReflectionActor>()||GlassSurfaceVariation.UsesGlass(renderer))continue;
            string label=(filter.name+" "+UnityEditor.AssetDatabase.GetAssetPath(filter.sharedMesh)).ToLowerInvariant();
            if(!new[]{"duvar","plaster","threshold","window","pencere","kolon","column","cube","floor","zemin","plane","ceiling","canopy","platform","frame"}.Any(label.Contains))continue;
            var existing=filter.GetComponent<BoxCollider>();if(existing&&existing.enabled&&!existing.isTrigger){Box(filter.transform,existing.center,existing.size);continue;}
            var bounds=filter.sharedMesh.bounds;var scale=filter.transform.lossyScale;var world=Vector3.Scale(bounds.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
            int depth=world.x<world.y?(world.x<world.z?0:2):(world.y<world.z?1:2);int u=(depth+1)%3,v=(depth+2)%3;
            if(world[depth]>Mathf.Min(world[u],world[v])*0.3f){if(label.Contains("kolon")||label.Contains("column")||label.Contains("cube"))Box(filter.transform,bounds.center,bounds.size);continue;}
            const int n=48;var occupied=new bool[n,n];
            using(var data=MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))using(var native=new NativeArray<Vector3>(filter.sharedMesh.vertexCount,Allocator.Temp))
            {
                var mesh=data[0];mesh.GetVertices(native);var vertices=native.ToArray();bool wide=mesh.indexFormat==IndexFormat.UInt32;var large=wide?mesh.GetIndexData<uint>():default;var small=wide?default:mesh.GetIndexData<ushort>();
                Vector2 Project(int index)=>new Vector2((vertices[index][u]-bounds.min[u])/bounds.size[u]*n,(vertices[index][v]-bounds.min[v])/bounds.size[v]*n);
                float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
                for(int part=0;part<mesh.subMeshCount;part++)
                {
                    var sub=mesh.GetSubMesh(part);if(sub.topology!=MeshTopology.Triangles)continue;
                    for(int j=sub.indexStart;j<sub.indexStart+sub.indexCount;j+=3)
                    {
                        var a=Project((wide?(int)large[j]:small[j])+sub.baseVertex);var b=Project((wide?(int)large[j+1]:small[j+1])+sub.baseVertex);var c=Project((wide?(int)large[j+2]:small[j+2])+sub.baseVertex);float area=Cross(b-a,c-a);if(Mathf.Abs(area)<0.00001f)continue;
                        int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))),0,n-1),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))),0,n-1),y0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))),0,n-1),y1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))),0,n-1);
                        for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){var p=new Vector2(x+.5f,y+.5f);float ab=Cross(b-a,p-a)/area,ac=Cross(p-a,c-a)/area;if(ab>=0&&ac>=0&&ab+ac<=1)occupied[x,y]=true;}
                    }
                }
            }
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)if(occupied[x,y])
            {
                int width=1,height=1;while(x+width<n&&occupied[x+width,y])width++;
                bool full=true;while(y+height<n&&full){for(int k=0;k<width;k++)if(!occupied[x+k,y+height]){full=false;break;}if(full)height++;}
                for(int yy=0;yy<height;yy++)for(int xx=0;xx<width;xx++)occupied[x+xx,y+yy]=false;
                var centre=bounds.center;var size=bounds.size;centre[u]=bounds.min[u]+(x+width*.5f)/n*bounds.size[u];centre[v]=bounds.min[v]+(y+height*.5f)/n*bounds.size[v];size[u]*=width/(float)n;size[v]*=height/(float)n;size[depth]=Mathf.Max(size[depth],.04f/Mathf.Max(.001f,Mathf.Abs(scale[depth])));Box(filter.transform,centre,size);
            }
        }
        // Some imported window meshes already have a broad physics box. That is useful
        // for collisions, but must never seal a glass opening in reflection depth.
        // Subtract the actual registered pane rectangles from coplanar structural boxes.
        var cutBoxes=new List<CombineInstance>();var cutLabels=new List<string>();
        for(int index=0;index<combined.Count;index++)
        {
            var item=combined[index];var worldMatrix=manager.transform.localToWorldMatrix*item.transform;var toLocal=worldMatrix.inverse;
            var pieces=new List<Bounds>{new Bounds(Vector3.zero,Vector3.one)};
            foreach(var group in manager.groups)foreach(var pane in group.windows)if(pane)
            {
                var normal=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
                int depth=0;float alignment=0;for(int axis=0;axis<3;axis++){var unit=Vector3.zero;unit[axis]=1;float dot=Mathf.Abs(Vector3.Dot(worldMatrix.MultiplyVector(unit).normalized,normal));if(dot>alignment){alignment=dot;depth=axis;}}
                if(alignment<.995f)continue;
                var filter=pane.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                var paneBoundsLocal=filter.sharedMesh.bounds;var min=Vector3.one*float.PositiveInfinity;var max=Vector3.one*float.NegativeInfinity;
                for(int corner=0;corner<8;corner++){var vertex=paneBoundsLocal.center+Vector3.Scale(paneBoundsLocal.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));var local=toLocal.MultiplyPoint3x4(pane.transform.TransformPoint(vertex));min=Vector3.Min(min,local);max=Vector3.Max(max,local);}
                var axisVector=Vector3.zero;axisVector[depth]=1;float padding=.15f/Mathf.Max(.001f,worldMatrix.MultiplyVector(axisVector).magnitude);
                if(min[depth]>.5f+padding||max[depth]<-.5f-padding)continue;
                int u=(depth+1)%3,v=(depth+2)%3;var next=new List<Bounds>();
                foreach(var piece in pieces)
                {
                    var a=piece.min;var b=piece.max;float left=Mathf.Max(a[u],min[u]),right=Mathf.Min(b[u],max[u]),bottom=Mathf.Max(a[v],min[v]),top=Mathf.Min(b[v],max[v]);
                    if(right<=left||top<=bottom){next.Add(piece);continue;}
                    void Slab(float loU,float hiU,float loV,float hiV){if(hiU-loU<1e-5f||hiV-loV<1e-5f)return;var lo=a;var hi=b;lo[u]=loU;hi[u]=hiU;lo[v]=loV;hi[v]=hiV;var bounds=new Bounds();bounds.SetMinMax(lo,hi);next.Add(bounds);}
                    Slab(a[u],left,a[v],b[v]);Slab(right,b[u],a[v],b[v]);Slab(left,right,a[v],bottom);Slab(left,right,top,b[v]);
                }
                pieces=next;
            }
            foreach(var piece in pieces){cutBoxes.Add(new CombineInstance{mesh=cube,transform=item.transform*Matrix4x4.TRS(piece.center,Quaternion.identity,piece.size)});cutLabels.Add(labels[index]);}
        }
        combined=cutBoxes;labels=cutLabels;
        var paneBounds=manager.groups.SelectMany(g=>g.windows).Where(w=>w).Select(w=>w.bounds).ToArray();var regionBounds=paneBounds[0];foreach(var b in paneBounds)regionBounds.Encapsulate(b);regionBounds.Expand(120);
        foreach(var terrain in Terrain.activeTerrains)
        {
            const int n=32;var vertices=new Vector3[(n+1)*(n+1)];var triangles=new int[n*n*6];var min=Vector3.Max(regionBounds.min,terrain.transform.position);var max=Vector3.Min(regionBounds.max,terrain.transform.position+terrain.terrainData.size);if(max.x<=min.x||max.z<=min.z)continue;
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){var p=new Vector3(Mathf.Lerp(min.x,max.x,x/(float)n),0,Mathf.Lerp(min.z,max.z,z/(float)n));p.y=terrain.SampleHeight(p)+terrain.transform.position.y;vertices[z*(n+1)+x]=inverse.MultiplyPoint3x4(p);}
            int t=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x,b=a+n+1;triangles[t++]=a;triangles[t++]=b;triangles[t++]=a+1;triangles[t++]=a+1;triangles[t++]=b;triangles[t++]=b+1;}
            var mesh=new Mesh{vertices=vertices,triangles=triangles};temporary.Add(mesh);combined.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});
        }
        var trace=new System.Text.StringBuilder();
        foreach(var group in manager.groups)foreach(var window in group.windows)if(window)
        {
            var p=group.plane.TransformPoint(group.localPoint);var n=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
            foreach(int side in new[]{-1,1})for(int i=0;i<labels.Count;i++)
            {
                var matrix=(manager.transform.localToWorldMatrix*combined[i].transform).inverse;var start=matrix.MultiplyPoint3x4(p+n*side*.03f);var delta=matrix.MultiplyVector(n*side*2.47f);float near=0,far=1;
                for(int axis=0;axis<3;axis++){if(Mathf.Abs(delta[axis])<1e-8f){if(Mathf.Abs(start[axis])>.5f)far=-1;}else{float a=(-.5f-start[axis])/delta[axis],b=(.5f-start[axis])/delta[axis];near=Mathf.Max(near,Mathf.Min(a,b));far=Mathf.Min(far,Mathf.Max(a,b));}}
                if(near<=far)trace.AppendLine(window.name+" side="+side+" blocked by "+labels[i]+" index="+i);
            }
        }
        System.IO.File.WriteAllText(Output+"/OcclusionTrace.txt",trace.ToString());
        var result=new Mesh{name="Station lightweight reflection occlusion",indexFormat=IndexFormat.UInt32};result.CombineMeshes(combined.ToArray(),true,true);result.RecalculateBounds();
        foreach(var mesh in temporary)UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(cube);return result;
    }
}

