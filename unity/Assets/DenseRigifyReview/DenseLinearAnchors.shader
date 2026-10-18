Shader "DenseRigifyReview/LinearStandardAnchors" {
 Properties { _LinearBase("Linear reflectance vector",Vector)=(.18,.18,.18,1) _Metal("Metallic",Float)=0 _Smooth("Smoothness",Float)=0 }
 SubShader { Tags { "RenderType"="Opaque" } CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 float4 _LinearBase;float _Metal,_Smooth;
 struct Input {float3 worldPos;};
 void surf(Input i,inout SurfaceOutputStandard o){o.Albedo=_LinearBase.rgb;o.Metallic=_Metal;o.Smoothness=_Smooth;o.Alpha=1;}
 ENDCG }
 Fallback "Standard"
}
