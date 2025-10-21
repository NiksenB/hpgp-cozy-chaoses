using UnityEngine;
using Unity.Entities;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject PlanePrefab;
    public GameObject AirportPrefab;
    public int PlaneCount;
    public int AirportCount;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                PlanePrefab = GetEntity(authoring.PlanePrefab, TransformUsageFlags.Dynamic),
                AirportPrefab = GetEntity(authoring.AirportPrefab, TransformUsageFlags.Dynamic),
                PlaneCount = authoring.PlaneCount,
                AirportCount = authoring.AirportCount
            });
        }
    }

}

public struct Config : IComponentData
{
    public Entity PlanePrefab;
    public Entity AirportPrefab;
    public int PlaneCount;
    public int AirportCount;
}
