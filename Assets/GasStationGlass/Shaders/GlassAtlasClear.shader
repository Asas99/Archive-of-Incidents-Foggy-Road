Shader "Hidden/FoggyRoad/Glass Atlas Clear"
{
 SubShader {Tags {"RenderPipeline"="UniversalPipeline"}
 Pass {ZWrite On ZTest Always Cull Off Blend Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 Vert(uint id:SV_VertexID):SV_POSITION
 {
     float2 p=float2((id<<1)&2,id&2);
     return float4(p*2-1,UNITY_RAW_FAR_CLIP_VALUE,1);
 }
 half4 Frag():SV_TARGET{return 0;}
 ENDHLSL
 }}
}
