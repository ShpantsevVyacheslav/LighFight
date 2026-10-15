Shader "LighFight/Review/CourtyardLeafCards" {
 Properties { _MainTex("Branch atlas",2D)="white"{} _Cutoff("Alpha cutoff",Range(0,1))=.45 _Fill("Thin-leaf ambient fill",Range(0,.3))=.08 }
 SubShader {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} LOD 200
  Cull Off AlphaToMask On
  CGPROGRAM
  #pragma target 3.0
  #pragma surface surf Standard alphatest:_Cutoff addshadow fullforwardshadows
  sampler2D _MainTex;half _Fill;
  struct Input {float2 uv_MainTex;float facing:VFACE;};
  void surf(Input IN,inout SurfaceOutputStandard o){fixed4 c=tex2D(_MainTex,IN.uv_MainTex);o.Albedo=c.rgb;o.Normal=float3(0,0,IN.facing>=0?1:-1);o.Metallic=0;o.Smoothness=.08;o.Emission=c.rgb*_Fill;o.Alpha=c.a;}
  ENDCG
 }
 Fallback "Transparent/Cutout/Diffuse"
}
