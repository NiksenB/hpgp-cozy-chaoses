using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

class PlaneFlightDataAuthoring : MonoBehaviour
{
    public float maxSpeed = 800f;          // m/s
    public float v1Speed = 70f;           // m/s
    
    public float thrust = 35; // kN
    public float cruisingAltitudePercentage = 0.5f;  // meters above planet surface

    public float maxClimbAngle = 25f;
    public float maxDescentAngle = 20f;
    
    public float steerStrength = 25f;
    public float pitchStrength = 30f;
    
    class Baker : Baker<PlaneFlightDataAuthoring>
    {
        public override void Bake(PlaneFlightDataAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlaneFlightDataComponent
            {
                MaxSpeed = authoring.maxSpeed,
                V1Speed = authoring.v1Speed,
                Thrust = authoring.thrust,
                CruisingAltitudePercentage = authoring.cruisingAltitudePercentage,
                
                MaxClimbAngle = authoring.maxClimbAngle,
                MaxDescentAngle = authoring.maxDescentAngle,
                
                SteerStrength = authoring.steerStrength,
                PitchStrength = authoring.pitchStrength,
                
                CurrentPhase = FlightPhase.TakeOff
            });
        }
    }
}

public struct PlaneFlightDataComponent : IComponentData
{
    public float MaxSpeed;          // m/s
    public float V1Speed;           // m/s
    public float Thrust ;           // kN
    public float CruisingAltitudePercentage;  // Percentage of planet radius
    public float MaxClimbAngle;
    public float MaxDescentAngle;

    public float SteerStrength;
    public float PitchStrength;
    
    public FlightPhase CurrentPhase;
}

public enum FlightPhase
{
    TakeOff,
    Climb,
    Cruise,
    Descent,
    Landing
}