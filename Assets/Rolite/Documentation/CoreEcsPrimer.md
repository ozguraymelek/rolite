# Core ECS primeri

Bu belge her konuyu önce teknik, hemen ardından başlangıç seviyesi diliyle açıklar.

## Burst uyarısı ve çözüm

**Teknik açıklama:** `state.GetEntityQuery(ComponentType.ReadOnly<T>(), ...)` overload'ı `params ComponentType[]` kullanır. C# bu çağrı için managed bir `ComponentType[]` dizisi üretir. `[BurstCompile]` içindeki imzada managed tür bulununca Burst native kod üretmez ve `signature contains managed types` uyarısını verir. `EntityQueryBuilder(Allocator.Temp)` ise bir `ref struct`tır; geçici tür listesini unmanaged allocator içinde tutar. `WithAll` ve `WithAllRW` sonrasında `Build(ref state)` query'yi sistemin yaşam süresine bağlar ve managed dizi oluşturmaz.

**Başlangıç seviyesi açıklama:** Burst, yalnız sade ve hızlı veriyle çalışmak ister. Eski çağrı arka planda küçük bir C# liste nesnesi yapıyordu. Yeni builder aynı filtreyi kısa süreli ECS belleğinde kurar. Sonuçta bulunan entity'ler aynıdır; uyarı ortadan kalkar.

## Singleton ve `GetSingletonEntity`

**Teknik açıklama:** Singleton özel bir entity türü değildir; bir `EntityQuery`nin tam olarak bir entity döndürmesi kuralıdır. `query.GetSingletonEntity()` bu entity'nin `Entity` handle'ını döndürür. Sıfır ya da birden fazla sonuç varsa güvenlik kontrolleri hata verir. Metot component döndürmediği için `IComponentData`, `IBufferElementData` ya da ikisini birlikte taşıyan herhangi bir singleton entity için kullanılabilir.

**Başlangıç seviyesi açıklama:** Singleton, odada yalnızca bir oyun yöneticisi olması kuralıdır. `GetSingletonEntity`, o yöneticinin kartvizitini verir. Bu kartvizitle state kartını da event listesini de açabilirsiniz. İki yönetici varsa Unity hangisini istediğinizi bilemez; hiç yoksa da çalışacak yönetici yoktur.

Oyun durumu, global zorluk, tek aktif run veya tek spawn koordinatörü singleton için uygundur. Oyuncu, düşman ve mermi gibi birden fazla olabilen şeyler için uygun değildir. Sıfır/bir adet olması normalse `TryGetSingleton...`; kesin bir adet olması normalse `RequireForUpdate(query)` ardından `GetSingletonEntity()` kullanın.

## `GetSingletonBuffer`, `GetSingletonRW` ve `GetBuffer`

**Teknik açıklama:** `query.GetSingletonBuffer<T>()`, query'nin bir entity eşleştirdiğini doğrular ve `T : unmanaged, IBufferElementData` buffer'ını verir. `query.GetSingletonRW<T>()`, `T : unmanaged, IComponentData` için `RefRW<T>` verir; query'nin T için RW erişimle kurulmuş olması gerekir. `EntityManager.GetBuffer<T>(entity)` ise entity handle zaten eldeyken buffer'a doğrudan ulaşır; singleton doğrulamasını query değil mimari kontrat yapar.

**Başlangıç seviyesi açıklama:** Tek global entity'yi arıyorsanız `GetSingletonBuffer` onun yapılacaklar listesini, `GetSingletonRW` tek ayar kartını düzenleme kalemini verir. Kartvizit zaten elinizdeyse `GetBuffer` ile listeyi doğrudan açarsınız. Bu projede önce kartviziti alıp sonra üç veriye de aynı entity üzerinden erişiyoruz.

```csharp
// Query GameState'i RW istiyorsa:
ref var gameState = ref query.GetSingletonRW<GameState>().ValueRW;
// Query'nin eşsiz entity'sindeki request listesi için:
var requests = query.GetSingletonBuffer<GameStateTransitionRequest>();
```

Kısa singleton API'leri query'nin gerçekten bir entity eşleştirdiği basit durumlarda iyi seçimdir. Birden çok component ve buffer'a erişirken mevcut `GetSingletonEntity() + EntityManager.Get...` yolu hangi entity'den ne okunduğunu daha görünür yapar.

## `IComponentData` yerine neden `IBufferElementData`?

**Teknik açıklama:** `IComponentData`, entity başına sabit boyutlu tek değerli veridir. `IBufferElementData`, entity üzerinde sıfır veya daha çok eleman taşıyan `DynamicBuffer`ın eleman türüdür. `GameState` tek `Value` ve `Version` taşıdığı için IComponentData'dır. Aynı frame'de birden çok modül geçiş isteyebileceği için request ve event türleri buffer elemanıdır. `Clear()` buffer kapasitesini korur, yalnız uzunluğu sıfırlar.

**Başlangıç seviyesi açıklama:** `GameState` tek satırlı bir ayar kartıdır. Request ve event'ler gün içinde birikebilen listelerdir. Tek kartı liste yapmak gereksizdir; birden çok isteği tek alana yazmak ise son isteğin öncekini ezmesine yol açar.

### Request, event, `Target` ve olası `Source`

`GameStateTransitionRequest`, “lütfen şu state'e geç” emridir; `Target`, varılmak istenen state'tir. Pause menüsü `Paused`, ölüm sistemi `GameOver`, ana menü `Menu` hedefli request ekler. `GameStateTransitionEvent`, “geçiş gerçekten oldu” makbuzudur; `Previous`, `Current` ve `Version` taşır. Aynı hedef zaten aktifse event üretilmez.

`Source` bugün zorunlu değildir çünkü sistem yalnız sonuca göre davranır. Telemetride geçişin UI mı ölüm mü ağ mı tarafından istendiğini görmek, çakışan isteklerde öncelik vermek, izin kontrolü yapmak veya farklı UI mesajı göstermek gerektiğinde eklenir. O durumda `TransitionSource : byte` enum'u (`Ui`, `PlayerDeath`, `Encounter`, `Network`, `Debug`) request'e eklenir. Serbest `string` yerine enum Burst, GC ve bellek açısından doğrudur.

## `GetEntityQuery` ve builder'ın arka planı

**Teknik açıklama:** EntityQuery, “şu component imzasına sahip archetype chunk'larını bul” planıdır; entity'leri C# listesinde tek tek tutmaz. `Build(ref state)` query'yi sistemin yaşam süresine bağlar. ECS eşleşen archetype/chunk listesini cache'ler. `WithAll<T>` component varlığını ve RO erişim niyetini, `WithAllRW<T>` varlığını ve yazma niyetini bildirir. Bu niyet dependency/safety sisteminin job'ları doğru sıraya koymasını sağlar.

**Başlangıç seviyesi açıklama:** Query, “kırmızı etiketi ve kılıcı olan kutuları göster” filtresidir. Unity bunu her karede depoyu baştan tarayarak değil, etiket kombinasyonlarına göre hazırlanmış indeks üzerinden yapar. Builder filtreyi sistem kurulurken hazırlar; oyun çalışırken aynı filtre yeniden kullanılır.

`GetEntityQueryDescs()` query'nin iç tanım(lar)ını geri verir; debug, araç geliştirme ve inceleme içindir, frame içi gameplay için değildir. `GetEntityQueryMask()` hızlı bir üyelik maskesidir; `mask.Matches(entity)` ile tek bir entity'nin query'ye uyup uymadığını denetler. Tüm eşleşmeleri dolaşmak için query yerine kullanılmaz.

## MonoBehaviour cümlesinin anlamı

**Teknik açıklama:** MonoBehaviour, GameObject'e bağlı managed C# nesnesidir. Inspector serileştirmesi, Unity callback'leri ve `Animator` referansları için uygundur; fakat managed heap, GC ve ana iş parçacığı UnityEngine API bağımlılığı vardır. `IComponentData`, unmanaged/blittable veri olarak chunk'ta saklanır; Burst/job ile çok sayıda entity üzerinde verimli işlenir.

**Başlangıç seviyesi açıklama:** MonoBehaviour Inspector'da rahatça tıklayıp ayarladığınız sahne bileşenidir. Kolaydır ama binlerce düşmanda Unity'nin tek tek ilgilenmesi gerekir. IComponentData ise çok sayıda küçük kartı düzenli kutularda tutar; hızlıdır ama Inspector ve Animator'ı doğrudan taşımaz. Bu yüzden oyun kararı ECS'te, görsel köprü MonoBehaviour'da kalır.

## World, gruplar ve sistem handle'ları

**Teknik açıklama:** `World`, kendi EntityManager'ı, entity/chunk depoları ve sistemleri olan ECS evrenidir. `ComponentSystemGroup`, child sistemleri sırayla güncelleyen container sistemidir. `ISystem` unmanaged value-type sistem tanımıdır; World içinde oluşturulunca `SystemHandle` elde edilir. `group.AddSystemToUpdateList(handle)` bu handle'ı grubun manuel update listesine ekler. `GetOrCreateSystem<T>()` unmanaged ISystem için handle döndürür. `GetOrCreateSystemManaged<T>()` managed group veya ComponentSystemBase nesnesi döndürür. `GetOrCreateSystemsAndLogException(...)`, birçok sistem type index'ini sırayla oluşturur, hatayı loglar ve default handle döndürür; bootstrap/araç işidir, sync point oluşturduğu için frame içinde kullanılmaz.

**Başlangıç seviyesi açıklama:** World oyun sunucusu, group işleri sırayla çağıran yönetici, ISystem işin tarifi, SystemHandle ise işin sıra numarasıdır. `GetOrCreateSystem` saf ECS işi için sıra numarası verir. `GetOrCreateSystemManaged` Unity nesnesi taşıyan yönetici/grup için gerçek C# nesnesi verir. `AddSystemToUpdateList`, “bu işi bu vardiyada çalıştır” demektir.

`World.DefaultGameObjectInjectionWorld`, authoring/Baker, hybrid ve legacy GameObject kodunun varsayılan ECS world'üne ulaşması için statik işarettir. Birden çok world varken rastgele kullanılmamalıdır; network client/server ve test world'lerinde ilgili world referansı açıkça geçirilmelidir.

### `WorldFlags`

`None` etiketsiz world; `Live` çalışan ana uygulama world'lerinin ortak biti; `Editor` editördeki canlı world; `Game` player'daki ana canlı oyun world'ü; `Simulation` AI/fizik/ağ gibi ek çalışan world; `Conversion` authoring'i runtime veriye çeviren world; `Staging` conversion ara alanı; `Shadow` diff/undo/Live Link için önceki durum world'ü; `Streaming` gelen stream verisi; `GameServer`, `GameClient`, `GameThinClient` netcode rolleridir. Rolite'in tek oyunculu ana dünyası `WorldFlags.Game`dir. Bunlar davranış mantığı değil world rol etiketidir.

## Entity Command Buffer (ECB)

**Teknik açıklama:** ECB, create/destroy entity veya Add/Remove component gibi yapısal değişimleri komut listesine kaydeder; seçilen playback noktasında EntityManager'a uygular. Paralel job'ların aynı anda world yapısını değiştirmesini önler. Component değerini değiştirmek (`SetComponentData`), mevcut buffer'a eleman eklemek/temizlemek veya enable bit değiştirmek çoğu zaman yapısal değişim değildir; ECB gerekmez.

**Başlangıç seviyesi açıklama:** Entity yaratmak ya da component eklemek depo raflarının yerini değiştirmek gibidir. Raflar sayılırken bunu aniden yapmazsınız; listeye yazıp vardiya sonunda uygularsınız. ECB bu listedir. Var olan kartın can değerini değiştirmek veya mevcut yapılacaklar listesine satır eklemek rafları taşımaz; ECB kullanmak gereksiz karmaşıklık yaratır.

ECB'yi job/parallel işlem içinde spawn-despawn, component ekleme-kaldırma ve gecikmiş playback için kullanın. OnCreate'da world henüz iterasyon yapmıyorken doğrudan EntityManager kullanmak güvenlidir. ECB'yi her normal değer yazımına sarmayın; gecikmiş görünürlük ve gereksiz playback maliyeti üretir.
