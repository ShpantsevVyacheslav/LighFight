Shader "LighFight/MeetingEnvironmentPBR" {
 Properties {
  _Color("Base factor",Color)=(1,1,1,1)
  _MainTex("Original base color",2D)="white"{}
  _MetallicGlossMap("Derived metallic/smoothness",2D)="white"{}
  _UseMetallicMap("Has packed map",Float)=0
  _Metallic("Metallic factor",Range(0,1))=0
  _Glossiness("Smoothness",Range(0,1))=.3
  _BumpMap("Original normal",2D)="bump"{}
  _BumpScale("Normal strength",Float)=1
  _Cull("Cull",Float)=2
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200 Cull [_Cull]
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex,_MetallicGlossMap,_BumpMap;float4 _Color;float _UseMetallicMap,_Metallic,_Glossiness,_BumpScale;
  struct Input {float2 uv_MainTex;};
  void surf(Input i,inout SurfaceOutputStandard o){
   fixed4 color=tex2D(_MainTex,i.uv_MainTex)*_Color;float4 packed=tex2D(_MetallicGlossMap,i.uv_MainTex);
   o.Albedo=color.rgb;o.Alpha=1;o.Metallic=lerp(_Metallic,packed.r,_UseMetallicMap);o.Smoothness=lerp(_Glossiness,packed.a,_UseMetallicMap);
   float3 n=UnpackNormal(tex2D(_BumpMap,i.uv_MainTex));n.xy*=_BumpScale;n.z=sqrt(saturate(1-dot(n.xy,n.xy)));o.Normal=normalize(n);
  }
  ENDCG
 }
 Fallback "Diffuse"
}
