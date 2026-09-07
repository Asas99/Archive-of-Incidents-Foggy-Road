Shader "Custom/RainyPBR_V9_ConstantDropShape"
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

        _DropScale ("Drop Density", Range(4,40)) = 12
        _VerticalScale ("Vertical Scale", Range(0.2,4)) = 1.15

        _DropSpeed ("Drop Speed", Range(0,5)) = 0.7

        _DropSize ("Drop Size", Range(0.02,0.5)) = 0.16

        _TrailWidth ("Trail Width", Range(0.01,0.25)) = 0.032
        _TrailWobble ("Trail Wobble", Range(0,0.4)) = 0.06

        _LongTrailAmount ("Long Trail Amount", Range(0,1)) = 0.22

        _RainNormalStrength ("Rain Normal Strength", Range(0,5)) = 1.0
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        LOD 300


        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

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

                float _DropScale;
                float _VerticalScale;

                float _DropSpeed;
                float _DropSize;

                float _TrailWidth;
                float _TrailWobble;

                float _LongTrailAmount;

                float _RainNormalStrength;

            CBUFFER_END


            // =========================================================
            // STRUCTS
            // =========================================================

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


            // =========================================================
            // RAIN SAMPLE
            // =========================================================

            struct RainResult
            {
                float mask;

                // Gradient of water height:
                // X = horizontal/circumference direction
                // Y = vertical/world-Y direction
                float gradHorizontal;
                float gradVertical;
            };


            // =========================================================
            // RANDOM
            // =========================================================

            float Hash11(float p)
            {
                p = frac(p * 0.1031);

                p *= p + 33.33;
                p *= p + p;

                return frac(p);
            }


            // =========================================================
            // GAUSSIAN
            //
            // Smooth everywhere.
            //
            // No smoothstep edge.
            // No hard cell boundary.
            // =========================================================

            float Gaussian(float x)
            {
                return exp(-x * x);
            }


            // =========================================================
            // PERIODIC DISTANCE
            //
            // Returns signed shortest distance in a repeating interval.
            //
            // Importantly, our actual droplet has essentially zero
            // influence at the repetition boundary, preventing the
            // horizontal stripes created by V4/V5.
            // =========================================================

            float PeriodicSignedDistance(
                float value,
                float spacing
            )
            {
                float x =
                    frac(
                        value / spacing +
                        0.5
                    )
                    - 0.5;

                return x * spacing;
            }


            // =========================================================
            // ONE RAIN LANE
            // =========================================================

            RainResult EvaluateLane(
                float horizontal,
                float vertical,
                float laneIndex,
                float laneCount,
                float horizontalMetricScale
            )
            {
                RainResult result;

                result.mask = 0;
                result.gradHorizontal = 0;
                result.gradVertical = 0;


                // -----------------------------------------------------
                // Random values
                // -----------------------------------------------------

                float r1 =
                    Hash11(
                        laneIndex * 17.123 +
                        4.17
                    );

                float r2 =
                    Hash11(
                        laneIndex * 43.731 +
                        11.73
                    );

                float r3 =
                    Hash11(
                        laneIndex * 91.171 +
                        21.91
                    );

                float r4 =
                    Hash11(
                        laneIndex * 151.37 +
                        37.51
                    );


                // -----------------------------------------------------
                // Lane center
                // -----------------------------------------------------

                float center =
                    laneIndex +
                    0.5;


                center +=
                    (r1 - 0.5)
                    * 0.55;


                // Continuous wobble.
                //
                // No floor()
                // No noise cell.
                center +=
                    sin(
                        vertical * 0.28 +
                        r2 * 6.283185
                    )
                    *
                    _TrailWobble;


                center +=
                    sin(
                        vertical * 0.71 +
                        r3 * 12.56637
                    )
                    *
                    _TrailWobble
                    *
                    0.25;


                // -----------------------------------------------------
                // Circular horizontal distance
                //
                // Important for the cylindrical mapping seam.
                // -----------------------------------------------------

                // Angular/lane-space shortest distance.
                float dxLane =
                    horizontal -
                    center;


                dxLane =
                    dxLane -
                    round(
                        dxLane /
                        laneCount
                    )
                    *
                    laneCount;


                // IMPORTANT:
                // One angular lane becomes physically narrower near the
                // top/bottom of a sphere.  Without this correction a
                // circular droplet becomes a long thin slit near the pole.
                //
                // Convert lane-space distance into a surface-metric
                // distance normalized to the equatorial/reference lane
                // width.  This keeps the droplet's physical aspect ratio
                // approximately constant as it falls.
                float dx =
                    dxLane *
                    horizontalMetricScale;


                // =====================================================
                // LONG TRAIL
                // =====================================================

                float longExists =
                    smoothstep(
                        1.0 - _LongTrailAmount,
                        1.0,
                        r4
                    );


                float trailWidth =
                    max(
                        _TrailWidth *
                        lerp(
                            0.8,
                            1.35,
                            r2
                        ),
                        0.001
                    );


                float normalizedTrailX =
                    dx /
                    trailWidth;


                float trail =
                    Gaussian(
                        normalizedTrailX
                    );


                // Never break the long trail vertically.
                // Long streaks should stay subtle.
                // The moving bead is the visual focus.
                float trailHeight =
                    trail *
                    longExists *
                    0.16;


                // Analytic horizontal derivative
                float trailGradX =
                    trailHeight *
                    (
                        -2.0 *
                        dx /
                        (
                            trailWidth *
                            trailWidth
                        )
                    );


                // =====================================================
                // MOVING DROP
                // =====================================================

                float speed =
                    _DropSpeed *
                    lerp(
                        0.65,
                        1.4,
                        r2
                    );


                float spacing =
                    lerp(
                        2.8,
                        5.5,
                        r3
                    );


                float movingVertical =
                    vertical +
                    _Time.y *
                    speed *
                    2.0;


                float dy =
                    PeriodicSignedDistance(
                        movingVertical +
                        r1 *
                        spacing,
                        spacing
                    );


                float dropWidth =
                    max(
                        _DropSize *
                        lerp(
                            0.75,
                            1.35,
                            r3
                        ),
                        0.001
                    );


                // Keep the moving bead close to round.
                // Slight vertical elongation still suggests gravity,
                // but avoids the "stretched capsule" look.
                // Fixed aspect ratio: the bead itself does not stretch
                // while moving over the surface.
                float dropHeight =
                    dropWidth;


                float nx =
                    dx /
                    dropWidth;


                float ny =
                    dy /
                    dropHeight;


                // Gaussian ellipsoid
                float drop =
                    exp(
                        -(
                            nx * nx +
                            ny * ny
                        )
                        * 1.6
                    );


                // -----------------------------------------------------
                // Analytic drop gradients
                // -----------------------------------------------------

                float dropGradX =
                    drop *
                    (
                        -3.2 *
                        dx /
                        (
                            dropWidth *
                            dropWidth
                        )
                    );


                float dropGradY =
                    drop *
                    (
                        -3.2 *
                        dy /
                        (
                            dropHeight *
                            dropHeight
                        )
                    );


                // =====================================================
                // SMALL TAIL UNDER/ABOVE MOVING DROP
                // =====================================================

                // Water trail behind the falling drop.
                //
                // Because vertical increases upward,
                // the trail exists ABOVE the moving head.
                float behind =
                    max(
                        dy,
                        0.0
                    );


                // Short wake behind the droplet.
                // Previously this was 5x the bead height and visually
                // merged the bead into one long stretched shape.
                float tailLength =
                    dropHeight *
                    1.35;


                float verticalTail =
                    exp(
                        -behind /
                        max(
                            tailLength,
                            0.001
                        )
                    );


                // Suppress everything below the drop.
                float tailSide =
                    smoothstep(
                        -0.03,
                        0.05,
                        dy
                    );


                verticalTail *=
                    tailSide;


                float tailHorizontal =
                    Gaussian(
                        dx /
                        max(
                            trailWidth * 0.75,
                            0.001
                        )
                    );


                float tail =
                    verticalTail *
                    tailHorizontal *
                    0.10;


                float tailGradX =
                    tail *
                    (
                        -2.0 *
                        dx /
                        max(
                            trailWidth *
                            trailWidth *
                            0.5625,
                            0.00001
                        )
                    );


                // approximate smooth vertical tail slope
                float tailGradY =
                    -tail /
                    max(
                        tailLength,
                        0.001
                    );


                // =====================================================
                // ADDITIVE WATER HEIGHT
                //
                // No max() between completely different procedural
                // cells. This makes the height field smoother.
                // =====================================================

                float height =
                    trailHeight +
                    drop +
                    tail;


                result.mask =
                    saturate(height);


                result.gradHorizontal =
                    trailGradX +
                    dropGradX +
                    tailGradX;


                result.gradVertical =
                    dropGradY +
                    tailGradY;


                return result;
            }


            // =========================================================
            // CYLINDRICAL RAIN FIELD
            //
            // Horizontal = around object's centre
            // Vertical   = WORLD Y
            //
            // Therefore animation can only travel up/down.
            // =========================================================

            RainResult RainField(
                float3 positionWS
            )
            {
                RainResult result;

                result.mask = 0;
                result.gradHorizontal = 0;
                result.gradVertical = 0;


                // Object origin in world space.
                float3 objectOriginWS =
                    TransformObjectToWorld(
                        float3(
                            0,
                            0,
                            0
                        )
                    );


                float3 relative =
                    positionWS -
                    objectOriginWS;


                // -----------------------------------------------------
                // Cylindrical angle
                // -----------------------------------------------------

                float angle =
                    atan2(
                        relative.z,
                        relative.x
                    );


                float normalizedAngle =
                    angle /
                    6.28318530718 +
                    0.5;


                // Force integer lane count.
                float laneCount =
                    max(
                        4.0,
                        floor(
                            _DropScale
                        )
                    );


                float horizontal =
                    normalizedAngle *
                    laneCount;


                // =====================================================
                // SURFACE METRIC CORRECTION
                // =====================================================
                //
                // Cylindrical angle is not a uniform physical distance on
                // a sphere: circles of latitude get smaller near the poles.
                // If we use angle directly, droplets become thin vertical
                // slits near the top and become rounder toward the equator.
                //
                // Use the distance from the object's centre as a reference
                // radius, then compensate horizontal distances by the local
                // XZ radius / reference radius ratio.
                //
                // On a sphere:
                //   referenceRadius ~= sphere radius (constant)
                //   localXZRadius   ~= radius * cos(latitude)
                //
                // This causes a droplet to occupy more angular width near
                // the pole, preserving roughly the same WORLD-SPACE shape.
                float referenceRadius =
                    max(
                        length(relative),
                        0.001
                    );


                float localXZRadius =
                    length(
                        relative.xz
                    );


                float horizontalMetricScale =
                    clamp(
                        localXZRadius /
                        referenceRadius,
                        0.02,
                        1.0
                    );


                // Reference physical width of one lane at the object's
                // reference/equatorial radius.
                float referenceLaneWorldWidth =
                    max(
                        (
                            6.28318530718 *
                            referenceRadius
                        )
                        /
                        laneCount,
                        0.001
                    );


                // Vertical coordinate is expressed in the SAME normalized
                // physical units as the corrected horizontal coordinate.
                // Therefore moving up/down no longer changes the bead's
                // apparent aspect ratio.
                float vertical =
                    (
                        relative.y /
                        referenceLaneWorldWidth
                    )
                    *
                    _VerticalScale;


                float baseLane =
                    floor(
                        horizontal
                    );


                float totalMask =
                    0.0;


                float totalGradX =
                    0.0;


                float totalGradY =
                    0.0;


                // -----------------------------------------------------
                // Nearby lanes
                //
                // Enough overlap that changing baseLane does not expose
                // a visible lane boundary.
                // -----------------------------------------------------

                [unroll]
                for (
                    int offset = -2;
                    offset <= 2;
                    offset++
                )
                {
                    float rawLane =
                        baseLane +
                        (float)offset;


                    float wrappedLane =
                        rawLane -
                        floor(
                            rawLane /
                            laneCount
                        )
                        *
                        laneCount;


                    RainResult lane =
                        EvaluateLane(
                            horizontal,
                            vertical,
                            wrappedLane,
                            laneCount,
                            horizontalMetricScale
                        );


                    totalMask +=
                        lane.mask;


                    totalGradX +=
                        lane.gradHorizontal;


                    totalGradY +=
                        lane.gradVertical;
                }


                result.mask =
                    saturate(
                        totalMask *
                        _RainAmount
                    );


                result.gradHorizontal =
                    totalGradX *
                    _RainAmount;


                result.gradVertical =
                    totalGradY *
                    _RainAmount;


                // Safe clamp.
                result.gradHorizontal =
                    clamp(
                        result.gradHorizontal,
                        -8.0,
                        8.0
                    );


                result.gradVertical =
                    clamp(
                        result.gradVertical,
                        -8.0,
                        8.0
                    );


                return result;
            }


            // =========================================================
            // APPLY RAIN NORMAL
            //
            // No finite differences.
            //
            // This is the main V6 grid fix.
            // =========================================================

            float3 ApplyRainNormal(
                float3 positionWS,
                float3 meshNormalWS,
                RainResult rain
            )
            {
                float3 N =
                    normalize(
                        meshNormalWS
                    );


                float3 objectOriginWS =
                    TransformObjectToWorld(
                        float3(
                            0,
                            0,
                            0
                        )
                    );


                float3 relative =
                    positionWS -
                    objectOriginWS;


                // -----------------------------------------------------
                // Horizontal tangent around world Y axis
                // -----------------------------------------------------

                float3 horizontalDirection =
                    float3(
                        -relative.z,
                        0,
                        relative.x
                    );


                float horizontalLength =
                    length(
                        horizontalDirection
                    );


                if (horizontalLength < 0.0001)
                {
                    horizontalDirection =
                        float3(
                            1,
                            0,
                            0
                        );
                }
                else
                {
                    horizontalDirection /=
                        horizontalLength;
                }


                // Project it onto the actual surface.
                horizontalDirection =
                    horizontalDirection -
                    N *
                    dot(
                        horizontalDirection,
                        N
                    );


                horizontalDirection =
                    normalize(
                        horizontalDirection +
                        float3(
                            0.00001,
                            0,
                            0
                        )
                    );


                // -----------------------------------------------------
                // Vertical falling direction projected onto surface
                // -----------------------------------------------------

                float3 worldUp =
                    float3(
                        0,
                        1,
                        0
                    );


                float3 verticalDirection =
                    worldUp -
                    N *
                    dot(
                        worldUp,
                        N
                    );


                float verticalLength =
                    length(
                        verticalDirection
                    );


                // IMPORTANT:
                // Do NOT fade the vertical normal component near the top
                // of a sphere.  Fading it was the reason the bead became
                // a long thin slit near the pole: only the horizontal
                // gradient remained visible.
                //
                // Normalize the projected gravity direction whenever it
                // exists.  At the exact pole, use a stable tangent fallback
                // so the bead keeps the same 2D normal shape.
                if (verticalLength > 0.0001)
                {
                    verticalDirection /=
                        verticalLength;
                }
                else
                {
                    verticalDirection =
                        normalize(
                            cross(
                                horizontalDirection,
                                N
                            )
                        );
                }


                // -----------------------------------------------------
                // Perturb the normal
                // -----------------------------------------------------

                // Softer normal displacement prevents the thin trail
                // from reading like a deep groove in the surface.
                float strength =
                    _RainNormalStrength *
                    0.035;


                float3 rainNormal =
                    N

                    -

                    horizontalDirection *
                    rain.gradHorizontal *
                    strength

                    -

                    verticalDirection *
                    rain.gradVertical *
                    strength;


                return normalize(
                    rainNormal
                );
            }


            // =========================================================
            // VERTEX
            // =========================================================

            Varyings Vert(
                Attributes input
            )
            {
                Varyings output =
                    (Varyings)0;


                UNITY_SETUP_INSTANCE_ID(
                    input
                );


                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS,
                        input.tangentOS
                    );


                output.positionCS =
                    positionInputs.positionCS;


                output.positionWS =
                    positionInputs.positionWS;


                output.normalWS =
                    normalInputs.normalWS;


                output.tangentWS =
                    float4(
                        normalInputs.tangentWS,
                        input.tangentOS.w
                    );


                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _BaseMap
                    );


                output.fogFactor =
                    ComputeFogFactor(
                        positionInputs.positionCS.z
                    );


                return output;
            }


            // =========================================================
            // FRAGMENT
            // =========================================================

            half4 Frag(
                Varyings input
            )
            :
            SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(
                    input
                );


                // =====================================================
                // ALBEDO
                // =====================================================

                half4 baseSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );


                half3 albedo =
                    baseSample.rgb *
                    _BaseColor.rgb;


                // =====================================================
                // BASE NORMAL MAP
                // =====================================================

                half4 normalSample =
                    SAMPLE_TEXTURE2D(
                        _NormalMap,
                        sampler_NormalMap,
                        input.uv
                    );


                half3 normalTS =
                    UnpackNormalScale(
                        normalSample,
                        _NormalStrength
                    );


                float3 meshNormalWS =
                    normalize(
                        input.normalWS
                    );


                float3 tangentWS =
                    normalize(
                        input.tangentWS.xyz
                    );


                float tangentSign =
                    input.tangentWS.w *
                    GetOddNegativeScale();


                float3 bitangentWS =
                    normalize(
                        cross(
                            meshNormalWS,
                            tangentWS
                        )
                        *
                        tangentSign
                    );


                float3x3 tangentToWorld =
                    float3x3(
                        tangentWS,
                        bitangentWS,
                        meshNormalWS
                    );


                float3 baseNormalWS =
                    normalize(
                        TransformTangentToWorld(
                            normalTS,
                            tangentToWorld
                        )
                    );


                // =====================================================
                // RAIN
                // =====================================================

                RainResult rain =
                    RainField(
                        input.positionWS
                    );


                float3 rainNormalWS =
                    ApplyRainNormal(
                        input.positionWS,
                        meshNormalWS,
                        rain
                    );


                // Ignore very faint water height for normal perturbation.
                // This keeps subtle trails subtle while the bead stays clear.
                float rainNormalMask =
                    smoothstep(
                        0.10,
                        0.48,
                        rain.mask
                    );


                float3 finalNormalWS =
                    normalize(
                        lerp(
                            baseNormalWS,
                            rainNormalWS,
                            rainNormalMask
                        )
                    );


                // =====================================================
                // WET SURFACE
                // =====================================================

                float wetMask =
                    saturate(
                        _Wetness +
                        rain.mask
                    );


                albedo *=
                    lerp(
                        1.0,
                        1.0 -
                        _WetDarkening,
                        wetMask
                    );


                float finalSmoothness =
                    lerp(
                        _Smoothness,
                        _WetSmoothness,
                        wetMask
                    );


                // =====================================================
                // URP INPUT
                // =====================================================

                InputData inputData =
                    (InputData)0;


                inputData.positionWS =
                    input.positionWS;


                inputData.normalWS =
                    finalNormalWS;


                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(
                        input.positionWS
                    );


                inputData.shadowCoord =
                    TransformWorldToShadowCoord(
                        input.positionWS
                    );


                inputData.fogCoord =
                    input.fogFactor;


                inputData.vertexLighting =
                    half3(
                        0,
                        0,
                        0
                    );


                inputData.bakedGI =
                    SampleSH(
                        finalNormalWS
                    );


                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        input.positionCS
                    );


                inputData.shadowMask =
                    half4(
                        1,
                        1,
                        1,
                        1
                    );


                // =====================================================
                // PBR
                // =====================================================

                SurfaceData surfaceData =
                    (SurfaceData)0;


                surfaceData.albedo =
                    albedo;


                surfaceData.metallic =
                    _Metallic;


                surfaceData.specular =
                    half3(
                        0,
                        0,
                        0
                    );


                surfaceData.smoothness =
                    finalSmoothness;


                surfaceData.normalTS =
                    normalTS;


                surfaceData.emission =
                    half3(
                        0,
                        0,
                        0
                    );


                surfaceData.occlusion =
                    1.0;


                surfaceData.alpha =
                    1.0;


                half4 color =
                    UniversalFragmentPBR(
                        inputData,
                        surfaceData
                    );


                color.rgb =
                    MixFog(
                        color.rgb,
                        inputData.fogCoord
                    );


                return color;
            }


            ENDHLSL
        }


        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }


    FallBack "Universal Render Pipeline/Lit"
}