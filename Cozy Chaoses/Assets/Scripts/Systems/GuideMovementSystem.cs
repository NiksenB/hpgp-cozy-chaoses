using Components;
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

        switch (config.ExecutionMode)
        {
            case ExecutionMode.Main:
                // Process alert guides
                foreach (var (transform, guidePath, alert, entity) in SystemAPI
                             .Query<RefRW<LocalTransform>, RefRW<GuidePathComponent>, RefRW<AlertComponent>>()
                             .WithAll<AlertComponent>()
                             .WithNone<ShouldDespawnTag>()
                             .WithNone<JustSpawnedMustBeMoved>()
                             .WithEntityAccess())
                {
                    var speed = config.PlaneSpeed;

                    if (math.distance(transform.ValueRO.Position, guidePath.ValueRO.EndPoint) <= 1f)
                    {
                        ecb.AddComponent(entity, new ShouldDespawnTag());
                        continue;
                    }

                    if (math.length(transform.ValueRO.Position - planet.Radius) < 0)
                        Debug.Log("I am an underground plane! Ouch!");

                    var pos = transform.ValueRO.Position;

                    var toCenter = math.normalize(pos);
                    var toTar = math.normalize(alert.ValueRO.EntityPos - pos);

                    var up = math.normalize(pos);
                    var currentForward = math.normalize(math.forward(transform.ValueRO.Rotation.value));

                    float3 idealDir;
                    var dot = math.dot(toTar, currentForward);

                    float3 toDest;

                    if (dot == 0 || dot > 0.95f)
                    {
                        var right30 = quaternion.AxisAngle(up, math.radians(30f));
                        idealDir = math.mul(right30, currentForward);
                    }
                    else
                    {
                        idealDir = math.normalize(-toTar);
                    }

                    var dirDiff = math.dot(currentForward, idealDir);
                    if (dirDiff < 0.99f)
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

                    var surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));

                    var rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
                    var rotationAngle = deltaTime * speed / (planet.Radius + guidePath.ValueRO.TargetAltitude);

                    var rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
                    var newDirection = math.mul(rot, toCenter);
                    var newSurfacePos = newDirection * math.length(transform.ValueRO.Position);

                    var newTransform = transform.ValueRO;
                    newTransform.Position = newSurfacePos;

                    var forward = math.normalize(newSurfacePos - pos);
                    var newRotation = quaternion.LookRotation(forward, up);
                    newTransform.Rotation = newRotation;

                    transform.ValueRW = newTransform;

                    if (math.length(alert.ValueRO.EntityPos - transform.ValueRO.Position) > 10f)
                        ecb.RemoveComponent<AlertComponent>(entity);
                }

                // Process normal guides
                foreach (var (transform, guidePath, entity) in SystemAPI
                             .Query<RefRW<LocalTransform>, RefRW<GuidePathComponent>>()
                             .WithNone<AlertComponent>()
                             .WithNone<ShouldDespawnTag>()
                             .WithNone<JustSpawnedMustBeMoved>()
                             .WithEntityAccess())
                {
                    if (math.distance(transform.ValueRO.Position, guidePath.ValueRO.EndPoint) <= 1f)
                    {
                        ecb.AddComponent(entity, new ShouldDespawnTag());
                        continue;
                    }

                    if (math.length(transform.ValueRO.Position - planet.Radius) < 0)
                        Debug.Log("I am an underground plane!");

                    var next = NavigationCalculator.CalculateNext(transform.ValueRO, guidePath.ValueRO,
                        config.PlaneSpeed, planet.Radius, deltaTime);
                    var newTransform = transform.ValueRO;
                    newTransform.Position = next.Item1;
                    newTransform.Rotation = next.Item2;
                    transform.ValueRW = newTransform;
                }

                break;

            case ExecutionMode.Schedule:
                var alertMoves = new MoveGuidesAvoidCollisionJobSingle
                {
                    ECB = ecb,
                    DeltaTime = deltaTime,
                    PlaneSpeed = config.PlaneSpeed,
                    Planet = planet
                }.Schedule(state.Dependency);

                state.Dependency = new MoveGuidesTowardsEnpointJobSingle
                {
                    ECB = ecb,
                    DeltaTime = deltaTime,
                    PlaneSpeed = config.PlaneSpeed,
                    Planet = planet
                }.Schedule(alertMoves);
                break;

            case ExecutionMode.ScheduleParallel:
                var alertMovesParallel = new MoveGuidesAvoidCollisionJobParallel
                {
                    ECB = ecb.AsParallelWriter(),
                    DeltaTime = deltaTime,
                    PlaneSpeed = config.PlaneSpeed,
                    Planet = planet
                }.ScheduleParallel(state.Dependency);
                alertMovesParallel.Complete();

                state.Dependency = new MoveGuidesTowardsEnpointJobParallel
                {
                    ECB = ecb.AsParallelWriter(),
                    DeltaTime = deltaTime,
                    PlaneSpeed = config.PlaneSpeed,
                    Planet = planet
                }.ScheduleParallel(alertMovesParallel);
                break;
        }
    }
}

// Single-threaded scheduled versions
[BurstCompile]
[WithNone(typeof(AlertComponent))]
[WithNone(typeof(ShouldDespawnTag))]
[WithNone(typeof(JustSpawnedMustBeMoved))]
public partial struct MoveGuidesTowardsEnpointJobSingle : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public float PlaneSpeed;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref GuidePathComponent guidePath)
    {
        if (math.distance(transform.Position, guidePath.EndPoint) <= 1f)
        {
            ECB.AddComponent<ShouldDespawnTag>(entity);
            return;
        }

        if (math.length(transform.Position - Planet.Radius) < 0) Debug.Log("I am an underground plane!");

        var next = NavigationCalculator.CalculateNext(transform, guidePath, PlaneSpeed, Planet.Radius, DeltaTime);
        transform.Position = next.Item1;
        transform.Rotation = next.Item2;
    }
}

[BurstCompile]
[WithAll(typeof(AlertComponent))]
[WithNone(typeof(ShouldDespawnTag))]
[WithNone(typeof(JustSpawnedMustBeMoved))]
public partial struct MoveGuidesAvoidCollisionJobSingle : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public float PlaneSpeed;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref GuidePathComponent guidePath,
        ref AlertComponent alert)
    {
        var speed = PlaneSpeed;

        if (math.distance(transform.Position, guidePath.EndPoint) <= 1f)
        {
            ECB.AddComponent<ShouldDespawnTag>(entity);
            return;
        }

        if (math.length(transform.Position - Planet.Radius) < 0) Debug.Log("I am an underground plane! Ouch!");

        var pos = transform.Position;

        var toCenter = math.normalize(pos);
        var toTar = math.normalize(alert.EntityPos - pos);

        var up = math.normalize(pos);
        var currentForward = math.normalize(math.forward(transform.Rotation.value));

        float3 idealDir;
        var dot = math.dot(toTar, currentForward);

        float3 toDest;

        if (dot == 0 || dot > 0.95f)
        {
            var right30 = quaternion.AxisAngle(up, math.radians(30f));
            idealDir = math.mul(right30, currentForward);
        }
        else
        {
            idealDir = math.normalize(-toTar);
        }

        var dirDiff = math.dot(currentForward, idealDir);
        if (dirDiff < 0.99f)
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

        var surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));

        var rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
        var rotationAngle = DeltaTime * speed / (Planet.Radius + guidePath.TargetAltitude);

        var rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
        var newDirection = math.mul(rot, toCenter);
        var newSurfacePos = newDirection * math.length(transform.Position);

        transform.Position = newSurfacePos;

        var forward = math.normalize(newSurfacePos - pos);
        var newRotation = quaternion.LookRotation(forward, up);
        transform.Rotation = newRotation;

        if (math.length(alert.EntityPos - transform.Position) > 10f) ECB.RemoveComponent<AlertComponent>(entity);
    }
}

// Parallel versions
[BurstCompile]
[WithNone(typeof(AlertComponent))]
[WithNone(typeof(ShouldDespawnTag))]
[WithNone(typeof(JustSpawnedMustBeMoved))]
public partial struct MoveGuidesTowardsEnpointJobParallel : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float DeltaTime;
    public float PlaneSpeed;
    public PlanetComponent Planet;

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref LocalTransform transform,
        ref GuidePathComponent guidePath)
    {
        if (math.distance(transform.Position, guidePath.EndPoint) <= 1f)
        {
            ECB.AddComponent<ShouldDespawnTag>(chunkIndex, entity);
            return;
        }

        if (math.length(transform.Position - Planet.Radius) < 0) Debug.Log("I am an underground plane!");

        var next = NavigationCalculator.CalculateNext(transform, guidePath, PlaneSpeed, Planet.Radius, DeltaTime);
        transform.Position = next.Item1;
        transform.Rotation = next.Item2;
    }
}

[BurstCompile]
[WithAll(typeof(AlertComponent))]
[WithNone(typeof(ShouldDespawnTag))]
[WithNone(typeof(JustSpawnedMustBeMoved))]
public partial struct MoveGuidesAvoidCollisionJobParallel : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float DeltaTime;
    public float PlaneSpeed;
    public PlanetComponent Planet;

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref LocalTransform transform,
        ref GuidePathComponent guidePath,
        ref AlertComponent alert)
    {
        var speed = PlaneSpeed;

        if (math.distance(transform.Position, guidePath.EndPoint) <= 1f)
        {
            ECB.AddComponent<ShouldDespawnTag>(chunkIndex, entity);
            return;
        }

        if (math.length(transform.Position - Planet.Radius) < 0) Debug.Log("I am an underground plane! Ouch!");

        var pos = transform.Position;

        var toCenter = math.normalize(pos);
        var toTar = math.normalize(alert.EntityPos - pos);

        var up = math.normalize(pos);
        var currentForward = math.normalize(math.forward(transform.Rotation.value));

        float3 idealDir;
        var dot = math.dot(toTar, currentForward);

        float3 toDest;

        if (dot == 0 || dot > 0.95f)
        {
            var right30 = quaternion.AxisAngle(up, math.radians(30f));
            idealDir = math.mul(right30, currentForward);
        }
        else
        {
            idealDir = math.normalize(-toTar);
        }

        var dirDiff = math.dot(currentForward, idealDir);
        if (dirDiff < 0.99f)
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

        var surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));

        var rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
        var rotationAngle = DeltaTime * speed / (Planet.Radius + guidePath.TargetAltitude);

        var rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
        var newDirection = math.mul(rot, toCenter);
        var newSurfacePos = newDirection * math.length(transform.Position);

        transform.Position = newSurfacePos;

        var forward = math.normalize(newSurfacePos - pos);
        var newRotation = quaternion.LookRotation(forward, up);
        transform.Rotation = newRotation;

        if (math.length(alert.EntityPos - transform.Position) > 10f)
            ECB.RemoveComponent<AlertComponent>(chunkIndex, entity);
    }
}
