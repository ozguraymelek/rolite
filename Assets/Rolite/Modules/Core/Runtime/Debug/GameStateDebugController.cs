// EntityManager ve ComponentType ile debug UI, ECS singleton'ını okuyup request yazabilir.
using Unity.Entities;
// MonoBehaviour ve IMGUI yalnızca test yüzeyi için kullanılan UnityEngine API'leridir.
using UnityEngine;

namespace Rolite.Core.Debugging
{
    /// <summary>
    /// İlk modülün Play Mode doğrulama aracıdır. Bu sınıf bilerek managed MonoBehaviour'dur: IMGUI
    /// ve sahne Inspector'ı ECS/Burst içinde kullanılamaz. Oyun simülasyonunun parçası değildir;
    /// release build'de erken döner. Alternatifi bir ECS UI köprüsüdür; onu 13. modülde kuracağız.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStateDebugController : MonoBehaviour
    {
        // Unity'nin managed IMGUI callback'i her GUI event'inde çağrılır.
        private void OnGUI()
        {
            // Editor dışındaki release build'de debug paneli çalıştırılmaz.
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            // Bootstrap tarafından atanan varsayılan world alınır.
            var world = World.DefaultGameObjectInjectionWorld;
            // Domain reload veya kapanış anında geçersiz world'e erişim engellenir.
            if (world == null || !world.IsCreated)
                return;

            // Managed test katmanı, EntityManager üzerinden ECS verisine ulaşır.
            var entityManager = world.EntityManager;
            // Geçici query yalnızca debug UI içindir; using, native query kaynağını event sonunda iade eder.
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GameState>());
            // Singleton kontratı bozulduysa yanlış entity'ye yazmak yerine panel çıkmaz.
            if (query.CalculateEntityCount() != 1)
                return;

            // Eşsiz global state entity'si alınır.
            var entity = query.GetSingletonEntity();
            // Inspector benzeri görünüm için blittable state component'i okunur.
            var state = entityManager.GetComponentData<GameState>(entity);

            // Sabit ekran alanı, test panelini gameplay kameradan bağımsız gösterir.
            GUILayout.BeginArea(new Rect(16, 16, 230, 210), GUI.skin.box);
            // Panel başlığıdır.
            GUILayout.Label("Rolite Core Debug");
            // Güncel enum değeri gözlemlenir.
            GUILayout.Label($"State: {state.Value}");
            // Gerçek geçiş sayısı gözlemlenir.
            GUILayout.Label($"Version: {state.Version}");
            // Yeni state eklendiğinde test butonunun otomatik oluşması için enum değerleri dolaşılır.
            foreach (GameFlowState target in System.Enum.GetValues(typeof(GameFlowState)))
            {
                // Her hedef için kullanıcıdan request üretme fırsatı verilir.
                if (GUILayout.Button($"Request {target}"))
                {
                    // Buton doğrudan state değiştirmez; üretim/tüketim ayrımını koruyan request buffer'a yazar.
                    entityManager.GetBuffer<GameStateTransitionRequest>(entity)
                        // Buffer'a eklenen eleman sonraki ECS update'inde FIFO olarak işlenecektir.
                        .Add(new GameStateTransitionRequest { Target = target });
                }
            }

            // Başlatılan IMGUI alanı dengeli biçimde kapatılır.
            GUILayout.EndArea();
        }
    }
}
