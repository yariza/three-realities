Shader "Unlit/ImposterPortal"
{
    Properties
    {
        _Scale ("Scale", Float) = 1
        _Radius ("Radius", Float) = 0.5
        _NoiseMagnitude("Noise Magnitude", Float) = 0.2
        _NoiseFrequency("Noise Frequency", Range(0, 10)) = 1
        _NoiseMotion("Noise Motion", Range(0, 10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        ZWrite On
        // ColorMask 0
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "./SimplexNoise3D.cginc"

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            struct fragOut
            {
                float4 color : SV_Target;
                float depth : Depth;
            };

            float _Scale;
            float _Radius;

            float _NoiseMagnitude;
            float _NoiseFrequency;
            float _NoiseMotion;

            v2f vert (appdata_base v)
            {
                v2f o;

                float3 viewPos = UnityObjectToViewPos(float4(0.0, 0.0, 0.0, 1.0))
                    + float4(v.vertex.xyz, 0.0) * _Scale;
                float3 worldPos = mul(UNITY_MATRIX_I_V, float4(viewPos, 1.0)).xyz;

                o.worldPos = worldPos;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                return o;
            }

            float raySphereIntersect(float3 r0, float3 rd, float3 s0, float sr)
            {
                // - r0: ray origin
                // - rd: normalized ray direction
                // - s0: sphere center
                // - sr: sphere radius
                // - Returns distance from r0 to first intersecion with sphere,
                //   or -1.0 if no intersection.
                float a = dot(rd, rd);
                float3 s0_r0 = r0 - s0;
                float b = 2.0 * dot(rd, s0_r0);
                float c = dot(s0_r0, s0_r0) - (sr * sr);
                if (b*b - 4.0*a*c < 0.0) {
                    return -1.0;
                }
                return (-b - sqrt((b*b) - 4.0*a*c))/(2.0*a);
            }

            float map(float3 pos, float3 center)
            {
                float3 r = pos - center;
                float3 rn = normalize(r);
                float noise = snoise(rn * _NoiseFrequency + _Time.yyy * _NoiseMotion * float3(0.13, 0.82, 0.11));
                return length(r) - _Radius + noise * _NoiseMagnitude;
            }

            fragOut frag (v2f i)
            {
                float3 worldPos = i.worldPos;
                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(worldPos - ro);
                float3 so = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
                // float sr = _Radius;

                const float eps = 1e-4;

                bool hit = false;

                for (uint i = 0; i < 50; i++)
                {
                    float t = map(ro, so);
                    ro += t * rd;

                    if (t < eps)
                    {
                        hit = true;
                        break;
                    }
                }

                if (!hit) discard;

                // float t = raySphereIntersect(ro, rd, so, sr);
                // clip(t);

                fragOut o;
                #if defined(UNITY_REVERSED_Z)
                o.depth = 0;
                #else
                o.depth = 1;
                #endif
                o.color = float4(0, 0, 0, 1);
                return o;
            }
            ENDCG
        }
    }
}
