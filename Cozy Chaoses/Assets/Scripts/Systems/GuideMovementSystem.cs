using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct GuideMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var deltaTime = SystemAPI.Time.DeltaTime;

        var alertMoves = new MoveGuidesAvoidCollisionJob
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(state.Dependency);
        
        state.Dependency = new MoveGuidesTowardsEnpointJob
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(alertMoves);
    }
}

[BurstCompile]
[WithNone(typeof(AlertComponent))]
[WithNone(typeof(ShouldDespawnTag))]
public partial struct MoveGuidesTowardsEnpointJob : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref GuidePathComponent guidePath)
    {
        // Despawn if landed or directly above airport
        float3 currentDir = math.normalize(transform.Position); 
        float3 destDir = math.normalize(guidePath.EndPoint);
        
        if (math.dot(currentDir, destDir) > 0.999f)
        {
            ECB.AddComponent(entity, new ShouldDespawnTag());
            return;
        }
        
        var next = NavigationCalculator.CalculateNext(transform, guidePath, Planet.Radius, DeltaTime);
        transform.Position = next.Item1;
        transform.Rotation = next.Item2;
    }
}


[BurstCompile]
[WithAll(typeof(AlertComponent))]
[WithNone(typeof(ShouldDespawnTag))]
public partial struct MoveGuidesAvoidCollisionJob : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref GuidePathComponent guidePath, ref AlertComponent alert)
    {
        Debug.Log("NOT Normal movement");

        // Despawn if landed or directly above airport
        float3 currentDir = math.normalize(transform.Position); 
        float3 destDir = math.normalize(guidePath.EndPoint);
        
        if (math.dot(currentDir, destDir) > 0.999f)
        {
            ECB.AddComponent(entity, new ShouldDespawnTag());
            return;
        }

        // movement logic here
        // float3 nextPos = NavigationCalculator.CalculateNext(guidePath, transform.Position, transform.Rotation);
        // float3 direction = math.normalize(nextPos - transform.Position);
        // float3 surfaceUp = math.normalize(nextPos);
        // transform.Rotation = quaternion.LookRotationSafe(direction, surfaceUp);
        // transform.Position = nextPos;
        var next = NavigationCalculator.CalculateNext(transform, guidePath, Planet.Radius, DeltaTime);
        transform.Position = next.Item1;
        transform.Rotation = next.Item2;
        
        // Check if still overlapping
        if (math.length(alert.EntityPos - transform.Position) > 10f)
        {
            ECB.RemoveComponent<AlertComponent>(entity);
        }
    }
}