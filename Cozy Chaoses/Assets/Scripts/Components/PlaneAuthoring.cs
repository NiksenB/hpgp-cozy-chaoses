using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlaneAuthoring : MonoBehaviour
{
    public GameObject Wings;
    public float3 dest;
    
    class Baker : Baker<PlaneAuthoring>
    {
        public override void Bake(PlaneAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlaneComponent
            {
                Wings = GetEntity(authoring.Wings, TransformUsageFlags.Dynamic),
                Dest = authoring.dest
            });
        }
    }
}

public struct PlaneComponent : IComponentData
{
    public Entity Wings;
    public float3 Dest;
}