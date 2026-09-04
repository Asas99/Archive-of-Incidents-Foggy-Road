#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports a Unity Terrain as an OBJ mesh with clean UV0 coordinates for Adobe Substance 3D Painter.
/// Output is written automatically to the user's Desktop and includes a UV layout PNG + README.
///
/// Put this file anywhere under Assets/Editor, then open:
/// Tools > Environment > Terrain Substance Exporter
/// </summary>
public sealed class TerrainSubstanceExporter : EditorWindow
{
    private enum UvMode
    {
        SingleTile_0_1,
        UDIM_2x2,
        UDIM_4x4,
        CustomUDIM
    }

    [Header("Terrain")]
    [SerializeField] private Terrain targetTerrain;

    [Header("Mesh Quality")]
    [Tooltip("Vertices per side. 513 is a strong default for Substance Painter. 1025 can create very large OBJ files.")]
    [SerializeField, Range(65, 2049)] private int verticesPerSide = 513;

    [Tooltip("Exports the terrain's interpolated normals so lighting/baking in Painter follows the Unity terrain more closely.")]
    [SerializeField] private bool exportNormals = true;

    [Tooltip("Keeps the exported mesh centered around X/Z origin. This does not change its size or shape.")]
    [SerializeField] private bool centerMeshAtOrigin = true;

    [Header("UV Layout")]
    [SerializeField] private UvMode uvMode = UvMode.SingleTile_0_1;

    [SerializeField, Range(1, 10)] private int customTilesX = 2;
    [SerializeField, Range(1, 10)] private int customTilesY = 2;

    [Tooltip("Also writes a PNG UV guide next to the OBJ.")]
    [SerializeField] private bool exportUvLayoutPng = true;

    [SerializeField, Range(1024, 8192)] private int uvLayoutSize = 4096;

    [Header("Output")]
    [SerializeField] private bool openFolderAfterExport = true;

    private Vector2 scroll;

    [MenuItem("Tools/Environment/Terrain Substance Exporter")]
    private static void OpenWindow()
    {
        TerrainSubstanceExporter window = GetWindow<TerrainSubstanceExporter>();
        window.titleContent = new GUIContent("Terrain -> Substance");
        window.minSize = new Vector2(440f, 560f);
    }

    private void OnGUI()
    {
        // Keep all settings scrollable, but pin the export button to the bottom
        // so it can never disappear below the visible EditorWindow area.
        EditorGUILayout.BeginVertical();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        EditorGUILayout.Space(8f);

        EditorGUILayout.HelpBox(
            "Exports the selected Unity Terrain to your Desktop as a Substance Painter-ready OBJ. " +
            "The OBJ contains a clean non-overlapping UV0 layout, normals, real terrain scale, and an optional UV guide PNG.",
            MessageType.Info);

        SerializedObject so = new SerializedObject(this);
        so.Update();

        DrawSection("TERRAIN");
        EditorGUILayout.PropertyField(so.FindProperty("targetTerrain"));

        if (targetTerrain == null && Selection.activeGameObject != null)
        {
            Terrain selectedTerrain = Selection.activeGameObject.GetComponent<Terrain>();
            if (selectedTerrain != null)
            {
                targetTerrain = selectedTerrain;
                so.Update();
            }
        }

        EditorGUILayout.Space(8f);
        DrawSection("MESH QUALITY");
        EditorGUILayout.PropertyField(so.FindProperty("verticesPerSide"));
        EditorGUILayout.PropertyField(so.FindProperty("exportNormals"));
        EditorGUILayout.PropertyField(so.FindProperty("centerMeshAtOrigin"));

        if (targetTerrain != null && targetTerrain.terrainData != null)
        {
            TerrainData data = targetTerrain.terrainData;
            long vertexCount = (long)verticesPerSide * verticesPerSide;
            long triangleCount = (long)(verticesPerSide - 1) * (verticesPerSide - 1) * 2L;

            EditorGUILayout.HelpBox(
                "Terrain: " + data.size.x.ToString("F1") + " x " + data.size.z.ToString("F1") + " m\n" +
                "Export mesh: " + vertexCount.ToString("N0") + " vertices / " + triangleCount.ToString("N0") + " triangles",
                vertexCount > 1100000 ? MessageType.Warning : MessageType.None);
        }

        EditorGUILayout.Space(8f);
        DrawSection("UV LAYOUT");
        EditorGUILayout.PropertyField(so.FindProperty("uvMode"));
        if (uvMode == UvMode.CustomUDIM)
        {
            EditorGUILayout.PropertyField(so.FindProperty("customTilesX"));
            EditorGUILayout.PropertyField(so.FindProperty("customTilesY"));
        }
        EditorGUILayout.PropertyField(so.FindProperty("exportUvLayoutPng"));
        if (exportUvLayoutPng)
            EditorGUILayout.PropertyField(so.FindProperty("uvLayoutSize"));

        GetUvTileCount(out int tilesX, out int tilesY);
        if (tilesX == 1 && tilesY == 1)
        {
            EditorGUILayout.HelpBox(
                "UV0 uses the full 0-1 square. This is the easiest Painter workflow. " +
                "Use 4K or 8K texture sets depending on the terrain size.",
                MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "UVs span " + tilesX + " x " + tilesY + " UDIM tiles. In Substance Painter create the project using the UV Tile workflow.",
                MessageType.Info);
        }

        EditorGUILayout.Space(8f);
        DrawSection("OUTPUT");
        EditorGUILayout.PropertyField(so.FindProperty("openFolderAfterExport"));

        // IMPORTANT: never put placeholder characters such as < > into Path.Combine on Windows.
        // Unity/Mono treats them as illegal path characters and OnGUI aborts before drawing the footer button.
        string desktop = GetDesktopFolder();
        string previewTerrainName = targetTerrain != null ? SanitizeFileName(targetTerrain.name) : "Terrain";
        string previewFolderName = "SubstanceTerrainExport_" + previewTerrainName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string previewPath = string.IsNullOrEmpty(desktop)
            ? previewFolderName
            : Path.Combine(desktop, previewFolderName);

        EditorGUILayout.SelectableLabel(
            previewPath,
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Painter is for surface/material painting. The exported OBJ keeps the Terrain geometry and UV0.",
            MessageType.None);

        so.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();

        // Sticky footer: always visible even on small Unity windows.
        EditorGUILayout.Space(5f);
        Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(lineRect, new Color(0f, 0f, 0f, 0.35f));
        EditorGUILayout.Space(5f);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(targetTerrain == null || targetTerrain.terrainData == null))
            {
                GUI.backgroundColor = new Color(0.45f, 0.84f, 0.58f);
                if (GUILayout.Button("EXPORT TERRAIN TO DESKTOP FOR SUBSTANCE", GUILayout.Height(48f), GUILayout.ExpandWidth(true)))
                    ExportTerrain();
                GUI.backgroundColor = Color.white;
            }
            GUILayout.Space(8f);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.EndVertical();
    }

    private static void DrawSection(string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private void GetUvTileCount(out int tilesX, out int tilesY)
    {
        switch (uvMode)
        {
            case UvMode.UDIM_2x2:
                tilesX = 2;
                tilesY = 2;
                break;
            case UvMode.UDIM_4x4:
                tilesX = 4;
                tilesY = 4;
                break;
            case UvMode.CustomUDIM:
                tilesX = Mathf.Clamp(customTilesX, 1, 10);
                tilesY = Mathf.Clamp(customTilesY, 1, 10);
                break;
            default:
                tilesX = 1;
                tilesY = 1;
                break;
        }
    }

    private void ExportTerrain()
    {
        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Terrain missing", "Assign a Terrain first.", "OK");
            return;
        }

        TerrainData data = targetTerrain.terrainData;
        int resolution = Mathf.Clamp(verticesPerSide, 65, 2049);
        GetUvTileCount(out int tilesX, out int tilesY);

        long vertexCount = (long)resolution * resolution;
        if (vertexCount > 1600000)
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Very large export",
                "This will export " + vertexCount.ToString("N0") + " vertices. The OBJ can become very large and Painter import may be slow.\n\nContinue?",
                "Export",
                "Cancel");
            if (!proceed)
                return;
        }

        string desktop = GetDesktopFolder();
        if (string.IsNullOrEmpty(desktop) || !Directory.Exists(desktop))
        {
            EditorUtility.DisplayDialog(
                "Desktop not found",
                "Unity could not resolve the Desktop folder on this system.\n\n" +
                "Try opening the project normally (not under a restricted account) or temporarily disable OneDrive Desktop redirection.",
                "OK");
            return;
        }

        string safeTerrainName = SanitizeFileName(targetTerrain.name);
        string folderName = "SubstanceTerrainExport_" + safeTerrainName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string exportFolder = Path.Combine(desktop, folderName);
        Directory.CreateDirectory(exportFolder);

        string objPath = Path.Combine(exportFolder, safeTerrainName + "_Substance.obj");
        string uvPath = Path.Combine(exportFolder, safeTerrainName + "_UV_Layout.png");
        string readmePath = Path.Combine(exportFolder, "README_Substance.txt");

        try
        {
            EditorUtility.DisplayProgressBar("Terrain -> Substance", "Writing OBJ vertices...", 0.05f);
            WriteObj(data, resolution, tilesX, tilesY, objPath);

            if (exportUvLayoutPng)
            {
                EditorUtility.DisplayProgressBar("Terrain -> Substance", "Creating UV layout PNG...", 0.88f);
                WriteUvLayoutPng(resolution, tilesX, tilesY, uvLayoutSize, uvPath);
            }

            EditorUtility.DisplayProgressBar("Terrain -> Substance", "Writing import notes...", 0.97f);
            WriteReadme(data, resolution, tilesX, tilesY, objPath, uvPath, readmePath);

            EditorUtility.ClearProgressBar();

            if (openFolderAfterExport)
                EditorUtility.RevealInFinder(objPath);

            EditorUtility.DisplayDialog(
                "Substance export complete",
                "Exported to Desktop:\n\n" + exportFolder + "\n\n" +
                "OBJ: " + Path.GetFileName(objPath) + "\n" +
                (exportUvLayoutPng ? "UV guide: " + Path.GetFileName(uvPath) + "\n" : string.Empty) +
                "UV tiles: " + tilesX + " x " + tilesY,
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Export failed", ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void WriteObj(TerrainData data, int resolution, int tilesX, int tilesY, string objPath)
    {
        Vector3 size = data.size;
        float offsetX = centerMeshAtOrigin ? size.x * 0.5f : 0f;
        float offsetZ = centerMeshAtOrigin ? size.z * 0.5f : 0f;
        CultureInfo invariant = CultureInfo.InvariantCulture;

        using (StreamWriter writer = new StreamWriter(objPath, false, new UTF8Encoding(false), 1024 * 1024))
        {
            writer.WriteLine("# Unity Terrain export for Adobe Substance 3D Painter");
            writer.WriteLine("# Terrain: " + targetTerrain.name);
            writer.WriteLine("# Size metres: " + size.x.ToString("F4", invariant) + " " + size.y.ToString("F4", invariant) + " " + size.z.ToString("F4", invariant));
            writer.WriteLine("# Resolution: " + resolution + " x " + resolution);
            writer.WriteLine("# UV tiles: " + tilesX + " x " + tilesY);
            writer.WriteLine("o " + SanitizeObjName(targetTerrain.name));

            // Vertices. Z is flipped so the OBJ uses a conventional right-handed orientation.
            for (int z = 0; z < resolution; z++)
            {
                float v = z / (float)(resolution - 1);
                if ((z & 15) == 0)
                    EditorUtility.DisplayProgressBar("Terrain -> Substance", "Writing OBJ vertices...", 0.05f + 0.25f * z / resolution);

                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)(resolution - 1);
                    float localX = u * size.x - offsetX;
                    float localZ = v * size.z - offsetZ;
                    float localY = data.GetInterpolatedHeight(u, v);
                    float exportZ = -localZ;

                    writer.Write("v ");
                    writer.Write(localX.ToString("R", invariant));
                    writer.Write(' ');
                    writer.Write(localY.ToString("R", invariant));
                    writer.Write(' ');
                    writer.WriteLine(exportZ.ToString("R", invariant));
                }
            }

            // UV0. Values above 1 intentionally become UDIM tiles when tilesX/Y > 1.
            for (int z = 0; z < resolution; z++)
            {
                float v = z / (float)(resolution - 1);
                if ((z & 31) == 0)
                    EditorUtility.DisplayProgressBar("Terrain -> Substance", "Writing UV coordinates...", 0.31f + 0.12f * z / resolution);

                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)(resolution - 1);
                    writer.Write("vt ");
                    writer.Write((u * tilesX).ToString("R", invariant));
                    writer.Write(' ');
                    writer.WriteLine((v * tilesY).ToString("R", invariant));
                }
            }

            // Terrain normals, also Z-flipped to match exported geometry.
            if (exportNormals)
            {
                for (int z = 0; z < resolution; z++)
                {
                    float v = z / (float)(resolution - 1);
                    if ((z & 31) == 0)
                        EditorUtility.DisplayProgressBar("Terrain -> Substance", "Writing normals...", 0.44f + 0.12f * z / resolution);

                    for (int x = 0; x < resolution; x++)
                    {
                        float u = x / (float)(resolution - 1);
                        Vector3 n = data.GetInterpolatedNormal(u, v).normalized;
                        writer.Write("vn ");
                        writer.Write(n.x.ToString("R", invariant));
                        writer.Write(' ');
                        writer.Write(n.y.ToString("R", invariant));
                        writer.Write(' ');
                        writer.WriteLine((-n.z).ToString("R", invariant));
                    }
                }
            }

            writer.WriteLine("s 1");

            // Faces. Winding is reversed because Z was flipped.
            int faceRows = resolution - 1;
            for (int z = 0; z < faceRows; z++)
            {
                if ((z & 15) == 0)
                    EditorUtility.DisplayProgressBar("Terrain -> Substance", "Writing triangles...", 0.57f + 0.28f * z / faceRows);

                for (int x = 0; x < resolution - 1; x++)
                {
                    int i0 = z * resolution + x + 1;
                    int i1 = i0 + 1;
                    int i2 = (z + 1) * resolution + x + 1;
                    int i3 = i2 + 1;

                    WriteFace(writer, i0, i1, i2, exportNormals);
                    WriteFace(writer, i1, i3, i2, exportNormals);
                }
            }
        }
    }

    private static void WriteFace(StreamWriter writer, int a, int b, int c, bool withNormals)
    {
        if (withNormals)
        {
            writer.Write("f ");
            WriteFaceVertex(writer, a, true);
            writer.Write(' ');
            WriteFaceVertex(writer, b, true);
            writer.Write(' ');
            WriteFaceVertex(writer, c, true);
            writer.WriteLine();
        }
        else
        {
            writer.Write("f ");
            WriteFaceVertex(writer, a, false);
            writer.Write(' ');
            WriteFaceVertex(writer, b, false);
            writer.Write(' ');
            WriteFaceVertex(writer, c, false);
            writer.WriteLine();
        }
    }

    private static void WriteFaceVertex(StreamWriter writer, int index, bool withNormal)
    {
        writer.Write(index);
        writer.Write('/');
        writer.Write(index);
        if (withNormal)
        {
            writer.Write('/');
            writer.Write(index);
        }
    }

    private static void WriteUvLayoutPng(int meshResolution, int tilesX, int tilesY, int requestedSize, string path)
    {
        int size = Mathf.Clamp(requestedSize, 1024, 8192);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        Color32 background = new Color32(245, 245, 245, 255);
        Color32 minor = new Color32(150, 150, 150, 255);
        Color32 major = new Color32(35, 35, 35, 255);

        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = background;
        texture.SetPixels32(pixels);

        // Painter-friendly UV guide. The terrain mesh is a regular grid, so a sampled grid is clearer
        // than drawing hundreds of thousands of nearly overlapping triangle edges.
        int subdivisions = Mathf.Clamp((meshResolution - 1) / 8, 16, 128);
        for (int i = 0; i <= subdivisions; i++)
        {
            int px = Mathf.RoundToInt(i / (float)subdivisions * (size - 1));
            DrawVertical(texture, px, minor);
            DrawHorizontal(texture, px, minor);
        }

        for (int tx = 0; tx <= tilesX; tx++)
        {
            int px = Mathf.RoundToInt(tx / (float)tilesX * (size - 1));
            DrawVertical(texture, px, major, 3);
        }
        for (int ty = 0; ty <= tilesY; ty++)
        {
            int py = Mathf.RoundToInt(ty / (float)tilesY * (size - 1));
            DrawHorizontal(texture, py, major, 3);
        }

        texture.Apply(false, false);
        byte[] png = texture.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(texture);
        File.WriteAllBytes(path, png);
    }

    private static void DrawVertical(Texture2D texture, int x, Color32 color, int thickness = 1)
    {
        int half = Mathf.Max(0, thickness / 2);
        for (int dx = -half; dx <= half; dx++)
        {
            int px = Mathf.Clamp(x + dx, 0, texture.width - 1);
            for (int y = 0; y < texture.height; y++)
                texture.SetPixel(px, y, color);
        }
    }

    private static void DrawHorizontal(Texture2D texture, int y, Color32 color, int thickness = 1)
    {
        int half = Mathf.Max(0, thickness / 2);
        for (int dy = -half; dy <= half; dy++)
        {
            int py = Mathf.Clamp(y + dy, 0, texture.height - 1);
            for (int x = 0; x < texture.width; x++)
                texture.SetPixel(x, py, color);
        }
    }

    private void WriteReadme(
        TerrainData data,
        int resolution,
        int tilesX,
        int tilesY,
        string objPath,
        string uvPath,
        string readmePath)
    {
        StringBuilder sb = new StringBuilder(2048);
        sb.AppendLine("UNITY TERRAIN -> SUBSTANCE 3D PAINTER EXPORT");
        sb.AppendLine("=============================================");
        sb.AppendLine();
        sb.AppendLine("Terrain: " + targetTerrain.name);
        sb.AppendLine("Terrain size: " + data.size.x.ToString("F2") + " x " + data.size.z.ToString("F2") + " metres");
        sb.AppendLine("Mesh resolution: " + resolution + " x " + resolution);
        sb.AppendLine("UV tiles: " + tilesX + " x " + tilesY);
        sb.AppendLine("OBJ: " + Path.GetFileName(objPath));
        if (exportUvLayoutPng)
            sb.AppendLine("UV guide: " + Path.GetFileName(uvPath));
        sb.AppendLine();
        sb.AppendLine("SUBSTANCE PAINTER");
        sb.AppendLine("1. File > New");
        sb.AppendLine("2. Select the exported OBJ as the mesh.");
        if (tilesX > 1 || tilesY > 1)
            sb.AppendLine("3. Enable the UV Tile / UDIM workflow. UVs intentionally extend outside 0-1.");
        else
            sb.AppendLine("3. Standard 0-1 UV workflow is ready; no unwrap is required.");
        sb.AppendLine("4. Start at 4096 texture resolution. Use 8192 only if the terrain needs more close-up detail.");
        sb.AppendLine("5. Bake Mesh Maps in Painter before adding smart materials/masks.");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT");
        sb.AppendLine("Substance Painter changes materials/textures, not the terrain geometry. Sculpt hills/ditches in Unity/Blender first if needed.");
        sb.AppendLine("OBJ scale is exported in Unity metres. The mesh is centered in X/Z when 'Center Mesh At Origin' is enabled.");
        sb.AppendLine();
        sb.AppendLine("RETURNING TEXTURES TO UNITY");
        sb.AppendLine("Export Base Color, Normal, Roughness/Smoothness-compatible maps, Height if used, and any masks you need.");
        sb.AppendLine("For Unity Terrain itself, Painter textures normally need to be re-applied through Terrain Layers/shaders or projected/baked back to terrain materials.");

        File.WriteAllText(readmePath, sb.ToString(), new UTF8Encoding(false));
    }

    private static string GetDesktopFolder()
    {
        // DesktopDirectory is normally correct on Windows, including localized Windows installs.
        // Keep fallbacks because Desktop can be redirected by OneDrive/domain policies.
        string[] candidates =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop")
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string candidate = candidates[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            try
            {
                string fullPath = Path.GetFullPath(candidate);
                if (Directory.Exists(fullPath))
                    return fullPath;
            }
            catch (Exception)
            {
                // Ignore malformed/unavailable fallback and try the next candidate.
            }
        }

        return string.Empty;
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            value = "Terrain";

        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        // Explicit Windows-safe pass as Unity/Mono path rules may differ by runtime.
        const string windowsInvalid = "<>:\"/\\|?*";
        for (int i = 0; i < windowsInvalid.Length; i++)
            value = value.Replace(windowsInvalid[i], '_');

        value = value.Trim().TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(value) ? "Terrain" : value;
    }

    private static string SanitizeObjName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Terrain";
        return value.Replace(' ', '_').Replace('\t', '_');
    }
}
#endif
