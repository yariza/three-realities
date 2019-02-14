// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Unlit/HandMask"
{
    Properties
    {
        _Extrude ("Extrude Amount", Range(0, 1)) = 0
        // _RimPower ("Rim Power", Range(0.01, 3)) = 1
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        // BlendOp Add
        // Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				// float4 posWorld : TEXCOORD0;
				// float3 normalDir : TEXCOORD1;
            };

            float4 _Color;
            float _Extrude;
            // float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                o.vertex.xy += TransformViewToProjection(norm.xy) * _Extrude;
				// o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				// o.normalDir = normalize( mul( float4( v.normal, 0.0 ), unity_WorldToObject ).xyz );;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				// float3 normalDirection = i.normalDir;
				// float3 viewDirection = normalize( _WorldSpaceCameraPos.xyz - i.posWorld.xyz );

				// float rim = pow( saturate( dot( viewDirection, normalDirection ) ), _RimPower );

                // sample the texture
                fixed4 col = _Color;
                // col.rgb *= rim;
                return col;
            }
            ENDCG
        }
    }
}
