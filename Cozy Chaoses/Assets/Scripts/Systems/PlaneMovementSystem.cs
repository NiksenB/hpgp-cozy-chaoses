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
        
        var alertPlanes = new AvoidCollissions
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(state.Dependency);
        
        state.Dependency = new MovePlanes
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(alertPlanes);
    }
}


[BurstCompile]
[WithAll(typeof(PlaneComponent))]
[WithNone(typeof(ShouldDespawnComponent))]
[WithAll(typeof(AlertComponent))]
public partial struct AvoidCollissions : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref AlertComponent alert, in PlaneComponent plane)
    {
        // Tanks tutorial note: Modify the point at which we sample the 3D noise function.
        var yPos = transform.Position;
        yPos.y = (float)entity.Index;
        
        float sphereRadius = Planet.Radius;
        float3 sphereCenter = Planet.Center;
        float speed = 5.0f;
        float3 dest = plane.Dest;
        float3 pos = transform.Position;

        float3 toCenter = math.normalize(pos - sphereCenter);
        float3 toDest = math.normalize(dest - pos);
        var toTar = math.normalize(alert.EntityPos - pos);
        
        float3 up = math.normalize(pos - sphereCenter);
        float3 currentForward = math.normalize(math.forward(transform.Rotation.value));

        float3 idealDir = float3.zero;
        var dot = math.dot(toTar, currentForward);
        
        // If planes are head-on or nearly head-on, rotate until 30° to the right
        if (dot == 0 || dot > 0.7f)
        {
            quaternion right30 = quaternion.AxisAngle(up, math.radians(30f));
            idealDir = math.mul(right30, currentForward);
        }
        // target is coming from less a dangerous angle, small adjustment to course
        else if (dot > 0.2f)
        {
            idealDir = math.normalize(-toTar);
        }
        
        // If we haven't turned enough yet
        float dirDiff = math.dot(currentForward, idealDir);
        if (dirDiff < 0.99f) // not yet close to ideal
        {
            float3 cross = math.cross(currentForward, idealDir);
            float sign = math.sign(math.dot(cross, up));
            quaternion smallTurn = quaternion.AxisAngle(up, math.radians(2f) * sign);
            toDest = math.mul(smallTurn, currentForward);
        }
        else
        {
            toDest = idealDir;
        }

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
        quaternion newRotation = quaternion.LookRotation(forward, up);
        transform.Rotation = newRotation;
        
        // Check if still overlapping
        if (math.length(alert.EntityPos - transform.Position) > 10f)
        {
            ECB.RemoveComponent<AlertComponent>(entity);
        }
    }
}

[BurstCompile]
[WithAll(typeof(PlaneComponent))]
[WithNone(typeof(ShouldDespawnComponent))]
[WithNone(typeof(AlertComponent))]
public partial struct MovePlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;
    
    public void Execute(Entity entity, ref LocalTransform transform, in PlaneComponent plane)
    {
        float sphereRadius = Planet.Radius;
        float3 sphereCenter = Planet.Center;
        float speed = 5.0f;
        
        // Tanks tutorial note: Modify the point at which we sample the 3D noise function.
        var yPos = transform.Position;
        yPos.y = (float)entity.Index;
        
        float3 dest = plane.Dest;
        float3 pos = transform.Position;
        
        if (HasArrived(pos, dest, sphereCenter))
        {
            ECB.AddComponent(entity, new ShouldDespawnComponent());
            return;
        }
        
        float3 toCenter = math.normalize(pos - sphereCenter);
        float3 toDest = math.normalize(dest - pos); 
        float3 up = math.normalize(pos - sphereCenter);
        float3 currentForward = math.normalize(math.forward(transform.Rotation.value));
        var dot = math.dot(toDest, currentForward);

        if (dot < 0.99f)
        {
            // adjust course so we only rotate 2 degrees
            float3 cross = math.cross(currentForward, toDest);
            float sign = math.sign(math.dot(cross, up));
            quaternion smallTurn = quaternion.AxisAngle(up, math.radians(2f) * sign);
            toDest = math.mul(smallTurn, currentForward);
        }
        
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