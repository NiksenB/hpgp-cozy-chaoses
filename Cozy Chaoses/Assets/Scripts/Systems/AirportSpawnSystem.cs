using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial struct AirportSpawnSystem : ISystem
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
        // Disable in first update, so it updates only once
        state.Enabled = false;
        
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var positions = new NativeArray<float3>(SystemAPI.GetSingleton<ConfigComponent>().AirportCount, Allocator.TempJob);
        
        state.Dependency = new SpawnAirports
        {
            ECB = ecb,
            Planet = planet,
            Positions = positions
        }.Schedule(state.Dependency);
        state.Dependency.Complete();
        positions.Dispose();
    }
}

[BurstCompile]
public partial struct SpawnAirports : IJobEntity
{
    public EntityCommandBuffer ECB;
    public PlanetComponent Planet;
    public NativeArray<float3> Positions;
    private void Execute(in ConfigComponent configComponent)
    {
        var sphereCenter = Planet.Center;
        var sphereRadius = Planet.Radius;

        var random = new Random(72);
        var positions = GeneratePoints(configComponent.AirportCount, Positions, sphereRadius);
        
        for (var i = 0; i < configComponent.AirportCount; i++)
        {
            var airportEntity = ECB.Instantiate(configComponent.AirportPrefab);
            
            var color = new URPMaterialPropertyBaseColor
            {
                Value = Utils.RandomColor(ref random)
            };
            ECB.SetComponent(airportEntity, color);
            
            ECB.SetComponent(airportEntity, new AirportComponent { NextPlaneSpawnTime = random.NextDouble(2d, 10d) });
            
            var up = math.normalize(positions[i] - sphereCenter);
            var rot = Quaternion.FromToRotation(Vector3.up, up);
            var pos = sphereCenter + up * sphereRadius;
            var scale = sphereRadius * 0.05f; // Relative to planet size
            
            var transform = LocalTransform.FromPositionRotationScale(pos, rot, scale);
            
            ECB.AddComponent(airportEntity, transform);
        }
    }
    
    // Below is from tutorial: https://devforum.roblox.com/t/generating-equidistant-points-on-a-sphere/874144
    private static NativeArray<float3> GeneratePoints(int n, NativeArray<float3> arr, float sphereRadius)
    {
        var goldenRatio = (1 + math.sqrt(5)) / 2;
        var angleIncrement = 2 * math.PI * goldenRatio;
    
        for (var i = 0; i < n; i++)
        {
            var distance = (float)i / n;
            var incline = math.acos(1 - 2 * distance);
            var azimuth = angleIncrement * i;
    
            var x = math.sin(incline) * math.cos(azimuth) * sphereRadius;
            var y = math.sin(incline) * math.sin(azimuth) * sphereRadius;
            var z = math.cos(incline) * sphereRadius;
        
            arr[i] = new float3(x, y, z);
        }
        return arr;
    }
}