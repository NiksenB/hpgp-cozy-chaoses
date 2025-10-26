using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public partial struct PlaneMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 sphereCenter = new float3(0, 0, 0);
        float sphereRadius = 25.0f;
        float speed = 5.0f;
        
        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, color, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<URPMaterialPropertyBaseColor>>()
                     .WithAll<Plane>()
                     .WithEntityAccess())
        {
            // Tanks tutorial note: Modify the point at which we sample the 3D noise function.
            var yPos = transform.ValueRO.Position;
            yPos.y = (float)entity.Index;
            
            float3 dest = state.GetComponentLookup<Plane>()[entity].Dest;
            float3 pos = transform.ValueRO.Position;
            
            // Placeholder for despawn behavior
            ColorIfArrived(pos, dest, sphereCenter, color);
            
            float3 toCenter = math.normalize(pos - sphereCenter);
            float3 toDest = math.normalize(dest - pos); 
            
            // NEW POSITION
            float3 surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));
            
            float3 rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
            float rotationAngle = dt * speed / (sphereRadius + 5f);

            quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
            float3 newDirection = math.mul(rot, toCenter);
            float3 newSurfacePos = sphereCenter + newDirection * (sphereRadius + 5.0f);

            transform.ValueRW.Position = newSurfacePos;

            // NEW ROTATION
            float3 forward = math.normalize(newSurfacePos - pos);
            float3 up = math.normalize(pos - sphereCenter);
            quaternion newRotation = quaternion.LookRotation(forward, up);
            transform.ValueRW.Rotation = newRotation;
        }
    }

    private void ColorIfArrived(float3 pos, float3 dest, float3 sphereCenter, RefRW<URPMaterialPropertyBaseColor> color)
    {
        float3 currentDir = math.normalize(pos - sphereCenter); 
        float3 destDir = math.normalize(dest - sphereCenter);
            
        if (math.dot(currentDir, destDir) > 0.999f) 
        {
            // despawn here
            color.ValueRW.Value = new float4(0, 0, 0, 1); 
        }
    }
}
