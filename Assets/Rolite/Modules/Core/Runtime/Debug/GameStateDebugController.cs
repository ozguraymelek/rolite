using Unity.Entities;
using UnityEngine;

namespace Rolite.Core.Debugging
{
    /// <summary>
    /// İlk modülün Play Mode doğrulama aracıdır. Sadece Editor veya Development Build'de görünür.
    /// Bir GameObject'e ekleyip butonlarla state request buffer'ını test edebilirsiniz.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStateDebugController : MonoBehaviour
    {
        private void OnGUI()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var entityManager = world.EntityManager;
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GameState>());
            if (query.CalculateEntityCount() != 1)
                return;

            var entity = query.GetSingletonEntity();
            var state = entityManager.GetComponentData<GameState>(entity);

            GUILayout.BeginArea(new Rect(16, 16, 230, 210), GUI.skin.box);
            GUILayout.Label("Rolite Core Debug");
            GUILayout.Label($"State: {state.Value}");
            GUILayout.Label($"Version: {state.Version}");
            foreach (GameFlowState target in System.Enum.GetValues(typeof(GameFlowState)))
            {
                if (GUILayout.Button($"Request {target}"))
                {
                    entityManager.GetBuffer<GameStateTransitionRequest>(entity)
                        .Add(new GameStateTransitionRequest { Target = target });
                }
            }

            GUILayout.EndArea();
        }
    }
}
