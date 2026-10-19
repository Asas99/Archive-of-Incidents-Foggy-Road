using System;
using UnityEngine;
using UnityEngine.Rendering;

// Reused by all vegetation sources for this camera pass. No camera or shadow map is created.
public sealed class GrassGpuDepth : IDisposable
{
 public RenderTexture pyramid { get; private set; }
 RenderTexture[] levels;
 readonly int[] sourceSize=new int[2],outputSize=new int[2];
 public int width, height, mipCount;
 static RenderTexture Create(int w, int h, bool mip)
 {
  var rt = new RenderTexture(w,h,0,RenderTextureFormat.RFloat,RenderTextureReadWrite.Linear) {
   name="Grass conservative depth",hideFlags=HideFlags.HideAndDontSave,enableRandomWrite=true,
   useMipMap=mip,autoGenerateMips=false,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
  if(!rt.Create())throw new InvalidOperationException("Grass depth texture could not be created");return rt;
 }
 public void Build(CommandBuffer cmd, ComputeShader shader, RenderTargetIdentifier source, int w, int h)
 {
  int pw=Mathf.NextPowerOfTwo(w),ph=Mathf.NextPowerOfTwo(h);
  if(!pyramid||pyramid.width!=pw||pyramid.height!=ph)
  {
   Dispose();mipCount=1+(int)Mathf.Log(Mathf.Max(pw,ph),2);pyramid=Create(pw,ph,true);
   levels=new RenderTexture[mipCount];for(int i=0;i<mipCount;i++)levels[i]=Create(Mathf.Max(1,pw>>i),Mathf.Max(1,ph>>i),false);
  }
  width=w;height=h;
  int copy=shader.FindKernel("CopyDepth"),reduce=shader.FindKernel("ReduceDepth");
  cmd.SetComputeIntParam(shader,"_ReversedZ",SystemInfo.usesReversedZBuffer?1:0);
  sourceSize[0]=w;sourceSize[1]=h;outputSize[0]=pw;outputSize[1]=ph;
  cmd.SetComputeIntParams(shader,"_SourceSize",sourceSize);cmd.SetComputeIntParams(shader,"_OutputSize",outputSize);
  cmd.SetComputeTextureParam(shader,copy,"_SourceDepth",source,0,RenderTextureSubElement.Depth);
  cmd.SetComputeTextureParam(shader,copy,"_PyramidWrite",levels[0]);cmd.DispatchCompute(shader,copy,(pw+7)/8,(ph+7)/8,1);
  cmd.CopyTexture(levels[0],0,0,pyramid,0,0);
  // Separate read/write images avoid overlapping SRV/UAV mip bindings on DX11.
  for(int i=1;i<mipCount;i++)
  {
   var previous=levels[i-1];var next=levels[i];
   sourceSize[0]=previous.width;sourceSize[1]=previous.height;outputSize[0]=next.width;outputSize[1]=next.height;
   cmd.SetComputeIntParams(shader,"_SourceSize",sourceSize);cmd.SetComputeIntParams(shader,"_OutputSize",outputSize);
   cmd.SetComputeIntParam(shader,"_ReadMip",0);cmd.SetComputeTextureParam(shader,reduce,"_PyramidRead",previous);
   cmd.SetComputeTextureParam(shader,reduce,"_PyramidWrite",next);cmd.DispatchCompute(shader,reduce,(next.width+7)/8,(next.height+7)/8,1);
   cmd.CopyTexture(next,0,0,pyramid,0,i);
  }
 }
 public void Dispose()
 {
  if(levels!=null)foreach(var rt in levels)Destroy(rt);levels=null;Destroy(pyramid);pyramid=null;
 }
 static void Destroy(RenderTexture rt){if(!rt)return;rt.Release();if(Application.isPlaying)UnityEngine.Object.Destroy(rt);else UnityEngine.Object.DestroyImmediate(rt);}
}
