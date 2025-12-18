using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct PlanetSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<ConfigComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Disable in first update, so it updates only once
        state.Enabled = false;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var config = SystemAPI.GetSingleton<ConfigComponent>();

        switch (config.ExecutionMode)
        {
            case ExecutionMode.Main:
                foreach (var configComponent in SystemAPI.Query<RefRO<ConfigComponent>>())
                {
                    var planetEntity = ecb.Instantiate(configComponent.ValueRO.PlanetPrefab);

                    var sphereCenter = new float3(0, 0, 0);
                    var sphereRadius = configComponent.ValueRO.PlanetRadius;
                    var transform = LocalTransform.FromPosition(sphereCenter).ApplyScale(sphereRadius * 2);
                    ecb.AddComponent(planetEntity, transform);
                    ecb.AddComponent(planetEntity, new PlanetComponent
                    {
                        Radius = sphereRadius,
                        Center = sphereCenter
                    });
                }
                break;

            case ExecutionMode.Schedule:
                state.Dependency = new SpawnPlanetSingle
                {
                    ECB = ecb
                }.Schedule(state.Dependency);
                break;

            case ExecutionMode.ScheduleParallel:
                state.Dependency = new SpawnPlanetParallel
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency);
                break;
        }
    }
}

[BurstCompile]
public partial struct SpawnPlanetSingle : IJobEntity
{
    public EntityCommandBuffer ECB;

    public void Execute(in ConfigComponent configComponent)
    {
        var planetEntity = ECB.Instantiate(configComponent.PlanetPrefab);

        var sphereCenter = new float3(0, 0, 0);
        var sphereRadius = configComponent.PlanetRadius;
        var transform = LocalTransform.FromPosition(sphereCenter).ApplyScale(sphereRadius * 2);
        ECB.AddComponent(planetEntity, transform);
        ECB.AddComponent(planetEntity, new PlanetComponent
        {
            Radius = sphereRadius,
            Center = sphereCenter
        });
    }
}

[BurstCompile]
public partial struct SpawnPlanetParallel : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute([ChunkIndexInQuery] int chunkIndex, in ConfigComponent configComponent)
    {
        var planetEntity = ECB.Instantiate(chunkIndex, configComponent.PlanetPrefab);

        var sphereCenter = new float3(0, 0, 0);
        var sphereRadius = configComponent.PlanetRadius;
        var transform = LocalTransform.FromPosition(sphereCenter).ApplyScale(sphereRadius * 2);
        ECB.AddComponent(chunkIndex, planetEntity, transform);
        ECB.AddComponent(chunkIndex, planetEntity, new PlanetComponent
        {
            Radius = sphereRadius,
            Center = sphereCenter
        });
    }
}
