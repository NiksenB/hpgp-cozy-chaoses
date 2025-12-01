using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.VisualScripting;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
partial struct PlaneCollisionSystem : ISystem
{
    private ComponentLookup<PlaneStabilizerComponent> _planeStabilizerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<PlaneStabilizerComponent>()));
        _planeStabilizerLookup = state.GetComponentLookup<PlaneStabilizerComponent>(false); 
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _planeStabilizerLookup.Update(ref state);

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        state.Dependency = new PlaneCollisionJob
        {
            ECB = ecb,
            PlaneStabilizerLookup = _planeStabilizerLookup,
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
        public ComponentLookup<PlaneStabilizerComponent> PlaneStabilizerLookup;

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
            
            ECB.AddComponent(planeStabilizerEntityA.ValueRW.GuideEntity, new ShouldDespawnTag());
            ECB.AddComponent(planeStabilizerEntityB.ValueRW.GuideEntity, new ShouldDespawnTag());
        }
    }
}