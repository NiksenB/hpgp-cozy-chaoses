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
            float height = math.length(transform.Position - new float3(planetRadius, planetRadius, planetRadius));
            float targetLandingDist = guidePath.TargetHeight * 2;
            
            if (math.length(transform.Position - guidePath.EndPoint) >= targetLandingDist)
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
            float3 up = math.normalize(transform.Position);
            float3 toDest = guidePath.EndPoint - transform.Position;
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
            float3 surfaceTangent = math.normalize(toDest - up * math.dot(toDest, up));

            float3 rotationAxis = math.normalize(math.cross(up, surfaceTangent));
            float rotationAngle = deltaTime * planeSpeed / (planetRadius + guidePath.TargetHeight);

            quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
            float3 newDirection = math.mul(rot, up);
            float3 newSurfacePos = newDirection * (planetRadius + guidePath.TargetHeight);

            // NEW ROTATION
            float3 forward = math.normalize(newSurfacePos - transform.Position);
            quaternion newRotation = quaternion.LookRotation(forward, up);

            return (newSurfacePos, newRotation);
        }
        
        private static (float3, quaternion) GoUpwards(LocalTransform transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            float3 up = math.normalize(transform.Position);
            float3 toDest = guidePath.EndPoint - transform.Position;
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
            float3 surfaceTangent = math.normalize(toDest - up * math.dot(toDest, up));

            float3 rotationAxis = math.normalize(math.cross(up, surfaceTangent));
            float rotationAngle = deltaTime * planeSpeed / (planetRadius + guidePath.TargetHeight);

            quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
            float3 newDirection = math.mul(rot, up);
            float3 newSurfacePos = newDirection * math.min(planetRadius + guidePath.TargetHeight, math.length(transform.Position)*1.0001f);

            // NEW ROTATION
            float3 forward = math.normalize(newSurfacePos - transform.Position);
            quaternion newRotation = quaternion.LookRotation(forward, up);

            return (newSurfacePos, newRotation);
        }
        
        private static (float3, quaternion) GoDownwards(LocalTransform transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            float3 up = math.normalize(transform.Position);
            float3 toDest = guidePath.EndPoint - transform.Position;
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
            float3 surfaceTangent = math.normalize(toDest - up * math.dot(toDest, up));

            float3 rotationAxis = math.normalize(math.cross(up, surfaceTangent));
            float rotationAngle = deltaTime * planeSpeed / (planetRadius + guidePath.TargetHeight);

            quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
            float3 newDirection = math.mul(rot, up);
            float3 newSurfacePos = newDirection * math.max(planetRadius+1f, math.length(transform.Position)*0.9999f);

            // NEW ROTATION
            float3 forward = math.normalize(newSurfacePos - transform.Position);
            quaternion newRotation = quaternion.LookRotation(forward, up);

            return (newSurfacePos, newRotation);
        }

        private static (float3, quaternion) GoLand(LocalTransform transform, GuidePathComponent guidePath, float planetRadius, float deltaTime)
        {
            float3 up = math.normalize(transform.Position);
            float3 toDest = guidePath.EndPoint - transform.Position;
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
            float3 surfaceTangent = math.normalize(toDest - up * math.dot(toDest, up));

            float3 rotationAxis = math.normalize(math.cross(up, surfaceTangent));
            float rotationAngle = deltaTime * planeSpeed / (planetRadius + guidePath.TargetHeight);

            quaternion rot = quaternion.AxisAngle(rotationAxis, rotationAngle);
            float3 newDirection = math.mul(rot, up);
            float3 newSurfacePos = newDirection * math.max(planetRadius+1f, math.length(transform.Position)*0.99f);

            // NEW ROTATION
            float3 forward = math.normalize(newSurfacePos - transform.Position);
            quaternion newRotation = quaternion.LookRotation(forward, up);

            return (newSurfacePos, newRotation);
        }
    }
