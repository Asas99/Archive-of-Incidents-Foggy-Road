#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Renders the actual vegetation meshes/materials in an isolated preview scene.
// A temporary shader defines the previous late-clip order; no source assets or
// current scene objects are edited. Wind is held at zero on test materials only.
public static class NatureOptimizationValidation
{
    sealed class Source { public string name; public Mesh mesh; public Material[] materials; }

    public static string Run(bool withShadows = false)
    {
        var controller = Object.FindFirstObjectByType<SceneOptimizationController>();
        var grass = Object.FindFirstObjectByType<FarGrassRenderer>();
        var sources = new List<Source>();
        var terrain = controller.terrains.First(t => t && t.terrainData);
        foreach (int index in new[] { 0, 4, 8 })
        {
            var prefab = terrain.terrainData.detailPrototypes[index].prototype;
            var renderer = prefab.GetComponentInChildren<MeshRenderer>();
            sources.Add(new Source { name = "Detail_" + index, mesh = renderer.GetComponent<MeshFilter>().sharedMesh, materials = renderer.sharedMaterials });
        }
        foreach (var prefab in terrain.terrainData.treePrototypes.Select(p => p.prefab).Where(p => p && p.GetComponent<LODGroup>()).Distinct())
        {
            var renderer = prefab.GetComponent<LODGroup>().GetLODs()[0].renderers[0];
            sources.Add(new Source { name = prefab.name, mesh = renderer.GetComponent<MeshFilter>().sharedMesh, materials = renderer.sharedMaterials });
        }
        foreach (var cache in grass.caches)
            sources.Add(new Source { name = "FarGrass", mesh = cache.mesh, materials = new[] { cache.material } });

        var scene = EditorSceneManager.NewPreviewScene();
        var objects = new List<Object>();
        var lateShaders = new Dictionary<Shader, Shader>();
        var earlyShaders = new Dictionary<Shader, Shader>();
        var report = new System.Text.StringBuilder();
        string folder = SessionState.GetString("NatureOptimizationBackup", "Library");
        var cameraGo = new GameObject("Vegetation comparison camera");
        SceneManager.MoveGameObjectToScene(cameraGo, scene);
        var camera = cameraGo.AddComponent<Camera>();
        camera.enabled = false;
        camera.cameraType = CameraType.Preview;
        camera.scene = scene;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.13f, .19f, .27f, 1);
        camera.orthographic = true;
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 300;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        var cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = withShadows;
        var lightGo = new GameObject("Comparison sun");
        SceneManager.MoveGameObjectToScene(lightGo, scene);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1;
        light.shadows = withShadows ? LightShadows.Soft : LightShadows.None;
        light.transform.rotation = Quaternion.Euler(40, 25, 0);

        var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        target.Create(); objects.Add(target);
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
        objects.Add(texture);
        var oldActive = RenderTexture.active;
        float oldEnabled = Shader.GetGlobalFloat("_FoggyOptimizationEnabled");
        string[] globalNames = { "_FoggyOptimizationCamera", "_FoggyTreeRange", "_FoggyGrassRange", "_FoggyFarGrassRange", "_FoggyDensityDistances", "_FoggyDensityValues" };
        var oldGlobals = globalNames.Select(Shader.GetGlobalVector).ToArray();
        float coverageStart = 0, coverageEnd = 0;
        Action<ScriptableRenderContext, Camera> setGlobals = (context, c) => {
            if (c != camera) return;
            Shader.SetGlobalFloat("_FoggyOptimizationEnabled", 1);
            Shader.SetGlobalVector("_FoggyOptimizationCamera", new Vector4(3, 0, 0, 1));
            Shader.SetGlobalVector("_FoggyTreeRange", new Vector4(coverageStart, coverageEnd, 0, 0));
            Shader.SetGlobalVector("_FoggyGrassRange", new Vector4(coverageStart, coverageEnd, 0, 0));
            float distance = camera.transform.position.magnitude;
            Shader.SetGlobalVector("_FoggyFarGrassRange", new Vector4(0, 0, distance + coverageStart - 3, distance + coverageEnd - 3));
            Shader.SetGlobalVector("_FoggyDensityDistances", Vector4.zero);
            Shader.SetGlobalVector("_FoggyDensityValues", new Vector4(1, 1, 1, .04f));
        };
        RenderPipelineManager.beginCameraRendering += setGlobals;
        int comparisons = 0, differing = 0;
        try
        {
            foreach (var source in sources)
            {
                var go = new GameObject(source.name);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.AddComponent<MeshFilter>().sharedMesh = source.mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = withShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                var early = new Material[source.materials.Length];
                var late = new Material[source.materials.Length];
                for (int i = 0; i < early.Length; i++)
                {
                    var original = source.materials[i];
                    early[i] = new Material(original); objects.Add(early[i]);
                    // All work and textures remain identical; only animation is
                    // frozen to make consecutive validation renders comparable.
                    foreach (string wind in new[] { "_WindQuality", "Wind_Intensity", "Wind_Blast", "Wind_Ripples" })
                        if (early[i].HasProperty(wind)) early[i].SetFloat(wind, 0);
                    if (early[i].HasProperty("Distance_Fade_Start")) { early[i].SetFloat("Distance_Fade_Start", 10000); early[i].SetFloat("Distance_Fade_End", 11000); }
                    Shader earlyShader = original.shader;
                    if (!lateShaders.TryGetValue(earlyShader, out var lateShader))
                    {
                        string path = AssetDatabase.GetAssetPath(earlyShader);
                        string text;
                        if (path.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase))
                        {
                            var importer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter")).First(t => t != null);
                            var method = importer.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).First(m => m.Name == "GetShaderText" && m.GetParameters().Length == 4 && m.GetParameters()[3].IsOut);
                            text = (string)method.Invoke(null, new object[] { path, null, null, null });
                        }
                        else text = File.ReadAllText(path);
                        int quote = text.IndexOf('"'), end = text.IndexOf('"', quote + 1);
                        text = text.Substring(0, quote + 1) + "Hidden/NatureLateClipValidation/" + lateShaders.Count + text.Substring(end);
                        // Preview render requests advance editor time. Freeze the
                        // generated input clocks in BOTH temporary shaders, and
                        // compile both through the same path to avoid comparing
                        // importer defaults against a cold temporary shader.
                        text = text.Replace("_TimeParameters.xyz", "float3(0, 1, 1)");
                        text = text.Replace("_Time.y", "0.0");
                        var validationEarly = ShaderUtil.CreateShaderAsset(text.Replace("Hidden/NatureLateClipValidation/", "Hidden/NatureEarlyClipValidation/"));
                        objects.Add(validationEarly);
                        if (ShaderUtil.ShaderHasError(validationEarly)) throw new Exception("Early validation shader compile failed");
                        earlyShaders.Add(earlyShader, validationEarly);
                        lateShader = ShaderUtil.CreateShaderAsset(text.Replace("HLSLPROGRAM", "HLSLPROGRAM\n#define FOGGY_VALIDATE_LATE_CLIP 1\n"));
                        objects.Add(lateShader);
                        if (ShaderUtil.ShaderHasError(lateShader)) throw new Exception("Validation shader compile failed: " + earlyShader.name);
                        lateShaders.Add(earlyShader, lateShader);
                    }
                    early[i].shader = earlyShaders[earlyShader];
                    late[i] = new Material(early[i]) { shader = lateShader }; objects.Add(late[i]);
                    foreach (string property in original.GetTexturePropertyNames())
                    {
                        if (early[i].HasProperty(property)) early[i].SetTexture(property, original.GetTexture(property));
                        if (late[i].HasProperty(property)) late[i].SetTexture(property, original.GetTexture(property));
                    }
                }
                var bounds = source.mesh.bounds;
                GameObject receiver = null;
                if (withShadows)
                {
                    receiver = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    SceneManager.MoveGameObjectToScene(receiver, scene);
                    receiver.transform.position = new Vector3(bounds.center.x, bounds.min.y - .01f, bounds.center.z);
                    receiver.transform.localScale = Vector3.one * Mathf.Max(1, bounds.size.magnitude);
                    var ground = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    objects.Add(ground);
                    receiver.GetComponent<Renderer>().sharedMaterial = ground;
                }
                camera.orthographicSize = Mathf.Max(.1f, bounds.size.magnitude * .6f);
                Vector3 offset = new Vector3(0, withShadows ? bounds.size.magnitude : 0, -Mathf.Max(5, bounds.size.magnitude * 2));
                camera.transform.SetPositionAndRotation(bounds.center + offset, Quaternion.LookRotation(-offset));
                foreach (int mode in new[] { 0, 1, 2 })
                {
                    coverageStart = mode == 0 ? 10 : mode == 1 ? 2 : 0;
                    coverageEnd = mode == 0 ? 20 : mode == 1 ? 4 : 1;
                    renderer.sharedMaterials = late;
                    Capture(camera, target, texture);
                    Capture(camera, target, texture);
                    Color32[] before = Capture(camera, target, texture);
                    Color32[] repeated = Capture(camera, target, texture);
                    int unstable = before.Where((pixel, i) => !pixel.Equals(repeated[i])).Count();
                    if (unstable != 0) throw new Exception("Unstable reference render: " + source.name + " pixels=" + unstable);
                    renderer.sharedMaterials = early;
                    Capture(camera, target, texture);
                    Capture(camera, target, texture);
                    Color32[] after = Capture(camera, target, texture);
                    int count = 0;
                    for (int i = 0; i < before.Length; i++) if (!before[i].Equals(after[i])) count++;
                    int foreground = before.Count(pixel => !pixel.Equals(before[0]));
                    if (mode == 0 && foreground < 64) throw new Exception("Fixture did not render: " + source.name);
                    comparisons++; differing += count;
                    report.AppendLine(source.name + " fade=" + mode + " foreground=" + foreground + " differing pixels=" + count + "/" + before.Length);
                }
                Object.DestroyImmediate(go);
                if (receiver) Object.DestroyImmediate(receiver);
            }
            report.AppendLine("TOTAL comparisons=" + comparisons + "; differing pixels=" + differing);
            File.WriteAllText(folder + (withShadows ? "/ShadowPixelValidation.txt" : "/PixelValidation.txt"), report.ToString());
            if (differing != 0) throw new Exception("Early clip changes pixels. See " + folder + "/PixelValidation.txt");
            return report.ToString();
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering -= setGlobals;
            Shader.SetGlobalFloat("_FoggyOptimizationEnabled", oldEnabled);
            for (int i = 0; i < globalNames.Length; i++) Shader.SetGlobalVector(globalNames[i], oldGlobals[i]);
            RenderTexture.active = oldActive;
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var obj in objects) if (obj) Object.DestroyImmediate(obj);
        }
    }

    static Color32[] Capture(Camera camera, RenderTexture target, Texture2D texture)
    {
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
        camera.SubmitRenderRequest(request);
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        texture.Apply(false);
        return texture.GetPixels32();
    }
}
#endif
