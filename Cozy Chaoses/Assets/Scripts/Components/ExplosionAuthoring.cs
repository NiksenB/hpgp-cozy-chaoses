using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ExplosionAuthoring : MonoBehaviour
{
    public float fade; 
    class Baker : Baker<ExplosionAuthoring>
    {
        public override void Bake(ExplosionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExplosionComponent
            {
                Fade = authoring.fade,
            });
        }
    }
}

public struct ExplosionComponent : IComponentData
{
    public float Fade;
}