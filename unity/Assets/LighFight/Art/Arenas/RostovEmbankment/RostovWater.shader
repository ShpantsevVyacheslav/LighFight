Shader "LighFight/RostovWater" {
    Properties {
        _DeepColor ("Deep Water Color", Color) = (0.05, 0.14, 0.22, 1.0)
        _ShallowColor ("Surface Wave Color", Color) = (0.16, 0.38, 0.46, 1.0)
        _BumpMap ("Wave Normal Map", 2D) = "bump" {}
        _WaveSpeed ("Wave Flow Speed (XY)", Vector) = (0.04, 0.015, -0.02, 0.03)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.92
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        fixed4 _DeepColor;
        fixed4 _ShallowColor;
        sampler2D _BumpMap;
        float4 _WaveSpeed;
        half _Glossiness;

        struct Input {
            float2 uv_BumpMap;
            float3 worldNormal;
            float3 viewDir;
            INTERNAL_DATA
        };

        void surf(Input IN, inout SurfaceOutputStandard o) {
            float2 uv1 = IN.uv_BumpMap * 3.5 + _Time.y * _WaveSpeed.xy;
            float2 uv2 = IN.uv_BumpMap * 5.0 + _Time.y * _WaveSpeed.zw;

            float3 n1 = UnpackNormal(tex2D(_BumpMap, uv1));
            float3 n2 = UnpackNormal(tex2D(_BumpMap, uv2));
            float3 n = normalize(n1 + n2);

            o.Normal = n;

            // Fresnel term: steeper angle sees deep water, glancing angle reflects sky
            half fresnel = 0.04 + (1.0 - 0.04) * pow(1.0 - saturate(dot(o.Normal, normalize(IN.viewDir))), 4.0);

            o.Albedo = lerp(_DeepColor.rgb, _ShallowColor.rgb, fresnel * 0.7);
            o.Metallic = 0.15;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
