Shader "Hidden/LighFight/GuiProbeExtraDecode" {Properties {_MainTex("Source",2D)="white"{}} SubShader { Cull Off ZWrite Off ZTest Always Pass {CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex;
float4 frag(v2f_img i):SV_Target {float4 c=tex2D(_MainTex,i.uv);c.rgb=GammaToLinearSpace(c.rgb);return c;}
ENDCG }}}
