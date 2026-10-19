Shader "LighFight/CourtyardSourcePbrReview" {
 Properties { _MainTex("Unchanged original base color (sRGB)",2D)="white"{} _RmTex("Unchanged original RM: G roughness/B metallic (linear)",2D)="white"{} _BumpMap("Unchanged original normal",2D)="bump"{} _NormalStrength("Normal strength",Range(0,1))=1 _NormalYSign("Normal Y diagnostic sign",Float)=1 _HideForeground("Hide camera-side decoration",Float)=1 _FrontCutZ("Unchanged cut boundary",Float)=-2.3 }
 SubShader { Tags { "RenderType"="Opaque" } LOD 300
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 sampler2D _MainTex,_RmTex,_BumpMap;float _NormalStrength,_NormalYSign,_HideForeground,_FrontCutZ;
 struct Input {float2 uv_MainTex;float3 worldPos;};
 void surf(Input i,inout SurfaceOutputStandard o){if(_HideForeground>.5&&i.worldPos.y>.12&&i.worldPos.z<_FrontCutZ)clip(-1);float4 albedo=tex2D(_MainTex,i.uv_MainTex);float4 rm=tex2D(_RmTex,i.uv_MainTex);o.Albedo=albedo.rgb;o.Alpha=albedo.a;o.Metallic=rm.b;o.Smoothness=1-rm.g;float3 n=UnpackNormal(tex2D(_BumpMap,i.uv_MainTex));n.xy*=float2(_NormalStrength,_NormalStrength*_NormalYSign);n.z=sqrt(saturate(1-dot(n.xy,n.xy)));o.Normal=normalize(n);}
 ENDCG
 }Fallback Off
}
