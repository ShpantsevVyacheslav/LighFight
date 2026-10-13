Shader "Hidden/LighFight/FittedFilmic" {
 Properties { _MainTex("Source",2D)="white"{} _Exposure("Exposure",Float)=1 }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass { CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex; float _Exposure;
   float4 frag(v2f_img i):SV_Target {
    float4 source=tex2D(_MainTex,i.uv);
    float3 x=max(0,source.rgb*_Exposure);
    float3 mapped=saturate((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14));
    return float4(mapped,source.a);
   }
  ENDCG }
 }
 Fallback Off
}
