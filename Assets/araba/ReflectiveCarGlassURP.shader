Shader "FoggyRoad/ReflectiveCarGlassURP"
{
    Properties
    {
        [Header(GLASS)]
        _GlassTint ("Glass Tint", Color) = (0.055, 0.085, 0.10, 1)
        _Opacity ("Front Opacity", Range(0.02, 0.95)) = 0.32
        _EdgeOpacity ("Edge Opacity", Range(0.02, 1.0)) = 0.72
        _Smoothness ("Glass Smoothness", Range(0, 1)) = 0.94

        [Header(REFLECTION)]
        _ReflectionTint ("Reflection Tint", Color) = (0.82, 0.90, 1.0, 1)
        _ReflectionStrength ("Reflection Strength", Range(0, 1.5)) = 0.82
        _BaseReflection ("Front Reflection", Range(0, 1)) = 0.12
        _FresnelStrength ("Edge Reflection", Range(0, 1.5)) = 0.88
        _FresnelPower ("Fresnel Power", Range(0.5, 10)) = 4.0
        _ReflectionBlur ("Reflection Blur", Range(0, 1)) = 0.08

        [Header(DUST)]
        _DustMask ("Dust Mask", 2D) = "black" {}
        _DustColor ("Dust Color", Color) = (0.48, 0.44, 0.36, 1)
        _DustStrength ("Dust Amount", Range(0, 2)) = 0.35
        _DustOpacity ("Dust Opacity", Range(0, 1)) = 0.20
        _DustSmoothness ("Dust Smoothness", Range(0, 1)) = 0.28
        _DustTiling ("Dust Tiling", Range(0.1, 8)) = 1.0

        [Header(SURFACE DETAIL)]
        [Normal] _NormalMap ("Micro Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_DustMask);
            SAMPLER(sampler_DustMask);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _GlassTint;
                float4 _ReflectionTint;
                float4 _DustColor;
                float4 _DustMask_ST;
                float4 _NormalMap_ST;
                half _Opacity;
                half _EdgeOpacity;
                half _Smoothness;
                half _ReflectionStrength;
                half _BaseReflection;
                half _FresnelStrength;
                half _FresnelPower;
                half _ReflectionBlur;
                half _DustStrength;
                half _DustOpacity;
                half _DustSmoothness;
                half _DustTiling;
                half _NormalStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 dustUV = TRANSFORM_TEX(input.uv, _DustMask) * _DustTiling;
                half dust = saturate(SAMPLE_TEXTURE2D(_DustMask, sampler_DustMask, dustUV).r * _DustStrength);

                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(input.uv, _NormalMap)),
                    _NormalStrength);

                half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                // The same material is used on both sides of the vehicle glass.
                if (dot(normalWS, viewDirectionWS) < 0.0h)
                    normalWS = -normalWS;

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirectionWS)), _FresnelPower);
                half smoothness = lerp(_Smoothness, _DustSmoothness, dust);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = lerp(_GlassTint.rgb, _DustColor.rgb, dust * 0.65h);
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.04h, 0.04h, 0.04h);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = 0.0h;
                surfaceData.alpha = saturate(lerp(_Opacity, _EdgeOpacity, fresnel) + dust * _DustOpacity);
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 1.0h;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS, normalWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // URP resolves the nearest reflection probe here and falls back to the skybox.
                half3 reflectionDirection = reflect(-viewDirectionWS, normalWS);
                half perceptualRoughness = saturate(1.0h - smoothness + _ReflectionBlur);
                half3 environmentReflection = GlossyEnvironmentReflection(
                    reflectionDirection,
                    input.positionWS,
                    perceptualRoughness,
                    1.0h,
                    inputData.normalizedScreenSpaceUV);

                half reflectionWeight = saturate((_BaseReflection + fresnel * _FresnelStrength) * _ReflectionStrength);
                environmentReflection *= _ReflectionTint.rgb;
                color.rgb = lerp(color.rgb, environmentReflection, reflectionWeight);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = surfaceData.alpha;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
