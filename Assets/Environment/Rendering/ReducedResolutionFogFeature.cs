using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Only the smooth fog transport is downsampled. Scene colour and geometry stay
// at the camera's original resolution and are combined using depth-aware taps.
public sealed class ReducedResolutionFogFeature : ScriptableRendererFeature
{
    public Material fogMaterial;
    public Shader compositeShader;
    [Range(.5f, 1f)] public float resolutionScale = .5f;
    FogPass pass;

    public override void Create()
    {
        pass?.Dispose();
        pass = new FogPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
    {
        if (!fogMaterial || !compositeShader || !compositeShader.isSupported ||
            data.cameraData.cameraType == CameraType.Preview ||
            data.cameraData.cameraType == CameraType.Reflection) return;
        pass.ConfigureInput(ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => pass?.Dispose();

    sealed class FogPass : ScriptableRenderPass
    {
        sealed class Data
        {
            public TextureHandle source, fog;
            public Material material;
            public Vector4 texelSize;
        }

        readonly ReducedResolutionFogFeature owner;
        readonly MaterialPropertyBlock fogProperties = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock compositeProperties = new MaterialPropertyBlock();
        Material fogCopy, composite;
        Material sourceMaterial;
        Shader sourceShader;
        static readonly Vector4 ScaleBias = new Vector4(1, 1, 0, 0);

        public FogPass(ReducedResolutionFogFeature owner)
        {
            this.owner = owner;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(fogCopy);
            CoreUtils.Destroy(composite);
        }

        void PrepareMaterials()
        {
            if (!fogCopy || sourceMaterial != owner.fogMaterial || sourceShader != owner.fogMaterial.shader)
            {
                CoreUtils.Destroy(fogCopy);
                sourceMaterial = owner.fogMaterial;
                sourceShader = sourceMaterial.shader;
                fogCopy = new Material(sourceMaterial) { hideFlags = HideFlags.HideAndDontSave };
            }
            // Weather and headlights can change the source material during play.
            fogCopy.CopyPropertiesFromMaterial(sourceMaterial);
            fogCopy.SetFloat("_FogOnly", 1);
            if (!composite || composite.shader != owner.compositeShader)
            {
                CoreUtils.Destroy(composite);
                composite = CoreUtils.CreateEngineMaterial(owner.compositeShader);
            }
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
        {
            PrepareMaterials();
            var resources = frame.Get<UniversalResourceData>();
            var camera = frame.Get<UniversalCameraData>();
            var source = resources.activeColorTexture;
            var fogDesc = graph.GetTextureDesc(source);
            fogDesc.name = "Fog transport at reduced resolution";
            fogDesc.width = Mathf.Max(1, Mathf.CeilToInt(camera.cameraTargetDescriptor.width * owner.resolutionScale));
            fogDesc.height = Mathf.Max(1, Mathf.CeilToInt(camera.cameraTargetDescriptor.height * owner.resolutionScale));
            fogDesc.depthBufferBits = DepthBits.None;
            fogDesc.msaaSamples = MSAASamples.None;
            fogDesc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
            fogDesc.filterMode = FilterMode.Point;
            fogDesc.clearBuffer = false;
            var fog = graph.CreateTexture(fogDesc);

            using (var builder = graph.AddRasterRenderPass<Data>("Fog / raymarch transport", out var data))
            {
                data.source = source;
                data.material = fogCopy;
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture, AccessFlags.Read);
                if (resources.additionalShadowsTexture.IsValid()) builder.UseTexture(resources.additionalShadowsTexture, AccessFlags.Read);
                builder.SetRenderAttachment(fog, 0, AccessFlags.Write);
                builder.SetRenderFunc((Data d, RasterGraphContext context) =>
                {
                    fogProperties.Clear();
                    fogProperties.SetTexture("_BlitTexture", (RTHandle)d.source);
                    fogProperties.SetVector("_BlitScaleBias", ScaleBias);
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1, fogProperties);
                });
            }

            var outputDesc = graph.GetTextureDesc(source);
            outputDesc.name = "Full resolution scene with fog";
            outputDesc.clearBuffer = false;
            var output = graph.CreateTexture(outputDesc);
            using (var builder = graph.AddRasterRenderPass<Data>("Fog / depth-aware composite", out var data))
            {
                data.source = source;
                data.fog = fog;
                data.material = composite;
                data.texelSize = new Vector4(1f / fogDesc.width, 1f / fogDesc.height, fogDesc.width, fogDesc.height);
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(fog, AccessFlags.Read);
                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(output, 0, AccessFlags.Write);
                builder.SetRenderFunc((Data d, RasterGraphContext context) =>
                {
                    compositeProperties.Clear();
                    compositeProperties.SetTexture("_BlitTexture", (RTHandle)d.source);
                    compositeProperties.SetTexture("_FogTransport", (RTHandle)d.fog);
                    compositeProperties.SetVector("_FogTransportTexelSize", d.texelSize);
                    compositeProperties.SetVector("_BlitScaleBias", ScaleBias);
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1, compositeProperties);
                });
            }
            resources.cameraColor = output;
        }
    }
}
