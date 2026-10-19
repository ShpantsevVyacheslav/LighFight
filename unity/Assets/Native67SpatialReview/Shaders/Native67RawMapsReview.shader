Shader "LighFight/Review/Native67 Raw Maps" {
 Properties {
  _MainTex("Original basecolor",2D)="white"{}
  _BumpMap("Original normal",2D)="bump"{}
  _RawRoughness("Original roughness",2D)="white"{}
  _RawMetallic("Original metallic",2D)="black"{}
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap,_RawRoughness,_RawMetallic;
  struct Input {float2 uv_MainTex;float facing:VFACE;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   float4 base=tex2D(_MainTex,IN.uv_MainTex);o.Albedo=base.rgb;o.Alpha=base.a;
   o.Metallic=tex2D(_RawMetallic,IN.uv_MainTex).r;
   o.Smoothness=1-tex2D(_RawRoughness,IN.uv_MainTex).r;
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_MainTex),1);
   o.Normal*=IN.facing>=0?1:-1;
  }
  ENDCG
 }
 Fallback Off
}
