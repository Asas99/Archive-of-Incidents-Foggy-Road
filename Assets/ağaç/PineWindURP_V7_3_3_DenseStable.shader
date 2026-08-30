Shader "PineTree/URP Dense Stable Wind V8_1 Clean"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (RGBA)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)

        [Header(Material)]
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.45
        [Toggle] _AlphaClip("Alpha Clip (Leaves=1 Bark=0)", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull (Leaves Off=0 Bark Back=2)", Float) = 0
        _Smoothness("Base Smoothness", Range(0,1)) = 0.10
        [Toggle] _FoliageMode("Foliage Mode (Leaves=1 Bark=0)", Float) = 1

        [Header(Bark Normal Mapping)]
        [Toggle] _UseNormalMap("Use Normal Map", Float) = 0
        [NoScaleOffset] [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0,2)) = 1.0

        [Header(Bark Micro Detail)]
        [Toggle] _UseDetailNormal("Use Detail Normal", Float) = 0
        [NoScaleOffset] [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalStrength("Detail Normal Strength", Range(0,2)) = 0.35
        _DetailNormalTiling("Detail Normal Tiling", Range(0.25,32)) = 6.0

        [Header(Bark Roughness)]
        [Toggle] _UseRoughnessMap("Use Roughness Map", Float) = 0
        [NoScaleOffset] _RoughnessMap("Roughness Map (Gray)", 2D) = "white" {}
        _RoughnessStrength("Roughness Map Influence", Range(0,1)) = 1.0

        [Header(Bark Ambient Occlusion)]
        [Toggle] _UseAOMap("Use AO Map", Float) = 0
        [NoScaleOffset] _AOMap("Ambient Occlusion Map (Gray)", 2D) = "white" {}
        _AOStrength("AO Strength", Range(0,1)) = 1.0

        [Header(Bark Height Parallax)]
        [Toggle] _UseParallax("Use Parallax Occlusion", Float) = 0
        [NoScaleOffset] _HeightMap("Height Map (White=High)", 2D) = "black" {}
        _ParallaxStrength("Parallax Depth", Range(0,0.08)) = 0.018
        _ParallaxMinSteps("Parallax Min Steps", Range(4,16)) = 6
        _ParallaxMaxSteps("Parallax Max Steps", Range(8,32)) = 18
        [Toggle] _HeightInvert("Invert Height Map", Float) = 0

        [Header(Main Wind)]
        _WindDirection("Wind Direction XYZ", Vector) = (1,0,0.25,0)
        _WindStrength("Main Sway (m)", Range(0,1.5)) = 0.16
        _WindSpeed("Main Wind Speed", Range(0,5)) = 0.85

        [Header(Branch Motion)]
        _BranchFlex("Branch Flex", Range(0,0.35)) = 0.040
        _BranchSpeed("Branch Speed Multiplier", Range(0.2,4)) = 1.45

        [Header(Gust)]
        _GustStrength("Gust Strength", Range(0,1)) = 0.18
        _GustSpeed("Gust Speed", Range(0,3)) = 0.28
        _GustScale("World Gust Scale", Range(0.001,1)) = 0.050

        [Header(Dense Foliage Micro Motion)]
        _LeafFlutter("Leaf Flutter (m)", Range(0,0.05)) = 0.006
        _LeafFlutterSpeed("Leaf Flutter Speed", Range(0,30)) = 9.0
        _LeafFlutterVertical("Vertical Flutter", Range(0,1)) = 0.12
        _LeafWindResponse("Leaf Wind Response", Range(0,2)) = 1.10
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "RenderPipeline"="UniversalPipeline"
        }

        LOD 400
        Cull [_Cull]
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);
        TEXTURE2D(_DetailNormalMap);
        SAMPLER(sampler_DetailNormalMap);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_AOMap);
        SAMPLER(sampler_AOMap);
        TEXTURE2D(_HeightMap);
        SAMPLER(sampler_HeightMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _AlphaClip;
            half _Smoothness;
            half _FoliageMode;
            half _UseNormalMap;
            half _NormalStrength;
            half _UseDetailNormal;
            half _DetailNormalStrength;
            float _DetailNormalTiling;
            half _UseRoughnessMap;
            half _RoughnessStrength;
            half _UseAOMap;
            half _AOStrength;
            half _UseParallax;
            float _ParallaxStrength;
            float _ParallaxMinSteps;
            float _ParallaxMaxSteps;
            half _HeightInvert;
            float4 _WindDirection;
            float _WindStrength;
            float _WindSpeed;
            float _BranchFlex;
            float _BranchSpeed;
            float _GustStrength;
            float _GustSpeed;
            float _GustScale;
            float _LeafFlutter;
            float _LeafFlutterSpeed;
            float _LeafFlutterVertical;
            float _LeafWindResponse;
        CBUFFER_END

        float3 ApplyPineWind(float3 pWS, float4 color)
        {
            float swayW = saturate(color.r);
            float branchW = saturate(color.g);
            float phase01 = saturate(color.b);
            float leafMask = saturate(color.a) * saturate(_FoliageMode);

            float2 windDir = float2(_WindDirection.x, _WindDirection.z);
            float windLen = max(length(windDir), 1e-4);
            windDir /= windLen;
            float2 windSide = float2(-windDir.y, windDir.x);

            float3 treeOriginWS = TransformObjectToWorld(float3(0,0,0));
            float treePhase = treeOriginWS.x * 0.071 + treeOriginWS.z * 0.053;
            float t = _Time.y * _WindSpeed;

            float bodyPhase = t + treePhase;
            float bodyWave =
                sin(bodyPhase) * 0.68 +
                sin(bodyPhase * 0.51 + 1.37) * 0.22 +
                sin(bodyPhase * 0.23 + 2.41) * 0.10;

            float gustPhase = _Time.y * _GustSpeed + dot(treeOriginWS.xz, float2(_GustScale, _GustScale * 0.73));
            float gust01 = sin(gustPhase) * 0.5 + 0.5;
            gust01 = gust01 * gust01;
            gust01 *= 0.68 + 0.32 * (sin(gustPhase * 0.43 + 1.15) * 0.5 + 0.5);

            float swayCurve = pow(swayW, 1.50);
            float bodyAmount = bodyWave * _WindStrength + gust01 * _GustStrength;
            pWS.xz += windDir * (bodyAmount * swayCurve);

            float branchPhase = t * _BranchSpeed + phase01 * 6.28318530718 + treePhase * 1.9;
            float branchWave =
                sin(branchPhase) * 0.72 +
                sin(branchPhase * 0.61 + 2.0) * 0.20 +
                sin(branchPhase * 1.31 + 0.45) * 0.08;

            float branchAmount = branchWave * _BranchFlex * branchW * (0.25 + 0.75 * swayW);
            pWS.xz += windSide * branchAmount;

            float flutterPhase = _Time.y * _LeafFlutterSpeed + phase01 * 17.0 + treePhase * 3.1;
            float flutterWave =
                sin(flutterPhase) * 0.62 +
                sin(flutterPhase * 1.77 + 0.73) * 0.25 +
                sin(flutterPhase * 0.53 + 2.20) * 0.13;

            float leafAmp = _LeafFlutter * _LeafWindResponse * leafMask * (0.70 + 0.55 * gust01);
            float flutter = flutterWave * leafAmp;
            pWS.xz += windSide * flutter;
            pWS.y += flutter * _LeafFlutterVertical;
            return pWS;
        }

        float SampleHeight01(float2 uv)
        {
            float h = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv).r;
            if (_HeightInvert > 0.5h)
                h = 1.0 - h;
            return saturate(h);
        }

        float2 ApplyParallaxOcclusion(float2 uv, float3 viewDirTS)
        {
            if (_UseParallax < 0.5h || _FoliageMode > 0.5h || _ParallaxStrength <= 0.00001)
                return uv;

            float3 V = normalize(viewDirTS);
            float vz = max(abs(V.z), 0.08);
            float ndotv = saturate(vz);
            int steps = (int)round(lerp(_ParallaxMaxSteps, _ParallaxMinSteps, ndotv));
            steps = clamp(steps, 4, 32);

            float layerDepth = 1.0 / (float)steps;
            float2 deltaUV = (V.xy / vz) * (_ParallaxStrength / (float)steps);
            float2 currentUV = uv;
            float currentLayerDepth = 0.0;
            float currentMapDepth = 1.0 - SampleHeight01(currentUV);

            [loop]
            for (int i = 0; i < 32; i++)
            {
                if (i >= steps || currentLayerDepth >= currentMapDepth)
                    break;

                currentUV -= deltaUV;
                currentLayerDepth += layerDepth;
                currentMapDepth = 1.0 - SampleHeight01(currentUV);
            }

            float2 prevUV = currentUV + deltaUV;
            float afterDepth = currentMapDepth - currentLayerDepth;
            float prevMapDepth = 1.0 - SampleHeight01(prevUV);
            float beforeDepth = prevMapDepth - (currentLayerDepth - layerDepth);
            float denom = afterDepth - beforeDepth;
            float weight = (abs(denom) > 1e-5) ? saturate(afterDepth / denom) : 0.0;
            return lerp(currentUV, prevUV, weight);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half3 tangentWS : TEXCOORD4;
                half3 bitangentWS : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 pWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float tangentSign = IN.tangentOS.w * GetOddNegativeScale();
                float3 bWS = cross(nWS, tWS) * tangentSign;

                pWS = ApplyPineWind(pWS, IN.color);

                OUT.positionWS = pWS;
                OUT.positionHCS = TransformWorldToHClip(pWS);
                OUT.normalWS = normalize(nWS);
                OUT.tangentWS = normalize(tWS);
                OUT.bitangentWS = normalize(bWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half3 BuildNormalTS(float2 uv)
            {
                half3 nTS = half3(0,0,1);

                if (_UseNormalMap > 0.5h)
                {
                    half4 packedN = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv);
                    nTS = UnpackNormalScale(packedN, _NormalStrength);
                }

                if (_UseDetailNormal > 0.5h)
                {
                    float2 detailUV = uv * _DetailNormalTiling;
                    half4 packedD = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUV);
                    half3 dTS = UnpackNormalScale(packedD, _DetailNormalStrength);
                    nTS = normalize(half3(nTS.xy + dTS.xy, max(0.05h, nTS.z * dTS.z)));
                }

                return nTS;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float3 viewDirTS;
                viewDirTS.x = dot(viewDirWS, IN.tangentWS);
                viewDirTS.y = dot(viewDirWS, IN.bitangentWS);
                viewDirTS.z = dot(viewDirWS, IN.normalWS);

                float2 uv = ApplyParallaxOcclusion(IN.uv, viewDirTS);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                if (_AlphaClip > 0.5h)
                    clip(tex.a - _Cutoff);

                half3 normalTS = BuildNormalTS(uv);
                half3 normalWS = normalize(
                    IN.tangentWS * normalTS.x +
                    IN.bitangentWS * normalTS.y +
                    IN.normalWS * normalTS.z
                );

                half smoothness = _Smoothness;
                if (_UseRoughnessMap > 0.5h)
                {
                    half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                    half mappedSmoothness = 1.0h - roughness;
                    smoothness = lerp(_Smoothness, mappedSmoothness, _RoughnessStrength);
                }

                half occlusion = 1.0h;
                if (_UseAOMap > 0.5h)
                {
                    half ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, uv).r;
                    occlusion = lerp(1.0h, ao, _AOStrength);
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.vertexLighting = half3(0,0,0);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                inputData.shadowMask = half4(1,1,1,1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = tex.rgb;
                surfaceData.alpha = tex.a;
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.02h,0.02h,0.02h);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = occlusion;

                half4 col = UniversalFragmentPBR(inputData, surfaceData);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 pWS = TransformObjectToWorld(IN.positionOS.xyz);
                pWS = ApplyPineWind(pWS, IN.color);
                OUT.positionHCS = TransformWorldToHClip(pWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                if (_AlphaClip > 0.5h)
                    clip(tex.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 pWS = TransformObjectToWorld(IN.positionOS.xyz);
                pWS = ApplyPineWind(pWS, IN.color);
                OUT.positionHCS = TransformWorldToHClip(pWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                if (_AlphaClip > 0.5h)
                    clip(tex.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
