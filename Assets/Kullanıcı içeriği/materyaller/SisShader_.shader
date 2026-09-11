Shader "Custom/RealisticVolumetricFog"
{
    Properties
    {
        [HDR]_FogColor("Fog Color", Color) = (0.56, 0.62, 0.64, 1)
        _Density("Atmosphere Density", Range(0.0, 0.04)) = 0.0045
        _GroundDensity("Ground Bank Density", Range(0.0, 0.08)) = 0.014
        _StartDistance("Start Distance", Float) = 4
        _MaxDistance("Max Distance", Float) = 160
        _BaseHeight("Base Height", Float) = 7
        _GroundThickness("Ground Bank Thickness", Range(0.25, 15.0)) = 3.2
        _HeightFalloff("Atmosphere Height Falloff", Range(0.001, 1.0)) = 0.085
        [IntRange]_Steps("Raymarch Steps", Range(8, 64)) = 48
        _WeatherScale("Weather Scale", Float) = 0.0024
        _NoiseScale("Detail Noise Scale", Float) = 0.012
        _NoiseStrength("Detail Strength", Range(0, 1)) = 0.58
        _Coverage("Bank Coverage", Range(0, 1)) = 0.58
        _Erosion("Bank Erosion", Range(0, 1)) = 0.52
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.35, 0)
        _WindSpeed("Fog Advection Speed", Float) = 0.06
        _SunScattering("Sun Scattering", Range(0, 4)) = 1.05
        _Anisotropy("Anisotropy", Range(-0.8, 0.8)) = 0.48
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.48
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.86
        _MaxOpacity("Maximum Fog Opacity", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "VolumetricFog"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Density;
                float _GroundDensity;
                float _StartDistance;
                float _MaxDistance;
                float _BaseHeight;
                float _GroundThickness;
                float _HeightFalloff;
                float _Steps;
                float _WeatherScale;
                float _NoiseScale;
                float _NoiseStrength;
                float _Coverage;
                float _Erosion;
                float4 _WindDirection;
                float _WindSpeed;
                float _SunScattering;
                float _Anisotropy;
                float _AmbientStrength;
                float _ShadowStrength;
                float _MaxOpacity;
            CBUFFER_END

            // Driven by DynamicWeatherSystem so fog, foliage and sky share one wind field.
            float4 _WeatherWindVector; // xyz direction, w instantaneous strength
            float4 _WeatherDynamics;   // x wind, y gust, z time, w turbulence
            float4 _WeatherFogSky;     // x fog flow, y sky flow, z gust frequency

            // Continuous 3D noise prevents the vertical columns caused by a single XZ lookup.
            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 cell = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(cell + float3(0, 0, 0));
                float n100 = Hash31(cell + float3(1, 0, 0));
                float n010 = Hash31(cell + float3(0, 1, 0));
                float n110 = Hash31(cell + float3(1, 1, 0));
                float n001 = Hash31(cell + float3(0, 0, 1));
                float n101 = Hash31(cell + float3(1, 0, 1));
                float n011 = Hash31(cell + float3(0, 1, 1));
                float n111 = Hash31(cell + float3(1, 1, 1));

                float4 nx = lerp(float4(n000, n010, n001, n011),
                                 float4(n100, n110, n101, n111), f.x);
                float2 nxy = lerp(nx.xz, nx.yw, f.y);
                return lerp(nxy.x, nxy.y, f.z);
            }

            // x: soft detail modulation, y: coherent low-lying bank mask.
            // The macro fields deliberately move only a few metres over many minutes.
            // This keeps a bank readable as a place in the landscape rather than a
            // fast, screen-sized effect.
            float2 SampleFogStructure(float3 worldPos)
            {
                float weatherEnabled = step(0.0001, dot(_WeatherWindVector.xz, _WeatherWindVector.xz));
                float2 windDirection = normalize(lerp(_WindDirection.xz,
                                                      _WeatherWindVector.xz,
                                                      weatherEnabled) + 0.0001);
                // Same rule as the pine wind shader: drift time is always _Time.y so the
                // banks keep moving even when the weather globals are stale.
                float weatherTime = _Time.y;
                float flowSpeed = lerp(_WindSpeed, _WeatherFogSky.x, weatherEnabled);
                float3 drift = float3(windDirection.x, 0.0, windDirection.y) *
                               (weatherTime * flowSpeed);

                // Broad, overlapping fields leave clear corridors between banks.
                float weather = ValueNoise3D((worldPos + drift) * max(_WeatherScale, 0.0001));
                float weatherSecondary = ValueNoise3D((worldPos * 1.73 - drift * 0.37 + 91.7) *
                                                       max(_WeatherScale, 0.0001));
                float detail = ValueNoise3D((worldPos + drift * 0.18 + 37.1) *
                                            max(_NoiseScale, 0.0001));

                float broadShape = lerp(weather, weatherSecondary, 0.42);
                float threshold = lerp(0.72, 0.43, _Coverage);
                float banks = smoothstep(threshold - 0.13, threshold + 0.13, broadShape);
                float erosion = smoothstep(0.20, 0.80, detail);
                banks *= lerp(1.0, erosion, _Erosion * 0.55);

                float detailModulation = lerp(1.0, lerp(0.80, 1.16, detail),
                                              _NoiseStrength * 0.55);
                return float2(detailModulation, banks);
            }

            float GetFogDensity(float3 worldPos, float distanceFromCamera)
            {
                float heightAboveBase = max(worldPos.y - _BaseHeight, 0.0);
                float atmosphereHeight = exp(-heightAboveBase * _HeightFalloff);
                float groundHeight = exp(-heightAboveBase /
                                         max(_GroundThickness, 0.01));
                float startFade = smoothstep(_StartDistance, _StartDistance + 12.0,
                                             distanceFromCamera);
                float2 structure = SampleFogStructure(worldPos);

                // A thin base haze keeps air perspective, while the denser bank term
                // remains spatially broken up. This avoids a uniform white wall.
                float atmosphere = _Density * atmosphereHeight *
                                   lerp(0.42, 1.0, structure.x);
                float groundBanks = _GroundDensity * groundHeight * structure.y;
                return max((atmosphere + groundBanks) * startFade, 0.0);
            }

            float PhaseHG(float cosTheta, float g)
            {
                float g2 = g * g;
                float denominator = pow(max(1.0 + g2 - 2.0 * g * cosTheta,
                                            0.001), 1.5);
                return (1.0 - g2) / denominator;
            }

            float RayJitter(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel,
                            float2(0.06711056, 0.00583715))));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture,
                                                        sampler_LinearClamp, uv);
                float rawDepth = SampleSceneDepth(uv);

                #if UNITY_REVERSED_Z
                    bool isSky = rawDepth <= 0.00001;
                    float farDepth = 0.00001;
                #else
                    bool isSky = rawDepth >= 0.99999;
                    float farDepth = 0.99999;
                #endif

                float3 cameraPos = GetCameraPositionWS();
                float3 farWorldPos = ComputeWorldSpacePosition(uv, farDepth,
                                                                UNITY_MATRIX_I_VP);
                float3 rayDirection = normalize(farWorldPos - cameraPos);

                float rayLength = _MaxDistance;
                if (!isSky)
                {
                    float3 surfaceWorldPos = ComputeWorldSpacePosition(uv, rawDepth,
                                                                       UNITY_MATRIX_I_VP);
                    rayLength = min(distance(cameraPos, surfaceWorldPos), _MaxDistance);
                }

                if (rayLength <= _StartDistance)
                    return sceneColor;

                int stepCount = clamp((int)_Steps, 8, 64);
                float marchLength = rayLength - _StartDistance;
                float stepSize = marchLength / stepCount;
                float travel = _StartDistance + stepSize * RayJitter(input.positionCS.xy);

                float transmittance = 1.0;
                float3 accumulatedFog = 0.0;
                Light mainLight = GetMainLight();
                float3 directionToLight = normalize(-mainLight.direction);
                float cosTheta = dot(-rayDirection, directionToLight);
                float phase = min(PhaseHG(cosTheta, _Anisotropy), 5.0);

                [loop]
                for (int i = 0; i < 64; i++)
                {
                    if (i >= stepCount)
                        break;

                    float3 samplePosition = cameraPos + rayDirection * travel;
                    float density = GetFogDensity(samplePosition, travel);
                    float4 shadowCoord = TransformWorldToShadowCoord(samplePosition);
                    Light sampleLight = GetMainLight(shadowCoord);
                    float shadow = lerp(1.0, sampleLight.shadowAttenuation,
                                        _ShadowStrength);
                    float sampleTransmittance = exp(-density * stepSize);
                    float scatteringAmount = 1.0 - sampleTransmittance;
                    float powder = 1.0 - exp(-density * stepSize * 6.0);
                    float3 incidentLight = _AmbientStrength.xxx +
                                           sampleLight.color * shadow * phase *
                                           _SunScattering * (1.0 + powder * 0.3);
                    accumulatedFog += transmittance * scatteringAmount *
                                      _FogColor.rgb * incidentLight;
                    transmittance *= sampleTransmittance;

                    if (transmittance < 0.015)
                        break;

                    travel += stepSize;
                }

                // Preserve distant contrast instead of ending in a flat grey wall.
                float opacity = 1.0 - transmittance;
                if (opacity > _MaxOpacity)
                {
                    accumulatedFog *= _MaxOpacity / max(opacity, 0.0001);
                    transmittance = 1.0 - _MaxOpacity;
                }

                return float4(sceneColor.rgb * transmittance + accumulatedFog, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
