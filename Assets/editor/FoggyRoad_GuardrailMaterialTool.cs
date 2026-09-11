// FoggyRoad_GuardrailMaterialTool.cs
// Unity 6+ Editor tool
// Menu: Tools > Foggy Road > Korkuluk Materyal Atayici
//
// Sahnedeki tum korkuluk modullerinin materyalini tek seferde degistirir.
// Iki mod:
//   1) Tum slotlara ata      -> her modulun butun materyal slotlari yeni materyal olur
//   2) Belirli materyali degistir -> sadece secilen eski materyali kullanan slotlar degisir
//      (ornegin sadece metal govdeyi degistirip reflektor ve civatalari korumak icin)

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FoggyRoadTools.GuardrailMaterial
{
    public class GuardrailMaterialTool : EditorWindow
    {
        private enum Mode
        {
            Tum_Slotlara_Ata = 0,
            Belirli_Materyali_Degistir = 1
        }

        [SerializeField] private GameObject targetRoot;
        [SerializeField] private bool useWholeScene = true;
        [SerializeField] private string nameFilter = "GRPRO";
        [SerializeField] private Material newMaterial;
        [SerializeField] private Material oldMaterial;
        [SerializeField] private Mode mode = Mode.Tum_Slotlara_Ata;

        private string status = "Yeni materyali sec, hedefi belirle ve 'MATERYALI UYGULA'ya bas.";
        private MessageType statusType = MessageType.Info;
        private Vector2 scroll;

        [MenuItem("Tools/Foggy Road/Korkuluk Materyal Atayici")]
        public static void OpenWindow()
        {
            GuardrailMaterialTool w = GetWindow<GuardrailMaterialTool>(false, "Korkuluk Materyal", true);
            w.minSize = new Vector2(420, 430);
            w.TryPickSelectedMaterial();
            w.Show();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("KORKULUK MATERYAL ATAYICI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sahnedeki tum korkuluk modullerinin materyalini tek seferde degistirir. " +
                "Islem Ctrl+Z ile geri alinabilir.", MessageType.None);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("1. HEDEF", EditorStyles.boldLabel);

            useWholeScene = EditorGUILayout.ToggleLeft(
                new GUIContent("Sahnedeki TUM korkuluklar",
                    "Isminde asagidaki metin gecen butun objeler (pasif olanlar dahil)."),
                useWholeScene);

            using (new EditorGUI.DisabledScope(!useWholeScene))
                nameFilter = EditorGUILayout.TextField("Isim Filtresi", nameFilter);

            using (new EditorGUI.DisabledScope(useWholeScene))
                targetRoot = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Hedef Kok Obje", "Ornek: Guardrails_Auto"),
                    targetRoot, typeof(GameObject), true);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("2. MATERYAL", EditorStyles.boldLabel);

            newMaterial = (Material)EditorGUILayout.ObjectField(
                "Yeni Materyal", newMaterial, typeof(Material), false);

            if (GUILayout.Button("Project'te SECILI materyali al", GUILayout.Height(22)))
                TryPickSelectedMaterial();

            mode = (Mode)EditorGUILayout.EnumPopup(
                new GUIContent("Mod", "Tum slotlar: modulun her parcasi ayni materyal olur."), mode);

            using (new EditorGUI.DisabledScope(mode != Mode.Belirli_Materyali_Degistir))
                oldMaterial = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Degistirilecek Materyal", "Sadece bu materyali kullanan slotlar degisir."),
                    oldMaterial, typeof(Material), false);

            if (mode == Mode.Tum_Slotlara_Ata)
            {
                EditorGUILayout.HelpBox(
                    "DIKKAT: Bu mod reflektor ve civata gibi ayri parcalari da ayni materyal yapar. " +
                    "Sadece metal govdeyi degistirmek istiyorsan 'Belirli Materyali Degistir' modunu kullan.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3. CALISTIR", EditorStyles.boldLabel);

            if (GUILayout.Button("KULLANILAN MATERYALLERI LISTELE", GUILayout.Height(24)))
                ListMaterials();

            if (GUILayout.Button("MATERYALI UYGULA", GUILayout.Height(34)))
                Apply();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(status, statusType);

            EditorGUILayout.EndScrollView();
        }

        private void TryPickSelectedMaterial()
        {
            Material m = Selection.activeObject as Material;
            if (m == null)
            {
                Object[] sel = Selection.GetFiltered(typeof(Material), SelectionMode.Assets);
                if (sel.Length > 0) m = sel[0] as Material;
            }

            if (m != null)
            {
                newMaterial = m;
                SetStatus("Secili materyal alindi: " + m.name, MessageType.Info);
            }
            else
            {
                SetStatus("Project penceresinde bir materyal secili degil.", MessageType.Warning);
            }
            Repaint();
        }

        private List<Renderer> CollectRenderers()
        {
            List<Renderer> list = new List<Renderer>();

            if (useWholeScene)
            {
                string filter = string.IsNullOrEmpty(nameFilter) ? null : nameFilter.ToUpperInvariant();
                Renderer[] all = Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < all.Length; i++)
                {
                    if (filter != null && !MatchesFilter(all[i].transform, filter)) continue;
                    list.Add(all[i]);
                }
            }
            else if (targetRoot != null)
            {
                list.AddRange(targetRoot.GetComponentsInChildren<Renderer>(true));
            }

            return list;
        }

        // Objenin kendisi ya da ust objelerinden biri filtreyi tasiyorsa dahil et
        private static bool MatchesFilter(Transform t, string upperFilter)
        {
            while (t != null)
            {
                if (t.name.ToUpperInvariant().Contains(upperFilter)) return true;
                t = t.parent;
            }
            return false;
        }

        private void ListMaterials()
        {
            List<Renderer> renderers = CollectRenderers();
            if (renderers.Count == 0)
            {
                SetStatus("Hedefe uyan renderer bulunamadi.", MessageType.Warning);
                return;
            }

            Dictionary<string, int> counts = new Dictionary<string, int>();
            for (int i = 0; i < renderers.Count; i++)
            {
                Material[] mats = renderers[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    string key = mats[m] != null ? mats[m].name : "(bos)";
                    int c;
                    counts.TryGetValue(key, out c);
                    counts[key] = c + 1;
                }
            }

            string text = renderers.Count + " renderer bulundu. Kullanilan materyaller:\n";
            foreach (KeyValuePair<string, int> kv in counts)
                text += "  - " + kv.Key + "  (" + kv.Value + " slot)\n";

            SetStatus(text, MessageType.Info);
        }

        private void Apply()
        {
            if (newMaterial == null)
            {
                SetStatus("Yeni materyal secilmedi.", MessageType.Error);
                return;
            }

            if (mode == Mode.Belirli_Materyali_Degistir && oldMaterial == null)
            {
                SetStatus("Degistirilecek (eski) materyal secilmedi.", MessageType.Error);
                return;
            }

            List<Renderer> renderers = CollectRenderers();
            if (renderers.Count == 0)
            {
                SetStatus("Hedefe uyan renderer bulunamadi. Isim filtresini kontrol et.", MessageType.Warning);
                return;
            }

            int changedRenderers = 0;
            int changedSlots = 0;

            Undo.SetCurrentGroupName("Korkuluk materyali degistir");
            int group = Undo.GetCurrentGroup();

            try
            {
                for (int i = 0; i < renderers.Count; i++)
                {
                    if (i % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Korkuluk Materyal Atayici",
                            "Materyaller atainiyor... " + i + "/" + renderers.Count,
                            (float)i / renderers.Count);
                    }

                    Renderer r = renderers[i];
                    Material[] mats = r.sharedMaterials;
                    bool dirty = false;

                    for (int m = 0; m < mats.Length; m++)
                    {
                        bool hit = mode == Mode.Tum_Slotlara_Ata || mats[m] == oldMaterial;
                        if (!hit || mats[m] == newMaterial) continue;

                        mats[m] = newMaterial;
                        dirty = true;
                        changedSlots++;
                    }

                    if (!dirty) continue;

                    Undo.RecordObject(r, "Korkuluk materyali degistir");
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                    changedRenderers++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Undo.CollapseUndoOperations(group);

            if (changedRenderers > 0)
                EditorSceneManager.MarkSceneDirty(renderers[0].gameObject.scene);

            SetStatus("Bitti.\n" +
                      "Taranan renderer: " + renderers.Count + "\n" +
                      "Degisen obje: " + changedRenderers + "\n" +
                      "Degisen materyal slotu: " + changedSlots + "\n" +
                      "Atanan materyal: " + newMaterial.name,
                      MessageType.Info);
        }

        private void SetStatus(string msg, MessageType type)
        {
            status = msg;
            statusType = type;
            Repaint();
        }
    }
}
#endif
