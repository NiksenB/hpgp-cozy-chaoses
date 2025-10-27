using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting;
using Random = Unity.Mathematics.Random;

public partial struct PlanetSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Disable in first update, so it updates only once
        state.Enabled = false;
        
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        state.Dependency = new SpawnPlanet
        {
            ECB = ecb
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct SpawnPlanet : IJobEntity
{
    public EntityCommandBuffer ECB;
    public void Execute(in Config config)
    {
        var planetEntity = ECB.Instantiate(config.PlanetPrefab);
        
        // TODO: Should probably be configurable
        var sphereCenter = new float3(0, 0, 0);
        var sphereRadius = config.PlanetRadius;
        var transform = LocalTransform.FromPosition(sphereCenter).ApplyScale(sphereRadius*2); // Assume unit sphere
        ECB.AddComponent(planetEntity, transform);
    }
}