using System.Collections.Generic;
using UnityEngine;

public sealed class FoggyRoadOptimizationBackup : ScriptableObject
{
    public bool hasBackup;
    public string scenePath;

    public float lodBias;
    public float shadowDistance;
    public float terrainDetailDensityScale;
    public float terrainDetailDistance;
    public float terrainTreeDistance;
    public float terrainBillboardStart;

    public List<Material> materials = new List<Material>();
    public List<bool> materialInstancing = new List<bool>();
    public List<Terrain> terrains = new List<Terrain>();
    public List<bool> terrainDrawInstanced = new List<bool>();
    public List<float> terrainTreeLodBias = new List<float>();
    public List<int> terrainMaximumFullLodTrees = new List<int>();
    public List<Material> fogMaterials = new List<Material>();
    public List<float> fogSteps = new List<float>();

    public bool hasAdvancedBackup;
    public float terrainPixelError;
    public int shadowCascades;
    public Object screenSpaceAmbientOcclusionFeature;
    public bool screenSpaceAmbientOcclusionActive;

    public bool hasPipelineBackup;
    public float pipelineRenderScale;
    public float pipelineShadowDistance;
    public bool pipelineSupportsHDR;
    public int pipelineMsaaSampleCount;
}
