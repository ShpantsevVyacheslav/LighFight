Shader "LighFight/Native Unlit" {
 Properties { _MainTex ("Albedo", 2D) = "white" {} [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Source face culling", Float) = 0 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  Cull [_Cull]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _MainTex_ST;
   struct v2f { float4 position:SV_POSITION;float2 uv:TEXCOORD0; };
   v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);return o;}
   fixed4 frag(v2f i):SV_Target{return tex2D(_MainTex,i.uv);}
   ENDCG
  }
 }
}
