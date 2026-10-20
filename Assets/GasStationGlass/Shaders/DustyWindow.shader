Shader "FoggyRoad/Gas Station/Dusty Window"
{
    Properties
    {
        _GlassTint("Glass Tint", Color) = (0.83,0.94,0.88,1)
        _Haze("Clean Glass Haze", Range(0,0.2)) = 0.015
        _F0("Front Reflectance", Range(0.02,0.12)) = 0.04
        _Smoothness("Clean Smoothness", Range(0,1)) = 0.97
        _DustMask("Dust Mask (black clean, white dirty)", 2D) = "black" {}
        [Toggle] _UseDustMask("Use Supplied Dust Mask", Float) = 0
        _DustAmount("Dust Amount", Range(0,1)) = 0.22
        _DustColor("Dust Color", Color) = (0.42,0.36,0.27,1)
        _DustOpacity("Dust Opacity", Range(0,1)) = 0.4
        _DustSmoothness("Dust Smoothness", Range(0,1)) = 0.4
        _DustSeed("Dust Pattern Seed", Float) = 0
        _PackedMasks("Surface Masks (R Dust / G Wear)", 2D) = "black" {}
        _SurfaceVariants("Shared Dust / Wear Variants", 2DArray) = "" {}
        [Toggle] _UseSurfaceVariants("Use Distinct Surface Variants", Float) = 0
        [HideInInspector] _SurfaceVariant("Surface Variant", Float) = 0
        [HideInInspector] _SurfaceMirror("Horizontal Surface Mirror", Float) = 0
        [HideInInspector] _BakedPositive("Local Baked Positive Side", Cube) = "" {}
        [HideInInspector] _BakedNegative("Local Baked Negative Side", Cube) = "" {}
        [HideInInspector] _UseLocalBaked("Use Regional Baked Cubes", Float) = 0
        [Toggle] _UsePackedMasks("Use Packed Surface Masks", Float) = 0
        _WearAmount("Light Surface Wear", Range(0,0.5)) = 0.12
        _TransmissionBlur("Dirty Glass Transmission Blur (1080p pixels)", Range(0,12)) = 5
        _TransmissionScatter("Dirt Transmission Scatter", Range(0,1)) = 0.3
        [HideInInspector] _PlanarAtlasRect("Atlas region", Vector) = (1,1,0,0)
        [HideInInspector] _TransmissionSamples("Transmission Samples", Float) = 4
        [HideInInspector] _TransmissionReady("Opaque Scene Available", Float) = 0
        [HideInInspector] _VariationEnabled("Per Pane Variation", Float) = 0
        [HideInInspector] _PaneU("Pane U", Vector) = (1,0,0,0)
        [HideInInspector] _PaneV("Pane V", Vector) = (0,1,0,0)
        [HideInInspector] _SurfaceRandom("Pattern Parameters", Vector) = (0,0,0,0)
        [HideInInspector] _WearCrop("Wear Crop", Vector) = (0.9,0.9,0.05,0.05)
        [HideInInspector] _DustMultiplier("Dust Variation", Float) = 1
        [HideInInspector] _WearMultiplier("Wear Variation", Float) = 1
        [HideInInspector] _PlanarTexture("Planar Reflection", 2D) = "black" {}
        [HideInInspector] _DynamicStrength("Dynamic layer strength", Range(0,1)) = 1
        [HideInInspector] _PlanarWeight("Planar Weight", Float) = 0
        [HideInInspector] _PlanarMaxMip("Planar Max Mip", Float) = 0
    }
    SubShader
    {
        // The per-pane dust coordinates use the original object-space vertices.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Name "Glass"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #define _SPECULAR_SETUP 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_DustMask); SAMPLER(sampler_DustMask);
            TEXTURE2D(_PackedMasks); SAMPLER(sampler_PackedMasks);
            TEXTURE2D_ARRAY(_SurfaceVariants); SAMPLER(sampler_SurfaceVariants);
            TEXTURECUBE(_BakedPositive); SAMPLER(sampler_BakedPositive);
            TEXTURECUBE(_BakedNegative); SAMPLER(sampler_BakedNegative);
            TEXTURE2D(_PlanarTexture); SAMPLER(sampler_PlanarTexture);
            float _GasGlassRenderingReflection;
            // Per-renderer values belong outside the material buffer.
            float4x4 _PlanarVP;
            CBUFFER_START(UnityPerMaterial)
                float4 _GlassTint, _DustColor, _DustMask_ST;
                float _Haze, _F0, _Smoothness, _UseDustMask, _DustAmount;
                float _DustOpacity, _DustSmoothness, _DustSeed;
                float4 _PlanarAtlasRect;
                float _TransmissionScatter;
                float _DynamicStrength;
                float _PlanarWeight, _PlanarMaxMip, _DynamicOverlay, _DynamicDebug;
                float4 _PaneU, _PaneV, _SurfaceRandom, _WearCrop;
                float _VariationEnabled, _UsePackedMasks, _WearAmount, _DustMultiplier, _WearMultiplier;
                float _TransmissionBlur, _TransmissionReady, _TransmissionSamples;
                float _UseSurfaceVariants, _SurfaceVariant, _SurfaceMirror;
                float _UseLocalBaked;
                float4 _BakedNormal, _BakedPositionPositive, _BakedPositionNegative;
                float4 _BakedMinPositive, _BakedMaxPositive, _BakedMinNegative, _BakedMaxNegative;
                float4 _BakedHDRPositive, _BakedHDRNegative;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1;
                float2 uv:TEXCOORD2;
                float fog:TEXCOORD3;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=_VariationEnabled>0.5 ? float2(dot(v.positionOS,_PaneU),dot(v.positionOS,_PaneV)) : v.uv;
                o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half Dust(float2 uv)
            {
                if (_UseDustMask > 0.5)
                    return saturate(SAMPLE_TEXTURE2D(_DustMask,sampler_DustMask,uv*_DustMask_ST.xy+_DustMask_ST.zw).r*_DustAmount);
                float2 p=uv+float2(_DustSeed,_DustSeed*0.73);
                float edge=1-smoothstep(0,0.12,min(min(uv.x,1-uv.x),min(uv.y,1-uv.y)));
                float bottom=1-smoothstep(0,0.22,uv.y);
                float dirt=(edge*0.65+bottom*0.35)*(0.35+0.65*Noise(p*17));
                dirt+=0.12*Noise(p*48)*Noise(p*7);
                dirt+=0.1*pow(Noise(float2(p.x*70,p.y*3)),8);
                return saturate(dirt*_DustAmount);
            }
            half2 SurfaceMasks(float2 uv)
            {
                if(_UsePackedMasks<0.5) return half2(Dust(uv),0);
                float2 seed=_VariationEnabled>0.5 ? _SurfaceRandom.xy : float2(_DustSeed,_DustSeed*0.73);
                float2 bounded=saturate(uv);
                bounded.x=lerp(bounded.x,1-bounded.x,_SurfaceMirror);
                // The perimeter stays anchored while details slide differently along each edge.
                float2 warp=float2(Noise(float2(bounded.y*13+seed.x,seed.y)),Noise(float2(bounded.x*11+seed.y,seed.x)))-0.5;
                float2 dustUV=saturate(bounded+warp*0.16*bounded*(1-bounded));
                half dust;
                if(_UseSurfaceVariants>0.5) dust=SAMPLE_TEXTURE2D_ARRAY(_SurfaceVariants,sampler_SurfaceVariants,dustUV,_SurfaceVariant).r;
                else dust=SAMPLE_TEXTURE2D(_PackedMasks,sampler_PackedMasks,dustUV).r;
                half distribution=lerp(0.55,1.25,Noise(bounded*float2(19,23)+seed));
                // Source masks are mostly dark: remap optical coverage before applying amount.
                // Preserve truly clean pixels while exposing grey accumulated dust.
                dust=pow(saturate(dust),0.5);
                dust=saturate(dust*distribution*_DustAmount*(_VariationEnabled>0.5 ? _DustMultiplier : 1)*2);
                float2 wearUV=bounded*_WearCrop.xy+_WearCrop.zw;
                half wear;
                if(_UseSurfaceVariants>0.5) wear=SAMPLE_TEXTURE2D_ARRAY(_SurfaceVariants,sampler_SurfaceVariants,wearUV,_SurfaceVariant).g;
                else wear=SAMPLE_TEXTURE2D(_PackedMasks,sampler_PackedMasks,wearUV).g;
                wear*=lerp(0.6,1.2,Noise(bounded*float2(11,17)+seed.yx));
                wear=saturate(sqrt(saturate(wear))*_WearAmount*(_VariationEnabled>0.5 ? _WearMultiplier : 1)*2);
                return half2(dust,wear);
            }
            half4 Frag(Varyings i):SV_Target
            {
                // Never reflect glass into glass or feed a previous reflection back into itself.
                clip(0.5-_GasGlassRenderingReflection);
                half3 n=normalize(i.normalWS), v=SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                n*=dot(n,v)<0 ? -1 : 1;
                half2 masks=SurfaceMasks(i.uv);
                half dust=masks.x,wear=masks.y;
                half contamination=saturate(dust+wear*1.5);
                half roughCoverage=sqrt(contamination);
                half smoothness=lerp(_Smoothness,_DustSmoothness,roughCoverage);
                half edge=1-saturate(dot(n,v));
                half edge2=edge*edge;
                half fresnel=_F0+(1-_F0)*edge2*edge2*edge;
                float2 screenUV=GetNormalizedScreenSpaceUV(i.positionCS);
                half3 environment;
                float3 ray=reflect(-v,n);
                if(_UseLocalBaked>0.5)
                {
                    half mip=PerceptualRoughnessToMipmapLevel(1-smoothness);
                    if(dot(_BakedNormal.xyz,v)>=0)
                    {
                        float3 direction=BoxProjectedCubemapDirection(ray,i.positionWS,_BakedPositionPositive,_BakedMinPositive,_BakedMaxPositive);
                        half4 encoded=SAMPLE_TEXTURECUBE_LOD(_BakedPositive,sampler_BakedPositive,direction,mip);
                        environment=DecodeHDREnvironment(encoded,_BakedHDRPositive);
                    }
                    else
                    {
                        float3 direction=BoxProjectedCubemapDirection(ray,i.positionWS,_BakedPositionNegative,_BakedMinNegative,_BakedMaxNegative);
                        half4 encoded=SAMPLE_TEXTURECUBE_LOD(_BakedNegative,sampler_BakedNegative,direction,mip);
                        environment=DecodeHDREnvironment(encoded,_BakedHDRNegative);
                    }
                }
                else if(_UseLocalBaked< -0.5) environment=0; // Unverified pane: never borrow an unrelated room.
                else environment=GlossyEnvironmentReflection(ray,i.positionWS,1-smoothness,1,screenUV);
                environment=MixFog(environment,i.fog);
                float4 projected=mul(_PlanarVP,float4(i.positionWS,1));
                float2 uv=projected.xy/max(projected.w,0.00001)*0.5+0.5;
                // GPU projection to a render target is Y-flipped on DirectX/Metal.
                // SubmitRenderRequest's sampled texture uses the opposite convention.
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y=1.0-uv.y;
                #endif
                float valid=step(0.00001,projected.w)*step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
                half3 reflection=environment;
                if (_PlanarWeight*valid>0.001)
                {
                    half mip=(1-smoothness)*_PlanarMaxMip;
                    // Extra roughness footprint makes the mask's reflection blur visible.
                    mip=min(_PlanarMaxMip,mip+roughCoverage*2);
                    half4 captured=SAMPLE_TEXTURE2D_LOD(_PlanarTexture,sampler_PlanarTexture,uv*_PlanarAtlasRect.xy+_PlanarAtlasRect.zw,mip);
                    half3 planar=captured.rgb;
                    if(_DynamicOverlay>0.5)
                    {
                        captured*=saturate(_DynamicStrength);
                        // Colour is premultiplied by filtered opaque coverage. Keep baked colour only in uncovered pixels.
                        planar=environment*(1-saturate(captured.a))+captured.rgb;
                        if(_DynamicDebug>0.5)planar=_DynamicDebug<1.5?captured.aaa:captured.rgb;
                    }
                    // Planar scene colour already contains scene fog.
                    reflection=lerp(environment,planar,saturate(_PlanarWeight*valid));
                }
                half3 dirtAlbedo=lerp(_GlassTint.rgb*0.15,_DustColor.rgb,saturate(dust*4));
                half opacity=saturate(_Haze+dust*_DustOpacity+wear*0.04);
                half3 diffuseLight=SampleSH(n);
                Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                diffuseLight+=main.color*main.distanceAttenuation*main.shadowAttenuation*saturate(dot(n,main.direction));
                half alpha=1;
                BRDFData brdf;
                InitializeBRDFData(half3(0,0,0),0,half3(_F0,_F0,_F0),smoothness,alpha,brdf);
                half3 highlights=LightingPhysicallyBased(brdf,main,n,v);
                InputData inputData=(InputData)0;
                inputData.positionWS=i.positionWS; inputData.normalizedScreenSpaceUV=screenUV;
                #if defined(_ADDITIONAL_LIGHTS)
                    #if USE_FORWARD_PLUS
                    UNITY_LOOP for (uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                    {
                        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                        Light l=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));
                        highlights+=LightingPhysicallyBased(brdf,l,n,v);
                        diffuseLight+=l.color*l.distanceAttenuation*l.shadowAttenuation*saturate(dot(n,l.direction));
                    }
                    #endif
                    uint count=GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light l=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));
                        highlights+=LightingPhysicallyBased(brdf,l,n,v);
                        diffuseLight+=l.color*l.distanceAttenuation*l.shadowAttenuation*saturate(dot(n,l.direction));
                    LIGHT_LOOP_END
                #endif
                half3 haze=MixFog(dirtAlbedo*diffuseLight,i.fog);
                highlights*=ComputeFogIntensity(i.fog);
                half a=fresnel+opacity*(1-fresnel);
                half3 colour=reflection*fresnel+haze*opacity*(1-fresnel)+highlights;
                if(_TransmissionReady>0.5 && _TransmissionSamples>0.5 && _TransmissionBlur>0.01 && contamination>0.001)
                {
                    // Shared URP opaque copy; clean glass keeps ordinary alpha blending.
                    float2 radius=float2(_TransmissionBlur/1920.0,_TransmissionBlur/1080.0)*roughCoverage;
                    half3 blurred;
                    [branch] if(_TransmissionSamples>3)
                        blurred=(SampleSceneColor(saturate(screenUV+radius*float2(-1,-1)))+
                                 SampleSceneColor(saturate(screenUV+radius*float2(1,-1)))+
                                 SampleSceneColor(saturate(screenUV+radius*float2(-1,1)))+
                                 SampleSceneColor(saturate(screenUV+radius*float2(1,1))))*0.25;
                    else
                    {
                        // Fixed symmetric positions avoid temporal noise while retaining a 2D footprint
                        // through the shared, bilinear half-resolution opaque texture.
                        float2 offset=radius*float2(0.92388,0.38268);
                        blurred=(SampleSceneColor(saturate(screenUV-offset))+
                                 SampleSceneColor(saturate(screenUV+offset)))*0.5;
                    }
                    half scatter=saturate(roughCoverage*_TransmissionScatter);
                    colour+=blurred*(1-a)*scatter;
                    a+=(1-a)*scatter;
                }
                return half4(colour,a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

