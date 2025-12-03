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
        // Despawn if landed or directly above airport
        float3 currentDir = math.normalize(transform.Position); 
        float3 destDir = math.normalize(guidePath.EndPoint);
        float speed = 5.0f;
        
        if (math.dot(currentDir, destDir) > 0.999f)
        {
            ECB.AddComponent(entity, new ShouldDespawnTag());
            return;
        }

        float3 pos = transform.Position;

        float3 toCenter = math.normalize(pos);
        var toTar = math.normalize(alert.EntityPos - pos);
        
        float3 up = math.normalize(pos);
        float3 currentForward = math.normalize(math.forward(transform.Rotation.value));

        float3 idealDir = float3.zero;
        var dot = math.dot(toTar, currentForward);
        
        float3 toDest;
        
        // If planes are head-on or nearly head-on, rotate until 30° to the right
        if (dot == 0 || dot > 0.7f)
        {
            quaternion right30 = quaternion.AxisAngle(up, math.radians(30f));
            idealDir = math.mul(right30, currentForward);
        }
        // target is coming from less a dangerous angle, small adjustment to course
        else if (dot > 0.2f)
        {
            idealDir = math.normalize(-toTar);
        }
        
        // If we haven't turned enough yet
        float dirDiff = math.dot(currentForward, idealDir);
        if (dirDiff < 0.99f) // not yet close to ideal
        {
            float3 cross = math.cross(currentForward, idealDir);
            float sign = math.sign(math.dot(cross, up));
            quaternion smallTurn = quaternion.AxisAngle(up, math.radians(2f) * sign);
            toDest = math.mul(smallTurn, currentForward);
        }
        else
        {
            toDest = idealDir;
        }

        // NEW POSITION
        float3 surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));

        float3 rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
        float rotationAngle = DeltaTime * speed / (Planet.Radius + guidePath.TargetHeight);

        quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
        float3 newDirection = math.mul(rot, toCenter);
        float3 newSurfacePos = newDirection * (Planet.Radius + guidePath.TargetHeight);

        transform.Position = newSurfacePos;

        // NEW ROTATION
        float3 forward = math.normalize(newSurfacePos - pos);
        quaternion newRotation = quaternion.LookRotation(forward, up);
        transform.Rotation = newRotation;
        
        // Check if still overlapping
        if (math.length(alert.EntityPos - transform.Position) > 10f)
        {
            ECB.RemoveComponent<AlertComponent>(entity);
        }
    }
}