using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph;
using Random = Unity.Mathematics.Random;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlaneSpawnSystem : ISystem
{
    private float timer;
    private Random random;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigComponent>();
        state.RequireForUpdate<PlanetComponent>();
        state.RequireForUpdate<AirportComponent>();
        random = new Random(72);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only shoot in frames where timer has expired
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0)
        {
            return;
        }

        timer = 1.5f; // reset timer
        
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        // Read the spawned planet from the world and pass it into the job
        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var config = SystemAPI.GetSingleton<ConfigComponent>();
        
        state.Dependency = new SpawnPlanes
        {
            ECB = ecb,
            Planet = planet,
            Config = config,
            Random =  random
            
        }.Schedule(state.Dependency);

    }
}

[BurstCompile]
[WithAll(typeof(AirportComponent))]
public partial struct SpawnPlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public PlanetComponent Planet;
    public ConfigComponent Config;
    public Random Random;
    private void Execute(ref LocalToWorld airport, ref URPMaterialPropertyBaseColor color)
    {
        var sphereCenter = Planet.Center;
        var sphereRadius = Planet.Radius;
        
        Entity planeEntity = ECB.Instantiate(Config.PlanePrefab);
        
        // PLACEHOLDER DESTINATION
        float3 r = new float3(
            Random.NextFloat(-100f, 100f),
            Random.NextFloat(-100f, 100f),
            Random.NextFloat(-100f, 100f)
        );
        float3 dest = sphereCenter + math.normalize(r - sphereCenter) * (sphereRadius + 5f);

        var planeTransform = LocalTransform.FromPosition(airport.Position);
        
        ECB.AddComponent(planeEntity, planeTransform);
        ECB.SetComponent(planeEntity, color);
        ECB.AddComponent(planeEntity, new PlaneComponent
        {
            Dest = dest
        });
        
    }
}