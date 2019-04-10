Shader "Unlit/ZedPlane/White"
{
    Properties
    {
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

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            #define NUM_EYES 2
            float4x4 _TransformMatrices[NUM_EYES];

            v2f vert (appdata_base v)
            {
                v2f o;
                float4 viewPos = mul(_TransformMatrices[unity_StereoEyeIndex], v.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, viewPos);
                // o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(1,1,1,1);
                return col;
            }
            ENDCG
        }
    }
}
