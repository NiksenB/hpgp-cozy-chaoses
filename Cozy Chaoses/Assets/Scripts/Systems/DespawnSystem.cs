using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public partial struct DespawnSystem : ISystem
{
    private NativeReference<int> _planesDespawned;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<ConfigComponent>();
        _planesDespawned = new NativeReference<int>(Allocator.Persistent);
    }

    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var configRW = SystemAPI.GetSingletonRW<ConfigComponent>();
        var config = configRW.ValueRO;

        _planesDespawned.Value = 0;
        var counterRef = _planesDespawned.GetUnsafePtr();

        switch (config.ExecutionMode)
        {
            case ExecutionMode.Main:
                foreach (var (_, entity) in SystemAPI.Query<RefRO<ShouldDespawnTag>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(entity);
                    Interlocked.Increment(ref UnsafeUtility.AsRef<int>(counterRef));
                }
                break;

            case ExecutionMode.Schedule:
                state.Dependency = new DespawnJobSingle
                {
                    ECB = ecb,
                    CounterRef = counterRef,
                }.Schedule(state.Dependency);
                break;

            case ExecutionMode.ScheduleParallel:
                state.Dependency = new DespawnJobParallel
                {
                    ECB = ecb.AsParallelWriter(),
                    CounterRef = counterRef,
                }.ScheduleParallel(state.Dependency);
                break;
        }
        
        state.Dependency.Complete();
        configRW.ValueRW.CurrentPlaneCount -= _planesDespawned.Value;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_planesDespawned.IsCreated)
            _planesDespawned.Dispose();
    }
}

[BurstCompile]
[WithAll(typeof(ShouldDespawnTag))]
public partial struct DespawnJobSingle : IJobEntity
{
    public EntityCommandBuffer ECB;
    [NativeDisableUnsafePtrRestriction] public unsafe int* CounterRef;

    public unsafe void Execute(Entity entity)
    {
        ECB.DestroyEntity(entity);
        Interlocked.Increment(ref UnsafeUtility.AsRef<int>(CounterRef));
    }
}

[BurstCompile]
[WithAll(typeof(ShouldDespawnTag))]
public partial struct DespawnJobParallel : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    [NativeDisableUnsafePtrRestriction] public unsafe int* CounterRef;

    public unsafe void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity)
    {
        ECB.DestroyEntity(chunkIndex, entity);
        Interlocked.Increment(ref UnsafeUtility.AsRef<int>(CounterRef));
    }
}
