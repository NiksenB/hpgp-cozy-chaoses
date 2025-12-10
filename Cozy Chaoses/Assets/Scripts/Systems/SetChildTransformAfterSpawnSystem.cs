using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

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
            // Based on: https://discussions.unity.com/t/what-is-the-proper-way-of-instantiating-an-entity-prefab-with-child-physics-bodies/910049/22
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var config = SystemAPI.GetSingleton<ConfigComponent>();

            switch (config.ExecutionMode)
            {
                case ExecutionMode.Main:
                    foreach (var (planeStabilizerComponent, velocity, entity) in SystemAPI.Query<RefRO<PlaneStabilizerComponent>, RefRW<PhysicsVelocity>>()
                                 .WithAll<JustSpawnedTag>()
                                 .WithEntityAccess())
                    {
                        var planeTransform = transformLookup[entity];
                        var guideTransform = transformLookup[planeStabilizerComponent.ValueRO.GuideEntity];

                        planeTransform.Position = guideTransform.Position;
                        planeTransform.Rotation = guideTransform.Rotation;

                        ecb.SetComponent(entity, planeTransform);

                        var newVelocity = velocity.ValueRO;
                        newVelocity.Angular = float3.zero;
                        newVelocity.Linear = float3.zero;
                        velocity.ValueRW = newVelocity;

                        ecb.RemoveComponent<JustSpawnedTag>(entity);
                    }
                    break;

                case ExecutionMode.Schedule:
                    state.Dependency = new SetInitialPlaneTransformJobSingle
                    {
                        ECB = ecb,
                        TransformLookup = transformLookup
                    }.Schedule(state.Dependency);
                    break;

                case ExecutionMode.ScheduleParallel:
                    state.Dependency = new SetInitialPlaneTransformJobParallel
                    {
                        ECB = ecb.AsParallelWriter(),
                        TransformLookup = transformLookup
                    }.ScheduleParallel(state.Dependency);
                    break;
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(JustSpawnedTag))]
    public partial struct SetInitialPlaneTransformJobSingle : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer ECB;

        public void Execute(Entity entity, in PlaneStabilizerComponent planeStabilizerComponent,
            ref PhysicsVelocity velocity)
        {
            var planeTransform = TransformLookup[entity];
            var guideTransform = TransformLookup[planeStabilizerComponent.GuideEntity];

            planeTransform.Position = guideTransform.Position;
            planeTransform.Rotation = guideTransform.Rotation;

            ECB.SetComponent(entity, planeTransform);

            velocity.Angular = float3.zero;
            velocity.Linear = float3.zero;

            ECB.RemoveComponent<JustSpawnedTag>(entity);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JustSpawnedTag))]
    public partial struct SetInitialPlaneTransformJobParallel : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in PlaneStabilizerComponent planeStabilizerComponent,
            ref PhysicsVelocity velocity)
        {
            var planeTransform = TransformLookup[entity];
            var guideTransform = TransformLookup[planeStabilizerComponent.GuideEntity];

            planeTransform.Position = guideTransform.Position;
            planeTransform.Rotation = guideTransform.Rotation;

            ECB.SetComponent(chunkIndex, entity, planeTransform);

            velocity.Angular = float3.zero;
            velocity.Linear = float3.zero;

            ECB.RemoveComponent<JustSpawnedTag>(chunkIndex, entity);
        }
    }
}
