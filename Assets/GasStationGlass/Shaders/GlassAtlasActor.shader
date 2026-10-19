Shader "Hidden/FoggyRoad/Glass Atlas Actor"
{
 Properties
 {
  _BaseMap("Base",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
  _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=1
  _MetallicGlossMap("Metallic",2D)="white"{} _Metallic("Metallic",Float)=0 _Smoothness("Smoothness",Float)=.5
  _EmissionMap("Emission",2D)="white"{} _EmissionColor("Emission tint",Color)=(0,0,0,0)
  _UseNormal("Use normal",Float)=0 _UseMetallic("Use metallic",Float)=0 _AlphaClip("Clip",Float)=0 _Cutoff("Cutoff",Float)=.5 _EmissionOn("Emission",Float)=0 _AtlasUnlit("Unlit",Float)=0
 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline"}
 Pass {Name "Lightweight reflected actor" ZWrite On ZTest LEqual Cull Off Blend Off
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #pragma multi_compile_local _ _GLASS_NEAR_SIMPLE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
 TEXTURE2D(_MetallicGlossMap);SAMPLER(sampler_MetallicGlossMap);
 TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST;half4 _BaseColor,_EmissionColor;half _Metallic,_Smoothness,_BumpScale,_UseNormal,_UseMetallic,_AlphaClip,_Cutoff,_EmissionOn,_AtlasUnlit;
 CBUFFER_END
 float4x4 _AtlasVP,_AtlasModel,_AtlasNormal;
 float _AtlasActorFade,_AtlasShadowReady,_AtlasLocalLights;int _AtlasCascadeCount;
 float3 _AtlasEye,_AtlasSunDirection;half3 _AtlasSunColour;
 float4 _AtlasLightPositions[4],_AtlasLightColours[4],_AtlasLightDirections[4];
 struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
 struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half4 tangent:TEXCOORD2;float2 uv:TEXCOORD3;half fog:TEXCOORD4;};
 Varyings Vert(Attributes i)
 {
  Varyings o;o.world=mul(_AtlasModel,float4(i.positionOS.xyz,1)).xyz;o.positionCS=mul(_AtlasVP,float4(o.world,1));
  o.normal=normalize(mul((float3x3)_AtlasNormal,i.normalOS));o.tangent=half4(normalize(mul((float3x3)_AtlasModel,i.tangentOS.xyz)),i.tangentOS.w);
  o.uv=i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;o.fog=ComputeFogFactor(o.positionCS.z);return o;
 }
 half3 Lit(BRDFData brdf,half3 n,half3 v,half3 direction,half3 colour,float attenuation)
 {
  Light l=(Light)0;l.direction=direction;l.color=colour;l.distanceAttenuation=attenuation;l.shadowAttenuation=1;
  return LightingPhysicallyBased(brdf,l,n,v);
 }
 half AtlasSunShadow(float3 world)
 {
  if(_AtlasShadowReady<.5)return 1;
  uint cascade=_AtlasCascadeCount>1?(uint)ComputeCascadeIndex(world):0;
  if(cascade>=(uint)_AtlasCascadeCount)return 1;
  float3 coord=mul(_MainLightWorldToShadow[cascade],float4(world,1)).xyz;
  if(any(coord.xy<0)||any(coord.xy>1)||coord.z<0||coord.z>1)return 1;
  // World-space raw shadow atlas: never sample the main camera's screen-space shadow.
  half attenuation=SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture,sampler_LinearClampCompare,coord);
  return lerp(1,attenuation,GetMainLightShadowParams().x);
 }
 half4 Frag(Varyings i):SV_TARGET
 {
  half4 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
  if(_AlphaClip>.5)clip(base.a-_Cutoff);
  half3 n=normalize(i.normal),v=normalize(_AtlasEye-i.world);
  #if !defined(_GLASS_NEAR_SIMPLE)
  if(_UseNormal>.5){half3 t=normalize(i.tangent.xyz);half3 b=cross(n,t)*i.tangent.w;half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);n=normalize(mul(normalTS,half3x3(t,b,n)));}
  #endif
  if(dot(n,v)<0)n=-n;
  half sunShadow=AtlasSunShadow(i.world);
  #if defined(_GLASS_NEAR_SIMPLE)
  half3 colour=base.rgb*(SampleSH(n)+_AtlasSunColour*saturate(dot(n,normalize(_AtlasSunDirection)))*sunShadow);
  #else
  half metallic=_Metallic,smoothness=_Smoothness;
  if(_UseMetallic>.5){half4 mg=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);metallic*=mg.r;smoothness*=mg.a;}
  BRDFData brdf;half alpha=1;InitializeBRDFData(base.rgb,metallic,half3(.04,.04,.04),smoothness,alpha,brdf);
  half3 colour=SampleSH(n)*brdf.diffuse+Lit(brdf,n,v,normalize(_AtlasSunDirection),_AtlasSunColour,sunShadow);
  #endif
  [branch]if(_AtlasLocalLights>.5)
  [unroll]for(int k=0;k<4;k++)
  {
   float3 delta=_AtlasLightPositions[k].xyz-i.world;float d2=max(dot(delta,delta),.0001);float range=max(_AtlasLightPositions[k].w,.001);
   float fade=saturate(1-d2/(range*range));float attenuation=fade*fade/max(1,d2);float3 direction=delta*rsqrt(d2);
   if(_AtlasLightColours[k].w>=0)attenuation*=smoothstep(_AtlasLightColours[k].w,max(_AtlasLightColours[k].w+.001,_AtlasLightDirections[k].w),dot(-direction,_AtlasLightDirections[k].xyz));
   #if defined(_GLASS_NEAR_SIMPLE)
   colour+=base.rgb*_AtlasLightColours[k].rgb*attenuation*saturate(dot(n,direction));
   #else
   colour+=Lit(brdf,n,v,direction,_AtlasLightColours[k].rgb,attenuation);
   #endif
  }
  if(_AtlasUnlit>.5)colour=base.rgb;
  if(_EmissionOn>.5)colour+=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb;
  half coverage=saturate(_AtlasActorFade);return half4(MixFog(colour,i.fog)*coverage,coverage);
 }
 ENDHLSL
 }}
}
