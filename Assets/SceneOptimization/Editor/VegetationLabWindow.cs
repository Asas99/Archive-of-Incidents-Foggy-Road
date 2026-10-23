using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>One vegetation profile, reversible Play Mode previews, and player-camera measurements.</summary>
public sealed class VegetationLabWindow : EditorWindow
{
 [SerializeField] SceneOptimizationController controller;
 [SerializeField] VegetationOptimizationSettings draft;
 [SerializeField] string baseline, message;
 [SerializeField] int tab;
 [SerializeField] Measurement reference, candidate;
 Vector2 scroll;
 SceneOptimizationProfile savedProfile, previewProfile;
 readonly Dictionary<FarGrassRenderer, SceneOptimizationProfile> grassProfiles = new Dictionary<FarGrassRenderer, SceneOptimizationProfile>();
 readonly List<TerrainState> terrainStates = new List<TerrainState>();
 readonly Dictionary<Material, Vector2> fades = new Dictionary<Material, Vector2>();
 bool preview;
 ProfilerRecorder gpu, batches, triangles;
 readonly List<float> frames = new List<float>(), gpuFrames = new List<float>(), batchFrames = new List<float>(), triangleFrames = new List<float>();
 Measurement measuring;
 double measureStart;
 int lastFrame;
 bool measuringReference;

 [Serializable] public sealed class Measurement
 {
  public string captured, settings, camera, gpu, environment;
  public Vector3 position; public Quaternion rotation;
  public float fov, medianMs, p95Ms, gpuMs, batches, triangles;
  public int width, height, samples, vSync, targetFps;
  public bool gpuAvailable, moved;
 }
 sealed class TerrainState
 {
  public Terrain terrain;
  public float distance, billboard, crossfade, lod, detailDistance, detailDensity;
  public int full;
  public TerrainState(Terrain t) { terrain=t; distance=t.treeDistance; billboard=t.treeBillboardDistance; crossfade=t.treeCrossFadeLength; lod=t.treeLODBiasMultiplier; full=t.treeMaximumFullLODCount; detailDistance=t.detailObjectDistance; detailDensity=t.detailObjectDensity; }
  public void Restore() { if(!terrain)return; terrain.treeDistance=distance; terrain.treeBillboardDistance=billboard; terrain.treeCrossFadeLength=crossfade; terrain.treeLODBiasMultiplier=lod; terrain.treeMaximumFullLODCount=full; terrain.detailObjectDistance=detailDistance; terrain.detailObjectDensity=detailDensity; }
 }
 [MenuItem("Tools/Foggy Road/Gelişmiş Ağaç ve Çimen Optimizasyonu")]
 public static void Open() => GetWindow<VegetationLabWindow>("Ağaç ve Çimen Lab");
 void OnEnable()
 {
  minSize=new Vector2(530,540);
  EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=PlayChanged;
  AssemblyReloadEvents.beforeAssemblyReload+=Cleanup; FindController();
 }
 void OnDisable() { Cleanup(); EditorApplication.update-=Tick; EditorApplication.playModeStateChanged-=PlayChanged; AssemblyReloadEvents.beforeAssemblyReload-=Cleanup; }
 void Cleanup() { CancelMeasure(); EndPreview(); }
 void PlayChanged(PlayModeStateChange state)
 {
  if(state==PlayModeStateChange.ExitingPlayMode || state==PlayModeStateChange.ExitingEditMode) Cleanup();
  if(state==PlayModeStateChange.EnteredEditMode || state==PlayModeStateChange.EnteredPlayMode)
  { controller=null; FindController(); }
 }
 void FindController()
 {
  if(controller)return;
  controller=Object.FindObjectsByType<SceneOptimizationController>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
   .FirstOrDefault(c=>c.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  if(controller && controller.profile && draft==null) Reload();
 }
 void Reload() { draft=VegetationOptimizationSettings.Read(controller.profile); baseline=JsonUtility.ToJson(draft); }
 void Run(Action action) { try{action();}catch(Exception e){message=e.Message;Debug.LogException(e);} }
 static GUIContent Label(string text,string tip) => new GUIContent(text,tip);
 void OnGUI()
 {
  FindController(); scroll=EditorGUILayout.BeginScrollView(scroll);
  using(new EditorGUI.DisabledScope(preview || measuring!=null))
  {
   EditorGUI.BeginChangeCheck(); var selected=(SceneOptimizationController)EditorGUILayout.ObjectField("Sahne kontrolcüsü",controller,typeof(SceneOptimizationController),true);
   if(EditorGUI.EndChangeCheck()){controller=selected;draft=null;if(controller&&controller.profile)Reload();}
  }
  if(!controller || !controller.profile)
  { EditorGUILayout.HelpBox("Oyunun sahnesini aç ve mevcut SceneOptimizationController bileşenini seç.",MessageType.Info);EditorGUILayout.EndScrollView();return; }
  if(draft==null)Reload();
  EditorGUILayout.LabelField("Profil",AssetDatabase.GetAssetPath(preview?savedProfile:controller.profile));
  tab=GUILayout.Toolbar(tab,new[]{"Ayarlar","Ağaç Denetimi","Ölçüm / A–B"});
  if(tab==0)DrawSettings();else if(tab==1)DrawAudit();else DrawMeasurements();
  if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
  EditorGUILayout.EndScrollView();
 }
 void DrawSettings()
 {
  bool playing=EditorApplication.isPlaying;
  EditorGUILayout.HelpBox("Taslak sürgüler kendiliğinden uygulanmaz. Play Mode’da Geçici Önizleme ile dene; düzenleme modunda Uygula ve Kaydet ile kalıcı yap. Kamera mesafeleri ve occlusion bake değiştirilmez.",MessageType.Info);
  using(new EditorGUI.DisabledScope(measuring!=null))
  {
   using(new EditorGUILayout.HorizontalScope())
   {
    if(GUILayout.Button("Kayıtlı değerler")) { if(preview)EndPreview();Reload(); }
    if(GUILayout.Button("Dengeli taslak"))Preset(false);
    if(GUILayout.Button("Performans taslağı"))Preset(true);
   }
   EditorGUILayout.LabelField("Ağaçlar",EditorStyles.boldLabel);
   draft.treeDensity=EditorGUILayout.Slider(Label("Görsel yoğunluk (%)","Terrain üzerindeki ana ağaç grupları. Kaynak boyama silinmez. Play Mode’da geçici veri kopyası kullanılır."),draft.treeDensity*100,0,100)/100;
   draft.treeEnd=EditorGUILayout.Slider("Görünürlük sonu (m)",draft.treeEnd,10,1000);
   draft.treeStart=EditorGUILayout.Slider("Uzak solma başlangıcı (m)",draft.treeStart,0,Mathf.Max(.1f,draft.treeEnd-.1f));
   draft.treeLodBias=EditorGUILayout.Slider(Label("Ağaç LOD çarpanı","Terrain LOD ağaçlarında (SpeedTree gibi) kullanılır. Küçük değer daha erken düşük LOD seçer. LOD üretmez; tüm modellerde aynı etkiyi yapmaz."),draft.treeLodBias,.1f,3);
   draft.treeBillboardDistance=EditorGUILayout.Slider(Label("Billboard başlangıcı (m)","Destekleyen ağaç tiplerinde etkilidir. Uyumlu billboard içermeyen prefab için otomatik billboard oluşturmaz."),draft.treeBillboardDistance,0,draft.treeEnd);
   draft.treeCrossFadeLength=EditorGUILayout.Slider("Billboard geçiş uzunluğu (m)",draft.treeCrossFadeLength,.1f,Mathf.Max(.1f,draft.treeEnd-.1f));
   draft.fullTrees=EditorGUILayout.IntSlider(Label("Tam ayrıntı ağaç sınırı","Terrain'in bu ayarı destekleyen ağaç tipleri içindir; bütün LODGroup modellerinde ortak bir sınır değildir."),draft.fullTrees,0,2000);
   EditorGUILayout.HelpBox("Görsel seyreltme çarpışmaları korur: görünmeyen ağacın collider’ı kalabilir. Bu yüzden önce LOD ve mesafeyi dene; yoğunluk azaltımını oynanabilir yol ve ormanda kontrol et. Yakın kaynak ağaç sayısı değiştirilmez.",MessageType.Warning);
   EditorGUILayout.Space();EditorGUILayout.LabelField("Çimen ve otlar",EditorStyles.boldLabel);
   draft.nativeGrassDensity=EditorGUILayout.Slider(Label("Terrain yakın çimen (%)","Mevcut Terrain detail yoğunluğuna çarpan. Kaynak detail haritaları silinmez; prototipin yoğunluk ölçeklemeyi desteklemesi gerekir."),draft.nativeGrassDensity*100,0,100)/100;
   draft.grassEnd=EditorGUILayout.Slider("Yakın ayrıntı sonu (m)",draft.grassEnd,1,80);
   draft.grassStart=EditorGUILayout.Slider("Yakın solma başlangıcı (m)",draft.grassStart,0,Mathf.Max(.1f,draft.grassEnd-.1f));
   draft.farGrassDensity=EditorGUILayout.Slider("GPU / uzak örtü yoğunluğu (%)",draft.farGrassDensity*100,0,100)/100;
   draft.farGrassEnd=EditorGUILayout.Slider("Toplam çimen sonu (m)",draft.farGrassEnd,5,250);
   draft.farGrassFade=EditorGUILayout.Slider("Uzak solma başlangıcı (m)",draft.farGrassFade,0,Mathf.Max(.1f,draft.farGrassEnd-.1f));
   draft.farGrassGpuOcclusion=EditorGUILayout.Toggle("GPU çimen örtülmesi",draft.farGrassGpuOcclusion);
   draft.farGrassDistanceThinning=EditorGUILayout.Toggle("Uzakta kademeli seyreltme",draft.farGrassDistanceThinning);
   using(new EditorGUI.DisabledScope(!draft.farGrassDistanceThinning))
   {
    draft.farDensityFullDistance=EditorGUILayout.FloatField("Tam yoğunluk sonu (m)",draft.farDensityFullDistance);
    draft.farDensityMidDistance=EditorGUILayout.FloatField("Orta yoğunluk noktası (m)",draft.farDensityMidDistance);
    draft.farDensityLowDistance=EditorGUILayout.FloatField("Düşük yoğunluk noktası (m)",draft.farDensityLowDistance);
    draft.farDensityMid=EditorGUILayout.Slider("Orta yoğunluk (%)",draft.farDensityMid*100,0,100)/100;
    draft.farDensityLow=EditorGUILayout.Slider("Düşük yoğunluk (%)",draft.farDensityLow*100,0,100)/100;
   }
   EditorGUILayout.LabelField("Ayrı küçük bitki grubu",EditorStyles.boldLabel);
   draft.smallVegetationEnd=EditorGUILayout.Slider("Çalı / küçük bitki sonu (m)",draft.smallVegetationEnd,1,250);
   draft.smallVegetationStart=EditorGUILayout.Slider("Solma başlangıcı (m)",draft.smallVegetationStart,0,Mathf.Max(.1f,draft.smallVegetationEnd-.1f));
   draft.Normalize();
   EditorGUILayout.Space();
   if(playing)
   {
    if(GUILayout.Button(preview?"Önizlemeyi Güncelle":"Geçici Önizlemeyi Başlat",GUILayout.Height(30)))Run(BeginPreview);
    if(preview && GUILayout.Button("Önizlemeyi Bitir — Kayıtlı Ayarlara Dön"))Run(EndPreview);
    EditorGUILayout.HelpBox("Önizleme kapatılınca, pencere kapanınca veya Play Mode bitince geçici ayarlar geri alınır. Taslak pencerede kalır; Play Mode’dan çıkıp kaydedebilirsin.",MessageType.Info);
   }
   else
   {
    bool stale=baseline!=JsonUtility.ToJson(VegetationOptimizationSettings.Read(controller.profile));
    if(stale)EditorGUILayout.HelpBox("Kayıtlı profil dışarıdan değişmiş. Kayıtlı değerler düğmesiyle yeniden oku.",MessageType.Warning);
    using(new EditorGUI.DisabledScope(stale || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
     if(GUILayout.Button("Uygula ve Kaydet (otomatik yedek alır)",GUILayout.Height(30)))Run(()=>{string path=VegetationOptimizationStorage.Apply(controller,draft);Reload();message="Kaydedildi. Yedek: "+path;});
    string undo=EditorPrefs.GetString(VegetationOptimizationStorage.LastKey(controller),"");
    using(new EditorGUI.DisabledScope(!File.Exists(undo)))
     if(GUILayout.Button("Son Kaydı Geri Al"))Run(()=>{VegetationOptimizationStorage.Apply(controller,VegetationOptimizationStorage.Load(controller,undo));Reload();message="Önceki bitki ayarları geri yüklendi.";});
   }
   using(new EditorGUILayout.HorizontalScope())
   {
    if(GUILayout.Button("Taslağı JSON Kaydet"))Run(()=>{string path=EditorUtility.SaveFilePanel("Bitki taslağı","","VegetationPreset.json","json");if(path.Length>0)File.WriteAllText(path,JsonUtility.ToJson(draft,true));});
    if(GUILayout.Button("JSON Taslağı Yükle"))Run(()=>{string path=EditorUtility.OpenFilePanel("Bitki taslağı","","json");if(path.Length>0){var loaded=JsonUtility.FromJson<VegetationOptimizationSettings>(File.ReadAllText(path));if(loaded==null || loaded.treeEnd<=0 || loaded.farGrassEnd<=0)throw new InvalidDataException("Geçerli bir bitki taslağı seç.");loaded.Normalize();draft=loaded;message="Taslak yüklendi; henüz uygulanmadı.";}});
   }
  }
 }
 void Preset(bool fast)
 {
  // Relative to the saved profile, never cumulative button presses or assumed defaults.
  draft=VegetationOptimizationSettings.Read(preview?savedProfile:controller.profile);
  draft.treeLodBias=Mathf.Min(draft.treeLodBias,fast?.6f:.85f);
  draft.treeEnd=Mathf.Min(draft.treeEnd,fast?220:280);draft.treeStart=Mathf.Min(draft.treeStart,draft.treeEnd-25);
  draft.treeBillboardDistance=Mathf.Min(draft.treeBillboardDistance,fast?90:130);
  draft.treeCrossFadeLength=Mathf.Min(draft.treeCrossFadeLength,fast?20:30);
  draft.fullTrees=Mathf.Min(draft.fullTrees,fast?150:300);
  draft.ResizeGrass(Mathf.Min(draft.farGrassEnd,fast?65:85));
  draft.grassEnd=Mathf.Min(draft.grassEnd,fast?25:32);draft.grassStart=Mathf.Min(draft.grassStart,draft.grassEnd-5);
  draft.nativeGrassDensity=Mathf.Min(draft.nativeGrassDensity,fast?.65f:.85f);
  draft.farGrassDensity=Mathf.Min(draft.farGrassDensity,fast?.65f:.85f);
  draft.Normalize();message="Hazır taslak oluşturuldu. Ağaç sayısı korunur; mesafe, LOD ve çimen ayarlarını denemek için uygula.";
 }
 public void BeginPreview()
 {
  if(!EditorApplication.isPlaying || !controller || !controller.profile)throw new InvalidOperationException("Önizleme için Play Mode gerekiyor.");
  if(!preview)
  {
   savedProfile=controller.profile;previewProfile=Instantiate(savedProfile);previewProfile.hideFlags=HideFlags.HideAndDontSave;
   foreach(var t in controller.terrains.Concat(controller.smallVegetationTerrains).Where(t=>t).Distinct())terrainStates.Add(new TerrainState(t));
   foreach(var m in controller.nearGrassMaterials.Concat(controller.smallVegetationMaterials).Where(m=>m && m.HasProperty("Distance_Fade_Start") && m.HasProperty("Distance_Fade_End")).Distinct())
    fades.Add(m,new Vector2(m.GetFloat("Distance_Fade_Start"),m.GetFloat("Distance_Fade_End")));
   foreach(var g in Object.FindObjectsByType<FarGrassRenderer>(FindObjectsSortMode.None).Where(g=>g.profile==savedProfile)) { grassProfiles.Add(g,g.profile);g.profile=previewProfile; }
   controller.profile=previewProfile;preview=true;
  }
  draft.Copy().Write(previewProfile);controller.ApplyTerrain();message="Geçici önizleme etkin. Oyuncu kamerasında dolaşarak kontrol edebilirsin.";
 }
 public void EndPreview()
 {
  if(!preview)return;
  CancelMeasure();
  try
  {
   if(controller){controller.ResetVegetationDensity();controller.profile=savedProfile;controller.ApplyTerrain();}
   foreach(var pair in grassProfiles)if(pair.Key)pair.Key.profile=pair.Value;
   foreach(var t in terrainStates)t.Restore();
   foreach(var pair in fades)if(pair.Key){pair.Key.SetFloat("Distance_Fade_Start",pair.Value.x);pair.Key.SetFloat("Distance_Fade_End",pair.Value.y);}
  }
  finally
  {
   grassProfiles.Clear();terrainStates.Clear();fades.Clear();if(previewProfile)DestroyImmediate(previewProfile);
   previewProfile=null;savedProfile=null;preview=false;
  }
  message="Önizleme bitti; önceki ayarlar geri alındı. Taslak korunuyor.";
 }
 void DrawAudit()
 {
  EditorGUILayout.HelpBox("Salt okunur denetim. LOD ve billboard kendiliğinden üretilmez. Rüzgâr ve gölge ayarları kullanılan ağaç shader’ına/prefabına bağlıdır; prefabı seçerek düzenleyebilirsin.",MessageType.Info);
  int total=0;
  foreach(var t in controller.terrains.Concat(controller.smallVegetationTerrains).Where(t=>t&&t.terrainData).Distinct())
  {
   var data=t.terrainData;var instances=data.treeInstances; total+=instances.Length;
   EditorGUILayout.Space();EditorGUILayout.LabelField(t.name+" — "+instances.Length.ToString("N0")+" örnek",EditorStyles.boldLabel);
   var counts=new int[data.treePrototypes.Length];foreach(var tree in instances)if(tree.prototypeIndex>=0&&tree.prototypeIndex<counts.Length)counts[tree.prototypeIndex]++;
   for(int i=0;i<data.treePrototypes.Length;i++)
   {
    var prefab=data.treePrototypes[i].prefab;if(!prefab)continue;
    using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
    {
     EditorGUILayout.ObjectField(prefab.name+" × "+counts[i],prefab,typeof(GameObject),false);
     var groups=prefab.GetComponentsInChildren<LODGroup>(true);
     var renderers=prefab.GetComponentsInChildren<Renderer>(true);
     var lods=groups.SelectMany(g=>g.GetLODs()).ToArray();
     bool billboard=renderers.Any(r=>r is BillboardRenderer);
     EditorGUILayout.LabelField("LOD grubu: "+groups.Length+" | LOD seviyesi toplamı: "+lods.Length+" | BillboardRenderer: "+(billboard?"var":"yok"));
     if(groups.Length==0)EditorGUILayout.HelpBox("LODGroup yok. Terrain’in yerleşik ağaç/billboard desteği modele ve shader’a bağlıdır.",MessageType.Warning);
     else if(lods.Length<=groups.Length)EditorGUILayout.HelpBox("Tek seviyeli LOD: daha basit uzak model kazancı bulunmuyor.",MessageType.Warning);
     for(int l=0;l<lods.Length;l++)EditorGUILayout.LabelField("LOD "+l+": "+TriangleCount(lods[l].renderers).ToString("N0")+" üçgen | ekran oranı "+lods[l].screenRelativeTransitionHeight.ToString("0.###"));
     if(lods.Length==0)EditorGUILayout.LabelField("Mesh üçgeni: "+TriangleCount(renderers).ToString("N0"));
     EditorGUILayout.LabelField("Materyal yuvası: "+renderers.Sum(r=>r.sharedMaterials.Length)+" | Gölge çizen renderer: "+renderers.Count(r=>r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off));
     if(GUILayout.Button("Prefabı Inspector’da Seç")){Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);}
    }
   }
  }
  EditorGUILayout.LabelField("Terrain ağaç/küçük bitki toplamı: "+total.ToString("N0"),EditorStyles.boldLabel);
  EditorGUILayout.HelpBox("Elle sahneye yerleştirilen bağımsız ağaçlar bu Terrain yoğunluk sürgüsünden etkilenmez. Mesh billboard kullanılıyorsa BillboardRenderer olmaması tek başına hata değildir. Yaprak alfa kesimi ve son LOD gölgelerini prefab üzerinde kontrol et.",MessageType.None);
 }
 static long TriangleCount(IEnumerable<Renderer> renderers)
 {
  long count=0;foreach(var r in renderers.Where(r=>r).Distinct())
  {
   Mesh mesh=r is SkinnedMeshRenderer skin?skin.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;
   if(!mesh)continue;for(int s=0;s<mesh.subMeshCount;s++)if(mesh.GetTopology(s)==MeshTopology.Triangles)count+=(long)mesh.GetIndexCount(s)/3;
  }return count;
 }
 Camera PlayerCamera() => Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None).Select(p=>p.GetComponent<Camera>()).FirstOrDefault(c=>c&&c.isActiveAndEnabled&&c.gameObject.scene==controller.gameObject.scene);
 void DrawMeasurements()
 {
  EditorGUILayout.HelpBox("Gerçek oyuncu kamerasında ölçer; sabit bir test kamerası oluşturmaz. Aynı konum/yön/çözünürlükte dur: önce A’yı ölç, önizleme uygula, sonra B’yi ölç. Her kayıt 2 sn ısınma + 8 sn örnekleme. Sonuç Unity Editor içindir; bağımsız oyun performansı farklı olabilir.",MessageType.Info);
  if(measuring!=null)
  { EditorGUILayout.LabelField("Ölçülüyor: "+Mathf.Clamp((float)(EditorApplication.timeSinceStartup-measureStart),0,10).ToString("0.0")+" / 10 sn");if(GUILayout.Button("Ölçümü İptal Et"))CancelMeasure(); }
  using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying || measuring!=null || EditorApplication.isPaused))
  {
   if(GUILayout.Button("A — Başlangıcı Ölç"))Run(()=>StartMeasure(true));
   if(GUILayout.Button("B — Yeni Ayarı Ölç"))Run(()=>StartMeasure(false));
  }
  DrawMeasurement("A",reference);DrawMeasurement("B",candidate);
  if(reference!=null && candidate!=null)
  {
   bool compatible=Comparable(reference,candidate);
   EditorGUILayout.HelpBox(compatible?"Kare süresi farkı: "+(candidate.medianMs-reference.medianMs).ToString("+0.00;-0.00;0.00")+" ms (küçük değer daha iyi). Editor ve ısınma dalgalanmaları için tekrar ölç.":"Kayıtlar doğrudan karşılaştırılamaz: kamera, hareket, çözünürlük veya FPS sınırı farklı.",compatible?MessageType.Info:MessageType.Warning);
  }
  if((reference!=null||candidate!=null) && GUILayout.Button("Ölçümleri JSON Dışa Aktar"))Run(()=>{string path=EditorUtility.SaveFilePanel("Bitki ölçümleri","","VegetationMeasurements.json","json");if(path.Length>0)File.WriteAllText(path,JsonUtility.ToJson(new MeasurementPair{a=reference,b=candidate},true));});
  var grass=controller.GetComponent<FarGrassRenderer>();
  if(grass && EditorApplication.isPlaying)
  {
   EditorGUILayout.LabelField("Çimen çizim yolu",grass.gpuStatus??"");
   if(grass.gpuActive && GUILayout.Button("GPU Eleme Sayaçlarını Oku"))grass.RequestGpuCounters();
   if(grass.gpuCountersValid)EditorGUILayout.HelpBox("Son GPU örneği: çizilen "+grass.gpuVisible+" | örtülen "+grass.gpuOccluded+" | görüş dışı "+grass.gpuFrustumRejected+" | mesafe "+grass.gpuRangeRejected+" | yoğunluk "+grass.gpuDensityRejected,MessageType.None);
   if(!grass.gpuActive)EditorGUILayout.LabelField("CPU yolu: görünen "+grass.visibleInstances+" | görüş dışı "+grass.frustumRejected+" | yoğunluk "+grass.densityRejected);
  }
 }
 [Serializable] sealed class MeasurementPair {public Measurement a,b;}
 static bool Comparable(Measurement a,Measurement b) => !a.moved&&!b.moved&&a.camera==b.camera&&a.width==b.width&&a.height==b.height&&Vector3.Distance(a.position,b.position)<.1f&&Quaternion.Angle(a.rotation,b.rotation)<.5f&&Mathf.Abs(a.fov-b.fov)<.1f&&a.vSync==b.vSync&&a.targetFps==b.targetFps;
 static void DrawMeasurement(string title,Measurement m)
 {
  if(m==null)return;EditorGUILayout.Space();EditorGUILayout.LabelField(title+" — "+m.samples+" örnek",EditorStyles.boldLabel);
  EditorGUILayout.LabelField("Kare: "+m.medianMs.ToString("0.00")+" ms | yaklaşık "+(1000/Mathf.Max(.01f,m.medianMs)).ToString("0.0")+" FPS | P95 "+m.p95Ms.ToString("0.00")+" ms");
  EditorGUILayout.LabelField("GPU: "+(m.gpuAvailable?m.gpuMs.ToString("0.00")+" ms":"sayaç yok")+" | Batches "+m.batches.ToString("N0")+" | üçgen "+m.triangles.ToString("N0"));
  EditorGUILayout.LabelField(m.width+"×"+m.height+" | VSync "+m.vSync+" | FPS sınırı "+m.targetFps);
  if(m.moved)EditorGUILayout.HelpBox("Ölçüm sırasında kamera hareket etti; aynı bakış karşılaştırması için tekrar ölç.",MessageType.Warning);
 }
 void StartMeasure(bool isReference)
 {
  var camera=PlayerCamera();if(!camera)throw new InvalidOperationException("Aktif PlayerInteraction oyuncu kamerası bulunamadı.");
  CancelMeasure();measuringReference=isReference;frames.Clear();gpuFrames.Clear();batchFrames.Clear();triangleFrames.Clear();
  measuring=new Measurement{captured=DateTime.Now.ToString("O"),settings=JsonUtility.ToJson(VegetationOptimizationSettings.Read(controller.profile)),camera=camera.name,position=camera.transform.position,rotation=camera.transform.rotation,fov=camera.fieldOfView,width=Screen.width,height=Screen.height,vSync=QualitySettings.vSyncCount,targetFps=Application.targetFrameRate,gpu=SystemInfo.graphicsDeviceName,environment="Unity Editor Play Mode; player camera; 2s warmup / 8s sample; frame samples observed by Editor update"};
  gpu=StartRecorder("GPU Frame Time");batches=StartRecorder("Batches Count");triangles=StartRecorder("Triangles Count");
  measureStart=EditorApplication.timeSinceStartup;lastFrame=Time.frameCount;
 }
 static ProfilerRecorder StartRecorder(string marker) {try{return ProfilerRecorder.StartNew(ProfilerCategory.Render,marker,1);}catch{return default;}}
 void Tick()
 {
  if(measuring==null)return;
  if(!EditorApplication.isPlaying || EditorApplication.isPaused || !controller){CancelMeasure();message="Ölçüm durduruldu: oyun durdu veya duraklatıldı.";return;}
  double elapsed=EditorApplication.timeSinceStartup-measureStart;
  if(Time.frameCount!=lastFrame)
  {
   lastFrame=Time.frameCount;
   if(elapsed>=2)
   {
    var camera=PlayerCamera();if(!camera){CancelMeasure();return;}
    measuring.moved|=Vector3.Distance(camera.transform.position,measuring.position)>.1f || Quaternion.Angle(camera.transform.rotation,measuring.rotation)>.5f || Screen.width!=measuring.width || Screen.height!=measuring.height || Mathf.Abs(camera.fieldOfView-measuring.fov)>.1f;
    if(Time.unscaledDeltaTime>0)frames.Add(Time.unscaledDeltaTime*1000);
    if(gpu.Valid && gpu.Count>0 && gpu.LastValue>0)gpuFrames.Add(gpu.LastValue/1000000f);
    if(batches.Valid && batches.Count>0)batchFrames.Add(batches.LastValue);
    if(triangles.Valid && triangles.Count>0)triangleFrames.Add(triangles.LastValue);
   }
  }
  if(elapsed>=10)
  {
   if(frames.Count<10){CancelMeasure();message="Yeterli kare örneği alınamadı.";return;}
   frames.Sort();gpuFrames.Sort();measuring.medianMs=frames[frames.Count/2];measuring.p95Ms=frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count*.95f)-1,0,frames.Count-1)];measuring.samples=frames.Count;
   measuring.gpuAvailable=gpuFrames.Count>0;measuring.gpuMs=gpuFrames.Count>0?gpuFrames[gpuFrames.Count/2]:0;
   measuring.batches=batchFrames.Count>0?batchFrames.Average():0;measuring.triangles=triangleFrames.Count>0?triangleFrames.Average():0;
   if(measuringReference)reference=measuring;else candidate=measuring;
   CancelMeasure();message="Ölçüm tamamlandı. Sonuçları dışa aktarabilirsin.";
  }
  Repaint();
 }
 void CancelMeasure() { measuring=null;gpu.Dispose();batches.Dispose();triangles.Dispose();gpu=default;batches=default;triangles=default; }
}
