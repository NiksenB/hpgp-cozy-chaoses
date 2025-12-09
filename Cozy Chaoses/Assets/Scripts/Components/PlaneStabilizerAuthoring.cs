using Unity.Entities;
using UnityEngine;

public class PlaneStabilizerAuthoring : MonoBehaviour
{
    public GameObject guideObject; // Reference to the guide entity
    public float rotationSpeed = 6f; // How fast to rotate towards target
    public float damping = 7f; // Damping factor for angular velocity
    public float maxAngularSpeed = 6f; // Limit on angular velocity
    public float responseSpeed = 8f; // How quickly to correct orientation
    public float forwardWeight = 1.0f; // Weight for forward alignment
    public float upWeight = 0.5f; // Weight for up alignment

    private class Baker : Baker<PlaneStabilizerAuthoring>
    {
        public override void Bake(PlaneStabilizerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var guideEntity = GetEntity(authoring.guideObject, TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlaneStabilizerComponent
            {
                GuideEntity = guideEntity,
                RotationSpeed = authoring.rotationSpeed,
                Damping = authoring.damping,
                MaxAngularSpeed = authoring.maxAngularSpeed,
                ResponseSpeed = authoring.responseSpeed,
                ForwardWeight = authoring.forwardWeight,
                UpWeight = authoring.upWeight
            });
        }
    }
}

public struct PlaneStabilizerComponent : IComponentData
{
    public Entity GuideEntity;
    public float RotationSpeed;
    public float Damping;
    public float MaxAngularSpeed;
    public float ResponseSpeed;
    public float ForwardWeight;
    public float UpWeight;
}