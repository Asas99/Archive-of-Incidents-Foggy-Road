using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Only vegetation fields are copied back: restoring a preset must not restore TerrainData
// or unrelated quality settings that the user may have edited since taking the backup.
[Serializable]
public sealed class VegetationOptimizationSettings
{
 public float grassStart, grassEnd, farGrassFade, farGrassEnd, farGrassDensity;
 public bool farGrassDistanceThinning;
 public bool farGrassGpuOcclusion;
 public float farDensityFullDistance, farDensityMidDistance, farDensityLowDistance;
 public float farDensityMid, farDensityLow, farDensityFadeHalfWidth;
 public float smallVegetationStart, smallVegetationEnd, treeStart, treeEnd;
 public float treeBillboardDistance, treeCrossFadeLength;

 public static VegetationOptimizationSettings Read(SceneOptimizationProfile p)
 {
  // Matching field names let JsonUtility read only this explicitly limited subset.
  return JsonUtility.FromJson<VegetationOptimizationSettings>(JsonUtility.ToJson(p));
 }
 public VegetationOptimizationSettings Copy() => JsonUtility.FromJson<VegetationOptimizationSettings>(JsonUtility.ToJson(this));
 static float Safe(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
 public void Normalize()
 {
  farGrassEnd = Mathf.Clamp(Safe(farGrassEnd, 90), 5, 250);
  grassEnd = Mathf.Clamp(Safe(grassEnd, 30), 1, Mathf.Min(80, farGrassEnd));
  grassStart = Mathf.Clamp(Safe(grassStart, grassEnd - 10), 0, grassEnd - .1f);
  farGrassFade = Mathf.Clamp(Safe(farGrassFade, farGrassEnd - 15), 0, farGrassEnd - .1f);
  farGrassDensity = Mathf.Clamp01(Safe(farGrassDensity, 1));
  farDensityFullDistance = Mathf.Clamp(Safe(farDensityFullDistance, farGrassEnd * .4f), 0, farGrassEnd - .3f);
  farDensityMidDistance = Mathf.Clamp(Safe(farDensityMidDistance, farGrassEnd * 2 / 3), farDensityFullDistance + .1f, farGrassEnd - .2f);
  farDensityLowDistance = Mathf.Clamp(Safe(farDensityLowDistance, farGrassEnd * 13 / 15), farDensityMidDistance + .1f, farGrassEnd - .1f);
  farDensityMid = Mathf.Clamp01(Safe(farDensityMid, .7f));
  farDensityLow = Mathf.Clamp(Safe(farDensityLow, .45f), 0, farDensityMid);
  farDensityFadeHalfWidth = Mathf.Clamp(Safe(farDensityFadeHalfWidth, .04f), .001f, .1f);
  smallVegetationEnd = Mathf.Clamp(Safe(smallVegetationEnd, 100), 1, 250);
  smallVegetationStart = Mathf.Clamp(Safe(smallVegetationStart, smallVegetationEnd - 20), 0, smallVegetationEnd - .1f);
  treeEnd = Mathf.Clamp(Safe(treeEnd, 300), 10, 1000);
  treeStart = Mathf.Clamp(Safe(treeStart, treeEnd - 30), 0, treeEnd - .1f);
  treeCrossFadeLength = Mathf.Clamp(Safe(treeCrossFadeLength, 40), .1f, treeEnd - .1f);
  treeBillboardDistance = Mathf.Clamp(Safe(treeBillboardDistance, treeEnd * 2 / 3), 0, treeEnd - treeCrossFadeLength);
 }
 public void ResizeGrass(float distance)
 {
  float nearWidth = Mathf.Max(.1f, grassEnd - grassStart);
  float old = Mathf.Max(.1f, farGrassEnd), ratio = distance / old;
  farGrassFade *= ratio; farDensityFullDistance *= ratio;
  farDensityMidDistance *= ratio; farDensityLowDistance *= ratio;
  farGrassEnd = distance; grassEnd = Mathf.Min(grassEnd, distance);
  grassStart = Mathf.Max(0, grassEnd - nearWidth); Normalize();
 }
 public void Write(SceneOptimizationProfile p)
 {
  Normalize(); JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), p);
  p.farGrassStart = p.farGrassFull = 0; // Continuous near coverage, never an empty ring.
 }
}

public static class VegetationOptimizationStorage
{
 [Serializable] public sealed class Snapshot
 {
  public int version = 1;
  public string scenePath, profileGuid, created;
  public VegetationOptimizationSettings settings;
 }
 public static string LastKey(SceneOptimizationController c) => "FoggyRoad.VegetationUndo." + AssetDatabase.AssetPathToGUID(c.gameObject.scene.path) + "." + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c.profile));
 public static void RequireEditable(SceneOptimizationController c)
 {
  if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("İşlem yalnızca düzenleme modunda yapılabilir.");
  if (!c || !c.profile || !c.gameObject.scene.IsValid() || !c.gameObject.scene.isLoaded || string.IsNullOrEmpty(c.gameObject.scene.path) || !AssetDatabase.Contains(c.profile))
   throw new InvalidOperationException("Kaydedilmiş bir sahne ve kayıtlı optimizasyon profili gerekiyor.");
 }
 public static string Backup(SceneOptimizationController c)
 {
  RequireEditable(c);
  string dir = Path.GetFullPath("Recovery/VegetationSettings_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
  Directory.CreateDirectory(dir);
  var snapshot = new Snapshot { scenePath = c.gameObject.scene.path, profileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c.profile)), created = DateTime.Now.ToString("O"), settings = VegetationOptimizationSettings.Read(c.profile) };
  File.WriteAllText(Path.Combine(dir, "Settings.json"), JsonUtility.ToJson(snapshot, true));
  File.WriteAllText(Path.Combine(dir, "Profile.json"), EditorJsonUtility.ToJson(c.profile, true));
  if (!EditorSceneManager.SaveScene(c.gameObject.scene, Path.Combine(dir, "SceneBefore.unity"), true)) throw new IOException("Sahne yedeği kaydedilemedi; ayarlar uygulanmadı.");
  var materials = c.nearGrassMaterials.Concat(c.smallVegetationMaterials).Where(m => m).Distinct();
  foreach (var m in materials)
  {
   string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));
   File.WriteAllText(Path.Combine(dir, "Material_" + guid + ".json"), EditorJsonUtility.ToJson(m, true));
  }
  File.WriteAllText(Path.Combine(dir, "Readme.txt"), "Settings.json yalnızca bitki optimizasyon ayarlarını geri getirir. SceneBefore.unity tam sahnenin ayrı kurtarma kopyasıdır; otomatik geri yüklenmez. Boyama, ağaç ve yükselti verileri araç tarafından değiştirilmez.\n");
  return Path.Combine(dir, "Settings.json");
 }
 public static VegetationOptimizationSettings Load(SceneOptimizationController c, string path)
 {
  RequireEditable(c);
  var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(path));
  if (snapshot == null || snapshot.version != 1 || snapshot.settings == null || snapshot.scenePath != c.gameObject.scene.path || snapshot.profileGuid != AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c.profile)))
   throw new InvalidDataException("Bu yedek seçili sahne/profil için oluşturulmamış veya desteklenmiyor.");
  var result = snapshot.settings.Copy(); result.Normalize(); return result;
 }
 public static string Apply(SceneOptimizationController c, VegetationOptimizationSettings settings)
 {
  RequireEditable(c);
  string previous = Backup(c); // No mutations before a successful backup.
  EditorPrefs.SetString(LastKey(c), previous);
  settings.Copy().Write(c.profile);
  // Apply just the vegetation subset. Global graphics, props and heightmap quality stay untouched.
  foreach (var t in c.terrains.Where(t => t))
  {
   t.detailObjectDistance = c.profile.grassEnd; t.treeDistance = c.profile.treeEnd;
   t.treeBillboardDistance = c.profile.treeBillboardDistance; t.treeCrossFadeLength = c.profile.treeCrossFadeLength;
   EditorUtility.SetDirty(t);
  }
  foreach (var t in c.smallVegetationTerrains.Where(t => t)) { t.treeDistance = c.profile.smallVegetationEnd; EditorUtility.SetDirty(t); }
  SetFade(c.nearGrassMaterials, c.profile.grassStart, c.profile.grassEnd);
  SetFade(c.smallVegetationMaterials, c.profile.smallVegetationStart, c.profile.smallVegetationEnd);
  EditorUtility.SetDirty(c.profile);
  // Save only our changed assets, not unrelated unsaved TerrainData or glass materials.
  AssetDatabase.SaveAssetIfDirty(c.profile);
  foreach (var m in c.nearGrassMaterials.Concat(c.smallVegetationMaterials).Where(m => m).Distinct()) AssetDatabase.SaveAssetIfDirty(m);
  EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
  if (!EditorSceneManager.SaveScene(c.gameObject.scene)) throw new IOException("Ayarlar uygulandı ancak sahne kaydedilemedi. Geri dönüş yedeği: " + previous);
  SceneView.RepaintAll(); return previous;
 }
 static void SetFade(Material[] materials, float start, float end)
 {
  foreach (var m in materials.Where(m => m && m.HasProperty("Distance_Fade_Start") && m.HasProperty("Distance_Fade_End")))
  { m.SetFloat("Distance_Fade_Start", start); m.SetFloat("Distance_Fade_End", end); EditorUtility.SetDirty(m); }
 }
}

public sealed class VegetationOptimizationWindow : EditorWindow
{
 [SerializeField] SceneOptimizationController controller;
 [SerializeField] VegetationOptimizationSettings draft;
 [SerializeField] string baseline, message, lastBackup;
 [SerializeField] bool advanced;
 Vector2 scroll;
 [MenuItem("Tools/Foggy Road/Bitki Optimizasyonu")]
 public static void Open() => GetWindow<VegetationOptimizationWindow>("Bitki Optimizasyonu");
 void OnEnable() { minSize = new Vector2(440, 480); EditorApplication.playModeStateChanged += PlayChanged; FindController(); }
 void OnDisable() => EditorApplication.playModeStateChanged -= PlayChanged;
 void PlayChanged(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredEditMode) { controller = null; FindController(); } Repaint(); }
 void OnInspectorUpdate() => Repaint();
 void FindController()
 {
  if (controller) return;
  var all = Object.FindObjectsByType<SceneOptimizationController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.gameObject.scene.isLoaded).ToArray();
  controller = all.FirstOrDefault(c => c.gameObject.scene == EditorSceneManager.GetActiveScene()) ?? (all.Length == 1 ? all[0] : null);
  if (controller && controller.profile && draft == null) Reload();
 }
 void Reload() { draft = VegetationOptimizationSettings.Read(controller.profile); baseline = JsonUtility.ToJson(draft); }
 void Run(Action action) { try { action(); } catch (Exception e) { message = e.Message; Debug.LogException(e); } Repaint(); }
 static float Width(string label, float start, float end) => end - EditorGUILayout.Slider(label, Mathf.Max(.1f, end - start), .1f, Mathf.Max(.1f, end));
 void OnGUI()
 {
  FindController(); scroll = EditorGUILayout.BeginScrollView(scroll);
  EditorGUI.BeginChangeCheck(); var chosen = (SceneOptimizationController)EditorGUILayout.ObjectField("Sahne kontrolcüsü", controller, typeof(SceneOptimizationController), true);
  if (EditorGUI.EndChangeCheck()) { controller = chosen; draft = null; if (controller && controller.profile) Reload(); }
  if (!controller || !controller.profile) { EditorGUILayout.HelpBox("Açık sahnedeki mevcut SceneOptimizationController ve profilini seç. Araç ikinci bir kontrolcü oluşturmaz.", MessageType.Info); EditorGUILayout.EndScrollView(); return; }
  if (draft == null) Reload();
  EditorGUILayout.LabelField("Profil", AssetDatabase.GetAssetPath(controller.profile));
  bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
  bool external = baseline != JsonUtility.ToJson(VegetationOptimizationSettings.Read(controller.profile));
  if (external) EditorGUILayout.HelpBox("Profil dışarıdan değişti. Önce Kayıtlı Ayarları Oku ile güncelle; formdaki değişiklikler silinir.", MessageType.Warning);
  EditorGUILayout.HelpBox("Düzenleme modunda ayarla → Uygula ve Kaydet → Play Mode’da test et. Sürgüler tek başına sahneyi değiştirmez. Normal Terrain eğreltileri çimen mesafesini takip eder.", MessageType.Info);
  using (new EditorGUI.DisabledScope(playing || EditorApplication.isCompiling))
  {
   EditorGUILayout.Space(); EditorGUILayout.LabelField("Çimen / ot / Terrain eğreltileri", EditorStyles.boldLabel);
   float distance = EditorGUILayout.Slider("Toplam mesafe (m)", draft.farGrassEnd, 5, 250);
   if (distance != draft.farGrassEnd) draft.ResizeGrass(distance);
   using (new EditorGUILayout.HorizontalScope()) foreach (int preset in new[] { 60, 90, 120 }) if (GUILayout.Button(preset + " m taslak")) draft.ResizeGrass(preset);
   float nearWidth = draft.grassEnd - draft.grassStart;
   draft.grassEnd = EditorGUILayout.Slider("Ayrıntılı yakın mesafe (m)", draft.grassEnd, 1, Mathf.Min(80, draft.farGrassEnd));
   draft.grassStart = Mathf.Max(0, draft.grassEnd - nearWidth);
   draft.grassStart = Width("Yakın geçiş genişliği (m)", draft.grassStart, draft.grassEnd);
   draft.farGrassFade = Width("Uzak geçiş genişliği (m)", draft.farGrassFade, draft.farGrassEnd);
   draft.farGrassDensity = EditorGUILayout.Slider("Uzak örtü yoğunluğu (%)", draft.farGrassDensity * 100, 0, 100) / 100;
   draft.farGrassGpuOcclusion = EditorGUILayout.Toggle("GPU çimen örtülmesi", draft.farGrassGpuOcclusion);
   EditorGUILayout.HelpBox("Yoğunluk yalnızca hafif örtüyü etkiler; boyadığın yoğun yakın bitkileri silmez.", MessageType.None);
   EditorGUILayout.LabelField("Ayrı küçük bitki Terrain grubu", EditorStyles.boldLabel);
   float plantWidth = draft.smallVegetationEnd - draft.smallVegetationStart;
   draft.smallVegetationEnd = EditorGUILayout.Slider("Bitki mesafesi (m)", draft.smallVegetationEnd, 1, 250);
   draft.smallVegetationStart = Mathf.Max(0, draft.smallVegetationEnd - plantWidth);
   draft.smallVegetationStart = Width("Bitki geçiş genişliği (m)", draft.smallVegetationStart, draft.smallVegetationEnd);
   EditorGUILayout.LabelField("Ağaçlar", EditorStyles.boldLabel);
   float treeWidth = draft.treeEnd - draft.treeStart;
   draft.treeEnd = EditorGUILayout.Slider("Ağaç mesafesi (m)", draft.treeEnd, 10, 1000);
   draft.treeStart = Mathf.Max(0, draft.treeEnd - treeWidth);
   draft.treeStart = Width("Ağaç geçiş genişliği (m)", draft.treeStart, draft.treeEnd);
   advanced = EditorGUILayout.Foldout(advanced, "Gelişmiş ayarlar", true);
   if (advanced)
   {
    draft.farGrassDistanceThinning = EditorGUILayout.Toggle("Uzakta yoğunluğu azalt", draft.farGrassDistanceThinning);
    draft.farDensityFullDistance = EditorGUILayout.FloatField("Tam yoğunluk sonu (m)", draft.farDensityFullDistance);
    draft.farDensityMidDistance = EditorGUILayout.FloatField("Orta yoğunluk noktası (m)", draft.farDensityMidDistance);
    draft.farDensityLowDistance = EditorGUILayout.FloatField("Düşük yoğunluk noktası (m)", draft.farDensityLowDistance);
    draft.farDensityMid = EditorGUILayout.Slider("Orta yoğunluk (%)", draft.farDensityMid * 100, 0, 100) / 100;
    draft.farDensityLow = EditorGUILayout.Slider("Düşük yoğunluk (%)", draft.farDensityLow * 100, 0, 100) / 100;
    draft.treeBillboardDistance = EditorGUILayout.FloatField("Billboard başlangıcı (m)", draft.treeBillboardDistance);
    draft.treeCrossFadeLength = EditorGUILayout.FloatField("Billboard geçişi (m)", draft.treeCrossFadeLength);
   }
   draft.Normalize();
   EditorGUILayout.Space();
   using (new EditorGUI.DisabledScope(external)) if (GUILayout.Button("Uygula ve Kaydet", GUILayout.Height(32))) Run(() => { lastBackup = VegetationOptimizationStorage.Apply(controller, draft); Reload(); message = "Kaydedildi. Play Mode’da test edebilirsin. Yedek: " + Path.GetDirectoryName(lastBackup); });
   if (GUILayout.Button("Kayıtlı Ayarları Oku (taslağı bırak)")) Reload();
   using (new EditorGUILayout.HorizontalScope())
   {
    if (GUILayout.Button("Yedek Al")) Run(() => { lastBackup = VegetationOptimizationStorage.Backup(controller); message = "Yedek: " + Path.GetDirectoryName(lastBackup); });
    string undo = EditorPrefs.GetString(VegetationOptimizationStorage.LastKey(controller), "");
    using (new EditorGUI.DisabledScope(!File.Exists(undo))) if (GUILayout.Button("Son Uygulamayı Geri Al")) Run(() => { var settings = VegetationOptimizationStorage.Load(controller, undo); lastBackup = VegetationOptimizationStorage.Apply(controller, settings); Reload(); message = "Önceki optimizasyon ayarları geri alındı ve kaydedildi. Boyama ve yükselti değiştirilmedi."; });
   }
   if (GUILayout.Button("Yedekten Ayarları Yükle (taslağa)")) Run(() => { string path = EditorUtility.OpenFilePanel("Bu sahnenin Settings.json yedeğini seç", Path.GetFullPath("Recovery"), "json"); if (path.Length > 0) { draft = VegetationOptimizationStorage.Load(controller, path); baseline = JsonUtility.ToJson(VegetationOptimizationSettings.Read(controller.profile)); message = "Yedek taslağa yüklendi. Kalıcı yapmak için Uygula ve Kaydet."; } });
   if (GUILayout.Button("Bitki Fırçasını Aç")) FoggyRoad_BitkiSilgi.AcEkle();
   if (GUILayout.Button("Uzak Çimen Önbelleğini Yenile (boyama sonrası)")) Run(() => { VegetationOptimizationStorage.RequireEditable(controller); lastBackup = VegetationOptimizationStorage.Backup(controller); VegetationBrushPaint.RefreshDistantGrass(controller.terrains); var renderer = controller.GetComponent<FarGrassRenderer>(); if (renderer) foreach (var cache in renderer.caches.Where(c => c)) AssetDatabase.SaveAssetIfDirty(cache); message = "Önbellek yenilendi; profil ve boyama değiştirilmedi."; });
  }
  if (playing) EditorGUILayout.HelpBox("Play Mode: ayarlar kilitli. Sayaçlar mevcut çalışma anını gösterir.", MessageType.Info);
  var p = controller.profile;
  EditorGUILayout.LabelField("Kayıtlı: yakın " + p.grassEnd.ToString("0.#") + " m • toplam " + p.farGrassEnd.ToString("0.#") + " m • ağaç " + p.treeEnd.ToString("0.#") + " m", EditorStyles.boldLabel);
  var grass = controller.GetComponent<FarGrassRenderer>();
  if (grass && playing)
  {
   EditorGUILayout.LabelField("Çizim yolu: " + grass.gpuStatus);
   if (grass.gpuActive && GUILayout.Button("GPU Sayaçlarını Oku (tek istek)")) grass.RequestGpuCounters();
   if (grass.gpuCountersValid) EditorGUILayout.HelpBox("Son GPU örneği — çizilen: " + grass.gpuVisible + " • örtülen: " + grass.gpuOccluded + " • mesafe: " + grass.gpuRangeRejected + " • görüş: " + grass.gpuFrustumRejected + " • yoğunluk: " + grass.gpuDensityRejected + "\nSayaçlar düğmeye basıldığında okunur; anlık FPS ölçümü değildir.", MessageType.None);
  }
  if (grass && playing && !grass.gpuActive) EditorGUILayout.HelpBox("Çizilen: " + grass.visibleInstances + " • üçgen: " + grass.triangles + " • çizim: " + grass.drawCalls + "\nTaranan: " + grass.testedInstances + " • mesafe elemesi: " + grass.rangeRejected + " • görüş elemesi: " + grass.frustumRejected + " • yoğunluk elemesi: " + grass.densityRejected, MessageType.None);
  if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
  if (!string.IsNullOrEmpty(lastBackup) && GUILayout.Button("Son Yedek Klasörünü Aç")) EditorUtility.RevealInFinder(lastBackup);
  EditorGUILayout.EndScrollView();
 }
}
