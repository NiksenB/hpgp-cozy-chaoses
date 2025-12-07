using Components;
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
    private ComponentLookup<PlaneStabilizerComponent> _planeStabilizerLookup;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<PlaneStabilizerComponent>()));
        _planeStabilizerLookup = state.GetComponentLookup<PlaneStabilizerComponent>(false); 
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _planeStabilizerLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        state.Dependency = new TcasCollisionJob
        {
            PlaneStabilizerLookup = _planeStabilizerLookup,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            ECB = ecb,
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
}

[BurstCompile]
public struct TcasCollisionJob : ITriggerEventsJob
{
    public ComponentLookup<PlaneStabilizerComponent> PlaneStabilizerLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    public EntityCommandBuffer ECB;
    
    public void Execute(TriggerEvent triggerEvent)
    {
        var entityA = triggerEvent.EntityA;
        var entityB = triggerEvent.EntityB;

        if (PlaneStabilizerLookup.HasComponent(entityA) &&
            PlaneStabilizerLookup.HasComponent(entityB))
        {
            float3 posA = TransformLookup[entityA].Position;
            float3 posB = TransformLookup[entityB].Position;

            var planeStabilizerEntityA = PlaneStabilizerLookup.GetRefRW(entityA);
            var planeStabilizerEntityB = PlaneStabilizerLookup.GetRefRW(entityB);
            
            ECB.AddComponent(planeStabilizerEntityA.ValueRW.GuideEntity, new AlertComponent { EntityPos = posB });
            ECB.AddComponent(planeStabilizerEntityB.ValueRW.GuideEntity, new AlertComponent { EntityPos = posA });
        }
    }
}