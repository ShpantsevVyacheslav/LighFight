Shader "LighFight/ArenaSource" {
 Properties { _MainTex("Original albedo",2D)="white"{} _HideForeground("Hide camera-side decoration",Float)=1 _FrontCutZ("Camera-side cut boundary",Float)=-2.3 }
 SubShader { Tags { "RenderType"="Opaque" } Pass {
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "UnityCG.cginc"
  sampler2D _MainTex;float4 _MainTex_ST;float _HideForeground,_FrontCutZ;
  struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
  struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
  v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
  fixed4 frag(v2f i):SV_Target {if(_HideForeground>.5 && i.world.y>.12 && i.world.z<_FrontCutZ)discard;return tex2D(_MainTex,i.uv);}
  ENDCG
 } }
}
