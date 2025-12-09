using Unity.Entities;
using UnityEngine;

namespace Components
{
    public class JustSpawnedTagAuthoring : MonoBehaviour
    {
        private class JustSpawnedTagAuthoringBaker : Baker<JustSpawnedTagAuthoring>
        {
            public override void Bake(JustSpawnedTagAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new JustSpawnedTag());
            }
        }
    }

    public struct JustSpawnedTag : IComponentData
    {
    }
}