using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FoggyRoadOptimizer
{
    private const string BackupPath = "Assets/FoggyRoad/Looks/FoggyRoadOptimizationBackup.asset";
    private const string RendererDataPath = "Assets/Settings/PC_Renderer.asset";
    private const string PipelineAssetPath = "Assets/Settings/PC_RPAsset.asset";
    private const string FogShaderName = "Custom/RealisticVolumetricFog";

    [MenuItem("Tools/Foggy Road/Optimize/Apply A - Instancing only")]
    public static void ApplyA() => ApplyLevel(1, "A - Instancing only");

    [MenuItem("Tools/Foggy Road/Optimize/Apply B - Fog-capped tree range")]
    public static void ApplyB() => ApplyLevel(2, "B - Fog-capped tree range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply C - Earlier tree LOD")]
    public static void ApplyC() => ApplyLevel(3, "C - Earlier tree LOD");

    [MenuItem("Tools/Foggy Road/Optimize/Apply D - Shorter ground detail")]
    public static void ApplyD() => ApplyLevel(4, "D - Shorter ground detail");

    [MenuItem("Tools/Foggy Road/Optimize/Apply E - Tighter shadow range")]
    public static void ApplyE() => ApplyLevel(5, "E - Tighter shadow range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply F - Lower fog sample cost")]
    public static void ApplyF() => ApplyLevel(6, "F - Lower fog sample cost");

    [MenuItem("Tools/Foggy Road/Optimize/Apply G - Near-only foliage shadows")]
    public static void ApplyG() => ApplyLevel(7, "G - Near-only foliage shadows");

    [MenuItem("Tools/Foggy Road/Optimize/Apply H - Fog-hidden foliage culling")]
    public static void ApplyH() => ApplyLevel(8, "H - Fog-hidden foliage culling");

    [MenuItem("Tools/Foggy Road/Optimize/Apply I - Near-only rock shadows")]
    public static void ApplyI() => ApplyLevel(9, "I - Near-only rock shadows");

    [MenuItem("Tools/Foggy Road/Optimize/Apply J - Early small-detail culling")]
    public static void ApplyJ() => ApplyLevel(10, "J - Early small-detail culling");

    [MenuItem("Tools/Foggy Road/Optimize/Apply K - Coarser terrain mesh")]
    public static void ApplyK() => ApplyLevel(11, "K - Coarser terrain mesh");

    [MenuItem("Tools/Foggy Road/Optimize/Apply L - One shadow cascade")]
    public static void ApplyL() => ApplyLevel(12, "L - One shadow cascade");

    [MenuItem("Tools/Foggy Road/Optimize/Apply M - Lower fog sample cost 24")]
    public static void ApplyM() => ApplyLevel(13, "M - Lower fog sample cost 24");

    [MenuItem("Tools/Foggy Road/Optimize/Apply N - Disable SSAO")]
    public static void ApplyN() => ApplyLevel(14, "N - Disable SSAO");

    [MenuItem("Tools/Foggy Road/Optimize/Apply O - 240m fog-capped tree range")]
    public static void ApplyO() => ApplyLevel(15, "O - 240m fog-capped tree range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply P - 185m fog-hidden foliage culling")]
    public static void ApplyP() => ApplyLevel(16, "P - 185m fog-hidden foliage culling");

    [MenuItem("Tools/Foggy Road/Optimize/Apply Q - 80m small-detail culling")]
    public static void ApplyQ() => ApplyLevel(17, "Q - 80m small-detail culling");

    [MenuItem("Tools/Foggy Road/Optimize/Apply R - 34m shadow range")]
    public static void ApplyR() => ApplyLevel(18, "R - 34m shadow range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply S - 20m foliage and 24m rock shadows")]
    public static void ApplyS() => ApplyLevel(19, "S - 20m foliage and 24m rock shadows");

    [MenuItem("Tools/Foggy Road/Optimize/Apply T - 42m lower ground detail")]
    public static void ApplyT() => ApplyLevel(20, "T - 42m lower ground detail");

    [MenuItem("Tools/Foggy Road/Optimize/Apply U - Lower fog sample cost 20")]
    public static void ApplyU() => ApplyLevel(21, "U - Lower fog sample cost 20");

    [MenuItem("Tools/Foggy Road/Optimize/Apply V - Aggressive tree LOD")]
    public static void ApplyV() => ApplyLevel(22, "V - Aggressive tree LOD");

    [MenuItem("Tools/Foggy Road/Optimize/Apply W - Coarser terrain mesh 3.5")]
    public static void ApplyW() => ApplyLevel(23, "W - Coarser terrain mesh 3.5");

    [MenuItem("Tools/Foggy Road/Optimize/Apply X - 180m terrain tree range")]
    public static void ApplyX() => ApplyLevel(24, "X - 180m terrain tree range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply Y - Stronger tree LOD")]
    public static void ApplyY() => ApplyLevel(25, "Y - Stronger tree LOD");

    [MenuItem("Tools/Foggy Road/Optimize/Apply Z - 28m shadow range")]
    public static void ApplyZ() => ApplyLevel(26, "Z - 28m shadow range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AA - 55m sparse small detail")]
    public static void ApplyAA() => ApplyLevel(27, "AA - 55m sparse small detail");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AB - Lower fog sample cost 16")]
    public static void ApplyAB() => ApplyLevel(28, "AB - Lower fog sample cost 16");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AC - Lower global LOD bias")]
    public static void ApplyAC() => ApplyLevel(29, "AC - Lower global LOD bias");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AD - Coarser terrain mesh 5")]
    public static void ApplyAD() => ApplyLevel(30, "AD - Coarser terrain mesh 5");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AE - 24m shadow range")]
    public static void ApplyAE() => ApplyLevel(31, "AE - 24m shadow range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AF - 30m sparse ground detail")]
    public static void ApplyAF() => ApplyLevel(32, "AF - 30m sparse ground detail");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AG - Lower fog sample cost 12")]
    public static void ApplyAG() => ApplyLevel(33, "AG - Lower fog sample cost 12");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AH - Slower fog culling refresh 0.4s")]
    public static void ApplyAH() => ApplyLevel(34, "AH - Slower fog culling refresh 0.4s");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AI - Slower fog culling refresh 0.6s")]
    public static void ApplyAI() => ApplyLevel(35, "AI - Slower fog culling refresh 0.6s");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AJ - 150m aggressive fog culling")]
    public static void ApplyAJ() => ApplyLevel(36, "AJ - 150m aggressive fog culling");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AK - 20m shadow range")]
    public static void ApplyAK() => ApplyLevel(37, "AK - 20m shadow range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AL - 140m aggressive tree range")]
    public static void ApplyAL() => ApplyLevel(38, "AL - 140m aggressive tree range");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AM - Lower fog sample cost 8")]
    public static void ApplyAM() => ApplyLevel(39, "AM - Lower fog sample cost 8");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AN - URP render scale 0.9")]
    public static void ApplyAN() => ApplyLevel(40, "AN - URP render scale 0.9");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AO - URP render scale 0.8")]
    public static void ApplyAO() => ApplyLevel(41, "AO - URP render scale 0.8");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AP - URP shadow distance 30m")]
    public static void ApplyAP() => ApplyLevel(42, "AP - URP shadow distance 30m");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AQ - URP shadow distance 24m")]
    public static void ApplyAQ() => ApplyLevel(43, "AQ - URP shadow distance 24m");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AR - Disable URP HDR")]
    public static void ApplyAR() => ApplyLevel(44, "AR - Disable URP HDR");

    [MenuItem("Tools/Foggy Road/Optimize/Apply AS - Force MSAA off")]
    public static void ApplyAS() => ApplyLevel(45, "AS - Force MSAA off");

    [MenuItem("Tools/Foggy Road/Optimize/Remove O+ - Return to N")]
    public static void RemovePostN() => ReturnToN();

    [MenuItem("Tools/Foggy Road/Optimize/Revert All")]
    public static void RevertAll()
    {
        FoggyRoadOptimizationBackup backup = LoadBackup();
        if (backup == null || !backup.hasBackup)
        {
            EditorUtility.DisplayDialog("Foggy Road Optimize", "No optimization backup is available.", "OK");
            return;
        }

        QualitySettings.lodBias = backup.lodBias;
        QualitySettings.shadowDistance = backup.shadowDistance;
        QualitySettings.terrainDetailDensityScale = backup.terrainDetailDensityScale;
        QualitySettings.terrainDetailDistance = backup.terrainDetailDistance;
        QualitySettings.terrainTreeDistance = backup.terrainTreeDistance;
        QualitySettings.terrainBillboardStart = backup.terrainBillboardStart;

        RestoreMaterials(backup);
        RestoreTerrains(backup);
        RestoreFogMaterials(backup);
        RemoveFoliageOptimizers();

        if (backup.hasAdvancedBackup)
        {
            QualitySettings.terrainPixelError = backup.terrainPixelError;
            QualitySettings.shadowCascades = backup.shadowCascades;
            SetRendererFeatureActive(backup.screenSpaceAmbientOcclusionFeature as ScriptableRendererFeature,
                                     backup.screenSpaceAmbientOcclusionActive);
        }

        RestorePipeline(backup);

        backup.hasBackup = false;
        EditorUtility.SetDirty(backup);
        SaveChanges();
        Debug.Log("Foggy Road Optimize reverted to the captured settings.");
    }

    private static void ReturnToN()
    {
        FoggyRoadOptimizationBackup backup = LoadBackup();
        if (backup == null || !backup.hasBackup)
        {
            EditorUtility.DisplayDialog("Foggy Road Optimize", "Apply a level before returning to N.", "OK");
            return;
        }

        ResetPostNOverrides(backup);
        ApplyLevel(14, "N baseline; removed O and later");
    }

    private static void ApplyLevel(int level, string label)
    {
        FoggyRoadOptimizationBackup backup = GetOrCaptureBackup();
        if (backup == null) return;

        ApplyInstancing(backup);

        if (level >= 2)
        {
            // The reference fog fully hides this range, while the original range is 5 km.
            QualitySettings.terrainTreeDistance = Mathf.Min(backup.terrainTreeDistance, 300f);
            QualitySettings.terrainBillboardStart = Mathf.Min(backup.terrainBillboardStart, 140f);
        }

        if (level >= 3)
        {
            QualitySettings.lodBias = Mathf.Min(backup.lodBias, 1.25f);
            for (int i = 0; i < backup.terrains.Count; i++)
            {
                Terrain terrain = backup.terrains[i];
                if (terrain == null) continue;
                Undo.RecordObject(terrain, "Foggy Road tree LOD optimization");
                terrain.treeLODBiasMultiplier = Mathf.Min(backup.terrainTreeLodBias[i], 0.9f);
                terrain.treeMaximumFullLODCount = Mathf.Min(backup.terrainMaximumFullLodTrees[i], 800);
                EditorUtility.SetDirty(terrain);
            }
        }

        if (level >= 4)
        {
            QualitySettings.terrainDetailDistance = Mathf.Min(backup.terrainDetailDistance, 55f);
            QualitySettings.terrainDetailDensityScale = Mathf.Min(backup.terrainDetailDensityScale, 0.8f);
        }

        if (level >= 5)
            QualitySettings.shadowDistance = Mathf.Min(backup.shadowDistance, 42f);

        if (level >= 6)
        {
            for (int i = 0; i < backup.fogMaterials.Count; i++)
            {
                Material material = backup.fogMaterials[i];
                if (material == null) continue;
                Undo.RecordObject(material, "Foggy Road fog optimization");
                material.SetFloat("_Steps", Mathf.Min(backup.fogSteps[i], 32f));
                EditorUtility.SetDirty(material);
            }
        }

        if (level >= 7)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.limitDistantFoliageShadows = true;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 8)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.cullFogHiddenFoliage = true;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 9)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.limitDistantRockShadows = true;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 10)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.cullSmallFoliageEarlier = true;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 11)
        {
            CaptureAdvancedBackup(backup);
            QualitySettings.terrainPixelError = Mathf.Max(backup.terrainPixelError, 2.25f);
        }

        if (level >= 12)
        {
            CaptureAdvancedBackup(backup);
            QualitySettings.shadowCascades = Mathf.Min(backup.shadowCascades, 1);
        }

        if (level >= 13)
        {
            for (int i = 0; i < backup.fogMaterials.Count; i++)
            {
                Material material = backup.fogMaterials[i];
                if (material == null) continue;
                Undo.RecordObject(material, "Foggy Road fog optimization");
                material.SetFloat("_Steps", Mathf.Min(backup.fogSteps[i], 24f));
                EditorUtility.SetDirty(material);
            }
        }

        if (level >= 14)
        {
            CaptureAdvancedBackup(backup);
            SetRendererFeatureActive(backup.screenSpaceAmbientOcclusionFeature as ScriptableRendererFeature, false);
        }

        if (level >= 15)
        {
            QualitySettings.terrainTreeDistance = Mathf.Min(backup.terrainTreeDistance, 240f);
            QualitySettings.terrainBillboardStart = Mathf.Min(backup.terrainBillboardStart, 110f);
        }

        if (level >= 16)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.fogCullDistance = 185f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 17)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.smallFoliageCullDistance = 80f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 18)
            QualitySettings.shadowDistance = Mathf.Min(backup.shadowDistance, 34f);

        if (level >= 19)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.foliageShadowDistance = 20f;
            optimizer.rockShadowDistance = 24f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 20)
        {
            QualitySettings.terrainDetailDistance = Mathf.Min(backup.terrainDetailDistance, 42f);
            QualitySettings.terrainDetailDensityScale = Mathf.Min(backup.terrainDetailDensityScale, 0.65f);
        }

        if (level >= 21)
        {
            for (int i = 0; i < backup.fogMaterials.Count; i++)
            {
                Material material = backup.fogMaterials[i];
                if (material == null) continue;
                Undo.RecordObject(material, "Foggy Road fog optimization");
                material.SetFloat("_Steps", Mathf.Min(backup.fogSteps[i], 20f));
                EditorUtility.SetDirty(material);
            }
        }

        if (level >= 22)
        {
            QualitySettings.lodBias = Mathf.Min(backup.lodBias, 1f);
            for (int i = 0; i < backup.terrains.Count; i++)
            {
                Terrain terrain = backup.terrains[i];
                if (terrain == null) continue;
                Undo.RecordObject(terrain, "Foggy Road tree LOD optimization");
                terrain.treeLODBiasMultiplier = Mathf.Min(backup.terrainTreeLodBias[i], 0.72f);
                terrain.treeMaximumFullLODCount = Mathf.Min(backup.terrainMaximumFullLodTrees[i], 500);
                EditorUtility.SetDirty(terrain);
            }
        }

        if (level >= 23)
        {
            CaptureAdvancedBackup(backup);
            QualitySettings.terrainPixelError = Mathf.Max(backup.terrainPixelError, 3.5f);
        }

        if (level >= 24)
        {
            QualitySettings.terrainTreeDistance = Mathf.Min(backup.terrainTreeDistance, 180f);
            QualitySettings.terrainBillboardStart = Mathf.Min(backup.terrainBillboardStart, 85f);
        }

        if (level >= 25)
        {
            QualitySettings.lodBias = Mathf.Min(backup.lodBias, 0.85f);
            for (int i = 0; i < backup.terrains.Count; i++)
            {
                Terrain terrain = backup.terrains[i];
                if (terrain == null) continue;
                Undo.RecordObject(terrain, "Foggy Road stronger tree LOD");
                terrain.treeLODBiasMultiplier = Mathf.Min(backup.terrainTreeLodBias[i], 0.65f);
                terrain.treeMaximumFullLODCount = Mathf.Min(backup.terrainMaximumFullLodTrees[i], 25);
                EditorUtility.SetDirty(terrain);
            }
        }

        if (level >= 26)
            QualitySettings.shadowDistance = Mathf.Min(backup.shadowDistance, 28f);

        if (level >= 27)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.smallFoliageCullDistance = 55f;
            EditorUtility.SetDirty(optimizer);
            QualitySettings.terrainDetailDistance = Mathf.Min(backup.terrainDetailDistance, 38f);
            QualitySettings.terrainDetailDensityScale = Mathf.Min(backup.terrainDetailDensityScale, 0.5f);
        }

        if (level >= 28)
        {
            for (int i = 0; i < backup.fogMaterials.Count; i++)
            {
                Material material = backup.fogMaterials[i];
                if (material == null) continue;
                Undo.RecordObject(material, "Foggy Road fog optimization");
                material.SetFloat("_Steps", Mathf.Min(backup.fogSteps[i], 16f));
                EditorUtility.SetDirty(material);
            }
        }

        if (level >= 29)
            QualitySettings.lodBias = Mathf.Min(backup.lodBias, 0.8f);

        if (level >= 30)
        {
            CaptureAdvancedBackup(backup);
            QualitySettings.terrainPixelError = Mathf.Max(backup.terrainPixelError, 5f);
        }

        if (level >= 31)
        {
            QualitySettings.shadowDistance = Mathf.Min(backup.shadowDistance, 24f);
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.foliageShadowDistance = 16f;
            optimizer.rockShadowDistance = 20f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 32)
        {
            QualitySettings.terrainDetailDistance = Mathf.Min(backup.terrainDetailDistance, 30f);
            QualitySettings.terrainDetailDensityScale = Mathf.Min(backup.terrainDetailDensityScale, 0.4f);
        }

        if (level >= 33)
        {
            for (int i = 0; i < backup.fogMaterials.Count; i++)
            {
                Material material = backup.fogMaterials[i];
                if (material == null) continue;
                Undo.RecordObject(material, "Foggy Road fog optimization");
                material.SetFloat("_Steps", Mathf.Min(backup.fogSteps[i], 12f));
                EditorUtility.SetDirty(material);
            }
        }

        if (level >= 34)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.refreshInterval = 0.4f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 35)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.refreshInterval = 0.6f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 36)
        {
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.fogCullDistance = 150f;
            optimizer.smallFoliageCullDistance = 45f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 37)
        {
            QualitySettings.shadowDistance = Mathf.Min(backup.shadowDistance, 20f);
            FoggyRoadFoliageOptimization optimizer = EnsureFoliageOptimizer();
            optimizer.foliageShadowDistance = 14f;
            optimizer.rockShadowDistance = 18f;
            EditorUtility.SetDirty(optimizer);
        }

        if (level >= 38)
        {
            QualitySettings.terrainTreeDistance = Mathf.Min(backup.terrainTreeDistance, 140f);
            QualitySettings.terrainBillboardStart = Mathf.Min(backup.terrainBillboardStart, 70f);
            for (int i = 0; i < backup.terrains.Count; i++)
            {
                Terrain terrain = backup.terrains[i];
                if (terrain == null) continue;
                Undo.RecordObject(terrain, "Foggy Road aggressive tree optimization");
                terrain.treeLODBiasMultiplier = Mathf.Min(backup.terrainTreeLodBias[i], 0.5f);
                terrain.treeMaximumFullLODCount = Mathf.Min(backup.terrainMaximumFullLodTrees[i], 10);
                EditorUtility.SetDirty(terrain);
            }
        }

        if (level >= 39)
        {
            for (int i = 0; i < backup.fogMaterials.Count; i++)
            {
                Material material = backup.fogMaterials[i];
                if (material == null) continue;
                Undo.RecordObject(material, "Foggy Road fog optimization");
                material.SetFloat("_Steps", Mathf.Min(backup.fogSteps[i], 8f));
                EditorUtility.SetDirty(material);
            }
        }

        if (level >= 40)
        {
            CapturePipelineBackup(backup);
            UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
            if (pipeline != null)
            {
                Undo.RecordObject(pipeline, "Foggy Road GPU render scale optimization");
                pipeline.renderScale = Mathf.Min(backup.pipelineRenderScale, 0.9f);
                EditorUtility.SetDirty(pipeline);
            }
        }

        if (level >= 41)
        {
            CapturePipelineBackup(backup);
            UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
            if (pipeline != null)
            {
                Undo.RecordObject(pipeline, "Foggy Road GPU render scale optimization");
                pipeline.renderScale = Mathf.Min(backup.pipelineRenderScale, 0.8f);
                EditorUtility.SetDirty(pipeline);
            }
        }

        if (level >= 42)
        {
            CapturePipelineBackup(backup);
            UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
            if (pipeline != null)
            {
                Undo.RecordObject(pipeline, "Foggy Road URP shadow optimization");
                pipeline.shadowDistance = Mathf.Min(backup.pipelineShadowDistance, 30f);
                EditorUtility.SetDirty(pipeline);
            }
        }

        if (level >= 43)
        {
            CapturePipelineBackup(backup);
            UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
            if (pipeline != null)
            {
                Undo.RecordObject(pipeline, "Foggy Road URP shadow optimization");
                pipeline.shadowDistance = Mathf.Min(backup.pipelineShadowDistance, 24f);
                EditorUtility.SetDirty(pipeline);
            }
        }

        if (level >= 44)
        {
            CapturePipelineBackup(backup);
            UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
            if (pipeline != null)
            {
                Undo.RecordObject(pipeline, "Foggy Road URP HDR optimization");
                pipeline.supportsHDR = false;
                EditorUtility.SetDirty(pipeline);
            }
        }

        if (level >= 45)
        {
            CapturePipelineBackup(backup);
            UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
            if (pipeline != null)
            {
                Undo.RecordObject(pipeline, "Foggy Road URP MSAA optimization");
                pipeline.msaaSampleCount = 1;
                EditorUtility.SetDirty(pipeline);
            }
        }

        SaveChanges();
        Debug.Log("Foggy Road Optimize applied " + label + ". Use Revert All to restore the captured settings.");
    }

    private static FoggyRoadOptimizationBackup GetOrCaptureBackup()
    {
        FoggyRoadOptimizationBackup backup = LoadBackup();
        if (backup == null)
        {
            backup = ScriptableObject.CreateInstance<FoggyRoadOptimizationBackup>();
            AssetDatabase.CreateAsset(backup, BackupPath);
        }

        string activeScenePath = SceneManager.GetActiveScene().path;
        if (backup.hasBackup && backup.scenePath != activeScenePath)
        {
            EditorUtility.DisplayDialog(
                "Foggy Road Optimize",
                "Revert the previous scene before applying this optimizer to another scene.",
                "OK");
            return null;
        }

        if (!backup.hasBackup)
            CaptureBackup(backup, activeScenePath);

        return backup;
    }

    private static FoggyRoadOptimizationBackup LoadBackup()
    {
        return AssetDatabase.LoadAssetAtPath<FoggyRoadOptimizationBackup>(BackupPath);
    }

    private static void CaptureBackup(FoggyRoadOptimizationBackup backup, string scenePath)
    {
        backup.hasBackup = true;
        backup.scenePath = scenePath;
        backup.lodBias = QualitySettings.lodBias;
        backup.shadowDistance = QualitySettings.shadowDistance;
        backup.terrainDetailDensityScale = QualitySettings.terrainDetailDensityScale;
        backup.terrainDetailDistance = QualitySettings.terrainDetailDistance;
        backup.terrainTreeDistance = QualitySettings.terrainTreeDistance;
        backup.terrainBillboardStart = QualitySettings.terrainBillboardStart;

        backup.materials.Clear();
        backup.materialInstancing.Clear();
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            foreach (Material material in renderer.sharedMaterials)
                CaptureMaterial(backup, material);
        }

        backup.terrains.Clear();
        backup.terrainDrawInstanced.Clear();
        backup.terrainTreeLodBias.Clear();
        backup.terrainMaximumFullLodTrees.Clear();
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            backup.terrains.Add(terrain);
            backup.terrainDrawInstanced.Add(terrain.drawInstanced);
            backup.terrainTreeLodBias.Add(terrain.treeLODBiasMultiplier);
            backup.terrainMaximumFullLodTrees.Add(terrain.treeMaximumFullLODCount);
        }

        backup.fogMaterials.Clear();
        backup.fogSteps.Clear();
        foreach (Material material in FindActiveFogMaterials())
        {
            backup.fogMaterials.Add(material);
            backup.fogSteps.Add(material.GetFloat("_Steps"));
        }

        EditorUtility.SetDirty(backup);
        AssetDatabase.SaveAssets();
    }

    private static void CaptureMaterial(FoggyRoadOptimizationBackup backup, Material material)
    {
        if (material == null || backup.materials.Contains(material)) return;
        backup.materials.Add(material);
        backup.materialInstancing.Add(material.enableInstancing);
    }

    private static void ApplyInstancing(FoggyRoadOptimizationBackup backup)
    {
        for (int i = 0; i < backup.materials.Count; i++)
        {
            Material material = backup.materials[i];
            if (material == null || !material.shader.isSupported) continue;
            Undo.RecordObject(material, "Foggy Road renderer instancing");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        for (int i = 0; i < backup.terrains.Count; i++)
        {
            Terrain terrain = backup.terrains[i];
            if (terrain == null) continue;
            Undo.RecordObject(terrain, "Foggy Road terrain instancing");
            terrain.drawInstanced = true;
            EditorUtility.SetDirty(terrain);
        }
    }

    private static List<Material> FindActiveFogMaterials()
    {
        var fogMaterials = new List<Material>();
        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererDataPath);
        if (rendererData == null) return fogMaterials;

        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature == null) continue;
            var serializedFeature = new SerializedObject(feature);
            SerializedProperty materialProperty = serializedFeature.FindProperty("passMaterial");
            Material material = materialProperty != null ? materialProperty.objectReferenceValue as Material : null;
            if (material != null && material.shader != null && material.shader.name == FogShaderName)
                fogMaterials.Add(material);
        }

        return fogMaterials;
    }

    private static void RestoreMaterials(FoggyRoadOptimizationBackup backup)
    {
        for (int i = 0; i < backup.materials.Count; i++)
        {
            Material material = backup.materials[i];
            if (material == null) continue;
            material.enableInstancing = backup.materialInstancing[i];
            EditorUtility.SetDirty(material);
        }
    }

    private static void RestoreTerrains(FoggyRoadOptimizationBackup backup)
    {
        for (int i = 0; i < backup.terrains.Count; i++)
        {
            Terrain terrain = backup.terrains[i];
            if (terrain == null) continue;
            terrain.drawInstanced = backup.terrainDrawInstanced[i];
            terrain.treeLODBiasMultiplier = backup.terrainTreeLodBias[i];
            terrain.treeMaximumFullLODCount = backup.terrainMaximumFullLodTrees[i];
            EditorUtility.SetDirty(terrain);
        }
    }

    private static void RestoreFogMaterials(FoggyRoadOptimizationBackup backup)
    {
        for (int i = 0; i < backup.fogMaterials.Count; i++)
        {
            Material material = backup.fogMaterials[i];
            if (material == null) continue;
            material.SetFloat("_Steps", backup.fogSteps[i]);
            EditorUtility.SetDirty(material);
        }
    }

    private static void CaptureAdvancedBackup(FoggyRoadOptimizationBackup backup)
    {
        if (backup.hasAdvancedBackup) return;

        backup.hasAdvancedBackup = true;
        backup.terrainPixelError = QualitySettings.terrainPixelError;
        backup.shadowCascades = QualitySettings.shadowCascades;
        ScriptableRendererFeature ssao = FindScreenSpaceAmbientOcclusion();
        backup.screenSpaceAmbientOcclusionFeature = ssao;
        backup.screenSpaceAmbientOcclusionActive = GetRendererFeatureActive(ssao);
        EditorUtility.SetDirty(backup);
        AssetDatabase.SaveAssets();
    }

    private static void CapturePipelineBackup(FoggyRoadOptimizationBackup backup)
    {
        if (backup.hasPipelineBackup) return;
        UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
        if (pipeline == null) return;

        backup.hasPipelineBackup = true;
        backup.pipelineRenderScale = pipeline.renderScale;
        backup.pipelineShadowDistance = pipeline.shadowDistance;
        backup.pipelineSupportsHDR = pipeline.supportsHDR;
        backup.pipelineMsaaSampleCount = pipeline.msaaSampleCount;
        EditorUtility.SetDirty(backup);
        AssetDatabase.SaveAssets();
    }

    private static UniversalRenderPipelineAsset LoadPipelineAsset()
    {
        return AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
    }

    private static void RestorePipeline(FoggyRoadOptimizationBackup backup)
    {
        if (!backup.hasPipelineBackup) return;
        UniversalRenderPipelineAsset pipeline = LoadPipelineAsset();
        if (pipeline == null) return;

        Undo.RecordObject(pipeline, "Foggy Road restore URP settings");
        pipeline.renderScale = backup.pipelineRenderScale;
        pipeline.shadowDistance = backup.pipelineShadowDistance;
        pipeline.supportsHDR = backup.pipelineSupportsHDR;
        pipeline.msaaSampleCount = backup.pipelineMsaaSampleCount;
        EditorUtility.SetDirty(pipeline);
    }

    private static ScriptableRendererFeature FindScreenSpaceAmbientOcclusion()
    {
        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererDataPath);
        if (rendererData == null) return null;

        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.GetType().Name == "ScreenSpaceAmbientOcclusion")
                return feature;
        }

        return null;
    }

    private static bool GetRendererFeatureActive(ScriptableRendererFeature feature)
    {
        if (feature == null) return false;
        SerializedProperty active = new SerializedObject(feature).FindProperty("m_Active");
        return active != null && active.boolValue;
    }

    private static void SetRendererFeatureActive(ScriptableRendererFeature feature, bool active)
    {
        if (feature == null) return;
        Undo.RecordObject(feature, "Foggy Road renderer feature optimization");
        var serializedFeature = new SerializedObject(feature);
        SerializedProperty activeProperty = serializedFeature.FindProperty("m_Active");
        if (activeProperty == null) return;
        activeProperty.boolValue = active;
        serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
    }

    private static FoggyRoadFoliageOptimization EnsureFoliageOptimizer()
    {
        FoggyRoadFoliageOptimization optimizer = Object.FindFirstObjectByType<FoggyRoadFoliageOptimization>();
        if (optimizer != null) return optimizer;

        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            throw new System.InvalidOperationException("A camera is required for foliage optimization.");

        optimizer = Undo.AddComponent<FoggyRoadFoliageOptimization>(camera.gameObject);
        optimizer.name = "Foggy Road Foliage Optimization";
        return optimizer;
    }

    private static void RemoveFoliageOptimizers()
    {
        foreach (FoggyRoadFoliageOptimization optimizer in
                 Object.FindObjectsByType<FoggyRoadFoliageOptimization>(FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(optimizer);
    }

    private static void ResetPostNOverrides(FoggyRoadOptimizationBackup backup)
    {
        // Restore the exact N baseline before reapplying N. ApplyLevel uses Min/Max,
        // so it cannot itself increase a value that a later level lowered.
        QualitySettings.lodBias = Mathf.Min(backup.lodBias, 1.25f);
        QualitySettings.shadowDistance = Mathf.Min(backup.shadowDistance, 42f);
        QualitySettings.terrainDetailDensityScale = Mathf.Min(backup.terrainDetailDensityScale, 0.8f);
        QualitySettings.terrainDetailDistance = Mathf.Min(backup.terrainDetailDistance, 55f);
        QualitySettings.terrainTreeDistance = Mathf.Min(backup.terrainTreeDistance, 300f);
        QualitySettings.terrainBillboardStart = Mathf.Min(backup.terrainBillboardStart, 140f);
        QualitySettings.terrainPixelError = Mathf.Max(backup.terrainPixelError, 2.25f);
        RestorePipeline(backup);

        for (int i = 0; i < backup.terrains.Count; i++)
        {
            Terrain terrain = backup.terrains[i];
            if (terrain == null) continue;
            Undo.RecordObject(terrain, "Foggy Road remove post-N optimizations");
            terrain.treeLODBiasMultiplier = Mathf.Min(backup.terrainTreeLodBias[i], 0.9f);
            terrain.treeMaximumFullLODCount = Mathf.Min(backup.terrainMaximumFullLodTrees[i], 800);
            EditorUtility.SetDirty(terrain);
        }

        for (int i = 0; i < backup.fogMaterials.Count; i++)
        {
            Material material = backup.fogMaterials[i];
            if (material == null) continue;
            Undo.RecordObject(material, "Foggy Road remove post-N optimizations");
            material.SetFloat("_Steps", Mathf.Min(backup.fogSteps[i], 24f));
            EditorUtility.SetDirty(material);
        }

        foreach (FoggyRoadFoliageOptimization optimizer in
                 Object.FindObjectsByType<FoggyRoadFoliageOptimization>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(optimizer, "Foggy Road remove post-N optimizations");
            optimizer.foliageShadowDistance = 28f;
            optimizer.rockShadowDistance = 32f;
            optimizer.fogCullDistance = 235f;
            optimizer.smallFoliageCullDistance = 105f;
            optimizer.refreshInterval = 0.2f;
            EditorUtility.SetDirty(optimizer);
        }
    }

    private static void SaveChanges()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
