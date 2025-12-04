using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Components
{
    public class GuidePathAuthoring : MonoBehaviour
    {
        public float3 endPoint; 
        public float3 controlPoint;
        public float targetHeight;

        private class GuidePathAuthoringBaker : Baker<GuidePathAuthoring>
        {
            public override void Bake(GuidePathAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                var component = GetPathComponent(authoring);

                AddComponent(entity, component);

                AddComponent<GuideTargetTag>(entity);
            }
        }
        
        private static GuidePathComponent GetPathComponent(GuidePathAuthoring authoring)
        {
            return new GuidePathComponent
            {
                StartPoint = authoring.transform.position,
                EndPoint = authoring.endPoint,
                TargetAltitude = authoring.targetHeight,
            };
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
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
    }
}

public struct GuidePathComponent : IComponentData
{
    public float3 StartPoint;
    public float3 EndPoint;
    public float TargetAltitude;
}

public struct GuideTargetTag : IComponentData
{
}

public struct PlaneStabilizer : IComponentData
{
    public Entity TargetEntity;
}