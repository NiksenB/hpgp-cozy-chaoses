using UnityEngine;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

using Random = Unity.Mathematics.Random;
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlaneSpawnSystem : ISystem
{
    private float timer;
    
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
        
        var planeTransform = state.EntityManager.GetComponentData<LocalTransform>(config.PlanePrefab);
        
        foreach (var (airport, airportTransform, color) in
                 SystemAPI.Query<RefRO<Airport>, RefRO<LocalToWorld>, RefRO<URPMaterialPropertyBaseColor>>())
        {
            Entity planeEntity = state.EntityManager.Instantiate(config.PlanePrefab);
            
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
                    state.EntityManager.SetComponentData(entity.Value, color.ValueRO);    
                }
            }
            
            planeTransform.Position =  airportTransform.ValueRO.Position;
            
            state.EntityManager.SetComponentData(planeEntity, planeTransform);
            state.EntityManager.SetComponentData(planeEntity, new Plane
            {
            });
        }
    }
}