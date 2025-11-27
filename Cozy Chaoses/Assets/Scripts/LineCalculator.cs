using System;
using Unity.Mathematics;

namespace DefaultNamespace
{
    public class LineCalculator
    {
        public static float3 Calculate(PlanePathComponent planePath, float t)
        {
            return planePath.Shape switch
            {
                PathShape.Linear => CalculateLinear(planePath, t),
                PathShape.SineWave => CalculateSineWave(planePath, t),
                // PathShape.Sigmoid => expr,
                PathShape.Curve => CalculateCurve(planePath, t),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        protected static float3 CalculateLinear(PlanePathComponent planePath, float t)
        {
            return math.lerp(planePath.StartPoint, planePath.EndPoint, t);
        }

        protected static float3 CalculateSineWave(PlanePathComponent planePath, float t)
        {
            
            float3 forwardDir = math.normalize(planePath.EndPoint - planePath.StartPoint);
            float3 rightDir = math.cross(forwardDir, math.up());
            float3 upDir = math.cross(rightDir, forwardDir);
            
            float3 linearPos = math.lerp(planePath.StartPoint, planePath.EndPoint, t);
            float sineOffset = math.sin(t * math.PI * 2 * planePath.Frequency) * planePath.AmplitudeOrSteepness;
            return linearPos + (upDir * sineOffset);
        }

        protected static float3 CalculateCurve(PlanePathComponent planePath, float t)
        {
            // Quadratic Bezier
            float u = 1 - t;
            return (u * u * planePath.StartPoint) + (2 * u * t * planePath.ControlPoint) + (t * t * planePath.EndPoint);
        }
    }
}