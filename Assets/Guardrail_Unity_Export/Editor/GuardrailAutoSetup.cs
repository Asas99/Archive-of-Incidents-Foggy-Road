#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GRPRO_AutoSetup
{
    const string TargetFbxFile = "Guardrails.fbx";
    const string SetupMarker = "GRPRO_AUTOSETUP_V3";

    static GRPRO_AutoSetup()
    {
        EditorApplication.delayCall += AutoSetupAll;
    }

    [MenuItem("Tools/Guardrail PRO/Auto Setup Imported Guardrail")]
    public static void AutoSetupAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(Path.GetFileName(path), TargetFbxFile, StringComparison.OrdinalIgnoreCase))
                Setup(path);
        }
    }

    static void Setup(string fbxPath)
    {
        var modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (modelImporter == null) return;

        string root = Path.GetDirectoryName(fbxPath).Replace("\\", "/");
        string texDir = root + "/Textures";
        string matDir = root + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder(root, "Materials");

        string basePath = texDir + "/GRPRO_Metal_BaseColor.png";
        string normalPath = texDir + "/GRPRO_Metal_Normal.png";
        string maskPath = texDir + "/GRPRO_Metal_MetallicSmoothness.png";

        ConfigureTexture(basePath, false, true);
        ConfigureTexture(normalPath, true, false);
        ConfigureTexture(maskPath, false, false);

        var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath);
        var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var maskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        if (lit == null)
        {
            Debug.LogError("Guardrail PRO: URP/Lit or Standard shader was not found.");
            return;
        }

        Material metal = GetOrCreate(matDir + "/GRPRO_Metal_Unity.mat", lit);
        SetBaseTexture(metal, baseTex);
        SetNormalTexture(metal, normalTex);
        SetMetallicTexture(metal, maskTex);
        SetFloatIfExists(metal, "_Metallic", 1.0f);
        SetFloatIfExists(metal, "_Smoothness", 1.0f);
        SetFloatIfExists(metal, "_BumpScale", 1.0f);
        metal.EnableKeyword("_NORMALMAP");
        metal.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(metal);

        Material bolts = GetOrCreate(matDir + "/GRPRO_Bolts_Unity.mat", lit);
        SetBaseColor(bolts, new Color(0.18f, 0.20f, 0.21f, 1f));
        SetFloatIfExists(bolts, "_Metallic", 0.95f);
        SetFloatIfExists(bolts, "_Smoothness", 0.78f);
        EditorUtility.SetDirty(bolts);

        Material reflector = GetOrCreate(matDir + "/GRPRO_Reflector_Unity.mat", lit);
        Color rc = new Color(1.000000f, 0.400000f, 0.025000f, 1f);
        SetBaseColor(reflector, rc);
        if (reflector.HasProperty("_EmissionColor"))
        {
            reflector.SetColor("_EmissionColor", rc * 1.400000f);
            reflector.EnableKeyword("_EMISSION");
        }
        SetFloatIfExists(reflector, "_Metallic", 0.0f);
        SetFloatIfExists(reflector, "_Smoothness", 0.66f);
        EditorUtility.SetDirty(reflector);

        AssetDatabase.SaveAssets();

        string[] steelNames = {
            "GRPRO_Dark_Wet_Steel",
            "GRPRO_Galvanized_Steel",
            "GRPRO_Black_Coated_Steel",
            "GRPRO_Custom_Steel"
        };
        foreach (string n in steelNames)
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), metal);
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "GRPRO_Bolts"), bolts);
        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "GRPRO_Reflector"), reflector);

        if (modelImporter.userData != SetupMarker)
        {
            modelImporter.userData = SetupMarker;
            modelImporter.SaveAndReimport();
        }
        else
        {
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
        }

        Debug.Log("Guardrail PRO: materials/textures configured automatically for " + fbxPath);
    }

    static void ConfigureTexture(string path, bool normalMap, bool srgb)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        TextureImporterType wanted = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ti.textureType != wanted) { ti.textureType = wanted; changed = true; }
        if (ti.sRGBTexture != srgb) { ti.sRGBTexture = srgb; changed = true; }
        if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; changed = true; }
        if (ti.filterMode != FilterMode.Trilinear) { ti.filterMode = FilterMode.Trilinear; changed = true; }
        if (ti.anisoLevel != 4) { ti.anisoLevel = 4; changed = true; }
        if (changed) ti.SaveAndReimport();
    }

    static Material GetOrCreate(string path, Shader shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader != shader)
            m.shader = shader;
        return m;
    }

    static void SetBaseTexture(Material m, Texture t)
    {
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        SetBaseColor(m, Color.white);
    }

    static void SetBaseColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    static void SetNormalTexture(Material m, Texture t)
    {
        if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", t);
    }

    static void SetMetallicTexture(Material m, Texture t)
    {
        if (m.HasProperty("_MetallicGlossMap")) m.SetTexture("_MetallicGlossMap", t);
    }

    static void SetFloatIfExists(Material m, string property, float value)
    {
        if (m.HasProperty(property)) m.SetFloat(property, value);
    }
}
#endif
