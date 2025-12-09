using Unity.Entities;
using Unity.Burst;

public partial struct ExplosionDespawnSystem : ISystem
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
        // This number must match the duration of the explosion particle system
        var duration = 1f;
        if (Elapsed >= explosion.Startpoint + duration)
        {
            ECB.DestroyEntity(entity);
        }
    }
}