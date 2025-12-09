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

    public void Execute(in ConfigComponent configComponent)
    {
        var planetEntity = ECB.Instantiate(configComponent.PlanetPrefab);

        // TODO: Should probably be configurable
        var sphereCenter = new float3(0, 0, 0);
        var sphereRadius = configComponent.PlanetRadius;
        var transform = LocalTransform.FromPosition(sphereCenter).ApplyScale(sphereRadius * 2); // Assume unit sphere
        ECB.AddComponent(planetEntity, transform);
        ECB.AddComponent(planetEntity, new PlanetComponent
        {
            Radius = sphereRadius,
            Center = sphereCenter
        });
    }
}