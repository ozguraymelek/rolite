## Bu modül, bu proje için özel bir ECS çalışma sırası (execution pipeline) oluşturuyor.

Hedef şunlar:
- ECS sistemlerinin otomatik olarak oluşturulmasını engellemek.
- Hangi sistemin hangi grupta çalışacağını belirlemek.
- Sistemlerin çalışma sırasını kontrol etmek.
- Oyun başlangıcını, oyun mantığını ve sunum katmanını birbirinden ayırmak.
- Mümkün olduğunca saff ISystem ve SystemHandle tabanlı mimari kullanmak.
- Sadece gruplama ve Unity PlayerLoop entegrasyonu için gereken managed katmanı kullanmak.

ComponentSystemGroup bir sistem değildir demek doğru değildir. Kendisi de bir ECS sistemidir ancak SystemBase sınıfından türetilen, içinde başka sistemleri çalıştıran özel bir managed sistemdir. Bu mimaride grup sistemleri managed, grupların içindeki oyun sistemleri ise unmanaged ISystem tabanlıdır. Bu sayede oyun sistemleri managed overhead olmadan çalışır ve gruplar, Unity PlayerLoop ile entegre olur.

### ECS içinde bir sistem, belirli bir işi gerçekleştiren yapıdır.
Örneğin bu proje için şu işlemler olabilir:
- Oyuncunun hareketini hesaplamak.
- Düşmanların hareketini hesaplamak.
- Hasar hesaplamak.
- Düşmanların ölüp ölmediğini kontrol etmek.
- Animasyon verilerini güncellemek.
- UI üzerindee can miktarını göstermek.

Bunların her biri ayrı ECS sistemleri olabilir.

Örneğin:
```csharp
public struct PlayerVelocity : IComponentData
{
    public float Value;
}
```
Bu component, oyuncunun hızını temsil eder/saklar. Şimdi oyuncunun hareketini hesaplayan bir sistem oluşturalım:
```csharp
public partial struct PlayerMovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Sistem oluşturulurken yapılacak işlemler.
    }
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var velocity in SystemAPI.Query<RefRO<PlayerVelocity>>())
        {
            // hareket hesaplaması burada yapılabilir.
        }
    }
}
```
Burada önemli olan şu ki, ***ISystem***, unmanaged system mimarisidir. Yani sistemin kendisi bir struct olarak tanımlanır. Klasik SystemBase gibi managed bir C# sınıfı değildir.

| Özellik                   | ISystem               | SystemBase                                       |
|---------------------------|-----------------------|--------------------------------------------------|
| Tür                       | struct                | class                                            |
| Managed                   | Hayır                 | Evet                                             |
| Sistem handle'ı           | SystemHandle          | SystemHandle                                     |
| Burst uyumluluğu          | Uygun koşullarda evet | Sistem kodu doğrudan aynı şekilde Burst çalışmaz |
| Ana kullanım              | Performans odaklı ECS | Managed ECS entegrasyonu                         |

**SystemHandle**, yalnızca ***ISystem*** sistemlerine ait değildir. Managed ve unmanaged ECS sistemlerinin ikisi de bir **SystemHandle** ile temsil edilir.  Dolayısıyla SystemHandle gördüğünde otomatik olarak ISystem olduğu anlamına gelmez.

# <span style="background-color: #252588; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">ComponentSystemGroup</span>
Diyelim ki oyunumuzda 10 farklı sistem var. Bunların hepsini tek bir yerde çalıştırmak yerine görevlerine göre gruplandırmak istiyoruz.

<div style="background-color: #090c15; padding: 20px; border-radius: 12px; font-family: sans-serif; color: #ffffff; max-width: 650px; box-sizing: border-box;">
  <!-- Üst Kutu -->
  <div style="background-color: #536100; border: 1px solid #1e293b; border-radius: 8px; padding: 18px; text-align: center; width: 100%; box-sizing: border-box;">
    <div style="font-weight: bold; font-size: 16px;">RogueliteRootSystemGroup</div>
    <div style="color: #000000; font-size: 13px; margin-top: 4px;">Ana ECS çalışma grubu</div>
  </div>
  <!-- Ok İşareti -->
  <div style="text-align: center; color: #94a3b8; font-size: 18px; margin: 10px 0;">↓</div>
  <!-- Alt Kutular (Tam Genişlikli 2x2 Grid) -->
  <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; width: 100%; box-sizing: border-box;">
    <div style="background-color: #0b2229; border: 1px solid #1e293b; border-radius: 8px; padding: 18px; text-align: center;">
      <div style="font-weight: bold; font-size: 15px;">Initialization</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Dünya kurulumu</div>
    </div>
    <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 18px; text-align: center;">
      <div style="font-weight: bold; font-size: 15px;">GameState</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Oyun akışı</div>
    </div>
    <div style="background-color: #261a12; border: 1px solid #1e293b; border-radius: 8px; padding: 18px; text-align: center;">
      <div style="font-weight: bold; font-size: 15px;">Simulation</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Oyun mekanikleri</div>
    </div>
    <div style="background-color: #1c1333; border: 1px solid #1e293b; border-radius: 8px; padding: 18px; text-align: center;">
      <div style="font-weight: bold; font-size: 15px;">Presentation</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Animator ve UI</div>
    </div>
  </div>
</div>

Bu yapıda **<span style="background-color: #536100; color: #ffffff; padding: 2px 6px; border-radius: 4px;">RogueliteRootSystemGroup</span>**, diğer grupları (**<span style="background-color: #0b2229; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Initialization</span>**, **<span style="background-color: #0e172a; color: #ffffff; padding: 2px 6px; border-radius: 4px;">GameState</span>**, **<span style="background-color: #261a12; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Simulation</span>**, **<span style="background-color: #1c1333; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Presentation</span>**) içerisinde bulundurur.
Diğer gruplar da kendi sistemlerini çalıştırır. Yani bir grup, doğrudan oyun mekaniğini gerçekleştirmek zorunda değildir. Görevi, alt sistemlerin hangi sırayla çalışacağını yönetmektir.

### <span style="background-color: #252588; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">ComponentSystemGroup</span> nasıl çalışır?
Unity'nin **<span style="color: #252588;">ComponentSystemGroup</span>** sınıfı, birden fazla ECS sistemini bir listede tutar. Bu listenin önemli bir özelliği vardır. <span style="color: #25ff88;">Hem managed sistemleri hem de unmanaged sistemleri destekler.</span> Kaynak kodu inceleyerek bunu görebiliriz:  
```csharp
public abstract unsafe partial class ComponentSystemGroup : SystemBase {
    // ...
    internal List<ComponentSystemBase> m_managedSystemsToUpdate;
    internal UnsafeList<SystemHandle> m_UnmanagedSystemsToUpdate;
    internal UnsafeList<UpdateIndex> m_MasterUpdateList;
    // ...
}
```
**m_managedSystemsToUpdate**, SystemBase gibi ***managed*** sistemleri tutar:
```csharp
public partial class ExampleManagedSystem : SystemBase
{
    protected override void OnUpdate() { }
}
```
**m_UnmanagedSystemsToUpdate**, ISystem gibi ***unmanaged*** sistemlerin SystemHandle değerlerini tutar.
```csharp
public partial struct ExampleUnmanagedSystem : ISystem
{
    public void OnUpdate(ref SystemState state) { }
}
```
**m_MasterUpdateList**, iki farklı sistem türünün çalışma sırasını bir arada yönetir. Örneğin bir grupta şu sistemler bulunsun:
1. PlayerMovementSystem (ISystem)
2. HealthSystem (ISystem)
3. AnimatorBridgeSystem (SystemBase)

Grup, bunları tek bir çalışma sırasına koyabilir. Yani managed ve unmanaged sistemler aynı grup içerisinde çalışabilir. Kaynak kodundaki `ComponentSystemGroup.UpdateAllSystems()` metodu bu işi gerçekleştirir.

- Grup, sistemleri listesine ekler.
- Gerekirse sıralama yapar.
- Her sistemin türünü kontrol eder.
- Managed sistemlerde `sys.Update()` çağırır. 
- Unmanaged sistemlerde `worldImpl.UpdateSystem(handle)` çağırır.

Böylece sistemler doğru sırayla çalıştırılır.

# <span style="background-color: #ff2588; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">DisableAutoCreation</span>
Normalde Unity ECS, sistemleri otomatik olarak keşfedebilir, oluşturabilir ve varsayılan World içerisine ekleyebilir. Örneğin:
```csharp
public partial struct EnemyMovementSystem : ISystem {
    public void OnUpdate(ref SystemState state) { }
}
```
Bu sistem, uygun World oluşturma sürecinde Unity tarafından otomatik oluşturulabilir. Ancak özel bir mimaride bütün sistemlerin otomatik oluşturulmasını istemeyebiliriz. Örneğin:
- `EnemyMovementSystem` sadece **<span style="background-color: #261a12; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Simulation</span>** grubunda çalışmalı.
- `PlayerMovementSystem` sadece **<span style="background-color: #261a12; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Simulation</span>** grubunda çalışmalı.
- `GameStateSystem` sadece **<span style="background-color: #0e172a; color: #ffffff; padding: 2px 6px; border-radius: 4px;">GameState</span>** grubunda çalışmalı.
- `AnimatorBridgeSystem` sadece **<span style="background-color: #1c1333; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Presentation</span>** grubunda çalışmalı.

Bu durumda sistemlerin oluşturulmasını ve hangi gruba ekleneceğini kendimiz yönetmek isteyebiliriz. İşte <span style="background-color: #ff2588; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">DisableAutoCreation</span> burada devreye girer.

```csharp
[DisableAutoCreation]
public partial struct EnemyMovementSystem : ISystem {
    public void OnUpdate(ref SystemState state) { }
}
```
Bu attribute, <span style="color: #25ff88;">"Unity, bu sistemi standart otomatik sistem keşfi ve oluşturma sürecinde kendiliğinden oluşturmasın"</span> der.
<span style="background-color: #ff2588; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">DisableAutoCreation</span>, sistemin çalışmasını tamamen yasaklamaz. Sadece otomatik oluşturulmasını engeller. Sistemi daha sonra kendimiz oluşturabilir ve çalıştırılacağı gruba ekleyebiliriz.

Diyelim ki projende 50 farklı sistem var. Bunların 20 tanesi Simulation, 10 tanesi Initialization, 10 tanesi GameState, 10 tanesi Presentation sistemleri. Otomatik oluşturma yaklaşımında sistemlerin hangi gruplara eklendiğini attribute'lar ve varsayılan World kuralları üzerinden yönetirsin. Manuel oluşturma yaklaşımında ise bütün kontrolü kendi bootstrap sürecine alabilirsin.
Böylece:
- İstemediğin sistemler oluşturulmaz.
- Sistemlerin yaşam döngüsünü kontrol edebilirsin.
- Her sistemin hangi gruba ait olduğunu açıkça belirleyebilirsin.
- Varsayılan `SimulationSystemGroup` bağımlılığını azaltabilirsin.

Fakat manuel yönetimin de maliyeti vardır. Sistemleri oluşturmak, gruplara eklemek ve yok etmeyi doğru yapmak senin sorumluluğundadır.

### <span style="background-color: #252588; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">ComponentSystemGroup</span> neden managed bir class?
```csharp
[DisableAutoCreation]
public sealed partial class RogueliteRootSystemGroup : ComponentSystemGroup { }
```
`ISystem` değil çünkü `ComponentSystemGroup`, Unity ECS'in mevcut grup yönetim mekanizmasını kullanır. Unity'nin kaynak kodundaki tanımlar:
```csharp
public abstract unsafe partial class ComponentSystemGroup : SystemBase
```
```csharp
public unsafe abstract partial class SystemBase : ComponentSystemBase
```
```csharp
public abstract unsafe partial class ComponentSystemBase
```

Yani:
<div style="background-color: #090c15; padding: 18px; border-radius: 12px; font-family: sans-serif; color: #ffffff; display: flex; align-items: center; justify-content: space-between; gap: 10px; max-width: 650px; box-sizing: border-box;">
  <div style="background-color: #252588; border: 1px solid #1e293b; border-radius: 8px; padding: 14px 16px; text-align: center; flex: 1; font-weight: bold; font-size: 14px;">
    ComponentSystemGroup
  </div>
  <div style="color: #94a3b8; font-size: 18px; font-weight: bold;">→</div>
  <div style="background-color: #0b2229; border: 1px solid #1e293b; border-radius: 8px; padding: 14px 16px; text-align: center; flex: 1; font-weight: bold; font-size: 14px;">
    SystemBase
  </div>
  <div style="color: #94a3b8; font-size: 18px; font-weight: bold;">→</div>
  <div style="background-color: #327fa8; border: 1px solid #1e293b; border-radius: 8px; padding: 14px 16px; text-align: center; flex: 1; font-weight: bold; font-size: 14px;">
    ComponentSystemBase
  </div>
</div>

Dolayısıyla <span style="background-color: #252588; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">ComponentSystemGroup</span> managed bir sistemdir.

### Neden <span style="background-color: #966906; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">ISystem</span> tabanlı özel bir grup oluşturmuyoruz?
Teorik olarak kendi `ISystem` yapımızı yazıp başka `SystemHandle` değerlerini manuel olarak güncelleyebiliriz:
```csharp
public partial struct MyCustomGroupSystem : ISystem
{
    private SystemHandle _systemA;
    private SystemHandle _systemB;

    public void OnUpdate(ref SystemState state)
    {
        state.WorldUnmanaged.UpdateSystem(_systemA);
        state.WorldUnmanaged.UpdateSystem(_systemB);
    }
}
```
Buradaki fikir, iki sistemi belirli bir sırada güncellemektir. Ancak bu yaklaşımın kritik dezavantajları vardır:
1. Alt sistemlerin yaşam döngüsünü daha dikkatli yönetmen gerekir.
2. Sistemin oluşturulması ve yok edilmesi sırasında handle geçerliliğini takip etmen gerekir.
3. `UpdateBefore` ve `UpdateAfter` gibi standart grup sıralama mekanizmasını yeniden oluşturman gerekebilir.
4. Nested group yönetimini kendin tasarlaman gerekir.

Yani, Unity'nin standart grup ve `PlayerLoop` entegrasyonundan yararlanmak yerine bu davranışı kendin üstlenirsin.

`WorldUnmanaged.UpdateSystem()` kullanmak tek başına yanlış değildir. Ancak kendi grup sistemini yazarken standart `ComponentSystemGroup` davranışını otomatik olarak elde etmiş olmazsın.

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; font-family: sans-serif; color: #ffffff; max-width: 650px; overflow: hidden; box-sizing: border-box;">
  <!-- 1. Satır: ComponentSystemGroup -->
  <div style="padding: 18px 20px; border-bottom: 1px solid #1e293b;">
    <div style="display: flex; justify-content: space-between; align-items: center;">
      <span style="font-weight: bold; font-size: 16px; color: #f8fafc;">ComponentSystemGroup</span>
      <span style="background-color: #291811; color: #ea580c; border: 1px solid #431407; font-size: 12px; font-weight: bold; padding: 3px 10px; border-radius: 6px;">Managed</span>
    </div>
    <div style="color: #94a3b8; font-size: 13px; margin-top: 8px;">
      Sistemleri toplar, sıralar ve Unity'nin grup güncelleme mekanizmasını kullanır.
    </div>
  </div>
  <!-- 2. Satır: ISystem -->
  <div style="padding: 18px 20px;">
    <div style="display: flex; justify-content: space-between; align-items: center;">
      <span style="font-weight: bold; font-size: 16px; color: #f8fafc;">ISystem</span>
      <span style="background-color: #0c241b; color: #22c55e; border: 1px solid #14532d; font-size: 12px; font-weight: bold; padding: 3px 10px; border-radius: 6px;">Unmanaged</span>
    </div>
    <div style="color: #94a3b8; font-size: 13px; margin-top: 8px;">
      Saf oyun mantığını ve ECS veri işlemlerini gerçekleştirir.
    </div>
  </div>
</div>

Managed grup kullanmak tek başına performans garantisi değildir. Gerçek performans, grup sayısı, sistem sayısı, yapılan işlemler, senkronizasyon noktaları ve iş yüküne bağlıdır.

## <span style="background-color: #536100; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">RogueliteRootSystemGroup</span>
```csharp
[DisableAutoCreation]
public sealed partial class RogueliteRootSystemGroup : ComponentSystemGroup { }
```
Bu grup, özel ECS mimarimizin ana giriş noktasıdır. Diğer grupları bunun içerisine ekleyeceğiz.

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 20px; font-family: monospace; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <!-- Ana Kök -->
  <div style="background-color: #536100; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-weight: bold; font-size: 14px; display: inline-block;">
    RogueliteRootSystemGroup
  </div>
  <!-- Alt Dallar -->
  <div style="margin-left: 20px; border-left: 2px solid #334155; padding-left: 16px; display: flex; flex-direction: column; gap: 10px; padding-top: 12px;">
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #d15f3f; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">RogueliteInitializationSystemGroup</span>
    </div>
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">GameStateSystemGroup</span>
    </div>
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #261a12; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">RogueliteSimulationSystemGroup</span>
    </div>
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #1c1333; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">RoguelitePresentationSystemGroup</span>
    </div>
  </div>
</div>
Böyle yapıyoruz çünkü bütün oyun sistemlerinin tek bir ana grup altında bulunmasını istiyoruz. Böylece ana grubu Unity'nin PlayerLoop'una bağladığımızda alt gruplar da onun güncellemesi üzerinden çalışabilir. Bir grubu tanımlamak, onu otomatik olarak oluşturmaz veya PlayerLoop'a bağlamaz. Bunları ayrıca gerçekleştirmemiz gerekir.

## <span style="background-color: #d15f3f; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">RogueliteInitializationSystemGroup</span>
```csharp
[DisableAutoCreation]
public sealed partial class RogueliteInitializationSystemGroup : ComponentSystemGroup { }
```
Bu grubun görevi dünya ve oyun başlangıç hazırlıklarını yönetmektir.
- Oyun konfigürasyonunu hazırlamak.
- Başlangıç entity'lerini oluşturmak.
- Oyuncu başlangıç verilerini hazırlamak.
- Oyun oturumu için gerekli ECS verilerini oluşturmak.

Örnek bir sistem:
```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(RogueliteInitializationSystemGroup))]
public partial struct WorldSetupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Dünya başlangıç hazırlıkları.
    }
}
```
- `DisableAutoCreation`: Sistemin otomatik oluşturulmasını engeller.
- `UpdateInGroup`: Sistem oluşturulup uygun şekilde gruba eklendiğinde hangi grupta çalışacağını belirtir.

## <span style="background-color: #0e172a; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">GameStateSystemGroup</span>
```csharp
[DisableAutoCreation]
public sealed partial class GameStateSystemGroup : ComponentSystemGroup { }
```
Bu grup oyun akışını yönetir.
- MainMenu durumundan Playing durumuna geçiş.
- Playing durumundan Paused durumuna geçiş.
- Oyuncu öldüğünde GameOver durumuna geçiş.
- Yeni run başlatma isteği.
- Level tamamlanma event'leri.

Örnek bir sistem:
```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(GameStateSystemGroup))]
public partial struct GameStateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Oyun durumunu kontrol et.
    }
}
```
Buradaki tasarım amacı oyun akışını fizik, hareket ve diğer saf oyun mekaniklerinden ayırmaktır. Örneğin bir düşman öldüğünde `Simulation` katmanı bir ölüm olayı üretebilir. `GameState` katmanı ise bu olayın oyun akışına etkisini değerlendirebilir. Böylece sistemlerin sorumlulukları birbirinden ayrılır.

## <span style="background-color: #261a12; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">RogueliteSimulationSystemGroup</span>
```csharp
[DisableAutoCreation]
public sealed partial class RogueliteSimulationSystemGroup : ComponentSystemGroup { }
```
Burası oyunun asıl mekaniklerinin çalışacağı gruptur.
- Oyuncu hareketi.
- Düşman hareketi.
- Çarpışma.
- Hasar.
- Silah ateşleme.
- Projectile hareketi.
- Düşman AI.
- Loot hesaplamaları.

Örnek bir sistem:
```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(RogueliteSimulationSystemGroup))]
public partial struct EnemyMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Düşman hareketini hesapla.
    }
}
```
Bu sistem yalnızca Simulation grubunun içine eklendiğinde o grubun güncelleme sırasına dahil olur.

## <span style="background-color: #1c1333; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">RoguelitePresentationSystemGroup</span>
```csharp
[DisableAutoCreation]
public sealed partial class RoguelitePresentationSystemGroup : ComponentSystemGroup { }
```
Bu grup oyun verilerini görsel veya `managed` katmana aktaran sistemleri barındırır.

- Animator parametrelerini güncellemek.
- UI verilerini göstermek.
- Managed Unity API'leriyle köprü kurmak.
- Görsel sunum için ECS verilerini dönüştürmek.

Örnek bir sistem:
```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(RoguelitePresentationSystemGroup))]
public partial class AnimatorBridgeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Animator ve ECS arasındaki köprü işlemleri.
    }
}
```
Burada `SystemBase` kullanılması bilinçli çünkü Animator gibi klasik Unity API'leri managed nesnelerle çalışır. Bu nedenle unmanaged `ISystem` yerine managed bir köprü sistemi tercih edilebilir. Yine de mümkün olan her sunum sisteminin managed olması gerekmez. ECS verisi üzerinde çalışan presentation sistemleri `ISystem` olabilir.

# Sistemlerin çalışma sırası nasıl belirlenir?
Bir sistemin <span style="color: #9de868;">**hangi grupta çalışacağı**</span>nı belirlemek ile <span style="color: #68dde8;">**o grup içindeki çalışma sırası**</span>nı belirlemek iki farklı konudur.

### <span style="color: #ff4917;">UpdateInGroup</span>
Sistemin hangi gruba (örneğin <span style="background-color: #1c1333; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">RoguelitePresentationSystemGroup</span>, <span style="background-color: #d15f3f; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">RogueliteInitializationSystemGroup</span>, ...) ait olduğunu belirler.
```csharp
[UpdateInGroup(typeof(RogueliteSimulationSystemGroup))]
public partial struct PlayerMovementSystem : ISystem {
    public void OnUpdate(ref SystemState state) { }
}
```
Bu sistem <span style="background-color: #261a12; color: #ffffff; font-weight: bold; padding: 2px 6px; border-radius: 4px;">RogueliteSimulationSystemGroup</span>grubuna aittir.

### <span style="color: #ed8264;">UpdateBefore</span>
Aynı gruptaki başka bir sistemden **önce** çalışmasını belirtir.
```csharp
[UpdateInGroup(typeof(RogueliteSimulationSystemGroup))]
[UpdateBefore(typeof(EnemyMovementSystem))]
public partial struct PlayerMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state) { }
}
```
Burada `PlayerMovementSystem`, `EnemyMovementSystem`'dan önce çalışır.

### <span style="color: #f2977e;">UpdateAfter</span>
Aynı gruptaki başka bir sistemden **sonra** çalışmasını belirtir.
```csharp
[UpdateInGroup(typeof(RogueliteSimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial struct WeaponSystem : ISystem
{
    public void OnUpdate(ref SystemState state) { }
}
```
Burada `WeaponSystem`, `PlayerMovementSystem`'dan sonra çalışır.

Örnek bir çalışma sırası:
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 20px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <!-- Başlık -->
  <div style="font-weight: bold; font-size: 16px; margin-bottom: 18px; color: #f8fafc;">
    RogueliteSimulationSystemGroup
  </div>
  <!-- 1. Adım -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 15px; line-height: 1.2;">1</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 16px;">
      <div style="font-weight: bold; font-size: 15px; color: #f8fafc; line-height: 1.2;">PlayerMovementSystem</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Oyuncu hareketini hesaplar.</div>
    </div>
  </div>
  <!-- 2. Adım -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 15px; line-height: 1.2;">2</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 16px;">
      <div style="font-weight: bold; font-size: 15px; color: #f8fafc; line-height: 1.2;">WeaponSystem</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Silah ve ateşleme işlemlerini hesaplar.</div>
    </div>
  </div>
  <!-- 3. Adım -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 15px; line-height: 1.2;">3</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 16px;">
      <div style="font-weight: bold; font-size: 15px; color: #f8fafc; line-height: 1.2;">ProjectileSystem</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Mermileri hareket ettirir.</div>
    </div>
  </div>
  <!-- 4. Adım (Son Adım - Altında çizgi yok) -->
  <div style="display: flex; align-items: flex-start;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 15px; line-height: 1.2;">4</span>
    </div>
    <div>
      <div style="font-weight: bold; font-size: 15px; color: #f8fafc; line-height: 1.2;">DamageSystem</div>
      <div style="color: #94a3b8; font-size: 13px; margin-top: 4px;">Hasar hesaplamalarını yapar.</div>
    </div>
  </div>
</div>

Bu sıralamayı yukarıdaki gibi attribute'larla tanımlayabiliriz.

<div style="background-color: #1f160c; border: 1px solid #451a03; border-left: 4px solid #f59e0b; border-radius: 8px; padding: 14px 18px; font-family: sans-serif; max-width: 650px; box-sizing: border-box;">
  <div style="color: #fbbf24; font-weight: bold; font-size: 14px; margin-bottom: 4px;">
    ⚠️ Uyarı
  </div>
  <div style="color: #cbd5e1; font-size: 13px; line-height: 1.5;">
    <span style="color: #ed8264;">UpdateBefore</span> ve <span style="color: #f2977e;">UpdateAfter</span> ilişkileri, hedef sistem aynı grubun sıralamasında bulunduğunda uygulanır. Bunlar farklı gruplar arasındaki çalışma sırasını belirlemek için kullanılmaz. Farklı grupların sırasını root grubunun sıralaması belirler.
  </div>
</div>

# Grupların sırasını nasıl belirleriz?
Şimdi dört ana grubun sırasını tanımlayalım. İlk önce <span style="background-color: #d15f3f; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Initialization</span> çalışmalı. Ardından <span style="background-color: #0e172a; color: #ffffff; padding: 2px 6px; border-radius: 4px;">GameState</span>, <span style="background-color: #261a12; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Simulation</span> ve <span style="background-color: #1c1333; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Presentation</span> çalışmalı.

```csharp
[DisableAutoCreation]
public sealed partial class RogueliteRootSystemGroup : ComponentSystemGroup { }
```

```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(RogueliteRootSystemGroup))]
[UpdateBefore(typeof(GameStateSystemGroup))]
public sealed partial class RogueliteInitializationSystemGroup : ComponentSystemGroup { }
```

```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(RogueliteRootSystemGroup))]
[UpdateBefore(typeof(RogueliteSimulationSystemGroup))]
public sealed partial class GameStateSystemGroup : ComponentSystemGroup { }
```

```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(RogueliteRootSystemGroup))]
[UpdateBefore(typeof(RoguelitePresentationSystemGroup))]
public sealed partial class RogueliteSimulationSystemGroup : ComponentSystemGroup { }
```

```csharp
[DisableAutoCreation]
[UpdateInGroup(typeof(RogueliteRootSystemGroup))]
public sealed partial class RoguelitePresentationSystemGroup : ComponentSystemGroup { }
```

Bu tanımlar şu sıralamayı ifade eder:
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 20px; font-family: monospace; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <!-- Ana Kök -->
  <div style="background-color: #536100; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-weight: bold; font-size: 14px; display: inline-block;">
    RogueliteRootSystemGroup
  </div>
  <!-- Alt Dallar -->
  <div style="margin-left: 20px; border-left: 2px solid #334155; padding-left: 16px; display: flex; flex-direction: column; gap: 10px; padding-top: 12px;">
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #d15f3f; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">RogueliteInitializationSystemGroup</span>
    </div>
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">GameStateSystemGroup</span>
    </div>
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #261a12; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">RogueliteSimulationSystemGroup</span>
    </div>
    <div style="display: flex; align-items: center;">
      <span style="color: #334155; margin-left: -16px; margin-right: 10px; font-weight: bold;">──</span>
      <span style="background-color: #1c1333; border: 1px solid #1e293b; border-radius: 6px; padding: 8px 12px; font-size: 13px;">RoguelitePresentationSystemGroup</span>
    </div>
  </div>
</div>

Burada `UpdateBefore` zinciri sıralamayı oluşturur:
- <span style="background-color: #d15f3f; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Initialization</span>, <span style="background-color: #0e172a; color: #ffffff; padding: 2px 6px; border-radius: 4px;">GameState</span>'ten önce.
- <span style="background-color: #0e172a; color: #ffffff; padding: 2px 6px; border-radius: 4px;">GameState</span>, <span style="background-color: #261a12; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Simulation</span>'dan önce.
- <span style="background-color: #261a12; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Simulation</span>, <span style="background-color: #1c1333; color: #ffffff; padding: 2px 6px; border-radius: 4px;">Presentation</span>'dan önce.

Burada, sıralama attribute'ları (`UpdateBefore`) aynı gruba eklenen alt gruplara uygulanır. Sıralamayı açıkça yazıyoruz çünkü bir grubun başka bir gruptan önce çalışması gerekiyorsa bunu kod üzerinden ifade etmek, sistemlerin eklenme sırasına güvenmekten daha güvenlidir. Ayrıca gelecekte yeni bir grup eklendiğinde mevcut sıralama ilişkilerini takip etmek kolaylaşır.

# Sistemleri kim oluşturacak ve gruplara kim ekleyecek?
Şimdiye kadar grupları tanımladık ve sıralamalarını belirledik ancak `DisableAutoCreation` kullandığımız için sistemler otomatik oluşturulmayacak. `Bootstrap` katmanı ile biz oluşturacağız.

`Bootstrap`, World oluşturulduktan sonra gerekli sistemleri ve grupları oluşturur, birbirlerine bağlar ve ana grubu Unity'nin güncelleme döngüsüne ekler.

## 1. Grupları oluştur
<div style="background-color: #000000; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 950px; font-family: sans-serif; box-sizing: border-box;">
  <!-- ÜST ÇUBUK -->
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">RogueliteECSGroupsInstaller.cs</span>
  </div>

  <!-- KOD ALANI (Boşluksuz, sıkı satır yapısı) -->
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">public static class</span> <span style="color: #38bdf8;">RogueliteECSInstaller</span> {</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public static void</span> <span style="color: #60a5fa;">Install</span>(<span style="color: #4ade80;">World</span> world) {</div>
    <!-- KIRMIZI VURGULU SATIR -->
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">var</span> rootGroup = world.<span style="color: #60a5fa;">GetOrCreateSystemManaged</span><<span style="color: #ebf70a;">RogueliteRootSystemGroup</span>>(); // Ana grup </div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">var</span> initializationGroup = world.<span style="color: #60a5fa;">GetOrCreateSystemManaged</span><<span style="color: #d15f3f;">RogueliteInitializationSystemGroup</span>>(); // Alt grup </div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">var</span> gameStateGroup = world.<span style="color: #60a5fa;">GetOrCreateSystemManaged</span><<span style="color: #2b6fff;">GameStateSystemGroup</span>>(); // Alt grup </div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">var</span> simulationGroup = world.<span style="color: #60a5fa;">GetOrCreateSystemManaged</span><<span style="color: #75360c;">RogueliteSimulationSystemGroup</span>>(); // Alt grup </div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">var</span> presentationGroup = world.<span style="color: #60a5fa;">GetOrCreateSystemManaged</span><<span style="color: #713df5;">RoguelitePresentationSystemGroup</span>>(); // Alt grup </div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px;">}</div>
  </div>

  <!-- ALT AÇIKLAMA ALANI -->
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    Bu installer sınıfı, belirtilen <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">World</code> üzerinde ana sistem grubunu ve ona bağlı alt sistem gruplarını oluşturur.
    <code style="color: #60a5fa; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">GetOrCreateSystemManaged<T>()</code> ile ilgili managed sistemler World içerisinde oluşturulur. Sistem mevcut değilse oluşturulur. Mevcutsa aynı sistem nesnesi alınır. Bu örnekte grupların hepsi <code style="color: #bc85ed; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">ComponentSystemGroup</code> türevi olduğu için managed olarak oluşturulurlar. SystemHandle değil, sistemin managed nesne referansını ver. Böylece <code style="color: #3bf5d6; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">AddSystemToUpdateList()</code> metoduna doğrudan grup nesnesini gönderebiliyoruz.
  </div>
</div>

## 2. Grupları root grubuna ekle
Şimdi oluşturduğumuz grupları ana gruba bağlayalım.

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 650px; font-family: sans-serif; box-sizing: border-box;">

  <!-- ÜST ÇUBUK -->
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">RogueliteECSInstaller.cs</span>
  </div>

  <!-- KOD ALANI -->
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;">rootGroup.<span style="color: #3bf5d6;">AddSystemToUpdateList</span>(<span style="color: #d15f3f; font-weight: bold;">initializationGroup</span>);</div>
    <div style="padding: 1px 18px;">rootGroup.<span style="color: #3bf5d6;">AddSystemToUpdateList</span>(<span style="color: #2b6fff; font-weight: bold;">gameStateGroup</span>);</div>
    <div style="padding: 1px 18px;">rootGroup.<span style="color: #3bf5d6;">AddSystemToUpdateList</span>(<span style="color: #75360c; font-weight: bold;">simulationGroup</span>);</div>
    <div style="padding: 1px 18px;">rootGroup.<span style="color: #3bf5d6;">AddSystemToUpdateList</span>(<span style="color: #713df5; font-weight: bold;">presentationGroup</span>);</div>
  </div>

  <!-- ALT AÇIKLAMA ALANI -->
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    Root grubu güncellendiğinde içerisindeki dört grup da güncellenecek. Burada <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">AddSystemToUpdateList()</code> kullanıyoruz çünkü alt gruplar da managed <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">ComponentSystemGroup</code> nesneleridir.
  </div>

</div>

Unity kaynak kodunda <span style="background-color: #3bf5d6; color: #000000; padding: 2px 6px; border-radius: 4px;">**AddSystemToUpdateList**</span> metodunun managed sistemleri listeye eklediğini açıklar. Ekleme sonrasında sıralama listesinin yeniden oluşturulması gerektiğini de belirtir:

<div style="background-color: #000000; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 950px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">ComponentSystemGroup.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;summary&gt;</div>
    <div style="padding: 1px 18px; color: #64748b;">/// Managed bir sistemi grubun güncelleme listesine (update list) ekler. Liste, grubun bir sonraki güncelleme işleminde sıralanacaktır.</div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;/summary&gt;</div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;param name="sys"&gt;Eklenecek sistem&lt;/param&gt;</div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;exception cref="ArgumentException"&gt;Bir grup kendisine eklendiğinde&lt;/exception&gt;</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">public void</span> <span style="color: #3bf5d6;">AddSystemToUpdateList</span>(<span style="color: #4ade80;">ComponentSystemBase</span> sys)</div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #60a5fa;">CheckCreated</span>();</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">if</span> (sys != <span style="color: #c084fc;">null</span>)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">if</span> (<span style="color: #c084fc;">this</span> == sys)</div>
    <div style="padding: 1px 18px 1px 90px;"><span style="color: #c084fc;">throw new</span> <span style="color: #38bdf8;">ArgumentException</span>($"Can't add {TypeManager.GetSystemName(GetType())} to its own update list");</div>
    <div style="padding: 1px 18px 1px 66px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 66px; color: #64748b;">// Check for duplicate Systems. Also see issue #1792</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">if</span> (m_managedSystemsToUpdate.<span style="color: #60a5fa;">IndexOf</span>(sys) >= 0)</div>
    <div style="padding: 1px 18px 1px 66px;">{</div>
    <div style="padding: 1px 18px 1px 90px;"><span style="color: #c084fc;">if</span> (m_managedSystemsToRemove.<span style="color: #60a5fa;">Contains</span>(sys))</div>
    <div style="padding: 1px 18px 1px 90px;">{</div>
    <div style="padding: 1px 18px 1px 114px;">m_managedSystemsToRemove.<span style="color: #60a5fa;">Remove</span>(sys);</div>
    <div style="padding: 1px 18px 1px 90px;">}</div>
    <div style="padding: 1px 18px 1px 90px;"><span style="color: #c084fc;">return</span>;</div>
    <div style="padding: 1px 18px 1px 66px;">}</div>
    <div style="padding: 1px 18px 1px 66px; height: 8px;"></div>
    <div style="padding: 2px 18px 2px 63px; background-color: rgba(56, 189, 248, 0.12); border-left: 3px solid #38bdf8;">m_MasterUpdateList.<span style="color: #60a5fa;">Add</span>(<span style="color: #c084fc;">new</span> <span style="color: #38bdf8;">UpdateIndex</span>(m_managedSystemsToUpdate.Count, <span style="color: #c084fc;">true</span>));</div>
    <div style="padding: 2px 18px 2px 63px; background-color: rgba(56, 189, 248, 0.12); border-left: 3px solid #38bdf8;">m_managedSystemsToUpdate.<span style="color: #60a5fa;">Add</span>(sys);</div>
    <div style="padding: 2px 18px 2px 63px; background-color: rgba(56, 189, 248, 0.12); border-left: 3px solid #38bdf8;">m_systemSortDirty = <span style="color: #c084fc;">true</span>;</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px;">}</div>
  </div>
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    ECS kaynak kodundaki bu metod grubun kendisine eklenmesini engeller, aynı sistemin tekrar eklenmesini kontrol eder ve sistemi <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">m_managedSystemsToUpdate</code> listesine dahil ederek sıralama bayrağını (<code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">m_systemSortDirty</code>) aktif eder.
  </div>
</div>

## 3. ISystem sistemlerini oluştur
Önce gerçek oyun sistemlerimizi belirleyelim:
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 650px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">PlayerMovementSystem.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">using</span> Unity.Entities;</div>
    <div style="padding: 1px 18px; height: 8px;"></div>
    <div style="padding: 1px 18px;">[<span style="color: #4ade80;">DisableAutoCreation</span>]</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">public partial struct</span> <span style="color: #38bdf8;">PlayerMovementSystem</span> : <span style="color: #4ade80;">ISystem</span></div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnCreate</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnUpdate</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnDestroy</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px;">}</div>
  </div>
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    Bu sistem <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">ISystem</code> uyguladığı için managed bir sınıf nesnesi olarak oluşturulamaz.
  </div>
</div>

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 650px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">EnemyMovementSystem.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">using</span> Unity.Entities;</div>
    <div style="padding: 1px 18px; height: 8px;"></div>
    <div style="padding: 1px 18px;">[<span style="color: #4ade80;">DisableAutoCreation</span>]</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">public partial struct</span> <span style="color: #38bdf8;">EnemyMovementSystem</span> : <span style="color: #4ade80;">ISystem</span></div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnCreate</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnUpdate</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnDestroy</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px;">}</div>
  </div>
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    Bu sistem <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">ISystem</code> uyguladığı için managed bir sınıf nesnesi olarak oluşturulamaz.
  </div>
</div>

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 650px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">WeaponSystem.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">using</span> Unity.Entities;</div>
    <div style="padding: 1px 18px; height: 8px;"></div>
    <div style="padding: 1px 18px;">[<span style="color: #4ade80;">DisableAutoCreation</span>]</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">public partial struct</span> <span style="color: #38bdf8;">WeaponSystem</span> : <span style="color: #4ade80;">ISystem</span></div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnCreate</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnUpdate</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public void</span> <span style="color: #60a5fa;">OnDestroy</span>(<span style="color: #c084fc;">ref</span> <span style="color: #4ade80;">SystemState</span> state)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px;">}</div>
  </div>
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    Bu sistem <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">ISystem</code> uyguladığı için managed bir sınıf nesnesi olarak oluşturulamaz.
  </div>
</div>

Şimdi bu oyun sistemlerimizi oluşturalım:

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 850px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">RogueliteECSInstaller.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">var</span> playerMovementHandle = world.<span style="color: #60a5fa;">CreateSystem</span>&lt;<span style="color: #38bdf8;">PlayerMovementSystem</span>&gt;();</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">var</span> enemyMovementHandle = world.<span style="color: #60a5fa;">CreateSystem</span>&lt;<span style="color: #38bdf8;">EnemyMovementSystem</span>&gt;();</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">var</span> weaponHandle = world.<span style="color: #60a5fa;">CreateSystem</span>&lt;<span style="color: #38bdf8;">WeaponSystem</span>&gt;();</div>
  </div>
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">CreateSystem&lt;T&gt;()</code>, unmanaged sistem oluşturmak için kullanılır ve bir <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">SystemHandle</code> döndürür. Yani <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">playerMovementHandle</code>, <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">enemyMovementHandle</code> ve <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">weaponHandle</code> değişkenleri <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">SystemHandle</code> türündedir.
  </div>
</div>

Artık bu handle türlerini bir <code style="color: #e2e8f0; background-color: #252588; padding: 1px 5px; border-radius: 4px;">ComponentSystemGroup</code> içerisine ekleyebiliriz.

## 4. System handle'larını doğru gruplara ekle
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 650px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">RogueliteECSInstaller.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;">simulationGroup.<span style="color: #60a5fa;">AddSystemToUpdateList</span>(<span style="color: #38bdf8;">playerMovementHandle</span>);</div>
    <div style="padding: 1px 18px;">simulationGroup.<span style="color: #60a5fa;">AddSystemToUpdateList</span>(<span style="color: #38bdf8;">enemyMovementHandle</span>);</div>
    <div style="padding: 1px 18px;">simulationGroup.<span style="color: #60a5fa;">AddSystemToUpdateList</span>(<span style="color: #38bdf8;">weaponHandle</span>);</div>
  </div>
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    Burada <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">ISystem nesnesini</code> doğrudan gruba vermiyoruz. Onun yerine <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">SystemHandle</code> veriyoruz. Çünkü unmanaged sistemler birer managed C# nesnesi olarak temsil edilmez. Unity, handle üzerinden ilgili sistemin durumuna erişebilir.
  </div>
</div>

ECS kaynak kodunda <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;"> AddSystemToUpdateList(SystemHandle)</code> metodunun unmanaged sistemleri gruba eklediği görülebilir. Ayrıca verilen handle'ın managed bir sisteme ait olması durumunda managed ekleme metoduna yönlendirme de yapar.

<div style="background-color: #000000; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 1050px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">ComponentSystemGroup.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
<div style="padding: 1px 18px;"><span style="color: #64748b;">// Grubun güncelleyeceği managed sistemlerin handle'larını tutan koleksiyon.</span></div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">internal</span> <span style="color: #4ade80;">List</span>&lt;<span style="color: #4ade80;">ComponentSystemBase</span>&gt; m_managedSystemsToUpdate = <span style="color: #c084fc;">new</span> <span style="color: #4ade80;">List</span>&lt;<span style="color: #4ade80;">ComponentSystemBase</span>&gt;();</div>
    <div style="padding: 1px 18px;"><span style="color: #64748b;">// Grubun güncelleyeceği unmanaged sistemlerin handle'larını tutan koleksiyon.</span></div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">internal</span> <span style="color: #4ade80;">UnsafeList</span>&lt;<span style="color: #4ade80;">SystemHandle</span>&gt; m_UnmanagedSystemsToUpdate;</div>
    <div style="padding: 1px 18px;"><span style="color: #64748b;">// Aşağıdaki UpdateIndex struct'ının açıklandığı yerdeki bilgileri kullanarak managed ve unmanaged sistemleri tek bir ortak sıralamada temsil eder. Yani Unity, managed ve unmanaged sistemleri ayrı koleksiyonlarda tutarken bunların güncelleme sırasını tek bir liste üzerinden yönetebilir.</span></div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">internal</span> <span style="color: #4ade80;">UnsafeList</span>&lt;<span style="color: #38bdf8;">UpdateIndex</span>&gt; m_MasterUpdateList;</div>
    <div style="padding: 1px 18px;"><span style="color: #64748b;">// ComponentSystemGroup.OnCreate içerisinde base.OnCreate() çağrısından sonra true olur ve ComponentSystemGroup.OnDestroy içerisinde base.OnDestroy() çağrısından sonra false olur.</span></div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">public</span> <span style="color: #4ade80;">bool</span> Created { get; private set; } = false; </div>
    <div style="padding: 1px 18px; height: 8px;"></div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;summary&gt;</div>
    <div style="padding: 1px 18px; color: #64748b;">/// Bu, grubun oluşturulmuş ve kullanılabilir durumda olup olmadığını kontrol eder. </div>
    <div style="padding: 1px 18px; color: #64748b;">/// ComponentSystemGroup bir sistemdir. Bir World içerisinde oluşturulur ve o World'e bağlı bir yaşam döngüsü vardır. </div>
    <div style="padding: 1px 18px; color: #64748b;">/// Örneğin 'var simulationSystemGroup = world.GetOrCreateSystemManaged<RogueliteSimulationSystemGroup>();', grubun oluşturulmasını veya mevcut grubun alınmasını sağlar. Ancak henüz oluşturulmamış veya geçerli durumda olmayan bir grup üzerinde işlem yapmak yanlıştır. CheckCreated() bu tür hatalı kullanımları önlemeye yardımcı olur.</div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;/summary&gt;</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">private void</span> <span style="color: #60a5fa;">CheckCreated</span>()</div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">if</span> (!Created)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 72px;"><span style="color: #c084fc;">throw new</span> <span style="color: #e3624b;">InvalidOperationException</span>(<span style="color: #ffffff;">$"Group of type {<span style="color: #60a5fa;">GetType</span>()} has not been created, either the derived class forgot to call base.OnCreate(), or it has been destroyed"</span>);</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px;">}</div>
    <div style="padding: 1px 18px; height: 8px;"></div>
    <div style="padding: 1px 18px; color: #64748b;">/// Bu fonksiyonun görevi, verilen SystemHandle değerinin m_UnmanagedSystemsToUpdate listesinde bulunup bulunmadığını kontrol etmektir. Bulursa listedeki indeksini, bulamazsa -1 döndürür. </div>
    <div style="padding: 1px 18px; color: #64748b;">/// Örneğin elimizde 'SystemHandle playerMovementHandle = world.CreateSystem<PlayerMovementSystem>();' olsun. Bu handle'ın grubun unmanaged güncelleme listesinde bulunup bulunmadığını kontrol eder. </div>
    <div style="padding: 1px 18px; color: #64748b;">/// En kötü durumda bütün listeyi dolaştığı için zaman karmaşıklığı O(n)'dir. </div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;/summary&gt;</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">private int</span> <span style="color: #60a5fa;">UnmanagedSystemIndex</span>(<span style="color: #4ade80;">SystemHandle</span> sysHandle)</div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 42px;"><span style="color: #64748b;">/* Koleksiyonun uzunluğunu saklar çünkü döngü boyunca değişmez. Döngü boyunca koleksiyonun uzunluğunu tekrar tekrar okumak yerine bir kez okuyup kullanır. */</span></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">int</span> len = m_UnmanagedSystemsToUpdate.Length;</div>
    <div style="padding: 1px 42px;"><span style="color: #64748b;">/* Ptr, koleksiyonun bellekte tuttuğu elemanların başlangıç adresine erişimi sağlar. Yani m_UnmanagedSystemsToUpdate koleksiyonunun kendisi yerine, onun elemanlarının bulunduğu belleğe doğrudan erişir. Yani, ptr[0] ilk handle'a, ptr[1] ikinci handle'a erişir ve bu böyle devam eder. Pointer kullanımı güvenli C# kodundan farklıdır. Bellek erişiminin doğru sınırlar içinde kalması kullanıcı sorumluluğundadır (bu kod için sorumluluk Unity tarafında). */</span></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">var</span> ptr = m_UnmanagedSystemsToUpdate.Ptr;</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">for</span> (<span style="color: #c084fc;">int</span> i = 0; i &lt; len; ++i)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 66px;"><span style="color: #64748b;">/* O an kontrol edilen handle (ptr[i]) ile aranılan handle (sysHandle) aynı mı diye bakılır. Eğer aynı ise o elemanın indeksi (mevcut i değeri) döndürülür. */</span></div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">if</span> (ptr[i] == sysHandle)</div>
    <div style="padding: 1px 18px 1px 66px;">{</div>
    <div style="padding: 1px 18px 1px 90px;"><span style="color: #c084fc;">return</span> i;</div>
    <div style="padding: 1px 18px 1px 66px;">}</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 42px;"><span style="color: #64748b;">/* Döngü tamamlandığında eşleşme bulunamadıysa -1 döndürür. */</span></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">return</span> -1;</div>
    <div style="padding: 1px 18px;">}</div>
    <div style="padding: 1px 18px; height: 8px;"></div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;summary&gt;</div>
    <div style="padding: 1px 18px; color: #64748b;">/// Unmanaged bir sistemi grubun güncelleme listesine ekler. Liste, grubun bir sonraki güncelleme işleminde sıralanacaktır.</div>
    <div style="padding: 1px 18px; color: #64748b;">/// Fonksiyonun parametresi SystemHandle olsa da yalnızca unmanaged sistemlerle sınırlı değildir ve listeye ekleme işlemi sonrasında sistemlerin sıralanması hemen yapılmayabilir.</div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;/summary&gt;</div>
    <div style="padding: 1px 18px; color: #64748b;">/// &lt;param name="sysHandle"&gt;Eklenecek sistem (aslında eklenecek sistemin handle'ı)&lt;/param&gt;</div>
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">public void</span> <span style="color: #3bf5d6; font-weight: bold;">AddSystemToUpdateList</span>(<span style="color: #4ade80;">SystemHandle</span> sysHandle)</div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 42px; color: #64748b;">// Grubun oluşturulmuş ve kullanılabilir durumda olup olmadığını kontrol eder.</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #60a5fa;">CheckCreated</span>();</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 42px; color: #64748b;">/* Handle geçersiz mi? Burada sysHandle değerinin varsayılan değer olup olmadığını kontrol ediliyor. SystemHandle bir struct'tır. Bu yüzden default kullanıldığında bu struct'ın varsayılan değeri elde edilir ve bu handle geçerli bir sistemi temsil etmez. Yani şu anlama gelir: "Geçerli bir sistem handle'ı verilmemişse hiçbir işlem yapma ve fonksiyondan çık". Bu kontrol sayesinde aşağıdaki Resolve işleminin geçersiz bir handle üzerinde yapılması önlenir. Bu kontrolün yalnızca handle'ın default olup olmadığını kontrol ettiğini ve handle'ın mutlaka geçerli, canlı ve bu grubun World'üne ait olduğunu tek başına garanti etmediğini unutma! */</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">if</span> (sysHandle == <span style="color: #c084fc;">default</span>)</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">return</span>;</div>
    <div style="padding: 1px 42px; color: #64748b;">/* SystemHandle, sistemin kendisi değildir. Sistemi temsil eden bir tanıtıcıdır. Bu handle üzerinden sistemin SystemState bilgisine ulaşılıyor. SystemState, Entities'in bir sistemin yaşam döngüsü ve çalışma durumu için tuttuğu altyapıdır. */</div>
    <div style="padding: 1px 42px; color: #64748b;">/* SystemHandle (sistemi tanımlayan handle) ---ResolveSystemState---> SystemState (sistemin çalışma ve yaşam döngüsü bilgileri) ------> Managed mı, Unmanaged mı? (m_ManagedSystem üzerinden kontrol) */</div>
    <div style="padding: 1px 42px; color: #64748b;">/* Buradaki World.Unmanaged, World'ün unmanaged sistem altyapısına erişim sağlayan API yüzeyidir. Ancak World.Unmanaged üzerinden ResolveSystemState çağırmak, sistemin mutlaka ISystem olduğu anlamına gelmez. Managed sistemlerin de sistem durumları vardır. Bir sonraki satırda bu ayrım yapılıyor. */</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">var</span> state = World.Unmanaged.<span style="color: #60a5fa;">ResolveSystemState</span>(sysHandle);</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 42px; color: #64748b;">/* state burada SystemState yapısına işaret eden bir pointer olarak kullanılıyor. 'state->m_ManagedSystem' ifadesi, state pointer'ının işaret ettiği SystemState içerisindeki m_ManagedSystem alanına erişir. m_ManagedSystem, sistemin managed bir C# nesnesiyle ilişkisini tutmak için kullanılır. IsAllocated ise bu alanın bir managed nesne referansına sahip olacak şekilde ayrılmış olup olmadığını kontrol eder. TRUE ise sistem managed, FALSE ise sistem unmanaged yapıdadır.*/</div>
    <div style="padding: 1px 42px; color: #64748b;">/* Örneğin "public partial class MyManagedSystem : SystemBase" managed bir sistemdir ve "public partial struct MyUnmanagedSystem : ISystem" unmanaged bir sistemdir. Buradaki kontrol sayesinde fonksiyon, SystemHandle üzerinden gelen sistemin hangi türde olduğunu ayırt edebilir. Bu satır, ISystem veya SystemBase türünü reflection ile kontrol etmiyor. Sistem durumunun içinde tutulan managed sistem referansının ayrılmış olup olmadığına bakıyor. */</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">bool</span> isManaged = state-&gt;m_ManagedSystem.IsAllocated;</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">if</span> (isManaged)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 64px; color: #64748b;">/* Managed sistem nesnesini elde etme */</div>
    <div style="padding: 1px 64px; color: #64748b;">/* "state->m_ManagedSystem.Target" ifadesi managed referansın işaret ettiği gerçek C# nesnesini elde eder. */</div>
    <div style="padding: 1px 64px; color: #64748b;">/* Örneğin sistemimiz "public partial class EnemyHealthSystem : SystemBase" olsun. EnemyHealthSystem, ComponentSystemBase sınıfından türediği için (SystemBase sınıfı ComponentBaseSystem sınıfından türemiştir) managed sistem nesnesidir. Target üzerinden elde edilen nesne, bu durumda "EnemyHealthSystem" örneğidir. Cast yapıyoruz çünkü Target generic bir nesne referansı olarak tutulur. Ancak AddSystemToUpdateList metodunun managed overload'u ComponentSystemBase türünden bir nesne bekler. */</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">var</span> sys = (<span style="color: #4ade80;">ComponentSystemBase</span>)state-&gt;m_ManagedSystem.Target;</div>
    <div style="padding: 1px 64px; color: #64748b;">/* Sınıf içerisinde aynı isimli iki metot overload edilmiş olabilir { AddSystemToUpdateList(SystemHandle sysHandle) ve AddSystemToUpdateList(ComponentSystemBase sys) }. İlk metot SystemHandle, ikinci metot ComponentSystemBase alır. Bizim elimizde şu anda ComponentSystemBase sys olduğu için C# ikinci metodu seçer. Yani "this.AddSystemToUpdateList(sys);" çağrısı kendisini tekrar çağırmaz. Diğer overload'u çağırır.*/</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">this</span>.<span style="color: #3bf5d6;">AddSystemToUpdateList</span>(sys);</div>
    <div style="padding: 1px 64px; color: #64748b;">/* Managed sistem zaten managed overload üzerinden gruba eklenmiştir. Bu nedenle unmanaged sisteme özel olan aşağıdaki işlemlerin yapılmasına gerek yoktur. */</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">return</span>;</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">else</span> <span style="color: #64748b;">// Sistem unmanaged ise</span> </div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 65px; color: #64748b;">/* Sistem zaten unmanaged güncelleme listesinde mi? */</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">if</span> (-1 != <span style="color: #60a5fa;">UnmanagedSystemIndex</span>(sysHandle))</div>
    <div style="padding: 1px 18px 1px 66px;">{</div>
    <div style="padding: 1px 90px; color: #64748b;">/* Sistem zaten güncelleme listesinde bulunuyorsa neden m_UnmanagedSystemsToRemove listesinde aranıyor? Çünkü bu iki liste birlikte bir tür ekleme ve çıkarma durumunu temsil eder. Şöyle düşün: Bir sistem gruba eklenmiş olsun.  Daha sonra gruptan çıkarılması istenmiş olsun. Ardından sistem, güncelleme listesinden hemen fiziksel olarak silinmek yerine çıkarılacaklar listesine kaydedilmiş olsun. Ve en son olarak aynı sistem tekrar eklenmek istenmiş olsun. İşte bu durumda sistem zaten güncelleme listesinde bulunur ancak çıkarılacaklar listesinde de işaretlenmiş olabilir. Bu satır, sistemin çıkarılmak üzere işaretlenip işaretlenmediğini kontrol eder. Bulursa indeksini, bulamazsa -1 döndürür. */</div>
    <div style="padding: 1px 18px 1px 90px;"><span style="color: #c084fc;">int</span> index = m_UnmanagedSystemsToRemove.<span style="color: #60a5fa;">IndexOf</span>(sysHandle);</div>
    <div style="padding: 1px 90px; color: #64748b;">/* Eğer sistem çıkarılacaklar listesinde bulunuyorsa oradan kaldırılır çünkü daha önce çıkarılmak istenmiş olsa bile şu an tekrar eklenmek istenmiş. Sistem zaten güncelleme listesinde olduğu için ikinci kez eklenmiyor. Bunun yerine varsa çıkarılma işareti kaldırılıyor. */</div>
    <div style="padding: 1px 18px 1px 90px;"><span style="color: #c084fc;">if</span> (-1 != index)</div>
    <div style="padding: 1px 18px 1px 114px;">m_UnmanagedSystemsToRemove.<span style="color: #60a5fa;">RemoveAt</span>(index);</div>
    <div style="padding: 1px 90px; color: #64748b;">/* Sistem zaten unmanaged güncelleme listesinde bulunuyorsa yapılması gereken işlem tamamlanmıştır. Dolayısıyla tekrar ekleme yapılmasına gerek yoktur. */</div>
    <div style="padding: 1px 18px 1px 90px;"><span style="color: #c084fc;">return</span>;</div>
    <div style="padding: 1px 18px 1px 66px;">}</div>
    <div style="padding: 1px 18px 1px 66px; height: 8px;"></div>
    <div style="padding: 1px 65px; color: #64748b;">/* Burası unmanaged sistemin gerçekten eklendiği yerdir. */</div>
    <div style="padding: 1px 65px; color: #64748b;">/* ! DEVAM ETMEDEN ÖNCE AŞAĞIDAKİ UpdateIndex BÖLÜMÜNE BAK ! */</div>
    <div style="padding: 1px 65px; color: #64748b;">/* ComponentSystemGroup içerisinde managed ve unmanaged sistemler ayrı koleksiyonlarda tutulur. Bunun temel nedeni bu iki sistem türünün farklı şekillerde temsil edilmesi ve çalıştırılmasıdır. 'm_ManagedSystemsToUpdate' managed sistemleri, 'm_UnmanagedSystemsToUpdate' unmanaged sistemleri tutar. */</div>
    <div style="padding: 1px 65px; color: #64748b;">/* Managed ve unmanaged sistemleri ayrı listelerde tutuyorsak, bunların birbirlerine göre çalışma sırasını nasıl belirleyeceğiz? 'm_MasterUpdateList' bu problemi çözer. Detaylı açıklama UpdateIndex bölümünün sonrasındaki "İki ayrı liste, tek bir ortak sıralama" bölümünde ... */</div>
    <div style="height: 15px;"></div>
    <div style="padding: 1px 65px; color: #64748b;">/* .Length ile yeni eklenecek unmanaged sistemin hangi indekse yerleştirileceğini buluyor. Örneğin unmanaged listede 2 eleman varsa indeks numaraları 0 ve 1 olur. Bir sonraki elemanın 2. indekse yazılması gerektiği için doğrudan Length değerini veriyor. */</div>
    <div style="padding: 1px 65px; color: #64748b;">/* false ile, eklenecek sistemin managed olmadığını (yani unmanaged olduğunu) bildiriyor. Add ile de oluşturduğu UpdateIndex değerini ortak listeye ekliyor.*/</div>
    <div style="padding: 1px 18px 1px 66px;">m_MasterUpdateList.<span style="color: #60a5fa;">Add</span>(<span style="color: #c084fc;">new</span> <span style="color: #38bdf8;">UpdateIndex</span>(m_UnmanagedSystemsToUpdate.Length, <span style="color: #c084fc;">false</span>));</div>
    <div style="padding: 1px 65px; color: #64748b;">/* Gerçek sistem handle'ını unmanaged sistem listesine ekliyor. Sonuç olarak iki farklı koleksiyona birbirleriyle ilişkili iki veri eklenmiş oluyor. Böylece m_MasterUpdateList ile gerçek sistem listesi arasındaki bağlantı kurulmuş oluyor. */</div>
    <div style="padding: 1px 18px 1px 66px;">m_UnmanagedSystemsToUpdate.<span style="color: #60a5fa;">Add</span>(sysHandle);</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;">m_systemSortDirty = <span style="color: #c084fc;">true</span>;</div>
    <div style="padding: 1px 18px;">}</div>
  </div>
</div>

<div style="height: 15px;"></div>

## UpdateIndex

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; max-width: 1050px; font-family: sans-serif; box-sizing: border-box;">
  <div style="background-color: #0d1323; border-bottom: 1px solid #1e293b; padding: 10px 16px; display: flex; align-items: center; justify-content: space-between;">
    <div style="display: flex; gap: 6px;">
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #f59e0b; display: inline-block;"></span>
      <span style="width: 10px; height: 10px; border-radius: 50%; background-color: #22c55e; display: inline-block;"></span>
    </div>
    <span style="color: #64748b; font-size: 11px; font-family: monospace;">UpdateIndex.cs</span>
  </div>
  <div style="padding: 14px 0; color: #e2e8f0; font-family: monospace; font-size: 13px; line-height: 1.4; overflow-x: auto;">
    <div style="padding: 1px 18px;"><span style="color: #c084fc;">internal struct</span> <span style="color: #38bdf8;">UpdateIndex</span></div>
    <div style="padding: 1px 18px;">{</div>
    <div style="padding: 1px 40px; color: #64748b;">/* Struct'ın bütün bilgisini saklayan alan */</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">private ushort</span> Data;</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 40px; color: #64748b;">/* Data içerisindeki 15. biti kontrol eder. */</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public bool</span> IsManaged =&gt; (Data &amp; <span style="color: #fde047;">0x8000</span>) != <span style="color: #fde047;">0</span>;</div>
    <div style="padding: 1px 40px; color: #64748b;">/* Data içerisindeki ilk 15 biti (sağdan sola doğru) okur. */</div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public int</span> Index =&gt; Data &amp; <span style="color: #fde047;">0x7fff</span>;</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">public</span> <span style="color: #60a5fa;">UpdateIndex</span>(<span style="color: #c084fc;">int</span> index, <span style="color: #c084fc;">bool</span> managed)</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 61px; color: #64748b;">/* İndeks değerini ushort türüne dönüştürerek Data içerisine yazar. */</div>
    <div style="padding: 1px 61px; color: #64748b;">/* index değerinin 15 bitlik alana sığması gerekir. Yani: index <= 32767 olmalıdır. Çünkü 32768 veya daha büyük bir değer verilirse indeksin en yüksek biti yani managed bayrağı olarak kullanılan biti de etkilenebilir. */</div>
    <div style="padding: 1px 18px 1px 66px;">Data = (<span style="color: #c084fc;">ushort</span>) index;</div>
    <div style="padding: 1px 61px; color: #64748b;">/* (1 << 15 ya da 0 << 15) olur. Bu da 0000 0000 0000 0000 ya da 0000 0000 0000 0001 değerini 15 basamak sola kaydırmak demektir. */</div>
    <div style="padding: 1px 18px 1px 66px;">Data |= (<span style="color: #c084fc;">ushort</span>)((managed ? <span style="color: #fde047;">1</span> : <span style="color: #fde047;">0</span>) &lt;&lt; <span style="color: #fde047;">15</span>);</div>
    <div style="padding: 1px 61px; color: #64748b;">/* Örneğin new UpdateIndex(5, true); çağrılmış olsun. */</div>
    <div style="padding: 1px 61px; color: #64748b;">/* 1.Satır: Data = 0000 0000 0000 0101 (5) */</div>
    <div style="padding: 1px 61px; color: #64748b;">/* 2.Satır: managed true olduğu için managed=1 ve 'Data |= (1 << 15)' olur.*/</div>
    <div style="padding: 1px 61px; color: #64748b;">/* 2.Satır: Binary 1 değerini (0000 0000 0000 0001) 15 basamak sola kaydırıp Data ile bitwise OR işlemine sok.*/</div>
    <div style="padding: 1px 61px; color: #64748b;">/* (0000 0000 0000 0001) --> (1000 0000 0000 0000) => "Data = Data | (1000 0000 0000 0000)"*/</div>
    <div style="padding: 1px 61px; color: #64748b;">/* (0000 0000 0000 0101) | (1000 0000 0000 0000) = (1000 0000 0000 0101) yani artık Data = (1000 0000 0000 0101) değerini taşır. Bu da Indeks değeri 5 olan Managed bir sistem olduğunu gösterir.*/</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px 1px 42px; height: 8px;"></div>
    <div style="padding: 1px 18px 1px 42px;"><span style="color: #c084fc;">override public string</span> <span style="color: #60a5fa;">ToString</span>()</div>
    <div style="padding: 1px 18px 1px 42px;">{</div>
    <div style="padding: 1px 18px 1px 66px;"><span style="color: #c084fc;">return</span> IsManaged ? <span style="color: #fbbf24;">"Managed: Index "</span> + Index : <span style="color: #fbbf24;">"UnManaged: Index "</span> + Index;</div>
    <div style="padding: 1px 18px 1px 42px;">}</div>
    <div style="padding: 1px 18px;">}</div>
  </div>
  <div style="background-color: #0e172a; border-top: 1px solid #1e293b; padding: 12px 18px; color: #94a3b8; font-size: 13px; line-height: 1.5;">
    UpdateIndex, bir sistemin kendisini saklamaz. Sistemin hangi listede bulunduğunu ve o listedeki indeksini saklar. Tek bir 16-bit <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">ushort</code> değerinin en yüksek bitini (15. bit) sistemin <b>Managed</b> olup olmadığını tutmak, kalan 15 bitini ise liste indeksini saklamak için kullanır. Böylece iki ayrı alan veya iki ayrı değişken yerine tek bir 16-bitlik değer kullanılıyor.
  </div>
</div>
<div style="height: 15px;"></div>
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 10px; padding: 16px 18px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <div style="color: #94a3b8; font-size: 14px; margin-bottom: 10px;">ushort Değer Aralığı (16-bit)</div>
  <div style="font-family: monospace; font-size: 20px; font-weight: bold; color: #38bdf8; text-align: center; padding: 8px; background-color: #0e172a; border-radius: 6px; margin-bottom: 14px;">
    0 &le; Data &le; 65535
  </div>
  <div style="display: flex; flex-direction: column; gap: 8px; font-size: 23px; color: #cbd5e1; line-height: 1.5;">
    <div><strong style="color: #f8fafc;">16-bit Toplam Aralık (2¹⁶ - 1):</strong> 0 ile 65.535 arası.</div>
  </div>
</div>
<div style="height: 15px;"></div>
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 10px; padding: 16px 18px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <div style="color: #94a3b8; font-size: 14px; margin-bottom: 10px;">Index Değer Aralığı (Alt 15-bit)</div>
  <div style="font-family: monospace; font-size: 20px; font-weight: bold; color: #4ade80; text-align: center; padding: 8px; background-color: #0e172a; border-radius: 6px; margin-bottom: 14px;">
    0 &le; Index &le; 32767
  </div>
  <div style="display: flex; flex-direction: column; gap: 8px; font-size: 23px; color: #cbd5e1; line-height: 1.5;">
    <div><strong style="color: #f8fafc;">Alt 15-bit Toplam Aralık (2¹⁵ - 1):</strong> 0 ile 32.767 arası yani en fazla 32.768 adet sistem indekslenebilir. </div>
  </div>
</div>
<div style="height: 15px;"></div>
<div style="display: flex; flex-direction: column; gap: 8px; font-size: 23px; color: #cbd5e1; line-height: 1.5;">
  <div><strong style="color: #60a5fa;">Managed Flag (15. bit — <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">0x8000</code>) </div>
</div>
<div style="display: flex; flex-direction: column; gap: 8px; font-size: 18px; color: #cbd5e1; line-height: 1.5;">
  <div><strong style="color: #ffffff;">32.768 (2¹⁵) değerine karşılık gelir. <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">Data &gt;= 32768</code> olduğunda <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">IsManaged</code> özelliği <code style="color: #c084fc;">true</code> döner.</div>
</div>

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 18px 20px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <div style="display: grid; grid-template-columns: 1fr 15fr; gap: 6px; margin-bottom: 6px; text-align: center; font-size: 11px;">
    <div style="background-color: #0c1730; border: 1px solid #1e3a8a; border-radius: 6px; padding: 5px 2px; color: #60a5fa; font-weight: bold;">Type</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 6px; padding: 5px 8px; color: #4ade80; font-weight: bold;">Index</div>
  </div>
  <div style="display: grid; grid-template-columns: repeat(16, 1fr); gap: 4px; text-align: center; font-family: monospace;">
    <div style="background-color: #0c1730; border: 1px solid #2563eb; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #60a5fa;">15</div>
      <div style="font-size: 9px; color: #93c5fd; margin-top: 3px;">M</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">14</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">13</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">12</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">11</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">10</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">9</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">8</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">7</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">6</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">5</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">4</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">3</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">2</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">1</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
    <div style="background-color: #09211d; border: 1px solid #166534; border-radius: 5px; padding: 8px 0;">
      <div style="font-size: 11px; font-weight: bold; color: #4ade80;">0</div>
      <div style="font-size: 9px; color: #86efac; margin-top: 3px;">I</div>
    </div>
  </div>
  <div style="display: grid; grid-template-columns: repeat(16, 1fr); gap: 4px; margin-top: 4px; text-align: center; font-family: monospace;">
    <div style="background-color: #0c1730; border: 1px solid #1e3a8a; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #93c5fd;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
    <div style="background-color: #09211d; border: 1px solid #14532d; border-radius: 4px; padding: 3px 0; font-size: 9px; color: #86efac;">0/1</div>
  </div>
  <div style="display: flex; flex-direction: column; gap: 6px; margin-top: 14px; font-size: 13px; color: #cbd5e1; border-top: 1px solid #1e293b; padding-top: 12px;">
    <div>• En soldaki bit (<code style="color: #60a5fa; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">15</code>): <strong>Managed</strong> bilgisi.</div>
    <div>• Kalan 15 bit (<code style="color: #4ade80; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">0–14</code>): <strong>Index</strong>.</div>
  </div>
</div>

Bu tasarım sayesinde Data tek başına her iki bilgiyi de taşır.

### IsManaged property
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 18px 20px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <!-- BAŞLIK ALANI -->
  <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #1e293b; padding-bottom: 12px; margin-bottom: 16px;">
    <span style="font-weight: bold; font-size: 14px; color: #f8fafc; letter-spacing: 0.5px;"></span>
    <span style="background-color: #291811; color: #e65a20; border: 1px solid #431407; font-size: 15px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Managed System</span>
  </div>
  <!-- ADIM 1 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">1</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">Hexadecimal değer:</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #fde047; font-weight: bold;">0x8000</div>
    </div>
  </div>
  <!-- ADIM 2 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">2</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">İkili (Binary) karşılığı:</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #e2e8f0; letter-spacing: 1px;">
        <span style="background-color: rgba(56, 189, 248, 0.2); color: #38bdf8; padding: 2px 5px; border-radius: 4px; font-weight: bold;">1</span>000 0000 0000 0000
      </div>
      <div style="font-size: 12.5px; color: #94a3b8; margin-top: 6px;">Yani yalnızca en yüksek bit (15. bit) <code style="color: #38bdf8; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">1</code>'dir.</div>
    </div>
  </div>
  <!-- ADIM 3 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">3</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;"></div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #e2e8f0; margin-bottom: 10px;">
        Data <span style="color: #c084fc; font-weight: bold;">&amp;</span> <span style="color: #fde047;">0x8000</span>
      </div>
      <div style="background-color: #0c192c; border-left: 3px solid #38bdf8; border-radius: 6px; padding: 10px 14px; font-size: 13px; color: #cbd5e1; line-height: 1.5;">
        <div>Burada <strong>bitwise AND (<code style="color: #c084fc;">&amp;</code>)</strong> işlemi yapılır.</div>
        <div style="margin-top: 4px; color: #94a3b8;">Bitwise AND işleminde <strong>iki bit de 1 ise</strong> sonuç <strong>1</strong> olur (diğer tüm durumlarda 0'dır).</div>
      </div>
    </div>
  </div>
  <!-- ADIM 4 -->
  <div style="display: flex; align-items: flex-start;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #4ade80; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">4</span>
    </div>
    <div style="flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">Örnek Bit-Bit AND İşlemi (<span style="color: #f584dc; font-weight: bold;">Managed = 1</span>, Index = 5):</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 12px 16px; font-family: monospace; font-size: 14px; line-height: 1.6; color: #e2e8f0; overflow-x: auto;">
        <div><span style="color: #94a3b8; display: inline-block; width: 65px;">Data :</span> <span style="color: #f584dc; font-weight: bold;">1</span>000 0000 0000 0<span style="color: #fbbf24;">1</span>0<span style="color: #fbbf24;">1</span></div>
        <div><span style="color: #94a3b8; display: inline-block; width: 65px;">Mask :</span> <span style="color: #38bdf8; font-weight: bold;">1</span>000 0000 0000 0000 <span style="color: #64748b;">(0x8000)</span></div>
        <div style="border-top: 1px solid #334155; margin: 6px 0;"></div>
        <div><span style="color: #4ade80; display: inline-block; width: 65px; font-weight: bold;">Sonuç:</span> <span style="background-color: rgba(74, 222, 128, 0.2); color: #4ade80; padding: 1px 4px; border-radius: 3px; font-weight: bold;">1</span>000 0000 0000 0000 <span style="color: #4ade80;">(!= 0 &rarr; true)</span></div>
      </div>
      <div style="font-size: 12px; color: #94a3b8; margin-top: 8px;">Sağdaki index bitleri (<code style="color: #fbbf24; background-color: #1e293b; padding: 1px 4px; border-radius: 3px;">0101</code> = 5) maske tarafından sıfırlanır. Yalnızca 15. bit korunur ve sonuç <code style="color: #4ade80; background-color: #1e293b; padding: 1px 4px; border-radius: 3px;">0</code>'dan farklı olduğu için <strong><code style="color: #fbbf24; background-color: #1e293b; padding: 1px 4px; border-radius: 3px;">(Data &amp; 0x8000) != 0</code> ifadesi true</strong> döner. Bu da sistemin <span style="background-color: #291811; color: #e65a20; border: 1px solid #431407; font-size: 12px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">managed</span> olduğunu gösterir.</div>
    </div>
  </div>
</div>

<div style="height: 15px;"></div>

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 18px 20px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <!-- BAŞLIK ALANI -->
  <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #1e293b; padding-bottom: 12px; margin-bottom: 16px;">
    <span style="font-weight: bold; font-size: 14px; color: #f8fafc; letter-spacing: 0.5px;"></span>
    <span style="background-color: #27451a; color: #0ce817; border: 1px solid #063808; font-size: 15px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Unmanaged System</span>
  </div>
  <!-- ADIM 1 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">1</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">Hexadecimal değer:</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #fde047; font-weight: bold;">0x8000</div>
    </div>
  </div>
  <!-- ADIM 2 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">2</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">İkili (Binary) karşılığı:</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #e2e8f0; letter-spacing: 1px;">
        <span style="background-color: rgba(56, 189, 248, 0.2); color: #38bdf8; padding: 2px 5px; border-radius: 4px; font-weight: bold;">1</span>000 0000 0000 0000
      </div>
      <div style="font-size: 12.5px; color: #94a3b8; margin-top: 6px;">Yani yalnızca en yüksek bit (15. bit) <code style="color: #38bdf8; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">1</code>'dir.</div>
    </div>
  </div>
  <!-- ADIM 3 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">3</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;"></div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #e2e8f0; margin-bottom: 10px;">
        Data <span style="color: #c084fc; font-weight: bold;">&amp;</span> <span style="color: #fde047;">0x8000</span>
      </div>
      <div style="background-color: #0c192c; border-left: 3px solid #38bdf8; border-radius: 6px; padding: 10px 14px; font-size: 13px; color: #cbd5e1; line-height: 1.5;">
        <div>Burada <strong>bitwise AND (<code style="color: #c084fc;">&amp;</code>)</strong> işlemi yapılır.</div>
        <div style="margin-top: 4px; color: #94a3b8;">Bitwise AND işleminde <strong>iki bit de 1 ise</strong> sonuç <strong>1</strong> olur (diğer tüm durumlarda 0'dır).</div>
      </div>
    </div>
  </div>
  <!-- ADIM 4 -->
  <div style="display: flex; align-items: flex-start;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #4ade80; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">4</span>
    </div>
    <div style="flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">Örnek Bit-Bit AND İşlemi (<span style="color: #f584dc; font-weight: bold;">Managed = 0</span>, Index = 5):</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 12px 16px; font-family: monospace; font-size: 14px; line-height: 1.6; color: #e2e8f0; overflow-x: auto;">
        <div><span style="color: #94a3b8; display: inline-block; width: 65px;">Data :</span> <span style="color: #f584dc; font-weight: bold;">0</span>000 0000 0000 0<span style="color: #fbbf24;">1</span>0<span style="color: #fbbf24;">1</span></div>
        <div><span style="color: #94a3b8; display: inline-block; width: 65px;">Mask :</span> <span style="color: #38bdf8; font-weight: bold;">1</span>000 0000 0000 0000 <span style="color: #64748b;">(0x8000)</span></div>
        <div style="border-top: 1px solid #334155; margin: 6px 0;"></div>
        <div><span style="color: #4ade80; display: inline-block; width: 65px; font-weight: bold;">Sonuç:</span> <span style="background-color: rgba(74, 222, 128, 0.2); color: #4ade80; padding: 1px 4px; border-radius: 3px; font-weight: bold;">0</span>000 0000 0000 0000 <span style="color: #4ade80;">(!= 0 &rarr; false)</span></div>
      </div>
      <div style="font-size: 12px; color: #94a3b8; margin-top: 8px;">Tüm index bitleri maske tarafından sıfırlanır. Sonuç <code style="color: #4ade80; background-color: #1e293b; padding: 1px 4px; border-radius: 3px;">0</code> olduğu için <strong><code style="color: #fbbf24; background-color: #1e293b; padding: 1px 4px; border-radius: 3px;">(Data &amp; 0x8000) != 0</code> ifadesi false</strong> döner. Bu da sistemin <span style="background-color: #27451a; color: #0ce817; border: 1px solid #063808; font-size: 12px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">unmanaged</span> olduğunu gösterir.</div>
    </div>
  </div>
</div>

### Index property
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 18px 20px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <!-- BAŞLIK ALANI -->
  <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #1e293b; padding-bottom: 12px; margin-bottom: 16px;">
    <span style="font-weight: bold; font-size: 14px; color: #f8fafc; letter-spacing: 0.5px;"></span>
    <span style="background-color: #27451a; color: #0ce817; border: 1px solid #063808; font-size: 15px; font-weight: bold; padding: 2px 8px; border-radius: 5px;"></span>
  </div>
  <!-- ADIM 1 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">1</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">Hexadecimal değer:</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #fde047; font-weight: bold;">0x7fff</div>
    </div>
  </div>
  <!-- ADIM 2 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">2</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">İkili (Binary) karşılığı:</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #e2e8f0; letter-spacing: 1px;">
        <span style="background-color: rgba(56, 189, 248, 0.2); color: #38bdf8; padding: 2px 5px; border-radius: 4px; font-weight: bold;">0</span> <span style="background-color: #f58989; color: #f01818; padding: 2px 5px; border-radius: 4px; font-weight: bold;">111 1111 1111 1111</span>
      </div>
      <div style="font-size: 12.5px; color: #94a3b8; margin-top: 6px;">Yani en yüksek bit <span style="background-color: rgba(56, 189, 248, 0.2); color: #38bdf8; padding: 2px 5px; border-radius: 4px; font-weight: bold;">0</span>, diğer 15 bit <span style="background-color: #f58989; color: #f01818; padding: 2px 5px; border-radius: 4px; font-weight: bold;">1</span>'dir.</div>
    </div>
  </div>
  <!-- ADIM 3 -->
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #38bdf8; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">3</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 18px; flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;"></div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-family: monospace; font-size: 14px; color: #e2e8f0; margin-bottom: 10px;">
        Data <span style="color: #c084fc; font-weight: bold;">&amp;</span> <span style="color: #fde047;">0x7fff</span>
      </div>
      <div style="background-color: #0c192c; border-left: 3px solid #38bdf8; border-radius: 6px; padding: 10px 14px; font-size: 13px; color: #cbd5e1; line-height: 1.5;">
        <div><strong>En yüksek biti temizler, kalan 15 biti korur.</strong></div>
      </div>
    </div>
  </div>
  <!-- ADIM 4 -->
  <div style="display: flex; align-items: flex-start;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 22px;">
      <span style="background-color: #1e293b; color: #4ade80; font-size: 12px; font-weight: bold; width: 22px; height: 22px; border-radius: 50%; display: flex; align-items: center; justify-content: center;">4</span>
    </div>
    <div style="flex: 1;">
      <div style="font-size: 14px; color: #cbd5e1; margin-bottom: 8px;">Örnek Bit-Bit AND İşlemi (<span style="color: #f584dc; font-weight: bold;">Managed = 1</span>, Index = 5):</div>
      <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 12px 16px; font-family: monospace; font-size: 14px; line-height: 1.6; color: #e2e8f0; overflow-x: auto;">
        <div><span style="color: #94a3b8; display: inline-block; width: 65px;">Data :</span> <span style="color: #f584dc; font-weight: bold;">1</span>000 0000 0000 0<span style="color: #fbbf24;">1</span>0<span style="color: #fbbf24;">1</span></div>
        <div><span style="color: #94a3b8; display: inline-block; width: 65px;">Mask :</span> <span style="color: #38bdf8; font-weight: bold;">0</span>111 1111 1111 1111 <span style="color: #64748b;">(0x7fff)</span></div>
        <div style="border-top: 1px solid #334155; margin: 6px 0;"></div>
        <div><span style="color: #4ade80; display: inline-block; width: 65px; font-weight: bold;">Sonuç:</span> <span style="background-color: rgba(74, 222, 128, 0.2); color: #4ade80; padding: 1px 4px; border-radius: 3px; font-weight: bold;">0</span>000 0000 0000 0<span style="color: #fbbf24;">1</span>0<span style="color: #fbbf24;">1</span> <span style="color: #4ade80;">(Index &rarr; 5)</span></div>
      </div>
      <div style="font-size: 12px; color: #94a3b8; margin-top: 8px;"> Managed bilgisi en yüksek bitte tutulduğu için indeks okunurken bu bitin yanlışlıkla indeksin parçası olarak değerlendirilmesi engelleniyor. Index property'sinin dönüş tipi int olsa da saklanan indeksin alanı yalnızca 15 bittir. Dolayısıyla temsil edilebilen indeks aralığı:</div>
      <div style="font-family: monospace; font-size: 18px; font-weight: bold; color: #b25ff5; text-align: center; padding: 8px; background-color: #0e172a; border-radius: 6px; margin-bottom: 14px;">
         0 &le; Index &le; 32767
      </div>
    </div>
  </div>
</div>

## İki ayrı liste, tek bir ortak sıralama
Örneğin oyununda şu dört sistemin bulunduğunu düşünelim:
- <span style="color: #ea580c; display: inline-block; width: 170px; font-weight: bold;">EnemyHealthSystem</span> → <span style="color: #ea580c; display: inline-block; width: 170px; font-weight: bold;">Managed</span>
- <span style="color: #2ce31b; display: inline-block; width: 170px; font-weight: bold;">EnemyMovementSystem</span> → <span style="color: #2ce31b; display: inline-block; width: 170px; font-weight: bold;">Unmanaged</span>
- <span style="color: #ea580c; display: inline-block; width: 170px; font-weight: bold;">PlayerHealthSystem</span> → <span style="color: #ea580c; display: inline-block; width: 170px; font-weight: bold;">Managed</span>
- <span style="color: #2ce31b; display: inline-block; width: 170px; font-weight: bold;">PlayerMovementSystem</span> → <span style="color: #2ce31b; display: inline-block; width: 170px; font-weight: bold;">Unmanaged</span>

Bunları kendi türlerine göre iki ayrı listeye yerleştirelim:

<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 18px 20px; font-family: monospace; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <div style="margin-bottom: 16px;">
    <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 10px;">
      <span style="background-color: #291811; color: #ea580c; border: 1px solid #431407; font-family: sans-serif; font-size: 11px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Managed</span>
    </div>
    <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-size: 13px; line-height: 1.7;">
      <div><span style="color: #ea580c; font-weight: bold;">[0]</span> <span style="color: #e2e8f0;">EnemyHealthSystem</span></div>
      <div><span style="color: #ea580c; font-weight: bold;">[1]</span> <span style="color: #e2e8f0;">PlayerHealthSystem</span></div>
    </div>
  </div>
  <div>
    <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 10px;">
      <span style="background-color: #0c241b; color: #22c55e; border: 1px solid #14532d; font-family: sans-serif; font-size: 11px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Unmanaged</span>
    </div>
    <div style="background-color: #0e172a; border: 1px solid #1e293b; border-radius: 8px; padding: 10px 14px; font-size: 13px; line-height: 1.7;">
      <div><span style="color: #22c55e; font-weight: bold;">[0]</span> <span style="color: #e2e8f0;">EnemyMovementSystem</span></div>
      <div><span style="color: #22c55e; font-weight: bold;">[1]</span> <span style="color: #e2e8f0;">PlayerMovementSystem</span></div>
    </div>
  </div>
</div>

Dolayısıyla yalnızca Index = 0 bilgisine bakarak hangi sistemden bahsettiğimizi anlayamayız. İşte <code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">UpdateIndex</code> burada devreye giriyor.

<code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">UpdateIndex</code> iki bilgi tutuyordu:
```csharp
public bool IsManaged => (Data & 0x8000) != 0;
public int Index => Data & 0x7fff;
```
Bu iki property sayesinde sistemin hangi koleksiyonda olduğunu ve o koleksiyondaki indeksini biliyoruz.

Örneğin:

<code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">new UpdateIndex(0, true);</code> Managed sistem listesindeki 0 indeksli sistemi temsil eder.

<code style="color: #e2e8f0; background-color: #1e293b; padding: 1px 5px; border-radius: 4px;">new UpdateIndex(0, false);</code> Unmanaged sistem listesindeki 0 indeksli sistemi temsil eder.

Şimdi <code style="color: #000000; background-color: #d682f5; padding: 1px 5px; border-radius: 4px;">m_MasterUpdateList</code> isimli koleksiyonun şu şekilde olduğunu düşünelim:
<div style="background-color: #090c15; border: 1px solid #1e293b; border-radius: 12px; padding: 18px 20px; font-family: sans-serif; color: #f8fafc; max-width: 650px; box-sizing: border-box;">
  <div style="font-weight: bold; font-size: 15px; margin-bottom: 18px; color: #d682f5;">m_MasterUpdateList </div><div style="font-weight: bold; font-size: 15px; margin-bottom: 18px; color: #f8fafc;">Ortak güncelleme sırası</div>
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 14px; line-height: 1.2;">1</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 16px; flex: 1;">
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <span style="font-weight: bold; font-size: 14px; color: #f8fafc; line-height: 1.2;">Managed: Index 0</span>
        <span style="background-color: #3d1b09; color: #ea580c; border: 1px solid #803f1d; font-size: 11px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Managed</span>
      </div>
      <div style="color: #ea580c; font-size: 13px; margin-top: 4px; font-family: monospace;">EnemyHealthSystem</div>
    </div>
  </div>
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 14px; line-height: 1.2;">2</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 16px; flex: 1;">
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <span style="font-weight: bold; font-size: 14px; color: #f8fafc; line-height: 1.2;">Unmanaged: Index 0</span>
        <span style="background-color: #0c241b; color: #22c55e; border: 1px solid #14532d; font-size: 11px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Unmanaged</span>
      </div>
      <div style="color: #22c55e; font-size: 13px; margin-top: 4px; font-family: monospace;">EnemyMovementSystem</div>
    </div>
  </div>
  <div style="display: flex; align-items: stretch;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 14px; line-height: 1.2;">3</span>
      <div style="width: 1px; background-color: #334155; flex-grow: 1; margin: 6px 0;"></div>
    </div>
    <div style="padding-bottom: 16px; flex: 1;">
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <span style="font-weight: bold; font-size: 14px; color: #f8fafc; line-height: 1.2;">Managed: Index 1</span>
        <span style="background-color: #3d1b09; color: #ea580c; border: 1px solid #803f1d; font-size: 11px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Managed</span>
      </div>
      <div style="color: #ea580c; font-size: 13px; margin-top: 4px; font-family: monospace;">PlayerHealthSystem</div>
    </div>
  </div>
  <div style="display: flex; align-items: flex-start;">
    <div style="display: flex; flex-direction: column; align-items: center; margin-right: 16px; width: 20px;">
      <span style="color: #94a3b8; font-size: 14px; line-height: 1.2;">4</span>
    </div>
    <div style="flex: 1;">
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <span style="font-weight: bold; font-size: 14px; color: #f8fafc; line-height: 1.2;">Unmanaged: Index 1</span>
        <span style="background-color: #0c241b; color: #22c55e; border: 1px solid #14532d; font-size: 11px; font-weight: bold; padding: 2px 8px; border-radius: 5px;">Unmanaged</span>
      </div>
      <div style="color: #22c55e; font-size: 13px; margin-top: 4px; font-family: monospace;">PlayerMovementSystem</div>
    </div>
  </div>
</div>

<code style="color: #000000; background-color: #d682f5; padding: 1px 5px; border-radius: 4px;">m_MasterUpdateList</code> sistemlerin kendisini değil, UpdateIndex değerlerini saklar.

Yani her eleman aslında şunu söyler:
- Managed veya unmanaged listelerinden hangisine bakılacak?
- O listede hangi indeksteki sistem alınacak?

Bu sayede iki ayrı sistem koleksiyonu tek bir ortak sıralama içerisinde temsil edilebiliyor.

m_MasterUpdateList içerisinde new UpdateIndex(1, false); varsa Unity gerçek sistemi nasıl buluyor? IsManaged ve Index property'lerini birlikte kullanarak.

Mantıksal olarak şöyle bir işlem yapılabilir:
```csharp
foreach (var updateIndex in m_MasterUpdateList)
{
    if (updateIndex.IsManaged)
    {
        // Managed sistem listesinden al
        var system = m_ManagedSystemsToUpdate[updateIndex.Index];
        // Managed sistemin güncellemesini gerçekleştir
    }
    else
    {
        // Unmanaged sistem listesinden al
        // !! 
        var systemHandle = m_UnmanagedSystemsToUpdate[updateIndex.Index];
        // Unmanaged sistemin güncellemesini gerçekleştir
    }
}
```

### m_MasterUpdateList neden gerekli?
Diyelim ki sistemlerin şu sırayla çalışmasını istiyorsun:
1. EnemyHealthSystem -- Managed
2. EnemyMovementSystem -- Unmanaged
3. PlayerHealthSystem -- Managed
4. PlayerMovementSystem -- Unmanaged

İki ayrı liste kullanmak tek başına yeterli olmaz çünkü managed sistemlerin kendi listesi, unmanaged sistemlerin kendi listesi vardır. Eğer managed sistem listesini önce tamamen çalıştırırsan sıralama şöyle olur:
1. EnemyHealthSystem
2. PlayerHealthSystem
3. EnemyMovementSystem
4. PlayerMovementSystem

Bu da hemen yukarıdaki sırayı bozar. Ancak m_MasterUpdateList sayesinde:
0. Managed: Index 0
1. Unmanaged: Index 0
2. Managed: Index 1
3. Unmanaged: Index 1
şeklinde ortak bir sıralama oluşturabilirsin. Bu sayede sistemlerin güncellenme sırasını istediğin gibi kontrol edebilirsin.

<div style="background-color: #0c192c; border: 1px solid #1e3a8a; border-left: 4px solid #38bdf8; border-radius: 8px; padding: 14px 18px; font-family: sans-serif; max-width: 650px; box-sizing: border-box;">
  <div style="color: #4dacfa; font-weight: bold; font-size: 14px; margin-bottom: 4px;">
    ÖZET
  </div>
  <div style="color: #ffffff; font-weight: bold; font-size: 14px; margin-bottom: 4px;">
    Kısacası m_MasterUpdateList, managed ve unmanaged sistemlerin farklı listelerde tutulmasına rağmen aynı ComponentSystemGroup içerisinde birlikte sıralanabilmesini sağlayan indeks tabanlı bir eşleştirme listesidir.
  </div>
  <div style="color: #cbd5e1; font-size: 13px; line-height: 1.5;">
  </div>
</div>

