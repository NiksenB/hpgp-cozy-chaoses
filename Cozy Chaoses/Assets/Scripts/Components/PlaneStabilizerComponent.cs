using Unity.Entities;

namespace Components
{
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
}