using System;
using UnityEngine;

public sealed class RoadWidthState : ScriptableObject
{
    public string scenePath,roadId,backupDirectory;
    public bool applied;
    public Mesh originalRoad,widenedRoad;
    public string originalRoadPath;
    public string report;
    public TerrainLink[] terrains=Array.Empty<TerrainLink>();
    public MeshLink[] meshes=Array.Empty<MeshLink>();
    public ColliderLink[] colliders=Array.Empty<ColliderLink>();
    public string profileJson,glassJson;
    [Serializable] public class TerrainLink { public string id; public TerrainData original,generated; public int approvedTreeRemovals; }
    [Serializable] public class MeshLink { public string id; public Mesh original,generated; }
    [Serializable] public class ColliderLink { public string id; public Mesh originalMesh,generatedMesh; public Vector3 center,size,newCenter,newSize; public float radius,height; public bool enabled; }
}
