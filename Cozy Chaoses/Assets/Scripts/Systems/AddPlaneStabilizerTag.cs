using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct AddPlaneStabilizerTag : ISystem
    {
        private ComponentLookup<PlaneStabilizer> _planeStabilizerLookup;
        
        public void OnCreate(ref SystemState state)
        {
            _planeStabilizerLookup = state.GetComponentLookup<PlaneStabilizer>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _planeStabilizerLookup.Update(ref state);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (planeComponent, entity) in SystemAPI.Query<RefRO<PlaneComponent>>().WithEntityAccess())
            {
                var planeEntity = planeComponent.ValueRO.PlaneEntityReference;
                if (_planeStabilizerLookup.HasComponent(planeEntity))
                {
                    continue;
                }
                
                Debug.Log("Trying to add PlaneStabilizer to plane entity: " + planeEntity);
                if (planeEntity != Entity.Null)
                {
                    Debug.Log("Adding PlaneStabilizer to plane entity: " + planeEntity);
                    ecb.AddComponent(planeEntity, new PlaneStabilizer
                    {
                        TargetEntity = entity, // Point back to the guide
                        RotationSpeed = 5f,
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}