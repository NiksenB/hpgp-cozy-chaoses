using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.GraphicsIntegration;
using UnityEngine;

namespace Components
{
    public class PlaneFlightDebugDataAuthoring : MonoBehaviour
    {
        private class PlaneFlightDebugDataAuthoringBaker : Baker<PlaneFlightDebugDataAuthoring>
        {
            public override void Bake(PlaneFlightDebugDataAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<PlaneFlightDebugDataComponent>(entity);
            }
        }
    }
}

public struct PlaneFlightDebugDataComponent : IComponentData
{
    public float3 CurrentPosition;
    public float3 CurrentPositionOnSphere;
    public float3 DestinationPositon;
    public float3 DestinationPositionOnSphere;
    public float3 ToDestination;
    public float3 TangentDirection;
    public float3 ForwardOnSphere;
    public float AngleRadians;
    public float AngleDegrees;
    public float CurrentPitchAngle;
    public float3 PlaneNormal;
    
    public float CurrentAltitude;
    public float TargetAltitude;
    public float CurrentSpeed;
    public float TargetSpeed;
    public float CurrentAngle;
    public float3 TargetAngle;
    public float CurrentPitch;
    public float TargetPitch;
    
    public float3 AccelerationVector;
    public float InverseMass;

    public float EarthScale;

    public float3 RotationAxis;
    public float ScaledPitchTorque;
    public float3 PitchVector;
    public float AltitudeError;
    public float3 PitchTorque;
    public float3 PitchAccelerationVector;

    public float DesiredPitchInput;
    public float3 FinalPitchInput;
    public float3 LocalUp;
    public float ForwardDotLocalUp;
    public float RawCurrentPitch;

    public float RollError;
    public float3 RollTorque;
    public float3 RollAccelerationVector;
}