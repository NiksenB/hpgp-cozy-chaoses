using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Components
{
    public class PlanePathAuthoring : MonoBehaviour
    {
        public PathShape shape;
        public float3 endPoint;     // Assign another GameObject as destination
        public Transform controlPoint; // Optional for curves
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
                    ControlPoint = authoring.controlPoint != null ? (float3)authoring.controlPoint.position : float3.zero,
                    Duration = authoring.duration,
                    Frequency = authoring.frequency,
                    Amplitude = authoring.amplitude,
                    ElapsedTime = 0f,
                    PlaneEntity = planeEntity,
                });
                
                AddComponent<GuideTargetTag>(entity);
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
    public float Duration;       // How long the flight takes
    public float Frequency;      // For Sine
    public float Amplitude;      // For Sine
    
    // Internal State
    public float ElapsedTime;
}

public struct GuideTargetTag : IComponentData { }

public struct PlaneStabilizer : IComponentData
{
    public Entity TargetEntity;
    public float RotationSpeed;
}