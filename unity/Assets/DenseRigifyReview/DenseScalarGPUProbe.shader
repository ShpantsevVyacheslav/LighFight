Shader "DenseRigifyReview/ScalarGPUProbe" {
 Properties { _ScalarMap("Original linear metal/smooth packing",2D)="white"{} _ProbeUV("UV",Vector)=(.5,.5,0,0) }
 SubShader { Cull Off ZWrite Off ZTest Always Pass {
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 sampler2D _ScalarMap;float4 _ProbeUV;
 float4 frag(v2f_img i):SV_Target {float4 p=tex2Dlod(_ScalarMap,float4(_ProbeUV.xy,0,0));return float4(p.r,p.a,0,1);}
 ENDCG
 } }
}
