using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class NavigationCalculator
{
    private enum FlightPhase
    {
        Climbing,
        Cruising,
        Descending
    }
    
    private const float Speed = 8f;
    private const float FrameChangeDegLimit = 0.5f;
    private const float MaxClimbAngleFromUp = 50f; // 40° above horizon = 50° from radial up
    private const float MaxDescentAngleFromUp = 120f; // 30° below horizon = 120° from radial up

    public static (float3, quaternion) CalculateNext(LocalTransform transform, GuidePathComponent guidePath,
        float planetRadius, float deltaTime)
    {
        float currentHeight = math.length(transform.Position) - planetRadius;
        float distToEnd = math.length(transform.Position - guidePath.EndPoint);
        
        FlightPhase phase = DetermineFlightPhase(currentHeight, guidePath.TargetHeight, distToEnd);
        
        return Calculate(transform, guidePath, planetRadius, deltaTime, phase);
    }
    
    private static FlightPhase DetermineFlightPhase(float currentHeight, float targetHeight, float distToEnd)
    {
        float descentStartDist = targetHeight * 2.5f;
        
        if (distToEnd <= descentStartDist)
        {
            return FlightPhase.Descending;
        }
        
        float heightDiff = currentHeight - targetHeight;
        if (math.abs(heightDiff) <= 5f)
        {
            return FlightPhase.Cruising;
        }
        
        return FlightPhase.Climbing;
    }
    
    private static (float3, quaternion) Calculate(LocalTransform transform, GuidePathComponent guidePath,
        float planetRadius, float deltaTime, FlightPhase phase)
    {
        float3 up = math.normalize(transform.Position);
        float3 forward = math.forward(transform.Rotation);
        
        float3 targetPoint = CalculateTargetPoint(guidePath, phase);
        float3 toTarget = targetPoint - transform.Position;
        
        // Clamp toTarget to respect climb/descent angle limits
        toTarget = ClampToAngleLimits(toTarget, up, phase);
        float3 desiredDirection = math.normalize(toTarget);
        
        (float3 newForward, float3 newUp) = SmoothTurn(up, forward, desiredDirection);
        
        quaternion newRotation = quaternion.LookRotation(newForward, newUp);
        float3 newPosition = transform.Position + newForward * Speed * deltaTime;
        
        return (newPosition, newRotation);
    }
    
    private static float3 CalculateTargetPoint(GuidePathComponent guidePath, FlightPhase phase)
    {
        if (phase == FlightPhase.Descending)
        {
            return guidePath.EndPoint;
        }
        return guidePath.EndPoint + math.normalize(guidePath.EndPoint) * guidePath.TargetHeight;
    }
    
private static float3 ClampToAngleLimits(float3 direction, float3 up, FlightPhase phase)
{
    // Calculate current angle from up vector
    float dotProduct = math.clamp(math.dot(math.normalize(direction), up), -1f, 1f);
    float angleFromUp = math.acos(dotProduct);
    
    // Determine allowed angle range based on phase
    float minAngle, maxAngle;
    
    switch (phase)
    {
        case FlightPhase.Climbing:
            // When climbing: 0° (straight up) to MaxClimbAngleFromUp (e.g., 50°)
            minAngle = 0f;
            maxAngle = math.radians(MaxClimbAngleFromUp);
            break;
            
        case FlightPhase.Cruising:
            // When cruising: horizontal flight (90° from up)
            minAngle = math.radians(80f);  // Slight tolerance around horizon
            maxAngle = math.radians(100f);
            break;
            
        case FlightPhase.Descending:
            // When descending: 90° (horizon) to MaxDescentAngleFromUp (e.g., 120°)
            minAngle = math.radians(90f);
            maxAngle = math.radians(MaxDescentAngleFromUp);
            break;
            
        default:
            minAngle = 0f;
            maxAngle = math.radians(180f);
            break;
    }
    
    // If within limits, return as-is
    if (angleFromUp >= minAngle && angleFromUp <= maxAngle)
    {
        return direction;
    }
    
    // Get horizontal component
    float3 horizontalDir = direction - up * math.dot(direction, up);
    
    // Avoid division by zero if direction is parallel to up
    if (math.length(horizontalDir) > 0.0001f)
    {
        horizontalDir = math.normalize(horizontalDir);
    }
    else
    {
        // If direction is parallel to up, pick an arbitrary horizontal direction
        horizontalDir = math.normalize(math.cross(up, new float3(0, 0, 1)));
        if (math.length(horizontalDir) < 0.0001f)
        {
            horizontalDir = math.normalize(math.cross(up, new float3(1, 0, 0)));
        }
    }
    
    // Clamp to the nearest boundary
    float clampedAngle = math.clamp(angleFromUp, minAngle, maxAngle);
    
    // Reconstruct direction at clamped angle
    float3 clampedDirection = math.cos(clampedAngle) * up + math.sin(clampedAngle) * horizontalDir;
    
    // Preserve original magnitude
    return clampedDirection * math.length(direction);
}
    
    private static (float3, float3) SmoothTurn(float3 up, float3 forward, float3 desiredDirection)
    {
        // Calculate angle between current and desired direction
        float dotProduct = math.clamp(math.dot(forward, desiredDirection), -1f, 1f);
        float angle = math.acos(dotProduct);
        
        // If already aligned closely enough, use desired direction
        if (angle < math.radians(FrameChangeDegLimit))
        {
            return (desiredDirection, up);
        }
        
        // Limit turn to max change per frame
        float turnAngle = math.min(angle, math.radians(FrameChangeDegLimit));
        float3 turnAxis = math.normalize(math.cross(forward, desiredDirection));
        
        // Apply limited turn
        quaternion limitedTurn = quaternion.AxisAngle(turnAxis, turnAngle);
        float3 newForward = math.normalize(math.mul(limitedTurn, forward));
        
        // Update up vector to maintain perpendicularity with new forward
        // Project onto plane perpendicular to newForward, then normalize
        float3 newUp = up - newForward * math.dot(up, newForward);
        newUp = math.normalize(newUp);
        
        return (newForward, newUp);
    }
}