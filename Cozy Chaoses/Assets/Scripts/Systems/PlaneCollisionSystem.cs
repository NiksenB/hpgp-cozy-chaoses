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
    private ComponentLookup<PlaneComponent> _planeComponentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<PlaneComponent>()));
        _planeComponentLookup = state.GetComponentLookup<PlaneComponent>(true); 
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _planeComponentLookup.Update(ref state);

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        state.Dependency = new PlaneCollisionJob
        {
            ECB = ecb,
            PlaneComponentLookup = _planeComponentLookup,
        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    struct PlaneCollisionJob : ITriggerEventsJob
    {
        public EntityCommandBuffer ECB;
        [ReadOnly] public ComponentLookup<PlaneComponent> PlaneComponentLookup;

        public void Execute(TriggerEvent collisionEvent)
        {
            var entityA = collisionEvent.EntityA;
            var entityB = collisionEvent.EntityB;

            var isBodyAPlane = PlaneComponentLookup.HasComponent(entityA);
            var isBodyBPlane = PlaneComponentLookup.HasComponent(entityB);
            
            if (!isBodyAPlane || !isBodyBPlane)
                return;
            
            // Both entities are planes, handle collision
            ECB.AddComponent(entityA, new ShouldDespawnComponent());
        }
    }
}