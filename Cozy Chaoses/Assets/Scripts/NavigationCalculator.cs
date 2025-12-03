using System;
using System.Net;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


    public class NavigationCalculator
    {
        private static readonly float planeSpeed = 4f;
        
        private float flyDegLimit = 50f;  
        private float frameChangeDegLimit = 0.5f;
        public static (float3, quaternion) CalculateNext(LocalTransform  transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            float3 position = transform.Position;
            float height = math.length(position - new float3(planetRadius, planetRadius, planetRadius));
            float targetLandingDist = guidePath.TargetHeight * 2;
            
            if (math.length(position - guidePath.EndPoint) >= targetLandingDist)
            {
                var hDiff = height - guidePath.TargetHeight;
                    
                if (math.abs(hDiff) <= 5f)
                {
                    return GoForward(transform, guidePath, planetRadius, deltaTime);
                }
                
                if (hDiff > 5f)
                {
                    return GoUpwards(transform, guidePath, planetRadius, deltaTime);
                }

                return GoDownwards(transform, guidePath, planetRadius, deltaTime);
            }
            
            return GoLand(transform, guidePath, planetRadius, deltaTime);
        }

        private static (float3, quaternion) GoForward(LocalTransform transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            float3 position = transform.Position;
            float3 toCenter = math.normalize(position);
            float3 up = math.normalize(position);
            float3 toDest = guidePath.EndPoint - position;
            float3 currentForward = math.normalize(math.forward(transform.Rotation.value));
            var dot = math.dot(toDest, currentForward);

            if (dot < 0.99f)
            {
                // adjust course so we only rotate 2 degrees
                float3 cross = math.cross(currentForward, toDest);
                float sign = math.sign(math.dot(cross, up));
                quaternion smallTurn = quaternion.AxisAngle(up, math.radians(2f) * sign);
                toDest = math.mul(smallTurn, currentForward);
            }

            // NEW POSITION
            float3 surfaceTangent = math.normalize(toDest - toCenter * math.dot(toDest, toCenter));

            float3 rotationAxis = math.normalize(math.cross(toCenter, surfaceTangent));
            float rotationAngle = deltaTime * planeSpeed / (planetRadius + guidePath.TargetHeight);

            quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
            float3 newDirection = math.mul(rot, toCenter);
            float3 newSurfacePos = newDirection * (planetRadius + guidePath.TargetHeight);

            // NEW ROTATION
            float3 forward = math.normalize(newSurfacePos - position);
            quaternion newRotation = quaternion.LookRotation(forward, up);

            return (newSurfacePos, newRotation);
        }
        
        private static (float3, quaternion) GoUpwards(LocalTransform transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            return GoForward(transform, guidePath, planetRadius, deltaTime);

        }
        
        private static (float3, quaternion) GoDownwards(LocalTransform transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            return GoForward(transform, guidePath, planetRadius, deltaTime);
        }

        private static (float3, quaternion) GoLand(LocalTransform transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            return GoForward(transform, guidePath, planetRadius, deltaTime);
        }
    }
