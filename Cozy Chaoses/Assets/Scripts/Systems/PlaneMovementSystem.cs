using System;
using DefaultNamespace;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

public partial struct PlaneMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PlanetComponent>();
        // state.RequireForUpdate<ConfigComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var deltaTime = SystemAPI.Time.DeltaTime;

        state.Dependency = new MovePlanes
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(PlaneComponent))]
[WithNone(typeof(ShouldDespawnComponent))]
public partial struct MovePlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref PlanePathComponent planePath)
    {
        planePath.ElapsedTime += DeltaTime;
        
        // Normalized time (0 to 1)
        float t = math.clamp(planePath.ElapsedTime / planePath.Duration, 0f, 1f);
        
        // Placeholder for despawn behavior
        if (t >= 1f)
        {
            ECB.AddComponent(entity, new ShouldDespawnComponent());
            return;
        }

        float3 newPos = LineCalculator.Calculate(planePath, t);

        if (math.distancesq(newPos, transform.Position) > 0.0001f)
        {
            float3 direction = math.normalize(newPos - transform.Position);
            float3 surfaceUp = math.normalize(newPos);
            transform.Rotation = quaternion.LookRotationSafe(direction, surfaceUp);
        }

        transform.Position = newPos;
    }
    
}