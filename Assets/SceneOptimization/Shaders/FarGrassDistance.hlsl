#ifndef FOGGY_DISTANCE_CLIP_INCLUDED
#define FOGGY_DISTANCE_CLIP_INCLUDED
float4 _FoggyFarGrassRange;
float4 _FoggyDensityDistances;
float4 _FoggyDensityValues;
float FoggyDensityCoverage(float3 origin,float distanceToCamera)
{
 float density=1;
 if(_FoggyDensityDistances.w>.5)
 {
  float a=_FoggyDensityDistances.x,b=max(a+.001,_FoggyDensityDistances.y),c=max(b+.001,_FoggyDensityDistances.z);
  density=distanceToCamera>b?lerp(_FoggyDensityValues.y,_FoggyDensityValues.z,smoothstep(b,c,distanceToCamera)):lerp(1,_FoggyDensityValues.y,smoothstep(a,b,distanceToCamera));
 }
 density=saturate(density*_FoggyDensityValues.x);
 uint2 cell=(uint2)(int2)floor(origin.xz);uint h=cell.x*73856093u ^ cell.y*19349663u;h^=h>>13;
 float fraction=(h&65535u)/65535.0;
 float width=clamp(_FoggyDensityValues.w,.001,.1),rank=width+(1-2*width)*fraction;
 return smoothstep(rank-width,rank+width,density);
}
void FoggyDistanceClip(float2 pixel)
{
 float3 origin=TransformObjectToWorld(float3(0,0,0));
 float d=distance(origin,GetCameraPositionWS());
 // No near exclusion for the continuous base. Avoid smoothstep(0,0,d).
 float nearCoverage=_FoggyFarGrassRange.y>_FoggyFarGrassRange.x+.001?smoothstep(_FoggyFarGrassRange.x,_FoggyFarGrassRange.y,d):1;
 float coverage=nearCoverage*(1-smoothstep(_FoggyFarGrassRange.z,_FoggyFarGrassRange.w,d))*FoggyDensityCoverage(origin,d);
 float noise=frac(52.9829189*frac(dot(floor(pixel),float2(.06711056,.00583715))));
 clip(coverage-noise-.00001);
}
#endif
