
Shader "Custom/RainyPBR_V10_SurfaceBasis"
{
    Properties
    {
        [Header(Surface)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Base Smoothness", Range(0,1)) = 0.45

        [Header(Normal)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1

        [Header(Wet Surface)]
        _Wetness ("General Wetness", Range(0,1)) = 0.10
        _WetDarkening ("Wet Darkening", Range(0,0.5)) = 0.08
        _WetSmoothness ("Wet Smoothness", Range(0,1)) = 0.97

        [Header(Rain)]
        _RainAmount ("Rain Amount", Range(0,1)) = 1
        _DropDensity ("Drop Density", Range(1,40)) = 12
        _DropSpeed ("Drop Speed", Range(0,5)) = 0.7
        _DropSize ("Drop Size", Range(0.01,0.5)) = 0.12
        _DropRoundness ("Drop Roundness", Range(0.5,1.5)) = 1.0

        [Header(Trails)]
        _TrailWidth ("Trail Width", Range(0.005,0.2)) = 0.025
        _TrailLength ("Trail Length", Range(0.05,2.0)) = 0.45
        _TrailStrength ("Trail Strength", Range(0,1)) = 0.35
        _TrailWobble ("Trail Wobble", Range(0,0.25)) = 0.05

        [Header(Rain Normal)]
        _RainNormalStrength ("Rain Normal Strength", Range(0,5)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _Metallic;
                float _Smoothness;

                float _NormalStrength;

                float _Wetness;
                float _WetDarkening;
                float _WetSmoothness;

                float _RainAmount;
                float _DropDensity;
                float _DropSpeed;
                float _DropSize;
                float _DropRoundness;

                float _TrailWidth;
                float _TrailLength;
                float _TrailStrength;
                float _TrailWobble;

                float _RainNormalStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float fogFactor   : TEXCOORD4;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct RainResult
            {
                float mask;
                float gradSide;
                float gradDown;
            };

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float SmoothNoise1D(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(Hash11(i), Hash11(i + 1.0), f);
            }

            void BuildSurfaceBasis(float3 normalWS, out float3 sideWS, out float3 downWS, out float verticalValidity)
            {
                float3 N = normalize(normalWS);
                float3 gravityWS = float3(0.0, -1.0, 0.0);

                // Project gravity onto the surface.
                float3 projectedDown = gravityWS - N * dot(gravityWS, N);
                float projectedLen = length(projectedDown);

                verticalValidity = saturate(projectedLen * 5.0);

                if (projectedLen > 0.0001)
                {
                    downWS = projectedDown / projectedLen;
                }
                else
                {
                    // Top/bottom-facing surface: there is no meaningful
                    // "down along the surface". Pick a stable tangent only
                    // so bead shape stays finite; streak contribution will
                    // be suppressed by verticalValidity.
                    float3 refAxis = abs(N.y) < 0.95 ? float3(0,1,0) : float3(1,0,0);
                    sideWS = normalize(cross(refAxis, N));
                    downWS = normalize(cross(N, sideWS));
                    return;
                }

                sideWS = normalize(cross(downWS, N));
            }

            RainResult EvaluateRainLocal(
                float2 surfaceCoord,
                float verticalValidity
            )
            {
                RainResult result;
                result.mask = 0;
                result.gradSide = 0;
                result.gradDown = 0;

                // Physical-ish local coordinates:
                // x = side across the surface
                // y = down along the surface
                float density = max(_DropDensity, 0.001);
                float2 p = surfaceCoord * density;

                float lane = floor(p.x);
                float localX = frac(p.x) - 0.5;

                float r1 = Hash11(lane * 13.71 + 1.23);
                float r2 = Hash11(lane * 37.11 + 7.91);
                float r3 = Hash11(lane * 79.37 + 17.17);

                float laneCenter = (r1 - 0.5) * 0.55;

                // Wobble along the actual falling direction.
                float wobble =
                    (SmoothNoise1D(p.y * 0.20 + r2 * 11.0) - 0.5) * _TrailWobble +
                    sin(p.y * 0.45 + r3 * 6.28318) * _TrailWobble * 0.35;

                float dx = localX - (laneCenter + wobble);

                // -----------------------------------------------------
                // Moving bead
                // -----------------------------------------------------

                float spacing = lerp(2.2, 4.8, r2);
                float speed = _DropSpeed * lerp(0.70, 1.30, r3);

                // Down direction is +Y in this local basis.
                float moving = p.y - _Time.y * speed * 2.0 + r1 * spacing;
                float yCell = frac(moving / spacing + 0.5) - 0.5;
                float dy = yCell * spacing;

                float beadRadius = max(_DropSize, 0.001);
                float beadHalfWidth = beadRadius;
                float beadHalfHeight = beadRadius * max(_DropRoundness, 0.001);

                float nx = dx / beadHalfWidth;
                float ny = dy / beadHalfHeight;

                float bead = exp(-(nx * nx + ny * ny) * 2.0);

                float beadGradX =
                    bead * (-4.0 * dx / max(beadHalfWidth * beadHalfWidth, 0.00001));

                float beadGradY =
                    bead * (-4.0 * dy / max(beadHalfHeight * beadHalfHeight, 0.00001));

                // -----------------------------------------------------
                // Trail behind bead (UPWARD from the falling head)
                // -----------------------------------------------------

                // Since +Y is downward, the trail behind the droplet is
                // located at smaller Y, i.e. dy < 0.
                float behind = max(-dy, 0.0);

                float trailWidth = max(_TrailWidth, 0.001);
                float trailX = exp(-pow(dx / trailWidth, 2.0));

                float trailY =
                    exp(-behind / max(_TrailLength, 0.001)) *
                    smoothstep(0.04, -0.02, dy);

                float trail =
                    trailX *
                    trailY *
                    _TrailStrength *
                    verticalValidity;

                float trailGradX =
                    trail * (-2.0 * dx / max(trailWidth * trailWidth, 0.00001));

                float trailGradY =
                    trail / max(_TrailLength, 0.001);

                // -----------------------------------------------------
                // Sparse lanes
                // -----------------------------------------------------

                float exists = smoothstep(0.15, 0.40, r3);

                float height = (bead + trail) * exists;

                result.mask = saturate(height);
                result.gradSide = (beadGradX + trailGradX) * exists;
                result.gradDown = (beadGradY + trailGradY) * exists;

                return result;
            }

            RainResult RainField(float3 positionWS, float3 normalWS)
            {
                RainResult result;
                result.mask = 0;
                result.gradSide = 0;
                result.gradDown = 0;

                float3 sideWS;
                float3 downWS;
                float verticalValidity;

                BuildSurfaceBasis(
                    normalWS,
                    sideWS,
                    downWS,
                    verticalValidity
                );

                // -----------------------------------------------------
                // KEY CHANGE:
                // No atan2, no cylindrical mapping, no UV mapping.
                //
                // Coordinates are measured directly in WORLD SPACE
                // along a local tangent basis on the surface.
                // This keeps the droplet shape independent of object
                // rotation and avoids latitude/cylindrical stretching.
                // -----------------------------------------------------

                float2 surfaceCoord = float2(
                    dot(positionWS, sideWS),
                    dot(positionWS, downWS)
                );

                result = EvaluateRainLocal(surfaceCoord, verticalValidity);

                result.mask *= _RainAmount;
                result.gradSide *= _RainAmount;
                result.gradDown *= _RainAmount;

                result.gradSide = clamp(result.gradSide, -10.0, 10.0);
                result.gradDown = clamp(result.gradDown, -10.0, 10.0);

                return result;
            }

            float3 ApplyRainNormal(
                float3 baseNormalWS,
                float3 geometricNormalWS,
                RainResult rain
            )
            {
                float3 sideWS;
                float3 downWS;
                float verticalValidity;

                BuildSurfaceBasis(
                    geometricNormalWS,
                    sideWS,
                    downWS,
                    verticalValidity
                );

                float strength = _RainNormalStrength * 0.04;

                float3 perturbed =
                    baseNormalWS
                    - sideWS * rain.gradSide * strength
                    - downWS * rain.gradDown * strength;

                return normalize(perturbed);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseSample =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                half3 albedo =
                    baseSample.rgb * _BaseColor.rgb;

                half4 normalSample =
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);

                half3 normalTS =
                    UnpackNormalScale(normalSample, _NormalStrength);

                float3 meshNormalWS =
                    normalize(input.normalWS);

                float3 tangentWS =
                    normalize(input.tangentWS.xyz);

                float tangentSign =
                    input.tangentWS.w * GetOddNegativeScale();

                float3 bitangentWS =
                    normalize(cross(meshNormalWS, tangentWS) * tangentSign);

                float3x3 tangentToWorld =
                    float3x3(tangentWS, bitangentWS, meshNormalWS);

                float3 baseNormalWS =
                    normalize(TransformTangentToWorld(normalTS, tangentToWorld));

                RainResult rain =
                    RainField(input.positionWS, meshNormalWS);

                float rainNormalMask =
                    smoothstep(0.08, 0.45, rain.mask);

                float3 rainNormalWS =
                    ApplyRainNormal(baseNormalWS, meshNormalWS, rain);

                float3 finalNormalWS =
                    normalize(lerp(baseNormalWS, rainNormalWS, rainNormalMask));

                float baseWet =
                    saturate(_Wetness);

                float rainWet =
                    saturate(rain.mask * 1.8);

                albedo *=
                    lerp(1.0, 1.0 - _WetDarkening, baseWet);

                albedo *=
                    lerp(1.0, 0.82, rainWet);

                float baseSmoothness =
                    lerp(_Smoothness, _WetSmoothness, baseWet);

                float finalSmoothness =
                    lerp(baseSmoothness, 1.0, rainWet);

                InputData inputData = (InputData)0;

                inputData.positionWS = input.positionWS;
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord =
                    TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0,0,0);
                inputData.bakedGI = SampleSH(finalNormalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1,1,1,1);

                SurfaceData surfaceData = (SurfaceData)0;

                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0,0,0);
                surfaceData.smoothness = finalSmoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = half3(0,0,0);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;

                half4 color =
                    UniversalFragmentPBR(inputData, surfaceData);

                color.rgb =
                    MixFog(color.rgb, inputData.fogCoord);

                return color;
            }

            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}
