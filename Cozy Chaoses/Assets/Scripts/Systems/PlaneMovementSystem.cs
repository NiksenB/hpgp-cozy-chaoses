using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

public partial struct PlaneMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PlanetComponent>();
        state.RequireForUpdate<ConfigComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var planet = SystemAPI.GetSingleton<PlanetComponent>();
        var deltaTime = SystemAPI.Time.DeltaTime;

        state.Dependency = new MovePlanes
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(PlaneComponent))]
[WithNone(typeof(ShouldDespawnComponent))]
public partial struct MovePlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;


    public void Execute(Entity entity, ref LocalTransform transform, ref PhysicsVelocity velocity, in PlaneComponent plane,
        ref PlaneFlightComponent planeFlight)
    {
        Debug.Log("Moving Plane Entity: " + entity.Index + " planet scale: " + Planet.ScaleFactor);
        if (planeFlight.MaxAltitude == 0f)
        {
            var minAltitude = Planet.Radius * planeFlight.MinAltitudeRatio;
            var maxAltitude = Planet.Radius * planeFlight.MaxAltitudeRatio;
            // TODO: Use a better random method
            planeFlight.MaxAltitude = (minAltitude + maxAltitude) / 2;
        }

        var sphereRadius = Planet.Radius;
        var sphereCenter = Planet.Center;

        var position = transform.Position;
        float3 currentVel = velocity.Linear; 
        var destination = plane.Dest;

        var toPlanet = sphereCenter - position;
        var up = math.normalize(-toPlanet);

        // // Despawn if has arrived. No need to calculate further.
        // if (HasArrived(position, destination, sphereCenter))
        // {
        //     ECB.AddComponent(entity, new ShouldDespawnComponent());
        //     return;
        // }

        // Update Flight State
        var currentAltitude = math.length(toPlanet) - sphereRadius;

        planeFlight.State = GetFlightState(ref planeFlight, currentAltitude);

        // Get target altitude based on state
        var targetAltitude = GetTargetAltitude(in planeFlight);

        // Create or update movement curve
        // TODO: Handle different curve types
        if (planeFlight.CurrentCurve == null)
        {
            // 100 meters straight ahead
            var forward = transform.Forward();
            var endPos = position + forward * EarthScale(100f);

            CreateNewCurve(ref planeFlight, position, endPos, targetAltitude);
        }

        // Update curve progress
        var curve = planeFlight.CurrentCurve.Value;
        curve.Progress += planeFlight.CurrentCurve.Value.Speed * DeltaTime;
        curve.Progress = math.saturate(curve.Progress);

        // Get targets for error calculation
        var targetPos = EvaluateSigmoidCurve(curve);
        var targetTangent = CalculateCurveTangent(curve);
        var targetVelocity = math.normalize(targetTangent) * curve.Speed;
        
        // Calculate errors
        float3 positionError = targetPos - position;
        float positionErrorMag = math.length(positionError);
        
        PhysicsJoint.CreatePositionMotor(
            new BodyFrame
            {
                Axis = math.normalize(targetTangent),
                PerpendicularAxis = math.cross(math.normalize(targetTangent), up),
                Position = float3.zero
            },
            new BodyFrame
            {
                Axis = float3.zero,
                PerpendicularAxis = float3.zero,
                Position = float3.zero
            },
            positionErrorMag,
            500f,
            5f,
            0.7f
        );

        planeFlight.CurrentCurve = curve;
    }

    private void CreateNewCurve(ref PlaneFlightComponent planeFlight, float3 startPos, float3 endPos,
        float targetAltitude)
    {
        planeFlight.CurrentCurve = new CurveData
        {
            Progress = 0f,
            Speed = 0.1f, // Percent of curve
            Type = CurveType.Sigmoid,
            StartPos = startPos,
            EndPos = endPos,
            Height = targetAltitude,
            K = 8f
        };
    }

    private static float CalcSigmoid(in CurveData curve)
    {
        return 1f / (1f + math.exp(-curve.K * (curve.Progress - 0.5f)));
    }

    private static float3 EvaluateSigmoidCurve(in CurveData curve)
    {
        var start = curve.StartPos;

        var sigmoid = CalcSigmoid(curve);

        var horizontal = math.lerp(start, curve.EndPos, curve.Progress);
        horizontal.y = start.y + sigmoid * curve.Height;
        return horizontal;
    }

    private static float3 CalculateCurveTangent(in CurveData curve)
    {
        var sigmoid = CalcSigmoid(curve);

        // Derivative of sigmoid: k * sigmoid * (1 - sigmoid)
        var sigmoidDeriv = curve.K * sigmoid * (1f - sigmoid);

        var tangent = curve.EndPos - curve.StartPos;
        tangent.y = sigmoidDeriv * curve.Height;

        return tangent;
    }


    private bool HasArrived(float3 pos, float3 dest, float3 sphereCenter)
    {
        var currentDir = math.normalize(pos - sphereCenter);
        var destDir = math.normalize(dest - sphereCenter);

        return math.dot(currentDir, destDir) > 0.999f;
    }

    private float GetTargetAltitude(in PlaneFlightComponent planeFlight)
    {
        return EarthScale(planeFlight.State switch
        {
            PlaneState.TakeOff or PlaneState.Climb or PlaneState.Cruise => planeFlight.MaxAltitude,
            PlaneState.Descent => 0f,
            PlaneState.Landing => 0f,
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private PlaneState GetFlightState(ref PlaneFlightComponent plane, float currentAltitude)
    {
        switch (plane.State)
        {
            case PlaneState.TakeOff:
                return PlaneState.Climb;
            case PlaneState.Climb:
                if (currentAltitude >= plane.MaxAltitude * 0.99f) return PlaneState.Cruise;
                break;

            case PlaneState.Cruise:
                break;
            case PlaneState.Descent:
                if (currentAltitude <= 0f) return PlaneState.Landing;
                break;

            case PlaneState.Landing:
                // Stay in landing state until HasArrived() triggers despawn
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return plane.State;
    }

    private float3 EarthScale(float3 toScale)
    {
        return toScale * Planet.ScaleFactor;
    }

    private float EarthScale(float toScale)
    {
        return toScale * Planet.ScaleFactor;
    }
}