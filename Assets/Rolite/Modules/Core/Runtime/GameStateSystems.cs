using Unity.Entities;
using UnityEngine;

namespace Rolite.Core
{
    /// <summary>
    /// World oluşturulurken, oyun akışı için tek bir entity oluşturur.
    /// EntityManager burada doğrudan güvenlidir: henüz frame iterasyonu başlamamıştır.
    /// </summary>
    [DisableAutoCreation]
    public sealed partial class CoreWorldInitializationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var gameStateEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(gameStateEntity, new GameState
            {
                Value = GameFlowState.Menu,
                Version = 0
            });
            EntityManager.AddBuffer<GameStateTransitionRequest>(gameStateEntity);
            EntityManager.AddBuffer<GameStateTransitionEvent>(gameStateEntity);

            // Kurulum tamamlandı; bu sistem yalnızca OnCreate yaşam döngüsü için vardır.
            Enabled = false;
            Debug.Log("[Rolite.Core] Rolite Game World initialized in Menu state.");
        }

        protected override void OnUpdate() { }
    }

    /// <summary>
    /// Bir önceki frame'in transient olaylarını, yeni istekler işlenmeden önce temizler.
    /// Böylece tüketiciler yalnızca bu frame'deki geçişleri görür.
    /// </summary>
    [DisableAutoCreation]
    public sealed partial class GameStateEventCleanupSystem : SystemBase
    {
        private EntityQuery _gameStateQuery;

        protected override void OnCreate()
        {
            _gameStateQuery = GetEntityQuery(
                ComponentType.ReadOnly<GameState>(),
                ComponentType.ReadWrite<GameStateTransitionEvent>());
            RequireForUpdate(_gameStateQuery);
        }

        protected override void OnUpdate()
        {
            var entity = _gameStateQuery.GetSingletonEntity();
            EntityManager.GetBuffer<GameStateTransitionEvent>(entity).Clear();
        }
    }

    /// <summary>
    /// Sıraya giren istekleri FIFO olarak uygular ve veri değişimini event buffer'ında yayınlar.
    /// Component ekleyip çıkarmadığı için state geçişi entity'yi farklı bir archetype/chunk'a taşımaz.
    /// </summary>
    [DisableAutoCreation]
    public sealed partial class GameStateTransitionSystem : SystemBase
    {
        private EntityQuery _gameStateQuery;

        protected override void OnCreate()
        {
            _gameStateQuery = GetEntityQuery(
                ComponentType.ReadWrite<GameState>(),
                ComponentType.ReadWrite<GameStateTransitionRequest>(),
                ComponentType.ReadWrite<GameStateTransitionEvent>());
            RequireForUpdate(_gameStateQuery);
        }

        protected override void OnUpdate()
        {
            var entity = _gameStateQuery.GetSingletonEntity();
            var requests = EntityManager.GetBuffer<GameStateTransitionRequest>(entity);
            if (requests.IsEmpty)
                return;

            var events = EntityManager.GetBuffer<GameStateTransitionEvent>(entity);
            var state = EntityManager.GetComponentData<GameState>(entity);

            for (var index = 0; index < requests.Length; index++)
            {
                var target = requests[index].Target;
                if (target == state.Value)
                    continue;

                var previous = state.Value;
                state.Value = target;
                state.Version++;
                events.Add(new GameStateTransitionEvent
                {
                    Previous = previous,
                    Current = target,
                    Version = state.Version
                });
            }

            requests.Clear();
            EntityManager.SetComponentData(entity, state);
        }
    }
}
