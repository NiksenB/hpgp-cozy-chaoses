using Unity.Entities;
using Unity.Mathematics;
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
    public float3 PlanetCenter;
    public float3 Position;
    public float3 ToPlanet;
    public float3 GravityDir;
    public float3 LocalUp;
    public float  SphereRadius;
    public float  EarthScale;
    public float  CurrentAltitude;
    public float3 LinearVelocity;
    public float  CurrentSpeed;
    public float3 VelocityDir;
    public float3 Forward;
    public float3 Right;
    public float3 Up;
    public float  TargetAltitude;
    public float  TargetSpeed;
    public float3 GravityAccel;
    public float  SpeedSqr;
    public float  LiftForce;
    public float3 LiftAccel;
    public float  DragForce;
    public float3 DragAccel;
    public float  SpeedError;
    public float  ThrustInput;
    public float  AltitudeError;
    public float  PitchInput;
    public float  RollError;
    public float  RollInput;
    public float3 ForwardOnSphere;
    public float  AngleToTarget;
    public float  YawInput;
    public float3 ThrustForce;
    public float3 ThrustAccel;
    public float3 PitchTorque;
    public float3 RollTorque;
    public float3 YawTorque;
    public float3 TotalTorque;
    public float3 AngularAccel;
}