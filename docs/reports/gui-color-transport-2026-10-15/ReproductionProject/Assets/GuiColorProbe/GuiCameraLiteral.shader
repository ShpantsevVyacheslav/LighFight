Shader "Hidden/LighFight/GuiCameraLiteral" {SubShader {Cull Off ZWrite Off ZTest Always Pass {CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
float4 frag(v2f_img i):SV_Target {if(i.uv.x<.2&&i.uv.y>.8)return float4(.8,.05,.05,1);if(i.uv.x>.8&&i.uv.y<.2)return float4(.05,.1,.8,1);return float4(.2,.2,.2,1);}
ENDCG}}}
