Shader "LighFight/ArenaSource" {
 Properties { _MainTex("Original albedo",2D)="white"{} _HideForeground("Hide camera-side decoration",Float)=1 _FrontCutZ("Camera-side cut boundary",Float)=-2.3 _ShadowStrength("Contact shadow strength",Range(0,1))=.65 }
 SubShader { Tags { "RenderType"="Opaque" } Pass { Tags { "LightMode"="ForwardBase" }
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #pragma multi_compile_fwdbase
  #pragma skip_variants LIGHTMAP_ON DIRLIGHTMAP_COMBINED DYNAMICLIGHTMAP_ON VERTEXLIGHT_ON
  #include "UnityCG.cginc"
  #include "AutoLight.cginc"
  sampler2D _MainTex;float4 _MainTex_ST;float _HideForeground,_FrontCutZ,_ShadowStrength;
  struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
  struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;SHADOW_COORDS(2)};
  v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;TRANSFER_SHADOW(o);return o;}
  fixed4 frag(v2f i):SV_Target {if(_HideForeground>.5 && i.world.y>.12 && i.world.z<_FrontCutZ)discard;fixed4 color=tex2D(_MainTex,i.uv);color.rgb*=lerp(1,SHADOW_ATTENUATION(i),_ShadowStrength);return color;}
  ENDCG
 }
 Pass {
  Tags { "LightMode"="ShadowCaster" }
  ZWrite On
  CGPROGRAM
  #pragma vertex shadowVert
  #pragma fragment shadowFrag
  #pragma multi_compile_shadowcaster
  #include "UnityCG.cginc"
  float _HideForeground,_FrontCutZ;
  struct shadowData { V2F_SHADOW_CASTER; float3 world:TEXCOORD1; };
  shadowData shadowVert(appdata_base v){shadowData o;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);return o;}
  float4 shadowFrag(shadowData i):SV_Target {if(_HideForeground>.5 && i.world.y>.12 && i.world.z<_FrontCutZ)discard;SHADOW_CASTER_FRAGMENT(i)}
  ENDCG
 }
 }
}
