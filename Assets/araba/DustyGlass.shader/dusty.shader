Shader "Custom/RealisticDustyCarGlass"
{
    Properties
    {
        [Header(GLASS)]
        _GlassColor ("Glass Tint", Color) = (0.10, 0.14, 0.16, 1)
        _Alpha ("Glass Transparency", Range(0.02, 0.95)) = 0.22
        _Smoothness ("Clean Glass Smoothness", Range(0,1)) = 0.92
        _Metallic ("Metallic", Range(0,1)) = 0.0

        [Header(DUST)]
        _DustMask ("Dust Mask", 2D) = "black" {}
        _DustColor ("Dust Color", Color) = (0.48, 0.43, 0.35, 1)
        _DustStrength ("Dust Amount", Range(0,3)) = 1.0
        _DustSmoothness ("Dust Smoothness", Range(0,1)) = 0.22
        _DustOpacity ("Dust Visibility", Range(0,1)) = 0.28
        _DustContrast ("Dust Contrast", Range(0.25,4)) = 1.4
        _DustTiling ("Dust Tiling", Range(0.1,8)) = 1.0

        [Header(REFLECTION)]
        _FresnelStrength ("Edge Reflection", Range(0,2)) = 0.38
        _FresnelPower ("Fresnel Power", Range(0.5,10)) = 4.5

        [Header(NORMAL)]
        [Normal]_NormalMap ("Micro Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float fogFactor   : TEXCOORD4;
            };

            TEXTURE2D(_DustMask);
            SAMPLER(sampler_DustMask);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _GlassColor;

                float _Alpha;
                float _Smoothness;
                float _Metallic;

                float4 _DustColor;
                float _DustStrength;
                float _DustSmoothness;
                float _DustOpacity;
                float _DustContrast;
                float _DustTiling;

                float _FresnelStrength;
                float _FresnelPower;

                float _NormalStrength;

            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                OUT.normalWS = normalInputs.normalWS;

                OUT.tangentWS = float4(
                    normalInputs.tangentWS,
                    IN.tangentOS.w
                );

                OUT.uv = IN.uv;

                OUT.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //----------------------------
                // DUST MASK
                //----------------------------

                float2 dustUV = IN.uv * _DustTiling;

                half dust =
                    SAMPLE_TEXTURE2D(
                        _DustMask,
                        sampler_DustMask,
                        dustUV
                    ).r;

                // Makes the generated mask much more visible.
                dust = pow(saturate(dust), _DustContrast);

                dust =
                    saturate(dust * _DustStrength);

                //----------------------------
                // NORMAL
                //----------------------------

                half3 normalTS =
                    UnpackNormal(
                        SAMPLE_TEXTURE2D(
                            _NormalMap,
                            sampler_NormalMap,
                            IN.uv * 3.0
                        )
                    );

                normalTS.xy *= _NormalStrength;

                float3 bitangent =
                    cross(IN.normalWS, IN.tangentWS.xyz)
                    * IN.tangentWS.w;

                float3x3 tangentToWorld =
                    float3x3(
                        IN.tangentWS.xyz,
                        bitangent,
                        IN.normalWS
                    );

                half3 normalWS =
                    normalize(
                        TransformTangentToWorld(
                            normalTS,
                            tangentToWorld
                        )
                    );

                //----------------------------
                // VIEW + FRESNEL
                //----------------------------

                half3 viewDir =
                    SafeNormalize(
                        GetWorldSpaceViewDir(IN.positionWS)
                    );

                half NdotV =
                    saturate(
                        dot(normalWS, viewDir)
                    );

                half fresnel =
                    pow(
                        1.0 - NdotV,
                        _FresnelPower
                    );

                fresnel *= _FresnelStrength;

                //----------------------------
                // GLASS / DUST MIX
                //----------------------------

                half3 glassColor =
                    _GlassColor.rgb;

                half3 dustyColor =
                    lerp(
                        glassColor,
                        _DustColor.rgb,
                        dust * 0.65
                    );

                half smoothness =
                    lerp(
                        _Smoothness,
                        _DustSmoothness,
                        dust
                    );

                //----------------------------
                // SURFACE DATA
                //----------------------------

                SurfaceData surfaceData =
                    (SurfaceData)0;

                surfaceData.albedo = dustyColor;
                surfaceData.metallic = _Metallic;

                // Glass benefits from fairly strong dielectric specular.
                surfaceData.specular =
                    half3(0.5, 0.5, 0.5);

                surfaceData.smoothness =
                    smoothness;

                surfaceData.normalTS =
                    half3(0,0,1);

                surfaceData.occlusion = 1;

                // Slight edge highlight/reflection
                surfaceData.emission =
                    fresnel * half3(0.16,0.19,0.22);

                surfaceData.alpha =
                    saturate(
                        _Alpha +
                        dust * _DustOpacity +
                        fresnel * 0.06
                    );

                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 1;

                //----------------------------
                // INPUT DATA
                //----------------------------

                InputData inputData =
                    (InputData)0;

                inputData.positionWS =
                    IN.positionWS;

                inputData.normalWS =
                    normalWS;

                inputData.viewDirectionWS =
                    viewDir;

                inputData.shadowCoord =
                    TransformWorldToShadowCoord(
                        IN.positionWS
                    );

                inputData.fogCoord =
                    IN.fogFactor;

                inputData.vertexLighting =
                    half3(0,0,0);

                inputData.bakedGI =
                    SampleSH(normalWS);

                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        IN.positionCS
                    );

                inputData.shadowMask =
                    half4(1,1,1,1);

                //----------------------------
                // URP PBR
                //----------------------------

                half4 color =
                    UniversalFragmentPBR(
                        inputData,
                        surfaceData
                    );

                color.rgb =
                    MixFog(
                        color.rgb,
                        inputData.fogCoord
                    );

                color.a =
                    surfaceData.alpha;

                return color;
            }

            ENDHLSL
        }
    }
}