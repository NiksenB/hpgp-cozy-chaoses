using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;

public partial struct PlaneMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 sphereCenter = new float3(0, 0, 0);
        float sphereRadius = 25.0f;
        
        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<Plane>()
                     .WithEntityAccess())
        {
            var pos = transform.ValueRO.Position;
            
            // Tanks turotial note:
            // This does not modify the actual position of the plane, only the point at
            // which we sample the 3D noise function. This way, every plane is using a
            // different slice and will move along its own different random flow field.
            pos.y = (float)entity.Index;
            
            var angle = (0.5f + noise.cnoise(pos / 10f)) * 4.0f * math.PI;
            var dir = float3.zero;
            math.sincos(angle, out dir.x, out dir.z);
            
            // PLANE
            // transform.ValueRW.Position += dir * dt * 5.0f;
            // transform.ValueRW.Rotation = quaternion.RotateY(angle);
            
            // SPHERE
            float3 currentPos = transform.ValueRO.Position;
            float3 currentSurfacePos = sphereCenter + math.normalize(currentPos - sphereCenter) * sphereRadius;
            float3 newPos = currentSurfacePos + dir * dt * 5.0f;
            float3 newSurfacePos = sphereCenter + math.normalize(newPos - sphereCenter) * sphereRadius;
            
            transform.ValueRW.Position = newSurfacePos;
            
            // TODO figure out angle on sphere surface
            transform.ValueRW.Rotation = quaternion.RotateY(angle);
        }
    }
}
