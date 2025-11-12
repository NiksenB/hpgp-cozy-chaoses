using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlanetAuthoring : MonoBehaviour
{
    class Baker : Baker<PlanetAuthoring>
    {
        public override void Bake(PlanetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlanetComponent
            {
            });
        }
    }
}

public struct PlanetComponent : IComponentData
{
    public float Radius;
    public float3 Center;
    public float EarthScale => Radius / 6371; // Scale relative to Earth's radius. Assuming radius is in kilometers.
    public float Gravity => 6f; // 9.81f * EarthScale; // Gravity scaled by EarthScale.
}

