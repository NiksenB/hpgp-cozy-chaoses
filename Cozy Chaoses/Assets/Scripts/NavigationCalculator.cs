using System;
using System.Net;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


public class NavigationCalculator
{
    private enum Dir
    {
        Up,
        Down,
        Forward,
        End
    }

    private const float Speed = 8f;
    private const float FrameChangeDegLimit = 0.5f;

    public static (float3, quaternion) CalculateNext(LocalTransform transform, GuidePathComponent guidePath,
        float planetRadius, float deltaTime)
    {
        float height = math.length(transform.Position - new float3(planetRadius, planetRadius, planetRadius));

        if (math.length(transform.Position - guidePath.EndPoint) >= guidePath.TargetHeight * 2)
        {
            var hDiff = height - guidePath.TargetHeight;

            if (math.abs(hDiff) <= 5f)
            {
                return Calculate(transform, guidePath, planetRadius, deltaTime, Dir.Forward);
            }

            if (hDiff > 5f)
            {
                return Calculate(transform, guidePath, planetRadius, deltaTime, Dir.Up);
            }

            return Calculate(transform, guidePath, planetRadius, deltaTime, Dir.Down);
        }

        return Calculate(transform, guidePath, planetRadius, deltaTime, Dir.End);
    }

    private static (float3, quaternion) Calculate(LocalTransform transform, GuidePathComponent guidePath,
        float planetRadius, float deltaTime, Dir dir)
    {
        float3 up = math.normalize(transform.Position);

        float3 toDest;

        if (dir == Dir.Forward || dir == Dir.Up)
        {
            float3 aboveEnd = guidePath.EndPoint + math.normalize(guidePath.EndPoint) * guidePath.TargetHeight;
            toDest = aboveEnd - transform.Position;
        }
        else
        {
            toDest = guidePath.EndPoint - transform.Position;
        }

        // limit change pr frame
        float3 currentForward = math.normalize(math.forward(transform.Rotation.value));
        // Calculate angular error
        float3 cross = math.cross(currentForward, toDest);
        float sign = math.sign(math.dot(cross, up));
        // Make small turn towards destination, making angular error smaller
        quaternion smallTurn = quaternion.AxisAngle(up, math.radians(FrameChangeDegLimit) * sign);
        // Set the actual turn angle
        toDest = math.mul(smallTurn, currentForward);

        // NEW POSITION
        float3 surfaceTangent = math.normalize(toDest - up * math.dot(toDest, up));

        float3 rotationAxis = math.normalize(math.cross(up, surfaceTangent));
        float rotationAngle = deltaTime * Speed / (planetRadius + guidePath.TargetHeight);

        quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
        float3 newDirection = math.mul(rot, up);

        float3 newSurfacePos = dir switch
        {
            Dir.Forward => newDirection * (planetRadius + guidePath.TargetHeight),
            Dir.Up => newDirection * math.min(planetRadius + guidePath.TargetHeight,
                math.length(transform.Position) * 1.0001f),
            Dir.Down => newDirection * math.max(planetRadius + 1f, math.length(transform.Position) * 0.9995f),
            Dir.End => newDirection * math.max(planetRadius, math.length(transform.Position) * 0.9995f),
            _ => throw new ArgumentOutOfRangeException()
        };

        // NEW ROTATION
        float3 forward = math.normalize(newSurfacePos - transform.Position);
        quaternion newRotation = quaternion.LookRotation(forward, up);

        return (newSurfacePos, newRotation);
    }
}