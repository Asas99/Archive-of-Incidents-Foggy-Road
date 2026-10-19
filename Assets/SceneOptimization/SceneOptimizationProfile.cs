using UnityEngine;
[CreateAssetMenu(menuName="Foggy Road/Scene Optimization Profile")]
public sealed class SceneOptimizationProfile : ScriptableObject
{
 public float grassStart=25,grassEnd=35,smallStart=65,smallEnd=80,mediumStart=100,mediumEnd=120,treeStart=270,treeEnd=300;
 // The lightweight base stays visible nearby too; near Terrain details only add variety.
 public float farGrassStart=0,farGrassFull=0,farGrassFade=130,farGrassEnd=150;
 [Range(0,1)] public float farGrassDensity=1;
 public bool farGrassDistanceThinning=true;
 public bool farGrassGpuOcclusion;
 public float farDensityFullDistance=60,farDensityMidDistance=100,farDensityLowDistance=130;
 [Range(0,1)] public float farDensityMid=.7f,farDensityLow=.45f;
 [Range(.001f,.1f)] public float farDensityFadeHalfWidth=.04f;
 public float smallVegetationStart=80,smallVegetationEnd=100,treeBillboardDistance=200,treeCrossFadeLength=40;
 public float lodBias=1.2f,shadowDistance=70,heightmapPixelError=4;
 public int shadowAtlas=2048,shadowCascades=2,fullTrees=400;
}
