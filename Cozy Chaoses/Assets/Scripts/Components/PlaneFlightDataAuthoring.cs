using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

class PlaneFlightDataAuthoring : MonoBehaviour
{
    public float maxSpeed = 800f;          // m/s
    public float v1Speed = 70f;           // m/s
    
    public float maxThrust = 66; // kN
    public float cruisingAltitudePercentage = 0.5f;  // meters above planet surface

    public float liftStrength = 25f;
    public float dragCoefficient = 0.05f;
    public float pitchStrength = 3000f;
    public float yawStrength = 2000f;
    public float rollStrength = 2000f;
    
    class Baker : Baker<PlaneFlightDataAuthoring>
    {
        public override void Bake(PlaneFlightDataAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlaneFlightDataComponent
            {
                MaxSpeed = authoring.maxSpeed,
                V1Speed = authoring.v1Speed,
                MaxThrust = authoring.maxThrust,
                CruisingAltitudePercentage = authoring.cruisingAltitudePercentage,
                
                LiftStrength = authoring.liftStrength,
                DragCoefficient = authoring.dragCoefficient,
                PitchStrength = authoring.pitchStrength,
                RollStrength = authoring.rollStrength,
                YawStrength = authoring.yawStrength,
                
                CurrentPhase = FlightPhase.Cruise
            });
        }
    }
}

public struct PlaneFlightDataComponent : IComponentData
{
    public float MaxSpeed;          // m/s
    public float V1Speed;           // m/s
    public float MaxThrust ;           // kN
    public float CruisingAltitudePercentage;  // Percentage of planet radius

    public float LiftStrength;
    public float DragCoefficient;
    public float PitchStrength;
    public float RollStrength;
    public float YawStrength;
    
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