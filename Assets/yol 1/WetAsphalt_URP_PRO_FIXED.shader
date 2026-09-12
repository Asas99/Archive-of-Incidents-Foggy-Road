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
        _TexturePlacementNoise ("Texture Placement Noise", Range(0,1)) = 0.85
        _TexturePlacementNoiseScale ("Texture Placement Noise Scale", Range(0.005,0.5)) = 0.06
        _TextureRepeatRemoval ("Texture Repeat Removal", Range(0,1)) = 0.9

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
        _MicroAsphaltContrast ("Asphalt Grain Contrast", Range(0,0.45)) = 0.18
        _AsphaltRelief ("Asphalt Aggregate Relief", Range(0,2.5)) = 1.0
        _MacroStrength ("Vertex Macro Variation", Range(0,1)) = 0.35
        _WearDarkening ("Wear Darkening", Range(0,0.9)) = 0.18

        [Header(Wet_Asphalt_Rain)]
        _Wetness ("Global Wetness", Range(0,1)) = 0.72
        _DampFilm ("Whole Road Damp Film", Range(0,1)) = 0.22
        _WetRoughness ("Wet Roughness", Range(0.02,0.5)) = 0.12
        _PuddleRoughness ("Puddle Roughness", Range(0.01,0.35)) = 0.055
        _PuddleLevel ("Puddle Water Level", Range(0,1)) = 0.5
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
        _PuddleReflection ("Puddle Reflection Strength", Range(0,1)) = 0.35
        _DarkPuddleReadability ("Dark Puddle Readability", Range(0,0.12)) = 0.035
        _ReflectionGain ("Environment Reflection Gain", Range(0,4)) = 1.0
        _ReflectionLimit ("Wet Reflection Limit", Range(0.05,1)) = 0.42
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
            half _TexturePlacementNoise;
            half _TexturePlacementNoiseScale;
            half _TextureRepeatRemoval;
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
            half _MicroAsphaltContrast;
            half _AsphaltRelief;
            half _MacroStrength;
            half _WearDarkening;
            half _Wetness;
            half _DampFilm;
            half _WetRoughness;
            half _PuddleRoughness;
            half _PuddleLevel;
            half _PuddleCoverage;
            half _PuddleEdge;
            half _PuddleRandomness;
            half _PuddleNoiseScale;
            half _PuddleFromHeight;
            half _WetDarkening;
            half _WaterFilm;
            half _WetNormalFlatten;
            half _PuddleNormalFlatten;
            half _PuddleReflection;
            half _DarkPuddleReadability;
            half _ReflectionGain;
            half _ReflectionLimit;
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
                // uv0 was already transformed by _BaseMap_ST in Vert(). Applying
                // TRANSFORM_TEX again here squares the tiling and filters the
                // aggregate into a smooth/grid-like surface on this long road.
                float2 uv = IN.uv0;

                // Break the visible square repetition without sacrificing the
                // authored aggregate. A second, irrationally-scaled and rotated
                // placement is mixed with broad world-space noise. Keeping the
                // noise selects between the two placements in irregular regions;
                // only the transition zones blend. This keeps aggregate crisp
                // instead of averaging both textures into a soft surface.
                const float tileCos = 0.8143409;
                const float tileSin = 0.5803860;
                half2 placementWarp = half2(
                    FBM(IN.positionWS.xz * (_TexturePlacementNoiseScale * 1.83h) + float2(31.7h, 4.2h)),
                    FBM(IN.positionWS.xz * (_TexturePlacementNoiseScale * 1.47h) + float2(6.1h, 57.9h))) - 0.5h;
                float2 uvPrimary = uv + placementWarp * (_TexturePlacementNoise * 0.72h);
                float2 uvScaled = uvPrimary * 1.173;
                float2 uvAlt = float2(
                    tileCos * uvScaled.x - tileSin * uvScaled.y,
                    tileSin * uvScaled.x + tileCos * uvScaled.y) + float2(17.31, 43.73);
                half placementNoise = smoothstep(0.22h, 0.78h,
                    FBM(IN.positionWS.xz * _TexturePlacementNoiseScale + float2(9.7h, 21.3h)));
                half placementBlend = _TexturePlacementNoise * smoothstep(0.38h, 0.62h, placementNoise);

                half wear = IN.color.r;
                // The authored vertex mask is nearly full on this road mesh.
                // Keep a restrained film everywhere, but let procedural pools
                // decide where the stronger rain reflections gather.
                half retention = saturate(IN.color.g * 0.34h);
                half macro = IN.color.b;

                half3 baseTexA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvPrimary).rgb;
                half3 baseTexB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvAlt).rgb;
                // Remove the photograph's repeating large light/dark blotches but
                // retain its high-frequency stones. The texture's coarsest mip is
                // a stable average; mip 5 represents only the unwanted broad tone.
                half3 baseMean = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, float2(0.37, 0.61), 10).rgb;
                half3 baseLowA = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, uvPrimary, 5).rgb;
                half3 baseLowB = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, uvAlt, 5).rgb;
                half3 highPassA = baseMean * clamp(baseTexA / max(baseLowA, 0.008h), 0.48h, 1.9h);
                half3 highPassB = baseMean * clamp(baseTexB / max(baseLowB, 0.008h), 0.48h, 1.9h);
                baseTexA = lerp(baseTexA, highPassA, _TextureRepeatRemoval);
                baseTexB = lerp(baseTexB, highPassB, _TextureRepeatRemoval);
                half3 albedo = lerp(baseTexA, baseTexB, placementBlend) * _BaseColor.rgb;
                half finePlacementNoise = FBM(IN.positionWS.xz * (_TexturePlacementNoiseScale * 3.4h) + float2(71.2h, 13.6h));
                half placementTone = (placementNoise - 0.5h) * 0.30h + (finePlacementNoise - 0.5h) * 0.14h;
                albedo *= 1.0h + placementTone * _TexturePlacementNoise;
                albedo *= lerp(1.0h, max(macro, 0.35h), _MacroStrength);
                albedo *= lerp(1.0h, 1.0h - _WearDarkening, wear);
                // Keep the aggregate readable in diffuse overcast light. This is
                // deliberately broader than the normal-map grain so it survives
                // at game-camera distance instead of becoming a smooth dark slab.
                half visibleGrain = FBM(IN.positionWS.xz * 4.5h);
                half aggregate = smoothstep(0.72h, 0.90h, visibleGrain);
                half grainOffset = (visibleGrain - 0.5h) * 2.0h;
                albedo *= 1.0h + grainOffset * _MicroAsphaltContrast * 0.42h;
                albedo += aggregate * _MicroAsphaltContrast * 0.12h;

                half heightA = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uvPrimary).r;
                half heightB = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uvAlt).r;
                half heightValue = lerp(heightA, heightB, placementBlend);
                half lowArea = saturate((1.0h - heightValue) * 1.35h);

                // The normal map alone becomes visually flat under diffuse fog.
                // Preserve physical lighting while adding restrained aggregate
                // relief from the authored height map so stones and tar cavities
                // remain readable at the gameplay camera distance.
                half aggregateRelief = (heightValue - 0.42h) * _AsphaltRelief;
                albedo *= saturate(1.0h + aggregateRelief);

                // Layered world-space noise gives rainwater broken, nested edges
                // instead of a few large, soft puddle islands.
                float2 puddleUV = IN.positionWS.xz * _PuddleNoiseScale;
                half broadPool = FBM(puddleUV);
                half mediumPool = FBM(puddleUV * 3.73h + float2(11.2h, 5.7h));
                half finePool = FBM(puddleUV * 10.9h + float2(3.4h, 17.8h));
                half puddlePattern = broadPool * 0.42h + mediumPool * 0.40h + finePool * 0.18h;
                half procPud = smoothstep(0.37h, 0.60h, puddlePattern);
                half puddleSeed = max(retention,
                                      lerp(retention, procPud,
                                           saturate(max(_PuddleRandomness, 0.82h))));
                puddleSeed = max(puddleSeed, lowArea * _PuddleFromHeight);

                // Coverage threshold moves as global wetness rises. Puddle Level
                // is a user-facing waterline: 0 keeps water in only the deepest
                // pockets, while 1 lets it spread into shallower road areas.
                half threshold = lerp(0.86h, 0.34h, _PuddleCoverage * _Wetness);
                threshold = saturate(threshold - (_PuddleLevel - 0.5h) * 0.48h);
                half wetArea = smoothstep(threshold, threshold + _PuddleEdge, puddleSeed) * _Wetness;
                half damp = _Wetness * _DampFilm;
                half wet = saturate(max(wetArea, damp));

                albedo *= lerp(1.0h, 1.0h - _WetDarkening, wet);
                albedo *= lerp(half3(1,1,1), _WetTint.rgb, wet * 0.16h);

                half roughA = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uvPrimary).r;
                half roughB = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uvAlt).r;
                half roughTex = lerp(roughA, roughB, placementBlend);
                half dryRough = lerp(_Roughness, roughTex, _RoughnessMapStrength);
                dryRough = saturate(dryRough + placementTone * _TexturePlacementNoise * 0.10h);
                half roughness = lerp(dryRough, _WetRoughness, wet);
                roughness = lerp(roughness, _PuddleRoughness, wetArea);
                roughness = saturate(roughness + grainOffset * _MicroAsphaltContrast * 0.16h * (1.0h - wetArea));

                float2 duv = IN.uv1 * _DetailNormalTiling;
                float h0 = AsphaltHeight(duv);
                half microFade = saturate(1.0h - wet * _WetNormalFlatten);
                microFade *= lerp(1.0h, 1.0h - _PuddleNormalFlatten, wetArea);

                half3 baseNormalA = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvPrimary), _NormalScale);
                half3 baseNormalB = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvAlt), _NormalScale);
                // Rotate the alternate tangent-space normal back into the road's
                // original tangent frame before blending the two placements.
                baseNormalB.xy = half2(
                    baseNormalB.x * tileCos + baseNormalB.y * tileSin,
                   -baseNormalB.x * tileSin + baseNormalB.y * tileCos);
                half3 baseNormalTS = normalize(lerp(baseNormalA, baseNormalB, placementBlend));
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
                // Controls the physical PBR sheen as well as the water-film
                // reflection below: zero keeps water dark and rough.
                smoothness *= lerp(0.18h, 1.0h, _PuddleReflection);

                half aoA = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, uvPrimary).r;
                half aoB = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, uvAlt).r;
                half ao = lerp(aoA, aoB, placementBlend);
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

                // With the directional light intentionally disabled, the long
                // asphalt strip otherwise collapses to black. Add only a tiny
                // blue night fill so the road remains readable without making
                // the wet film look self-illuminated.
                half3 nightRoadFill = half3(0.012h, 0.015h, 0.019h) *
                                      lerp(0.82h, 1.0h, wet);
                color.rgb += nightRoadFill;

                // Use the actual sky / reflection probe for the water film instead
                // of adding a flat coloured patch. This keeps puddles transparent
                // while producing the long, broken reflections seen on wet roads.
                half NoV = saturate(dot(normalWS, viewDirWS));
                half fresnel = pow(1.0h - NoV, 5.0h);
                half3 reflectionVector = reflect(-viewDirWS, normalWS);
                half3 environmentReflection = GlossyEnvironmentReflection(
                    reflectionVector,
                    IN.positionWS,
                    roughness,
                    occlusion,
                    input.normalizedScreenSpaceUV);

                half filmMask = saturate(damp * 0.58h + wetArea * 0.82h);
                half waterFresnel = lerp(0.06h + _FresnelBoost * 0.08h,
                                         1.0h,
                                         fresnel);
                half reflectionWeight = saturate(
                    filmMask * waterFresnel * (0.42h + _WaterFilm * 0.78h));
                // Keep the fallback very subdued: in a nearly unlit night scene
                // water should be readable mainly where it catches actual sky,
                // not look like it emits its own light.
                float2 streakPosition = float2(IN.positionWS.x * 0.11,
                                               IN.positionWS.z * 0.028);
                half primaryStreak = FBM(streakPosition);
                half crossingStreak = FBM(float2(IN.positionWS.x * 0.045 + 8.3,
                                                 IN.positionWS.z * 0.065 - 3.7));
                half broadStreak = smoothstep(0.26h, 0.72h,
                                              primaryStreak * 0.68h +
                                              crossingStreak * 0.32h);
                half fineBreakup = FBM(IN.positionWS.xz * 0.18 + float2(7.1, 19.3));
                half streakMask = lerp(0.14h, 1.52h, broadStreak) *
                                  lerp(0.58h, 1.16h, fineBreakup);
                half skyFacing = lerp(0.72h, 1.18h,
                                      saturate(reflectionVector.y));
                half3 cloudySkyFloor = half3(0.040h, 0.045h, 0.052h) *
                                       streakMask * skyFacing;
                half3 reflectedScene = (environmentReflection * _ReflectionGain +
                                        cloudySkyFloor) * _ReflectionTint.rgb;
                // A bright foggy sky can contain near-white values even at night.
                // Compress it before mixing so puddles never become isolated
                // white light patches on an otherwise dark road.
                half reflectionLuminance = dot(reflectedScene,
                                                half3(0.2126h, 0.7152h, 0.0722h));
                half maxNightReflection = _ReflectionLimit;
                reflectedScene *= min(1.0h,
                                     maxNightReflection / max(reflectionLuminance, 0.001h));
                // A rain-soaked road carries a continuous water film. Keep the
                // breakup subtle so the whole surface reads as wet rather than
                // a few isolated reflective islands.
                reflectionWeight *= lerp(0.52h, 0.82h, broadStreak);
                reflectionWeight = min(reflectionWeight, _ReflectionLimit);
                reflectionWeight *= _PuddleReflection;
                color.rgb = lerp(color.rgb, reflectedScene, reflectionWeight);

                // Turn the elongated rain pattern into readable wet-road bands.
                // The environment reflection can be nearly black at night, so
                // this restrained neutral sheen preserves the puddle structure
                // without making the asphalt look self-lit or blue.
                half waterBand = saturate(wetArea * (0.34h + broadStreak * 0.66h));
                half3 bandSheen = half3(0.140h, 0.153h, 0.160h) *
                                  streakMask * skyFacing;
                color.rgb = lerp(color.rgb,
                                 color.rgb * 0.30h + bandSheen,
                                 waterBand * 0.78h);
                half visiblePuddle = smoothstep(0.42h, 0.62h, puddlePattern);
                half visibleLong = smoothstep(0.28h, 0.72h, broadStreak);
                half waterRead = saturate(visiblePuddle * 0.78h + visibleLong * 0.22h);
                half3 readableSheen = half3(0.118h, 0.130h, 0.138h) *
                                      (0.58h + 0.42h * streakMask);
                color.rgb = lerp(color.rgb,
                                 color.rgb * 0.34h + readableSheen,
                                 waterRead * 0.52h);
                half puddleBreak = smoothstep(0.52h, 0.78h, puddlePattern);
                color.rgb *= lerp(1.0h, 0.72h,
                                  puddleBreak * wetArea * 0.38h);
                // Leave only a trace of fallback sheen for the darkest patches.
                color.rgb += cloudySkyFloor * filmMask *
                             lerp(0.025h, 0.10h, broadStreak);
                // In a moonless or heavily fogged scene, water has almost no sky
                // to reflect. A neutral, restrained lift keeps puddle shapes
                // readable without turning them into blue mirror patches.
                color.rgb += half3(0.42h, 0.44h, 0.45h) * wetArea *
                             _DarkPuddleReadability * lerp(0.65h, 1.0h, broadStreak);
                // Keep the asphalt itself in the dark neutral range while the
                // wet bands above remain readable as localized highlights.
                color.rgb *= 0.76h;
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
