using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public sealed class FarGrassFeature : ScriptableRendererFeature
{
 public ComputeShader grassCulling;
 public Shader indirectGrassShader;
 GrassPass pass;
 public override void Create(){pass?.Dispose();pass=new GrassPass(this);}
 protected override void Dispose(bool disposing){pass?.Dispose();foreach(var grass in FarGrassRenderer.Active)if(grass)grass.ReleaseGpu();}
 public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
 {
  bool work=false;foreach(var grass in FarGrassRenderer.Active)if(grass&&grass.Accepts(data.cameraData.camera))
  {
   if(grass.CanUseGpu(data.cameraData.camera,grassCulling,indirectGrassShader,data.cameraData.cameraTargetDescriptor.msaaSamples))
   {
    try{grass.PrepareGpu(data.cameraData.camera,indirectGrassShader);}
    catch(System.Exception e){grass.ReleaseGpu();grass.UseCpuFallback(e.Message);grass.Prepare(data.cameraData.camera);}
   }
   else {grass.ReleaseGpu();grass.Prepare(data.cameraData.camera);}
   work|=grass.drawCalls>0;
  }
  if(work)renderer.EnqueuePass(pass);
 }
 sealed class GrassPass:ScriptableRenderPass
 {
  sealed class Data {public Camera camera;public TextureHandle colour,depth;public Matrix4x4 projection,view;public UniversalCameraData cameraData;public int width,height;public bool gpu;}
  readonly FarGrassFeature owner;readonly GrassGpuDepth pyramid=new GrassGpuDepth();
  public GrassPass(FarGrassFeature owner){this.owner=owner;renderPassEvent=RenderPassEvent.BeforeRenderingTransparents;}
  public void Dispose(){pyramid.Dispose();}
  public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
  {
   var r=frame.Get<UniversalResourceData>();
   using(var b=graph.AddUnsafePass<Data>("Vegetation / visible far grass",out var d))
   {
    var cameraData=frame.Get<UniversalCameraData>();d.camera=cameraData.camera;
    d.projection=cameraData.GetProjectionMatrix();d.view=cameraData.GetViewMatrix();d.cameraData=cameraData;
    d.width=cameraData.cameraTargetDescriptor.width;d.height=cameraData.cameraTargetDescriptor.height;
    foreach(var grass in FarGrassRenderer.Active)if(grass&&grass.Accepts(d.camera)&&grass.gpuActive&&grass.drawCalls>0)d.gpu=true;
    d.colour=r.activeColorTexture;d.depth=r.activeDepthTexture;b.UseTexture(d.colour,AccessFlags.ReadWrite);b.UseTexture(d.depth,AccessFlags.ReadWrite);
    if(r.mainShadowsTexture.IsValid())b.UseTexture(r.mainShadowsTexture,AccessFlags.Read);
    if(r.additionalShadowsTexture.IsValid())b.UseTexture(r.additionalShadowsTexture,AccessFlags.Read);
    b.AllowPassCulling(false);
    b.SetRenderFunc((Data data,UnsafeGraphContext context)=>{
     var cmd=CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
     bool gpu=data.gpu;
     if(gpu)
     {
      // Unbind the writable depth attachment before reading it as a compute SRV.
      context.cmd.SetRenderTarget(data.colour);
      try{pyramid.Build(cmd,owner.grassCulling,data.depth,data.width,data.height);}
      catch(System.Exception e){gpu=false;foreach(var grass in FarGrassRenderer.Active)if(grass&&grass.Accepts(data.camera)){grass.UseCpuFallback(e.Message);grass.Prepare(data.camera);}}
     }
     context.cmd.SetRenderTarget(data.colour,data.depth);
     foreach(var grass in FarGrassRenderer.Active)if(grass&&grass.Accepts(data.camera))
     {if(gpu&&grass.gpuActive)grass.DrawGpu(cmd,owner.grassCulling,pyramid,data.camera,GL.GetGPUProjectionMatrix(data.projection,data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.colour,data.depth))*data.view);else grass.Draw(cmd);}
    });
   }
  }
  public override void Execute(ScriptableRenderContext context,ref RenderingData data)
  {
   var cmd=CommandBufferPool.Get("Vegetation / visible far grass");foreach(var grass in FarGrassRenderer.Active)if(grass&&grass.Accepts(data.cameraData.camera)){grass.UseCpuFallback("Render Graph compatibility mode");grass.Prepare(data.cameraData.camera);grass.Draw(cmd);}context.ExecuteCommandBuffer(cmd);CommandBufferPool.Release(cmd);
  }
 }
}
