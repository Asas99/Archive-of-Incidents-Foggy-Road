using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed partial class FarGrassRenderer : MonoBehaviour
{
 internal static bool ProfileIncludeMountainCameras;
 public SceneOptimizationProfile profile;
 public FarGrassCache[] caches=System.Array.Empty<FarGrassCache>();
 public int candidatePatches,visibleInstances,drawCalls,triangles;
 public int totalInstances,testedInstances,rangeRejected,frustumRejected,densityRejected;
 public static readonly List<FarGrassRenderer> Active=new List<FarGrassRenderer>();
 sealed class DrawData {public Mesh mesh;public Material material;public Shader shader;public int pass,triangles;}
 sealed class Batch {public DrawData draw;public Matrix4x4[] matrices=new Matrix4x4[256];public int count;}
 readonly Dictionary<FarGrassCache,DrawData> drawData=new Dictionary<FarGrassCache,DrawData>();
 readonly List<Batch> batches=new List<Batch>();
 readonly Plane[] planes=new Plane[6];
 int used;
 bool warmed;
 MaterialPropertyBlock properties;
#if UNITY_EDITOR
 [System.NonSerialized] public Camera validationCamera;
#endif
 void OnEnable(){if(!Active.Contains(this))Active.Add(this);WarmBuffers();}
 void OnDisable(){Active.Remove(this);ReleaseGpu();}
 public bool Accepts(Camera camera)
 {
  if(!profile||!camera||camera.gameObject.scene!=gameObject.scene)return false;
  if(!ProfileIncludeMountainCameras&&camera.GetComponent<DistantMountainBackdrop>())return false;
#if UNITY_EDITOR
  if(camera==validationCamera)return true;
#endif
  return camera.cameraType==CameraType.Game&&camera.targetTexture==null;
 }
 public void Prepare(Camera camera)
 {
  used=0;candidatePatches=visibleInstances=drawCalls=triangles=0;totalInstances=testedInstances=rangeRejected=frustumRejected=densityRejected=0;if(!profile||!camera)return;
  if(!warmed)WarmBuffers();
  GeometryUtility.CalculateFrustumPlanes(camera,planes);Vector3 eye=camera.transform.position;
  float near2=profile.farGrassStart*profile.farGrassStart,far2=profile.farGrassEnd*profile.farGrassEnd;
  foreach(var cache in caches)
  {
   if(!cache||!cache.mesh||!cache.material)continue;
   if(!drawData.TryGetValue(cache,out var draw)||draw.mesh!=cache.mesh||draw.material!=cache.material||draw.shader!=cache.material.shader)
   {
    draw=new DrawData{mesh=cache.mesh,material=cache.material,shader=cache.material.shader,triangles=(int)cache.mesh.GetIndexCount(0)/3};
    draw.pass=draw.material.FindPass("Universal Forward");if(draw.pass<0)draw.pass=draw.material.FindPass("Universal Forward Only");drawData[cache]=draw;
   }
   if(draw.pass<0)continue;
   Batch current=null;
   foreach(var patch in cache.patches)
   {
    totalInstances+=patch.matrices.Length;
    if(patch.bounds.SqrDistance(eye)>far2){rangeRejected+=patch.matrices.Length;continue;}
    if(!GeometryUtility.TestPlanesAABB(planes,patch.bounds)){frustumRejected+=patch.matrices.Length;continue;}
    candidatePatches++;
    for(int i=0;i<patch.matrices.Length;i++)
    {
     testedInstances++;
     Vector3 origin=patch.matrices[i].GetColumn(3);
     Vector3 delta=origin-eye;float d2=delta.sqrMagnitude;
     if((profile.farGrassStart>0&&d2<=near2)||d2>=far2){rangeRejected++;continue;}
     float radius=patch.radii[i];bool inside=true;
     for(int p=0;p<6;p++)if(planes[p].GetDistanceToPoint(patch.centers[i]) < -radius){inside=false;break;}
     if(!inside){frustumRejected++;continue;}
     if(GrassDensitySampling.DensityCoverage(profile,origin,Mathf.Sqrt(d2))<=0){densityRejected++;continue;}
     if(current==null||current.count==256)
     {
      if(used==batches.Count)batches.Add(new Batch());current=batches[used++];current.draw=draw;current.count=0;
     }
     current.matrices[current.count++]=patch.matrices[i];visibleInstances++;
     triangles+=draw.triangles;
    }
   }
  }
  drawCalls=used;
 }
 void WarmBuffers()
 {
  if(!profile||caches.Length==0)return;int total=0,sources=0;
  foreach(var cache in caches)if(cache){sources++;if(cache.instanceCount>0)total+=cache.instanceCount;else foreach(var patch in cache.patches)total+=patch.matrices.Length;}
  // Conservative full-circle capacity. Frustum and distance thinning normally use a smaller subset.
  int capacity=Mathf.Min(total,Mathf.CeilToInt(Mathf.PI*Mathf.Pow(profile.farGrassEnd+2,2))*sources);
  int count=(capacity+255)/256;while(batches.Count<count)batches.Add(new Batch());warmed=true;
 }
 public void Draw(CommandBuffer cmd)
 {
  if(!profile)return;
  if(properties==null)properties=new MaterialPropertyBlock();
  properties.SetVector("_FoggyFarGrassRange",new Vector4(profile.farGrassStart,profile.farGrassFull,profile.farGrassFade,profile.farGrassEnd));
  properties.SetVector("_FoggyDensityDistances",new Vector4(profile.farDensityFullDistance,profile.farDensityMidDistance,profile.farDensityLowDistance,profile.farGrassDistanceThinning?1:0));
  properties.SetVector("_FoggyDensityValues",new Vector4(profile.farGrassDensity,profile.farDensityMid,profile.farDensityLow,Mathf.Clamp(profile.farDensityFadeHalfWidth,.001f,.1f)));
  for(int i=0;i<used;i++)
  {
   var b=batches[i];cmd.DrawMeshInstanced(b.draw.mesh,0,b.draw.material,b.draw.pass,b.matrices,b.count,properties);
  }
 }
}
