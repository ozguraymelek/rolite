# Rolite modül mimarisi ve bağımlılık sözleşmeleri

## Evrensel kurallar

- Her gameplay sistemi `partial struct ISystem` ve `[BurstCompile]` olarak yazılır. Sadece Unity
  callback'i, Companion `Animator`, UI veya disk I/O gerektiren sınırlar managed kalır.
- Bir modül başka modülün sistemini çağırmaz. Sahip modül `IComponentData`,
  `IBufferElementData` veya transient event buffer'ı tanımlar; tüketiciler yalnızca bu veriyi okur.
- Kalıcı küçük veri `IComponentData`, değişken sayılı ilişki/istek `DynamicBuffer`, tek-frame
  bildirimi event buffer'ıdır. Add/Remove component yapısal değişimdir ve entity'yi archetype
  chunk'ları arasında taşır; sadece gerçekten gerektiğinde `EntityCommandBuffer` kullanılır.
- Sistemler `[DisableAutoCreation]` taşır. Bootstrap, `SystemHandle` değerlerini seçili gruba
  açıkça ekler. Attribute ile dolaylı sıralama kullanılmaz.

## Modül haritası

| Modül | Sahibi olduğu veri | Girdi / çıktı sözleşmesi | Çalışma alanı |
|---|---|---|---|
| `Rolite.Shared` | Ortak ID, sabit enum ve çapraz-modül event temel tipleri | Yalnız veri; sistem içermez | Bağımlılık grafiğinin kökü |
| `Rolite.Core` | `GameState`, state request/event buffer'ları | Tüm modüller state okur; yalnız Core state yazar | Initialization, GameState, Simulation, Presentation grup kökleri |
| `Rolite.Lifecycle` | Spawn/despawn request, yaşam, ölüm, zorluk | Combat hasar event'i alır; spawn/death event'i yayınlar | Simulation'ın ilk kısmı, ECB playback sınırı |
| `Rolite.HybridVisual` | Companion referansı, görsel durum önbelleği | Simulation transform/animation state okur; Animator'a yazar | Presentation; managed ana iş parçacığı sınırı |
| `Rolite.Stats` | Base stat, hesaplanmış stat, modifier buffer | Classes/Runes/Inventory modifier üretir; Combat sonucu okur | Simulation, hasar ve hareketten önce |
| `Rolite.CharacterClasses` | ClassDefinition blob/authoring sonucu, class tag | Stats ve Skills için başlangıç verisi yayınlar | Baker + spawn sonrası ilk kurulum |
| `Rolite.Movement` | Input intent, velocity, facing, dash state | Input bridge intent yazar; Physics/Visual konum okur | Simulation; Physics'ten önce intent, sonra entegrasyon |
| `Rolite.Inventory` | Slot buffer, equipped weapon referansı | Loot request alır; Combat'a weapon profile verir | Simulation, stat hesaplamasından önce |
| `Rolite.Combat` | Attack intent, hitbox/projectile, damage event | Movement/Skills intent üretir; Lifecycle hasar tüketir | Simulation, Physics query sonrasında |
| `Rolite.Skills` | Skill slot/cooldown buffer, cast request | Input/Runes cast üretir; Combat effect event'i alır | Simulation, Combat öncesi karar aşaması |
| `Rolite.Runes` | Seçili rune buffer, trigger modifier'ları | OnHit/OnDash/OnKill event'i okur; Stats/Combat request yazar | Simulation, kaynak event üretiminden sonra |
| `Rolite.Progression` | XP, level, meta node unlock verisi | Encounter XP verir; Persistence yükler/kaydeder | Simulation ve run sonu |
| `Rolite.Encounter` | Room state, wave queue, quest progress | Lifecycle death event'i alır; spawn request üretir | Simulation, Lifecycle event'inden sonra |
| `Rolite.PersistenceUI` | Managed DTO, save request/result | Core/Progression snapshot okur; UI intent buffer'a çevirir | Presentation/uygulama yaşam döngüsü |

## Modül 1 — Core akışı

```text
Unity PlayerLoop
  → Rolite Game World
    → Initialization group
      → CoreWorldInitializationSystem.OnCreate (bir GameState singleton'ı kurar, kendini kapatır)
    → GameState group
      → GameStateEventCleanupSystem (önceki frame event'lerini Clear)
      → GameStateTransitionSystem (request buffer'ını FIFO tüket, GameState'i güncelle, event yaz)
    → Simulation group (gelecek saf gameplay ISystem'leri)
    → Presentation group (gelecek managed köprüler)
```

### Core entity archetype'i

Tek global entity'nin archetype'i aşağıdaki üç türdür:

```text
GameState + GameStateTransitionRequest buffer + GameStateTransitionEvent buffer
```

`GameState` sabit boyutlu chunk verisidir. İki buffer da entity üzerinde sürekli bulunur; bu nedenle
geçişte component eklenip çıkarılmaz. `Value` değiştirmek ve `Version` artırmak sadece mevcut chunk
belleğine yazmaktır. Buffer'ın inline kapasitesini aşan sıra dışı yoğunlukta ECS harici buffer belleği
kullanır; normal frame'lerde `Clear()` kapasiteyi koruduğu için yeniden tahsis etmez.

### Query ve veri erişimi

`GameStateEventCleanupSystem` query'si `GameState` + event buffer ister. `RequireForUpdate`, singleton
yaratılana kadar sistemi uyutur. `GameStateTransitionSystem` üç türü birden ister; böylece eksik
kurulumdaki entity'leri işlemeye çalışmaz. `GetSingletonEntity()` kasıtlı bir invarianta dayanır:
global state birden fazlaysa hata görünür olur; sessizce ilk entity'yi seçmek daha tehlikelidir.

Request buffer'ına aynı frame'de birden çok istek yazılabilir. Transition sistemi index sırasıyla
işlediği için geçişler deterministik FIFO'dur. Aynı hedefe tekrarlanan istek event üretmez. Tüketiciler
event buffer'ını Transition sisteminden sonraki grupta okur; sonraki frame cleanup sistemi onu siler.

### Neden ISystem ve nerede managed sınır var?

Üç Core işlem sistemi `ISystem` value type'ıdır; query handle dışındaki managed durum tutmaz ve Burst
derlemesine uygundur. Bu, SystemBase sınıf tahsisini ve managed dispatch'i kaldırır. Buna karşılık
`ICustomBootstrap`, `ComponentSystemGroup` ve debug `MonoBehaviour` Unity/Entities entegrasyon API'leri
nedeniyle managed kalır. Gruplar oyun verisi işlemez; onların tek görevi unmanaged `SystemHandle` değerlerini
elle seçilmiş sırada PlayerLoop'a bağlamaktır.

## Gelecek modüllerin ayrıntılı uygulama sınırları

### 2. Lifecycle & Physics

`Rolite.Lifecycle`, `SpawnRequest` ve `DespawnRequest` buffer'larını world singleton'ında; `Health`, `DamageRequest`, `DeathTag` ve difficulty bileşenlerini actor entity'lerinde sahiplenir. Frame sırası `SpawnRequestCollect → ECB playback → DamageResolve → DeathPublish → DespawnCollect` olur. Producer'lar doğrudan entity yaratıp silmez: paralel sistemler request/event üretir, tek ECB playback noktası yapısal değişiklikleri uygular. Unity Physics dünyası bundan sonra oluşur; Movement ve Combat yalnızca collision world'ü query eder. Difficulty singleton'ı spawn bütçesi, düşman health/hasar çarpanı, elite olasılığı ve wave yoğunluğunu tek veri kaynağından etkiler.

### 3. Hybrid Visual & Animation Bridge

`Rolite.HybridVisual`, Baker'ın ürettiği entity üzerinde `CompanionLink` managed component referansını ve Burst sistemlerinin yazdığı blittable `AnimationState` verisini ayırır. Simulation, `AnimationState` değerini (`Idle`, `Run`, `Attack`, `Dash`, `Die`, normalized speed) yazar; Presentation sonunda tek bir managed bridge bu değeri `Animator` parametrelerine kopyalar. Böylece Animator sistemlere bağımlı olmaz ve Burst tarafına sızmaz. Kamera da `CameraFollowTarget` verisini okuyan managed köprüdür; entity transform'u ana otoritedir, GameObject transform'u kopyadır.

### 4. Stats & Attributes

Her actor `BaseStats` ve `ResolvedStats` IComponentData'sını, değişken sayılı `StatModifier` buffer'ını taşır. `StatsResolveSystem`, önce base değeri kopyalar, modifier'ları sabit öncelik sırasıyla uygular, sonucu `ResolvedStats`a yazar. Böylece Combat aynı frame içinde hesaplama yapmaz. Modifier buffer, geçici buff/debuff için component ekleyip kaldırmaktan ucuzdur; ancak modifier kimlikleri ve stack kuralı açıkça tanımlanmalıdır. Runes, Inventory ve Skills yalnız modifier üretir; stat hesaplayıcısını çağırmaz.

### 5. Character Classes

ScriptableObject class tanımı Baker ile immutable `BlobAssetReference<ClassDefinitionBlob>`a dönüştürülür; actor entity'si yalnız `ClassDefinitionReference` tutar. Blob, binlerce aynı sınıf entity'si tarafından paylaşılır ve Burst ile okunur; runtime'da tek actor'a özel değiştirmek için uygun değildir. `ClassSetup` sistemi başlangıç stat modifier'ı, başlangıç silahı ve skill slot request'lerini üretir. Yeni sınıf eklemek, veri varlığı eklemekle sınırlı kalır; sınıfa özel switch-case sistem kodu yazılmaz.

### 6. Movement & Input

Managed Input System adapter'ı cihaz verisini `PlayerInputIntent` IComponentData'sına yazar. Unmanaged Movement ISystem'leri `Intent → DashDecision → Velocity → LocalTransform/Facing` sırasıyla çalışır. `DashState` cooldown/zaman verisini, `MovementBlockTag` ise ölüm/stun gibi kilitleri taşır. Input adapter yalnızca intent üretir; fizik, animasyon veya saldırı çağırmaz. Isometrik yön dönüşümü saf matematik sisteminde yapılır; raycast gerektiğinde Unity Physics collision world'ü read-only sorgulanır.

### 7. Inventory & Weapons

`InventorySlot` DynamicBuffer, ekipman sıralamasını; `EquippedWeapon` ise aktif slot referansını taşır. Silahın ortak davranışı `WeaponProfileBlob` ile verilir; sword, pistol, AR, SMG, sniper ve staff aynı profile şemasını kullanır. `EquipRequest` tüketicisi silahı seçer ve Stats'a modifier request, Combat'a attack profile erişimi sağlar. Inventory Combat'a sistem çağrısı yapmaz; Combat yalnız aktif profile bakar.

### 8. Basic Attack & Combat

`AttackIntent` buffer'ı input/AI'dan gelir; `AttackState` combo zamanlamasını taşır. Combat sırası `IntentConsume → ComboAdvance → Hitbox/ProjectileSpawnRequest → PhysicsQuery → DamageEvent` olur. Yakın dövüş hitbox'ı kısa ömürlü entity veya query verisi; mermi ise enableable `Projectile` component'i olan havuz entity'sidir. Enable/disable, create/destroy'e göre archetype taşınmasını azaltır. Hasar sonucu Lifecycle'a `DamageRequest` olarak gider; Combat Health component'ini doğrudan değiştirmez.

### 9. Skills

Actor üzerinde `SkillSlot` buffer'ı bulunur; her eleman skill ID, cooldown bitiş zamanı ve seviye taşır. `SkillCastRequest` producer'lardan gelir, `SkillCooldownSystem` izin kontrolü yapar, sonra hedef/alan/effect request'i üretir. Active skill davranışı data-driven effect tanımıyla seçilir; Combat veya Movement bu request'i kendi alanında tüketir. Böylece cooldown hesabı ile hasar/teleport davranışı birbirine bağlanmaz.

### 10. Dynamic Runes

`EquippedRune` buffer'ı run boyunca seçilen boon'ları tutar. Combat'ın `OnHit`, Movement'ın `OnDash` ve Lifecycle'ın `OnKill` event buffer'ları, Rune trigger sistemlerinin girdisidir. Rune sistemi bunları `StatModifier`, `ExtraDamageRequest`, `CastRequest` veya `SpawnRequest`a çevirir. Event'ler transient, rün seçimi kalıcıdır; bu ayrım bir trigger'ın yanlışlıkla sonraki frame tekrar çalışmasını önler.

### 11. Skill Tree & Progression

Run entity'si `Experience`, `RunLevel` ve `LevelUpChoiceRequest` taşır; profil entity/DTO'su kalıcı `UnlockedNode` verisine sahiptir. Encounter XP event üretir, Progression bunu resolve eder, UI'ye seçim request'i yollar. UI yalnız `ChooseUpgradeIntent` üretir; node doğrulaması Progression'da yapılır. Run içi veri bir koşu sonunda silinir, meta veri PersistenceUI tarafından serialize edilir; ikisi aynı component'te tutulmaz.

### 12. Quest & Encounter

`RoomState`, `WaveDefinitionReference`, `PendingSpawn` buffer'ı ve `QuestProgress` bu modülün verisidir. Lifecycle'ın death event'i `WaveProgressSystem` tarafından sayılır; dalga tamamlanınca room state değişir, gerekli spawn request'leri Lifecycle'a yazılır. Oda sistemi düşman prefabını doğrudan instantiate etmez. Bu sınır pool, zorluk ve despawn kararlarının tek yerde kalmasını sağlar.

### 13. Serialization & UI Bridge

Bu tek modül disk I/O ve Unity UI için managed sınırdır. Saf ECS snapshot sistemleri `RunSaveSnapshot` buffer/DTO'su hazırlar; managed serializer bunu JSON dosyasına yazar. Load sonucu `LoadRequest` olarak ECS'e döner ve Lifecycle/Progression sahip olduğu veriyi kurar. UI köprüsü health, rune seçimi ve pause gibi görünür durumları read-only izler; kullanıcı eylemini request buffer'ına dönüştürür. Böylece UI, Health veya GameState'i doğrudan mutasyona uğratmaz.
