Shader "LighFight/Shadowless Character" {
 Properties {
  _Color ("Source color",Color)=(1,1,1,1)
  _MainTex ("Source albedo",2D)="white"{}
  _DetailSkinTex ("Replacement hand albedo",2D)="white"{}
  _UseDetailSkin ("Use authored hand mask",Float)=0
  [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Source culling",Float)=2
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  Cull [_Cull]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 detailUV:TEXCOORD1;float4 color:COLOR;};
   struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float2 detailUV:TEXCOORD1;half mask:TEXCOORD2;};
   sampler2D _MainTex,_DetailSkinTex;float4 _MainTex_ST;fixed4 _Color;half _UseDetailSkin;
   v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.detailUV=v.detailUV;o.mask=v.color.r*_UseDetailSkin;return o;}
   fixed4 frag(v2f i):SV_Target{return lerp(tex2D(_MainTex,i.uv),tex2D(_DetailSkinTex,i.detailUV),saturate(i.mask))*_Color;}
   ENDCG
  }
 }
}
