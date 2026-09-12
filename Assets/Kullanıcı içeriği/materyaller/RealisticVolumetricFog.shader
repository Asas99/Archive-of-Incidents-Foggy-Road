Shader "Custom/RealisticVolumetricFog"
{
    Properties
    {
        [HDR]_FogColor("Fog Color", Color) = (0.56, 0.62, 0.64, 1)
        _Density("Atmosphere Density", Range(0.0, 1)) = 0.01
        _GroundDensity("Ground Bank Density", Range(0.0, 0.8)) = 0.03
        _StartDistance("Start Distance", Float) = 4
        _MaxDistance("Max Distance", Float) = 160
        _BaseHeight("Base Height", Float) = 7
        _GroundThickness("Ground Bank Thickness", Range(0.25, 15.0)) = 3.2
        _HeightFalloff("Atmosphere Height Falloff", Range(0.001, 1.0)) = 0.085
        [IntRange]_Steps("Raymarch Steps", Range(8, 64)) = 48
        _WeatherScale("Weather Scale", Float) = 0.0045
        _NoiseScale("Detail Noise Scale", Float) = 0.032
        _NoiseStrength("Detail Strength", Range(0, 1)) = 0.58
        _Coverage("Bank Coverage", Range(0, 1)) = 0.58
        _Erosion("Bank Erosion", Range(0, 1)) = 0.52
        _VolumeContrast("3D Density Contrast", Range(0.5, 4.0)) = 2.2
        _ClearPocketAmount("Clear Pocket Amount", Range(0, 1)) = 0.5
        _DomainWarp("3D Domain Warp", Range(0, 3)) = 1.2
        _DistantBreakup("Distant Curtain Breakup", Range(0, 1)) = 0.65
        _FogZoneScale("Fog Zone Scale", Range(0.001, 0.03)) = 0.009
        _FogZoneBreakup("Fog Zone Breakup", Range(0, 1)) = 0.72
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.35, 0)
        _WindSpeed("Wind Speed", Float) = 0.08
        _FogAreaCenter("Wind Fog Area Center XZ", Vector) = (420, 0, -35, 0)
        _FogAreaSize("Wind Fog Area Size XZ", Vector) = (100, 140, 0, 0)
        _FogAreaSoftness("Wind Fog Area Edge Softness", Float) = 18
        _AreaWindDirection("Fog Bank Wind Direction", Vector) = (0.72, 0.38, 0, 0)
        _AreaWindSpeed("Fog Bank Wind Speed", Float) = 0.12
        _AreaWaveScale("Fog Bank Wave Scale", Float) = 0.035
        _AreaWaveStrength("Fog Bank Wave Strength", Range(0, 1)) = 0.82
        _AreaEdgeBreakup("Fog Bank Edge Breakup", Range(0, 1)) = 0.78
        _AreaInfluence("Fog Bank Area Influence", Range(0, 1)) = 0.88
        _SunScattering("Sun Scattering", Range(0, 4)) = 1.05
        _AdditionalLightScattering("Headlight / Local Light Scattering", Range(0, 4)) = 1.0
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
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
                float _VolumeContrast;
                float _ClearPocketAmount;
                float _DomainWarp;
                float _DistantBreakup;
                float _FogZoneScale;
                float _FogZoneBreakup;
                float4 _WindDirection;
                float _WindSpeed;
                float4 _FogAreaCenter;
                float4 _FogAreaSize;
                float _FogAreaSoftness;
                float4 _AreaWindDirection;
                float _AreaWindSpeed;
                float _AreaWaveScale;
                float _AreaWaveStrength;
                float _AreaEdgeBreakup;
                float _AreaInfluence;
                float _SunScattering;
                float _AdditionalLightScattering;
                float _Anisotropy;
                float _AmbientStrength;
                float _ShadowStrength;
                float _MaxOpacity;
            CBUFFER_END

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

            // Multi-octave volumetric noise. Axis rotations keep successive
            // octaves from producing visible repeated slabs in world space.
            float FogFBM3D(float3 p)
            {
                float value = ValueNoise3D(p) * 0.55;
                p = p.yzx * 2.03 + float3(19.1, 7.3, 31.7);
                value += ValueNoise3D(p) * 0.30;
                p = p.zxy * 2.01 + float3(-13.4, 27.8, 5.6);
                value += ValueNoise3D(p) * 0.15;
                return value;
            }

            // x: detail modulation, y: low banks, z: broken wisps, w: micro detail.
            // Coordinates and uniform-only values are prepared once per pixel in Frag.
            float4 SampleFogStructure(float3 weatherPosition,
                                      float3 detailPosition,
                                      float threshold)
            {
                float detail = ValueNoise3D(detailPosition);
                float microDetail = ValueNoise3D(detailPosition * 2.07 +
                                                 float3(-11.4, 23.7, 8.6));

                // Warp the large 3D field with smaller 3D fields so dense areas
                // bend into organic pockets instead of reading as flat bands.
                float warp = ((detail - 0.5) + (microDetail - 0.5) * 0.55) *
                             _DomainWarp;
                float3 warpedWeather = weatherPosition +
                                       float3(warp, -warp * 0.38, warp * 0.67);
                float weather = FogFBM3D(warpedWeather);
                weather = saturate((weather - 0.5) * _VolumeContrast + 0.5);

                float erosionNoise = detail * 0.68 + microDetail * 0.32;
                float banks = smoothstep(threshold - 0.07, threshold + 0.13, weather);
                float erodedBanks = banks * smoothstep(0.30, 0.72, erosionNoise);
                banks = lerp(banks, erodedBanks, _Erosion);

                // Carve genuinely clear volumes between fog banks. Increasing
                // Clear Pocket Amount makes the gaps wider and darker.
                float pocketField = weather * 0.72 + detail * 0.20 +
                                    microDetail * 0.08;
                float clearThreshold = lerp(0.30, 0.68, _ClearPocketAmount);
                float volumeMask = smoothstep(clearThreshold - 0.07,
                                              clearThreshold + 0.11,
                                              pocketField);
                float detailModulation = lerp(
                    1.0,
                    lerp(0.28, 1.75, detail) * lerp(0.70, 1.30, microDetail),
                    _NoiseStrength);
                return float4(detailModulation, banks, volumeMask, microDetail);
            }

            float GetFogDensity(float3 worldPos,
                                float distanceFromCamera,
                                float3 weatherPosition,
                                float3 detailPosition,
                                float coverageThreshold,
                                float inverseGroundThickness)
            {
                float heightAboveBase = max(worldPos.y - _BaseHeight, 0.0);
                float atmosphereHeight = exp(-heightAboveBase * _HeightFalloff);
                float groundHeight = exp(-heightAboveBase * inverseGroundThickness);
                float startFade = smoothstep(_StartDistance, _StartDistance + 18.0,
                                             distanceFromCamera);
                float treeLayerFade = smoothstep(_StartDistance + 8.0,
                                                 _StartDistance + 42.0,
                                                 distanceFromCamera);
                float groundBankFade = smoothstep(_StartDistance + 22.0,
                                                  _StartDistance + 62.0,
                                                  distanceFromCamera);
                float4 structure = SampleFogStructure(weatherPosition,
                                                      detailPosition,
                                                      coverageThreshold);

                // World-anchored macro volumes distribute fog across the whole
                // map. Their scale is independent from the camera and ray length,
                // so travelling through the level reveals distinct banks and
                // genuinely clear areas instead of carrying one screen-space veil.
                float3 zonePosition = worldPos *
                                      float3(_FogZoneScale,
                                             _FogZoneScale * 0.52,
                                             _FogZoneScale);
                float zoneField = FogFBM3D(zonePosition +
                                            float3(17.3, -6.1, 31.7));
                zoneField = zoneField * 0.76 +
                            ValueNoise3D(zonePosition * 0.41 +
                                         float3(-9.8, 21.4, 4.6)) * 0.24;
                float zoneThreshold = lerp(0.34, 0.61, _FogZoneBreakup);
                float fogZone = smoothstep(zoneThreshold - 0.10,
                                           zoneThreshold + 0.13,
                                           zoneField);
                float zonePockets = FogFBM3D(zonePosition * 2.65 +
                                              float3(-37.2, 8.4, 13.9));
                zonePockets = smoothstep(0.43, 0.67, zonePockets);
                fogZone *= lerp(0.10, 1.0, zonePockets);
                float zoneFade = smoothstep(_StartDistance + 10.0,
                                            _StartDistance + 38.0,
                                            distanceFromCamera);
                float zoneMask = lerp(1.0,
                                      lerp(0.025, 1.0, fogZone),
                                      zoneFade * _FogZoneBreakup);

                // Keep the far road from becoming a single grey curtain. A very
                // broad 3D region mask opens darker gaps only as distance builds.
                float distantFade = smoothstep(_MaxDistance * 0.34,
                                               _MaxDistance * 0.92,
                                               distanceFromCamera);
                float distantField = FogFBM3D(weatherPosition * 0.43 +
                                              float3(41.7, -18.2, 9.4));
                distantField = distantField * 0.68 + structure.z * 0.32;
                float distantThreshold = lerp(0.31, 0.66, _DistantBreakup);
                float distantRegions = smoothstep(distantThreshold - 0.10,
                                                  distantThreshold + 0.16,
                                                  distantField);
                float distantBreakup = lerp(1.0,
                                            lerp(0.06, 1.18, distantRegions),
                                            distantFade * _DistantBreakup);

                // Keep only a faint continuous layer; most fog now forms pockets.
                float atmosphereMask = lerp(0.0, 1.50, structure.z);
                atmosphereMask *= lerp(1.0, lerp(0.55, 1.25, structure.w),
                                       _NoiseStrength);
                float atmosphereDistance = lerp(0.28, 1.0, treeLayerFade);
                float atmosphere = _Density * atmosphereHeight * structure.x *
                                   atmosphereMask * atmosphereDistance *
                                   distantBreakup * zoneMask;
                float groundBanks = _GroundDensity * groundHeight * structure.y *
                                    groundBankFade *
                                    lerp(1.0, distantBreakup, 0.45) *
                                    lerp(1.0, zoneMask, 0.72);

                // Thin broken strands around trunk height, never a uniform band.
                float midHeight = exp(-abs(worldPos.y - (_BaseHeight + 3.8)) * 0.24);
                float midWisps = _Density * 0.36 * midHeight * structure.z *
                                 structure.w * treeLayerFade * distantBreakup *
                                 zoneMask;

                // A bounded fog bank gives the wind a readable shape. The
                // rectangle is softened and its edge is displaced by a slow,
                // flowing noise field so it rolls into the scene instead of
                // appearing as a hard volume cutoff.
                float2 areaCenter = _FogAreaCenter.xz;
                float2 areaHalfSize = max(_FogAreaSize.xy * 0.5, 1.0);
                float2 areaFlow = _AreaWindDirection.xy;
                areaFlow /= max(length(areaFlow), 0.001);
                float areaTime = _Time.y * _AreaWindSpeed;
                float2 areaDrift = areaFlow * areaTime;
                float areaScale = max(_AreaWaveScale, 0.001);
                float edgeNoiseX = ValueNoise3D(float3(
                    worldPos.xz * areaScale * 0.42 + areaDrift * 0.55,
                    worldPos.y * 0.025 + 11.7));
                float edgeNoiseZ = ValueNoise3D(float3(
                    worldPos.zx * areaScale * 0.36 - areaDrift * 0.38,
                    worldPos.y * 0.031 + 27.4));
                float2 edgeShift = (float2(edgeNoiseX, edgeNoiseZ) - 0.5) *
                                   _FogAreaSoftness * _AreaEdgeBreakup;
                float2 areaLocal = abs(worldPos.xz - areaCenter);
                float2 edgeStart = max(areaHalfSize - _FogAreaSoftness + edgeShift,
                                        float2(0.5, 0.5));
                float2 edgeEnd = max(areaHalfSize + edgeShift, edgeStart + 0.5);
                float2 areaMask2 = 1.0 - smoothstep(edgeStart, edgeEnd, areaLocal);
                float areaMask = areaMask2.x * areaMask2.y;

                float flowNoiseA = ValueNoise3D(float3(
                    worldPos.xz * areaScale + areaDrift,
                    worldPos.y * 0.040 + 4.2));
                float flowNoiseB = ValueNoise3D(float3(
                    worldPos.zx * areaScale * 1.85 - areaDrift * 0.72,
                    worldPos.y * 0.065 + 19.8));
                float flowField = saturate(flowNoiseA * 0.66 + flowNoiseB * 0.34);
                float movingRibbons = smoothstep(0.26, 0.78, flowField);
                float waveStrength = lerp(0.62, 1.42, movingRibbons);
                waveStrength = lerp(1.0, waveStrength, _AreaWaveStrength);
                float areaDensityMultiplier = lerp(1.0, waveStrength,
                                                   areaMask * _AreaInfluence);
                return max((atmosphere + groundBanks + midWisps) *
                           areaDensityMultiplier * startFade, 0.0);
            }

            float PhaseHG(float cosTheta, float g)
            {
                float g2 = g * g;
                float phaseBase = max(1.0 + g2 - 2.0 * g * cosTheta, 0.001);
                // rsqrt(x^3) is equivalent to 1 / pow(x, 1.5), while avoiding
                // a false D3D division-by-zero warning after shader inlining.
                return (1.0 - g2) * rsqrt(phaseBase * phaseBase * phaseBase);
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
                float3 rayDirection;
                float rayLength;

                if (isSky)
                {
                    float3 farWorldPos = ComputeWorldSpacePosition(uv, farDepth,
                                                                   UNITY_MATRIX_I_VP);
                    rayDirection = normalize(farWorldPos - cameraPos);
                    rayLength = _MaxDistance;
                }
                else
                {
                    // The surface point and far-plane point lie on the same
                    // perspective ray. Reconstructing only the surface point
                    // avoids a second inverse-VP matrix multiply per pixel.
                    float3 surfaceVector =
                        ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP) -
                        cameraPos;
                    float surfaceDistance = length(surfaceVector);
                    rayDirection = surfaceVector / max(surfaceDistance, 0.00001);
                    rayLength = min(surfaceDistance, _MaxDistance);
                }

                if (rayLength <= _StartDistance)
                    return sceneColor;

                int stepCount = clamp((int)_Steps, 8, 64);
                float marchLength = rayLength - _StartDistance;
                float stepSize = marchLength / max((float)stepCount, 1.0);
                float travel = _StartDistance + stepSize * RayJitter(input.positionCS.xy);

                // These values are invariant for all ray-march samples in this pixel.
                // Hoisting them avoids repeating identical uniform math up to 48 times.
                float weatherScale = max(_WeatherScale, 0.0001);
                float detailScale = max(_NoiseScale, 0.0001);
                float inverseGroundThickness = rcp(max(_GroundThickness, 0.01));
                float coverageThreshold = lerp(0.68, 0.30, _Coverage);
                float3 windOffset = float3(_WindDirection.x, 0.12, _WindDirection.z) *
                                    (_Time.y * _WindSpeed);

                float3 marchStep = rayDirection * stepSize;
                float3 samplePosition = cameraPos + rayDirection * travel;
                float3 weatherPosition = samplePosition * weatherScale + windOffset * 0.28;
                float3 detailPosition = samplePosition.zyx * detailScale +
                                        windOffset.zyx + 17.31;
                float3 weatherStep = marchStep * weatherScale;
                float3 detailStep = marchStep.zyx * detailScale;

                float transmittance = 1.0;
                float3 accumulatedFog = 0.0;
                Light mainLight = GetMainLight();
                // URP supplies a normalized directional-light vector.
                float3 directionToLight = -mainLight.direction;
                float cosTheta = dot(-rayDirection, directionToLight);
                float phase = min(PhaseHG(cosTheta, _Anisotropy), 5.0);
                float3 directLight = mainLight.color * phase * _SunScattering;
                float3 ambientLight = _AmbientStrength.xxx;
                float3 fogTint = _FogColor.rgb;

                // Clear pockets have zero density. Their shadow and local-light
                // lookups cannot contribute to the final image, so skip them.
                #if defined(_ADDITIONAL_LIGHTS)
                    uint localLightCount = min(GetAdditionalLightsCount(), 4u);
                #endif

                [loop]
                for (int i = 0; i < stepCount; i++)
                {
                    float density = GetFogDensity(samplePosition,
                                                  travel,
                                                  weatherPosition,
                                                   detailPosition,
                                                   coverageThreshold,
                                                   inverseGroundThickness);

                    if (density <= 0.0)
                    {
                        travel += stepSize;
                        samplePosition += marchStep;
                        weatherPosition += weatherStep;
                        detailPosition += detailStep;
                        continue;
                    }

                    float4 shadowCoord = TransformWorldToShadowCoord(samplePosition);
                    float shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                    float shadow = lerp(1.0, shadowAttenuation,
                                        _ShadowStrength);
                    float opticalDepth = density * stepSize;
                    float sampleTransmittance = exp(-opticalDepth);
                    float scatteringAmount = 1.0 - sampleTransmittance;
                    float powder = 1.0 - exp(-opticalDepth * 6.0);
                    // Carry tree shadows into the fog volume so openings and
                    // occluded pockets read differently between the trunks.
                    float shadowedAmbient = lerp(0.48, 1.0, shadow);
                    float3 incidentLight = ambientLight * shadowedAmbient +
                                           directLight * shadow *
                                           (1.0 + powder * 0.3);

                    // Punctual lights are essential for night fog: headlights
                    // illuminate only the mist inside their cones instead of
                    // brightening the whole scene. Limit the loop for predictable
                    // raymarch cost on the long forest road.
                    #if defined(_ADDITIONAL_LIGHTS)
                        [loop] for (uint lightIndex = 0u;
                                    lightIndex < localLightCount;
                                    ++lightIndex)
                        {
                            Light localLight = GetAdditionalLight(lightIndex,
                                                                  samplePosition);
                            float localForward = saturate(
                                dot(-rayDirection, localLight.direction) * 0.5 + 0.5);
                            float localPhase = lerp(0.55, 2.7,
                                                    localForward * localForward *
                                                    localForward);
                            float localAttenuation = localLight.distanceAttenuation *
                                                     localLight.shadowAttenuation;
                            incidentLight += localLight.color * localAttenuation *
                                             localPhase * _AdditionalLightScattering;
                        }
                    #endif

                    accumulatedFog += transmittance * scatteringAmount *
                                      fogTint * incidentLight;
                    transmittance *= sampleTransmittance;

                    if (transmittance < 0.015)
                        break;

                    travel += stepSize;
                    samplePosition += marchStep;
                    weatherPosition += weatherStep;
                    detailPosition += detailStep;
                }

                // Preserve distant contrast instead of ending in a flat grey wall.
                float opacity = 1.0 - transmittance;
                if (opacity > _MaxOpacity)
                {
                    accumulatedFog *= _MaxOpacity / max(opacity, 0.0001);
                    transmittance = 1.0 - _MaxOpacity;
                }

                float3 finalColor = sceneColor.rgb * transmittance + accumulatedFog;
                return float4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
