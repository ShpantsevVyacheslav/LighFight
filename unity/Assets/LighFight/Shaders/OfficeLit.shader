Shader "LighFight/OfficeLit"
{
    Properties
    {
        _Color ("Color",Color)=(1,1,1,1)
        _MainTex ("Texture",2D)="white"{}
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
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; SHADOW_COORDS(3) };
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; half _Glossiness;
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);TRANSFER_SHADOW(o);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                half3 n=normalize(i.normal);half3 l=normalize(_WorldSpaceLightPos0.xyz);
                half diffuse=max(0,dot(n,l));half shadow=SHADOW_ATTENUATION(i);
                half3 v=normalize(_WorldSpaceCameraPos-i.world);
                half spec=pow(max(0,dot(n,normalize(l+v))),lerp(8,64,_Glossiness))*.18;
                fixed3 baseColor=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
                fixed3 ambient=max(half3(.08,.08,.08),ShadeSH9(half4(n,1)));
                return fixed4(baseColor*(ambient+_LightColor0.rgb*diffuse*shadow)+spec*_LightColor0.rgb*shadow,1);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback "Diffuse"
}
