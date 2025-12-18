using Unity.Mathematics;
using Unity.Transforms;

public static class NavigationCalculator
{
    private const float maxDirChangePerFrame = 0.75f;

    private const float minClimbAngleFromUp = 40f; // 50° above horizon = 40° from radial up
    private const float maxClimbAngleFromUp = 70f; // 40° above horizon = 50° from radial up
    private const float minDescentAngleFromUp = 85f; // 0° below horizon = 90° from radial up. Allows "cruising".
    private const float maxDescentAngleFromUp = 120f; // 30° below horizon = 120° from radial up

    public static (float3, quaternion) CalculateNext(LocalTransform transform, GuidePathComponent guidePath,
        float speed, float planetRadius, float deltaTime)
    {
        var currentHeight = math.length(transform.Position) - planetRadius;
        var distToEnd = math.length(transform.Position - guidePath.EndPoint);

        var phase = DetermineFlightPhase(currentHeight, guidePath.TargetAltitude, distToEnd, planetRadius);

        return Calculate(transform, guidePath, speed, deltaTime, phase);
    }

    private static FlightPhase DetermineFlightPhase(float currentHeight, float targetHeight, float distToEnd,  float planetRadius)
    {
        var descentStartDist = targetHeight * 1.5f + (planetRadius * 0.3f);

        if (distToEnd <= descentStartDist || currentHeight >= targetHeight * 1.1) return FlightPhase.Descending;

        // We are in cruising state if we are at target height, within 10% tolerance  
        if (math.abs(currentHeight - targetHeight) <= targetHeight * 0.1f) return FlightPhase.Cruising;

        return FlightPhase.Climbing;
    }

    private static (float3, quaternion) Calculate(LocalTransform transform, GuidePathComponent guidePath,
        float speed, float deltaTime, FlightPhase phase)
    {
        var up = math.normalize(transform.Position);
        var forward = math.forward(transform.Rotation);

        var targetPoint = CalculateTargetPoint(guidePath, phase);
        var toTarget = targetPoint - transform.Position;

        // Clamp toTarget to respect climb/descent angle limits
        toTarget = ClampToAngleLimits(toTarget, up, phase);
        var desiredDirection = math.normalize(toTarget);

        var (newForward, newUp) = SmoothTurn(up, forward, desiredDirection);

        var newRotation = quaternion.LookRotation(newForward, newUp);
        var newPosition = transform.Position + newForward * speed * deltaTime;

        return (newPosition, newRotation);
    }

    private static float3 CalculateTargetPoint(GuidePathComponent guidePath, FlightPhase phase)
    {
        if (phase == FlightPhase.Descending) return guidePath.EndPoint;
        return guidePath.EndPoint + math.normalize(guidePath.EndPoint) * guidePath.TargetAltitude;
    }

    private static float3 ClampToAngleLimits(float3 direction, float3 up, FlightPhase phase)
    {
        var dotProduct = math.clamp(math.dot(math.normalize(direction), up), -1f, 1f);
        var currentAngle = math.acos(dotProduct);

        float minAngle, maxAngle;

        switch (phase)
        {
            case FlightPhase.Climbing:
                minAngle = math.radians(minClimbAngleFromUp);
                maxAngle = math.radians(maxClimbAngleFromUp);
                break;

            case FlightPhase.Cruising:
                minAngle = math.radians(90f);
                maxAngle = math.radians(95f);
                break;

            case FlightPhase.Descending:
                minAngle = math.radians(minDescentAngleFromUp);
                maxAngle = math.radians(maxDescentAngleFromUp);
                break;

            default:
                minAngle = math.radians(minClimbAngleFromUp);
                maxAngle = math.radians(maxDescentAngleFromUp);
                break;
        }

        if (currentAngle >= minAngle && currentAngle <= maxAngle) return direction;

        // Clamp to nearest boundary
        var clampedAngle = math.clamp(currentAngle, minAngle, maxAngle);

        // Rebuild direction at clamped angle
        var horizontalDir = math.normalize(direction - up * math.dot(direction, up));
        var clampedDirection = math.cos(clampedAngle) * up + math.sin(clampedAngle) * horizontalDir;

        return clampedDirection;
    }

    private static (float3, float3) SmoothTurn(float3 up, float3 forward, float3 desiredDirection)
    {
        var dotProduct = math.clamp(math.dot(forward, desiredDirection), -1f, 1f);
        var angle = math.acos(dotProduct);

        if (angle < math.radians(maxDirChangePerFrame)) return (desiredDirection, up);

        var turnAxis = math.normalize(math.cross(forward, desiredDirection));

        // Limit turn to max change per frame
        var turnAngle = math.min(angle, math.radians(maxDirChangePerFrame));
        var limitedTurn = quaternion.AxisAngle(turnAxis, turnAngle);
        var newForward = math.normalize(math.mul(limitedTurn, forward));

        // Update up vector to maintain perpendicularity with new forward
        // Project onto plane perpendicular to newForward, then normalize
        var newUp = up - newForward * math.dot(up, newForward);
        newUp = math.normalize(newUp);

        return (newForward, newUp);
    }

    private enum FlightPhase
    {
        Climbing,
        Cruising,
        Descending
    }
}