using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class FarGrassRenderer
{
 [StructLayout(LayoutKind.Sequential)] struct Instance {public Matrix4x4 matrix,inverseMatrix;public Vector4 bounds;}
 [StructLayout(LayoutKind.Sequential)] struct Range {public uint start,count;}
 sealed class GpuCache : IDisposable
 {
  public FarGrassCache cache;public FarGrassCache.Patch[] source;public Material material;
  public ComputeBuffer instances,visible,args,ranges,counters;public Range[] candidates,all;public int count,candidateInstances;
  public Mesh mesh;public Material original;public float windPadding,windSignature;
  public void Dispose(){instances?.Release();visible?.Release();args?.Release();ranges?.Release();counters?.Release();if(material){if(Application.isPlaying)UnityEngine.Object.Destroy(material);else UnityEngine.Object.DestroyImmediate(material);}}
 }
 readonly Dictionary<FarGrassCache,GpuCache> gpuCaches=new Dictionary<FarGrassCache,GpuCache>();
 readonly List<GpuCache> gpuDraws=new List<GpuCache>();
 readonly List<FarGrassCache> staleGpuCaches=new List<FarGrassCache>();
 readonly Vector4[] gpuPlanes=new Vector4[6];
 readonly uint[] zeroCounters=new uint[5];
 public bool gpuActive {get;private set;}
 public string gpuStatus {get;private set;}="CPU — henüz çizilmedi";
 public bool gpuCountersValid {get;private set;}
 public int gpuVisible,gpuOccluded,gpuRangeRejected,gpuFrustumRejected,gpuDensityRejected;
 bool readGpuCounters,counterFailed;int counterVersion,pendingCounters;
 public void RequestGpuCounters(){readGpuCounters=true;gpuCountersValid=false;}
 public bool CanUseGpu(Camera camera,ComputeShader compute,Shader shader,int msaa)
 {
  string reason=null;
  var api=SystemInfo.graphicsDeviceType;
  if(!profile||!profile.farGrassGpuOcclusion)reason="profilde kapalı";
  else if(!compute||!shader||!shader.isSupported)reason="GPU kaynakları hazır değil";
  else if(!SystemInfo.supportsComputeShaders||!SystemInfo.supportsInstancing||SystemInfo.graphicsShaderLevel<45||!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat))reason="GPU desteği yok";
  else if(api!=GraphicsDeviceType.Direct3D11&&api!=GraphicsDeviceType.Direct3D12)reason="bu grafik API'si doğrulanmadı";
  else if(msaa!=1||camera.stereoEnabled||camera.orthographic||camera.rect!=new Rect(0,0,1,1))reason="kamera/MSAA düzeni desteklenmiyor";
  gpuActive=reason==null;gpuStatus=gpuActive?"GPU — mesafe / görüş / yoğunluk / örtülme":"CPU — "+reason;
  return gpuActive;
 }
 GpuCache GetGpu(FarGrassCache cache,Shader shader)
 {
  float wind=0;foreach(var property in windProperties)if(cache.material.HasProperty(property))wind+=Mathf.Abs(cache.material.GetFloat(property));
  if(gpuCaches.TryGetValue(cache,out var old)&&ReferenceEquals(old.source,cache.patches)&&old.mesh==cache.mesh&&old.original==cache.material&&old.material.shader==shader&&old.windSignature==wind)return old;
  old?.Dispose();gpuCaches.Remove(cache);
  int n=0;foreach(var patch in cache.patches)n+=patch.matrices.Length;if(n==0)return null;
  var data=new Instance[n];var all=new Range[cache.patches.Length];int offset=0;float padding=0;
  for(int p=0;p<cache.patches.Length;p++)
  {
   var patch=cache.patches[p];all[p]=new Range{start=(uint)offset,count=(uint)patch.matrices.Length};
   for(int i=0;i<patch.matrices.Length;i++) {var m=patch.matrices[i];var c=patch.centers[i];float extra=2*wind*Mathf.Max(m.GetColumn(0).magnitude,m.GetColumn(1).magnitude,m.GetColumn(2).magnitude);padding=Mathf.Max(padding,extra);data[offset++]=new Instance{matrix=m,inverseMatrix=m.inverse,bounds=new Vector4(c.x,c.y,c.z,patch.radii[i]+extra)};}
  }
  var result=new GpuCache{cache=cache,source=cache.patches,mesh=cache.mesh,original=cache.material,all=all,candidates=new Range[all.Length],windPadding=padding,windSignature=wind};
  try
  {
   result.instances=new ComputeBuffer(n,144);result.instances.SetData(data);
   result.visible=new ComputeBuffer(n,4,ComputeBufferType.Append);result.args=new ComputeBuffer(1,20,ComputeBufferType.IndirectArguments);
   result.args.SetData(new uint[]{cache.mesh.GetIndexCount(0),0,cache.mesh.GetIndexStart(0),(uint)cache.mesh.GetBaseVertex(0),0});
   result.ranges=new ComputeBuffer(Mathf.Max(1,all.Length),8);result.counters=new ComputeBuffer(5,4);
   result.material=new Material(cache.material){shader=shader,enableInstancing=true,hideFlags=HideFlags.HideAndDontSave};
   gpuCaches.Add(cache,result);return result;
  }
  catch {result.Dispose();throw;}
 }
 public void PrepareGpu(Camera camera,Shader shader)
 {
  gpuDraws.Clear();candidatePatches=visibleInstances=drawCalls=triangles=0;totalInstances=testedInstances=rangeRejected=frustumRejected=densityRejected=0;
  GeometryUtility.CalculateFrustumPlanes(camera,planes);for(int i=0;i<6;i++)gpuPlanes[i]=new Vector4(planes[i].normal.x,planes[i].normal.y,planes[i].normal.z,planes[i].distance);
  if(profile.farGrassDensity<=0)return;
  staleGpuCaches.Clear();foreach(var pair in gpuCaches)if(Array.IndexOf(caches,pair.Key)<0)staleGpuCaches.Add(pair.Key);
  foreach(var stale in staleGpuCaches){gpuCaches[stale].Dispose();gpuCaches.Remove(stale);}
  Vector3 eye=camera.transform.position;float far2=profile.farGrassEnd*profile.farGrassEnd;
  foreach(var cache in caches)
  {
   if(!cache||!cache.mesh||!cache.material)continue;
   var gpu=GetGpu(cache,shader);if(gpu==null)continue;gpu.count=0;gpu.candidateInstances=0;
   for(int p=0;p<cache.patches.Length;p++)
   {
    var patch=cache.patches[p];totalInstances+=patch.matrices.Length;
    var expanded=patch.bounds;expanded.Expand(gpu.windPadding*2);
    if(expanded.SqrDistance(eye)>far2){rangeRejected+=patch.matrices.Length;continue;}
    if(!GeometryUtility.TestPlanesAABB(planes,expanded)){frustumRejected+=patch.matrices.Length;continue;}
    gpu.candidates[gpu.count++]=gpu.all[p];gpu.candidateInstances+=patch.matrices.Length;candidatePatches++;testedInstances+=patch.matrices.Length;
   }
   if(gpu.count>0){gpu.ranges.SetData(gpu.candidates,0,0,gpu.count);gpuDraws.Add(gpu);}
  }
  drawCalls=gpuDraws.Count;
 }
 public void DrawGpu(CommandBuffer cmd,ComputeShader compute,GrassGpuDepth depth,Camera camera,Matrix4x4 vp)
 {
  int kernel=compute.FindKernel("Cull");
  var range=new Vector4(profile.farGrassStart,profile.farGrassFull,profile.farGrassFade,profile.farGrassEnd);
  var distances=new Vector4(profile.farDensityFullDistance,profile.farDensityMidDistance,profile.farDensityLowDistance,profile.farGrassDistanceThinning?1:0);
  var values=new Vector4(profile.farGrassDensity,profile.farDensityMid,profile.farDensityLow,profile.farDensityFadeHalfWidth);
  cmd.SetComputeMatrixParam(compute,"_ViewProjection",vp);cmd.SetComputeVectorArrayParam(compute,"_Planes",gpuPlanes);
  cmd.SetComputeVectorParam(compute,"_Eye",camera.transform.position);cmd.SetComputeFloatParam(compute,"_NearClip",camera.nearClipPlane);
  cmd.SetComputeVectorParam(compute,"_Viewport",new Vector4(depth.width,depth.height,depth.pyramid.width,depth.pyramid.height));cmd.SetComputeIntParam(compute,"_MipCount",depth.mipCount);
  cmd.SetComputeVectorParam(compute,"_GrassRange",range);cmd.SetComputeVectorParam(compute,"_DensityDistances",distances);cmd.SetComputeVectorParam(compute,"_DensityValues",values);
  cmd.SetComputeIntParam(compute,"_ReversedZ",SystemInfo.usesReversedZBuffer?1:0);cmd.SetComputeTextureParam(compute,kernel,"_PyramidRead",depth.pyramid);
  bool read=readGpuCounters&&SystemInfo.supportsAsyncGPUReadback;int version=counterVersion;
  if(read){readGpuCounters=false;counterFailed=false;version=++counterVersion;pendingCounters=gpuDraws.Count;gpuVisible=gpuOccluded=gpuRangeRejected=gpuFrustumRejected=gpuDensityRejected=0;}
  foreach(var gpu in gpuDraws)
  {
   cmd.SetBufferCounterValue(gpu.visible,0);cmd.SetBufferData(gpu.counters,zeroCounters);
   cmd.SetComputeIntParam(compute,"_PatchCount",gpu.count);cmd.SetComputeBufferParam(compute,kernel,"_GrassInstances",gpu.instances);
   cmd.SetComputeBufferParam(compute,kernel,"_PatchRanges",gpu.ranges);cmd.SetComputeBufferParam(compute,kernel,"_VisibleGrass",gpu.visible);cmd.SetComputeBufferParam(compute,kernel,"_Counters",gpu.counters);
   cmd.DispatchCompute(compute,kernel,gpu.count,1,1);cmd.CopyCounterValue(gpu.visible,gpu.args,4);
   if(properties==null)properties=new MaterialPropertyBlock();properties.Clear();
   properties.SetVector("_FoggyFarGrassRange",range);properties.SetVector("_FoggyDensityDistances",distances);properties.SetVector("_FoggyDensityValues",values);
   properties.SetBuffer("_GrassInstances",gpu.instances);properties.SetBuffer("_VisibleGrass",gpu.visible);
   cmd.DrawMeshInstancedIndirect(gpu.mesh,0,gpu.material,gpu.material.FindPass("Universal Forward"),gpu.args,0,properties);
   if(read)
   {
    int captured=version,candidateCount=gpu.candidateInstances;
    cmd.RequestAsyncReadback(gpu.counters,request=>{
     if(!this||captured!=counterVersion)return;
     if(!request.hasError){var result=request.GetData<uint>();gpuVisible+=(int)result[0];gpuOccluded+=(int)result[1];gpuRangeRejected+=(int)result[2];gpuDensityRejected+=(int)result[4];
      // Every submitted candidate has exactly one outcome. Derive frustum rejects
      // from conservation instead of another atomic on the early-exit path.
      gpuFrustumRejected+=Mathf.Max(0,candidateCount-(int)(result[0]+result[1]+result[2]+result[4]));}
     counterFailed|=request.hasError;if(--pendingCounters==0)gpuCountersValid=!counterFailed;
    });
   }
  }
 }
 public void UseCpuFallback(string reason){gpuActive=false;gpuStatus="CPU — "+reason;}
 public void ReleaseGpu(){counterVersion++;gpuCountersValid=false;foreach(var pair in gpuCaches)pair.Value.Dispose();gpuCaches.Clear();gpuDraws.Clear();}
 static readonly string[] windProperties={"Wind_Intensity","Wind_Blast","Wind_Ripples"};
}
