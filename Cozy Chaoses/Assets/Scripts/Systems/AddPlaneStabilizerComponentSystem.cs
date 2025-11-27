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
                        GuideEntity = entity, // Point back to the guide
                        RotationSpeed = 6f, // How fast to rotate towards target
                        Damping = 7f, // Damping factor for angular velocity
                        MaxAngularSpeed = 6f, // Limit on angular velocity
                        ResponseSpeed = 8f, // How quickly to correct orientation
                        ForwardWeight = 1.0f, // Weight for forward alignment
                        UpWeight = 0.5f, // Weight for up alignment
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}