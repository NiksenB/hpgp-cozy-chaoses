using System.Numerics;
using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public partial struct ApplyGravitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlanetComponent>();
            state.RequireForUpdate<PlaneStabilizerComponent>();
            state.RequireForUpdate<ConfigComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var config = SystemAPI.GetSingleton<ConfigComponent>();
            var planet = SystemAPI.GetSingleton<PlanetComponent>();

            switch (config.ExecutionMode)
            {
                case ExecutionMode.Main:

                    var center = planet.Center;

                    foreach (var (physicsVelocity, physicsMass, localTransform) in SystemAPI
                                 .Query<RefRW<PhysicsVelocity>, RefRO<PhysicsMass>, RefRO<LocalTransform>>()
                                 .WithAll<PlaneStabilizerComponent>()
                                 .WithNone<JustSpawnedTag>())
                    {
                        float3 toCenter = center - localTransform.ValueRO.Position;
                        float3 gravityDirection = math.normalize(toCenter);
                        float3 gravityForce = gravityDirection * config.GravityAcceleration *
                                              physicsMass.ValueRO.InverseMass;

                        physicsVelocity.ValueRW.Linear += gravityForce * deltaTime;
                    }

                    break;
                case ExecutionMode.Schedule:
                    state.Dependency = new ApplyGravityJob
                    {
                        Config = config,
                        Planet = planet,
                        DeltaTime = deltaTime
                    }.Schedule(state.Dependency);
                    break;
                case ExecutionMode.ScheduleParallel:
                    state.Dependency = new ApplyGravityJob
                    {
                        Config = config,
                        Planet = planet,
                        DeltaTime = deltaTime
                    }.ScheduleParallel(state.Dependency);
                    break;
            }
        }
    }

    [BurstCompile]
    [WithNone(typeof(JustSpawnedTag))]
    public partial struct ApplyGravityJob : IJobEntity
    {
        [ReadOnly] public ConfigComponent Config;
        [ReadOnly] public PlanetComponent Planet;
        public float DeltaTime;

        public void Execute(ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass,
            in LocalTransform localTransform, in PlaneStabilizerComponent stabilizer)
        {
            float3 toCenter = Planet.Center - localTransform.Position;
            float3 gravityDirection = math.normalize(toCenter);

            float3 gravityForce = gravityDirection * Config.GravityAcceleration * physicsMass.InverseMass;

            physicsVelocity.Linear += gravityForce * DeltaTime;
        }
    }
}