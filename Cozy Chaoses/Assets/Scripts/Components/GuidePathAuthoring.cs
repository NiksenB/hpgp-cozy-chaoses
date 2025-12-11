using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Components
{
    public class GuidePathAuthoring : MonoBehaviour
    {
        public float3 endPoint;
        public float targetHeight;

        private static GuidePathComponent GetPathComponent(GuidePathAuthoring authoring)
        {
            return new GuidePathComponent
            {
                EndPoint = authoring.endPoint,
                TargetAltitude = authoring.targetHeight
            };
        }

        private class GuidePathAuthoringBaker : Baker<GuidePathAuthoring>
        {
            public override void Bake(GuidePathAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var component = GetPathComponent(authoring);

                AddComponent(entity, component);
            }
        }
    }
}

public struct GuidePathComponent : IComponentData
{
    public float3 EndPoint;
    public float TargetAltitude;
}