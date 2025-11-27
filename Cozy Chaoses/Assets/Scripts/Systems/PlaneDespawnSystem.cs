using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting;
using Random = Unity.Mathematics.Random;

public partial struct PlaneDespawnSystem : ISystem
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
        
        state.Dependency = new DespawnPlanes
        {
            ECB = ecb,
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(ShouldDespawnTag))]
public partial struct DespawnPlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public void Execute(Entity entity)
    {
        ECB.DestroyEntity(entity);
    }
}