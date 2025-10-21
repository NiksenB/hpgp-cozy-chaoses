using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
public partial struct AirportSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Disable in first update, so it updates only once
        state.Enabled = false;

        float3 sphereCenter = new float3(0, 0, 0);
        float sphereRadius = 25.0f;
        
        var config = SystemAPI.GetSingleton<Config>();

        var random = new Random(72);
        
        for (int i = 0; i < config.AirportCount; i++)
        {
            var airportEntity = state.EntityManager.Instantiate(config.AirportPrefab);
            
            var color = new URPMaterialPropertyBaseColor
            {
                Value = RandomColor(ref random)
            };
            
            state.EntityManager.SetComponentData(airportEntity, color);    
            
            float3 newPos = new float3(
                random.NextFloat(-100f, 100f),
                random.NextFloat(-100f, 100f),
                random.NextFloat(-100f, 100f)
            );
            
            float3 newSurfacePos = sphereCenter + math.normalize(newPos - sphereCenter) * sphereRadius;
            LocalTransform transform = LocalTransform.FromPosition(newSurfacePos);
            
            state.EntityManager.SetComponentData(airportEntity, transform);
        }
    }
    
    // From Tank Tutorial:
    // Return a random color that is visually distinct.
    // (Naive randomness would produce a distribution of colors clustered 
    // around a narrow range of hues. See https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/ )
    static float4 RandomColor(ref Random random)
    {
        // 0.618034005f is inverse of the golden ratio
        var hue = (random.NextFloat() + 0.618034005f) % 1;
        return (Vector4)Color.HSVToRGB(hue, 1.0f, 1.0f);
    }
}
