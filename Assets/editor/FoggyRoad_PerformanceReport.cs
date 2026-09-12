// Sahnedeki gerçek performans yükünü Console'a raporlar.
// Kullanım: Unity üst menü -> Tools -> Foggy Road -> Performans Raporu
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FoggyRoad_PerformanceReport
{
    [MenuItem("Tools/Foggy Road/Performans Raporu")]
    public static void Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("================ FOGGY ROAD PERFORMANS RAPORU ================");

        ReportCameras(sb);
        ReportTerrain(sb);
        ReportRenderers(sb);
        ReportLights(sb);

        sb.AppendLine("==============================================================");
        Debug.Log(sb.ToString());
    }

    static void ReportCameras(StringBuilder sb)
    {
        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var active = cams.Where(c => c.isActiveAndEnabled).ToArray();
        sb.AppendLine($"\n--- KAMERALAR --- (aktif: {active.Length} / toplam: {cams.Length})");
        foreach (var c in cams)
        {
            string durum = c.isActiveAndEnabled ? "AKTIF " : "kapali";
            string hedef = c.targetTexture != null ? $"-> RenderTexture({c.targetTexture.name})" : "-> EKRAN";
            sb.AppendLine($"  [{durum}] {c.name}  {hedef}  farClip={c.farClipPlane}");
        }
        if (active.Length > 1)
            sb.AppendLine($"  !! UYARI: {active.Length} aktif kamera var. Sahne her karede {active.Length} kez ciziliyor.");
    }

    static void ReportTerrain(StringBuilder sb)
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var td = t.terrainData;
            if (td == null) continue;
            sb.AppendLine($"\n--- TERRAIN: {t.name} --- (aktif: {t.isActiveAndEnabled})");
            sb.AppendLine($"  treeDistance={t.treeDistance}  billboardStart={t.treeBillboardDistance}  maxFullLOD={t.treeMaximumFullLODCount}");
            sb.AppendLine($"  detailObjectDistance={t.detailObjectDistance}  detailDensity={t.detailObjectDensity}");
            sb.AppendLine($"  heightmapPixelError={t.heightmapPixelError}  drawInstanced={t.drawInstanced}  castShadows={t.shadowCastingMode}");

            var instances = td.treeInstances;
            sb.AppendLine($"  >> TOPLAM AGAC (tree instance): {instances.Length}");

            var protos = td.treePrototypes;
            var sayim = new int[protos.Length];
            foreach (var inst in instances)
                if (inst.prototypeIndex >= 0 && inst.prototypeIndex < sayim.Length) sayim[inst.prototypeIndex]++;

            for (int i = 0; i < protos.Length; i++)
            {
                var pf = protos[i].prefab;
                string ad = pf != null ? pf.name : "<bos>";
                string yol = pf != null ? AssetDatabase.GetAssetPath(pf) : "-";
                int tri = pf != null ? MeshTriCount(pf) : 0;
                bool lod = pf != null && pf.GetComponentInChildren<LODGroup>() != null;
                long toplam = (long)tri * sayim[i];
                sb.AppendLine($"    [{i}] {ad,-28} adet={sayim[i],6}  tri/adet={tri,7}  LODGroup={(lod ? "VAR" : "YOK !!")}  ~toplam tri={toplam:N0}");
                sb.AppendLine($"         kaynak: {yol}");
            }

            // Detail (cim) katmanlari
            var dp = td.detailPrototypes;
            sb.AppendLine($"  >> Detail (cim/cali) katman sayisi: {dp.Length}  detailResolution={td.detailResolution}");
            for (int i = 0; i < dp.Length; i++)
            {
                string ad = dp[i].prototype != null ? dp[i].prototype.name : (dp[i].prototypeTexture != null ? dp[i].prototypeTexture.name + " (texture)" : "<bos>");
                sb.AppendLine($"    [{i}] {ad}  render={dp[i].renderMode}  density={dp[i].density}");
            }
        }
    }

    static void ReportRenderers(StringBuilder sb)
    {
        var rends = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        long toplamTri = 0;
        int golgeAtan = 0;
        var liste = new List<(string ad, int tri, string shader, bool golge, string yol)>();

        foreach (var r in rends)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            int tri = TriCount(mf.sharedMesh);
            toplamTri += tri;
            bool g = r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
            if (g) golgeAtan++;
            string sh = r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "<mat yok>";
            liste.Add((r.name, tri, sh, g, HierarchyPath(r.transform)));
        }

        sb.AppendLine($"\n--- SAHNE MESH'LERI (terrain agaclari HARIC) ---");
        sb.AppendLine($"  Aktif MeshRenderer sayisi : {rends.Length}");
        sb.AppendLine($"  Toplam ucgen              : {toplamTri:N0}");
        sb.AppendLine($"  Golge atan obje sayisi    : {golgeAtan}");

        sb.AppendLine($"\n  >> EN AGIR 20 OBJE:");
        foreach (var x in liste.OrderByDescending(a => a.tri).Take(20))
            sb.AppendLine($"    {x.tri,9:N0} tri | golge={(x.golge ? "VAR" : "yok")} | {x.shader} | {x.yol}");

        sb.AppendLine($"\n  >> AYNI ISIMDEN KAC TANE VAR (ilk 15):");
        foreach (var g in liste.GroupBy(a => a.ad).OrderByDescending(g => g.Count()).Take(15))
            sb.AppendLine($"    {g.Count(),6} adet x {g.Key}   (toplam {g.Sum(a => (long)a.tri):N0} tri)");

        sb.AppendLine($"\n  >> SHADER BASINA OBJE SAYISI:");
        foreach (var g in liste.GroupBy(a => a.shader).OrderByDescending(g => g.Count()))
            sb.AppendLine($"    {g.Count(),6} obje | {g.Sum(a => (long)a.tri),12:N0} tri | {g.Key}");
    }

    static void ReportLights(StringBuilder sb)
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        sb.AppendLine($"\n--- ISIKLAR --- (aktif: {lights.Length})");
        foreach (var l in lights)
            sb.AppendLine($"  {l.name}: tip={l.type} golge={l.shadows} mod={l.lightmapBakeType}");
    }

    static int TriCount(Mesh m)
    {
        int t = 0;
        for (int i = 0; i < m.subMeshCount; i++) t += (int)(m.GetIndexCount(i) / 3);
        return t;
    }

    static int MeshTriCount(GameObject go)
    {
        int t = 0;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) t += TriCount(mf.sharedMesh);
        foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null) t += TriCount(smr.sharedMesh);
        return t;
    }

    static string HierarchyPath(Transform t)
    {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
