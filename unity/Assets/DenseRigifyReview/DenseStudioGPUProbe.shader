Shader "DenseRigifyReview/StudioGPUProbe" {
 Properties { _StudioCube("Prefiltered linear studio",Cube)=""{} _ProbeDir("Direction",Vector)=(0,1,0,0) _Mode("SH if1",Float)=0 }
 SubShader { Cull Off ZWrite Off ZTest Always Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 samplerCUBE _StudioCube;float4 _ProbeDir;float _Mode;
 float4 frag(v2f_img i):SV_Target {if(_Mode>.5)return float4(ShadeSH9(float4(normalize(_ProbeDir.xyz),1)),1);return texCUBElod(_StudioCube,float4(normalize(_ProbeDir.xyz),0));}
 ENDCG } }
}
