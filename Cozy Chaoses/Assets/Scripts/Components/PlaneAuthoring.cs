using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlaneAuthoring : MonoBehaviour
{
    public float3 dest;
    
    class Baker : Baker<PlaneAuthoring>
    {
        public override void Bake(PlaneAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlaneComponent
            {
                Dest = authoring.dest
            });
        }
    }
}

public struct PlaneComponent : IComponentData
{
    public float3 Dest;
}