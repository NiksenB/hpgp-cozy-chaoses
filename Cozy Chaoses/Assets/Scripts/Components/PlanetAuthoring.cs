using Unity.Entities;
using UnityEngine;

public class PlanetAuthoring : MonoBehaviour
{
    public float radius = 25f;
    
    class Baker : Baker<PlanetAuthoring>
    {
        public override void Bake(PlanetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Planet
            AddComponent(entity, new PlanetComponent
            {
                Radius = authoring.radius
            });
        }
    }
}

public struct PlanetComponent : IComponentData
{
    public float Radius;
}