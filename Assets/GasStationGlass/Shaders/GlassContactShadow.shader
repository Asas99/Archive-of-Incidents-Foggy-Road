Shader "Hidden/FoggyRoad/Glass Contact Shadow"
{
 SubShader {Tags {"RenderPipeline"="UniversalPipeline"}
 Pass {ZWrite Off ZTest LEqual Cull Off Blend One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4x4 _GlassOcclusionMVP;float _AtlasContactOpacity;
 struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 V Vert(float4 position:POSITION,float2 uv:TEXCOORD0){V o;o.positionCS=mul(_GlassOcclusionMVP,float4(position.xyz,1));o.uv=uv*2-1;return o;}
 half4 Frag(V i):SV_TARGET{float r=dot(i.uv,i.uv);half a=saturate(_AtlasContactOpacity)*(1-smoothstep(.15,1,r));return half4(0,0,0,a);}
 ENDHLSL
 }}
}
