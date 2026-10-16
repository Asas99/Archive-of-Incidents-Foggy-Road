using System.Collections.Generic;
using UnityEngine;

/// <summary>Explicit reflected activity. Register on a visible vehicle/body, never on a camera proxy.</summary>
[DisallowMultipleComponent]
public sealed class ReflectionActor : MonoBehaviour
{
    static readonly HashSet<ReflectionActor> actors = new HashSet<ReflectionActor>();
    static readonly List<ReflectionActor> taggedActors = new List<ReflectionActor>();
    static readonly string[] discoveryTags = { "Car", "NPC" };
    static double nextTagDiscovery;
    public static IEnumerable<ReflectionActor> Registered => actors;
    public static int RegisteredCount { get { actors.RemoveWhere(actor=>!actor); return actors.Count; } }
    [Tooltip("Optional changing lights whose illumination also needs a live reflection.")]
    public Light[] activityLights = System.Array.Empty<Light>();
    public bool includeChildLights=true;
    Renderer[] surfaces;
    Light[] childLights;
    Rigidbody body;
    Vector3 previousPosition, estimatedVelocity;
    double previousTime;
    public string Reason => "Actor: " + name;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry() { actors.Clear(); taggedActors.Clear(); nextTagDiscovery=0; }
    /// <summary>Shared discovery for every glass manager; never a per-window scene scan.</summary>
    public static void DiscoverTaggedActors(double now)
    {
        if(now<nextTagDiscovery) return;
        nextTagDiscovery=now+0.5;
        for(int i=taggedActors.Count-1;i>=0;i--)
        {
            var actor=taggedActors[i];
            if(!actor) { taggedActors.RemoveAt(i); continue; }
            if(actor.CompareTag("Car") || actor.CompareTag("NPC")) continue;
            // Only components created by tag discovery are controlled by re-tagging.
            actor.enabled=false;
            taggedActors.RemoveAt(i);
            Destroy(actor);
        }
        foreach(var tag in discoveryTags)
        {
            foreach(var root in GameObject.FindGameObjectsWithTag(tag))
            {
                var actor=root.GetComponent<ReflectionActor>();
                if(actor)
                {
                    if(actor.isActiveAndEnabled) actors.Add(actor);
                    continue;
                }
                actor=Register(root);
                taggedActors.Add(actor);
            }
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RestoreRegistry()
    {
        foreach(var actor in FindObjectsByType<ReflectionActor>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(actor.isActiveAndEnabled) actor.OnEnable();
    }
    void OnEnable()
    {
        RefreshRenderers(); previousPosition=transform.position; previousTime=Time.realtimeSinceStartupAsDouble;
        estimatedVelocity=Vector3.zero; actors.Add(this);
    }
    void OnDisable() { actors.Remove(this); }
    void OnDestroy() { actors.Remove(this); }
    void OnTransformChildrenChanged() { RefreshRenderers(); }
    public void RefreshRenderers() { surfaces=GetComponentsInChildren<Renderer>(true); childLights=GetComponentsInChildren<Light>(true); body=GetComponent<Rigidbody>(); }
    public static ReflectionActor Register(GameObject root)
    {
        var actor=root.GetComponent<ReflectionActor>();
        if(!actor) actor=root.AddComponent<ReflectionActor>();
        actor.RefreshRenderers(); if(actor.isActiveAndEnabled) actors.Add(actor); return actor;
    }
    public bool TryGetBounds(int layers,double now,out Bounds bounds,out bool lightEffect)
    {
        bounds=default; lightEffect=false; bool found=false;
        if(!isActiveAndEnabled) return false;
        if(surfaces==null) RefreshRenderers();
        foreach(var surface in surfaces)
        {
            if(!surface || !surface.enabled || surface.forceRenderingOff || surface.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly || !surface.gameObject.activeInHierarchy || (layers&(1<<surface.gameObject.layer))==0 ||
               surface is ParticleSystemRenderer || surface is TrailRenderer || surface.GetComponent<GlassSurfaceVariation>()) continue;
            if(surface.GetComponentInParent<Camera>()) continue;
            if(surface.bounds.size.sqrMagnitude<0.00001f) continue;
            if(!found) bounds=surface.bounds; else bounds.Encapsulate(surface.bounds); found=true;
        }
        for(int pass=0;pass<2;pass++) foreach(var light in (pass==0 ? activityLights : (includeChildLights ? childLights : System.Array.Empty<Light>())))
        {
            if(!light || !light.isActiveAndEnabled || light.intensity<=0 || light.type==LightType.Directional) continue;
            var volume=new Bounds(light.transform.position,Vector3.one*light.range*2);
            if(!found) bounds=volume; else bounds.Encapsulate(volume); found=true; lightEffect=true;
        }
        double dt=now-previousTime;
        if(dt>0.0001)
        { estimatedVelocity=(transform.position-previousPosition)/(float)dt; previousTime=now; previousPosition=transform.position; }
        if(!found) return false;
        Vector3 velocity=body && !body.isKinematic ? body.linearVelocity : estimatedVelocity;
        // Cover a fast actor's next quarter-second, including its current parked position.
        var predicted=bounds; predicted.center+=Vector3.ClampMagnitude(velocity,100)*0.25f;
        bounds.Encapsulate(predicted); bounds.Expand(0.2f); return true;
    }
}
