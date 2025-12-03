using System;
using System.Net;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


    public class NavigationCalculator
    {
        private enum Dir { Up, Down, Forward, End }

        private static readonly float Speed = 5f;
        
        private static readonly float VerticalDegLimit = 50f;  
        private static readonly float FrameChangeDegLimit = 0.5f;
        public static (float3, quaternion) CalculateNext(LocalTransform  transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            float height = math.length(transform.Position - new float3(planetRadius, planetRadius, planetRadius));
            float targetLandingDist = guidePath.TargetHeight * 2;
            
            if (math.length(transform.Position - guidePath.EndPoint) >= targetLandingDist)
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
            float3 toDest = guidePath.EndPoint - transform.Position;
            float3 currentForward = math.normalize(math.forward(transform.Rotation.value));
            var dot = math.dot(toDest, currentForward);

            if (dot < 0.99f)
            {
                // limit horizontal rotation pr frame
                float3 cross = math.cross(currentForward, toDest);
                float sign = math.sign(math.dot(cross, up));
                quaternion smallTurn = quaternion.AxisAngle(up, math.radians(FrameChangeDegLimit) * sign);
                toDest = math.mul(smallTurn, currentForward);
            }

            // NEW POSITION
            float3 surfaceTangent = math.normalize(toDest - up * math.dot(toDest, up));

            float3 rotationAxis = math.normalize(math.cross(up, surfaceTangent));
            float rotationAngle = deltaTime * Speed / (planetRadius + guidePath.TargetHeight);

            quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
            float3 newDirection = math.mul(rot, up);

            float3 newSurfacePos = dir switch
            {
                Dir.Forward => newDirection * (planetRadius + guidePath.TargetHeight),
                Dir.Up => newDirection * math.min(planetRadius + guidePath.TargetHeight, math.length(transform.Position)*1.0001f),
                Dir.Down => newDirection * math.max(planetRadius+1f, math.length(transform.Position)*0.9999f),
                Dir.End => newDirection * math.max(planetRadius, math.length(transform.Position)*0.9999f),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            // NEW ROTATION
            float3 forward = math.normalize(newSurfacePos - transform.Position);
            quaternion newRotation = quaternion.LookRotation(forward, up);

            return (newSurfacePos, newRotation);
        }
    }
