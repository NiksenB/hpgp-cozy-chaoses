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
        ref PhysicsVelocity velocity, in PhysicsMass mass, in PlaneComponent plane)
    {
        float sphereRadius = Planet.Radius;
        float3 sphereCenter = Planet.Center;

        float3 currentPosition = transform.Position;
        float3 currentUp = math.normalize(currentPosition - sphereCenter);
        float3 currentForward = transform.Forward();
        float3 currentDirection = math.normalize(currentPosition - sphereCenter);
        float currentAltitude = math.length(currentPosition - sphereCenter) - sphereRadius;
        float currentSpeed = math.length(velocity.Linear);

        float3 targetDirectionOnSphere = GetGreatCircleDirection(currentPosition, plane.Dest, sphereCenter);

        UpdateFlightPhase(ref flightDataComponent, plane, currentPosition, currentAltitude);
        if (flightDataComponent.CurrentPhase == FlightPhase.Landing)
        {
            ECB.AddComponent(entity, new ShouldDespawnComponent());
            return;
        }

        switch (flightDataComponent.CurrentPhase)
        {
            case FlightPhase.TakeOff:
                ApplyForceToVelocity(ref velocity, mass, flightDataComponent.Acceleration, DeltaTime, currentForward);

                float takeoffLift = currentSpeed * 0.5f;
                ApplyForceToVelocity(ref velocity, mass, takeoffLift, DeltaTime, currentUp);

                break;
            case FlightPhase.Climb:
                TurnTowardsTarget(flightDataComponent, ref velocity, mass, transform, targetDirectionOnSphere,
                    currentUp, DeltaTime);

                ApplyThrust(ref velocity, mass, currentForward, currentSpeed, flightDataComponent.MaxSpeed,
                    flightDataComponent.Acceleration, DeltaTime);

                float climbForce = flightDataComponent.ClimbRate * 50f;
                ApplyForceToVelocity(ref velocity, mass, climbForce, DeltaTime, currentUp);
                break;
            case FlightPhase.Cruise:
                TurnTowardsTarget(flightDataComponent, ref velocity, mass, transform, targetDirectionOnSphere,
                    currentUp, DeltaTime);

                ApplyThrust(ref velocity, mass, currentForward, currentSpeed, flightDataComponent.MaxSpeed,
                    flightDataComponent.Acceleration, DeltaTime);

                float altitudeError = flightDataComponent.CruisingAltitude - currentAltitude;
                float liftForce = altitudeError * 10f; // Proportional lift
                ApplyForceToVelocity(ref velocity, mass, liftForce, DeltaTime, currentUp);
                break;
        }
    }

    private float3 GetGreatCircleDirection(float3 from, float3 to, float3 sphereCenter)
    {
        float3 fromDir = math.normalize(from - sphereCenter);
        float3 toDir = math.normalize(to - sphereCenter);

        float3 cross = math.cross(fromDir, toDir);
        float3 greatCircleDir = math.cross(cross, fromDir);

        return math.normalize(greatCircleDir);
    }

    private void ApplyForceToVelocity(ref PhysicsVelocity velocity, in PhysicsMass mass, float3 force, float deltaTime,
        float3 direction)
    {
        float3 acceleration = direction * force * deltaTime;
        float massf = 1f / mass.InverseMass;
        velocity.Linear += acceleration / massf;
    }

    private void TurnTowardsTarget(in PlaneFlightDataComponent flightDataComponent, ref PhysicsVelocity velocity,
        in PhysicsMass mass, in LocalTransform tranform, float3 targetDirection, float3 planetUpDirection,
        float deltaTime)
    {
float3 currentUp = tranform.Up();
    float3 currentForward = tranform.Forward();

    float3 stabilityTorque = math.cross(currentUp, planetUpDirection) * flightDataComponent.StabilityStrength;

    float3 projectedTarget = targetDirection - math.dot(targetDirection, currentUp) * currentUp;
    projectedTarget = math.normalize(projectedTarget);

    float3 yawTorque = math.cross(currentForward, projectedTarget) * flightDataComponent.SteerStrength;

    float3 totalTorque = stabilityTorque + yawTorque;

    // apply steering / stability torques
    ApplyTorque(ref velocity, mass, totalTorque, deltaTime);

    // apply exponential angular damping (avoids double-integrating damping as a torque)
    float dampingFactor = math.exp(-flightDataComponent.AngularDamping * deltaTime);
    velocity.Angular *= dampingFactor;
    }

    // Simplified PID for thrust
    void ApplyThrust(ref PhysicsVelocity vel, PhysicsMass mass, float3 currentDirection, float currentSpeed,
        float targetSpeed, float acceleration, float deltaTime)
    {
        float error = targetSpeed - currentSpeed;
        // Simple P-controller: force is proportional to error
        float thrust = math.clamp(error, -1, 1) * acceleration;
        ApplyForceToVelocity(ref vel, mass, thrust, deltaTime, currentDirection);
    }

    void ApplyTorque(ref PhysicsVelocity vel, in PhysicsMass mass, float3 torque, float deltaTime)
    {
        vel.Angular += mass.InverseInertia * torque * deltaTime;
    }

    private void UpdateFlightPhase(ref PlaneFlightDataComponent flightDataComponent, in PlaneComponent plane,
        float3 currentPosition, float currentAltitude)
    {
        switch (flightDataComponent.CurrentPhase)
        {
            case FlightPhase.TakeOff:
                if (currentAltitude >= 20f)
                {
                    flightDataComponent.CurrentPhase = FlightPhase.Cruise;
                }

                break;
            case FlightPhase.Climb:
                if (currentAltitude >= flightDataComponent.CruisingAltitude * 0.95f)
                {
                    flightDataComponent.CurrentPhase = FlightPhase.Cruise;
                }

                break;
            case FlightPhase.Cruise:
                float distanceToTarget = math.distance(currentPosition, plane.Dest);
                if (distanceToTarget <= 1000f)
                {
                    flightDataComponent.CurrentPhase = FlightPhase.Descent;
                }

                break;
            case FlightPhase.Descent:
                if (currentAltitude <= 20f)
                {
                    flightDataComponent.CurrentPhase = FlightPhase.Landing;
                }

                break;
            case FlightPhase.Landing:
                break;
        }
    }
}