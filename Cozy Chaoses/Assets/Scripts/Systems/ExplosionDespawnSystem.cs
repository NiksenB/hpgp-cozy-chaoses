using Unity.Burst;
using Unity.Entities;

public partial struct ExplosionDespawnSystem : ISystem
{
    public const float ExplosionDuration = 1f;

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

        var elapsed = (float)SystemAPI.Time.ElapsedTime;
        var config = SystemAPI.GetSingleton<ConfigComponent>();

        switch (config.ExecutionMode)
        {
            case ExecutionMode.Main:
                foreach (var (explosion, entity) in SystemAPI.Query<RefRO<ExplosionComponent>>().WithEntityAccess())
                {
                    if (elapsed >= explosion.ValueRO.Startpoint + ExplosionDuration)
                        ecb.DestroyEntity(entity);
                }
                break;

            case ExecutionMode.Schedule:
                state.Dependency = new ManageExplosionsSingle
                {
                    ECB = ecb,
                    Elapsed = elapsed
                }.Schedule(state.Dependency);
                break;

            case ExecutionMode.ScheduleParallel:
                state.Dependency = new ManageExplosionsParallel
                {
                    ECB = ecb.AsParallelWriter(),
                    Elapsed = elapsed
                }.ScheduleParallel(state.Dependency);
                break;
        }
    }
}

[BurstCompile]
public partial struct ManageExplosionsSingle : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float Elapsed;

    public void Execute(Entity entity, in ExplosionComponent explosion)
    {
        if (Elapsed >= explosion.Startpoint + ExplosionDespawnSystem.ExplosionDuration)
            ECB.DestroyEntity(entity);
    }
}

[BurstCompile]
public partial struct ManageExplosionsParallel : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float Elapsed;

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in ExplosionComponent explosion)
    {
        if (Elapsed >= explosion.Startpoint + ExplosionDespawnSystem.ExplosionDuration)
            ECB.DestroyEntity(chunkIndex, entity);
    }
}
