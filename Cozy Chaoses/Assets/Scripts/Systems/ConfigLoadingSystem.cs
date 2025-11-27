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

            ConfigFileData configData = GetConfig();
            
            var entity = SystemAPI.GetSingletonEntity<PrefabConfigComponent>();
            PrefabConfigComponent prefabConfig = SystemAPI.GetSingleton<PrefabConfigComponent>();

            state.EntityManager.AddComponentData(entity, new ConfigComponent
            {
                PlanePrefab = prefabConfig.PlanePrefab,
                
                AirportPrefab = prefabConfig.AirportPrefab,
                AirportCount = configData.airportCount,
                
                PlanetPrefab = prefabConfig.PlanetPrefab,
                PlanetRadius = configData.planetRadius
            });
            state.EntityManager.RemoveComponent<PrefabConfigComponent>(entity);
        }
        
        private ConfigFileData GetConfig()
        {
            // Get the location of the application executable
            string currDir = System.Environment.CurrentDirectory;
            string configFilePath = Path.Combine(currDir, "config.json");
            
            ConfigFileData configData;
            
            if (File.Exists(configFilePath))
            {
                string jsonString = System.IO.File.ReadAllText(configFilePath);
                configData = JsonUtility.FromJson<ConfigFileData>(jsonString);
            }
            else
            {
                Debug.LogError("Config file not found at " + configFilePath + ". Creating default config file.");

                configData = GetDefaultConfig();
                string jsonString = JsonUtility.ToJson(configData, true);
                File.WriteAllText(configFilePath, jsonString);
            }

            return configData;
        }

        private ConfigFileData GetDefaultConfig()
        {
            return new ConfigFileData
            {
                airportCount = 25,
                planetRadius = 100f
            };
        }
    }
    
    struct ConfigFileData
    {
        public int airportCount;
        public float planetRadius;
    }
}