Shader "DenseRigifyReview/ExplicitSRGBEncode" {
 Properties { _MainTex("Linear filmic result",2D)="white"{} }
 SubShader { Cull Off ZWrite Off ZTest Always Pass {
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 float4 frag(v2f_img i):SV_Target {
 float4 p=tex2D(_MainTex,i.uv);float3 x=max(0,p.rgb);
 float3 encoded=lerp(12.92*x,1.055*pow(x,1.0/2.4)-.055,step(.0031308,x));
 return float4(encoded,p.a);
 }
 ENDCG
 } }
}
