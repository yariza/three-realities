Shader "Unlit/ZedPlane/Color"
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
                float2 uv : TEXCOORD0;
            };

            #define NUM_EYES 2
            float4x4 _TransformMatrices[NUM_EYES];

            sampler2D _ColorTextureLeft;
            sampler2D _ColorTextureRight;

            v2f vert (appdata_base v)
            {
                v2f o;
                float4 viewPos = mul(_TransformMatrices[unity_StereoEyeIndex], v.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, viewPos);
                o.uv = float2(v.texcoord.x, 1.0 - v.texcoord.y);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 color;
                if (unity_StereoEyeIndex > 0)
                {
                    color = tex2D(_ColorTextureRight, i.uv).bgra;
                }
                else
                {
                    color = tex2D(_ColorTextureLeft, i.uv).bgra;
                }
                fixed4 col = fixed4(color.rgb, 1.0);
                return col;
            }
            ENDCG
        }
    }
}
