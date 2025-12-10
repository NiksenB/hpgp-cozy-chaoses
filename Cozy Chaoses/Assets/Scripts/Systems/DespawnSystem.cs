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

        state.Dependency = new DespawnJob
        {
            ECB = ecb
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(ShouldDespawnTag))]
public partial struct DespawnJob : IJobEntity
{
    public EntityCommandBuffer ECB;

    public void Execute(Entity entity)
    {
        ECB.DestroyEntity(entity);
    }
}