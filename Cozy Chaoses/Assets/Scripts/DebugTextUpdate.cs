using TMPro;
using Unity.Entities;
using UnityEngine;

public class DebugTextUpdate : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI debugText;

    [SerializeField, Range(0.1f, 2f)] float sampleDuration = 1f;

    private int _frames;

    private float _duration, _bestDuration = float.MaxValue, _worstDuration;

    void Update()
    {
        float frameDuration = Time.unscaledDeltaTime;
        _duration += frameDuration;
        CalculateFrameTimes(frameDuration);
        
        if (_duration >= sampleDuration)
        {
            string entitiesText = GetEntitiesText();
            string frameTimes = GetFrameTimes();
            debugText.text = $"{entitiesText}\n\n{frameTimes}";
        }
    }
    
    private void CalculateFrameTimes(float frameDuration)
    {
        _frames += 1;

        if (frameDuration < _bestDuration)
        {
            _bestDuration = frameDuration;
        }

        if (frameDuration > _worstDuration)
        {
            _worstDuration = frameDuration;
        }
    }

    private string GetEntitiesText()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(new ComponentType[] { typeof(ConfigComponent) });
        var config = query.GetSingleton<ConfigComponent>();

        return $"ENTITIES:\n{config.CurrentPlaneCount:##,###}/{config.MaxPlaneCount:##,###}";
    }

    private string GetFrameTimes()
    {
        var res = $"MS\n{1000f * _bestDuration:0,0.0}\n{1000f * _duration / _frames:0,0.0}\n{1000f * _worstDuration:0,0.0}";
        
        _frames = 0;
        _duration = 0f;
        _bestDuration = float.MaxValue;
        _worstDuration = 0f;

        return res;
    }
}