using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct TcasCollisionDetectionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        state.Dependency = new TcasCollisionJob
        {
            PlaneLookup = SystemAPI.GetComponentLookup<PlaneComponent>(true),
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            ECB = ecb,
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
}

[BurstCompile]
public struct TcasCollisionJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<PlaneComponent> PlaneLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    public EntityCommandBuffer ECB;
    
    public void Execute(TriggerEvent triggerEvent)
    {
        var entityA = triggerEvent.EntityA;
        var entityB = triggerEvent.EntityB;

        if (PlaneLookup.HasComponent(entityA) &&
            PlaneLookup.HasComponent(entityB))
        {
            float3 posA = TransformLookup[entityA].Position;
            float3 posB = TransformLookup[entityB].Position;

            ECB.AddComponent(entityA, new AlertComponent { EntityPos = posB });
            ECB.AddComponent(entityB, new AlertComponent { EntityPos = posA });
        }
    }
}