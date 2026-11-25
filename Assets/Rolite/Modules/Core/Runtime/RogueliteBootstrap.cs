// World, ICustomBootstrap ve PlayerLoop köprüsü Entities paketinden gelir.
using Unity.Entities;

namespace Rolite.Core
{
    /// <summary>
    /// Unity'nin varsayılan dünyasını ve otomatik sistem keşfini devre dışı bırakır.
    /// Bu sınıf zorunlu managed sınırdır: ICustomBootstrap Unity tarafından managed olarak çağrılır.
    /// Frame başına çalışan oyun sistemleri ise aşağıda ISystem olarak oluşturulur.
    /// </summary>
    public sealed class RogueliteBootstrap : ICustomBootstrap
    {
        // Unity bu metodu player loop kurulmadan önce bir kez çağırır.
        public bool Initialize(string defaultWorldName)
        {
            // WorldFlags.Game, bu dünyanın oyun dünyası olduğunu Entity araçlarına bildirir.
            var world = new World("Rolite Game World", WorldFlags.Game);

            // Hybrid/authoring kodunun varsayılan world'e ulaşacağı açık referanstır.
            World.DefaultGameObjectInjectionWorld = world;

            // ComponentSystemGroup, ISystem'leri PlayerLoop'a bağlayan zorunlu managed konteynerdir.
            // Gruplar iş mantığı çalıştırmaz; sadece elle belirlenmiş sırada child handle günceller.
            var root = world.GetOrCreateSystemManaged<RogueliteRootSystemGroup>();
            var initialization = world.GetOrCreateSystemManaged<RogueliteInitializationSystemGroup>();
            var gameState = world.GetOrCreateSystemManaged<GameStateSystemGroup>();
            var simulation = world.GetOrCreateSystemManaged<RogueliteSimulationSystemGroup>();
            var presentation = world.GetOrCreateSystemManaged<RoguelitePresentationSystemGroup>();

            // GetOrCreateSystem<T>(), unmanaged ISystem için SystemHandle döndürür; managed SystemBase
            // nesnesi tahsis etmez. OnCreate singleton'ı kurar, sonra sistem kendini kapatır.
            initialization.AddSystemToUpdateList(
                world.GetOrCreateSystem<CoreWorldInitializationSystem>());

            // Event temizliği her zaman yeni request işlenmeden önce çalışır.
            gameState.AddSystemToUpdateList(
                world.GetOrCreateSystem<GameStateEventCleanupSystem>());
            // FIFO request tüketicisi temiz sistemden sonra çalışır ve yeni event'leri yazar.
            gameState.AddSystemToUpdateList(
                world.GetOrCreateSystem<GameStateTransitionSystem>());
            // Kök sırası global kontrattır; modüller bunu attribute ile değiştiremez.
            root.AddSystemToUpdateList(initialization);
            root.AddSystemToUpdateList(gameState);
            root.AddSystemToUpdateList(simulation);
            root.AddSystemToUpdateList(presentation);

            // World'ü Unity PlayerLoop'a bir kez ekler; root group bundan sonra her frame güncellenir.
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

            // true, DefaultWorldInitialization'a ikinci/otomatik bir dünya kurmamasını söyler.
            return true;
        }
    }
}
