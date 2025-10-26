using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlaneSpawnSystem : ISystem
{
    private float timer;
    private Random random;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
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
        timer = 1.5f;   // reset timer

        var config = SystemAPI.GetSingleton<Config>();
        var radius = config.PlanetRadius;
        
        var planeTransform = state.EntityManager.GetComponentData<LocalTransform>(config.PlanePrefab);
        
        foreach (var (airport, airportTransform, color) in
                 SystemAPI.Query<RefRO<Airport>, RefRO<LocalToWorld>, RefRO<URPMaterialPropertyBaseColor>>())
        {
            Entity planeEntity = state.EntityManager.Instantiate(config.PlanePrefab);
            
            var linkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(planeEntity);
            foreach (var entity in linkedEntities)
            {
                if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity.Value))
                {
                    state.EntityManager.SetComponentData(entity.Value, color.ValueRO);    
                }
            }
            
            // PLACEHOLDER DESTINATION
            float3 r = new float3(
                random.NextFloat(-100f, 100f),
                random.NextFloat(-100f, 100f),
                random.NextFloat(-100f, 100f)
            );
            float3 dest = float3.zero + math.normalize(r - float3.zero) * (radius + 5f);
            
            planeTransform.Position =  airportTransform.ValueRO.Position;
            
            state.EntityManager.SetComponentData(planeEntity, planeTransform);
            state.EntityManager.SetComponentData(planeEntity, new Plane
            {
                Dest = dest
            });
        }
    }
}