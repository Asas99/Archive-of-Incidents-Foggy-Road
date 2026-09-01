// Wet Asphalt - URP
// Companion shader for the Blender "Professional Road Generator" addon.
// Reads the baked RoadMask vertex colours so the result matches Blender:
//    vertex colour R = wear      G = water retention      B = macro brightness
//    UV0 = RoadUV (metric road texture)   UV1 = DetailUV (metric detail map)
// Assign only _BaseMap; everything else already has Blender's defaults.

Shader "Foggy Road/Wet Asphalt URP"
{
    Properties
    {
        [MainTexture] _BaseMap        ("Road Texture (UV0)", 2D) = "grey" {}
        [MainColor]   _BaseColor      ("Base Tint", Color) = (1,1,1,1)

        [Space(10)]
        [Normal] _DetailNormalMap     ("Detail Normal (UV1)", 2D) = "bump" {}
        _DetailNormalScale            ("Detail Normal Strength", Range(0,2)) = 0.6

        [Space(10)]
        _Roughness                    ("Dry Roughness", Range(0,1)) = 0.82
        _WetRoughness                 ("Wet Roughness", Range(0,1)) = 0.16

        [Space(10)]
        _Wetness                      ("Wetness", Range(0,1)) = 0.6
        _DampFilm                     ("Damp Film", Range(0,1)) = 0.12
        _PuddleEdge                   ("Puddle Edge Softness", Range(0.01,0.5)) = 0.17
        _PuddleRandomness             ("Puddle Randomness (shader)", Range(0,1)) = 0.0
        _PuddleNoiseScale             ("Puddle Noise Scale", Range(0.02,1)) = 0.12
        _WetDarkening                 ("Wet Darkening", Range(0,0.95)) = 0.55
        _WaterFilm                    ("Water Film (spec boost)", Range(0,1)) = 0.35

        [Space(10)]
        _GrainStrength                ("Asphalt Grain", Range(0,2)) = 0.35
        _GrainScale                   ("Grain Scale (per metre)", Range(1,60)) = 14
        _CrackAmount                  ("Cracks", Range(0,1)) = 0.25
        _CrackScale                   ("Crack Scale", Range(0.5,12)) = 3.2
        _RoughnessVariation           ("Roughness Variation", Range(0,0.4)) = 0.12

        [Space(10)]
        _MacroStrength                ("Macro Variation", Range(0,1)) = 1.0
        _WearDarkening                ("Wear Darkening", Range(0,0.9)) = 0.34

        [HideInInspector] _Cutoff     ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _DetailNormalMap_ST;
            half4  _BaseColor;
            half   _DetailNormalScale;
            half   _Roughness;
            half   _WetRoughness;
            half   _Wetness;
            half   _DampFilm;
            half   _PuddleEdge;
            half   _PuddleRandomness;
            half   _PuddleNoiseScale;
            half   _WetDarkening;
            half   _WaterFilm;
            half   _GrainStrength;
            half   _GrainScale;
            half   _CrackAmount;
            half   _CrackScale;
            half   _RoughnessVariation;
            half   _MacroStrength;
            half   _WearDarkening;
            half   _Cutoff;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   RoadVert
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);

            // ---- procedural asphalt micro detail ------------------------
            // Replaces the noise/voronoi bump of the Blender material so the
            // surface is not perfectly flat. UV1 is metric, so the grain keeps
            // a constant real-world size no matter how long the road is.
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
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FBM(float2 p)
            {
                float v = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    v += VNoise(p) * a;
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            // asphalt height field: fine grain minus crack lines
            float AsphaltHeight(float2 uv)
            {
                float grain = FBM(uv * _GrainScale);
                float cell  = VNoise(uv * _CrackScale);
                float crack = 1.0 - smoothstep(0.0, 0.055, abs(cell - 0.5));
                return grain - crack * _CrackAmount;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uvLM       : TEXCOORD2;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv0         : TEXCOORD0;
                float2 uv1         : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                half3  normalWS    : TEXCOORD3;
                half4  tangentWS   : TEXCOORD4;
                half4  color       : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                half   fogFactor   : TEXCOORD7;
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
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.tangentWS  = half4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv0        = TRANSFORM_TEX(IN.uv0, _BaseMap);
                OUT.uv1        = TRANSFORM_TEX(IN.uv1, _DetailNormalMap);
                OUT.color      = IN.color;
                OUT.shadowCoord = GetShadowCoord(pos);
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);

                OUTPUT_LIGHTMAP_UV(IN.uvLM, unity_LightmapST, OUT.uvLM);
                OUTPUT_SH(nrm.normalWS, OUT.vertexSH);
                return OUT;
            }

            half4 RoadFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // ---- baked masks --------------------------------------------
                half wear      = IN.color.r;   // worn / patched asphalt
                half retention = IN.color.g;   // how well this spot holds water
                half macro     = IN.color.b;   // albedo multiplier, breaks tiling

                // ---- albedo --------------------------------------------------
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv0).rgb * _BaseColor.rgb;
                albedo *= lerp(1.0h, macro, _MacroStrength);
                albedo *= lerp(1.0h, 1.0h - _WearDarkening, wear);

                // ---- extra shader-side puddle randomness ----------------------
                // Leave at 0 to match Blender exactly (the baked mask already
                // scatters puddles). Raise it to re-scatter them in world space
                // without re-exporting the mesh.
                if (_PuddleRandomness > 0.001h)
                {
                    half procPud = smoothstep(0.55h, 0.78h,
                        FBM(IN.positionWS.xz * _PuddleNoiseScale));
                    retention = lerp(retention, procPud, _PuddleRandomness);
                }

                // ---- wetness (same maths as the Blender material) ------------
                half lo      = 1.0h - _Wetness;
                half wetArea = smoothstep(lo, lo + _PuddleEdge, retention) * _Wetness;
                half damp    = _Wetness * _DampFilm;
                half wet     = saturate(max(wetArea, damp));

                albedo *= lerp(1.0h, 1.0h - _WetDarkening, wet);

                half roughness  = lerp(_Roughness, _WetRoughness, wet);
                half smoothness = saturate(1.0h - roughness + wetArea * _WaterFilm * 0.35h);


                // ---- micro detail: procedural grain + optional normal map ----
                // Water fills the pores, so detail fades out where the road is wet.
                half detailFade = 1.0h - wet;

                float2 duv = IN.uv1;
                float  eps = 0.35 / max(_GrainScale, 1.0);
                float  h0  = AsphaltHeight(duv);
                float  hx  = AsphaltHeight(duv + float2(eps, 0));
                float  hy  = AsphaltHeight(duv + float2(0, eps));

                half3 grainTS = normalize(half3(
                    (h0 - hx) * _GrainStrength * detailFade * 4.0h,
                    (h0 - hy) * _GrainStrength * detailFade * 4.0h,
                    1.0h));

                half3 mapTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, IN.uv1),
                    _DetailNormalScale * detailFade);

                half3 detailTS = normalize(half3(grainTS.xy + mapTS.xy, grainTS.z * mapTS.z));

                // break up the uniform sheen the flat surface was showing
                roughness  = saturate(roughness + (h0 - 0.5h) * _RoughnessVariation * detailFade);
                smoothness = saturate(1.0h - roughness + wetArea * _WaterFilm * 0.35h);
                albedo    *= lerp(1.0h, 0.82h + h0 * 0.36h, detailFade * 0.6h);

                half3 bitangent = IN.tangentWS.w * cross(IN.normalWS, IN.tangentWS.xyz);
                half3x3 tbn = half3x3(IN.tangentWS.xyz, bitangent, IN.normalWS);
                half3 normalWS = NormalizeNormalPerPixel(mul(detailTS, tbn));

                // ---- lighting -------------------------------------------------
                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = albedo;
                surface.metallic   = 0.0h;
                surface.specular   = half3(0,0,0);
                surface.smoothness = smoothness;
                surface.normalTS   = detailTS;
                surface.occlusion  = 1.0h;
                surface.alpha      = 1.0h;

                InputData input = (InputData)0;
                input.positionWS         = IN.positionWS;
                input.normalWS           = normalWS;
                input.viewDirectionWS    = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                input.shadowCoord        = IN.shadowCoord;
                input.fogCoord           = IN.fogFactor;
                input.vertexLighting     = half3(0,0,0);
                input.bakedGI            = SAMPLE_GI(IN.uvLM, IN.vertexSH, normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                input.shadowMask         = half4(1,1,1,1);

                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
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
            #pragma vertex   ShadowPassVertex
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
            #pragma vertex   DepthOnlyVertex
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
            #pragma vertex   DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
