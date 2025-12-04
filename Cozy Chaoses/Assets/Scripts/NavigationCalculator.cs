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
    
    private const float MaxDirChangePerFrame = 0.5f; 
    
    private const float MinClimbAngleFromUp = 40f; // 50° above horizon = 40° from radial up
    private const float MaxClimbAngleFromUp = 70f; // 40° above horizon = 50° from radial up
    private const float MinDescentAngleFromUp = 90f; // 0° below horizon = 90° from radial up. Allows "cruising".
    private const float MaxDescentAngleFromUp = 120f; // 30° below horizon = 120° from radial up

    public static (float3, quaternion) CalculateNext(LocalTransform transform, GuidePathComponent guidePath,
        float speed, float planetRadius, float deltaTime)
    {
        float currentHeight = math.length(transform.Position) - planetRadius;
        float distToEnd = math.length(transform.Position - guidePath.EndPoint);
        
        FlightPhase phase = DetermineFlightPhase(currentHeight, guidePath.TargetAltitude, distToEnd);
        
        return Calculate(transform, guidePath, speed, deltaTime, phase);
    }
    
    private static FlightPhase DetermineFlightPhase(float currentHeight, float targetHeight, float distToEnd)
    {
        float descentStartDist = targetHeight * 2.5f;
        
        if (distToEnd <= descentStartDist)
        {
            return FlightPhase.Descending;
        }
        
        // We are in cruising state if we are at target height, within 10% tolerance  
        if (math.abs(currentHeight - targetHeight) <= targetHeight * 0.1f) 
        {
            return FlightPhase.Cruising;
        }
        
        return FlightPhase.Climbing;
    }
    
    private static (float3, quaternion) Calculate(LocalTransform transform, GuidePathComponent guidePath,
        float speed, float deltaTime, FlightPhase phase)
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
        float3 newPosition = transform.Position + newForward * speed * deltaTime;
        
        return (newPosition, newRotation);
    }
    
    private static float3 CalculateTargetPoint(GuidePathComponent guidePath, FlightPhase phase)
    {
        if (phase == FlightPhase.Descending)
        {
            return guidePath.EndPoint;
        }
        return guidePath.EndPoint + math.normalize(guidePath.EndPoint) * guidePath.TargetAltitude;
    }
    
private static float3 ClampToAngleLimits(float3 direction, float3 up, FlightPhase phase)
{
    float dotProduct = math.clamp(math.dot(math.normalize(direction), up), -1f, 1f);
    float currentAngle = math.acos(dotProduct);
    
    float minAngle, maxAngle;
    
    switch (phase)
    {
        case FlightPhase.Climbing:
            minAngle = math.radians(MinClimbAngleFromUp);
            maxAngle = math.radians(MaxClimbAngleFromUp);
            break;
            
        case FlightPhase.Cruising:
            minAngle = math.radians(90f);  
            maxAngle = math.radians(95f);
            break;
            
        case FlightPhase.Descending:
            minAngle = math.radians(MinDescentAngleFromUp);
            maxAngle = math.radians(MaxDescentAngleFromUp);
            break;
            
        default:
            minAngle = math.radians(MinClimbAngleFromUp);
            maxAngle = math.radians(MaxDescentAngleFromUp);
            break;
    }
    
    if (currentAngle >= minAngle && currentAngle <= maxAngle)
    {
        return direction;
    } 
    
    // Clamp to nearest boundary
    float clampedAngle = math.clamp(currentAngle, minAngle, maxAngle);
    
    // Rebuild direction at clamped angle
    float3 horizontalDir = math.normalize(direction - up * math.dot(direction, up));
    float3 clampedDirection = math.cos(clampedAngle) * up + math.sin(clampedAngle) * horizontalDir;
    
    return clampedDirection;
}
    
    private static (float3, float3) SmoothTurn(float3 up, float3 forward, float3 desiredDirection)
    {
        float dotProduct = math.clamp(math.dot(forward, desiredDirection), -1f, 1f);
        float angle = math.acos(dotProduct);
        
        if (angle < math.radians(MaxDirChangePerFrame))
        {
            return (desiredDirection, up);
        }
        
        float3 turnAxis = math.normalize(math.cross(forward, desiredDirection));
        
        // Limit turn to max change per frame
        float turnAngle = math.min(angle, math.radians(MaxDirChangePerFrame));
        quaternion limitedTurn = quaternion.AxisAngle(turnAxis, turnAngle);
        float3 newForward = math.normalize(math.mul(limitedTurn, forward));
        
        // Update up vector to maintain perpendicularity with new forward
        // Project onto plane perpendicular to newForward, then normalize
        float3 newUp = up - newForward * math.dot(up, newForward);
        newUp = math.normalize(newUp);
        
        return (newForward, newUp);
    }
}