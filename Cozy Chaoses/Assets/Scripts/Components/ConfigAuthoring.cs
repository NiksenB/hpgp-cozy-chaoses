using Unity.Entities;
using UnityEngine;

public enum ExecutionMode
{
    Main,
    Schedule,
    ScheduleParallel
}

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject planePrefab;
    public GameObject airportPrefab;
    public GameObject planetPrefab;
    public GameObject explosionPrefab;

    private class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new PrefabConfigComponent
            {
                PlanetPrefab = GetEntity(authoring.planetPrefab, TransformUsageFlags.Dynamic),
                AirportPrefab = GetEntity(authoring.airportPrefab, TransformUsageFlags.Dynamic),
                PlanePrefab = GetEntity(authoring.planePrefab, TransformUsageFlags.Dynamic),
                ExplosionPrefab = GetEntity(authoring.explosionPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct PrefabConfigComponent : IComponentData
{
    public Entity PlanePrefab;
    public Entity AirportPrefab;
    public Entity PlanetPrefab;
    public Entity ExplosionPrefab;
}

public struct ConfigComponent : IComponentData
{
    public ExecutionMode ExecutionMode;
    public bool EnableDebugMode;
    public bool EnableDespawnOnCollision;
    public double NextPlaneSpawnTimeLower;
    public double NextPlaneSpawnTimeUpper;

    public Entity PlanePrefab;
    public float PlaneSpeed; // How fast planes move
    public float PlaneRotationSpeed; // How fast to rotate towards target
    public float PlaneDamping; // Damping factor for angular velocity
    public float PlaneMaxAngularSpeed; // Limit on angular velocity
    public float PlaneResponseSpeed; // How quickly to correct orientation
    public float PlaneForwardWeight; // Weight for forward alignment
    public float PlaneUpWeight; // Weight for up alignment

    public Entity AirportPrefab;
    public int AirportCount;

    public Entity PlanetPrefab;
    public float PlanetRadius;

    public Entity ExplosionPrefab;
}