using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
[DisallowMultipleComponent]
public sealed class SceneOptimizationController : MonoBehaviour
{
 public SceneOptimizationProfile profile;
 public Terrain[] terrains=System.Array.Empty<Terrain>();
 public Terrain[] smallVegetationTerrains=System.Array.Empty<Terrain>();
 public LODGroup[] distanceGroups=System.Array.Empty<LODGroup>();
 public float[] distanceEnds=System.Array.Empty<float>();
 public bool[] smallDistanceGroups=System.Array.Empty<bool>();
 public bool occlusionReady;
 public Material[] nearGrassMaterials=System.Array.Empty<Material>(),smallVegetationMaterials=System.Array.Empty<Material>();
 readonly Dictionary<Camera,bool> oldCameraOcclusion=new Dictionary<Camera,bool>();
 readonly VegetationDensityRuntime vegetationDensity=new VegetationDensityRuntime();
 int lastSignature;bool configured;
 float oldLodBias,oldShadowDistance;bool qualitySaved;
 static readonly int FadeCamera=Shader.PropertyToID("_FoggyOptimizationCamera"),FadeEnabled=Shader.PropertyToID("_FoggyOptimizationEnabled");
 void OnEnable(){if(!profile)return;oldLodBias=QualitySettings.lodBias;oldShadowDistance=QualitySettings.shadowDistance;qualitySaved=true;QualitySettings.lodBias=profile.lodBias;QualitySettings.shadowDistance=profile.shadowDistance;ApplyTerrain();RenderPipelineManager.beginCameraRendering+=Begin;}
 public void ApplyTerrain(){if(!profile)return;foreach(var t in terrains)if(t){t.detailObjectDistance=profile.grassEnd;t.treeDistance=profile.treeEnd;t.treeBillboardDistance=profile.treeBillboardDistance;t.treeCrossFadeLength=profile.treeCrossFadeLength;t.treeMaximumFullLODCount=profile.fullTrees;t.treeLODBiasMultiplier=profile.treeLodBias;t.heightmapPixelError=profile.heightmapPixelError;t.drawInstanced=true;}foreach(var t in smallVegetationTerrains)if(t){t.treeDistance=profile.smallVegetationEnd;t.detailObjectDistance=0;t.detailObjectDensity=0;t.drawHeightmap=false;}vegetationDensity.Apply(terrains,profile.treeDensity,profile.nativeGrassDensity);SetFade(nearGrassMaterials,profile.grassStart,profile.grassEnd);SetFade(smallVegetationMaterials,profile.smallVegetationStart,profile.smallVegetationEnd);}
 static void SetFade(Material[] materials,float start,float end){foreach(var m in materials)if(m&&m.HasProperty("Distance_Fade_Start")){m.SetFloat("Distance_Fade_Start",start);m.SetFloat("Distance_Fade_End",end);}}
 void Begin(ScriptableRenderContext context,Camera camera)
 {
  bool game=camera&&camera.cameraType==CameraType.Game&&camera.targetTexture==null&&camera.gameObject.scene==gameObject.scene;
  if(game&&!FarGrassRenderer.ProfileIncludeMountainCameras&&camera.GetComponent<DistantMountainBackdrop>())game=false;
  Shader.SetGlobalFloat(FadeEnabled,game?1:0);if(!game||!profile)return;
  Shader.SetGlobalVector(FadeCamera,camera.transform.position);
  if(!oldCameraOcclusion.ContainsKey(camera))oldCameraOcclusion.Add(camera,camera.useOcclusionCulling);
  camera.useOcclusionCulling=occlusionReady;
  Shader.SetGlobalVector("_FoggySmallRange",new Vector4(profile.smallStart,Mathf.Max(profile.smallStart+.01f,profile.smallEnd),0,0));
  Shader.SetGlobalVector("_FoggyMediumRange",new Vector4(profile.mediumStart,Mathf.Max(profile.mediumStart+.01f,profile.mediumEnd),0,0));
  Shader.SetGlobalVector("_FoggyTreeRange",new Vector4(profile.treeStart,Mathf.Max(profile.treeStart+.01f,profile.treeEnd),0,0));
  Shader.SetGlobalVector("_FoggyGrassRange",new Vector4(profile.grassStart,Mathf.Max(profile.grassStart+.01f,profile.grassEnd),0,0));
  float fov=camera.orthographic?-camera.orthographicSize:camera.fieldOfView;
  int signature=System.HashCode.Combine(fov,profile.smallEnd,profile.mediumEnd,profile.lodBias,profile.grassStart,profile.grassEnd,profile.treeEnd,System.HashCode.Combine(profile.smallVegetationStart,profile.smallVegetationEnd,profile.treeBillboardDistance,profile.treeCrossFadeLength));
  signature=System.HashCode.Combine(signature,profile.treeDensity,profile.nativeGrassDensity,profile.treeLodBias,profile.fullTrees);
  if(configured&&lastSignature==signature)return;
  configured=true;lastSignature=signature;ApplyTerrain();
  for(int i=0;i<distanceGroups.Length&&i<distanceEnds.Length;i++)
  {
   var g=distanceGroups[i];if(!g)continue;var lods=g.GetLODs();if(lods.Length!=1)continue;
   // Conservative final cull follows shader fade; objects retain their native per-camera frustum culling.
   float size=g.size*Mathf.Max(Mathf.Abs(g.transform.lossyScale.x),Mathf.Abs(g.transform.lossyScale.y),Mathf.Abs(g.transform.lossyScale.z));
   float end=i<smallDistanceGroups.Length?(smallDistanceGroups[i]?profile.smallEnd:profile.mediumEnd):distanceEnds[i];
   float threshold=camera.orthographic?.00001f:Mathf.Clamp(size*profile.lodBias/(2*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)*(end+size*2)),.00001f,.95f);
   lods[0].screenRelativeTransitionHeight=threshold;g.SetLODs(lods);
  }
 }
 public void ResetVegetationDensity(){vegetationDensity.Dispose();}
 void OnDisable(){vegetationDensity.Dispose();RenderPipelineManager.beginCameraRendering-=Begin;Shader.SetGlobalFloat(FadeEnabled,0);configured=false;foreach(var pair in oldCameraOcclusion)if(pair.Key)pair.Key.useOcclusionCulling=pair.Value;oldCameraOcclusion.Clear();if(qualitySaved){QualitySettings.lodBias=oldLodBias;QualitySettings.shadowDistance=oldShadowDistance;qualitySaved=false;}}
}
