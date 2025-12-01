using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
partial struct PlaneCollisionSystem : ISystem
{
    private ComponentLookup<PlaneTag> _planeTagLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<PlaneTag>()));
        _planeTagLookup = state.GetComponentLookup<PlaneTag>(true); 
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _planeTagLookup.Update(ref state);

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        state.Dependency = new PlaneCollisionJob
        {
            ECB = ecb,
            PlaneTagLookup = _planeTagLookup,
        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [WithAll(typeof(AlertComponent))]
    struct PlaneCollisionJob : ICollisionEventsJob
    {
        public EntityCommandBuffer ECB;
        [ReadOnly] public ComponentLookup<PlaneTag> PlaneTagLookup;

        public void Execute(CollisionEvent collisionEvent)
        {
            var entityA = collisionEvent.EntityA;
            var entityB = collisionEvent.EntityB;

            var isBodyAPlane = PlaneTagLookup.HasComponent(entityA);
            var isBodyBPlane = PlaneTagLookup.HasComponent(entityB);
            
            if (!isBodyAPlane || !isBodyBPlane)
                return;
            
            // Both entities are planes, handle collision
            ECB.AddComponent(entityA, new ShouldDespawnTag());
            ECB.AddComponent(entityB, new ShouldDespawnTag());
        }
    }
}