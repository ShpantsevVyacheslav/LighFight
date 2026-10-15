Shader "Hidden/LighFight/GuiProbeLinearDraw" {
 Properties {_MainTex("Source",2D)="white"{} _Radius("Radius",Float)=12 _RectSize("Rect size",Vector)=(160,143,0,0) _SourceUV("UV",Vector)=(0,0,1,1)}
 SubShader {Tags{"Queue"="Overlay"} Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha Pass {CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _SourceUV,_RectSize;float _Radius;
 float4 frag(v2f_img i):SV_Target {float4 c=tex2D(_MainTex,i.uv);float2 local=(i.uv-_SourceUV.xy)/_SourceUV.zw;float2 q=abs((local-.5)*_RectSize.xy)-(_RectSize.xy*.5-_Radius);float d=length(max(q,0))+min(max(q.x,q.y),0)-_Radius;c.a*=1-smoothstep(-.5,.5,d);return c;}
 ENDCG }}}
