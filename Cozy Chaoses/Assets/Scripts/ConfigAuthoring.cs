using UnityEngine;
using Unity.Entities;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject PlanePrefab;
    public int PlaneCount;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                PlanePrefab = GetEntity(authoring.PlanePrefab, TransformUsageFlags.Dynamic),
                PlaneCount = authoring.PlaneCount
            });
        }
    }

}

public struct Config : IComponentData
{
    public Entity PlanePrefab;
    public int PlaneCount;
}
