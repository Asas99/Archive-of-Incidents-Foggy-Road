using UnityEngine;

// Keep the arithmetic in sync with FarGrassDistance.hlsl; no Unity native calls per instance.
public static class GrassDensitySampling
{
 public static float Density(SceneOptimizationProfile p,float distance)
 {
  float value=1;
  if(p.farGrassDistanceThinning)
  {
   float a=p.farDensityFullDistance,b=Mathf.Max(a+.001f,p.farDensityMidDistance),c=Mathf.Max(b+.001f,p.farDensityLowDistance);
   if(distance>b)value=Mathf.Lerp(p.farDensityMid,p.farDensityLow,Smooth(b,c,distance));
   else value=Mathf.Lerp(1,p.farDensityMid,Smooth(a,b,distance));
  }
  return Mathf.Clamp01(value*p.farGrassDensity);
 }
 public static float StableFraction(Vector3 origin)
 {
  unchecked{uint h=(uint)Mathf.FloorToInt(origin.x)*73856093u ^ (uint)Mathf.FloorToInt(origin.z)*19349663u;h^=h>>13;return (h&65535)/65535f;}
 }
 public static float DensityCoverage(SceneOptimizationProfile p,Vector3 origin,float distance)
 {
  float width=Mathf.Clamp(p.farDensityFadeHalfWidth,.001f,.1f);
  float rank=width+(1-2*width)*StableFraction(origin);
  return Smooth(rank-width,rank+width,Density(p,distance));
 }
 static float Smooth(float a,float b,float value){float t=Mathf.Clamp01((value-a)/(b-a));return t*t*(3-2*t);}
}
