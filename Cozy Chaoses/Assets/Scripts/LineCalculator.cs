using System;
using Unity.Mathematics;

namespace DefaultNamespace
{
    public class LineCalculator
    {
        public static float3 Calculate(GuidePathComponent guidePath, float t)
        {
            return guidePath.Shape switch
            {
                PathShape.Linear => CalculateLinear(guidePath, t),
                PathShape.SineWave => CalculateSineWave(guidePath, t),
                // PathShape.Sigmoid => expr,
                PathShape.Curve => CalculateCurve(guidePath, t),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        protected static float3 CalculateLinear(GuidePathComponent guidePath, float t)
        {
            return math.lerp(guidePath.StartPoint, guidePath.EndPoint, t);
        }

        protected static float3 CalculateSineWave(GuidePathComponent guidePath, float t)
        {
            
            float3 forwardDir = math.normalize(guidePath.EndPoint - guidePath.StartPoint);
            float3 rightDir = math.cross(forwardDir, math.up());
            float3 upDir = math.cross(rightDir, forwardDir);
            
            float3 linearPos = math.lerp(guidePath.StartPoint, guidePath.EndPoint, t);
            float sineOffset = math.sin(t * math.PI * 2 * guidePath.Frequency) * guidePath.AmplitudeOrSteepness;
            return linearPos + (upDir * sineOffset);
        }

        protected static float3 CalculateCurve(GuidePathComponent guidePath, float t)
        {
            // Quadratic Bezier
            float u = 1 - t;
            return (u * u * guidePath.StartPoint) + (2 * u * t * guidePath.ControlPoint) + (t * t * guidePath.EndPoint);
        }
    }
}