# Benzinlik camı

URP 17 / Unity 6.0 için çift taraflı, kalıcı kir varyantlı ve baked/canlı hibrit HDR cam.

## Kurulum

Mevcut prototip sahnesindeki 10 cam kuruludur. Çalışan camı çoğaltıp yerleştir ve sahneyi kaydet; yeni camlar otomatik kaydolur.

İlk kurulum veya çevreyi yeniden bake etmek için `Tools > Foggy Road > Gas Station Glass > Install Hybrid on All Scene Windows` kullan. Bu işlem açık prototip sahnesini önce `Backups` klasörüne kopyalar, ortak kir dizisini ve bölgesel cubemap'leri hazırlar, cam/aktör bağlantılarını kurar ve sahneyi kaydeder. Eski `Install on Selected Windows` / `Bake Local Reflection Probes` komutları eski planar kurulum içindir; güncel hibrit kurulumu yenilemek için kullanılmamalıdır.

`Gas Station Dynamic Glass` Inspector'ında **Hybrid** varsayılandır. Baked modunda canlı çevre renderı yoktur. Realtime, önceki sürekli planar davranışını karşılaştırma için korur. Aktif grup, aktör, toplam capture ve son aktivite nedeni Inspector'da görünür. Editörde varsayılan baked önizlemedir; canlı önizleme için `Preview In Scene View` ve gerektiğinde `Realtime` açılır.

Hibrit kurulum camları taşımaz veya farklı düzlemleri zorla birleştirmez. Aynı düzlemdekiler bir grubu paylaşır. Mevcut ayrı Forward+ renderer canlı capture için kullanılır; baked capture'lar runtime'da güncellenmeyen, açık cubemap referanslı Custom probe'lardır. Camın pozitif/negatif yüzü, doğru yarı alandaki en yakın ortak cubemap'i box projection ile örnekler; probe hacimlerinin üst üste binmesi iç/dış yüzü yanlış karıştırmaz.

## Aktörler ve geçiş

`CarController` araçları Awake'te otomatik `ReflectionActor` kaydı yapar. Kurulum görünür gövdesi olan karakterleri de bağlar. Yeni NPC veya script'siz hareketli modelin köküne `ReflectionActor` ekle; pool etkinleştirme/devre dışı bırakma desteklenir. Render edilemeyen kontrolcü, kamera altındaki birinci şahıs proxy'leri, particle/trail ve sadece gölge çizen renderer'lar aktivite sayılmaz. Bileşenin `activityLights` alanı harici değişen ışıklar içindir; çocuk farlar varsayılan olarak takip edilir.

0.1 saniyelik aday kontrolü camın sanal yansıma görüşü ve cam açıklığı üzerinden yapılır. Oyuncunun arkasındaki araç da görünür olabilir. Hızlı araçlar için 0.25 saniyelik hız öngörüsü ve pay kullanılır. Konservatif üç ışınlı collider testi tamamen engellenmiş aktörleri eler; eksik collider veya kısmi örtülme durumunda fazla canlı render yapılabilir.

İlk geçerli capture'dan sonra 0.4 saniyede canlıya geçilir. Son aktör çıktıktan sonra 1.5 saniye beklenir ve 0.4 saniyede baked'e dönülür. Park eden araç/duran kişi görünürken canlı mod açık kalır. Oyuncu gövdesi de yansıyorsa boş durum olmayabilir. Boşta planar render gönderilmez; transmission blur için gerekli ortak opaque renk kopyası çalışmaya devam edebilir. Kullanılmayan yansıma kamera/RT'si 3 saniyede bırakılır.

`Player Camera` oyuncunun gerçek kamerası olmalı. Boş bırakılırsa aktif `Camera.main`, o da yoksa aktif ana Game kamerası aranır. Scene View önizlemesi editörde son aktif Scene View kamerasını kullanır. Mobil veya başka bir URP asset'ine geçilirse uyumsuz renderer yerine probe yansımasına dönülür; o pipeline için kurulum tekrar yapılabilir.

## Görünüm

`Station Dusty Glass` ve mevcut `glass mat` materyallerinde ön yüz yansıtıcılığı 0.04, temiz smoothness 0.97, toz 0.48, toz opaklığı 0.55 ve eskitme 0.18 ayarlandı. Shader'ın ham varsayılan tozu 0.22'dir. Gönderilen iki görsel `Textures/DustSource.png` ve `WearSource.png` olarak saklanır. Kullanılan `GlassSurfaceMasks.png` dokusunda R toz, G çizik/silme/su izidir. Doku lineer, mipmap açık, PC'de BC7 ve Read/Write kapalıdır.

Editörde bu shader'ı kullanan bir materyali plane/quad üzerine bırakmak yeterlidir: `GlassSurfaceVariation` otomatik eklenir. Kopyalanan ve prefab olarak eklenen her cam ayrı, kalıcı bir kimlik alır. `GlassSurfaceVariants.asset`, 12 adet 1024x1024, BC7 sıkıştırılmış ve mipmap'li R=toz/G=eskitme katmanı içerir. Mevcut 10 cam farklı taban varyantlarına sahiptir. Bağımsız kenar şeritleri, alt birikim yüksekliği, temizlenmiş bölgeler ve kimliğe bağlı kırpma/yatay aynalama büyük kir şekillerini de farklılaştırır. Aynı iki kaynak görselden gelen küçük ortak ayrıntılar kalabilir. Alt kir altta, su izleri dik kalır.

Cam Inspector'ında varyant numarası ve kalıcı kimlik görünür. `Regenerate selected dust patterns`, kaydet/yükle kimliğini değiştirmeden desen revizyonunu artırır. Değişikliği Ctrl+S ile kaydet. Sahneyi yeniden açma, Play Mode veya enable/disable deseni değiştirmez. Dizi ortaktır; cam başına materyal/doku oluşturulmaz ve runtime maske üretilmez.

Her pencere ayrı bir düzlemsel `MeshRenderer` olmalıdır. Tek mesh içindeki bütün pencereler aynı kimliği paylaşır. Editörde okunamayan mesh de bake edilir; runtime'da ilk kez oluşturulan mesh için okunabilir geometri veya önceden bake edilmiş prefab gerekir.

Oyun sırasında materyal atanan yeni bir cam için `GlassSurfaceVariation.RegisterGlass(renderer)` çağır. Önceden bileşenli prefab klonları kendiliğinden kaydolur. Kaydet/yükle sistemindeki bir pencere için dönen bileşende `SetStableId(kaydedilenPencereKimligi)` kullan; aynı kimlik iki pencereye atanamaz. Runtime klonlarının rastgele kimliği yeni oyun oturumunda kendiliğinden korunmaz; kalıcılık gerekiyorsa kimliği kaydet/yükle.

`Dust Amount` ve `Wear Amount` bütün camların temel miktarıdır; cam başına yoğunluk bunun yaklaşık %75–125'i arasında değişir. Kirli bölgeler daha mat, hafif daha opak olur ve yansımaları daha bulanık gösterir. Merkezde temiz cam görünümü korunur. Shader iki yüzü de çizer; aynı camın iç ve dış yüzünde aynı fiziksel kir yerleşimi görünür.

Koyu kaynak maskelerine coverage dönüşümü uygulanır; sıfır değer temiz kalır, gri birikimler görünür hale gelir. Roughness ayrı bir eğriyle yükseltilir. `Dirty Glass Transmission Blur (1080p pixels)` kirli yerlerde arka görüntünün dağılmasını ayarlar; mevcut materyallerde 5'tir. 0 yapıldığında yalnızca yansımanın roughness bulanıklığı kalır. Oyun kamerasının URP opaque görüntüsü dört örnekle kullanılır: yalnızca opaque nesneler ve gökyüzü dağılır, daha sonra çizilen saydam nesneler bu görüntüde yoktur. Bu bir ekran görüntüsü yaklaşımıdır, fiziksel hacimsel saçılma değildir.

Kontrolcü, yakın ve görünür cam olduğunda kaynak Game kamerası için opaque copy açar; uzaklaşınca/kapatılınca önceki kamera ayarını geri yükler. Yansıma kameralarında copy kapalıdır. Scene View'da arka görüntü bulanıklığı yalnızca pipeline Opaque Texture destekliyorsa görünür; oyuncu kamerasındaki sonuç için Play Mode'a bak. Toz ve yansıma roughness'ı Scene View'da da uygulanır.

Kaynak PNG'leri değiştirirsen `Bake Distinct Surface Variants` ile diziyi yenile. `Pack Dust and Wear Masks` eski tek paket modunu yeniler; yoğunluk ayarlarını sıfırlamaz. Siyah temiz, beyaz yoğun iz olmalıdır. `Use Distinct Surface Variants` kapatılarak eski paket karşılaştırılabilir. Normal map gerekmez.

Gerçek zamanlı yansıma sahneyi mevcut ışıklarıyla yeniden render eder. Oyuncunun tam beden yansıması için render edilebilir bir model gerekir. Birinci şahıs kollarını yansımadan çıkarmak için ayrı bir layer kullanıp `Reflection Layers` üzerinden kapat. Shader camdan cama yansımayı ve fiziksel ışık kırılmasını simüle etmez. Çok hafif cam rengi yüzey haze katkısıyla yaklaşık verilir.

Script/Animator ile açılan bir kapının camını ayrı `Window Group` içinde tut. Kurulum Animator/Rigidbody altındaki veya adında `door`/`kapı` geçen camları otomatik ayırır. Ayrı bir grubun plane transform'u kapıyla birlikte hareket etmelidir.

## Performans ve ölçüm

- 1080p için yaklaşık 960 × 540 HDR doku; her grup aynı düzlemdeki tüm camlarına bu dokuyu verir.
- Hybrid'de en fazla iki gerekli/görünür grup, grup başına en fazla 30 Hz ve kare başına en fazla bir capture. Öncelik ekran alanı, sonra mesafedir. İki aktif grup ve düşük oyun FPS'sinde her grubun gerçek hızı 30 Hz'den düşük olabilir. Bütçe dışındaki gruplar baked kalır. Realtime modunda önceki yakın/her-kare ve uzak/30 Hz davranışı korunur.
- Yansıma oyuncu kamerasının çizim mesafesini, mevcut LOD bias'ını ve pipeline gölge mesafesini kullanır. Görünümü sınırlayan son 35 m / 20 m / LOD 0.4 / yakın 30 Hz optimizasyonu kullanıcının isteğiyle geri alındı.
- Ekran alanı %1 altında olan veya kamera görüş alanı dışındaki gruplar render üretmez. Görüş alanı kontrolü duvar arkasında kalmayı tespit eden bir occlusion testi değildir.
- Yansıma kamerasında post-processing, ana renderer özellikleri ve MSAA çalışmaz. UI ve TransparentFX layer'ları başlangıçta dışarıda bırakılır. Küçük dekor/efektleri ayrı layer'larla çıkarabilirsin; `Props` layer'ının tamamı otomatik çıkarılmaz.
- Temiz/kirli yansıma mip seviyeleriyle bulanıklaştırılır; bu, filtrelenmiş gerçek fiziksel roughness'ın yaklaşık karşılığıdır.
- Desen için cam başına ayrı materyal, doku veya kamera oluşturulmaz. Parametreler MaterialPropertyBlock ile, mevcut yansıma parametreleri korunarak atanır. Bu yöntem URP SRP Batcher uyumluluğunu etkileyebilir; draw-call maliyetini Profiler'da ölç. Runtime'da her kare bütün camları arayan bir toz güncellemesi yoktur.
- Arka görüntü bulanıklığı görünür camlar için ortak kaynak kamera renk kopyasını kullanır; cam başına kopya yoktur. Kirli piksellerde dört ek texture örneği alınır. GPU maliyeti sıfır değildir ve ölçülmelidir.
- Yerel baked probe'lar güncel hareketli nesneleri veya sonradan değişen ışığı yansıtmaz. Ortamı değiştirdikten sonra yeniden bake gerekir. Bake'a girecek çevre nesnelerinin `Reflection Probe Static` ayarını kontrol et.

Unity Profiler'da `Gas Station Glass / Planar Reflection` ve GPU render maliyetini, aynı oyuncu rotasında sistem açık/kapalı karşılaştır. 1080p / 60 FPS hedefi ve yaklaşık 2 ms ek GPU bütçesi bir ölçüm hedefidir, garanti değildir. Bütçe aşılırsa önce Resolution Scale 0.25'e, sonra mesafeleri 10/18/22 metreye düşür. Gerekirse Max Active Groups = 1 seç.

Game View Stats üçgen sayısı camın kendi mesh'ini değil, kamera ve gölge geçişleri dahil çizimleri sayar. Planar yansıma sahneyi tekrar çizdiği için sayı artar; renk kopyası ve toz texture örneklemeleri ek üçgen üretmez. Resolution Scale düşürmek piksel maliyetini azaltır, tek başına geometri sayısını azaltmaz. Önceki geometri audit raporu Recovery/GlassVariationValidation altında tutulur; bounds bazlı tahmin gerçek draw sayısı değildir.

Yakın/uzak, iç/dış, kapı hareketi, ışık değişimi ve hareketli karakter kontrollerini gerçek benzinlik sahnesinde yap. Aynı anda üçüncü bir cam düzlemi görünürse iki yakın grup dinamik, diğerleri probe kullanır. Önizlemedeki ışık/exposure ile oyuncu kamerasının görüntüsü farklı olabilir.

Sistem kapatıldığında planar ağırlıkları sıfırlanır, ek kameralar ve render dokuları serbest bırakılır.

## Doğrulama

`Validate Hybrid and Compare 1080p`, mevcut cam kimliklerini/MPB'lerini ve iki tarafın baked bağlantılarını kontrol eder; aynı geçici kamerayla dört modu karşılaştırır. `Library/GlassHybridSceneCheck.txt` zamanları senkron offscreen render + GPU readback içerir; ayrı CPU/GPU süresi veya Game View FPS değildir. Gerçek oyuncu rotası GPU/CPU/p95 karşılaştırması Unity Profiler'da yapılmalıdır. Test raporları ve görsel varyant karşılaştırması `Recovery/GlassVariationValidation` altında tutulur. Tam canlı modda ağır çevre geometri maliyeti geri gelir; baked ile tam planar parallax aynı değildir.

## Tag ile araç / NPC kaydı

`Car` ve `NPC` tagleri projeye eklendi. Trafik aracının ana objesine `Car`, insan/NPC prefabının ana objesine `NPC` ver; alt meshleri ayrı ayrı tagleme. Hybrid modda ortak keşif en fazla 0.5 saniyede bir çalışır ve gerekli `ReflectionActor` bileşenini runtime'da ekler. Spawn ve pool'dan açılan nesneler bir sonraki keşifte bulunur; kapananlar kayıttan çıkar. İlk kez spawn edilen nesnede algılama gecikmesi en fazla yaklaşık 0.5 saniyedir. Tag, sadece canlı yansıma adayı olmasını sağlar; camın yansıma görüşüne girmeyen nesne capture başlatmaz.

Mevcut `CarController` kullandığın aracı otomatik kaydetmeye devam eder. Elle eklenmiş `ReflectionActor` da desteklenir; mevcut Player/başka tagini değiştirmek zorunda değilsin. Yalnızca tag keşfinin eklediği bileşen, `Car`/`NPC` tagi kaldırılınca kaldırılır. Bileşeni elle kapatırsan tag keşfi tekrar açmaz. Statik dekorları `Car` yapma: yansımada duran tagli araç da canlı modu açık tutar.

## Cam probe'larının çevreden ayrılması

Cam yöneticisinin altındaki Custom probe bileşenleri yalnızca cubemap ve box projection verisi taşır; `enabled=false` kalmalıdır. Shader dokuyu doğrudan okur, bu nedenle cam yansıması çalışır. Bu probe'ları açmak, Unity'nin otomatik probe ataması yoluyla zemin/duvar/metal yüzeylerin yansımalarını da değiştirir. `RefreshBakedBindings` ve yeni bake kurulumları bileşenleri kapalı tutar. Editör yükseltmesi mevcut prototype sahnesini yedekleyip bu durumu kaydeder. Cam yöneticisi açık kalabilir; eski, artık kullanılmayan probe'lar da kapalıdır.
