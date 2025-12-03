using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlaneAndGuideSpawnSystem : ISystem
{
    private float timer;
    private NativeArray<LocalTransform> airports;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<ConfigComponent>();
        state.RequireForUpdate<PlanetComponent>();
        state.RequireForUpdate<AirportComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        var config = SystemAPI.GetSingleton<ConfigComponent>();
        
        if (!airports.IsCreated || airports.Length == 0)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<AirportComponent, LocalTransform>().Build();
            
            airports = query.ToComponentDataArray<LocalTransform>(Allocator.Persistent);
        }
        
        var planet = SystemAPI.GetSingleton<PlanetComponent>();

        var elapsedTime = SystemAPI.Time.ElapsedTime;

        state.Dependency = new SpawnPlanes
        {
            ECB = ecb,
            Config = config,
            ElapsedTime = elapsedTime,
            Airports = airports,
            Planet = planet,
        }.Schedule(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        airports.Dispose();
    }
}

[BurstCompile]
[WithAll(typeof(AirportComponent), typeof(LocalTransform))]
public partial struct SpawnPlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public ConfigComponent Config;
    public double ElapsedTime;
    public NativeArray<LocalTransform> Airports;
    public PlanetComponent Planet;
    
    private void Execute(ref AirportComponent sourceComponent, in LocalTransform sourceTransform)
    {
        if (ElapsedTime < sourceComponent.NextPlaneSpawnTime)
        {
            return;
        }
 
        var random = new Random((uint)ElapsedTime + 100);
        sourceComponent.NextPlaneSpawnTime += random.NextDouble(10d, 100d);
        
        Entity planeEntity = ECB.Instantiate(Config.PlanePrefab);
        
        var di = math.abs(random.NextInt()) % Airports.Length;
        
        while (sourceTransform.Position.Equals(Airports[di].Position))
        {
            di = (di+1) %  Airports.Length;
        }

        var dest = Airports[di].Position;
        
        // Spawn a little above the airport
        var up = math.normalize(sourceTransform.Position);
        var spawnPosition = sourceTransform.Position + up * 1f;
        
        ECB.AddComponent(planeEntity, LocalTransform.FromPositionRotation(spawnPosition, sourceTransform.Rotation));
        ECB.SetComponent(planeEntity, new GuidePathComponent
        {
            StartPoint = sourceTransform.Position,
            EndPoint = dest,
            TargetHeight = random.NextFloat(5f, 15f), // TODO: Make a function of distance
        });
    }
}