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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Based on: https://discussions.unity.com/t/what-is-the-proper-way-of-instantiating-an-entity-prefab-with-child-physics-bodies/910049/22 
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            state.Dependency = new SetInitialPlaneTransformJob
            {
                ECB = ecb,
                TransformLookup = transformLookup
            }.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JustSpawnedTag))]
    public partial struct SetInitialPlaneTransformJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer ECB;

        // This is hacky and i kinda hate it, but I don't see another way right now
        [BurstCompile]
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
}