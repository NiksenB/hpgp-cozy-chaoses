using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlaneAuthoring : MonoBehaviour
{
    public GameObject planeObject; // Reference to the actual plane
    
    class Baker : Baker<PlaneAuthoring>
    {
        public override void Bake(PlaneAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            var planeEntity = GetEntity(authoring.planeObject, TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlaneComponent
            {
                PlaneEntityReference = planeEntity
            });
        }
    }
}

public struct PlaneComponent : IComponentData
{
    public Entity PlaneEntityReference;
}