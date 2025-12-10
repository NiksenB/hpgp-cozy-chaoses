using Unity.Burst;
using Unity.Entities;

public partial struct DespawnSystem : ISystem
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
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var config = SystemAPI.GetSingleton<ConfigComponent>();

        switch (config.ExecutionMode)
        {
            case ExecutionMode.Main:
                foreach (var (_, entity) in SystemAPI.Query<RefRO<ShouldDespawnTag>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(entity);
                }
                break;

            case ExecutionMode.Schedule:
                state.Dependency = new DespawnJobSingle
                {
                    ECB = ecb
                }.Schedule(state.Dependency);
                break;

            case ExecutionMode.ScheduleParallel:
                state.Dependency = new DespawnJobParallel
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(state.Dependency);
                break;
        }
    }
}

[BurstCompile]
[WithAll(typeof(ShouldDespawnTag))]
public partial struct DespawnJobSingle : IJobEntity
{
    public EntityCommandBuffer ECB;

    public void Execute(Entity entity)
    {
        ECB.DestroyEntity(entity);
    }
}

[BurstCompile]
[WithAll(typeof(ShouldDespawnTag))]
public partial struct DespawnJobParallel : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity)
    {
        ECB.DestroyEntity(chunkIndex, entity);
    }
}
