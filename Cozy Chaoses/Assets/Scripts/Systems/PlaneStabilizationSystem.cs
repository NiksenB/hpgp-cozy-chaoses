using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public partial struct PlaneStabilizationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlaneStabilizerComponent>();
            state.RequireForUpdate<ConfigComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            var config = SystemAPI.GetSingleton<ConfigComponent>();

            state.Dependency = new StabilizePlaneJob
            {
                TransformLookup = transformLookup,
                DeltaTime = deltaTime,
                Config = config
            }.Schedule(state.Dependency);
        }
    }
}

// [BurstCompile]
public partial struct StabilizePlaneJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    [ReadOnly] public ConfigComponent Config;
    public float DeltaTime;

    public void Execute(ref PhysicsVelocity velocity, in LocalTransform transform,
        in PlaneStabilizerComponent planeStabilizerComponent)
    {
        if (!TransformLookup.HasComponent(planeStabilizerComponent.GuideEntity)) return;

        var targetTransform = TransformLookup[planeStabilizerComponent.GuideEntity];

        var currentRotation = math.normalizesafe(transform.Rotation);
        var targetRotation = math.normalizesafe(targetTransform.Rotation);

        DrawDebug(transform, currentRotation);

        // Get actual direction vectors in world space
        var currentForward = math.mul(currentRotation, new float3(0, 0, 1));
        var currentUp = math.mul(currentRotation, new float3(0, 1, 0));

        var targetForward = math.mul(targetRotation, new float3(0, 0, 1));
        var targetUp = math.mul(targetRotation, new float3(0, 1, 0));

        var angularError = float3.zero;

        // Forward alignment
        var forwardCross = math.cross(currentForward, targetForward);
        var forwardDot = math.dot(currentForward, targetForward);

        var forwardSinAngle = math.length(forwardCross);
        if (forwardSinAngle > 0.0001f)
        {
            var forwardAxis = forwardCross / forwardSinAngle;
            var forwardAngle = math.atan2(forwardSinAngle, forwardDot);
            angularError += forwardAxis * forwardAngle * Config.PlaneForwardWeight;
        }

        // Up alignment
        var targetUpProjected = targetUp - currentForward * math.dot(targetUp, currentForward);
        var targetUpProjLength = math.length(targetUpProjected);

        if (targetUpProjLength > 0.0001f)
        {
            targetUpProjected /= targetUpProjLength;

            // Same for current up
            var currentUpProjected = currentUp - currentForward * math.dot(currentUp, currentForward);
            var currentUpProjLength = math.length(currentUpProjected);

            if (currentUpProjLength > 0.0001f)
            {
                currentUpProjected /= currentUpProjLength;

                var upCross = math.cross(currentUpProjected, targetUpProjected);
                var upDot = math.dot(currentUpProjected, targetUpProjected);

                var upSinAngle = math.length(upCross);
                if (upSinAngle > 0.0001f)
                {
                    var upAxis = upCross / upSinAngle;
                    var upAngle = math.atan2(upSinAngle, upDot);
                    angularError += upAxis * upAngle * Config.PlaneUpWeight;
                }
            }
        }

        // Check if we're close enough to stop
        if (math.lengthsq(angularError) < 0.000001f)
        {
            velocity.Angular = math.lerp(velocity.Angular, float3.zero, math.saturate(Config.PlaneDamping * DeltaTime));
            return;
        }

        var proportionalTerm = angularError * Config.PlaneRotationSpeed;
        var derivativeTerm = velocity.Angular * Config.PlaneDamping;

        var targetAngularVelocity = proportionalTerm - derivativeTerm;

        // Clamp maximum angular velocity
        var speed = math.length(targetAngularVelocity);
        if (speed > Config.PlaneMaxAngularSpeed)
            targetAngularVelocity = targetAngularVelocity / speed * Config.PlaneMaxAngularSpeed;

        var blendFactor = 1f - math.exp(-Config.PlaneResponseSpeed * DeltaTime);
        velocity.Angular = math.lerp(velocity.Angular, targetAngularVelocity, blendFactor);
    }

    public void DrawDebug(LocalTransform transform, quaternion currentRotation)
    {
        var planeForward = math.mul(currentRotation, new float3(0, 0, 1));
        var planeUp = math.mul(currentRotation, new float3(0, 1, 0));

        Debug.DrawRay(transform.Position, planeForward * 2f, Color.blue); // Plane forward
        Debug.DrawRay(transform.Position, planeUp * 2f, Color.green); // Plane up
    }
}