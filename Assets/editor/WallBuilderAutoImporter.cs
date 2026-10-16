
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WallBuilderAutoImporter
{
    [Serializable]
    private class MaterialEntry
    {
        public string name;
        public string baseMap;
        public string normalMap;
        public float roughness;
        public float metallic;
        public float[] baseColor;
    }

    [Serializable]
    private class Manifest
    {
        public string format;
        public int version;
        public string fbx;
        public string prefab;
        public MaterialEntry[] materials;
    }

    static WallBuilderAutoImporter()
    {
        EditorApplication.delayCall += ProcessAll;
    }

    private class Watcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Any(p =>
                p.EndsWith("WallExportManifest.json", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("_Normal.png", StringComparison.OrdinalIgnoreCase)))
            {
                EditorApplication.delayCall += ProcessAll;
            }
        }
    }

    private static string JoinAsset(string root, string relative)
    {
        return (root.TrimEnd('/') + "/" + relative.Replace("\\", "/")).Replace("//", "/");
    }

    private static string NormalizeMaterialName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        string n = name;

        // Unity/FBX may append "(Instance)", numeric suffixes, or whitespace.
        n = n.Replace(" (Instance)", "").Trim();

        // Remove common FBX duplicate suffixes: .001, .002...
        int dot = n.LastIndexOf('.');
        if (dot > 0 && dot < n.Length - 1)
        {
            string tail = n.Substring(dot + 1);
            int dummy;
            if (int.TryParse(tail, out dummy))
                n = n.Substring(0, dot);
        }

        return n;
    }

    private static Shader FindLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null) return shader;

        shader = Shader.Find("HDRP/Lit");
        if (shader != null) return shader;

        return Shader.Find("Standard");
    }

    private static void ConfigureNormal(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool changed = false;

        if (importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            changed = true;
        }

        if (importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static Material MakeMaterial(string path, MaterialEntry e, string root)
    {
        Shader shader = FindLitShader();
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (shader != null && mat.shader != shader)
        {
            mat.shader = shader;
        }

        Texture2D baseTex = null;
        Texture2D normalTex = null;

        if (!string.IsNullOrEmpty(e.baseMap))
            baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(JoinAsset(root, e.baseMap));

        if (!string.IsNullOrEmpty(e.normalMap))
        {
            string np = JoinAsset(root, e.normalMap);
            ConfigureNormal(np);
            normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(np);
        }

        Color color = Color.white;
        if (e.baseColor != null && e.baseColor.Length >= 4)
            color = new Color(e.baseColor[0], e.baseColor[1], e.baseColor[2], e.baseColor[3]);

        if (mat.HasProperty("_BaseMap") && baseTex != null)
            mat.SetTexture("_BaseMap", baseTex);

        if (mat.HasProperty("_MainTex") && baseTex != null)
            mat.SetTexture("_MainTex", baseTex);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (mat.HasProperty("_BumpMap") && normalTex != null)
        {
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", e.metallic);

        float smoothness = Mathf.Clamp01(1.0f - e.roughness);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", smoothness);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ProcessManifest(string manifestPath)
    {
        TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);
        if (text == null) return;

        Manifest manifest;
        try
        {
            manifest = JsonUtility.FromJson<Manifest>(text.text);
        }
        catch
        {
            return;
        }

        if (manifest == null || manifest.format != "WallBuilderUnityExport")
            return;

        string root = Path.GetDirectoryName(manifestPath).Replace("\\", "/");
        string matsFolder = root + "/Materials";

        if (!AssetDatabase.IsValidFolder(matsFolder))
            AssetDatabase.CreateFolder(root, "Materials");

        // IMPORTANT:
        // Never assign materials by array/index order.
        // FBX/Unity can reorder material slots during import.
        // We create and remap by the ORIGINAL BLENDER MATERIAL NAME.
        Dictionary<string, Material> materialByName =
            new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        foreach (MaterialEntry e in manifest.materials)
        {
            string safeName = e.name;
            foreach (char c in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(c, '_');

            string matPath = matsFolder + "/" + safeName + ".mat";
            Material created = MakeMaterial(matPath, e, root);

            materialByName[NormalizeMaterialName(e.name)] = created;
        }

        string fbxPath = JoinAsset(root, manifest.fbx);
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx == null) return;

        GameObject instance = PrefabUtility.InstantiatePrefab(fbx) as GameObject;
        if (instance == null) return;

        try
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Material[] importedSlots = renderer.sharedMaterials;
                Material[] replacement = new Material[importedSlots.Length];

                for (int i = 0; i < importedSlots.Length; i++)
                {
                    Material imported = importedSlots[i];

                    if (imported == null)
                    {
                        replacement[i] = null;
                        continue;
                    }

                    string importedName = NormalizeMaterialName(imported.name);

                    Material matched;
                    if (materialByName.TryGetValue(importedName, out matched))
                    {
                        replacement[i] = matched;
                        continue;
                    }

                    // Fallback: compare by known face suffix.
                    string low = importedName.ToLowerInvariant();
                    string wantedSuffix = null;

                    if (low.Contains("_front")) wantedSuffix = "_front";
                    else if (low.Contains("_back")) wantedSuffix = "_back";
                    else if (low.Contains("_left")) wantedSuffix = "_left";
                    else if (low.Contains("_right")) wantedSuffix = "_right";
                    else if (low.Contains("_topbottom")) wantedSuffix = "_topbottom";

                    Material suffixMatch = null;
                    if (wantedSuffix != null)
                    {
                        foreach (var kv in materialByName)
                        {
                            if (kv.Key.ToLowerInvariant().Contains(wantedSuffix))
                            {
                                suffixMatch = kv.Value;
                                break;
                            }
                        }
                    }

                    // Final fallback keeps the imported FBX slot material instead of
                    // placing a wrong face material on the mesh.
                    replacement[i] = suffixMatch != null ? suffixMatch : imported;
                }

                renderer.sharedMaterials = replacement;
            }

            string prefabPath = JoinAsset(root, manifest.prefab);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }

        AssetDatabase.SaveAssets();
    }

    public static void ProcessAll()
    {
        string[] guids = AssetDatabase.FindAssets("WallExportManifest t:TextAsset");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("WallExportManifest.json", StringComparison.OrdinalIgnoreCase))
                ProcessManifest(path);
        }
    }
}
#endif
