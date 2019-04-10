Shader "Unlit/ZedPlane/Depth"
{
    Properties
    {
        _Density("Density", Range(0, 1)) = 0.01
        _Color0("Color 0", Color) = (1, 1, 1, 1)
        _Color1("Color 1", Color) = (0, 0, 0, 0)
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

            float _Density;
            float4 _Color0;
            float4 _Color1;

            sampler2D _DepthTextureLeft;
            sampler2D _DepthTextureRight;
            sampler2D _NormalTextureLeft;
            sampler2D _NormalTextureRight;

            v2f vert (appdata_base v)
            {
                v2f o;
                float4 viewPos = mul(_TransformMatrices[unity_StereoEyeIndex], v.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, viewPos);
                o.uv = float2(v.texcoord.x, 1.0 - v.texcoord.y);
                // o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth;
                if (unity_StereoEyeIndex > 0)
                {
                    depth = tex2D(_DepthTextureRight, i.uv).r;
                }
                else
                {
                    depth = tex2D(_DepthTextureLeft, i.uv).r;
                }

                float fog = exp(-depth * depth * _Density);
                float3 fogColor = lerp(_Color1.rgb, _Color0.rgb, saturate(fog));

                fixed4 col = fixed4(fogColor, 1.0);
                return col;
            }
            ENDCG
        }
    }
}
