using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting;
using Random = Unity.Mathematics.Random;

public partial struct AirportSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<ConfigComponent>();
        // Ensure a planet exists before we try to spawn airports that depend on it
        state.RequireForUpdate<PlanetComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Disable in first update, so it updates only once
        state.Enabled = false;
        
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        // Read the spawned planet from the world and pass it into the job
        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        
        state.Dependency = new SpawnAirports
        {
            ECB = ecb,
            Planet = planet
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct SpawnAirports : IJobEntity
{
    public EntityCommandBuffer ECB;
    public PlanetComponent Planet;
    public void Execute(in ConfigComponent configComponent)
    {
        // TODO: Should probably be configurable
        var sphereCenter = Planet.Center;
        var sphereRadius = Planet.Radius;

        var random = new Random(72);
        
        for (var i = 0; i < configComponent.AirportCount; i++)
        {
            var airportEntity = ECB.Instantiate(configComponent.AirportPrefab);
            
            var color = new URPMaterialPropertyBaseColor
            {
                Value = Utils.RandomColor(ref random)
            };
            ECB.SetComponent(airportEntity, color);
            
            var newPos = new float3(
                random.NextFloat(-100f, 100f),
                random.NextFloat(-100f, 100f),
                random.NextFloat(-100f, 100f)
            );
            var newSurfacePos = sphereCenter + math.normalize(newPos - sphereCenter) * sphereRadius;
            var scale = sphereRadius * 0.05f; // Relative to planet size
            var transform = LocalTransform.FromPosition(newSurfacePos).ApplyScale(scale);
            ECB.AddComponent(airportEntity, transform);
        }
    }
}