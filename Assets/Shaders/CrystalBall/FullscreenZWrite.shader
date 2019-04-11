Shader "Unlit/FullscreenZWrite"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        ZWrite On
        ColorMask 0
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex FullscreenTriangle
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            //By CeeJay.dk
            //License : CC0 - http://creativecommons.org/publicdomain/zero/1.0/

            //Basic Buffer/Layout-less fullscreen triangle vertex shader
            void FullscreenTriangle(in uint id : SV_VertexID, out float4 position : SV_Position, out float2 texcoord : TEXCOORD0)
            {
                /*
                //See: https://web.archive.org/web/20140719063725/http://www.altdev.co/2011/08/08/interesting-vertex-shader-trick/

                1
                ( 0, 2)
                [-1, 3]   [ 3, 3]
                    .
                    |`.
                    |  `.
                    |    `.
                    '------`
                0         2
                ( 0, 0)   ( 2, 0)
                [-1,-1]   [ 3,-1]

                ID=0 -> Pos=[-1,-1], Tex=(0,0)
                ID=1 -> Pos=[-1, 3], Tex=(0,2)
                ID=2 -> Pos=[ 3,-1], Tex=(2,0)
                */

                texcoord.x = (id == 2) ?  2.0 :  0.0;
                texcoord.y = (id == 1) ?  2.0 :  0.0;

                position = float4(texcoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 1.0, 1.0);
            }

            float frag (v2f i) : Depth
            {
                float depth;
                #if defined(UNITY_REVERSED_Z)
                depth = 1;
                #else
                depth = 0;
                #endif
                return depth;
            }
            ENDCG
        }
    }
}
