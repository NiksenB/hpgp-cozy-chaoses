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

    public void Execute(
        ref PlaneFlightDebugDataComponent debugData,
        Entity entity,
        ref LocalTransform transform,
        ref PhysicsVelocity velocity,
        in PhysicsMass mass,
        in PlaneComponent plane,
        ref PlaneFlightDataComponent config)
    {
        // --- 1. GET CURRENT STATE ---
        // Get all the state variables we need, just like you did before.

        // Planet and Gravity
        float3 planetCenter = Planet.Center;
        float planetGravity = Planet.Gravity;
        float3 position = transform.Position;
        float3 toPlanet = planetCenter - position;
        float3 gravityDir = math.normalize(toPlanet); // This is "Down"
        float3 localUp = -gravityDir; // This is "Up"
        float sphereRadius = Planet.Radius;
        float earthScale = Planet.EarthScale;
        float currentAltitude = math.length(toPlanet) - Planet.Radius;

        // Velocity and Aerodynamics
        float3 linearVelocity = velocity.Linear;
        float currentSpeed = math.length(linearVelocity);
        float3 velocityDir = (currentSpeed > 0.01f) ? (linearVelocity / currentSpeed) : float3.zero;

        // Plane's Orientation
        float3 forward = transform.Forward();
        float3 right = transform.Right();
        float3 up = transform.Up(); // The way the plane's "roof" is pointing

        // Get auto pilot values
        float targetAltitude = GetTargetAltitude(config, sphereRadius) * earthScale;
        UpdateFlightPhase(ref config, earthScale, currentSpeed, currentAltitude, targetAltitude);

        if (config.CurrentPhase == FlightPhase.Descent)
        {
            velocity.Linear = new float3(0, 0, 0);
            ECB.RemoveComponent<PlaneComponent>(entity);
            return;
        }

        float targetSpeed = GetTargetSpeed(config) * earthScale;
        targetAltitude = GetTargetAltitude(config, sphereRadius) * earthScale;

        // --- 2. APPLY ENVIRONMENTAL FORCES (The "Brawn") ---
        // These are the non-negotiable physics of being in an atmosphere.

        // (A) GRAVITY: Always on, always pulls down.
        // We apply this as an acceleration (m/s^2).
        float3 gravityAccel = gravityDir * planetGravity; // Use a realistic 'G'
        velocity.Linear += earthScale * gravityAccel * DeltaTime;

        // (B) LIFT: The "magic" of wings. We'll simplify.
        // Lift is proportional to speed-squared and pushes along the plane's *UP* vector.
        // A more realistic model uses Angle of Attack, but this is far more stable.
        float speedSqr = currentSpeed * currentSpeed;
        float liftForce = config.LiftStrength * speedSqr;
        float3 liftAccel = up * liftForce * mass.InverseMass;
        velocity.Linear += earthScale * liftAccel * DeltaTime;

        // (C) DRAG: Air resistance.
        // Drag is proportional to speed-squared and pushes *against* the velocity vector.
        float dragForce = config.DragCoefficient * speedSqr;
        float3 dragAccel = -velocityDir * dragForce * mass.InverseMass;
        velocity.Linear += earthScale * dragAccel * DeltaTime;


        // --- 3. RUN AUTOPILOT "BRAIN" ---
        // Now, we decide what the *controls* should be doing.
        // We use simple P-controllers (like you had) to get a -1 to 1 input.

        // (A) Thrust Controller: Tries to match TargetSpeed
        float speedError = targetSpeed - currentSpeed;
        float thrustInput = math.clamp(speedError * 0.1f, 0f, 1f); // Only thrust forward

        // (B) Pitch Controller: Tries to match TargetAltitude
        float altitudeError = targetAltitude - currentAltitude;
        float pitchInput = math.clamp(altitudeError * 0.01f, -1f, 1f); // Tune the '0.01f' sensitivity

        // (C) Roll Controller: Tries to stay level with the planet's surface
        // We find the "roll error" by seeing how much our 'right' vector is pointing 'up'.
        float rollError = math.dot(right, localUp);
        float rollInput = math.clamp(-rollError * 2f, -1f, 1f); // Tune sensitivity

        // (D) Yaw Controller: Tries to point at the target
        // (This re-uses your old logic, which was correct!)
        float3 forwardOnSphere = math.normalize(forward - math.dot(forward, localUp) * localUp);
        float angleToTarget = Vector3.SignedAngle(forwardOnSphere, forwardOnSphere /*targetDirection*/, localUp);
        float yawInput = math.clamp(angleToTarget * 0.05f, -1f, 1f); // Tune sensitivity


        // --- 4. APPLY CONTROL FORCES (The "Brawn" part 2) ---
        // The Brain has made its decisions. Now the Brawn executes them.

        // (A) APPLY THRUST: Linear force along the plane's 'forward'.
        float3 thrustForce = forward * thrustInput * config.MaxThrust;
        float3 thrustAccel = thrustForce * mass.InverseMass;
        velocity.Linear += earthScale * thrustAccel * DeltaTime;

        // (B) APPLY TORQUES: Rotational forces.

        // Pitch: Rotates around the 'right' vector
        float3 pitchTorque = right * pitchInput * config.PitchStrength;

        // Roll: Rotates around the 'forward' vector
        float3 rollTorque = forward * rollInput * config.RollStrength;

        // Yaw: Rotates around the 'up' vector
        float3 yawTorque = up * yawInput * config.YawStrength;

        // Apply all torques as angular acceleration
        float3 totalTorque = (pitchTorque + rollTorque + yawTorque);
        float3 angularAccel = mass.InverseInertia * totalTorque; // Use InverseInertia

        velocity.Angular += earthScale * angularAccel * DeltaTime;
        
        
        debugData.PlanetCenter = planetCenter;
        debugData.Position     = position;
        debugData.ToPlanet     = toPlanet;
        debugData.GravityDir   = gravityDir;
        debugData.LocalUp      = localUp;
        debugData.SphereRadius = sphereRadius;
        debugData.EarthScale   = earthScale;
        debugData.CurrentAltitude = currentAltitude;
        debugData.LinearVelocity = linearVelocity;
        debugData.CurrentSpeed = currentSpeed;
        debugData.VelocityDir  = velocityDir;
        debugData.Forward      = forward;
        debugData.Right        = right;
        debugData.Up           = up;
        debugData.TargetAltitude = targetAltitude;
        debugData.TargetSpeed  = targetSpeed;
        debugData.GravityAccel = gravityAccel;
        debugData.SpeedSqr     = speedSqr;
        debugData.LiftForce    = liftForce;
        debugData.LiftAccel    = liftAccel;
        debugData.DragForce    = dragForce;
        debugData.DragAccel    = dragAccel;
        debugData.SpeedError   = speedError;
        debugData.ThrustInput  = thrustInput;
        debugData.AltitudeError = altitudeError;
        debugData.PitchInput   = pitchInput;
        debugData.RollError    = rollError;
        debugData.RollInput    = rollInput;
        debugData.ForwardOnSphere = forwardOnSphere;
        debugData.AngleToTarget = angleToTarget;
        debugData.YawInput     = yawInput;
        debugData.ThrustForce  = thrustForce;
        debugData.ThrustAccel  = thrustAccel;
        debugData.PitchTorque  = pitchTorque;
        debugData.RollTorque   = rollTorque;
        debugData.YawTorque    = yawTorque;
        debugData.TotalTorque  = totalTorque;
        debugData.AngularAccel = angularAccel;
    }

    private float GetTargetAltitude(in PlaneFlightDataComponent flightData, float sphereRadius)
    {
        return flightData.CurrentPhase switch
        {
            FlightPhase.TakeOff => 0f,
            FlightPhase.Climb or FlightPhase.Cruise => flightData.CruisingAltitudePercentage *
                                                       sphereRadius,
            _ => 0f
        };
    }

    private float GetTargetSpeed(in PlaneFlightDataComponent flightData)
    {
        return flightData.CurrentPhase switch
        {
            FlightPhase.TakeOff => flightData.MaxSpeed,
            FlightPhase.Climb or FlightPhase.Cruise => flightData.MaxSpeed,
            _ => 0f
        };
    }

    private void UpdateFlightPhase(ref PlaneFlightDataComponent flightData, float earthScale, float currentSpeed,
        float currentAltitude,
        float targetAltitude)
    {
        switch (flightData.CurrentPhase)
        {
            case FlightPhase.TakeOff:
                if (currentSpeed >= flightData.V1Speed * earthScale)
                {
                    flightData.CurrentPhase = FlightPhase.Climb;
                }

                break;
            case FlightPhase.Climb:
                if (currentAltitude >= targetAltitude * 0.95f)
                {
                    flightData.CurrentPhase = FlightPhase.Cruise;
                }

                break;
            case FlightPhase.Cruise:
                // Something about distance to target
                break;
        }
    }
}