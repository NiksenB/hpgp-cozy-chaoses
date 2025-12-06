using Unity.Entities;
using Unity.Burst;

// TODO decide if this logic should be part of DespawnSystem
public partial struct ExplosionFadingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        var elapsed = (float)SystemAPI.Time.ElapsedTime;
        
        state.Dependency = new ManageExplosions
        {
            ECB = ecb,
            Elapsed = elapsed,
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct ManageExplosions : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float Elapsed;
    public void Execute(Entity entity, in ExplosionComponent explosion)
    {
        if (Elapsed >= explosion.Startpoint + explosion.Duration)
        {
            ECB.DestroyEntity(entity);
        }
    }
}