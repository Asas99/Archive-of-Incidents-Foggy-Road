
Shader "Custom/RainyPBR_V16_ImpactSplatter"
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
        _Wetness ("General Wetness", Range(0,1)) = 0.05
        _WetDarkening ("Wet Darkening", Range(0,0.5)) = 0.04
        _DropDarkening ("Drop Darkening", Range(0,1)) = 0.00
        _DropShadingStrength ("Drop Shading Strength", Range(0,1)) = 0.35
        _WetSmoothness ("Wet Smoothness", Range(0,1)) = 0.98

        [Header(Rain Shape)]
        _RainAmount ("Rain Amount", Range(0,2)) = 1
        _DropDensity ("Drop Density", Range(0.5,60)) = 8
        _DropSpeed ("Drop Speed", Range(0,10)) = 0.7
        _DropSize ("Drop Size", Range(0.02,0.5)) = 0.17
        _DropRoundness ("Drop Roundness", Range(0.5,1.5)) = 1.0

        [Header(Trails)]
        _TrailWidth ("Trail Width", Range(0.005,0.2)) = 0.03
        _TrailLength ("Trail Length", Range(0.05,2.0)) = 0.55
        _TrailStrength ("Trail Strength", Range(0,1)) = 0.30
        _TrailWobble ("Trail Wobble", Range(0,0.25)) = 0.045

        [Header(Drift)]
        _SideDriftAmount ("Side Drift Amount", Range(0,0.35)) = 0.08
        _SideDriftFrequency ("Side Drift Frequency", Range(0,4)) = 1.2
        _SideDriftSpeed ("Side Drift Speed", Range(0,4)) = 1.0

        [Header(Impact Splatter)]
        _SplatterAmount ("Splatter Amount", Range(0,2)) = 1.0
        _SplatterDensity ("Splatter Density", Range(0.5,40)) = 7.0
        _SplatterSize ("Splatter Size", Range(0.02,0.5)) = 0.16
        _SplatterRingWidth ("Splatter Ring Width", Range(0.005,0.15)) = 0.025
        _SplatterSpeed ("Splatter Speed", Range(0,8)) = 1.8
        _SplatterLifetime ("Splatter Lifetime", Range(0.1,1)) = 0.55
        _SplatterNormalStrength ("Splatter Normal Strength", Range(0,5)) = 1.0

        [Header(Rain Normal)]
        _RainNormalStrength ("Rain Normal Strength", Range(0,5)) = 0.8
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
                float _DropDarkening;
                float _DropShadingStrength;
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

                float _SideDriftAmount;
                float _SideDriftFrequency;
                float _SideDriftSpeed;

                float _SplatterAmount;
                float _SplatterDensity;
                float _SplatterSize;
                float _SplatterRingWidth;
                float _SplatterSpeed;
                float _SplatterLifetime;
                float _SplatterNormalStrength;

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

            struct Rain2DResult
            {
                float mask;
                float gradX;
                float gradY;
            };

            struct SplatterResult
            {
                float mask;
                float gradX;
                float gradZ;
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

            float2 RepeatCentered(float value, float spacing)
            {
                float cell = floor(value / spacing);
                float centered = (frac(value / spacing + 0.5) - 0.5) * spacing;
                return float2(cell, centered);
            }

            Rain2DResult EvaluateRain2D(float2 uv)
            {
                Rain2DResult r;
                r.mask = 0.0;
                r.gradX = 0.0;
                r.gradY = 0.0;

                // Stable world-space field.
                float2 p = uv * _DropDensity;

                float lane = floor(p.x);
                float localX = frac(p.x) - 0.5;

                float a = Hash11(lane * 13.17 + 1.17);
                float b = Hash11(lane * 37.71 + 8.21);
                float c = Hash11(lane * 79.91 + 17.53);

                float exists = smoothstep(0.18, 0.40, c);

                float laneCenter = (a - 0.5) * 0.55;

                // Static/local curvature in the water path.
                float wobble =
                    (SmoothNoise1D(p.y * 0.20 + b * 11.0) - 0.5) * _TrailWobble +
                    sin(p.y * 0.45 + c * 6.28318) * _TrailWobble * 0.35;

                float spacing = lerp(2.0, 4.4, b);
                float speed   = _DropSpeed * lerp(0.70, 1.35, c);

                // Dynamic sideways meander:
                // raindrops rarely fall as perfectly straight lines.
                // This offset bends the whole falling path, so both the
                // droplet head and its trail drift left/right together.
                float driftCoord =
                    p.y *
                    max(_SideDriftFrequency, 0.0) *
                    lerp(0.75, 1.35, b)

                    -

                    _Time.y *
                    speed *
                    max(_SideDriftSpeed, 0.0) *
                    1.35;

                float sideDrift =
                    sin(
                        driftCoord +
                        a * 6.28318
                    ) *
                    _SideDriftAmount

                    +

                    sin(
                        driftCoord * 0.53 +
                        c * 12.56636
                    ) *
                    _SideDriftAmount *
                    0.45;

                float pathOffset =
                    laneCenter +
                    wobble +
                    sideDrift;

                float dx = localX - pathOffset;

                // y grows downward in the projection coordinates we pass in.
                float travel = p.y - _Time.y * speed * 2.0 + a * spacing;
                float dy = (frac(travel / spacing + 0.5) - 0.5) * spacing;

                float beadHalfWidth  = max(_DropSize, 0.001);
                float beadHalfHeight = max(_DropSize * _DropRoundness, 0.001);

                float nx = dx / beadHalfWidth;
                float ny = dy / beadHalfHeight;

                // Round droplet head.
                float bead = exp(-(nx * nx + ny * ny) * 2.2);

                float beadGradX =
                    bead * (-4.4 * dx / max(beadHalfWidth * beadHalfWidth, 0.00001));

                float beadGradY =
                    bead * (-4.4 * dy / max(beadHalfHeight * beadHalfHeight, 0.00001));

                // Trail behind the droplet: because +Y means downward,
                // trail is where dy < 0 (above the falling head).
                float behind = max(-dy, 0.0);

                float trailX = exp(-pow(dx / max(_TrailWidth, 0.001), 2.0));
                float trailY = exp(-behind / max(_TrailLength, 0.001));

                float trailGate = 1.0 - smoothstep(-0.02, 0.05, dy);

                float trail =
                    trailX *
                    trailY *
                    trailGate *
                    _TrailStrength;

                float trailGradX =
                    trail * (-2.0 * dx / max(_TrailWidth * _TrailWidth, 0.00001));

                // Since behind = -dy for dy < 0, d/dy exp(-behind/L) = +trail/L.
                float trailGradY =
                    trail / max(_TrailLength, 0.001);

                float height = (bead + trail) * exists;

                r.mask = saturate(height);
                r.gradX = (beadGradX + trailGradX) * exists;
                r.gradY = (beadGradY + trailGradY) * exists;

                return r;
            }

            SplatterResult EvaluateSplatter(float2 worldXZ)
            {
                SplatterResult outR;
                outR.mask = 0.0;
                outR.gradX = 0.0;
                outR.gradZ = 0.0;

                float density = max(_SplatterDensity, 0.001);
                float2 p = worldXZ * density;
                float2 baseCell = floor(p);

                // Check neighboring cells so expanding rings do not get cut
                // at procedural cell borders.
                [unroll]
                for (int oy = -1; oy <= 1; oy++)
                {
                    [unroll]
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        float2 cell = baseCell + float2(ox, oy);

                        float r1 = Hash21(cell + float2(1.17, 9.31));
                        float r2 = Hash21(cell + float2(7.73, 3.11));
                        float r3 = Hash21(cell + float2(15.17, 21.71));

                        // Random impact point inside each cell.
                        float2 impactPos =
                            cell +
                            0.5 +
                            float2(r1 - 0.5, r2 - 0.5) * 0.58;

                        float2 d = p - impactPos;
                        float dist = length(d);
                        float2 dir = d / max(dist, 0.0001);

                        // Each cell has its own repeating impact moment.
                        float cycle =
                            frac(
                                _Time.y *
                                _SplatterSpeed *
                                lerp(0.70, 1.35, r2) +
                                r3 * 9.73
                            );

                        // Only the early part of the cycle is visible.
                        float life =
                            saturate(
                                1.0 -
                                cycle /
                                max(_SplatterLifetime, 0.001)
                            );

                        float active =
                            1.0 -
                            step(
                                _SplatterLifetime,
                                cycle
                            );

                        // Expanding circular crown/ring.
                        float radius =
                            lerp(
                                _SplatterSize * 0.15,
                                _SplatterSize,
                                saturate(
                                    cycle /
                                    max(_SplatterLifetime, 0.001)
                                )
                            );

                        float ringWidth =
                            max(
                                _SplatterRingWidth *
                                lerp(0.8, 1.25, r1),
                                0.001
                            );

                        float ringDelta = dist - radius;
                        float ring =
                            exp(
                                -(
                                    ringDelta *
                                    ringDelta
                                )
                                /
                                max(
                                    ringWidth *
                                    ringWidth,
                                    0.00001
                                )
                                * 2.0
                            );

                        ring *= active * life;

                        float ringDeriv =
                            ring *
                            (
                                -4.0 *
                                ringDelta /
                                max(
                                    ringWidth *
                                    ringWidth,
                                    0.00001
                                )
                            );

                        float2 ringGrad =
                            dir *
                            ringDeriv;


                        // Small central impact crown. It vanishes quickly.
                        float centerRadius =
                            max(
                                _SplatterSize * 0.22,
                                0.001
                            );

                        float center =
                            exp(
                                -(
                                    dist *
                                    dist
                                )
                                /
                                max(
                                    centerRadius *
                                    centerRadius,
                                    0.00001
                                )
                                * 2.2
                            );

                        float centerLife =
                            saturate(
                                1.0 -
                                cycle /
                                max(
                                    _SplatterLifetime * 0.40,
                                    0.001
                                )
                            );

                        center *=
                            active *
                            centerLife;

                        float centerDeriv =
                            center *
                            (
                                -4.4 *
                                dist /
                                max(
                                    centerRadius *
                                    centerRadius,
                                    0.00001
                                )
                            );

                        float2 centerGrad =
                            dir *
                            centerDeriv;


                        // A few small satellite splash droplets around the ring.
                        float satelliteMask = 0.0;
                        float2 satelliteGrad = float2(0,0);

                        [unroll]
                        for (int s = 0; s < 5; s++)
                        {
                            float sr =
                                Hash11(
                                    r1 * 31.7 +
                                    (float)s * 17.13 +
                                    dot(cell, float2(3.1, 7.7))
                                );

                            float sa =
                                (
                                    (float)s / 5.0
                                    +
                                    sr * 0.16
                                )
                                *
                                6.2831853;

                            float satelliteRadius =
                                radius *
                                lerp(
                                    0.85,
                                    1.45,
                                    sr
                                );

                            float2 satelliteCenter =
                                impactPos +
                                float2(
                                    cos(sa),
                                    sin(sa)
                                )
                                *
                                satelliteRadius;

                            float2 sd =
                                p -
                                satelliteCenter;

                            float sdist =
                                length(sd);

                            float sSize =
                                max(
                                    _SplatterSize *
                                    lerp(
                                        0.035,
                                        0.075,
                                        sr
                                    ),
                                    0.001
                                );

                            float blob =
                                exp(
                                    -(
                                        sdist *
                                        sdist
                                    )
                                    /
                                    max(
                                        sSize *
                                        sSize,
                                        0.00001
                                    )
                                    * 2.0
                                );

                            blob *=
                                active *
                                life *
                                0.75;

                            satelliteMask +=
                                blob;

                            float2 sdir =
                                sd /
                                max(
                                    sdist,
                                    0.0001
                                );

                            float blobDeriv =
                                blob *
                                (
                                    -4.0 *
                                    sdist /
                                    max(
                                        sSize *
                                        sSize,
                                        0.00001
                                    )
                                );

                            satelliteGrad +=
                                sdir *
                                blobDeriv;
                        }

                        float localMask =
                            ring +
                            center +
                            satelliteMask;

                        outR.mask +=
                            localMask;

                        float2 localGrad =
                            ringGrad +
                            centerGrad +
                            satelliteGrad;

                        outR.gradX +=
                            localGrad.x;

                        outR.gradZ +=
                            localGrad.y;
                    }
                }

                outR.mask =
                    saturate(
                        outR.mask
                    );

                outR.gradX =
                    clamp(
                        outR.gradX,
                        -12.0,
                        12.0
                    );

                outR.gradZ =
                    clamp(
                        outR.gradZ,
                        -12.0,
                        12.0
                    );

                return outR;
            }


            float3 ProjectOntoSurface(float3 dir, float3 normalWS)
            {
                float3 v = dir - normalWS * dot(dir, normalWS);
                float len = length(v);
                if (len > 0.0001)
                    return v / len;

                // Fallback stable tangent
                float3 refAxis = abs(normalWS.y) < 0.95 ? float3(0,1,0) : float3(1,0,0);
                return normalize(cross(refAxis, normalWS));
            }

            void AccumulateProjection(
                inout float totalMask,
                inout float3 normalAccum,
                float projectionWeight,
                float3 baseNormalWS,
                float3 meshNormalWS,
                float2 uv,
                float3 sideAxisWS,
                float3 downAxisWS,
                float verticalSurface
            )
            {
                Rain2DResult rr = EvaluateRain2D(uv);

                float mask = rr.mask * projectionWeight * verticalSurface;
                totalMask += mask;

                float3 sideOnSurface = ProjectOntoSurface(sideAxisWS, meshNormalWS);
                float3 downOnSurface = ProjectOntoSurface(downAxisWS, meshNormalWS);

                float strength = _RainNormalStrength * 0.04;

                float3 projectedNormal =
                    baseNormalWS
                    - sideOnSurface * (rr.gradX * projectionWeight * verticalSurface * strength)
                    - downOnSurface * (rr.gradY * projectionWeight * verticalSurface * strength);

                normalAccum += projectedNormal * mask;
            }

            void RainField(
                float3 positionWS,
                float3 meshNormalWS,
                float3 baseNormalWS,
                out float rainMask,
                out float3 rainNormalWS
            )
            {
                float3 N = normalize(meshNormalWS);
                float3 up = float3(0,1,0);
                float3 down = float3(0,-1,0);

                float3 nAbs = abs(N);

                // Continuous side-facing weight.
                // Avoids a visible latitude-like transition band on curved meshes.
                float verticalSurface =
                    length(
                        float2(
                            N.x,
                            N.z
                        )
                    );

                verticalSurface =
                    saturate(
                        verticalSurface
                    );

                // Smooth continuous falloff toward top/bottom-facing surfaces.
                verticalSurface *=
                    verticalSurface;


                // Sharper but still continuous X/Z triplanar weighting.
                // Squaring the normal components reduces muddy projection blending.
                float wx =
                    nAbs.x *
                    nAbs.x;

                float wz =
                    nAbs.z *
                    nAbs.z;

                float sideSum =
                    max(
                        wx + wz,
                        0.0001
                    );

                wx /=
                    sideSum;

                wz /=
                    sideSum;

                float totalMask = 0.0;
                float3 normalAccum = 0.0;

                // Projection for X-facing surfaces: use Z/Y coordinates.
                AccumulateProjection(
                    totalMask,
                    normalAccum,
                    wx,
                    baseNormalWS,
                    N,
                    float2(positionWS.z, -positionWS.y),
                    float3(0,0,1),
                    down,
                    verticalSurface
                );

                // Projection for Z-facing surfaces: use X/Y coordinates.
                AccumulateProjection(
                    totalMask,
                    normalAccum,
                    wz,
                    baseNormalWS,
                    N,
                    float2(positionWS.x, -positionWS.y),
                    float3(1,0,0),
                    down,
                    verticalSurface
                );


                // =====================================================
                // IMPACT SPLATTER ON UPWARD-FACING SURFACES
                // =====================================================
                //
                // Rain falls along -WorldY, so a surface whose normal
                // points toward +WorldY receives direct impacts.
                // Side streaks fade out while the splatter fades in.
                float impactSurface =
                    smoothstep(
                        0.45,
                        0.93,
                        N.y
                    );

                if (impactSurface > 0.0001)
                {
                    SplatterResult splatter =
                        EvaluateSplatter(
                            positionWS.xz
                        );

                    float splatterMask =
                        saturate(
                            splatter.mask *
                            impactSurface *
                            _SplatterAmount
                        );

                    if (splatterMask > 0.0001)
                    {
                        float3 xOnSurface =
                            ProjectOntoSurface(
                                float3(1,0,0),
                                N
                            );

                        float3 zOnSurface =
                            ProjectOntoSurface(
                                float3(0,0,1),
                                N
                            );

                        float splatterStrength =
                            _SplatterNormalStrength *
                            0.035;

                        float3 splatterNormal =
                            normalize(
                                baseNormalWS

                                -

                                xOnSurface *
                                splatter.gradX *
                                splatterStrength

                                -

                                zOnSurface *
                                splatter.gradZ *
                                splatterStrength
                            );

                        totalMask +=
                            splatterMask;

                        normalAccum +=
                            splatterNormal *
                            splatterMask;
                    }
                }


                rainMask = saturate(totalMask * _RainAmount);

                if (rainMask > 0.0001)
                    rainNormalWS = normalize(normalAccum / max(totalMask, 0.0001));
                else
                    rainNormalWS = baseNormalWS;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

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

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;

                half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
                half3 normalTS = UnpackNormalScale(normalSample, _NormalStrength);

                float3 meshNormalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float tangentSign = input.tangentWS.w * GetOddNegativeScale();
                float3 bitangentWS = normalize(cross(meshNormalWS, tangentWS) * tangentSign);

                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, meshNormalWS);
                float3 baseNormalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));

                float rainMask;
                float3 rainNormalWS;
                RainField(input.positionWS, meshNormalWS, baseNormalWS, rainMask, rainNormalWS);

                float rainNormalMask =
                    smoothstep(0.06, 0.40, rainMask) *
                    _DropShadingStrength;

                float3 finalNormalWS =
                    normalize(
                        lerp(
                            baseNormalWS,
                            rainNormalWS,
                            rainNormalMask
                        )
                    );

                float baseWet = saturate(_Wetness);

                // Geometric rain mask stays unchanged so the drops do not
                // disappear or become fewer.  This separate factor controls
                // only how strongly the water changes lighting/shading.
                float rainWet =
                    saturate(
                        rainMask *
                        1.8 *
                        _DropShadingStrength
                    );

                albedo *= lerp(1.0, 1.0 - _WetDarkening, baseWet);
                albedo *=
                    lerp(
                        1.0,
                        1.0 - _DropDarkening,
                        rainWet
                    );

                float baseSmoothness = lerp(_Smoothness, _WetSmoothness, baseWet);
                float finalSmoothness = lerp(baseSmoothness, 1.0, rainWet);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0,0,0);
                inputData.bakedGI = SampleSH(finalNormalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
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

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }

            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Universal Render Pipeline/Lit"
}
