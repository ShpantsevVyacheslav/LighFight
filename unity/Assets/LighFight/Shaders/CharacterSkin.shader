Shader "LighFight/CharacterSkin"
{
    Properties
    {
        _Color ("Color",Color)=(1,1,1,1)
        _MainTex ("Texture",2D)="white"{}
        _DetailSkinTex ("Hand skin detail",2D)="white"{}
        _DetailNormalMap ("Hand detail normal",2D)="bump"{}
        _NormalMap ("Skin normal",2D)="bump"{}
        _NormalStrength ("Normal strength",Range(0,1))=.35
        _DetailNormalStrength ("Hand detail normal strength",Range(0,1))=.12
        _Glossiness ("Smoothness",Range(0,1))=.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma skip_variants LIGHTMAP_ON DIRLIGHTMAP_COMBINED DYNAMICLIGHTMAP_ON VERTEXLIGHT_ON
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0;float2 detailUV:TEXCOORD1;float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; float3 tangent:TEXCOORD3; float3 bitangent:TEXCOORD4; float2 detailUV:TEXCOORD6;half blend:TEXCOORD7; SHADOW_COORDS(5) };
            sampler2D _DetailSkinTex;sampler2D _DetailNormalMap;sampler2D _NormalMap;half _NormalStrength;half _DetailNormalStrength;sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; half _Glossiness;
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.tangent=UnityObjectToWorldDir(v.tangent.xyz);o.bitangent=cross(o.normal,o.tangent)*(v.tangent.w*unity_WorldTransformParams.w);
                o.detailUV=v.detailUV;o.blend=v.color.r;
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);TRANSFER_SHADOW(o);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                half3 geometricNormal=normalize(i.normal);
                half3 baseNormal=UnpackNormal(tex2D(_NormalMap,i.uv));baseNormal.xy*=_NormalStrength;baseNormal=normalize(baseNormal);
                // Derive the atlas basis from the deformed surface: imported tangents may
                // belong to the original mesh before the replacement hands were welded.
                float3 atlasDP1=ddx(i.world),atlasDP2=ddy(i.world);
                float2 atlasUV1=ddx(i.uv),atlasUV2=ddy(i.uv);
                float3 atlasP2=cross(atlasDP2,geometricNormal),atlasP1=cross(geometricNormal,atlasDP1);
                float3 atlasT=atlasP2*atlasUV1.x+atlasP1*atlasUV2.x;
                float3 atlasB=atlasP2*atlasUV1.y+atlasP1*atlasUV2.y;
                float atlasScale=rsqrt(max(max(dot(atlasT,atlasT),dot(atlasB,atlasB)),1e-16));
                half3 bodyNormal=normalize(atlasT*atlasScale*baseNormal.x+atlasB*atlasScale*baseNormal.y+geometricNormal*baseNormal.z);
                // UV1 has its own chart orientation; UV0 tangents cannot orient its normal map.
                float3 dp1=ddx(i.world),dp2=ddy(i.world);float2 duv1=ddx(i.detailUV),duv2=ddy(i.detailUV);
                float3 dp2perp=cross(dp2,geometricNormal),dp1perp=cross(geometricNormal,dp1);
                float3 detailT=dp2perp*duv1.x+dp1perp*duv2.x,detailB=dp2perp*duv1.y+dp1perp*duv2.y;
                float basisScale=rsqrt(max(max(dot(detailT,detailT),dot(detailB,detailB)),1e-16));
                half3 detail=UnpackNormal(tex2D(_DetailNormalMap,i.detailUV));detail.xy*=_DetailNormalStrength;detail=normalize(detail);
                half3 handNormal=normalize(detailT*basisScale*detail.x+detailB*basisScale*detail.y+geometricNormal*detail.z);
                half3 n=normalize(lerp(bodyNormal,handNormal,i.blend));half3 l=normalize(_WorldSpaceLightPos0.xyz);
                half diffuse=max(0,dot(n,l));half shadow=SHADOW_ATTENUATION(i);
                half3 v=normalize(_WorldSpaceCameraPos-i.world);
                // Broad diffuse with a restrained dielectric highlight. Matte cloth
                // must not receive the same strong highlight as glossy surfaces.
                half spec=pow(max(0,dot(n,normalize(l+v))),lerp(32,192,_Glossiness))
                    *lerp(.015,.16,_Glossiness)*diffuse;
                half3 diffuseLight=_LightColor0.rgb*diffuse*shadow*.78;
                fixed3 baseColor=lerp(tex2D(_MainTex,i.uv).rgb,tex2D(_DetailSkinTex,i.detailUV).rgb,i.blend)*_Color.rgb;
                fixed3 ambient=max(half3(.08,.08,.08),ShadeSH9(half4(n,1)));
                return fixed4(baseColor*(ambient+diffuseLight)+spec*_LightColor0.rgb*shadow,1);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback "Diffuse"
}
