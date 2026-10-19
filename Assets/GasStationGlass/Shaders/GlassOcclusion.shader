Shader "Hidden/FoggyRoad/Glass Occlusion"
{
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline"}
  Pass
  {
   ZWrite On ZTest LEqual Cull Off ColorMask 0
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4x4 _GlassOcclusionMVP;
   float4 Vert(float4 positionOS:POSITION):SV_POSITION{return mul(_GlassOcclusionMVP,float4(positionOS.xyz,1));}
   half4 Frag():SV_TARGET{return 0;}
   ENDHLSL
  }
  Pass
  {
   ZWrite On ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4x4 _GlassOcclusionMVP;
   float4 Vert(float4 positionOS:POSITION):SV_POSITION{return mul(_GlassOcclusionMVP,float4(positionOS.xyz,1));}
   half4 Frag(float4 positionCS:SV_POSITION):SV_TARGET{return half4(Linear01Depth(positionCS.z,_ZBufferParams).xxx,1);}
   ENDHLSL
  }
 }
}

