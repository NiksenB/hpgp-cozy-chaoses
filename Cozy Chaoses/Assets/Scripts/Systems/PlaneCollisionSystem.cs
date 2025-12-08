using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = System.Random;

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
partial struct PlaneCollisionSystem : ISystem
{
    private ComponentLookup<PlaneStabilizerComponent> _planeStabilizerLookup;
    private ComponentLookup<LocalTransform> _localTransformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<ConfigComponent>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<PlaneStabilizerComponent>()));

        _planeStabilizerLookup = state.GetComponentLookup<PlaneStabilizerComponent>(false);
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _planeStabilizerLookup.Update(ref state);
        _localTransformLookup.Update(ref state);

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var elapsed = (float)SystemAPI.Time.ElapsedTime;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var config = SystemAPI.GetSingleton<ConfigComponent>();

        state.Dependency = new PlaneCollisionJob
        {
            ECB = ecb,
            Elapsed = elapsed,
            Explosion = config.ExplosionPrefab,
            PlaneStabilizerLookup = _planeStabilizerLookup,
            LocalTransformLookup = _localTransformLookup
        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    [WithAll(typeof(AlertComponent))]
    struct PlaneCollisionJob : ICollisionEventsJob
    {
        public EntityCommandBuffer ECB;
        public float Elapsed;
        public Entity Explosion;
        public ComponentLookup<PlaneStabilizerComponent> PlaneStabilizerLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

        public void Execute(CollisionEvent collisionEvent)
        {
            var entityA = collisionEvent.EntityA;
            var entityB = collisionEvent.EntityB;

            var isBodyAPlane = PlaneStabilizerLookup.HasComponent(entityA);
            var isBodyBPlane = PlaneStabilizerLookup.HasComponent(entityB);

            if (!isBodyAPlane || !isBodyBPlane)
                return;
            
            var planeStabilizerEntityA = PlaneStabilizerLookup.GetRefRW(entityA);
            var planeStabilizerEntityB = PlaneStabilizerLookup.GetRefRW(entityB);

            // Despawn planes
            ECB.AddComponent(planeStabilizerEntityA.ValueRW.GuideEntity, new ShouldDespawnTag());
            ECB.AddComponent(planeStabilizerEntityB.ValueRW.GuideEntity, new ShouldDespawnTag());

            // Spawn explosion
            var posA = LocalTransformLookup[entityA].Position;
            var posB = LocalTransformLookup[entityB].Position;
            var collisionPoint = (posA + posB) * 0.5f;

            var explosionEntity = ECB.Instantiate(Explosion);
            ECB.AddComponent(explosionEntity, new ExplosionComponent { Startpoint = Elapsed });
            ECB.SetComponent(explosionEntity,
                LocalTransform.FromPosition(collisionPoint));
        }
    }
}