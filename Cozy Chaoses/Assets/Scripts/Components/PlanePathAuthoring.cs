using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Components
{
    public class PlanePathAuthoring : MonoBehaviour
    {
        public PathShape shape;
        public float3 endPoint; // Assign another GameObject as destination
        public float3 controlPoint; // Optional for curves
        public float duration = 10f;
        public float frequency = 2f;
        public float amplitude = 5f;
        public float stabilizationSpeed = 10f;
        public GameObject planeObject; // Reference to the actual plane

        private class PlanePathAuthoringBaker : Baker<PlanePathAuthoring>
        {
            public override void Bake(PlanePathAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var planeEntity = GetEntity(authoring.planeObject, TransformUsageFlags.Dynamic);

                // Add Path Data
                AddComponent(entity, new PlanePathComponent
                {
                    Shape = authoring.shape,
                    StartPoint = authoring.transform.position,
                    EndPoint = authoring.endPoint,
                    ControlPoint = authoring.controlPoint,
                    Duration = authoring.duration,
                    Frequency = authoring.frequency,
                    Amplitude = authoring.amplitude,
                    ElapsedTime = 0f,
                    PlaneEntity = planeEntity,
                });

                AddComponent<GuideTargetTag>(entity);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
            Vector3 end = endPoint;
            Vector3 control = controlPoint;

            int segments = 50;
            Vector3 prevPos = start;

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 currentPos = Vector3.zero;

                Vector3 forwardDir = (end - start).normalized;
                Vector3 rightDir = Vector3.Cross(forwardDir, Vector3.up);
                if (rightDir == Vector3.zero) rightDir = Vector3.right;
                Vector3 upDir = Vector3.Cross(rightDir, forwardDir);

                switch (shape)
                {
                    case PathShape.Linear:
                        currentPos = Vector3.Lerp(start, end, t);
                        break;
                    case PathShape.SineWave:
                        Vector3 linearPos = Vector3.Lerp(start, end, t);
                        float sineOffset = Mathf.Sin(t * Mathf.PI * 2 * frequency) * amplitude;
                        currentPos = linearPos + (upDir * sineOffset);
                        break;
                }

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
    Sigmoid,
    Curve // Simple Quadratic Bezier
}

public struct PlanePathComponent : IComponentData
{
    public PathShape Shape;
    public float3 StartPoint;
    public float3 EndPoint;
    public float3 ControlPoint; // For Bezier/Curve
    public Entity PlaneEntity;

    // Configuration
    public float Duration; // How long the flight takes
    public float Frequency; // For Sine
    public float Amplitude; // For Sine

    // Internal State
    public float ElapsedTime;
}

public struct GuideTargetTag : IComponentData
{
}

public struct PlaneStabilizer : IComponentData
{
    public Entity TargetEntity;
    public float RotationSpeed;
}