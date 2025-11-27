using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct AddPlaneStabilizerComponentSystem : ISystem
    {
        private ComponentLookup<PlaneStabilizerComponent> _planeStabilizerLookup;

        public void OnCreate(ref SystemState state)
        {
            _planeStabilizerLookup = state.GetComponentLookup<PlaneStabilizerComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _planeStabilizerLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (planeComponent, entity) in SystemAPI.Query<RefRO<GuideComponent>>().WithEntityAccess())
            {
                var planeEntity = planeComponent.ValueRO.PlaneEntityReference;
                if (_planeStabilizerLookup.HasComponent(planeEntity))
                {
                    continue;
                }

                if (planeEntity != Entity.Null)
                {
                    ecb.AddComponent(planeEntity, new PlaneStabilizerComponent
                    {
                        TargetEntity = entity, // Point back to the guide
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}