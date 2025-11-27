using Unity.Entities;
using UnityEngine;

class ShouldDespawnAuthoring : MonoBehaviour
{
    class Baker : Baker<ShouldDespawnAuthoring>
    {
        public override void Bake(ShouldDespawnAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ShouldDespawnTag>(entity);
        }
    }
}

public struct ShouldDespawnTag : IComponentData
{
}