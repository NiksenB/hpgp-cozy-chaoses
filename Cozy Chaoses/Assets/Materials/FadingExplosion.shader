Shader "Custom/FadingExplosion"
{
    Properties
    {
        _BaseColor2 ("Base Color", Color) = (1,1,1,1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _FadeStart ("Fade Start (0–1)", Range(0,1)) = 0.2
        _FadeEnd ("Fade End (0–1)", Range(0,1)) = 1.0
        _Lifetime ("Lifetime Normalized", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade

        sampler2D _NoiseTex;

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor2;
            float _Lifetime;
            float _FadeStart;
            float _FadeEnd;
        CBUFFER_END

        struct Input {
            float2 uv_NoiseTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float noise = tex2D(_NoiseTex, IN.uv_NoiseTex).r;

            // How much should the object be faded?
            float fade = saturate((_Lifetime - _FadeStart) / (_FadeEnd - _FadeStart));

            // Use noise to get fog-dissolve look
            float dissolveMask = smoothstep(fade, fade + 0.2, noise);

            float alpha = (1 - dissolveMask);   // More fade → more transparent

            o.Albedo = _BaseColor2.rgb;
            o.Alpha = alpha * _BaseColor2.a;
        }
        ENDCG
    }
}
