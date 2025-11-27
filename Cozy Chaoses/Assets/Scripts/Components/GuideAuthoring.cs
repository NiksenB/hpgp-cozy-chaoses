using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GuideAuthoring : MonoBehaviour
{
    public GameObject planeObject; // Reference to the actual plane
    
    class Baker : Baker<GuideAuthoring>
    {
        public override void Bake(GuideAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            var planeEntity = GetEntity(authoring.planeObject, TransformUsageFlags.Dynamic);

            AddComponent(entity, new GuideComponent
            {
                PlaneEntityReference = planeEntity
            });
        }
    }
}

public struct GuideComponent : IComponentData
{
    public Entity PlaneEntityReference;
}