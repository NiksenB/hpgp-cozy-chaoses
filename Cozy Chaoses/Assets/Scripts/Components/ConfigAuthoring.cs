using UnityEngine;
using Unity.Entities;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject planePrefab;
    public GameObject airportPrefab;
    public GameObject planetPrefab;
    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            ConfigFileData configData = ReadConfig();
            
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new ConfigComponent
            {
                PlanetPrefab = GetEntity(authoring.planetPrefab, TransformUsageFlags.Dynamic),
                AirportPrefab = GetEntity(authoring.airportPrefab, TransformUsageFlags.Dynamic),
                PlanePrefab = GetEntity(authoring.planePrefab, TransformUsageFlags.Dynamic),
                
                PlanetRadius = configData.planetRadius,
                AirportCount = configData.airportCount
            });
        }
        
        private ConfigFileData ReadConfig()
        {
            // Get the location of the application executable
            string currDir = System.Environment.CurrentDirectory;
            string configFilePath = System.IO.Path.Combine(currDir, "config.json");
            
            if (System.IO.File.Exists(configFilePath))
            {
                string jsonString = System.IO.File.ReadAllText(configFilePath);
                ConfigFileData configData = JsonUtility.FromJson<ConfigFileData>(jsonString);
                return configData;
            }
            else
            {
                ConfigFileData defaultConfig = new ConfigFileData
                {
                    airportCount = 25,
                    planetRadius = 100f
                };
                
                string jsonString = JsonUtility.ToJson(defaultConfig, true);
                System.IO.File.WriteAllText(configFilePath, jsonString);
                return defaultConfig;
            }
        }
    }
}
struct ConfigFileData
{
    public float planetRadius;
    public int airportCount;
}

public struct ConfigComponent : IComponentData
{
    public Entity PlanePrefab;
    public Entity AirportPrefab;
    public Entity PlanetPrefab;
    public float PlanetRadius;
    public int AirportCount;
}
