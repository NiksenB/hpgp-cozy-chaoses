using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Components
{
    public class GuidePathAuthoring : MonoBehaviour
    {
        public float3 endPoint;
        public float targetHeight;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            var start = transform.position;
            Vector3 end = endPoint;
            // float height = targetHeight;

            // GuidePathComponent pathComponent = GetPathComponent(this);

            // int segments = 50;
            // Vector3 prevPos = start;
            //
            // for (int i = 1; i <= segments; i++)
            // {
            //     float t = (float)i / segments;
            //     Vector3 currentPos = LineCalculator.Calculate(pathComponent, t);
            //
            //     Gizmos.DrawLine(prevPos, currentPos);
            //     prevPos = currentPos;
            // }

            // Anchor points
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(start, 0.5f);
            Gizmos.DrawWireSphere(end, 0.5f);
            Gizmos.color = Color.red;
        }

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