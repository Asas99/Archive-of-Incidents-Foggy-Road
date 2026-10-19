#ifndef FOGGY_DISTANCE_CLIP_INCLUDED
#define FOGGY_DISTANCE_CLIP_INCLUDED
float4 _FoggyOptimizationCamera;
float _FoggyOptimizationEnabled;
float4 _FoggySmallRange;
void FoggyDistanceClip(float2 pixel)
{
 if(_FoggyOptimizationEnabled<.5)return;
 float3 origin=TransformObjectToWorld(float3(0,0,0));
 float coverage=1-smoothstep(_FoggySmallRange.x,_FoggySmallRange.y,distance(origin,_FoggyOptimizationCamera.xyz));
 float noise=frac(52.9829189*frac(dot(floor(pixel),float2(.06711056,.00583715))));
 clip(coverage-noise-.00001);
}
#endif
