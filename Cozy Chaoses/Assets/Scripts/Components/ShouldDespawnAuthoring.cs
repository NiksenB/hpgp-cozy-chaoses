using Unity.Entities;
using UnityEngine;

class ShouldDespawnAuthoring : MonoBehaviour
{
    
}

class ShouldDespawnAuthoringBaker : Baker<ShouldDespawnAuthoring>
{
    public override void Bake(ShouldDespawnAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<ShouldDespawnComponent>(entity);
    }
}

public struct ShouldDespawnComponent : IComponentData
{
    
}