using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Rendering;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
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

    public void Execute(Entity entity, ref LocalTransform transform, ref PhysicsVelocity velocity, ref URPMaterialPropertyBaseColor color, in PlaneComponent plane)
    {
        float sphereRadius = Planet.Radius;
        float3 sphereCenter = Planet.Center;
        float speed = 5.0f;

        float3 dest = plane.Dest;
        float3 pos = transform.Position;

        // Check if arrived at destination
        if (HasArrived(pos, dest, sphereCenter))
        {
            ECB.AddComponent(entity, new ShouldDespawnComponent());
            velocity.Linear = float3.zero;
            velocity.Angular = float3.zero;
            return;
        }

        float3 toCenter = math.normalize(pos - sphereCenter);
        float3 toDest = math.normalize(dest - pos);

        // Calculate tangent direction along sphere surface toward destination
        float3 surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));

        // Calculate current distance from sphere center
        float currentDistance = math.length(pos - sphereCenter);
        float targetDistance = sphereRadius + 5.0f;
        float distanceError = targetDistance - currentDistance;

        // Add radial correction to maintain altitude above sphere
        float radialCorrectionStrength = 10.0f;
        float3 radialCorrection = toCenter * distanceError * radialCorrectionStrength;

        // Combine tangential movement with radial correction
        velocity.Linear = surfaceTangent * speed + radialCorrection;

        // Calculate rotation for orientation
        float3 forward = surfaceTangent;
        float3 up = toCenter;
        quaternion targetRotation = quaternion.LookRotation(forward, up);

        // Apply angular velocity to smoothly rotate toward target orientation
        quaternion currentRotation = transform.Rotation;
        quaternion rotationDelta = math.mul(targetRotation, math.inverse(currentRotation));

        // Convert quaternion to axis-angle for angular velocity
        float angle;
        float3 axis;
        ToAngleAxis(rotationDelta, out angle, out axis);

        // Normalize angle to [-pi, pi]
        if (angle > math.PI)
            angle -= 2.0f * math.PI;

        // Apply damped angular velocity to prevent wild spinning
        float angularSpeed = 5.0f; // Max rotation speed in radians per second
        float targetAngularVelocity = angle / DeltaTime;
        float clampedAngularVelocity = math.clamp(targetAngularVelocity, -angularSpeed, angularSpeed);

        velocity.Angular = axis * clampedAngularVelocity;
    }

    private bool HasArrived(float3 pos, float3 dest, float3 sphereCenter)
    {
        float3 currentDir = math.normalize(pos - sphereCenter);
        float3 destDir = math.normalize(dest - sphereCenter);

        return math.dot(currentDir, destDir) > 0.999f;
    }

    private void ToAngleAxis(quaternion q, out float angle, out float3 axis)
    {
        if (math.abs(q.value.w) > 1.0f)
        {
            q = math.normalize(q);
        }

        angle = 2.0f * math.acos(q.value.w);
        float s = math.sqrt(1.0f - q.value.w * q.value.w);

        if (s < 0.001f)
        {
            axis = new float3(1, 0, 0);
        }
        else
        {
            axis = new float3(q.value.x / s, q.value.y / s, q.value.z / s);
        }
    }
}