Shader "Environment/Dynamic Overcast Sky"
{
    Properties
    {
        [HDR] _Tint("Atmosphere Tint", Color) = (0.78, 0.83, 0.88, 1)
        _Exposure("Exposure", Range(0, 8)) = 0.92
        _Rotation("Rotation", Range(0, 360)) = 0
        _CloudMotion("Cloud Motion", Range(0, 2)) = 0.38
        _CloudParallax("Cloud Layer Parallax", Range(0, 0.08)) = 0.025
        _CloudContrast("Cloud Contrast", Range(0.5, 2)) = 0.88
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Tint;
            float _Exposure;
            float _Rotation;
            float _CloudMotion;
            float _CloudParallax;
            float _CloudContrast;
            float4 _WeatherWindVector;
            float4 _WeatherDynamics;
            float4 _WeatherFogSky;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float2 DirectionToLatLong(float3 direction)
            {
                direction = normalize(direction);
                float2 uv;
                uv.x = atan2(direction.x, direction.z) * (0.5 / PI) + 0.5;
                uv.y = asin(clamp(direction.y, -1.0, 1.0)) / PI + 0.5;
                uv.x = frac(uv.x + _Rotation / 360.0);
                return uv;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + 1), f.x), f.y);
            }

            float CloudFbm(float2 p)
            {
                float value = 0.0;
                value += Noise2D(p) * 0.53;
                p = p * 2.03 + 17.1;
                value += Noise2D(p) * 0.27;
                p = p * 2.07 - 8.4;
                value += Noise2D(p) * 0.135;
                p = p * 2.11 + 3.7;
                value += Noise2D(p) * 0.0675;
                return value;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float2 uv = DirectionToLatLong(direction);
                float2 wind = normalize(_WeatherWindVector.xz + float2(0.0001, 0.0001));
                float time = _Time.y * _CloudMotion * max(_WeatherFogSky.y, 0.01);
                float gust = _WeatherDynamics.y;

                float horizon = pow(1.0 - saturate(direction.y * 0.5 + 0.5), 1.7);
                float2 cloudUv = float2(uv.x * 7.0, uv.y * 4.2);
                float2 broadOffset = wind * time * 0.085;
                float2 detailOffset = -wind * time * 0.17;
                detailOffset += float2(sin(time * 0.13), cos(time * 0.09)) * 0.08 * gust;

                float broad = CloudFbm(cloudUv + broadOffset);
                float detail = CloudFbm(cloudUv * 1.85 + detailOffset + _CloudParallax);
                float cloud = smoothstep(0.24, 0.82, saturate(broad * 0.72 + detail * 0.28));

                half3 zenithColor = half3(0.19, 0.22, 0.25);
                half3 horizonColor = half3(0.40, 0.43, 0.45);
                half3 baseSky = lerp(zenithColor, horizonColor, horizon);
                half cloudShade = lerp(0.68, 1.04, cloud);
                half3 color = baseSky * cloudShade;
                color = pow(max(color, 0.0001), _CloudContrast.xxx);
                return half4(color * _Tint.rgb * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
