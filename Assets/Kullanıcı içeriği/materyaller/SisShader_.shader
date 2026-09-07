Shader "Custom/RealisticVolumetricFog"
{
    Properties
    {
        [HDR]_FogColor("Fog Color", Color) = (0.65, 0.7, 0.75, 1)

        _Density("Density", Range(0.0, 0.2)) = 0.025

        _StartDistance("Start Distance", Float) = 0
        _MaxDistance("Max Distance", Float) = 150

        _BaseHeight("Base Height", Float) = 5
        _HeightFalloff("Height Falloff", Range(0.001, 2.0)) = 0.08

        [IntRange]_Steps("Raymarch Steps", Range(4, 64)) = 24

        _NoiseTex("Fog Noise", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Float) = 0.015
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.6

        _WindDirection("Wind Direction", Vector) = (1, 0, 0.35, 0)
        _WindSpeed("Wind Speed", Float) = 0.5

        _SunScattering("Sun Scattering", Range(0, 4)) = 1
        _Anisotropy("Anisotropy", Range(-0.8, 0.8)) = 0.35

        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "VolumetricFog"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _FogColor;

                float _Density;

                float _StartDistance;
                float _MaxDistance;

                float _BaseHeight;
                float _HeightFalloff;

                float _Steps;

                float _NoiseScale;
                float _NoiseStrength;

                float4 _WindDirection;
                float _WindSpeed;

                float _SunScattering;
                float _Anisotropy;

                float _AmbientStrength;

            CBUFFER_END


            // ------------------------------------------------------------
            // Henyey-Greenstein phase function
            // Işığın sis içerisindeki yönsel saçılımı
            // ------------------------------------------------------------

            float PhaseHG(float cosTheta, float g)
            {
                float g2 = g * g;

                float denominator =
                    pow(
                        max(
                            1.0 + g2 - 2.0 * g * cosTheta,
                            0.0001
                        ),
                        1.5
                    );

                return (1.0 - g2) / denominator;
            }


            // ------------------------------------------------------------
            // Fog Noise
            // ------------------------------------------------------------

            float SampleFogNoise(float3 worldPos)
            {
                float2 wind =
                    _WindDirection.xz *
                    _Time.y *
                    _WindSpeed;

                float2 uv1 =
                    worldPos.xz *
                    _NoiseScale +
                    wind;

                float2 uv2 =
                    worldPos.xz *
                    (_NoiseScale * 2.13) -
                    wind * 0.47;


                float noise1 =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        uv1
                    ).r;


                float noise2 =
                    SAMPLE_TEXTURE2D(
                        _NoiseTex,
                        sampler_NoiseTex,
                        uv2
                    ).r;


                float noise =
                    noise1 * 0.65 +
                    noise2 * 0.35;


                // Noise strength = 0 olduğunda tamamen homojen fog
                return lerp(
                    1.0,
                    noise * 2.0,
                    _NoiseStrength
                );
            }


            // ------------------------------------------------------------
            // Height Fog Density
            // ------------------------------------------------------------

            float GetFogDensity(float3 worldPos, float distanceFromCamera)
            {
                float heightAboveBase =
                    max(
                        worldPos.y - _BaseHeight,
                        0.0
                    );


                float heightDensity =
                    exp(
                        -heightAboveBase *
                        _HeightFalloff
                    );


                float startFade =
                    smoothstep(
                        _StartDistance,
                        _StartDistance + 5.0,
                        distanceFromCamera
                    );


                float noise =
                    SampleFogNoise(worldPos);


                float density =
                    _Density *
                    heightDensity *
                    noise *
                    startFade;


                return max(density, 0.0);
            }


            // ------------------------------------------------------------
            // Fragment
            // ------------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;


                // Original camera image
                float4 sceneColor =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv
                    );


                // --------------------------------------------------------
                // Scene Depth
                // --------------------------------------------------------

                float rawDepth =
                    SampleSceneDepth(uv);


                bool isSky;


                #if UNITY_REVERSED_Z

                    isSky = rawDepth <= 0.00001;

                    float farDepth = 0.00001;

                #else

                    isSky = rawDepth >= 0.99999;

                    float farDepth = 0.99999;

                #endif


                // --------------------------------------------------------
                // Camera → world ray reconstruction
                // --------------------------------------------------------

                float3 cameraPos =
                    GetCameraPositionWS();


                float3 farWorldPos =
                    ComputeWorldSpacePosition(
                        uv,
                        farDepth,
                        UNITY_MATRIX_I_VP
                    );


                float3 rayDirection =
                    normalize(
                        farWorldPos -
                        cameraPos
                    );


                // --------------------------------------------------------
                // How far can the fog ray travel?
                // --------------------------------------------------------

                float rayLength;


                if (isSky)
                {
                    rayLength = _MaxDistance;
                }
                else
                {
                    float3 surfaceWorldPos =
                        ComputeWorldSpacePosition(
                            uv,
                            rawDepth,
                            UNITY_MATRIX_I_VP
                        );


                    rayLength =
                        distance(
                            cameraPos,
                            surfaceWorldPos
                        );


                    rayLength =
                        min(
                            rayLength,
                            _MaxDistance
                        );
                }


                // Pixel fog başlamadan önce yüzeye çarpıyorsa
                // direkt original color
                if (rayLength <= _StartDistance)
                {
                    return sceneColor;
                }


                // --------------------------------------------------------
                // Raymarch setup
                // --------------------------------------------------------

                int stepCount =
                    clamp(
                        (int)_Steps,
                        4,
                        64
                    );


                float marchLength =
                    rayLength -
                    _StartDistance;


                float stepSize =
                    marchLength /
                    stepCount;


                // İlk sample'ı step'in ortasına koyuyoruz.
                // Banding'i biraz azaltıyor.
                float travel =
                    _StartDistance +
                    stepSize * 0.5;


                float transmittance = 1.0;

                float3 accumulatedFog = 0;


                // --------------------------------------------------------
                // Main directional light
                // --------------------------------------------------------

                Light mainLight =
                    GetMainLight();


                float3 lightColor =
                    mainLight.color;


                // GetMainLight().direction ışığın sahneye doğru yönü.
                // Scatter hesabında sample'dan ışığa giden yön lazım.
                float3 directionToLight =
                    normalize(
                        -mainLight.direction
                    );


                // --------------------------------------------------------
                // Raymarch
                // --------------------------------------------------------

                [loop]
                for (int i = 0; i < 64; i++)
                {
                    if (i >= stepCount)
                        break;


                    float3 samplePosition =
                        cameraPos +
                        rayDirection *
                        travel;


                    float density =
                        GetFogDensity(
                            samplePosition,
                            travel
                        );


                    // ----------------------------------------------------
                    // Directional scattering
                    // ----------------------------------------------------

                    float cosTheta =
                        dot(
                            -rayDirection,
                            directionToLight
                        );


                    float phase =
                        PhaseHG(
                            cosTheta,
                            _Anisotropy
                        );


                    // Prevent extreme highlights
                    phase =
                        min(
                            phase,
                            6.0
                        );


                    float3 lighting =
                        _AmbientStrength +
                        lightColor *
                        phase *
                        _SunScattering;


                    // ----------------------------------------------------
                    // Beer-Lambert extinction
                    // ----------------------------------------------------

                    float extinction =
                        density *
                        stepSize;


                    float sampleTransmittance =
                        exp(-extinction);


                    float scatteringAmount =
                        1.0 -
                        sampleTransmittance;


                    accumulatedFog +=
                        transmittance *
                        scatteringAmount *
                        _FogColor.rgb *
                        lighting;


                    transmittance *=
                        sampleTransmittance;


                    // Artık ışığın %1'inden azı geçiyorsa
                    // devam etmeye gerek yok.
                    if (transmittance < 0.01)
                        break;


                    travel += stepSize;
                }


                // --------------------------------------------------------
                // Composite
                // --------------------------------------------------------

                float3 finalColor =
                    sceneColor.rgb *
                    transmittance +
                    accumulatedFog;


                return float4(
                    finalColor,
                    sceneColor.a
                );
            }

            ENDHLSL
        }
    }
}