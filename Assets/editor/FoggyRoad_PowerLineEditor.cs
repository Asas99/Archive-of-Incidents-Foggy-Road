// FoggyRoad_PowerLine icin Inspector + editor-ici otomatik guncelleme.
//
// Watcher burada, runtime dosyasinda degil: boylece runtime script'i tamamen
// editor kodundan arindirilmis olur ve build'de hicbir izi kalmaz.
//
// Otomatik guncelleme neden polling?
//   ObjectChangeEvents dogru yaklasim ama event tiplerini dogru filtrelemek
//   kirilgan ve "bazen guncellenmiyor" en kotu sonuc. [ExecuteAlways] Update()
//   ise editorde guvenilir tetiklenmiyor. Her editor frame'inde 80 direk icin
//   position+rotation karsilastirmasi mikrosaniye seviyesinde - poll bedava.
//
// transform.hasChanged KULLANILMIYOR: baska bir script de onu kullaniyor
// olabilir, false'a cekmek onu bozar. Bunun yerine son deger cache'lenir.
//
// Menu: Tools -> Foggy Road/Kablolari Yeniden Bagla
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ======================================================================
//  Otomatik guncelleme
// ======================================================================
[InitializeOnLoad]
public static class FoggyRoad_PowerLineWatcher
{
    const double MinInterval = 1.0 / 30.0;   // saniyede en fazla 30 rebuild

    static FoggyRoad_PowerLine[] cache;
    static double nextRebuild;

    static FoggyRoad_PowerLineWatcher()
    {
        EditorApplication.update += Tick;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    public static void Refresh()
    {
        cache = Object.FindObjectsByType<FoggyRoad_PowerLine>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None);
    }

    static void OnHierarchyChanged()
    {
        // Direk silinmis / eklenmis / kopyalanmis olabilir.
        Refresh();
        if (cache == null) return;

        for (int i = 0; i < cache.Length; i++)
        {
            FoggyRoad_PowerLine pl = cache[i];
            if (pl == null || !pl.autoUpdateInEditor) continue;
            pl.ResortPoles();
            pl.Rebuild();
        }
    }

    static void OnUndoRedo()
    {
        Refresh();
        if (cache == null) return;
        for (int i = 0; i < cache.Length; i++)
            if (cache[i] != null) cache[i].Rebuild();
    }

    static void Tick()
    {
        // Play modunda poll yok: runtime maliyeti kesinlikle sifir kalsin.
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.timeSinceStartup < nextRebuild) return;

        if (cache == null) Refresh();
        if (cache == null) return;

        bool stale = false;
        for (int i = 0; i < cache.Length; i++)
        {
            FoggyRoad_PowerLine pl = cache[i];
            if (pl == null) { stale = true; continue; }
            if (!pl.autoUpdateInEditor || !pl.isActiveAndEnabled) continue;

            if (pl.IsDirty())
            {
                pl.Rebuild();
                nextRebuild = EditorApplication.timeSinceStartup + MinInterval;
            }
        }

        if (stale) Refresh();
    }

    // ==================================================================
    //  "Her seyi duzelt" kapisi
    // ==================================================================
    [MenuItem("Tools/Foggy Road/Kablolari Yeniden Bagla")]
    public static void YenidenBagla()
    {
        Refresh();
        if (cache == null || cache.Length == 0)
        {
            Debug.LogWarning("[FoggyRoad] Sahnede FoggyRoad_PowerLine bulunamadi.");
            return;
        }

        int toplamDirek = 0;
        for (int i = 0; i < cache.Length; i++)
        {
            FoggyRoad_PowerLine pl = cache[i];
            if (pl == null) continue;

            Undo.RecordObject(pl, "Kablolari yeniden bagla");
            pl.CleanNulls();
            pl.ResortPoles();
            pl.Rebuild();
            toplamDirek += pl.poles.Count;
            EditorUtility.SetDirty(pl);
        }

        Debug.Log($"[FoggyRoad] {cache.Length} kablo hatti yeniden baglandi, " +
                  $"toplam {toplamDirek} direk. Ctrl+Z ile geri alinabilir.");
    }
}

// ======================================================================
//  Ortak yardimcilar (dizici de kullanir)
// ======================================================================
public static class FoggyRoad_PowerLineAssets
{
    public const string KabloMatYolu = "Assets/FoggyRoad/Looks/PowerLine_Black.mat";
    // Klasor adinda Turkce karakter var; kaynak kodu ASCII tutmak icin escape.
    public const string DirekPrefabYolu = "Assets/elektrikdireği/Direk.prefab";

    /// <summary>Windows ters bolu isaretlerini '/' yapar (backslash literal kullanmadan).</summary>
    static string AsciiYol(string yol)
    {
        if (string.IsNullOrEmpty(yol)) return yol;
        return yol.Replace(System.IO.Path.DirectorySeparatorChar, '/')
                  .Replace(System.IO.Path.AltDirectorySeparatorChar, '/');
    }

    /// <summary>Projede hazir bir "Direk" prefab'i varsa bulur (yol degismis olabilir).</summary>
    public static GameObject BulDirekPrefabi()
    {
        GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(DirekPrefabYolu);
        if (g != null) return g;

        string[] guids = AssetDatabase.FindAssets("Direk t:prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (System.IO.Path.GetFileNameWithoutExtension(yol) != "Direk") continue;

            GameObject aday = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            if (aday != null && aday.transform.Find("Anchor_L") != null) return aday;
        }
        return null;
    }

    /// <summary>Siyah kablo materyalini uretir (varsa mevcut olani dondurur).</summary>
    public static Material KabloMateryali()
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(KabloMatYolu);
        if (m != null) return m;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null)
        {
            Debug.LogError("[FoggyRoad] Kablo icin shader bulunamadi.");
            return null;
        }

        KlasorGarantile("Assets/FoggyRoad/Looks");

        m = new Material(sh);
        m.name = "PowerLine_Black";
        RenkAta(m, new Color(0.035f, 0.035f, 0.040f, 1f));
        FloatAta(m, "_Metallic", 0f);
        FloatAta(m, "_Smoothness", 0.25f);
        FloatAta(m, "_Glossiness", 0.25f);

        AssetDatabase.CreateAsset(m, KabloMatYolu);
        AssetDatabase.SaveAssets();
        Debug.Log("[FoggyRoad] Kablo materyali olusturuldu: " + KabloMatYolu);
        return m;
    }

    /// <summary>
    /// FBX'in Prefab Variant'ini uretir ve icine Anchor_L / Anchor_R ekler.
    /// Model prefab'a dogrudan cocuk eklenemedigi icin variant sart. Anchor bir
    /// Transform olunca kullanici Scene view'da surukleyip ince ayar yapabilir.
    /// Idempotent: prefab varsa dokunulmaz.
    /// </summary>
    public static GameObject DirekPrefabi(GameObject fbx, Vector3 anchorSol, Vector3 anchorSag)
    {
        GameObject mevcut = BulDirekPrefabi();
        if (mevcut != null) return mevcut;
        if (fbx == null) return null;

        // Prefab'i FBX'in yanina kaydet: klasor adi ne olursa olsun calisir.
        string fbxYolu = AssetDatabase.GetAssetPath(fbx);
        string hedefYol = DirekPrefabYolu;
        if (!string.IsNullOrEmpty(fbxYolu))
        {
            string klasor = System.IO.Path.GetDirectoryName(fbxYolu);
            if (!string.IsNullOrEmpty(klasor))
                hedefYol = AsciiYol(klasor) + "/Direk.prefab";
        }

        GameObject gecici = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        if (gecici == null) return null;

        try
        {
            AnchorEkle(gecici.transform, "Anchor_L", anchorSol);
            AnchorEkle(gecici.transform, "Anchor_R", anchorSag);

            KlasorGarantile(AsciiYol(System.IO.Path.GetDirectoryName(hedefYol)));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gecici, hedefYol);
            Debug.Log("[FoggyRoad] Direk prefab variant'i olusturuldu: " + hedefYol);
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(gecici);
        }
    }

    static void AnchorEkle(Transform parent, string ad, Vector3 localPos)
    {
        Transform mevcut = parent.Find(ad);
        if (mevcut != null) { mevcut.localPosition = localPos; return; }

        GameObject go = new GameObject(ad);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
    }

    public static void KlasorGarantile(string klasor)
    {
        if (string.IsNullOrEmpty(klasor) || AssetDatabase.IsValidFolder(klasor)) return;

        string[] parcalar = klasor.Split('/');
        string birikim = parcalar[0];
        for (int i = 1; i < parcalar.Length; i++)
        {
            string sonraki = birikim + "/" + parcalar[i];
            if (!AssetDatabase.IsValidFolder(sonraki))
                AssetDatabase.CreateFolder(birikim, parcalar[i]);
            birikim = sonraki;
        }
    }

    static void RenkAta(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    static void FloatAta(Material m, string ad, float v)
    {
        if (m.HasProperty(ad)) m.SetFloat(ad, v);
    }
}

// ======================================================================
//  Inspector
// ======================================================================
[CustomEditor(typeof(FoggyRoad_PowerLine))]
public class FoggyRoad_PowerLineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FoggyRoad_PowerLine pl = (FoggyRoad_PowerLine)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Islemler", EditorStyles.boldLabel);

        int gecerli = 0;
        for (int i = 0; i < pl.poles.Count; i++) if (pl.poles[i] != null) gecerli++;
        int aciklik = Mathf.Max(0, gecerli - 1);
        EditorGUILayout.HelpBox(
            $"{gecerli} direk, {aciklik} aciklik, {aciklik * pl.wireCount} kablo parcasi.\n" +
            "Direkleri Scene view'da elle tasiyabilir, silebilir, araya yenisini " +
            "ekleyebilirsin. Kablo kendini otomatik yeniler.", MessageType.None);

        if (GUILayout.Button("Kabloyu yeniden kur"))
        {
            pl.Rebuild();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Direkleri yeniden sirala"))
        {
            Undo.RecordObject(pl, "Direkleri yeniden sirala");
            pl.ResortPoles();
            pl.Rebuild();
            EditorUtility.SetDirty(pl);
        }

        if (GUILayout.Button("Direkleri cocuklardan topla"))
        {
            Undo.RecordObject(pl, "Direkleri topla");
            pl.CollectPolesFromChildren(null);
            pl.Rebuild();
            EditorUtility.SetDirty(pl);
            Debug.Log($"[FoggyRoad] {pl.poles.Count} direk toplandi.");
        }

        if (pl.wireMaterial == null && GUILayout.Button("Kablo materyalini olustur"))
        {
            Undo.RecordObject(pl, "Kablo materyali");
            pl.wireMaterial = FoggyRoad_PowerLineAssets.KabloMateryali();
            pl.Rebuild();
            EditorUtility.SetDirty(pl);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Build almadan once mesh'i asset olarak pisirebilirsin. Bu, kablo " +
            "geometrisini diske yazar ve bilesen silinse bile kablolar durur.",
            MessageType.None);

        if (GUILayout.Button("Mesh'i asset olarak pisir"))
            MeshiPisir(pl);
    }

    static void MeshiPisir(FoggyRoad_PowerLine pl)
    {
        pl.Rebuild();
        Mesh m = pl.CurrentMesh;
        if (m == null || m.vertexCount == 0)
        {
            Debug.LogWarning("[FoggyRoad] Pisirilecek kablo mesh'i yok.");
            return;
        }

        FoggyRoad_PowerLineAssets.KlasorGarantile("Assets/FoggyRoad/Generated");
        string yol = AssetDatabase.GenerateUniqueAssetPath(
            "Assets/FoggyRoad/Generated/PowerLine_" + pl.gameObject.name + ".asset");

        Mesh kopya = Object.Instantiate(m);
        kopya.name = System.IO.Path.GetFileNameWithoutExtension(yol);
        AssetDatabase.CreateAsset(kopya, yol);
        AssetDatabase.SaveAssets();

        MeshFilter mf = pl.GetComponent<MeshFilter>();
        Undo.RecordObject(mf, "Mesh pisir");
        mf.sharedMesh = kopya;

        Undo.RecordObject(pl, "Mesh pisir");
        pl.autoUpdateInEditor = false;
        EditorUtility.SetDirty(pl);

        Debug.Log($"[FoggyRoad] Kablo mesh'i pisirildi: {yol} ({kopya.vertexCount} vertex). " +
                  "Otomatik guncelleme kapatildi; tekrar acmak istersen " +
                  "'autoUpdateInEditor' kutusunu isaretle.");
    }

    // ------------------------------------------------------------ gizmo
    void OnSceneGUI()
    {
        FoggyRoad_PowerLine pl = (FoggyRoad_PowerLine)target;

        Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        for (int i = 0; i < pl.poles.Count; i++)
        {
            Transform p = pl.poles[i];
            if (p == null) continue;

            List<Vector3> anchors = pl.CollectAnchors(p);
            for (int a = 0; a < anchors.Count; a++)
                Handles.SphereHandleCap(0, anchors[a], Quaternion.identity,
                                        HandleUtility.GetHandleSize(anchors[a]) * 0.12f,
                                        EventType.Repaint);

            Handles.Label(p.position + Vector3.up * 19f, i.ToString());
        }

        if (pl.spine != null && pl.spine.Count > 1)
        {
            Handles.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            Handles.DrawAAPolyLine(3f, pl.spine.ToArray());
        }
    }
}
#endif
