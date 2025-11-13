using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class PlaneFlightAuthoring : MonoBehaviour
{
    public float mass = 11000; // in kg
    
    public float minAltitudeRatio = 0.3f;
    public float maxAltitudeRatio = 0.8f;
    
    public float descentStartDistance = 20000; // in meters
    
    class Baker : Baker<PlaneFlightAuthoring>
    {
        public override void Bake(PlaneFlightAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlaneFlightComponent
            {
                MinAltitudeRatio = authoring.minAltitudeRatio,
                MaxAltitudeRatio = authoring.maxAltitudeRatio,
                CurrentSpeed = 0,
                State = PlaneState.TakeOff,
            });
        }
    }
}

public struct PlaneFlightComponent : IComponentData
{
    public float MinAltitudeRatio; // Ratio of planet radius
    public float MaxAltitudeRatio; // Ratio of planet radius
    
    // Internal state
    public float MaxAltitude;
    public float DescentStartAltitude; 
    public float CurrentSpeed;
    public PlaneState State;
    
    public CurveData? CurrentCurve;
}

public struct CurveData
{
    public float Progress; // 0 to 1 along curve
    public float Speed;
    public CurveType Type; // Sigmoid, Bezier, etc.
    public float3 StartPos;
    public float3 EndPos;
    public float Height; // For sigmoid curve
    public float K; // Steepness for sigmoid
}

public enum PlaneState
{
    TakeOff,
    Climb,
    Cruise,
    Descent,
    Landing
}

public enum CurveType
{
    Sigmoid,
    // Bezier
}
