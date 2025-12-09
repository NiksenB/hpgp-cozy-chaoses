using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

// Required for Debug.DrawLine

namespace Systems
{
#if UNITY_EDITOR
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct GuidePathDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PlanetComponent>();
            state.RequireForUpdate<ConfigComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var planet = SystemAPI.GetSingleton<PlanetComponent>();
            var config = SystemAPI.GetSingleton<ConfigComponent>();
            var dt = SystemAPI.Time.fixedDeltaTime;

            state.Dependency = new DrawLinesJob
            {
                ECB = ecb,
                Planet = planet,
                Config = config,
                DeltaTime = dt
            }.Schedule(state.Dependency);
        }

        public partial struct DrawLinesJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public float DeltaTime;
            public PlanetComponent Planet;
            public ConfigComponent Config;

            public void Execute(in LocalTransform transform, ref GuidePathComponent guidePath)
            {
                Vector3 start = transform.Position;

                var segments = 100;
                var prevPos = start;
                Quaternion prevRotation = transform.Rotation;

                for (var i = 1; i <= segments; i++)
                {
                    var newTransform = LocalTransform.FromPositionRotation(prevPos, prevRotation);
                    // float t = (float)i / segments;
                    var result = NavigationCalculator.CalculateNext(newTransform, guidePath, Config.PlaneSpeed,
                        Planet.Radius, DeltaTime);


                    Debug.DrawLine(prevPos, result.Item1, Color.cyan);
                    prevPos = result.Item1;
                    prevRotation = result.Item2;
                }

                // Draw line up from transform position to indicate height
                Debug.DrawLine(transform.Position, transform.Position + transform.Up() * 2f, Color.yellow);
                Debug.DrawLine(transform.Position, transform.Position + transform.Forward() * 2f, Color.red);
            }
        }
    }
}
#endif