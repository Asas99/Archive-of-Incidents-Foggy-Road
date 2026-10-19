using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>Used only by the dedicated empty reflection renderer. Never draws the scene renderer lists.</summary>
public sealed class GlassDynamicReflectionFeature : ScriptableRendererFeature
{
    Pass pass;
    public override void Create(){pass=new Pass();}
    public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
    {
        if(GasStationPlanarGlass.DynamicCapture && data.cameraData.camera==GasStationPlanarGlass.DynamicCapture.CaptureCamera)renderer.EnqueuePass(pass);
    }
    sealed class Pass:ScriptableRenderPass
    {
        sealed class Data {public GasStationPlanarGlass owner;}
        public Pass(){renderPassEvent=RenderPassEvent.AfterRenderingOpaques;}
        public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
        {
            using(var builder=graph.AddRasterRenderPass<Data>("Glass / lightweight actors and occlusion",out var data))
            {
                data.owner=GasStationPlanarGlass.DynamicCapture;var resources=frame.Get<UniversalResourceData>();
                builder.SetRenderAttachment(resources.activeColorTexture,0,AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture,AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((Data d,RasterGraphContext context)=>d.owner.DrawDynamicReflection(context.cmd));
            }
        }
        public override void Execute(ScriptableRenderContext context,ref RenderingData data)
        {
            var command=CommandBufferPool.Get("Glass / lightweight actors and occlusion");
            GasStationPlanarGlass.DynamicCapture.DrawDynamicReflection(command);context.ExecuteCommandBuffer(command);CommandBufferPool.Release(command);
        }
    }
}
