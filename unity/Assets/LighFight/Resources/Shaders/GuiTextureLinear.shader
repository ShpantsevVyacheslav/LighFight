Shader "Hidden/LighFight/GuiTextureLinear" {
 Properties {_MainTex("Source",2D)="white"{} _Tint("Linear GUI tint",Vector)=(1,1,1,1) _Radius("Radius",Float)=0 _RectSize("Rect size",Vector)=(160,143,0,0) _SourceUV("UV",Vector)=(0,0,1,1)}
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _SourceUV,_RectSize,_Tint;float _Radius;
 float4 frag(v2f_img i):SV_Target {float4 c=tex2D(_MainTex,i.uv)*_Tint;float2 local=(i.uv-_SourceUV.xy)/_SourceUV.zw;if(_Radius>0){float2 q=abs((local-.5)*_RectSize.xy)-(_RectSize.xy*.5-_Radius);float d=length(max(q,0))+min(max(q.x,q.y),0)-_Radius;float edge=max(fwidth(d),.0001);c.a*=1-smoothstep(-edge*.5,edge*.5,d);}return c;}
 ENDCG
 SubShader {Tags{"Queue"="Overlay"} Cull Off ZWrite Off ZTest Always
 Pass {Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 ENDCG}
 Pass {Blend Off CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 ENDCG}}
}
