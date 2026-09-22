using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public sealed class FoggyRoadBlenderCarImporter : AssetPostprocessor
{
    private const string ManifestFileName = "CarExportManifest.json";

    private static readonly HashSet<string> PendingManifests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static FoggyRoadBlenderCarImporter()
    {
        EditorApplication.delayCall += () =>
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportAllBlenderCars();
        };
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (string.Equals(Path.GetFileName(assetPath), ManifestFileName, StringComparison.OrdinalIgnoreCase))
                PendingManifests.Add(assetPath);
        }

        if (PendingManifests.Count == 0)
            return;

        EditorApplication.delayCall += ProcessPendingManifests;
    }

    [MenuItem("Tools/Foggy Road/Import Blender Car Exports")]
    public static void ImportAllBlenderCars()
    {
        string[] guids = AssetDatabase.FindAssets("CarExportManifest", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path).Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase))
                PendingManifests.Add(path);
        }

        ProcessPendingManifests();
    }

    private static void ProcessPendingManifests()
    {
        if (PendingManifests.Count == 0)
            return;

        string[] manifests = new string[PendingManifests.Count];
        PendingManifests.CopyTo(manifests);
        PendingManifests.Clear();

        foreach (string manifestPath in manifests)
        {
            try
            {
                ApplyManifest(manifestPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Foggy Road Blender export import failed for {manifestPath}: {exception}");
            }
        }
    }

    private static void ApplyManifest(string manifestAssetPath)
    {
        string manifestAbsolutePath = ToAbsolutePath(manifestAssetPath);
        if (!File.Exists(manifestAbsolutePath))
            return;

        CarExportManifest manifest = JsonUtility.FromJson<CarExportManifest>(File.ReadAllText(manifestAbsolutePath));
        if (manifest == null || !string.Equals(manifest.format, "FoggyRoadCarExport", StringComparison.Ordinal))
        {
            Debug.LogWarning($"Skipped unsupported Blender manifest: {manifestAssetPath}");
            return;
        }

        string manifestDirectory = Path.GetDirectoryName(manifestAssetPath)?.Replace('\\', '/') ?? "Assets";
        string fbxAssetPath = CombineAssetPath(manifestDirectory, manifest.fbx);
        AssetDatabase.ImportAsset(fbxAssetPath, ImportAssetOptions.ForceSynchronousImport);

        string materialFolder = CombineAssetPath(manifestDirectory, "Materials");
        EnsureAssetFolder(materialFolder);
        Dictionary<string, Material> materials = CreateMaterials(manifest, manifestDirectory, materialFolder);
        AssetDatabase.SaveAssets();

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
        if (model == null)
        {
            Debug.LogWarning($"Blender FBX was not found after import: {fbxAssetPath}");
            return;
        }

        string prefabName = string.IsNullOrEmpty(manifest.rootName) ? Path.GetFileNameWithoutExtension(manifest.fbx) : manifest.rootName;
        string prefabPath = CombineAssetPath(manifestDirectory, prefabName + ".prefab");
        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null)
        {
            Debug.LogWarning($"Could not instantiate Blender FBX for prefab creation: {fbxAssetPath}");
            return;
        }

        try
        {
            ApplyMaterials(instance, manifest, materials);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }

        Debug.Log($"Blender car imported: {prefabPath} ({materials.Count} materials)");
    }

    private static Dictionary<string, Material> CreateMaterials(
        CarExportManifest manifest,
        string manifestDirectory,
        string materialFolder)
    {
        Dictionary<string, Material> result = new Dictionary<string, Material>(StringComparer.Ordinal);
        if (manifest.materials == null)
            return result;

        foreach (CarExportMaterial data in manifest.materials)
        {
            if (data == null || string.IsNullOrEmpty(data.name))
                continue;

            string assetPath = CombineAssetPath(materialFolder, data.name + ".mat");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                    throw new InvalidOperationException("No URP/Lit or Standard shader is available.");

                material = new Material(shader) { name = data.name };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            SetColor(material, "_BaseColor", ToColor(data.baseColor, Color.white));
            SetColor(material, "_Color", ToColor(data.baseColor, Color.white));
            SetFloat(material, "_Metallic", data.metallic);
            SetFloat(material, "_Smoothness", Mathf.Clamp01(1f - data.roughness));
            SetFloat(material, "_Alpha", data.alpha);

            SetTexture(material, "_BaseMap", LoadTexture(manifestDirectory, data.baseMap));
            SetTexture(material, "_MainTex", LoadTexture(manifestDirectory, data.baseMap));
            SetTexture(material, "_MetallicGlossMap", LoadTexture(manifestDirectory, data.metallicMap));
            SetTexture(material, "_BumpMap", LoadNormalTexture(manifestDirectory, data.normalMap));
            SetTexture(material, "_EmissionMap", LoadTexture(manifestDirectory, data.emissionMap));
            EditorUtility.SetDirty(material);
            result[data.name] = material;
        }

        return result;
    }

    private static void ApplyMaterials(GameObject root, CarExportManifest manifest, Dictionary<string, Material> materials)
    {
        Dictionary<string, CarExportObject> byPath = new Dictionary<string, CarExportObject>(StringComparer.Ordinal);
        Dictionary<string, CarExportObject> byName = new Dictionary<string, CarExportObject>(StringComparer.Ordinal);

        if (manifest.objects != null)
        {
            foreach (CarExportObject data in manifest.objects)
            {
                if (data == null)
                    continue;
                if (!string.IsNullOrEmpty(data.path))
                    byPath[data.path] = data;
                if (!string.IsNullOrEmpty(data.name))
                    byName[data.name] = data;
            }
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            string path = RelativePath(root.transform, renderer.transform);
            CarExportObject data = null;
            byPath.TryGetValue(path, out data);
            if (data == null)
                byName.TryGetValue(renderer.gameObject.name, out data);
            if (data == null || data.materials == null)
                continue;

            Material[] slots = new Material[data.materials.Length];
            for (int i = 0; i < data.materials.Length; i++)
                materials.TryGetValue(data.materials[i], out slots[i]);
            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static Texture2D LoadTexture(string manifestDirectory, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;
        return AssetDatabase.LoadAssetAtPath<Texture2D>(CombineAssetPath(manifestDirectory, relativePath));
    }

    private static Texture2D LoadNormalTexture(string manifestDirectory, string relativePath)
    {
        Texture2D texture = LoadTexture(manifestDirectory, relativePath);
        if (texture == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string CombineAssetPath(string directory, string child)
    {
        return (directory.TrimEnd('/') + "/" + child.TrimStart('/', '\\')).Replace('\\', '/');
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string RelativePath(Transform root, Transform child)
    {
        if (root == child)
            return string.Empty;
        List<string> parts = new List<string>();
        Transform current = child;
        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static Color ToColor(float[] values, Color fallback)
    {
        if (values == null || values.Length < 3)
            return fallback;
        return new Color(values[0], values[1], values[2], values.Length > 3 ? values[3] : 1f);
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
            material.SetColor(property, value);
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private static void SetTexture(Material material, string property, Texture2D texture)
    {
        if (texture != null && material.HasProperty(property))
            material.SetTexture(property, texture);
    }

    [Serializable]
    private sealed class CarExportManifest
    {
        public string format;
        public string version;
        public string fbx;
        public string rootName;
        public CarExportObject[] objects;
        public CarExportMaterial[] materials;
    }

    [Serializable]
    private sealed class CarExportObject
    {
        public string name;
        public string path;
        public string type;
        public string[] materials;
    }

    [Serializable]
    private sealed class CarExportMaterial
    {
        public string name;
        public float[] baseColor;
        public float metallic;
        public float roughness;
        public float alpha;
        public string baseMap;
        public string normalMap;
        public string metallicMap;
        public string roughnessMap;
        public string emissionMap;
    }
}
