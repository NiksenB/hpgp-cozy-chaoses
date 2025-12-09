using Unity.Entities;
using UnityEngine;

internal class ShouldDespawnAuthoring : MonoBehaviour
{
    private class Baker : Baker<ShouldDespawnAuthoring>
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