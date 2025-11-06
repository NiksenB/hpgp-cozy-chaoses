using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
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
        var deltaTime = (float)SystemAPI.Time.DeltaTime;

        state.Dependency = new MovePlanes
        {
            ECB = ecb,
            DeltaTime = deltaTime,
            Planet = planet
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
[WithNone(typeof(ShouldDespawnComponent))]
public partial struct MovePlanes : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float DeltaTime;
    public PlanetComponent Planet;

    public void Execute(Entity entity, ref LocalTransform transform, ref PlaneFlightDataComponent flightDataComponent,
        ref PlaneFlightDebugDataComponent debugData, ref PhysicsVelocity velocity, in PhysicsMass mass,
        in PlaneComponent plane)
    {
        float sphereRadius = Planet.Radius;
        float3 sphereCenter = Planet.Center;
        float earthScale = Planet.EarthScale;

        Vector3 planeNormal = transform.Forward();
        
        float3 currentPosition = transform.Position;
        float3 currentPositionOnSphere = math.normalize(currentPosition - sphereCenter);

        float3 destinationPositon = plane.Dest;
        float3 destinationPositionOnSphere = math.normalize(destinationPositon - sphereCenter);
        
        float3 toDestination = destinationPositionOnSphere - currentPositionOnSphere;
        float3 tangentDirection = math.normalize(Vector3.ProjectOnPlane(toDestination, currentPositionOnSphere));
        
        float3 forwardOnSphere = math.normalize(Vector3.ProjectOnPlane(planeNormal, currentPositionOnSphere));
        float targetAngle = Vector3.SignedAngle(forwardOnSphere, tangentDirection, currentPositionOnSphere);
        
        float angleRadians = Mathf.Acos(Vector3.Dot(planeNormal.normalized, currentPositionOnSphere));
        float angleDegrees = angleRadians * Mathf.Rad2Deg;
        float currentPitchAngle = 90f - angleDegrees;
        
        float currentSpeed = math.length(velocity.Linear);
        float currentAltitude = math.length(currentPosition - sphereCenter) - sphereRadius;
        
        UpdateFlightPhase(ref flightDataComponent, currentSpeed);
        
        if (flightDataComponent.CurrentPhase == FlightPhase.Climb)
        {
            ECB.RemoveComponent<PlaneComponent>(entity);
            velocity.Linear = new float3(0, 0, 0);
            return;
        }
        
        float targetSpeed = GetTargetSpeed(flightDataComponent) * earthScale;
        float targetAltitude = GetTargetAltitude(flightDataComponent, sphereRadius) * earthScale;
        
        debugData.AccelerationVector = math.max(ApplyForwardThrust(ref velocity, mass, earthScale, planeNormal, flightDataComponent.Thrust,
            DeltaTime, currentSpeed, targetSpeed), debugData.AccelerationVector);
        
        
        
        
        
        
        
        
        debugData.CurrentPosition = currentPosition;
        debugData.CurrentPositionOnSphere = currentPositionOnSphere;
        debugData.DestinationPositon = destinationPositon;
        debugData.DestinationPositionOnSphere = destinationPositionOnSphere;
        debugData.ToDestination = toDestination;
        debugData.TangentDirection = tangentDirection;
        debugData.ForwardOnSphere = forwardOnSphere;
        debugData.AngleRadians = angleRadians;
        debugData.AngleDegrees = angleDegrees;
        debugData.CurrentPitchAngle = currentPitchAngle;
        debugData.PlaneNormal = planeNormal;

        debugData.CurrentAltitude = currentAltitude;
        debugData.TargetAltitude = targetAltitude;
        debugData.CurrentSpeed = currentSpeed;
        debugData.TargetSpeed = targetSpeed;
        // debugData.CurrentAngle = ;
        debugData.TargetAngle = targetAngle;
        debugData.CurrentPitch = currentPitchAngle;
        // debugData.TargetPitch;
        debugData.InverseMass = mass.InverseMass;
        debugData.EarthScale = earthScale;
    }

    private float GetTargetAltitude(in PlaneFlightDataComponent flightData, float sphereRadius)
    {
        return flightData.CurrentPhase switch
        {
            FlightPhase.TakeOff or FlightPhase.Climb or FlightPhase.Cruise => flightData.CruisingAltitudePercentage *
                                                                              sphereRadius,
            _ => 0f
        };
    }

    private float GetTargetSpeed(in PlaneFlightDataComponent flightData)
    {
        return flightData.CurrentPhase switch
        {
            FlightPhase.TakeOff => flightData.V1Speed,
            FlightPhase.Climb or FlightPhase.Cruise => flightData.MaxSpeed,
            _ => 0f
        };
    }

    private void UpdateFlightPhase(ref PlaneFlightDataComponent flightData, float currentSpeed)
    {
        switch (flightData.CurrentPhase)
        {
            case FlightPhase.TakeOff:
                if (currentSpeed >=       flightData.V1Speed)
                {
                    flightData.CurrentPhase = FlightPhase.Climb;
                }

                break;
        }
    }

    private float3 ApplyForwardThrust(ref PhysicsVelocity vel, in PhysicsMass mass, float earthScale, float3 forwardDirection,
        float thrustForce, float deltaTime, float currentSpeed, float targetSpeed)
    {
        float thrustNewtons = (thrustForce * 1000f) * earthScale;
        float3 thrustVector = forwardDirection * thrustNewtons;
        
        float speedError = targetSpeed - currentSpeed;
        float3 thrust = math.clamp(speedError, -1, 1) * thrustVector;
        float3 accelerationVector = thrust * mass.InverseMass;
        
        vel.Linear += accelerationVector * deltaTime;
        return accelerationVector;
    }
    
    //     float3 currentUp = math.normalize(currentPosition - sphereCenter);
        //     float3 currentForward = transform.Forward();
        //     float3 currentDirection = math.normalize(currentPosition - sphereCenter);
        //     float currentAltitude = math.length(currentPosition - sphereCenter) - sphereRadius;
        //     float currentSpeed = math.length(velocity.Linear);
        //
        //     float cruisingAltitude = sphereRadius * flightDataComponent.CruisingAltitudePercentage;
        //
        //     float3 targetDirectionOnSphere = GetGreatCircleDirection(currentPosition, plane.Dest, sphereCenter);
        //
        //     UpdateFlightPhase(ref flightDataComponent, plane, cruisingAltitude, currentPosition, currentAltitude);
        //     if (flightDataComponent.CurrentPhase == FlightPhase.Landing)
        //     {
        //         ECB.AddComponent(entity, new ShouldDespawnComponent());
        //         return;
        //     }
        //
        //     switch (flightDataComponent.CurrentPhase)
        //     {
        //         case FlightPhase.TakeOff:
        //             ApplyForceToVelocity(ref velocity, mass, flightDataComponent.Acceleration, DeltaTime, currentForward);
        //
        //             float takeoffLift = currentSpeed * 0.5f;
        //             ApplyForceToVelocity(ref velocity, mass, takeoffLift, DeltaTime, currentUp);
        //
        //             break;
        //         case FlightPhase.Climb:
        //             TurnTowardsTarget(flightDataComponent, ref velocity, mass, transform, targetDirectionOnSphere,
        //                 currentUp, DeltaTime);
        //
        //             ApplyThrust(ref velocity, mass, currentForward, currentSpeed, flightDataComponent.MaxSpeed,
        //                 flightDataComponent.Acceleration, DeltaTime);
        //
        //             float climbForce = flightDataComponent.ClimbRate * 50f;
        //             ApplyForceToVelocity(ref velocity, mass, climbForce, DeltaTime, currentUp);
        //             break;
        //         case FlightPhase.Cruise:
        //             TurnTowardsTarget(flightDataComponent, ref velocity, mass, transform, targetDirectionOnSphere,
        //                 currentUp, DeltaTime);
        //
        //             ApplyThrust(ref velocity, mass, currentForward, currentSpeed, flightDataComponent.MaxSpeed,
        //                 flightDataComponent.Acceleration, DeltaTime);
        //
        //             float altitudeError = cruisingAltitude - currentAltitude;
        //             float liftForce = altitudeError * 10f; // Proportional lift
        //             ApplyForceToVelocity(ref velocity, mass, liftForce, DeltaTime, currentUp);
        //             break;
        //     }
        // }
        //
        // private void TurnTowardsTarget(in PlaneFlightDataComponent flightDataComponent, ref PhysicsVelocity velocity,
        //     in PhysicsMass mass, in LocalTransform tranform, float3 targetDirection, float3 planetUpDirection,
        //     float deltaTime)
        // {
        //     float3 currentUp = tranform.Up();
        //     float3 currentForward = tranform.Forward();
        //
        //     float3 stabilityTorque = math.cross(currentUp, planetUpDirection) * flightDataComponent.StabilityStrength;
        //
        //     float3 projectedTarget = targetDirection - math.dot(targetDirection, currentUp) * currentUp;
        //     projectedTarget = math.normalize(projectedTarget);
        //
        //     float3 yawTorque = math.cross(currentForward, projectedTarget) * flightDataComponent.SteerStrength;
        //
        //     float3 totalTorque = stabilityTorque + yawTorque;
        //
        //     // apply steering / stability torques
        //     ApplyTorque(ref velocity, mass, totalTorque, deltaTime);
        //
        //     // apply exponential angular damping (avoids double-integrating damping as a torque)
        //     float dampingFactor = math.exp(-flightDataComponent.AngularDamping * deltaTime);
        //     velocity.Angular *= dampingFactor;
        // }
        //
        // // Simplified PID for thrust
        // void ApplyThrust(ref PhysicsVelocity vel, PhysicsMass mass, float3 currentDirection, float currentSpeed,
        //     float targetSpeed, float acceleration, float deltaTime)
        // {
        //     float error = targetSpeed - currentSpeed;
        //     // Simple P-controller: force is proportional to error
        //     float thrust = math.clamp(error, -1, 1) * acceleration;
        //     ApplyForceToVelocity(ref vel, mass, thrust, deltaTime, currentDirection);
        // }
        //
        // void ApplyTorque(ref PhysicsVelocity vel, in PhysicsMass mass, float3 torque, float deltaTime)
        // {
        //     vel.Angular += mass.InverseInertia * torque * deltaTime;
        // }
        //
        // private void UpdateFlightPhase(ref PlaneFlightDataComponent flightDataComponent, in PlaneComponent plane,
        //     float cruisingAltitude, float3 currentPosition, float currentAltitude)
        // {
        //     switch (flightDataComponent.CurrentPhase)
        //     {
        //         case FlightPhase.TakeOff:
        //             if (currentAltitude >= cruisingAltitude * 0.1f)
        //             {
        //                 flightDataComponent.CurrentPhase = FlightPhase.Cruise;
        //             }
        //
        //             break;
        //         case FlightPhase.Climb:
        //             if (currentAltitude >= cruisingAltitude * 0.95f)
        //             {
        //                 flightDataComponent.CurrentPhase = FlightPhase.Cruise;
        //             }
        //
        //             break;
        //         case FlightPhase.Cruise:
        //             float distanceToTarget = math.distance(currentPosition, plane.Dest);
        //             // if (distanceToTarget <= 1000f)
        //             // {
        //             //     flightDataComponent.CurrentPhase = FlightPhase.Descent;
        //             // }
        //
        //             break;
        //         case FlightPhase.Descent:
        //             if (currentAltitude <= currentAltitude * 0.1f)
        //             {
        //                 flightDataComponent.CurrentPhase = FlightPhase.Landing;
        //             }
        //
        //             break;
        //         case FlightPhase.Landing:
        //             break;
        //     }
    // }
}