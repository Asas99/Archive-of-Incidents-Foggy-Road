using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class SpeedTreeWindRendererFeature : ScriptableRendererFeature
{
    private SpeedTreeWindPass shadowPass;
    private SpeedTreeWindPass depthPass;
    private SpeedTreeWindPass opaquePass;

    public override void Create()
    {
        shadowPass = new SpeedTreeWindPass("Foggy Road SpeedTree Wind (Shadows)",
            RenderPassEvent.BeforeRenderingShadows);
        depthPass = new SpeedTreeWindPass("Foggy Road SpeedTree Wind (Depth)",
            RenderPassEvent.BeforeRenderingPrePasses);
        opaquePass = new SpeedTreeWindPass("Foggy Road SpeedTree Wind (Color)",
            RenderPassEvent.BeforeRenderingOpaques);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(shadowPass);
        renderer.EnqueuePass(depthPass);
        renderer.EnqueuePass(opaquePass);
    }

    private sealed class SpeedTreeWindPass : ScriptableRenderPass
    {
        private sealed class PassData
        {
            internal Vector4 windVector;
            internal Vector4 windGlobal;
        }

        private readonly string profilerName;
        private static readonly int WeatherWindVector = Shader.PropertyToID("_WeatherWindVector");
        private static readonly int WeatherDynamics = Shader.PropertyToID("_WeatherDynamics");
        private static readonly int SpeedTreeWindVector = Shader.PropertyToID("_ST_WindVector");
        private static readonly int SpeedTreeWindGlobal = Shader.PropertyToID("_ST_WindGlobal");
        private static readonly int SpeedTreeBranchAdherences = Shader.PropertyToID("_ST_WindBranchAdherences");

        public SpeedTreeWindPass(string profilerName, RenderPassEvent passEvent)
        {
            this.profilerName = profilerName;
            renderPassEvent = passEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!TryBuildWindParameters(out Vector4 windVector, out Vector4 windGlobal))
                return;

            CommandBuffer command = CommandBufferPool.Get(profilerName);
            SetWindGlobals(command, windVector, windGlobal);
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!TryBuildWindParameters(out Vector4 windVector, out Vector4 windGlobal))
                return;

            using (var builder = renderGraph.AddUnsafePass<PassData>(profilerName, out var passData))
            {
                passData.windVector = windVector;
                passData.windGlobal = windGlobal;
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
                {
                    CommandBuffer command = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    SetWindGlobals(command, data.windVector, data.windGlobal);
                });
            }
        }

        private static bool TryBuildWindParameters(out Vector4 windVector, out Vector4 windGlobal)
        {
            Vector4 weatherWind = Shader.GetGlobalVector(WeatherWindVector);
            Vector2 direction = new Vector2(weatherWind.x, weatherWind.z);
            float strength;

            if (direction.sqrMagnitude < 0.0001f)
            {
                // No weather system in the scene (or it has not ticked yet). Fall back to a
                // steady breeze instead of bailing out, so the canopy never renders frozen.
                direction = new Vector2(0.93f, 0.36f);
                strength = 0.5f;
            }
            else
            {
                strength = Mathf.Clamp01(weatherWind.w * 0.55f);
            }

            direction.Normalize();

            // The wind clock must never come from _WeatherDynamics: that global only advances
            // while DynamicWeatherSystem is ticking, and a stale value freezes every tree in
            // play mode. Time.realtimeSinceStartup advances in the editor and in play mode.
            float time = Time.realtimeSinceStartup;

            windVector = new Vector4(direction.x, 0f, direction.y, strength);
            windGlobal = new Vector4(
                time * 0.22f,
                Mathf.Lerp(0.34f, 0.52f, strength),
                1f / 18f,
                1.45f);
            return true;
        }

        private static void SetWindGlobals(CommandBuffer command, Vector4 windVector, Vector4 windGlobal)
        {
            command.SetGlobalVector(SpeedTreeWindVector, windVector);
            command.SetGlobalVector(SpeedTreeWindGlobal, windGlobal);
            command.SetGlobalVector(SpeedTreeBranchAdherences, Vector4.zero);
        }
    }
}
