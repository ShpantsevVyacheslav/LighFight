Shader "DenseRigifyReview/OriginalRGBNormalStandard" {
 Properties { _MainTex("Original base sRGB",2D)="white"{} _RawNormal("Original RGB normal linear",2D)="bump"{} _MetallicGlossMap("Original metallic/smoothness",2D)="white"{} _DecodeXY("Reconstruct Z diagnostic",Float)=0 }
 SubShader { Tags { "RenderType"="Opaque" } LOD 300
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 sampler2D _MainTex,_RawNormal,_MetallicGlossMap;float _DecodeXY;
 struct Input {float2 uv_MainTex;float2 uv_RawNormal;float2 uv_MetallicGlossMap;};
 void surf(Input i,inout SurfaceOutputStandard o){
 float4 base=tex2D(_MainTex,i.uv_MainTex);float4 p=tex2D(_MetallicGlossMap,i.uv_MetallicGlossMap);
 o.Albedo=base.rgb;o.Alpha=1;o.Metallic=p.r;o.Smoothness=p.a;
 float3 n=tex2D(_RawNormal,i.uv_RawNormal).rgb*2-1;
 if(_DecodeXY>.5)n.z=sqrt(1-saturate(dot(n.xy,n.xy)));
 o.Normal=normalize(n);
 }
 ENDCG
 }
 Fallback "Standard"
}
