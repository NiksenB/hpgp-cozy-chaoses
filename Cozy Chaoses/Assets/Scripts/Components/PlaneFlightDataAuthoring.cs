using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

class PlaneFlightDataAuthoring : MonoBehaviour
{
    public float maxSpeed = 250f;          // m/s
    public float cruisingAltitudePercentage = 0.5f;  // meters above planet surface
    public float accelerationForce = 100000f;      // m/s^2
    public float climbRate = 20f;         // m/s
    public float descentRate = 15f;       // m/s
    
    public float steerStrength = 25f;
    public float stabilityStrength = 30f;
    public float angularDamping = 3.5f;
    
    class Baker : Baker<PlaneFlightDataAuthoring>
    {
        public override void Bake(PlaneFlightDataAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlaneFlightDataComponent
            {
                MaxSpeed = authoring.maxSpeed,
                CruisingAltitudePercentage = authoring.cruisingAltitudePercentage,
                Acceleration = authoring.accelerationForce,
                ClimbRate = authoring.climbRate,
                DescentRate = authoring.descentRate,
                
                SteerStrength = authoring.steerStrength,
                StabilityStrength = authoring.stabilityStrength,
                AngularDamping = authoring.angularDamping,
                
                CurrentPhase = FlightPhase.TakeOff
            });
        }
    }
}

public struct PlaneFlightDataComponent : IComponentData
{
    public float MaxSpeed;          // m/s
    public float CruisingAltitudePercentage;  // Percentage of planet radius
    public float Acceleration;      // m/s^2
    public float ClimbRate;         // m/s
    public float DescentRate;       // m/s

    public float SteerStrength;
    public float StabilityStrength;
    public float AngularDamping;
    
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