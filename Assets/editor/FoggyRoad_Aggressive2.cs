// AGRESIF 2 - ucgen sayisi saldirisi
// Menu: Tools -> Foggy Road -> AGRESIF2 ...
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FoggyRoad_Aggressive2
{
    // ================= A) Agac LOD esiklerini sikilastir =================
    [MenuItem("Tools/Foggy Road/AGRESIF2 A - Agac LOD Esiklerini Sikilastir")]
    public static void LodSikilastir()
    {
        // Yeni esikler: sayi BUYUDUKCE LOD daha ERKEN degisir (daha ucuz)
        float[] hedef = { 0.70f, 0.42f, 0.20f, 0.06f };

        var islenen = new HashSet<GameObject>();
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var proto in t.terrainData.treePrototypes)
            {
                var pf = proto.prefab;
                if (pf == null || islenen.Contains(pf)) continue;
                islenen.Add(pf);

                var lg = pf.GetComponentInChildren<LODGroup>();
                if (lg == null) { Debug.LogWarning("  " + pf.name + ": LODGroup yok, atlandi"); continue; }

                var lods = lg.GetLODs();
                var eski = "";
                for (int i = 0; i < lods.Length; i++) eski += lods[i].screenRelativeTransitionHeight.ToString("0.###") + " ";

                for (int i = 0; i < lods.Length; i++)
                {
                    float h = i < hedef.Length ? hedef[i] : hedef[hedef.Length - 1];
                    // son LOD'u her zaman en kucuk tut
                    if (i == lods.Length - 1) h = 0.06f;
                    lods[i].screenRelativeTransitionHeight = h;
                    lods[i].fadeTransitionWidth = 0f;
                }
                lg.SetLODs(lods);
                lg.RecalculateBounds();
                EditorUtility.SetDirty(lg);
                PrefabUtility.SavePrefabAsset(pf);

                var yeni = "";
                for (int i = 0; i < lods.Length; i++) yeni += lods[i].screenRelativeTransitionHeight.ToString("0.###") + " ";
                Debug.Log("[FoggyRoad] " + pf.name + " LOD esikleri:  [" + eski.Trim() + "]  ->  [" + yeni.Trim() + "]");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[FoggyRoad] AGRESIF2-A TAMAM.");
    }

    // ================= B) Bariyer/prop mesafe culling kur =================
    [MenuItem("Tools/Foggy Road/AGRESIF2 B - Bariyer Mesafe Culling Kur")]
    public static void MesafeCulling()
    {
        int gLayer = LayerEkle("Guardrail");
        int pLayer = LayerEkle("Props");
        if (gLayer < 0) { Debug.LogError("[FoggyRoad] Bos layer kalmamis, Guardrail eklenemedi."); return; }

        // 1) yol altindaki GRPRO_* objelerini Guardrail layer'ina tasi
        var kok = GameObject.Find("yol");
        int tasinan = 0;
        if (kok != null)
        {
            foreach (var tf in kok.GetComponentsInChildren<Transform>(true))
            {
                if (!tf.name.StartsWith("GRPRO_")) continue;
                if (tf.gameObject.layer == gLayer) continue;
                Undo.RecordObject(tf.gameObject, "Layer degistir");
                tf.gameObject.layer = gLayer;
                EditorUtility.SetDirty(tf.gameObject);
                tasinan++;
            }
        }
        Debug.Log("[FoggyRoad] " + tasinan + " bariyer objesi '" + LayerMask.LayerToName(gLayer) + "' layer'ina tasindi.");

        // 2) elektrik diregi vb. proplari Props layer'ina tasi
        int prop = 0;
        if (pLayer >= 0)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!go.name.ToLower().Contains("elektrik")) continue;
                foreach (var tf in go.GetComponentsInChildren<Transform>(true))
                {
                    if (tf.gameObject.layer == pLayer) continue;
                    Undo.RecordObject(tf.gameObject, "Layer degistir");
                    tf.gameObject.layer = pLayer;
                    EditorUtility.SetDirty(tf.gameObject);
                    prop++;
                }
            }
            Debug.Log("[FoggyRoad] " + prop + " prop objesi 'Props' layer'ina tasindi.");
        }

        // 3) aktif kameralara culling script'ini ekle
        int kamera = 0;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var comp = c.GetComponent<FoggyRoad_LayerCulling>();
            if (comp == null) comp = Undo.AddComponent<FoggyRoad_LayerCulling>(c.gameObject);
            comp.bariyerLayer = "Guardrail";
            comp.bariyerMesafe = 95f;
            comp.propLayer = "Props";
            comp.propMesafe = 140f;
            comp.Uygula();
            EditorUtility.SetDirty(comp);
            kamera++;
            Debug.Log("[FoggyRoad] Kamera '" + c.name + "' -> bariyer 95m, prop 140m sonrasi cizilmeyecek.");
        }
        Debug.Log("[FoggyRoad] AGRESIF2-B TAMAM. " + kamera + " kamera ayarlandi. Ctrl+S.");
    }

    static int LayerEkle(string ad)
    {
        int mevcut = LayerMask.NameToLayer(ad);
        if (mevcut >= 0) return mevcut;

        var tm = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tm == null || tm.Length == 0) return -1;
        var so = new SerializedObject(tm[0]);
        var layers = so.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)   // 0-7 Unity'nin
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(el.stringValue)) continue;
            el.stringValue = ad;
            so.ApplyModifiedProperties();
            Debug.Log("[FoggyRoad] Layer " + i + " = '" + ad + "' olusturuldu.");
            return i;
        }
        return -1;
    }

    // ================= C) Agaclari seyrelt =================
    [MenuItem("Tools/Foggy Road/AGRESIF2 C - Agaclari Seyrelt (%35 sil)")]
    public static void Seyrelt35() { Seyrelt(0.35f); }

    [MenuItem("Tools/Foggy Road/AGRESIF2 C2 - Agaclari Seyrelt (%50 sil)")]
    public static void Seyrelt50() { Seyrelt(0.50f); }

    static void Seyrelt(float oran)
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var td = t.terrainData;
            var eski = td.treeInstances;

            bool ok = EditorUtility.DisplayDialog("Agac Seyreltme",
                "Terrain: " + t.name + "\nMevcut agac: " + eski.Length +
                "\n\n%" + (oran * 100) + " silinecek -> kalan yaklasik " + Mathf.RoundToInt(eski.Length * (1 - oran)) +
                "\n\nBU GERI ALINAMAZ. Yedegin var mi?",
                "Evet, sil", "Iptal");
            if (!ok) { Debug.Log("[FoggyRoad] Seyreltme iptal edildi."); return; }

            // Terrain verisinin yedegini al
            string kaynak = AssetDatabase.GetAssetPath(td);
            string yedek = System.IO.Path.GetDirectoryName(kaynak) + "/" +
                           System.IO.Path.GetFileNameWithoutExtension(kaynak) + "_YEDEK.asset";
            if (!System.IO.File.Exists(yedek))
            {
                AssetDatabase.CopyAsset(kaynak, yedek);
                Debug.Log("[FoggyRoad] Terrain yedegi olusturuldu: " + yedek);
            }

            var rng = new System.Random(12345);
            var yeni = new List<TreeInstance>(eski.Length);
            foreach (var inst in eski)
                if (rng.NextDouble() >= oran) yeni.Add(inst);

            Undo.RegisterCompleteObjectUndo(td, "Agac seyrelt");
            td.SetTreeInstances(yeni.ToArray(), true);
            EditorUtility.SetDirty(td);
            t.Flush();

            Debug.Log("[FoggyRoad] '" + t.name + "' agac sayisi: " + eski.Length + " -> " + yeni.Count);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[FoggyRoad] AGRESIF2-C TAMAM.");
    }

    [MenuItem("Tools/Foggy Road/AGRESIF2 C3 - Seyreltmeyi GERI AL (yedekten)")]
    public static void SeyreltmeGeriAl()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string kaynak = AssetDatabase.GetAssetPath(t.terrainData);
            string yedek = System.IO.Path.GetDirectoryName(kaynak) + "/" +
                           System.IO.Path.GetFileNameWithoutExtension(kaynak) + "_YEDEK.asset";
            if (!System.IO.File.Exists(yedek)) { Debug.LogWarning("[FoggyRoad] Yedek yok: " + yedek); continue; }

            var yedekTd = AssetDatabase.LoadAssetAtPath<TerrainData>(yedek);
            if (yedekTd == null) continue;
            Undo.RegisterCompleteObjectUndo(t.terrainData, "Seyreltme geri al");
            t.terrainData.SetTreeInstances(yedekTd.treeInstances, true);
            EditorUtility.SetDirty(t.terrainData);
            t.Flush();
            Debug.Log("[FoggyRoad] '" + t.name + "' agaclari yedekten geri yuklendi: " + yedekTd.treeInstances.Length);
        }
        AssetDatabase.SaveAssets();
    }

    // ================= D) Terrain son kisma =================
    [MenuItem("Tools/Foggy Road/AGRESIF2 D - Terrain Son Kisma")]
    public static void TerrainSon()
    {
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(t, "Terrain son kisma");
            t.treeDistance = 120f;
            t.treeBillboardDistance = 35f;
            t.treeMaximumFullLODCount = 12;
            t.treeCrossFadeLength = 6f;
            t.detailObjectDistance = 35f;
            t.detailObjectDensity = 0.15f;
            t.heightmapPixelError = 35f;
            t.basemapDistance = 150f;
            EditorUtility.SetDirty(t);
            Debug.Log("[FoggyRoad] '" + t.name + "' -> treeDist=120 billboard=35 maxLOD=12 detailDist=35 pixelError=35");
        }
        Debug.Log("[FoggyRoad] AGRESIF2-D TAMAM. Ctrl+S.");
    }

    // ================= E) FPS sayaci ekle (build icin) =================
    [MenuItem("Tools/Foggy Road/AGRESIF2 E - Oyuna FPS Sayaci Ekle")]
    public static void FpsEkle()
    {
        var mevcut = Object.FindFirstObjectByType<FoggyRoad_FpsCounter>();
        if (mevcut != null) { Debug.Log("[FoggyRoad] FPS sayaci zaten var: " + mevcut.name); Selection.activeObject = mevcut.gameObject; return; }

        var go = new GameObject("FPS Sayaci");
        Undo.RegisterCreatedObjectUndo(go, "FPS sayaci ekle");
        go.AddComponent<FoggyRoad_FpsCounter>();
        Selection.activeObject = go;
        Debug.Log("[FoggyRoad] FPS sayaci eklendi. Build alinca sol ustte gorunecek.");
    }

    // ================= HEPSI =================
    [MenuItem("Tools/Foggy Road/AGRESIF2 -- HEPSINI CALISTIR (seyreltme HARIC) --")]
    public static void Hepsi()
    {
        LodSikilastir();
        MesafeCulling();
        TerrainSon();
        FpsEkle();
        AssetDatabase.SaveAssets();
        Debug.Log("===== [FoggyRoad] AGRESIF2 TAMAM. Ctrl+S yap, Play'e bas, olc. =====");
    }
}
