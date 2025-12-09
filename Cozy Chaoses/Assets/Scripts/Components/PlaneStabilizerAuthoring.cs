using Unity.Entities;
using UnityEngine;

public class PlaneStabilizerAuthoring : MonoBehaviour
{
    public GameObject guideObject; // Reference to the guide entity

    private class Baker : Baker<PlaneStabilizerAuthoring>
    {
        public override void Bake(PlaneStabilizerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var guideEntity = GetEntity(authoring.guideObject, TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlaneStabilizerComponent
            {
                GuideEntity = guideEntity
            });
        }
    }
}

public struct PlaneStabilizerComponent : IComponentData
{
    public Entity GuideEntity;
}