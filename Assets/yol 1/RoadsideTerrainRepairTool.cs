#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Rebuilds only the roadside corridor on a safe 2049px TerrainData copy.
/// Road heights are triangle-rasterized, so nearest-vertex/Voronoi wedges cannot form.
/// </summary>
public static class RoadsideTerrainRepairTool
{
    private const int Resolution = 2049;
    private const float Outer = 24f;
    private const float Shoulder = 0.85f;
    private const float Clearance = 0.10f;
    private const float DitchCenter = 2.35f;
    private const float DitchWidth = 3.4f;
    private const float DitchDepth = 0.32f;
    private const float MaxSlope = 38f;

    [MenuItem("Tools/Forest Road/Repair Roadside Terrain (Safe 2049)")]
    public static void RepairActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        Terrain terrain = null;
        GameObject road = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "yol2") road = root;
            Terrain candidate = root.GetComponentInChildren<Terrain>(true);
            if (candidate != null && candidate.name == "Terrain") terrain = candidate;
        }
        if (terrain == null || terrain.terrainData == null)
            throw new InvalidOperationException("Terrain named 'Terrain' was not found.");
        if (road == null)
            throw new InvalidOperationException("Road root named 'yol2' was not found.");

        TerrainData source = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        Vector3 size = source.size;
        string outputPath = AssetDatabase.GenerateUniqueAssetPath(
            "Assets/GeneratedTerrain/" + source.name + "_RoadsideRepaired_2049.asset");

        TerrainData copy = null;
        try
        {
            EditorUtility.DisplayProgressBar("Roadside Repair", "Resampling heightmap...", 0.05f);
            float[,] heights = Resample(source.GetHeights(0, 0, source.heightmapResolution, source.heightmapResolution));

            EditorUtility.DisplayProgressBar("Roadside Repair", "Rasterizing road triangles...", 0.18f);
            RasterRoad(road.GetComponentsInChildren<MeshFilter>(true), origin, size,
                out bool[,] roadMask, out float[,] roadY, out RectInt roadBounds);

            EditorUtility.DisplayProgressBar("Roadside Repair", "Building continuous road grade...", 0.43f);
            BuildFields(roadMask, roadY, size, roadBounds,
                out float[,] distance, out float[,] grade, out RectInt workBounds);

            int artifacts = CountArtifacts(heights, distance, size, workBounds);
            EditorUtility.DisplayProgressBar("Roadside Repair", "Shaping shoulder, ditch and slope...", 0.68f);
            ShapeCorridor(heights, roadMask, distance, grade, origin, size, workBounds);

            EditorUtility.DisplayProgressBar("Roadside Repair", "Creating safe TerrainData copy...", 0.86f);
            copy = Object.Instantiate(source);
            copy.name = source.name + "_RoadsideRepaired_2049";
            AssetDatabase.CreateAsset(copy, outputPath);
            copy.heightmapResolution = Resolution;
            copy.SetHeightsDelayLOD(0, 0, heights);
            copy.SyncHeightmap();
            ReprojectTrees(copy, heights);
            EditorUtility.SetDirty(copy);

            Undo.RecordObject(terrain, "Assign repaired TerrainData");
            terrain.terrainData = copy;
            TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Assign repaired TerrainData");
                collider.terrainData = copy;
                EditorUtility.SetDirty(collider);
            }
            terrain.Flush();
            EditorUtility.SetDirty(terrain);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            SceneView.RepaintAll();
            Debug.Log($"[RoadsideTerrainRepair] Complete path='{outputPath}', resolution={source.heightmapResolution}->2049, detectedArtifacts={artifacts}.");
        }
        catch
        {
            if (copy != null && terrain.terrainData != copy && !string.IsNullOrEmpty(outputPath))
                AssetDatabase.DeleteAsset(outputPath);
            throw;
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private static float[,] Resample(float[,] source)
    {
        int old = source.GetLength(0);
        float[,] result = new float[Resolution, Resolution];
        float scale = (old - 1f) / (Resolution - 1f);
        for (int z = 0; z < Resolution; z++)
        {
            float fz = z * scale; int z0 = (int)fz; int z1 = Mathf.Min(old - 1, z0 + 1); float tz = fz - z0;
            for (int x = 0; x < Resolution; x++)
            {
                float fx = x * scale; int x0 = (int)fx; int x1 = Mathf.Min(old - 1, x0 + 1); float tx = fx - x0;
                result[z, x] = Mathf.Lerp(
                    Mathf.Lerp(source[z0, x0], source[z0, x1], tx),
                    Mathf.Lerp(source[z1, x0], source[z1, x1], tx), tz);
            }
        }
        return result;
    }

    private static void RasterRoad(MeshFilter[] filters, Vector3 origin, Vector3 size,
        out bool[,] mask, out float[,] height, out RectInt bounds)
    {
        mask = new bool[Resolution, Resolution];
        height = new float[Resolution, Resolution];
        for (int z = 0; z < Resolution; z++) for (int x = 0; x < Resolution; x++) height[z, x] = float.NegativeInfinity;
        int minX = Resolution, minZ = Resolution, maxX = -1, maxZ = -1, accepted = 0;

        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null) continue;
            Vector3[] vertices = filter.sharedMesh.vertices;
            int[] triangles = filter.sharedMesh.triangles;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 a = filter.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 b = filter.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 c = filter.transform.TransformPoint(vertices[triangles[i + 2]]);
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-6f || n.normalized.y < 0.30f) continue;
                Vector3 ga = Grid(a, origin, size), gb = Grid(b, origin, size), gc = Grid(c, origin, size);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(ga.x, Mathf.Min(gb.x, gc.x))) - 1, 0, Resolution - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(ga.x, Mathf.Max(gb.x, gc.x))) + 1, 0, Resolution - 1);
                int z0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(ga.z, Mathf.Min(gb.z, gc.z))) - 1, 0, Resolution - 1);
                int z1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(ga.z, Mathf.Max(gb.z, gc.z))) + 1, 0, Resolution - 1);
                float den = (gb.z - gc.z) * (ga.x - gc.x) + (gc.x - gb.x) * (ga.z - gc.z);
                if (Mathf.Abs(den) < 1e-6f) continue;
                bool wrote = false;
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    float wa = ((gb.z - gc.z) * (x - gc.x) + (gc.x - gb.x) * (z - gc.z)) / den;
                    float wb = ((gc.z - ga.z) * (x - gc.x) + (ga.x - gc.x) * (z - gc.z)) / den;
                    float wc = 1f - wa - wb;
                    if (wa < -0.08f || wb < -0.08f || wc < -0.08f) continue;
                    mask[z, x] = true;
                    height[z, x] = Mathf.Max(height[z, x], wa * a.y + wb * b.y + wc * c.y);
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
                    wrote = true;
                }
                if (wrote) accepted++;
            }
        }
        if (accepted == 0) throw new InvalidOperationException("No driveable road triangles overlap the Terrain.");
        bounds = new RectInt(minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
    }

    private static Vector3 Grid(Vector3 p, Vector3 origin, Vector3 size) => new Vector3(
        (p.x - origin.x) / size.x * (Resolution - 1), p.y,
        (p.z - origin.z) / size.z * (Resolution - 1));

    private static void BuildFields(bool[,] mask, float[,] roadY, Vector3 size, RectInt roadBounds,
        out float[,] distance, out float[,] grade, out RectInt work)
    {
        float cellX = size.x / (Resolution - 1f), cellZ = size.z / (Resolution - 1f);
        work = Expand(roadBounds, Mathf.CeilToInt((Outer + 8f) / cellX), Mathf.CeilToInt((Outer + 8f) / cellZ));
        int[,] nx = new int[Resolution, Resolution], nz = new int[Resolution, Resolution];
        distance = new float[Resolution, Resolution]; grade = new float[Resolution, Resolution];
        for (int z = work.yMin; z < work.yMax; z++) for (int x = work.xMin; x < work.xMax; x++)
        { nx[z, x] = mask[z, x] ? x : -1; nz[z, x] = mask[z, x] ? z : -1; }
        for (int pass = 0; pass < 2; pass++)
        {
            for (int z = work.yMin; z < work.yMax; z++) for (int x = work.xMin; x < work.xMax; x++)
            { Relax(nx,nz,x,z,x-1,z,cellX,cellZ,work); Relax(nx,nz,x,z,x,z-1,cellX,cellZ,work); Relax(nx,nz,x,z,x-1,z-1,cellX,cellZ,work); Relax(nx,nz,x,z,x+1,z-1,cellX,cellZ,work); }
            for (int z = work.yMax - 1; z >= work.yMin; z--) for (int x = work.xMax - 1; x >= work.xMin; x--)
            { Relax(nx,nz,x,z,x+1,z,cellX,cellZ,work); Relax(nx,nz,x,z,x,z+1,cellX,cellZ,work); Relax(nx,nz,x,z,x+1,z+1,cellX,cellZ,work); Relax(nx,nz,x,z,x-1,z+1,cellX,cellZ,work); }
        }
        for (int z = work.yMin; z < work.yMax; z++) for (int x = work.xMin; x < work.xMax; x++)
        {
            int sx = nx[z,x], sz = nz[z,x];
            if (sx < 0) { distance[z,x] = float.PositiveInfinity; continue; }
            float dx=(x-sx)*cellX, dz=(z-sz)*cellZ; distance[z,x]=Mathf.Sqrt(dx*dx+dz*dz); grade[z,x]=roadY[sz,sx];
        }
        float[,] next=(float[,])grade.Clone(); float wx=1f/(cellX*cellX), wz=1f/(cellZ*cellZ), div=2f*(wx+wz);
        for(int pass=0;pass<18;pass++)
        {
            for(int z=work.yMin;z<work.yMax;z++) for(int x=work.xMin;x<work.xMax;x++)
            {
                if(mask[z,x]||distance[z,x]>Outer+5f){next[z,x]=grade[z,x];continue;}
                int l=Mathf.Max(work.xMin,x-1),r=Mathf.Min(work.xMax-1,x+1),d=Mathf.Max(work.yMin,z-1),u=Mathf.Min(work.yMax-1,z+1);
                next[z,x]=((grade[z,l]+grade[z,r])*wx+(grade[d,x]+grade[u,x])*wz)/div;
            }
            float[,] swap=grade;grade=next;next=swap;
        }
    }

    private static void Relax(int[,] nx,int[,] nz,int x,int z,int cx,int cz,float sx,float sz,RectInt b)
    {
        if(cx<b.xMin||cx>=b.xMax||cz<b.yMin||cz>=b.yMax||nx[cz,cx]<0)return;
        int px=nx[cz,cx],pz=nz[cz,cx]; float dx=(x-px)*sx,dz=(z-pz)*sz,cd=dx*dx+dz*dz;
        if(nx[z,x]>=0){dx=(x-nx[z,x])*sx;dz=(z-nz[z,x])*sz;if(dx*dx+dz*dz<=cd)return;}
        nx[z,x]=px;nz[z,x]=pz;
    }

    private static void ShapeCorridor(float[,] h,bool[,] mask,float[,] dist,float[,] grade,Vector3 origin,Vector3 size,RectInt work)
    {
        float maxGrade=Mathf.Tan(MaxSlope*Mathf.Deg2Rad);
        for(int z=work.yMin;z<work.yMax;z++) for(int x=work.xMin;x<work.xMax;x++)
        {
            float d=dist[z,x]; if(float.IsInfinity(d)||d>Outer)continue;
            float road=grade[z,x], natural=origin.y+h[z,x]*size.y;
            if(mask[z,x]){h[z,x]=Mathf.Clamp01((road-Clearance-origin.y)/size.y);continue;}
            float shoulderDrop=0.025f*Mathf.Min(d,Shoulder);
            float q=(d-DitchCenter)/Mathf.Max(0.25f,DitchWidth*0.36f);
            float profile=road-Clearance-shoulderDrop-DitchDepth*Mathf.Exp(-0.5f*q*q);
            float run=Mathf.Max(0f,d-Shoulder),limit=maxGrade*run;
            float constrained=Mathf.Clamp(natural,profile-limit,profile+limit);
            float target=Mathf.Lerp(profile,constrained,Smooth(Mathf.InverseLerp(4.25f,Outer,d)));
            target=Mathf.Lerp(target,natural,Smooth(Mathf.InverseLerp(Outer-4f,Outer,d)));
            h[z,x]=Mathf.Clamp01((target-origin.y)/size.y);
        }
        float[,] source=(float[,])h.Clone();
        for(int z=Mathf.Max(1,work.yMin);z<Mathf.Min(Resolution-1,work.yMax);z++) for(int x=Mathf.Max(1,work.xMin);x<Mathf.Min(Resolution-1,work.xMax);x++)
        {
            float d=dist[z,x]; if(mask[z,x]||d<=Shoulder||d>=Outer-1f)continue;
            float average=(source[z,x]*4f+source[z,x-1]+source[z,x+1]+source[z-1,x]+source[z+1,x])/8f;
            h[z,x]=Mathf.Lerp(source[z,x],average,0.28f*(1f-Smooth(d/Outer)));
        }
    }

    private static int CountArtifacts(float[,] h,float[,] dist,Vector3 size,RectInt work)
    {
        int count=0;
        for(int z=Mathf.Max(1,work.yMin);z<Mathf.Min(Resolution-1,work.yMax);z++) for(int x=Mathf.Max(1,work.xMin);x<Mathf.Min(Resolution-1,work.xMax);x++)
        {
            if(dist[z,x]<0.25f||dist[z,x]>Outer)continue;
            float center=h[z,x]*size.y,average=(h[z,x-1]+h[z,x+1]+h[z-1,x]+h[z+1,x])*0.25f*size.y;
            if(Mathf.Abs(center-average)>0.48f)count++;
        }
        return count;
    }

    private static void ReprojectTrees(TerrainData data,float[,] heights)
    {
        TreeInstance[] trees=data.treeInstances;
        for(int i=0;i<trees.Length;i++)
        {
            TreeInstance tree=trees[i]; float x=tree.position.x*(Resolution-1),z=tree.position.z*(Resolution-1);
            int x0=Mathf.Clamp((int)x,0,Resolution-1),z0=Mathf.Clamp((int)z,0,Resolution-1),x1=Mathf.Min(Resolution-1,x0+1),z1=Mathf.Min(Resolution-1,z0+1);
            float y=Mathf.Lerp(Mathf.Lerp(heights[z0,x0],heights[z0,x1],x-x0),Mathf.Lerp(heights[z1,x0],heights[z1,x1],x-x0),z-z0);
            tree.position=new Vector3(tree.position.x,y,tree.position.z);trees[i]=tree;
        }
        data.treeInstances=trees;
    }

    private static RectInt Expand(RectInt b,int mx,int mz)
    {
        int x0=Mathf.Max(0,b.xMin-mx),z0=Mathf.Max(0,b.yMin-mz),x1=Mathf.Min(Resolution,b.xMax+mx),z1=Mathf.Min(Resolution,b.yMax+mz);
        return new RectInt(x0,z0,x1-x0,z1-z0);
    }

    private static float Smooth(float v){float t=Mathf.Clamp01(v);return t*t*t*(t*(t*6f-15f)+10f);}
}
#endif
