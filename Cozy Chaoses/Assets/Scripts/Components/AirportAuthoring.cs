using Unity.Entities;
using UnityEngine;

public class AirportAuthoring : MonoBehaviour
{
    class Baker : Baker<AirportAuthoring>
    {
        public override void Bake(AirportAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new AirportComponent
            {NextPlaneSpawnTime = entity.Index % 20d});
        }
    }
}

public struct AirportComponent : IComponentData
{
    public double NextPlaneSpawnTime;
}