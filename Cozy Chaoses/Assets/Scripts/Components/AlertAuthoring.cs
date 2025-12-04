using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class AlertAuthoring : MonoBehaviour
{
    public float3 entityPos;
    
    class Baker : Baker<AlertAuthoring>
    {
        public override void Bake(AlertAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new AlertComponent
            {
                EntityPos = authoring.entityPos
            });
        }
    }
}

public struct AlertComponent : IComponentData
{
    public float3 EntityPos;
}