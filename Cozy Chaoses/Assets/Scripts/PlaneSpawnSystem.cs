using UnityEngine;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Rendering;
using Random = Unity.Mathematics.Random;
public partial struct PlaneSpawnSystem : ISystem
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

        var config = SystemAPI.GetSingleton<Config>();

        var random = new Random(42);
        
        for (int i = 0; i < config.PlaneCount; i++)
        {
            var planeEntity = state.EntityManager.Instantiate(config.PlanePrefab);
            var color = new URPMaterialPropertyBaseColor
            {
                Value = RandomColor(ref random)
            };
            
            // From Tank tutorial:
            // Every root entity instantiated from a prefab has a LinkedEntityGroup component, which
            // is a list of all the entities that make up the prefab hierarchy (including the root).
            // (LinkedEntityGroup is a special kind of component called a "DynamicBuffer", which is
            // a resizable array of struct values instead of just a single struct.)
            
            var linkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(planeEntity);
            foreach (var entity in linkedEntities)
            {
                if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity.Value))
                {
                    state.EntityManager.SetComponentData(entity.Value, color);    
                }
            }
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
