using Unity.Entities;
using UnityEngine;

public class PlaneAuthoring : MonoBehaviour
{
    public GameObject Wings;
    
    class Baker : Baker<PlaneAuthoring>
    {
        public override void Bake(PlaneAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Plane
            {
                Wings = GetEntity(authoring.Wings, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct Plane : IComponentData
{
    public Entity Wings;
}