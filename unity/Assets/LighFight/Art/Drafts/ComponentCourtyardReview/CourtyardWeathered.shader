Shader "LighFight/CourtyardWeatheredReview" {
Properties {_Wall("Wall mode",Float)=0 _GroundTex("Generated natural gravel albedo",2D)="gray"{}}
SubShader {Tags {"RenderType"="Opaque"} CGPROGRAM
#pragma surface surf Standard fullforwardshadows addshadow
#pragma target 3.0
float _Wall; sampler2D _GroundTex;
struct Input {float3 worldPos;float3 worldNormal;INTERNAL_DATA};
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
void surf(Input i,inout SurfaceOutputStandard o){float2 p=i.worldPos.xz; if(_Wall>.5){float3 worldN=WorldNormalVector(i,float3(0,0,1)); if(abs(worldN.x)>.5) p=i.worldPos.zy; else p=i.worldPos.xy;}float slow=noise(p*.65),fine=lerp(.5,noise(p*25),1-smoothstep(.35,1.1,max(length(ddx(p)),length(ddy(p)))*25));float height;
if(_Wall>.5){float stain=smoothstep(.3,.8,noise(p*1.3))*smoothstep(.4,0,i.worldPos.y);o.Albedo=lerp(float3(.52,.51,.47),float3(.24,.25,.20),stain*.55)*lerp(.84,1.13,slow);o.Albedo*=lerp(.88,1.08,fine);height=fine*.00055+slow*.002;o.Smoothness=.12;}
else {o.Albedo=tex2D(_GroundTex,p*.5).rgb; height=0; o.Smoothness=.08;}
o.Normal=float3(0,0,1);o.Metallic=0;o.Alpha=1;}
ENDCG }Fallback Off}
