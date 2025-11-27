using DefaultNamespace;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine; // Required for Debug.DrawLine

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
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new DrawLinesJob
            {
                ECB = ecb,
            }.Schedule(state.Dependency);
        }

        public partial struct DrawLinesJob : IJobEntity
        {
            public EntityCommandBuffer ECB;

            public void Execute(in LocalTransform transform, ref GuidePathComponent guidePath)
            {
                Vector3 start = guidePath.StartPoint;

                int segments = 50;
                Vector3 prevPos = start;

                for (int i = 1; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    Vector3 currentPos = LineCalculator.Calculate(guidePath, t);

                    Debug.DrawLine(prevPos, currentPos, Color.cyan);
                    prevPos = currentPos;
                }
                
                // Draw line up from transform position to indicate height
                Debug.DrawLine(transform.Position, transform.Position + transform.Up() * 2f, Color.yellow);
                Debug.DrawLine(transform.Position, transform.Position + transform.Forward() * 2f, Color.red);
            }
        }
    }
}
#endif