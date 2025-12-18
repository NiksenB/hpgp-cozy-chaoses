using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateAfter(typeof(PlaneAndGuideSpawnSystem))]
    public partial struct SetChildTransformAfterSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ConfigComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (setup, entity) in SystemAPI.Query<RefRO<JustSpawnedMustBeMoved>>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<LocalTransform>(entity))
                {
                    ecb.SetComponent(entity, LocalTransform.FromPositionRotation(setup.ValueRO.Position, setup.ValueRO.Rotation));
                }

                if (SystemAPI.HasBuffer<LinkedEntityGroup>(entity))
                {
                    var linkedGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);

                    foreach (var linkedEntity in linkedGroup)
                    {
                        var childEntity = linkedEntity.Value;

                        if (SystemAPI.HasComponent<PhysicsVelocity>(childEntity))
                        {
                            ecb.SetComponent(childEntity, PhysicsVelocity.Zero);
                        }

                        if (!SystemAPI.HasComponent<Parent>(childEntity) && SystemAPI.HasComponent<JustSpawnedTag>(childEntity) && childEntity != entity)
                        {
                            ecb.SetComponent(childEntity, LocalTransform.FromPositionRotation(setup.ValueRO.Position, setup.ValueRO.Rotation));
                            ecb.RemoveComponent<JustSpawnedTag>(childEntity);
                        }
                    }
                }

                ecb.RemoveComponent<JustSpawnedMustBeMoved>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}