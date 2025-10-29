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
    internal ComponentDataHandles MComponentDataHandles;

    internal struct ComponentDataHandles
    {
        public ComponentLookup<PlaneComponent> PlaneComponentLookup;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityData;

        public ComponentDataHandles(ref SystemState systemState)
        {
            PlaneComponentLookup = systemState.GetComponentLookup<PlaneComponent>(true);
            PhysicsVelocityData = systemState.GetComponentLookup<PhysicsVelocity>(false);
        }

        public void Update(ref SystemState systemState)
        {
            PlaneComponentLookup.Update(ref systemState);
            PhysicsVelocityData.Update(ref systemState);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<PlaneComponent>()));
        MComponentDataHandles = new ComponentDataHandles(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        MComponentDataHandles.Update(ref state);

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        // var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
        //     .CreateCommandBuffer(state.WorldUnmanaged);

        state.Dependency = new PlaneCollisionJob
        {
            // ECB = ecb,
            PlaneComponentLookup = MComponentDataHandles.PlaneComponentLookup,
            PhysicsVelocityData = MComponentDataHandles.PhysicsVelocityData,
        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    struct PlaneCollisionJob : ICollisionEventsJob
    {
        // public EntityCommandBuffer ECB;
        [ReadOnly] public ComponentLookup<PlaneComponent> PlaneComponentLookup;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityData;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            bool isBodyADynamic = PhysicsVelocityData.HasComponent(entityA);
            bool isBodyBDynamic = PhysicsVelocityData.HasComponent(entityB);
            
            bool isBodyAPlane = PlaneComponentLookup.HasComponent(entityA);
            bool isBodyBPlane = PlaneComponentLookup.HasComponent(entityB);
            
            // Debug.Log("Collision detected!");

            if (isBodyAPlane && isBodyBDynamic)
            {
                var velocityComponent = PhysicsVelocityData[entityB];
                velocityComponent.Linear = new Unity.Mathematics.float3(0, 10f, 0);
                PhysicsVelocityData[entityB] = velocityComponent;
            }

            if (isBodyBPlane && isBodyADynamic)
            {
                var velocityComponent = PhysicsVelocityData[entityA];
                velocityComponent.Linear = new Unity.Mathematics.float3(0, 10f, 0);
                PhysicsVelocityData[entityA] = velocityComponent;
            }
        }
    }
}