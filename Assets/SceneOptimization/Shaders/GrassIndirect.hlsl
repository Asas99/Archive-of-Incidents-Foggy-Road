#ifndef FOGGY_GRASS_INDIRECT_INCLUDED
#define FOGGY_GRASS_INDIRECT_INCLUDED
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
struct FoggyGrassInstance {float4x4 objectToWorld;float4x4 inverseMatrix;float4 bounds;};
StructuredBuffer<FoggyGrassInstance> _GrassInstances;
StructuredBuffer<uint> _VisibleGrass;
#endif
void FoggySetupGrass()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
 FoggyGrassInstance instance=_GrassInstances[_VisibleGrass[unity_InstanceID]];
 unity_ObjectToWorld=instance.objectToWorld;
 unity_WorldToObject=instance.inverseMatrix;
#endif
}
#endif
