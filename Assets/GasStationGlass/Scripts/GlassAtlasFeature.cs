using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public sealed class GlassAtlasFeature : ScriptableRendererFeature
{
    AtlasPass pass;
    public override void Create(){pass=new AtlasPass();}
    public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
    {
        var owner=GasStationPlanarGlass.AtlasOwner;
        if(owner&&owner.UsesAtlas&&owner.HasAtlasWork&&owner.LastSourceCamera==data.cameraData.camera)renderer.EnqueuePass(pass);
    }
    sealed class AtlasPass:ScriptableRenderPass
    {
        sealed class Data {public GasStationPlanarGlass owner;public TextureHandle colour,depth;public Rect viewport;public bool shadow;public int cascades;}
        public AtlasPass(){renderPassEvent=RenderPassEvent.AfterRenderingOpaques;}
        public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
        {
            var owner=GasStationPlanarGlass.AtlasOwner;if(!owner||!owner.HasAtlasWork)return;
            using(var builder=graph.AddUnsafePass<Data>("Glass / shared lightweight atlas",out var data))
            {
                var resources=frame.Get<UniversalResourceData>();data.owner=owner;data.colour=resources.activeColorTexture;data.depth=resources.activeDepthTexture;data.viewport=frame.Get<UniversalCameraData>().camera.pixelRect;
                builder.UseTexture(graph.ImportTexture(owner.AtlasHandle),AccessFlags.ReadWrite);builder.UseTexture(graph.ImportTexture(owner.AtlasDepthHandle),AccessFlags.ReadWrite);builder.UseTexture(data.colour,AccessFlags.ReadWrite);builder.UseTexture(data.depth,AccessFlags.ReadWrite);
                var lightData=frame.Get<UniversalLightData>();
                data.shadow=resources.mainShadowsTexture.IsValid()&&lightData.mainLightIndex>=0&&lightData.visibleLights[lightData.mainLightIndex].light==owner.AtlasSun;
                data.cascades=frame.Get<UniversalShadowData>().mainLightShadowCascadesCount;
                if(data.shadow)builder.UseTexture(resources.mainShadowsTexture,AccessFlags.Read);
                builder.AllowPassCulling(false);builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((Data d,UnsafeGraphContext context)=>{d.owner.DrawAtlas(CommandBufferHelpers.GetNativeCommandBuffer(context.cmd),d.shadow,d.cascades);context.cmd.SetRenderTarget(d.colour,d.depth);CommandBufferHelpers.GetNativeCommandBuffer(context.cmd).SetViewport(d.viewport);});
            }
        }
        public override void Execute(ScriptableRenderContext context,ref RenderingData data)
        {
            var owner=GasStationPlanarGlass.AtlasOwner;if(!owner)return;
            var cmd=CommandBufferPool.Get("Glass / shared atlas");owner.DrawAtlas(cmd);cmd.SetRenderTarget(data.cameraData.renderer.cameraColorTargetHandle,data.cameraData.renderer.cameraDepthTargetHandle);cmd.SetViewport(data.cameraData.camera.pixelRect);context.ExecuteCommandBuffer(cmd);CommandBufferPool.Release(cmd);
        }
    }
}
