Shader "DenseRigifyReview/AmbientRendererProbe" {
 Properties { _ProbeDir("World diffuse normal",Vector)=(0,1,0,0) _Mode("SH0 reflection1",Float)=0 }
 SubShader { Tags { "RenderType"="Opaque" } Cull Off Pass {
 Tags { "LightMode"="ForwardBase" }
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fwdbase
 #pragma target 3.0
 #include "UnityCG.cginc"
 float4 _ProbeDir;float _Mode;
 struct v2f {float4 vertex:SV_POSITION;};
 v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);return o;}
 float4 frag(v2f i):SV_Target {if(_Mode>.5){half4 p=UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0,normalize(_ProbeDir.xyz),0);return float4(DecodeHDR(p,unity_SpecCube0_HDR),1);}return float4(ShadeSH9(float4(normalize(_ProbeDir.xyz),1)),1);}
 ENDCG
 } }
}
