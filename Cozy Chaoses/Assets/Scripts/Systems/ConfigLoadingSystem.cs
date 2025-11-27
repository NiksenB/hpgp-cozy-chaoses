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
            state.RequireForUpdate<ConfigComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            ConfigFileData configData = GetConfig();
            Debug.Log(configData);
            Debug.Log("Loaded Config: AirportCount = " + configData.airportCount + ", PlanetRadius = " + configData.planetRadius);
            // Get the ConfigComponent singleton
            RefRW<ConfigComponent> entity = SystemAPI.GetSingletonRW<ConfigComponent>();
            entity.ValueRW.AirportCount = configData.airportCount;
            entity.ValueRW.PlanetRadius = configData.planetRadius;
        }
        
        private ConfigFileData GetConfig()
        {
            // Get the location of the application executable
            string currDir = System.Environment.CurrentDirectory;
            string configFilePath = System.IO.Path.Combine(currDir, "config.json");
            
            ConfigFileData configData;
            
            if (System.IO.File.Exists(configFilePath))
            {
                string jsonString = System.IO.File.ReadAllText(configFilePath);
                configData = JsonUtility.FromJson<ConfigFileData>(jsonString);
            }
            else
            {

                configData = GetDefaultConfig();
                string jsonString = JsonUtility.ToJson(configData, true);
                System.IO.File.WriteAllText(configFilePath, jsonString);
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
        public float planetRadius;
        public int airportCount;
    }
}