// UZAK AGAC RUZGAR OPTIMIZASYONU
//
// Terrain'in kullandigi agac prefablarinda LOD0 ve LOD1 ayni, tam rüzgarli
// materyalleri kullanir. LOD2 ise ekranda kucuk ve sis icinde kaldigi halde
// ayni vertex wind yolunu calistiriyordu.
//
// Bu arac yalnizca LOD2 renderer'larina materyal kopyasi baglar. Kopyada
// _WindQuality ve _WINDQUALITY kapatilir; geometri, doku, LOD mesafeleri,
// golgeler ve LOD0/LOD1 degismez. LOD3 billboard ise zaten hedeflenmez.
//
// Menu: Tools -> Foggy Road/Uzak Agac Ruzgari/...
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FoggyRoad_UzakAgacRuzgarLOD
{
    private const string NoWindSuffix = "_NoWindLOD2";

    [MenuItem("Tools/Foggy Road/Uzak Agac Ruzgari/1 - LOD2 Ruzgarini Kapat", false, 40)]
    public static void Uygula()
    {
        int prefabCount = 0;
        int rendererCount = 0;
        int materialCount = 0;

        foreach (string prefabPath in TerrainAgacPrefabYollari())
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            bool prefabChanged = false;

            try
            {
                LODGroup group = root.GetComponent<LODGroup>();
                if (group == null)
                    continue;

                LOD[] lods = group.GetLODs();
                const int targetLod = 2;
                if (lods.Length <= targetLod)
                    continue;

                // Son LOD bir billboard ise buna dokunma: billboard zaten wind vertex
                // animasyonu calistirmaz ve bu arac yalnizca mesh LOD2 icindir.
                if (group.lastLODBillboard && targetLod == lods.Length - 1)
                    continue;

                foreach (Renderer renderer in lods[targetLod].renderers)
                {
                    if (renderer == null)
                        continue;

                    Material[] materials = renderer.sharedMaterials;
                    bool rendererChanged = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material source = materials[i];
                        // Once generated, static copies must be normalized too. This
                        // prevents a previous partial run from leaving nested
                        // "_NoWindLOD2_NoWindLOD2" material names behind.
                        bool isStaticCopy = OriginalSourceMaterial(source) != source;
                        if (!RuzgarliAgacMateryali(source) && !isStaticCopy)
                            continue;

                        Material staticMaterial = GetOrCreateStaticCopy(source);
                        if (staticMaterial == null || materials[i] == staticMaterial)
                            continue;

                        materials[i] = staticMaterial;
                        rendererChanged = true;
                        materialCount++;
                    }

                    if (!rendererChanged)
                        continue;

                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    prefabChanged = true;
                    rendererCount++;
                }

                if (!prefabChanged)
                    continue;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                prefabCount++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        string report = "[FoggyRoad] Uzak agac ruzgari uygulandi. " +
                        "Prefab: " + prefabCount + ", LOD2 renderer: " + rendererCount +
                        ", materyal baglantisi: " + materialCount + ".\n" +
                        "LOD0/LOD1 tam rüzgarli; LOD2 static; billboard degismedi.";
        Debug.Log(report);
    }

    [MenuItem("Tools/Foggy Road/Uzak Agac Ruzgari/2 - Durumu Kontrol Et", false, 41)]
    public static void KontrolEt()
    {
        int checkedRenderers = 0;
        int staticRenderers = 0;
        List<string> missing = new List<string>();

        foreach (string prefabPath in TerrainAgacPrefabYollari())
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                LODGroup group = root.GetComponent<LODGroup>();
                if (group == null || group.lodCount <= 2)
                    continue;

                foreach (Renderer renderer in group.GetLODs()[2].renderers)
                {
                    if (renderer == null)
                        continue;

                    checkedRenderers++;
                    bool isStatic = true;
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (RuzgarliAgacMateryali(material))
                        {
                            isStatic = false;
                            missing.Add(Path.GetFileName(prefabPath) + " / " + renderer.name +
                                        " / " + material.name);
                        }
                    }

                    if (isStatic)
                        staticRenderers++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        string report = "[FoggyRoad] LOD2 kontrolu: " + staticRenderers + "/" +
                        checkedRenderers + " renderer static materyal kullaniyor.";
        if (missing.Count > 0)
            report += "\nEksik: " + string.Join("\n", missing);

        Debug.Log(report);
    }

    private static HashSet<string> TerrainAgacPrefabYollari()
    {
        HashSet<string> result = new HashSet<string>();
        foreach (Terrain terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (terrain.terrainData == null)
                continue;

            foreach (TreePrototype prototype in terrain.terrainData.treePrototypes)
            {
                if (prototype.prefab == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(prototype.prefab);
                if (!string.IsNullOrEmpty(path))
                    result.Add(path);
            }
        }

        return result;
    }

    private static bool RuzgarliAgacMateryali(Material material)
    {
        if (material == null || !material.HasProperty("_WindQuality"))
            return false;

        // ShaderGraph ile olusturulan optimize agac shader'inda wind toggle'i bu
        // property'dir. Eski SpeedTree materyallerinde de ayni ad kullanilir.
        if (material.GetFloat("_WindQuality") > 0.5f)
            return true;

        foreach (string keyword in material.shaderKeywords)
            if (keyword.StartsWith("_WINDQUALITY_"))
                return true;

        return false;
    }

    private static Material GetOrCreateStaticCopy(Material source)
    {
        // Eski bir denemeden kalmis static kopya LOD2'de ise, yeniden onun
        // kopyasini alma. Her zaman ilk (ruzgarli) materyale geri donup tek,
        // kanonik "_NoWindLOD2" varyantini kullan.
        source = OriginalSourceMaterial(source);
        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(sourcePath))
            return null;

        string targetPath = Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(sourcePath) + NoWindSuffix + ".mat").Replace('\\', '/');
        Material copy = AssetDatabase.LoadAssetAtPath<Material>(targetPath);

        if (copy == null)
        {
            copy = new Material(source);
            AssetDatabase.CreateAsset(copy, targetPath);
        }
        else
        {
            // Kaynak materyalde daha sonra doku/renk duzeltmesi yapilirsa bu araci
            // yeniden calistirmak static kopyayi da o degisikliklerle esler.
            EditorUtility.CopySerialized(source, copy);
        }

        copy.name = source.name + NoWindSuffix;
        if (copy.HasProperty("_WindQuality"))
            copy.SetFloat("_WindQuality", 0f);
        if (copy.HasProperty("_WINDQUALITY"))
        {
            // KeywordEnum'in 0 degeri "Fastest"tir; yani 0 burada "kapali"
            // demek degil. -1 herhangi bir wind enum keyword'u secmez.
            copy.SetFloat("_WINDQUALITY", -1f);
        }

        List<string> keywords = new List<string>();
        foreach (string keyword in copy.shaderKeywords)
            if (!keyword.StartsWith("_WINDQUALITY_"))
                keywords.Add(keyword);
        copy.shaderKeywords = keywords.ToArray();

        EditorUtility.SetDirty(copy);
        return copy;
    }

    private static Material OriginalSourceMaterial(Material material)
    {
        string path = AssetDatabase.GetAssetPath(material);
        if (string.IsNullOrEmpty(path))
            return material;

        string directory = Path.GetDirectoryName(path) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(path);
        bool changed = false;
        while (name.EndsWith(NoWindSuffix))
        {
            name = name.Substring(0, name.Length - NoWindSuffix.Length);
            changed = true;
        }

        if (!changed)
            return material;

        Material original = AssetDatabase.LoadAssetAtPath<Material>(
            Path.Combine(directory, name + ".mat").Replace('\\', '/'));
        return original != null ? original : material;
    }
}
#endif
