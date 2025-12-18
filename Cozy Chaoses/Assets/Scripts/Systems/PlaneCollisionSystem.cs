using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
internal partial struct PlaneCollisionSystem : ISystem
{
    private ComponentLookup<PlaneStabilizerComponent> _planeStabilizerLookup;
    private ComponentLookup<JustSpawnedTag> _justSpawnedLookup;
    private ComponentLookup<LocalTransform> _localTransformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<ConfigComponent>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<PlaneStabilizerComponent>()));

        _planeStabilizerLookup = state.GetComponentLookup<PlaneStabilizerComponent>();
        _justSpawnedLookup = state.GetComponentLookup<JustSpawnedTag>(true);
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _planeStabilizerLookup.Update(ref state);
        _justSpawnedLookup.Update(ref state);
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
            EnableCollisions = config.EnableDespawnOnCollision,
            EnableExplosions = config.EnableExplosionsOnCollision,
            Explosion = config.ExplosionPrefab,
            PlaneStabilizerLookup = _planeStabilizerLookup,
            JustSpawnedLookup = _justSpawnedLookup,
            LocalTransformLookup = _localTransformLookup
        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    [WithAll(typeof(AlertComponent))]
    private struct PlaneCollisionJob : ICollisionEventsJob
    {
        public EntityCommandBuffer ECB;
        public float Elapsed;
        public Entity Explosion;
        public bool EnableCollisions;
        public bool EnableExplosions;
        public ComponentLookup<PlaneStabilizerComponent> PlaneStabilizerLookup;
        [ReadOnly] public ComponentLookup<JustSpawnedTag> JustSpawnedLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

        public void Execute(CollisionEvent collisionEvent)
        {
            var entityA = collisionEvent.EntityA;
            var entityB = collisionEvent.EntityB;

            var isBodyAPlane = PlaneStabilizerLookup.HasComponent(entityA);
            var isBodyBPlane = PlaneStabilizerLookup.HasComponent(entityB);
            
            var isBodyAJustSpawned = JustSpawnedLookup.HasComponent(entityA);
            var isBodyBJustSpawned = JustSpawnedLookup.HasComponent(entityB);

            if (!isBodyAPlane || !isBodyBPlane || isBodyAJustSpawned || isBodyBJustSpawned)
                return;

            var planeStabilizerEntityA = PlaneStabilizerLookup.GetRefRW(entityA);
            var planeStabilizerEntityB = PlaneStabilizerLookup.GetRefRW(entityB);

            // Despawn planes
            if (EnableCollisions)
            {
                ECB.AddComponent(planeStabilizerEntityA.ValueRW.GuideEntity, new ShouldDespawnTag());
                ECB.AddComponent(planeStabilizerEntityB.ValueRW.GuideEntity, new ShouldDespawnTag());
            }

            if (EnableExplosions)
            {
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
}