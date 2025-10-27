using UnityEngine;
using Unity.Entities;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject planePrefab;
    public GameObject airportPrefab;
    public GameObject planetPrefab;
    public float planetRadius;
    public int airportCount;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new ConfigComponent
            {
                PlanetPrefab = GetEntity(authoring.planetPrefab, TransformUsageFlags.Dynamic),
                AirportPrefab = GetEntity(authoring.airportPrefab, TransformUsageFlags.Dynamic),
                PlanePrefab = GetEntity(authoring.planePrefab, TransformUsageFlags.Dynamic),
                PlanetRadius = authoring.planetRadius,
                AirportCount = authoring.airportCount
            });
        }
    }

}

public struct ConfigComponent : IComponentData
{
    public Entity PlanePrefab;
    public Entity AirportPrefab;
    public Entity PlanetPrefab;
    public float PlanetRadius;
    public int AirportCount;
}
