using UnityEngine;

[PreferBinarySerialization]
public sealed class FarGrassCache : ScriptableObject
{
 [System.Serializable] public sealed class Patch
 {
  public Bounds bounds;
  public Matrix4x4[] matrices;
  public Vector3[] centers;
  public float[] radii;
 }
 public Mesh mesh;
 public Material material;
 public Patch[] patches=System.Array.Empty<Patch>();
 public Vector3 sourcePosition;
 public int instanceCount,sourceDetailHash;
}
