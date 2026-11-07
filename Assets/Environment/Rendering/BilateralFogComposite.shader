Shader "Hidden/FoggyRoad/BilateralFogComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D_X(_FogTransport);
            float4 _FogTransportTexelSize;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float depth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float2 grid = uv * _FogTransportTexelSize.zw - .5;
                float2 cell = floor(grid);
                float2 fraction = frac(grid);
                float4 sum = 0;
                float weightSum = 0;
                float bestDifference = 1e30;
                float4 closest = float4(0, 0, 0, 1);
                [unroll] for (int y = 0; y < 2; y++)
                [unroll] for (int x = 0; x < 2; x++)
                {
                    float2 tap = saturate((cell + float2(x, y) + .5) * _FogTransportTexelSize.xy);
                    float tapDepth = LinearEyeDepth(SampleSceneDepth(tap), _ZBufferParams);
                    float difference = abs(tapDepth - depth);
                    float4 fog = SAMPLE_TEXTURE2D_X_LOD(_FogTransport, sampler_PointClamp, tap, 0);
                    if (difference < bestDifference) { bestDifference = difference; closest = fog; }
                    float2 spatial = lerp(1 - fraction, fraction, float2(x, y));
                    float weight = spatial.x * spatial.y * exp2(-difference / max(.01, depth * .01));
                    sum += fog * weight;
                    weightSum += weight;
                }
                float4 transport = weightSum > .00001 ? sum / weightSum : closest;
                return half4(scene.rgb * transport.a + transport.rgb, scene.a);
            }
            ENDHLSL
        }
    }
}
