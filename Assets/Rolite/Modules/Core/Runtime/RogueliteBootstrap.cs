using Unity.Entities;

namespace Rolite.Core
{
    /// <summary>
    /// Unity'nin varsayılan dünyasını ve otomatik sistem keşfini devre dışı bırakır.
    /// Bu sınıfın tek görevi, oyunun PlayerLoop'a bağlanan tek ECS dünyasını ve
    /// değişmez üst-seviye çalışma sırasını oluşturmaktır.
    /// </summary>
    public sealed class RogueliteBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            var world = new World("Rolite Game World", WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = world;

            var root = world.GetOrCreateSystemManaged<RogueliteRootSystemGroup>();
            var initialization = world.GetOrCreateSystemManaged<RogueliteInitializationSystemGroup>();
            var gameState = world.GetOrCreateSystemManaged<GameStateSystemGroup>();
            var simulation = world.GetOrCreateSystemManaged<RogueliteSimulationSystemGroup>();
            var presentation = world.GetOrCreateSystemManaged<RoguelitePresentationSystemGroup>();

            // OnCreate içinde singleton'ı üretir. Disabled olduğu için frame başına çalışmaz.
            initialization.AddSystemToUpdateList(
                world.GetOrCreateSystemManaged<CoreWorldInitializationSystem>());

            // Group içi sıra da AddSystemToUpdateList çağrıları ile açıkça belirlenmiştir.
            gameState.AddSystemToUpdateList(
                world.GetOrCreateSystemManaged<GameStateEventCleanupSystem>());
            gameState.AddSystemToUpdateList(
                world.GetOrCreateSystemManaged<GameStateTransitionSystem>());

            // Kök sırayı burada değiştirmek, oyunun global çalışma sırasını değiştirmek demektir.
            root.AddSystemToUpdateList(initialization);
            root.AddSystemToUpdateList(gameState);
            root.AddSystemToUpdateList(simulation);
            root.AddSystemToUpdateList(presentation);

            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

            // true: DefaultWorldInitialization'ın otomatik sistem dünyası kurmasına izin verme.
            return true;
        }
    }
}
