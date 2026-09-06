// Wet Asphalt PRO - URP
// Companion shader for Professional Curve Road Generator v4.
// UV0 = full road PBR texture set. UV1 = optional metric micro-normal.
// Vertex Color: R = wear, G = water retention, B = macro variation.
// Height convention: black = low / grooves, white = high.

Shader "Foggy Road/Wet Asphalt URP PRO"
{
    Properties
    {
        [Header(Main_PBR_Maps_UV0)]
        [MainTexture] _BaseMap ("Albedo / Road Texture", 2D) = "grey" {}
        [MainColor] _BaseColor ("Base Tint", Color) = (1,1,1,1)
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Strength", Range(0,2)) = 1.0
        _AOMap ("Ambient Occlusion", 2D) = "white" {}
        _AOStrength ("AO Strength", Range(0,1)) = 1.0
        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        _RoughnessMapStrength ("Roughness Map Influence", Range(0,1)) = 0.0

        [Header(Height_Parallax_UV0)]
        _HeightMap ("Height Map", 2D) = "gray" {}
        _HeightStrength ("Parallax Height", Range(0,0.12)) = 0.025
        _HeightCenter ("Height Midpoint", Range(0,1)) = 0.5
        _ParallaxMinSteps ("POM Min Steps", Range(4,32)) = 8
        _ParallaxMaxSteps ("POM Max Steps", Range(8,64)) = 24
        _ParallaxFadeStart ("POM Fade Start Distance", Float) = 35
        _ParallaxFadeEnd ("POM Fade End Distance", Float) = 80

        [Header(Micro_Detail_UV1)]
        [Normal] _DetailNormalMap ("Detail Normal", 2D) = "bump" {}
        _DetailNormalScale ("Detail Normal Strength", Range(0,2)) = 0.35
        _DetailNormalTiling ("Detail Normal Tiling", Float) = 1.0
        _ProceduralGrain ("Procedural Grain", Range(0,1)) = 0.35
        _GrainScale ("Grain Scale per metre", Range(1,80)) = 18
        _CrackAmount ("Procedural Micro Cracks", Range(0,1)) = 0.12
        _CrackScale ("Crack Scale", Range(0.5,16)) = 4.0

        [Header(Dry_Asphalt)]
        _Roughness ("Dry Roughness", Range(0,1)) = 0.82
        _RoughnessVariation ("Micro Roughness Variation", Range(0,0.35)) = 0.08
        _MacroStrength ("Vertex Macro Variation", Range(0,1)) = 0.35
        _WearDarkening ("Wear Darkening", Range(0,0.9)) = 0.18

        [Header(Wet_Asphalt_Rain)]
        _Wetness ("Global Wetness", Range(0,1)) = 0.72
        _DampFilm ("Whole Road Damp Film", Range(0,1)) = 0.22
        _WetRoughness ("Wet Roughness", Range(0.02,0.5)) = 0.12
        _PuddleRoughness ("Puddle Roughness", Range(0.01,0.35)) = 0.055
        _PuddleCoverage ("Puddle Coverage", Range(0,1)) = 0.55
        _PuddleEdge ("Puddle Edge Softness", Range(0.01,0.5)) = 0.16
        _PuddleRandomness ("World-space Puddle Randomness", Range(0,1)) = 0.45
        _PuddleNoiseScale ("Puddle Noise Scale", Range(0.01,1)) = 0.10
        _PuddleFromHeight ("Collect Water in Low Height Areas", Range(0,1)) = 0.75
        _WetDarkening ("Wet Darkening", Range(0,0.9)) = 0.38
        _WaterFilm ("Water Reflection Boost", Range(0,1)) = 0.75
        _WetNormalFlatten ("Flatten Micro Detail When Wet", Range(0,1)) = 0.7
        _PuddleNormalFlatten ("Flatten Normal in Puddles", Range(0,1)) = 0.92

        [Header(Optional_Artistic_Controls)]
        _WetTint ("Wet Tint", Color) = (0.82,0.9,1,1)
        _ReflectionTint ("Reflection Tint", Color) = (0.72,0.82,0.95,1)
        _FresnelBoost ("Wet Fresnel Boost", Range(0,1)) = 0.18

        [HideInInspector] _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 400

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _NormalMap_ST;
            float4 _AOMap_ST;
            float4 _RoughnessMap_ST;
            float4 _HeightMap_ST;
            float4 _DetailNormalMap_ST;
            half4 _BaseColor;
            half4 _WetTint;
            half4 _ReflectionTint;
            half _NormalScale;
            half _AOStrength;
            half _RoughnessMapStrength;
            half _HeightStrength;
            half _HeightCenter;
            half _ParallaxMinSteps;
            half _ParallaxMaxSteps;
            float _ParallaxFadeStart;
            float _ParallaxFadeEnd;
            half _DetailNormalScale;
            float _DetailNormalTiling;
            half _ProceduralGrain;
            half _GrainScale;
            half _CrackAmount;
            half _CrackScale;
            half _Roughness;
            half _RoughnessVariation;
            half _MacroStrength;
            half _WearDarkening;
            half _Wetness;
            half _DampFilm;
            half _WetRoughness;
            half _PuddleRoughness;
            half _PuddleCoverage;
            half _PuddleEdge;
            half _PuddleRandomness;
            half _PuddleNoiseScale;
            half _PuddleFromHeight;
            half _WetDarkening;
            half _WaterFilm;
            half _WetNormalFlatten;
            half _PuddleNormalFlatten;
            half _FresnelBoost;
            half _Cutoff;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RoadVert
            #pragma fragment RoadFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_instancing

            // The very large road mesh exhibits camera-relative shadow-atlas artifacts.
            // Keep direct lighting, but do not sample realtime shadows in this material.
            #define _RECEIVE_SHADOWS_OFF 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_AOMap); SAMPLER(sampler_AOMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            TEXTURE2D(_DetailNormalMap); SAMPLER(sampler_DetailNormalMap);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1,0));
                float c = Hash21(i + float2(0,1));
                float d = Hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float FBM(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    v += VNoise(p) * a;
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            float AsphaltHeight(float2 uv)
            {
                float grain = FBM(uv * _GrainScale);
                float cell = VNoise(uv * _CrackScale);
                float crack = 1.0 - smoothstep(0.0, 0.05, abs(cell - 0.5));
                return grain - crack * _CrackAmount;
            }

            // View-angle adaptive Parallax Occlusion Mapping.
            // Returns UV0 offset only; UV1 micro detail intentionally stays metric.
            float2 ParallaxOcclusionUV(float2 uv, float3 viewDirTS, float fade)
            {
                if (_HeightStrength <= 0.00001 || fade <= 0.0001)
                    return uv;

                float ndotv = saturate(abs(viewDirTS.z));
                float stepsF = lerp(_ParallaxMaxSteps, _ParallaxMinSteps, ndotv);
                int steps = (int)clamp(stepsF, 4.0, 64.0);

                float layerDepth = 1.0 / steps;
                float currentLayer = 0.0;
                float2 parallaxDir = viewDirTS.xy / max(abs(viewDirTS.z), 0.08);
                float2 deltaUV = parallaxDir * (_HeightStrength * fade) / steps;

                float2 currentUV = uv;
                float sampledHeight = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, currentUV, 0).r;
                sampledHeight = saturate(sampledHeight - _HeightCenter + 0.5);

                [loop] for (int i = 0; i < 64; i++)
                {
                    if (i >= steps || currentLayer >= sampledHeight)
                        break;
                    currentUV -= deltaUV;
                    sampledHeight = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, currentUV, 0).r;
                    sampledHeight = saturate(sampledHeight - _HeightCenter + 0.5);
                    currentLayer += layerDepth;
                }

                float2 prevUV = currentUV + deltaUV;
                float afterDepth = sampledHeight - currentLayer;
                float prevHeight = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, prevUV, 0).r;
                prevHeight = saturate(prevHeight - _HeightCenter + 0.5);
                float beforeDepth = prevHeight - (currentLayer - layerDepth);
                float weight = afterDepth / max(afterDepth - beforeDepth, 0.0001);
                weight = saturate(weight);
                return lerp(currentUV, prevUV, weight);
            }

            half3 BlendNormalsTS(half3 a, half3 b)
            {
                // Robust RNM-like blend for two tangent-space normals.
                a = normalize(a);
                b = normalize(b);
                return normalize(half3(a.xy + b.xy, a.z * b.z));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uvLM : TEXCOORD2;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half4 tangentWS : TEXCOORD4;
                half4 color : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                half fogFactor : TEXCOORD7;
                DECLARE_LIGHTMAP_OR_SH(uvLM, vertexSH, 8);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings RoadVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = nrm.normalWS;
                OUT.tangentWS = half4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv0 = TRANSFORM_TEX(IN.uv0, _BaseMap);
                OUT.uv1 = IN.uv1;
                OUT.color = IN.color;
                OUT.shadowCoord = GetShadowCoord(pos);
                OUT.fogFactor = ComputeFogFactor(pos.positionCS.z);
                OUTPUT_LIGHTMAP_UV(IN.uvLM, unity_LightmapST, OUT.uvLM);
                OUTPUT_SH(nrm.normalWS, OUT.vertexSH);
                return OUT;
            }

            half4 RoadFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half3 N = normalize(IN.normalWS);
                half3 T = normalize(IN.tangentWS.xyz);
                half3 B = normalize(IN.tangentWS.w * cross(N, T));
                float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                float3 viewDirTS = float3(dot(viewDirWS, T), dot(viewDirWS, B), dot(viewDirWS, N));

                // Keep the road surface stable while the camera moves. POM on this
                // long spline mesh crosses UV seams and creates large dark trails.
                float2 uv = IN.uv0;

                half wear = IN.color.r;
                half retention = IN.color.g;
                half macro = IN.color.b;

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                albedo *= lerp(1.0h, max(macro, 0.35h), _MacroStrength);
                albedo *= lerp(1.0h, 1.0h - _WearDarkening, wear);

                half heightValue = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv).r;
                half lowArea = saturate((1.0h - heightValue) * 1.35h);

                half procPud = smoothstep(0.50h, 0.78h, FBM(IN.positionWS.xz * _PuddleNoiseScale));
                half puddleSeed = max(retention, lerp(retention, procPud, _PuddleRandomness));
                puddleSeed = max(puddleSeed, lowArea * _PuddleFromHeight);

                // Coverage threshold moves as global wetness rises.
                half threshold = lerp(0.86h, 0.34h, _PuddleCoverage * _Wetness);
                half wetArea = smoothstep(threshold, threshold + _PuddleEdge, puddleSeed) * _Wetness;
                half damp = _Wetness * _DampFilm;
                half wet = saturate(max(wetArea, damp));

                albedo *= lerp(1.0h, 1.0h - _WetDarkening, wet);
                albedo *= lerp(half3(1,1,1), _WetTint.rgb, wet * 0.16h);

                half roughTex = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                half dryRough = lerp(_Roughness, roughTex, _RoughnessMapStrength);
                half roughness = lerp(dryRough, _WetRoughness, wet);
                roughness = lerp(roughness, _PuddleRoughness, wetArea);

                float2 duv = IN.uv1 * _DetailNormalTiling;
                float h0 = AsphaltHeight(duv);
                half microFade = saturate(1.0h - wet * _WetNormalFlatten);
                microFade *= lerp(1.0h, 1.0h - _PuddleNormalFlatten, wetArea);

                half3 baseNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), _NormalScale);
                half3 detailMapTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, TRANSFORM_TEX(duv, _DetailNormalMap)), _DetailNormalScale * microFade);

                float eps = 0.35 / max(_GrainScale, 1.0);
                float hx = AsphaltHeight(duv + float2(eps,0));
                float hy = AsphaltHeight(duv + float2(0,eps));
                half3 grainTS = normalize(half3(
                    (h0 - hx) * _ProceduralGrain * microFade * 3.0h,
                    (h0 - hy) * _ProceduralGrain * microFade * 3.0h,
                    1.0h));

                half3 normalTS = BlendNormalsTS(baseNormalTS, detailMapTS);
                normalTS = BlendNormalsTS(normalTS, grainTS);
                normalTS.xy *= lerp(1.0h, 1.0h - _PuddleNormalFlatten, wetArea);
                normalTS = normalize(normalTS);

                roughness = saturate(roughness + (h0 - 0.5h) * _RoughnessVariation * (1.0h - wetArea));
                half smoothness = saturate(1.0h - roughness);
                smoothness = saturate(smoothness + wetArea * _WaterFilm * 0.22h);

                half ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, uv).r;
                half occlusion = lerp(1.0h, ao, _AOStrength);

                half3x3 tbn = half3x3(T, B, N);
                half3 normalWS = NormalizeNormalPerPixel(mul(normalTS, tbn));

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.metallic = 0.0h;
                surface.specular = half3(0,0,0);
                surface.smoothness = smoothness;
                surface.normalTS = normalTS;
                surface.occlusion = occlusion;
                surface.alpha = 1.0h;

                InputData input = (InputData)0;
                input.positionWS = IN.positionWS;
                input.normalWS = normalWS;
                input.viewDirectionWS = viewDirWS;
                input.shadowCoord = IN.shadowCoord;
                input.fogCoord = IN.fogFactor;
                input.vertexLighting = VertexLighting(IN.positionWS, normalWS);
                input.bakedGI = SAMPLE_GI(IN.uvLM, IN.vertexSH, normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                input.shadowMask = half4(1,1,1,1);

                half4 color = UniversalFragmentPBR(input, surface);

                // Subtle grazing-angle wet reflection lift. PBR still does the real reflection probe work.
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 5.0h);
                color.rgb += _ReflectionTint.rgb * fresnel * wetArea * _FresnelBoost * _WaterFilm;
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
