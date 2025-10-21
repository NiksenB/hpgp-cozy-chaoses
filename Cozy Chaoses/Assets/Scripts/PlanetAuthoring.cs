using Unity.Entities;
using UnityEngine;

public class PlanetAuthoring : MonoBehaviour
{
    public float radius = 50f;
    
    class Baker : Baker<PlanetAuthoring>
    {
        public override void Bake(PlanetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Planet
            {
                Radius = authoring.radius
            });
        }
    }
}

public struct Planet : IComponentData
{
    public float Radius;
}