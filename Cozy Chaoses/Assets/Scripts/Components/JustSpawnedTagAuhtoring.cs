using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Components
{
    public class JustSpawnedAuthoring : MonoBehaviour
    {
        private class JustSpawnedTagAuthoringBaker : Baker<JustSpawnedAuthoring>
        {
            public override void Bake(JustSpawnedAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new JustSpawnedTag());
            }
        }
    }

    public struct JustSpawnedTag : IComponentData
    {
    }
    
    public struct JustSpawnedMustBeMoved : IComponentData
    {
        public float3 Position;
        public quaternion Rotation; 
    }
}