Shader "LighFight/Native PBR" {
 Properties {
  _Color ("Color", Color) = (1,1,1,1)
  _MainTex ("Albedo", 2D) = "white" {}
  _BumpMap ("Normal", 2D) = "bump" {}
  _BumpScale ("Normal scale", Float) = 1
  _MetallicGlossMap ("Metallic / smoothness", 2D) = "white" {}
  _Metallic ("Metallic", Range(0,1)) = 0
  _Glossiness ("Smoothness", Range(0,1)) = 0.5
  _GlossMapScale ("Smoothness scale", Range(0,1)) = 1
  [ToggleOff] _SpecularHighlights ("Specular highlights", Float) = 1
  [ToggleOff] _GlossyReflections ("Glossy reflections", Float) = 1
  [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Source face culling", Float) = 0
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 300
  Cull [_Cull]
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
  #pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
  #pragma shader_feature_local _NORMALMAP
  #pragma shader_feature_local _METALLICGLOSSMAP
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex, _BumpMap, _MetallicGlossMap;
  fixed4 _Color;
  half _BumpScale, _Metallic, _Glossiness, _GlossMapScale;
  struct Input { float2 uv_MainTex; float2 uv_BumpMap; float2 uv_MetallicGlossMap; float facing : VFACE; };
  void surf(Input IN, inout SurfaceOutputStandard o) {
   fixed4 color=tex2D(_MainTex,IN.uv_MainTex)*_Color;o.Albedo=color.rgb;o.Alpha=color.a;
   #ifdef _METALLICGLOSSMAP
    fixed4 packed=tex2D(_MetallicGlossMap,IN.uv_MetallicGlossMap);o.Metallic=packed.r;o.Smoothness=packed.a*_GlossMapScale;
   #else
    o.Metallic=_Metallic;o.Smoothness=_Glossiness;
   #endif
   #ifdef _NORMALMAP
    o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   #else
    o.Normal=half3(0,0,1);
   #endif
   o.Normal*=IN.facing>=0?1:-1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
