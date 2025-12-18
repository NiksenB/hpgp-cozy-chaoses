using System;
using System.IO;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct ConfigLoadingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabConfigComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var configData = GetConfig();

            CameraController.AdjustCameraPosition(configData.planetRadius);

            var entity = SystemAPI.GetSingletonEntity<PrefabConfigComponent>();
            var prefabConfig = SystemAPI.GetSingleton<PrefabConfigComponent>();

            state.EntityManager.AddComponentData(entity, new ConfigComponent
            {
                ExecutionMode = configData.executionMode,
                EnableDebugMode = configData.enableDebugMode,
                EnableDespawnOnCollision = configData.enableDespawnOnCollision,
                EnableExplosionsOnCollision = configData.enableExplosionsOnCollision,
                EnablePlaneStabilization = configData.enablePlaneStabilization,
                NextPlaneSpawnTimeLower = configData.nextPlaneSpawnTimeLower,
                NextPlaneSpawnTimeUpper = configData.nextPlaneSpawnTimeUpper,
                MaxPlaneCount = configData.maxPlaneCount,
                CurrentPlaneCount = 0,

                PlanePrefab = prefabConfig.PlanePrefab,
                PlaneRotationSpeed = configData.planeRotationSpeed,
                PlaneSpeed = configData.planeSpeed,
                PlaneDamping = configData.planeDamping,
                PlaneMaxAngularSpeed = configData.planeMaxAngularSpeed,
                PlaneResponseSpeed = configData.planeResponseSpeed,
                PlaneForwardWeight = configData.planeForwardWeight,
                PlaneUpWeight = configData.planeUpWeight,
                
                PlaneNoGravityPrefab = prefabConfig.PlaneNoGravityPrefab,

                AirportPrefab = prefabConfig.AirportPrefab,
                AirportCount = configData.airportCount,

                PlanetPrefab = prefabConfig.PlanetPrefab,
                PlanetRadius = configData.planetRadius,

                ExplosionPrefab = prefabConfig.ExplosionPrefab
            });
            state.EntityManager.RemoveComponent<PrefabConfigComponent>(entity);
        }

        private ConfigFileData GetConfig()
        {
            // Get the location of the application executable
            var configFilePath = Path.Combine(Application.streamingAssetsPath, "config.json");

            ConfigFileData configData;

            if (File.Exists(configFilePath))
            {
                var jsonString = File.ReadAllText(configFilePath);
                Debug.Log("Config file found at " + configFilePath + ". Loading config. from string: " + jsonString);
                configData = JsonUtility.FromJson<ConfigFileData>(jsonString);
            }
            else
            {
                Debug.LogWarning("Config file not found at " + configFilePath + ". Creating default config file.");

                configData = GetDefaultConfig();
                var jsonString = JsonUtility.ToJson(configData, true);
                File.WriteAllText(configFilePath, jsonString);
            }

            return configData;
        }

        private ConfigFileData GetDefaultConfig()
        {
            return new ConfigFileData
            {
                executionMode = ExecutionMode.Schedule,
                enableDebugMode = false,
                enableDespawnOnCollision = true,
                enableExplosionsOnCollision = true,
                enablePlaneStabilization = true,
                nextPlaneSpawnTimeLower = 10.0,
                nextPlaneSpawnTimeUpper = 100.0,
                maxPlaneCount = 1000,
                airportCount = 25,
                planetRadius = 100f,
                planeSpeed = 5f,
                planeRotationSpeed = 6f,
                planeDamping = 7f,
                planeMaxAngularSpeed = 6f,
                planeResponseSpeed = 8f,
                planeForwardWeight = 1.0f,
                planeUpWeight = 0.5f
            };
        }
    }

    internal struct ConfigFileData
    {
        public ExecutionMode executionMode;
        public bool enableDebugMode;
        public bool enableDespawnOnCollision;
        public bool enableExplosionsOnCollision;
        public bool enablePlaneStabilization;
        public double nextPlaneSpawnTimeLower;
        public double nextPlaneSpawnTimeUpper;
        public int maxPlaneCount;
        
        public int airportCount;
        public float planetRadius;
        public float planeSpeed;
        public float planeRotationSpeed;
        public float planeDamping;
        public float planeMaxAngularSpeed;
        public float planeResponseSpeed;
        public float planeForwardWeight;
        public float planeUpWeight;
    }
}