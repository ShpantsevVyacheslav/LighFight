Shader "DenseRigifyReview/NormalGPUProbe" {
 Properties { _NormalMap("Actual imported source normal",2D)="bump"{} _ProbeUV("UV",Vector)=(.5,.5,0,0) _Mode("Mode",Float)=0 }
 SubShader { Cull Off ZWrite Off ZTest Always Pass {
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 #include "UnityStandardUtils.cginc"
 sampler2D _NormalMap;float4 _ProbeUV;float _Mode;
 float4 frag(v2f_img i):SV_Target {
 float4 p=tex2Dlod(_NormalMap,float4(_ProbeUV.xy,0,0));
 if(_Mode<.5)return p;
 if(_Mode<1.5)return float4(UnpackScaleNormal(p,1),1);
 if(_Mode<2.5)return float4(UnpackNormal(p),1);
 float4 flags=0;
 #if defined(UNITY_NO_DXT5nm)
 flags.x=1;
 #endif
 #if defined(UNITY_ASTC_NORMALMAP_ENCODING)
 flags.y=1;
 #endif
 #if defined(UNITY_USE_NATIVE_HDR)
 flags.z=1;
 #endif
 return flags;
 }
 ENDCG
 } }
}
