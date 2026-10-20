using System;
using System.Collections.Generic;
using UnityEngine;

public static class RoadVegetationAdapter
{
    public static (int moved,int grounded) UpdateTrees(TerrainData data,Vector3 origin,TerrainData ground,Vector3 groundOrigin,RoadWidthGeometry field)
    {
        var trees=data.treeInstances;var size=data.size;var occupied=new Dictionary<Vector2Int,List<int>>();var positions=new Vector3[trees.Length];
        Vector2Int Key(Vector3 p)=>new Vector2Int(Mathf.FloorToInt(p.x/3),Mathf.FloorToInt(p.z/3));
        void Add(int i){var k=Key(positions[i]);if(!occupied.TryGetValue(k,out var ids))occupied[k]=ids=new List<int>();ids.Add(i);}
        for(int i=0;i<trees.Length;i++){positions[i]=origin+Vector3.Scale(trees[i].position,size);Add(i);}
        bool Free(Vector3 p,int self,float radius)
        {
            var k=Key(p);for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)if(occupied.TryGetValue(k+new Vector2Int(x,z),out var ids))foreach(int i in ids)if(i!=self&&new Vector2(p.x-positions[i].x,p.z-positions[i].z).sqrMagnitude<Mathf.Pow(radius+.45f,2))return false;
            return true;
        }
        float Height(Vector3 p)=>groundOrigin.y+ground.GetInterpolatedHeight((p.x-groundOrigin.x)/ground.size.x,(p.z-groundOrigin.z)/ground.size.z);
        int moved=0,grounded=0;var hits=new Collider[32];
        for(int i=0;i<trees.Length;i++)
        {
            var tree=trees[i];var p=positions[i];if(!field.Query(p,out var q)||q.Outside(1.5f)>6.75f)continue;
            var prototype=data.treePrototypes[tree.prototypeIndex].prefab;
            float radius=.55f*Mathf.Max(.5f,tree.widthScale);
            bool small=!prototype||prototype.name.IndexOf("fern",StringComparison.OrdinalIgnoreCase)>=0||prototype.name.IndexOf("grass",StringComparison.OrdinalIgnoreCase)>=0||prototype.name.IndexOf("plant",StringComparison.OrdinalIgnoreCase)>=0;
            if(small)radius=.3f;
            if(q.Outside(1.5f)<.75f+radius)
            {
                float sign=q.side>=0?1:-1;var tangent=Vector3.Cross(q.across,Vector3.up).normalized;bool found=false;
                for(int outward=0;outward<32&&!found;outward++)for(int along=0;along<17&&!found;along++)
                {
                    float shift=along==0?0:((along+1)/2)*(along%2==0?-1:1)*1.5f;
                    var candidate=q.center+q.across*sign*(q.half*1.5f+.75f+radius+outward*.75f)+tangent*shift;
                    float u=(candidate.x-origin.x)/size.x,v=(candidate.z-origin.z)/size.z;
                    if(u<0||u>1||v<0||v>1||!field.Query(candidate,out var test)||test.side*sign<0||test.Outside(1.5f)<.75f+radius-.01f||!Free(candidate,i,radius))continue;
                    float gu=(candidate.x-groundOrigin.x)/ground.size.x,gv=(candidate.z-groundOrigin.z)/ground.size.z;
                    if(ground.GetSteepness(gu,gv)>38)continue;
                    candidate.y=Height(candidate);
                    int count=Physics.OverlapSphereNonAlloc(candidate+Vector3.up*(radius+.2f),radius,hits,~0,QueryTriggerInteraction.Ignore);bool blocked=count==hits.Length;
                    for(int h=0;h<count;h++)if(!(hits[h] is TerrainCollider)){blocked=true;break;}
                    if(blocked)continue;
                    occupied[Key(positions[i])].Remove(i);positions[i]=candidate;Add(i);p=candidate;found=true;
                }
                if(!found)throw new InvalidOperationException("No safe same-side tree position: "+i+" "+prototype?.name);
                moved++;
            }
            float y=Height(p);if(Mathf.Abs(p.y-y)>.001f)grounded++;p.y=y;tree.position=new Vector3((p.x-origin.x)/size.x,(p.y-origin.y)/size.y,(p.z-origin.z)/size.z);trees[i]=tree;
        }
        data.treeInstances=trees;return(moved,grounded);
    }
}
