// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/RefractionUnlit"
{
    Properties
    {
        _RefractionTex ("Refraction Texture", Cube) = "" {}
        _RefractionIndex ("Refraction Index", Range(0, 10)) = 1
        _Fresnel ("Fresnel Coefficient", float) = 5.0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimFresnel ("Rim Fresnel", Range(0.01, 10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct v2f
            {
                float3 viewDir : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            float _RefractionIndex;
            float _Fresnel;
            samplerCUBE _RefractionTex;
            float4 _RimColor;
            float _RimFresnel;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // compute world space view direction
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                // world space normal
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = worldViewDir;
                o.worldNormal = worldNormal;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half3 n = normalize(i.worldNormal);
                half3 v = normalize(i.viewDir);
                half3 refractDir = refract(-v, n, _RefractionIndex);

                half3 refractColor = texCUBE (_RefractionTex, refractDir, float3(0,0,0), float3(0,0,0)).rgb;
                half fr = pow(dot(v, n), _Fresnel);

                half3 rimColor = _RimColor.rgb;
                half rim = pow(1.0 - dot(v, n), _RimFresnel);

                // sample the texture
                fixed4 col;
                col.a = 1.0;
                col.rgb = refractColor * fr + rimColor * rim;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
