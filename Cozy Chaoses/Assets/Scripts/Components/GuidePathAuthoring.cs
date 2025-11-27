using DefaultNamespace;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Components
{
    public class GuidePathAuthoring : MonoBehaviour
    {
        public PathShape shape;
        public float3 endPoint; // Assign another GameObject as destination
        public float3 controlPoint; // Optional for curves
        public float duration = 10f;
        public float frequency = 2f;
        public float amplitudeOrSteepness = 5f;

        private class GuidePathAuthoringBaker : Baker<GuidePathAuthoring>
        {
            public override void Bake(GuidePathAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                var component = GetPathComponent(authoring);

                // Add Path Data
                AddComponent(entity, component);

                AddComponent<GuideTargetTag>(entity);
            }
        }
        
        private static GuidePathComponent GetPathComponent(GuidePathAuthoring authoring)
        {
            return new GuidePathComponent
            {
                Shape = authoring.shape,
                StartPoint = authoring.transform.position,
                EndPoint = authoring.endPoint,
                ControlPoint = authoring.controlPoint,
                Duration = authoring.duration,
                Frequency = authoring.frequency,
                AmplitudeOrSteepness = authoring.amplitudeOrSteepness,
                ElapsedTime = 0f,
            };
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
            Vector3 end = endPoint;
            Vector3 control = controlPoint;

            GuidePathComponent pathComponent = GetPathComponent(this);

            int segments = 50;
            Vector3 prevPos = start;

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 currentPos = LineCalculator.Calculate(pathComponent, t);

                Gizmos.DrawLine(prevPos, currentPos);
                prevPos = currentPos;
            }

            // Anchor points
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(start, 0.5f);
            Gizmos.DrawWireSphere(end, 0.5f);
            if (shape == PathShape.Curve)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(control, 0.3f);
            }
        }
    }
}

public enum PathShape
{
    Linear,
    SineWave,
    Curve // Simple Quadratic Bezier
}

public struct GuidePathComponent : IComponentData
{
    public PathShape Shape;
    public float3 StartPoint;
    public float3 EndPoint;
    public float3 ControlPoint; // For Bezier/Curve

    // Configuration
    public float Duration; // How long the flight takes
    public float Frequency; // For Sine
    public float AmplitudeOrSteepness; // For Sine/Sigmoid

    // Internal State
    public float ElapsedTime;
}

public struct GuideTargetTag : IComponentData
{
}

public struct PlaneStabilizer : IComponentData
{
    public Entity TargetEntity;
}