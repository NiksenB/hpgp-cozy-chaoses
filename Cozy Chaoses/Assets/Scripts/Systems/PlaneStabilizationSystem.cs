using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Systems
{
    public partial struct PlaneStabilizationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var deltaTime = SystemAPI.Time.DeltaTime;

            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true);

            state.Dependency = new StabilizePlaneJob
            {
                ECB = ecb,
                TransformLookup = transformLookup,
                DeltaTime = deltaTime
            }.Schedule(state.Dependency);
        }
    }
}

[BurstCompile]
public partial struct StabilizePlaneJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    public EntityCommandBuffer ECB;
    public float DeltaTime;

    public void Execute(ref PhysicsVelocity velocity, in LocalTransform transform, in PlaneStabilizer stabilizer)
    {
        if (!TransformLookup.HasComponent(stabilizer.TargetEntity)) return;

        // Get the guide rotation
        LocalTransform targetTransform = TransformLookup[stabilizer.TargetEntity];

        // Ensure rotations are normalized to avoid "Input quaternion was not normalized"
        quaternion desiredRotation = new quaternion(math.normalize(targetTransform.Rotation.value));
        quaternion currentRotation = new quaternion(math.normalize(transform.Rotation.value));

        // Calculate the difference required to align
        quaternion delta = math.mul(math.inverse(currentRotation), desiredRotation);

        // Clamp w to avoid NaNs from acos due to floating point drift
        float w = math.clamp(delta.value.w, -1f, 1f);
        float angle = 2f * math.acos(w);

        float s = math.sqrt(math.max(0f, 1f - (w * w)));
        float3 axis;
        if (s < 0.0001f)
        {
            axis = new float3(1f, 0f, 0f);
            angle = 0f;
        }
        else
        {
            axis = delta.value.xyz / s;
        }

        // angle wrapping
        if (angle > math.PI) angle = -(2f * math.PI - angle);

        // angular velocity
        float3 targetAngularVel = math.mul(currentRotation, axis) * angle * stabilizer.RotationSpeed;

        // dampening
        velocity.Angular = math.lerp(velocity.Angular, targetAngularVel, DeltaTime * 5f);
    }
}