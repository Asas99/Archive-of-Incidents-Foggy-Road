// RoadTool – Road surface damage decal shader.
// Unlit, transparent, URP 17 / Unity 6 compatible, SRP-Batcher friendly.
// Per-stamp falloff is carried in UV1.x so many stamps share ONE material (batched)
// yet each keeps its own soft edge. Blend mode (Alpha / Additive / Multiply) is a
// keyword variant driven from C# together with _SrcBlend / _DstBlend.
Shader "RoadTool/RoadPaintDecal"
{
    Properties
    {
        [MainTexture] _BaseMap ("Damage Texture", 2D) = "white" {}
        [MainColor]   _BaseColor ("Tint", Color) = (1,1,1,1)
        _Falloff ("Min Falloff", Range(0,1)) = 0.0
        _FalloffPower ("Falloff Power", Range(0.25,4)) = 1.5
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 5
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "DecalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _BLEND_ADDITIVE _BLEND_MULTIPLY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float  _Falloff;
                float  _FalloffPower;
                float  _SrcBlend;
                float  _DstBlend;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1; // x = per-stamp falloff (0 soft .. 1 hard)
                half4  color      : COLOR;     // rgb = tint, a = opacity
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float  falloff     : TEXCOORD1;
                half4  color       : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.falloff = IN.uv1.x;
                OUT.color = IN.color;
                return OUT;
            }

            // Round, soft brush mask from the square quad UV.
            // d = 0 at centre, ~1 at edge mid-points, ~1.41 at corners (so corners round off).
            half RadialFalloff (float2 uv, float stampFalloff)
            {
                float d = saturate(length(uv - 0.5) * 2.0);
                float inner = saturate(max(stampFalloff, _Falloff)); // solid centre fraction
                float t = saturate((1.0 - d) / max(1e-3, 1.0 - inner));
                return pow(t, _FalloffPower);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 col = tex * _BaseColor * IN.color;
                half fo = RadialFalloff(IN.uv, IN.falloff);

                #if defined(_BLEND_MULTIPLY)
                    // DstColor * Zero blend: white = "no change". Lerp toward white by coverage
                    // so partial / faded pixels darken less.
                    half a = saturate(col.a * fo);
                    col.rgb = lerp(half3(1.0, 1.0, 1.0), col.rgb, a);
                    col.a = 1.0;
                #else
                    col.a *= fo;
                #endif

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
