using Unity.Entities;
using UnityEngine;

internal class CollidingAuthoring : MonoBehaviour
{
}

internal class CollidingAuthoringBaker : Baker<CollidingAuthoring>
{
    public override void Bake(CollidingAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<CollidingComponent>(entity);
    }
}


public struct CollidingComponent : IComponentData
{
    public Entity CollidedWith;
}