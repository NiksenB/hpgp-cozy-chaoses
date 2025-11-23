using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlaneAuthoring : MonoBehaviour
{
    class Baker : Baker<PlaneAuthoring>
    {
        public override void Bake(PlaneAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlaneComponent());
        }
    }
}

public struct PlaneComponent : IComponentData
{
}