using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AddPlaneStabilizerTag : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (planePathData, entity) in SystemAPI.Query<RefRO<PlanePathComponent>>().WithEntityAccess())
            {
                var planeEntity = planePathData.ValueRO.PlaneEntity;
                if (planeEntity != Entity.Null)
                {
                    ecb.AddComponent(planeEntity, new PlaneStabilizer
                    {
                        TargetEntity = entity, // Point back to the guide
                        RotationSpeed = 10f,
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}