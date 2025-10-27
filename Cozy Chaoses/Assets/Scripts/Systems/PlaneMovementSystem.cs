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
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PlanetComponent>();
        state.RequireForUpdate<ConfigComponent>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var deltaTime = (float)SystemAPI.Time.DeltaTime;
        
        state.Dependency = new MovePlanes
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(PlaneComponent))]
[WithNone(typeof(ShouldDespawnComponent))]
public partial struct MovePlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;
    
    public void Execute(Entity entity, ref LocalTransform transform, ref URPMaterialPropertyBaseColor color, in PlaneComponent plane)
    {
        float sphereRadius = Planet.Radius;
        float3 sphereCenter = Planet.Center;
        float speed = 5.0f;
        
        // Tanks tutorial note: Modify the point at which we sample the 3D noise function.
        var yPos = transform.Position;
        yPos.y = (float)entity.Index;

        float3 dest = plane.Dest;
        float3 pos = transform.Position;
        
        // Placeholder for despawn behavior
        if (HasArrived(pos, dest, sphereCenter))
        {
            ECB.AddComponent(entity, new ShouldDespawnComponent());
            return;
        }
        
        float3 toCenter = math.normalize(pos - sphereCenter);
        float3 toDest = math.normalize(dest - pos); 
        
        // NEW POSITION
        float3 surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));
        
        float3 rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
        float rotationAngle = DeltaTime * speed / (sphereRadius + 5f);

        quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
        float3 newDirection = math.mul(rot, toCenter);
        float3 newSurfacePos = sphereCenter + newDirection * (sphereRadius + 5.0f);

        transform.Position = newSurfacePos;

        // NEW ROTATION
        float3 forward = math.normalize(newSurfacePos - pos);
        float3 up = math.normalize(pos - sphereCenter);
        quaternion newRotation = quaternion.LookRotation(forward, up);
        transform.Rotation = newRotation;
    }
    
    private bool HasArrived(float3 pos, float3 dest, float3 sphereCenter)
    {
        float3 currentDir = math.normalize(pos - sphereCenter); 
        float3 destDir = math.normalize(dest - sphereCenter);

        return math.dot(currentDir, destDir) > 0.999f;
    }
}