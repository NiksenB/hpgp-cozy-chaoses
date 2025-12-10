using Unity.Entities;
using UnityEngine;

namespace Components
{
    public class PlaneTagAuthoring : MonoBehaviour
    {
        private class PlaneComponentAuthoringBaker : Baker<PlaneTagAuthoring>
        {
            public override void Bake(PlaneTagAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new PlaneTag());
            }
        }
    }

    public struct PlaneTag : IComponentData
    {
    }
}