using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlanetAuthoring : MonoBehaviour
{
    private class Baker : Baker<PlanetAuthoring>
    {
        public override void Bake(PlanetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlanetComponent());
        }
    }
}

public struct PlanetComponent : IComponentData
{
    public float Radius;
    public float3 Center;
}