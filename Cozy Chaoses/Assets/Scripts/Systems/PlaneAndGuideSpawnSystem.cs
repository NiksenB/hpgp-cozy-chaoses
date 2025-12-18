using System.Threading;
using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlaneAndGuideSpawnSystem : ISystem
{
    private float _timer;
    private NativeArray<LocalTransform> _airports;
    private NativeReference<int> _planesSpawned;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<ConfigComponent>();
        state.RequireForUpdate<AirportComponent>();
        _planesSpawned = new NativeReference<int>(Allocator.Persistent);
    }

    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var configRW = SystemAPI.GetSingletonRW<ConfigComponent>();
        var config = configRW.ValueRO;

        if (!_airports.IsCreated || _airports.Length == 0)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<AirportComponent, LocalTransform>().Build();

            _airports = query.ToComponentDataArray<LocalTransform>(Allocator.Persistent);
        }

        var elapsedTime = SystemAPI.Time.ElapsedTime;
        _planesSpawned.Value = 0;
        var counterRef = _planesSpawned.GetUnsafePtr();

        switch (config.ExecutionMode)
        {
            case ExecutionMode.Main:
                foreach (var (airportComponent, sourceTransform) in SystemAPI
                             .Query<RefRW<AirportComponent>, RefRO<LocalTransform>>()
                             .WithAll<AirportComponent, LocalTransform>())
                {
                    if (elapsedTime < airportComponent.ValueRO.NextPlaneSpawnTime) continue;
                    if (configRW.ValueRO.CurrentPlaneCount + *counterRef >= config.MaxPlaneCount) continue;
                    Interlocked.Increment(ref UnsafeUtility.AsRef<int>(counterRef));

                    var random = new Random((uint)elapsedTime + 100);
                    airportComponent.ValueRW.NextPlaneSpawnTime += random.NextDouble(config.NextPlaneSpawnTimeLower,
                        config.NextPlaneSpawnTimeUpper);
                    var planeAndGuideEntity = ecb.Instantiate(config.PlanePrefab);

                    var di = math.abs(random.NextInt()) % _airports.Length;

                    while (sourceTransform.ValueRO.Position.Equals(_airports[di].Position))
                        di = (di + 1) % _airports.Length;

                    var dest = _airports[di].Position;
                    var dist = math.length(dest - sourceTransform.ValueRO.Position);

                    var up = math.normalize(sourceTransform.ValueRO.Position);
                    var spawnPosition = sourceTransform.ValueRO.Position + up * 1f;

                    var directionToDest = dest - spawnPosition;
                    var forward = directionToDest - math.dot(directionToDest, up) * up;

                    if (math.lengthsq(forward) < 1e-4f)
                    {
                        forward = math.cross(up, new float3(1, 0, 0));
                        if (math.lengthsq(forward) < 1e-4f)
                            forward = math.cross(up, new float3(0, 0, 1));
                    }

                    forward = math.normalize(forward);

                    var spawnRotation = quaternion.LookRotation(forward, up);

                    ecb.AddComponent(planeAndGuideEntity,
                        LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
                    
                    ecb.AddComponent(planeAndGuideEntity, new JustSpawnedMustBeMoved
                    {
                        Position = spawnPosition,
                        Rotation = spawnRotation
                    });
                    
                    ecb.AddComponent(planeAndGuideEntity, new GuidePathComponent
                    {
                        EndPoint = dest,
                        TargetAltitude = random.NextFloat(0.01f * config.PlanetRadius * (dist / config.PlanetRadius),
                            0.05f * config.PlanetRadius * (dist / config.PlanetRadius))
                    });
                }

                break;

            case ExecutionMode.Schedule:
                _planesSpawned.Value = 0;
                state.Dependency = new SpawnPlanesSingle
                {
                    ECB = ecb,
                    Config = config,
                    ElapsedTime = elapsedTime,
                    Airports = _airports,
                    CounterRef = counterRef
                }.Schedule(state.Dependency);
                state.Dependency.Complete();
                break;

            case ExecutionMode.ScheduleParallel:
                state.Dependency = new SpawnPlanesParallel
                {
                    ECB = ecb.AsParallelWriter(),
                    Config = config,
                    ElapsedTime = elapsedTime,
                    Airports = _airports,
                    CounterRef = counterRef
                }.ScheduleParallel(state.Dependency);
                state.Dependency.Complete();
                break;
        }

        configRW.ValueRW.CurrentPlaneCount += *counterRef;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_airports.IsCreated)
            _airports.Dispose();
        if (_planesSpawned.IsCreated)
            _planesSpawned.Dispose();
    }
}

[BurstCompile]
[WithAll(typeof(AirportComponent), typeof(LocalTransform))]
public partial struct SpawnPlanesSingle : IJobEntity
{
    public EntityCommandBuffer ECB;
    public ConfigComponent Config;
    public double ElapsedTime;
    [ReadOnly] public NativeArray<LocalTransform> Airports;
    [NativeDisableUnsafePtrRestriction] public unsafe int* CounterRef;

    private unsafe void Execute(ref AirportComponent sourceComponent, in LocalTransform sourceTransform)
    {
        if (ElapsedTime < sourceComponent.NextPlaneSpawnTime) return;
        if (Config.CurrentPlaneCount + *CounterRef >= Config.MaxPlaneCount) return;
        Interlocked.Increment(ref UnsafeUtility.AsRef<int>(CounterRef));

        var random = new Random((uint)ElapsedTime + 100);
        sourceComponent.NextPlaneSpawnTime +=
            random.NextDouble(Config.NextPlaneSpawnTimeLower, Config.NextPlaneSpawnTimeUpper);

        var planeAndGuideEntity = ECB.Instantiate(Config.PlanePrefab);

        var di = math.abs(random.NextInt()) % Airports.Length;

        while (sourceTransform.Position.Equals(Airports[di].Position)) di = (di + 1) % Airports.Length;

        var dest = Airports[di].Position;
        var dist = math.length(dest - sourceTransform.Position);

        var up = math.normalize(sourceTransform.Position);
        var spawnPosition = sourceTransform.Position + up * 1f;

        var directionToDest = dest - spawnPosition;
        var forward = directionToDest - math.dot(directionToDest, up) * up;

        if (math.lengthsq(forward) < 1e-4f)
        {
            forward = math.cross(up, new float3(1, 0, 0));
            if (math.lengthsq(forward) < 1e-4f)
                forward = math.cross(up, new float3(0, 0, 1));
        }

        forward = math.normalize(forward);

        var spawnRotation = quaternion.LookRotation(forward, up);

        ECB.AddComponent(planeAndGuideEntity,
            new JustSpawnedMustBeMoved { Position = spawnPosition, Rotation = spawnRotation });
        ECB.AddComponent(planeAndGuideEntity, new GuidePathComponent
        {
            EndPoint = dest,
            TargetAltitude = random.NextFloat(0.01f * Config.PlanetRadius * (dist / Config.PlanetRadius),
                0.05f * Config.PlanetRadius * (dist / Config.PlanetRadius))
        });
    }
}

[BurstCompile]
[WithAll(typeof(AirportComponent), typeof(LocalTransform))]
public partial struct SpawnPlanesParallel : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public ConfigComponent Config;
    public double ElapsedTime;
    [ReadOnly] public NativeArray<LocalTransform> Airports;
    [NativeDisableUnsafePtrRestriction] public unsafe int* CounterRef;

    private unsafe void Execute([ChunkIndexInQuery] int chunkIndex, ref AirportComponent sourceComponent,
        in LocalTransform sourceTransform)
    {
        if (ElapsedTime < sourceComponent.NextPlaneSpawnTime) return;
        if (Config.CurrentPlaneCount + *CounterRef >= Config.MaxPlaneCount) return;
        Interlocked.Increment(ref UnsafeUtility.AsRef<int>(CounterRef));

        var random = new Random((uint)ElapsedTime + 100);
        sourceComponent.NextPlaneSpawnTime +=
            random.NextDouble(Config.NextPlaneSpawnTimeLower, Config.NextPlaneSpawnTimeUpper);
        var planeAndGuideEntity = ECB.Instantiate(chunkIndex, Config.PlanePrefab);

        var di = math.abs(random.NextInt()) % Airports.Length;

        while (sourceTransform.Position.Equals(Airports[di].Position)) di = (di + 1) % Airports.Length;

        var dest = Airports[di].Position;
        var dist = math.length(dest - sourceTransform.Position);

        var up = math.normalize(sourceTransform.Position);
        var spawnPosition = sourceTransform.Position + up * 1f;

        var directionToDest = dest - spawnPosition;
        var forward = directionToDest - math.dot(directionToDest, up) * up;

        if (math.lengthsq(forward) < 1e-4f)
        {
            forward = math.cross(up, new float3(1, 0, 0));
            if (math.lengthsq(forward) < 1e-4f)
                forward = math.cross(up, new float3(0, 0, 1));
        }

        forward = math.normalize(forward);

        var spawnRotation = quaternion.LookRotation(forward, up);

        ECB.AddComponent(chunkIndex, planeAndGuideEntity,
            LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
        
        ECB.AddComponent(chunkIndex, planeAndGuideEntity,
            new JustSpawnedMustBeMoved { Position = spawnPosition, Rotation = spawnRotation });

        ECB.AddComponent(chunkIndex, planeAndGuideEntity, new GuidePathComponent
        {
            EndPoint = dest,
            TargetAltitude = random.NextFloat(0.01f * Config.PlanetRadius * (dist / Config.PlanetRadius),
                0.05f * Config.PlanetRadius * (dist / Config.PlanetRadius))
        });
    }
}