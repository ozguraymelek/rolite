// BurstCompile unmanaged sistem kodunu native makine koduna derleyebilmek için kullanılır.
using Unity.Burst;
// Allocator.Temp, sadece query tanımı kurulurken kullanılan kısa ömürlü unmanaged bellektir.
using Unity.Collections;
// EntityQuery, SystemState ve ISystem ECS çalışma zamanı API'leridir.
using Unity.Entities;

namespace Rolite.Core
{
    /// <summary>
    /// World oluşturulurken tek GameState entity'sini üretir. `struct ISystem`, SystemBase'in
    /// managed sınıf tahsisini ve virtual callback maliyetini ortadan kaldırır. Dezavantajı,
    /// managed referanslar (Animator, MonoBehaviour, string loglama) tutamamasıdır.
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct CoreWorldInitializationSystem : ISystem
    {
        // BurstCompile, OnCreate içindeki yapısal kurulumun da Burst güvenli kalmasını denetler.
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Boş entity yalnızca world ilk kurulurken yaratılır; iterasyon olmadığı için doğrudan
            // EntityManager yapısal çağrısı ECB gerektirmez.
            var gameStateEntity = state.EntityManager.CreateEntity();
            // Component'i bir kez eklemek archetype'ı kurar; sonraki state geçişleri bu archetype'ı korur.
            state.EntityManager.AddComponentData(gameStateEntity, new GameState
            {
                // Başlangıç akışı menüdür; gameplay modülleri InRun olmadıkça iş yapmayacaktır.
                Value = GameFlowState.Menu,
                // İlk gerçek geçişin sürümünü 1 yapmak için sayaç sıfırdan başlar.
                Version = 0
            });
            // Request buffer'ı bütün producer modüllerinin ortak, sıralı giriş kuyruğudur.
            state.EntityManager.AddBuffer<GameStateTransitionRequest>(gameStateEntity);
            // Event buffer'ı consumer modüllerinin o frame okunacak çıkış kanalıdır.
            state.EntityManager.AddBuffer<GameStateTransitionEvent>(gameStateEntity);

            // Kurulum tamamdır; ISystem handle'ını kapatmak OnUpdate maliyetini sıfırlar.
            state.Enabled = false;
        }

        // ISystem sözleşmesi OnUpdate ister; sistem OnCreate sonunda kapatıldığı için çağrılmaz.
        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }
    }

    /// <summary>
    /// Bir önceki frame'in transient olaylarını, yeni istekler işlenmeden önce temizler.
    /// Böylece tüketiciler yalnızca bu frame'deki geçişleri görür.
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct GameStateEventCleanupSystem : ISystem
    {
        // EntityQuery unmanaged bir handle'dır; sistemi Burst uyumlu tutarken tekrar query kurulumunu önler.
        private EntityQuery _gameStateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Neden state.GetEntityQuery(ComponentType, ComponentType) değil?
            // O overload `params ComponentType[]` kullanır; C# bunun için managed bir dizi oluşturur.
            // Burst managed diziyi native koda çeviremediği için "signature contains managed types" uyarısı verir.
            // EntityQueryBuilder ise unmanaged/ref-struct API'dir; Burst ile uyumlu query tanımı üretir.
            using var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
            // WithAll<GameState> yalnız varlığı kontrol eder ve read-only erişim bildirir.
            // WithAllRW<Event> Clear çağrısının buffer uzunluğunu değiştireceğini ECS'e önceden bildirir.
            _gameStateQuery = queryBuilder
                .WithAll<GameState>()
                .WithAllRW<GameStateTransitionEvent>()
                .Build(ref state);
            // Singleton oluşmadan sistem güncellenmez; null/boş query kontrolünü kaldırır.
            state.RequireForUpdate(_gameStateQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // GetSingletonEntity, mimari kontrat gereği tam bir GameState entity bekler.
            var entity = _gameStateQuery.GetSingletonEntity();
            // Clear kapasiteyi korur; her frame yeniden allocation yerine buffer uzunluğunu sıfırlar.
            state.EntityManager.GetBuffer<GameStateTransitionEvent>(entity).Clear();
        }
    }

    /// <summary>
    /// Sıraya giren istekleri FIFO olarak uygular ve veri değişimini event buffer'ında yayınlar.
    /// Component ekleyip çıkarmadığı için state geçişi entity'yi farklı bir archetype/chunk'a taşımaz.
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct GameStateTransitionSystem : ISystem
    {
        // Üç component türünü bir arada filtreleyen, cache'lenmiş unmanaged query handle'ı.
        private EntityQuery _gameStateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Params dizisi yaratmayan aynı Burst-uyumlu builder yolu burada da kullanılır.
            using var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
            // State SetComponentData ile, request Clear ile, event Add ile yazılır; üçü de RW bildirilir.
            _gameStateQuery = queryBuilder
                // Entities 6.6'nın builder overload'ı en fazla iki generic tür alır; üçüncü tür zincirlenir.
                .WithAllRW<GameState, GameStateTransitionRequest>()
                .WithAllRW<GameStateTransitionEvent>()
                .Build(ref state);
            // GameState singleton'ı yoksa OnUpdate'ın çalışmasını tamamen atlar.
            state.RequireForUpdate(_gameStateQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Bu query'nin tek entity kontratı, yanlışlıkla ikinci global state yaratılmasını erken görünür yapar.
            var entity = _gameStateQuery.GetSingletonEntity();
            // DynamicBuffer handle'ı entity'nin request kuyruğunu allocation yapmadan gösterir.
            var requests = state.EntityManager.GetBuffer<GameStateTransitionRequest>(entity);
            // İstek yokken event/state belleğine yazmamak write dependency ve iş yükünü azaltır.
            if (requests.Length == 0)
                return;

            // Bu frame'in yayın buffer'ı cleanup sisteminden sonra boştur.
            var events = state.EntityManager.GetBuffer<GameStateTransitionEvent>(entity);
            // Blittable component değere kopyalanır; loop sonunda tek seferde geri yazılır.
            var gameState = state.EntityManager.GetComponentData<GameState>(entity);

            // Buffer index sırası FIFO'dur; aynı frame'de Menu→InRun→Paused ardışık event üretir.
            for (var index = 0; index < requests.Length; index++)
            {
                // Mevcut istek elemanının hedef state'i okunur.
                var target = requests[index].Target;
                // Aynı değere geçiş event üretmez; UI/consumer tarafında gereksiz iş önlenir.
                if (target == gameState.Value)
                    continue;

                // Event için eski değer, mutation'dan önce saklanır.
                var previous = gameState.Value;
                // Enum alanını değiştirmek aynı archetype içindeki 1 byte veriyi günceller.
                gameState.Value = target;
                // Her gerçek değişim monoton sürümü artırır.
                gameState.Version++;
                // Olay buffer'ına sabit boyutlu, Burst-uyumlu event elemanı eklenir.
                events.Add(new GameStateTransitionEvent
                {
                    // Dinleyiciye yön bilgisi verilir.
                    Previous = previous,
                    // Dinleyiciye varılan state verilir.
                    Current = target,
                    // Event, güncel state sürümüyle ilişkilendirilir.
                    Version = gameState.Version
                });
            }

            // İşlenmiş queue elemanları silinir; kapasite sonraki frame üreticileri için korunur.
            requests.Clear();
            // Değere kopyalanmış component tek bir write ile chunk'a geri konur.
            state.EntityManager.SetComponentData(entity, gameState);
        }
    }
}
