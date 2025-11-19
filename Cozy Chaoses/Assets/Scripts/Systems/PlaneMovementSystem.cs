using System;
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
        // state.RequireForUpdate<PlanetComponent>();
        // state.RequireForUpdate<ConfigComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        // var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var deltaTime = SystemAPI.Time.DeltaTime;

        state.Dependency = new MovePlanes
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            // Planet = planet
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
// [WithAll(typeof(PlaneComponent))]
[WithNone(typeof(ShouldDespawnComponent))]
public partial struct MovePlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    // public PlanetComponent Planet;

    public void Execute(ref LocalTransform transform, ref PlanePathComponent planePath)
    {
        planePath.ElapsedTime += DeltaTime;
        
        // Normalized time (0 to 1)
        float t = math.clamp(planePath.ElapsedTime / planePath.Duration, 0f, 1f);

        float3 newPos = float3.zero;
        float3 forwardDir = math.normalize(planePath.EndPoint - planePath.StartPoint);
        float3 rightDir = math.cross(forwardDir, math.up());
        float3 upDir = math.cross(rightDir, forwardDir); // Or just math.up() depending on preference

        switch (planePath.Shape)
        {
            case PathShape.Linear:
                newPos = math.lerp(planePath.StartPoint, planePath.EndPoint, t);
                break;

            case PathShape.SineWave:
                // Linear movement forward + Sine movement Up
                float3 linearPos = math.lerp(planePath.StartPoint, planePath.EndPoint, t);
                float sineOffset = math.sin(t * math.PI * 2 * planePath.Frequency) * planePath.Amplitude;
                newPos = linearPos + (upDir * sineOffset);
                break;
            //
            // case PathShape.Sigmoid:
            //     // Sigmoid math: 1 / (1 + e^-x). We remap t from 0..1 to -6..6 for the curve
            //     float k = -6f + (t * 12f); 
            //     float sig = 1f / (1f + math.exp(-k));
            //     newPos = math.lerp(path.StartPoint, path.EndPoint, sig);
            //     break;
            //
            // case PathShape.Curve:
            //     // Quadratic Bezier: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
            //     float u = 1 - t;
            //     newPos = (u * u * path.StartPoint) + (2 * u * t * path.ControlPoint) + (t * t * path.EndPoint);
            //     break;
        }

        if (math.distancesq(newPos, transform.Position) > 0.0001f)
        {
            float3 direction = math.normalize(newPos - transform.Position);
            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
        }

        transform.Position = newPos;
    }
}