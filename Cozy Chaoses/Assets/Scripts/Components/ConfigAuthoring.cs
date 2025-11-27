using UnityEngine;
using Unity.Entities;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject planePrefab;
    public GameObject airportPrefab;
    public GameObject planetPrefab;
    
    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new PrefabConfigComponent
            {
                PlanetPrefab = GetEntity(authoring.planetPrefab, TransformUsageFlags.Dynamic),
                AirportPrefab = GetEntity(authoring.airportPrefab, TransformUsageFlags.Dynamic),
                PlanePrefab = GetEntity(authoring.planePrefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct PrefabConfigComponent : IComponentData
{
    public Entity PlanePrefab;
    public Entity AirportPrefab;
    public Entity PlanetPrefab;
}

public struct ConfigComponent : IComponentData
{
    public Entity PlanePrefab;
    
    public Entity AirportPrefab;
    public int AirportCount;
    
    public Entity PlanetPrefab;
    public float PlanetRadius;
}
