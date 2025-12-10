using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public static class Utils
{
    // From Tank Tutorial:
    // Return a random color that is visually distinct.
    // (Naive randomness would produce a distribution of colors clustered 
    // around a narrow range of hues. See https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/ )
    public static float4 RandomColor(ref Random random)
    {
        // 0.618034005f is inverse of the golden ratio
        var hue = (random.NextFloat() + 0.618034005f) % 1;
        return (Vector4)Color.HSVToRGB(hue, 1.0f, 1.0f);
    }
}