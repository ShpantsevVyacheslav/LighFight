Shader "LighFight/RostovEmbankmentPBR" {
    Properties {
        _Color ("Color Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo Map", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _PuddleMap ("Puddle / Wetness Mask", 2D) = "white" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1.0
        _Glossiness ("Base Smoothness", Range(0, 1)) = 0.45
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Wetness ("Wetness Amount", Range(0, 1)) = 0.85
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _PuddleMap;
        fixed4 _Color;
        half _NormalStrength;
        half _Glossiness;
        half _Metallic;
        half _Wetness;

        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_PuddleMap;
        };

        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            // Puddle map: dark pixels (0) = wet puddles, bright pixels (1) = dry stone
            half puddleSample = tex2D(_PuddleMap, IN.uv_MainTex).r;
            half isWet = saturate((1.0 - puddleSample) * _Wetness * 1.6);

            // Wet stone darkening (porous stone absorbs light when wet)
            o.Albedo = lerp(c.rgb, c.rgb * 0.62, isWet);
            o.Metallic = _Metallic;

            // Puddles become mirror-smooth (0.96 smoothness), dry stone is _Glossiness
            o.Smoothness = lerp(_Glossiness, 0.96, isWet);

            // Water pools flatten surface normals
            float3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            half effectiveNormalStr = lerp(_NormalStrength, _NormalStrength * 0.15, isWet);
            n.xy *= effectiveNormalStr;
            n.z = sqrt(saturate(1.0 - dot(n.xy, n.xy)));
            o.Normal = normalize(n);
            o.Alpha = c.a;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
