using Unity.Entities;
using UnityEngine;

namespace Rolite.Authoring
{
    /// <summary>
    /// Bu component, bir entity'nin hızını tanımlar.
    /// </summary>
    public struct PlayerVelocity : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring bileşeni, Editörde bir GameObject'in özelliklerini tanımlamanın bir yolunu sağlar.
    /// Bu authoring bileşeni, hız özelliğini tanımlamayı sağlar.
    /// ECS, çalışma zamanında authoring bileşenini kullanmaz, ancak Baker sınıfını kullanarak onu bir entity bileşenine dönüştürür.
    /// </summary>
    public class PlayerVelocityAuthoring : MonoBehaviour
    {
        public float Value;
        
        /// <summary>
        /// Baking işleminde, bu Baker, bir alt sahnedeki her PlayerVelocityAuthoring örneği için bir kez çalışır.
        /// </summary>
        private class PlayerVelocityAuthoringBaker : Unity.Entities.Baker<PlayerVelocityAuthoring>
        {
            public override void Bake(PlayerVelocityAuthoring authoring)
            {
                // GetEntity, "ECS’nin pre-built ecs baker yöntemlerini kullanarak GameObject’ten oluşturduğu" bir varlık döndürür.
                // TransformUsageFlags.Dynamic, Bake yöntemine varlığa Transforms.LocalTransform bileşenini eklemesi talimatını verir.
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                var velocity = new PlayerVelocity
                {
                    Value = authoring.Value
                };

                AddComponent(entity, velocity);
            }
        }
    }
}