using System.Collections.Generic;
using UnityEngine;

/// <summary>Arrival latch: a parked actor cannot keep extending the live window.</summary>
public sealed class GlassArrivalBurst
{
    readonly HashSet<int> present=new HashSet<int>();
    readonly HashSet<int> sampled=new HashSet<int>();
    double until=double.NegativeInfinity;
    public bool Live(double now)=>now<until;
    public void Reset(){present.Clear();sampled.Clear();until=double.NegativeInfinity;}
    public void BeginSample()=>sampled.Clear();
    public void Observe(int id,bool insideEnter,bool insideExit,double now,float seconds)
    {
        if(!insideExit)return;
        if(present.Contains(id)){sampled.Add(id);return;}
        if(!insideEnter)return;
        sampled.Add(id);until=now+Mathf.Max(.1f,seconds);
    }
    public void EndSample(){present.Clear();foreach(var id in sampled)present.Add(id);}
}

public sealed partial class GasStationPlanarGlass
{
    bool UpdateArrivalBurst(WindowGroup group,double now)
    {
        group.arrivalBurst.BeginSample();
        foreach(var actor in ReflectionActor.Registered)
        {
            if(!actor || !actor.isActiveAndEnabled || actor.gameObject.scene!=gameObject.scene)continue;
            bool eligible=actor.LowDrawParts.Count>0;
            for(var t=actor.transform;!eligible&&t;t=t.parent)eligible=t.CompareTag("Car")||t.CompareTag("NPC");
            if(!eligible || !actor.TryGetBounds(reflectionLayers.value,now,out var bounds,out _,false))continue;
            float distance=float.PositiveInfinity;
            foreach(var pane in group.windows)if(pane)distance=Mathf.Min(distance,Vector3.Distance(bounds.ClosestPoint(pane.bounds.center),pane.bounds.ClosestPoint(bounds.center)));
            group.arrivalBurst.Observe(actor.GetInstanceID(),distance<=actorEnterDistance,distance<=Mathf.Max(actorEnterDistance,actorExitDistance),now,group.arrivalBurstSeconds);
        }
        group.arrivalBurst.EndSample();
        return group.arrivalBurst.Live(now);
    }
}
