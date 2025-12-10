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
        state.RequireForUpdate<ConfigComponent>();
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var config = SystemAPI.GetSingleton<ConfigComponent>();
        var deltaTime = SystemAPI.Time.DeltaTime;

        var alertMoves = new MoveGuidesAvoidCollisionJob
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            PlaneSpeed = config.PlaneSpeed,
            Planet = planet
        }.Schedule(state.Dependency);

        state.Dependency = new MoveGuidesTowardsEnpointJob
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            PlaneSpeed = config.PlaneSpeed,
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
    public float PlaneSpeed;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref GuidePathComponent guidePath)
    {
        // Despawn if landed 
        if (math.distance(transform.Position, guidePath.EndPoint) <= 1f)
        {
            ECB.AddComponent(entity, new ShouldDespawnTag());
            return;
        }

        // Crash if colliding with ground?
        if (math.length(transform.Position - Planet.Radius) < 0) Debug.Log("I am an underground plane!");

        var next = NavigationCalculator.CalculateNext(transform, guidePath, PlaneSpeed, Planet.Radius, DeltaTime);
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
    public float PlaneSpeed;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref GuidePathComponent guidePath,
        ref AlertComponent alert)
    {
        var speed = PlaneSpeed;

        // Despawn if landed 
        if (math.distance(transform.Position, guidePath.EndPoint) <= 1f)
        {
            ECB.AddComponent(entity, new ShouldDespawnTag());
            return;
        }

        // Crash if colliding with ground?
        if (math.length(transform.Position - Planet.Radius) < 0) Debug.Log("I am an underground plane! Ouch!");

        var pos = transform.Position;

        var toCenter = math.normalize(pos);
        var toTar = math.normalize(alert.EntityPos - pos);

        var up = math.normalize(pos);
        var currentForward = math.normalize(math.forward(transform.Rotation.value));

        float3 idealDir;
        var dot = math.dot(toTar, currentForward);

        float3 toDest;

        // If planes are head-on or nearly head-on, rotate until 30° to the right
        if (dot == 0 || dot > 0.95f)
        {
            var right30 = quaternion.AxisAngle(up, math.radians(30f));
            idealDir = math.mul(right30, currentForward);
        }
        else
        {
            idealDir = math.normalize(-toTar);
        }

        // If we haven't turned enough yet
        var dirDiff = math.dot(currentForward, idealDir);
        if (dirDiff < 0.99f) // not yet close to ideal
        {
            var cross = math.cross(currentForward, idealDir);
            var sign = math.sign(math.dot(cross, up));
            var smallTurn = quaternion.AxisAngle(up, math.radians(0.5f) * sign);
            toDest = math.mul(smallTurn, currentForward);
        }
        else
        {
            toDest = idealDir;
        }

        // NEW POSITION
        var surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));

        var rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
        var rotationAngle = DeltaTime * speed / (Planet.Radius + guidePath.TargetAltitude);

        var rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
        var newDirection = math.mul(rot, toCenter);
        var newSurfacePos = newDirection * math.length(transform.Position);

        transform.Position = newSurfacePos;

        // NEW ROTATION
        var forward = math.normalize(newSurfacePos - pos);
        var newRotation = quaternion.LookRotation(forward, up);
        transform.Rotation = newRotation;

        // Check if still overlapping
        if (math.length(alert.EntityPos - transform.Position) > 10f) ECB.RemoveComponent<AlertComponent>(entity);
    }
}