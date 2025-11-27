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
            state.RequireForUpdate<PlaneStabilizer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true);

            state.Dependency = new StabilizePlaneJob
            {
                TransformLookup = transformLookup,
                DeltaTime = deltaTime
            }.Schedule(state.Dependency);
        }
    }
}

// [BurstCompile]
public partial struct StabilizePlaneJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    public float DeltaTime;

    public void Execute(ref PhysicsVelocity velocity, in LocalTransform transform, in PlaneStabilizer stabilizer)
    {
   if (!TransformLookup.HasComponent(stabilizer.TargetEntity)) return;

        LocalTransform targetTransform = TransformLookup[stabilizer.TargetEntity];

        quaternion currentRotation = math.normalizesafe(transform.Rotation);
        quaternion targetRotation = math.normalizesafe(targetTransform.Rotation);
        
        DrawDebug(transform, currentRotation);

        // Get actual direction vectors in world space
        float3 currentForward = math.mul(currentRotation, new float3(0, 0, 1));
        float3 currentUp = math.mul(currentRotation, new float3(0, 1, 0));
        
        float3 targetForward = math.mul(targetRotation, new float3(0, 0, 1));
        float3 targetUp = math.mul(targetRotation, new float3(0, 1, 0));

        float3 angularError = float3.zero;

        // Forward alignment
        float3 forwardCross = math.cross(currentForward, targetForward);
        float forwardDot = math.dot(currentForward, targetForward);
        
        float forwardSinAngle = math.length(forwardCross);
        if (forwardSinAngle > 0.0001f)
        {
            float3 forwardAxis = forwardCross / forwardSinAngle;
            float forwardAngle = math.atan2(forwardSinAngle, forwardDot);
            angularError += forwardAxis * forwardAngle * stabilizer.ForwardWeight;
        }

        // Up alignment
        float3 targetUpProjected = targetUp - currentForward * math.dot(targetUp, currentForward);
        float targetUpProjLength = math.length(targetUpProjected);
        
        if (targetUpProjLength > 0.0001f)
        {
            targetUpProjected /= targetUpProjLength;
            
            // Same for current up
            float3 currentUpProjected = currentUp - currentForward * math.dot(currentUp, currentForward);
            float currentUpProjLength = math.length(currentUpProjected);
            
            if (currentUpProjLength > 0.0001f)
            {
                currentUpProjected /= currentUpProjLength;
                
                float3 upCross = math.cross(currentUpProjected, targetUpProjected);
                float upDot = math.dot(currentUpProjected, targetUpProjected);
                
                float upSinAngle = math.length(upCross);
                if (upSinAngle > 0.0001f)
                {
                    float3 upAxis = upCross / upSinAngle;
                    float upAngle = math.atan2(upSinAngle, upDot);
                    angularError += upAxis * upAngle * stabilizer.UpWeight;
                }
            }
        }

        // Check if we're close enough to stop
        if (math.lengthsq(angularError) < 0.000001f)
        {
            velocity.Angular = math.lerp(velocity.Angular, float3.zero, math.saturate(stabilizer.Damping * DeltaTime));
            return;
        }

        float3 proportionalTerm = angularError * stabilizer.RotationSpeed;
        float3 derivativeTerm = velocity.Angular * stabilizer.Damping;
        
        float3 targetAngularVelocity = proportionalTerm - derivativeTerm;
        
        // Clamp maximum angular velocity
        float speed = math.length(targetAngularVelocity);
        if (speed > stabilizer.MaxAngularSpeed)
        {
            targetAngularVelocity = (targetAngularVelocity / speed) * stabilizer.MaxAngularSpeed;
        }

        float blendFactor = 1f - math.exp(-stabilizer.ResponseSpeed * DeltaTime);
        velocity.Angular = math.lerp(velocity.Angular, targetAngularVelocity, blendFactor);
    }

    public void DrawDebug(LocalTransform transform, quaternion currentRotation)
    {
        float3 planeForward = math.mul(currentRotation, new float3(0, 0, 1));
        float3 planeUp = math.mul(currentRotation, new float3(0, 1, 0));

        Debug.DrawRay(transform.Position, planeForward * 2f, Color.blue);   // Plane forward
        Debug.DrawRay(transform.Position, planeUp * 2f, Color.green);       // Plane up
    }
}
