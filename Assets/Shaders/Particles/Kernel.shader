Shader "Hidden/ZEDParticles/Kernel"
{
    Properties
    {
        _PositionBuffer ("-", 2D) = ""{}
        _ColorBuffer ("-", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _PositionBuffer;
    sampler2D _ColorBuffer;
    float4 _ColorBuffer_TexelSize;

    sampler2D _XYZTex;
    sampler2D _ColorTex;

    float4x4 _CameraViewMat;
    float2 _LifeParams;
    float4 _Config;

#if USE_HAND_POSITION
    #define HANDS_COUNT 2
    float4 _HandCenters[HANDS_COUNT];

    sampler2D _HandTex;
    float _HandRadius;
    // float4x4 _CameraVPMat;
#endif

    // PRNG function
    float nrand(float2 uv, float salt)
    {
        uv += float2(salt, _Config.y);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    float nrand(float3 uvt, float salt)
    {
        uvt += float3(salt, _Config.y, salt * 0.193456);
        return frac(sin(dot(uvt, float3(12.9898, 78.233, 56.234))) * 43758.5453);
    }

    float4 new_particle_position(float2 uv)
    {
        float t = _Config.w;
        uv += float2(
            nrand(float3(uv, _Time.x), 11),
            nrand(float3(uv, _Time.x), 12)
        ) * _ColorBuffer_TexelSize.xy;

        // Random position
        float4 XYZPos = float4(tex2Dlod(_XYZTex, float4(uv, 0.0, 0.0)).rgb ,1.0f);
        float3 p = mul(_CameraViewMat, XYZPos).xyz;

        bool throttle = (frac(nrand(uv, 13) + _Time.y) > _Config.x);

    #if USE_HAND_POSITION
        // float4 screenCoord = mul(_CameraVPMat, float4(p, 1.0));
        // screenCoord = ComputeScreenPos(screenCoord);
        // screenCoord.xy /= screenCoord.w;
        bool clipHand = tex2Dlod(_HandTex, float4(uv.xy, 0.0, 0.0)).r > 0.5;
        float3 leftHandPos = p - _HandCenters[0];
        float3 rightHandPos = p - _HandCenters[1];
        float sqDist = _HandRadius * _HandRadius;
        bool handRange = (dot(leftHandPos, leftHandPos) > sqDist) && (dot(rightHandPos, rightHandPos) > sqDist);
        // throttle = throttle || clipHand || handRange;
        throttle = throttle || clipHand || handRange;
    #endif

        // Throttling: discards particle emission by adding offset.
        float4 offs = float4(1e8, 1e8, 1e8, -1) * throttle;

        return float4(p, 0.5) + offs;
    }

    float4 new_particle_color(float2 uv)
    {
        uv += float2(
            nrand(float3(uv, _Time.x), 11),
            nrand(float3(uv, _Time.x), 12)
        ) * _ColorBuffer_TexelSize.xy;

        float4 color = float4(tex2Dlod(_ColorTex, float4(uv, 0.0, 0.0)).bgr, 1.0f);
        return color;
    }

    // pass 0
    float4 frag_init_position(v2f_img i) : SV_Target
    {
        // Crate a new particle and randomize its initial life.
        return float4(0, 0, 0, nrand(i.uv, 14) - 0.5);
    }

    // pass 1
    float4 frag_init_color(v2f_img i) : SV_Target
    {
        // return new_particle_color(i.uv);
        return float4(0,0,0,1.0);
    }

    // pass 2
    float4 frag_update_position(v2f_img i) : SV_Target
    {
        float4 p = tex2D(_PositionBuffer, i.uv);

        // Decaying
        float dt = _Config.z;
        p.w -= lerp(_LifeParams.x, _LifeParams.y, nrand(i.uv, 12)) * dt;

        if (p.w > -0.5)
        {
            return p;
        }
        else
        {
            return new_particle_position(i.uv);
        }
    }

    // pass 3
    float4 frag_update_color(v2f_img i) : SV_Target
    {
        float4 p = tex2D(_PositionBuffer, i.uv);
        float4 c = tex2D(_ColorBuffer, i.uv);

        if (p.w < 0.5)
        {
            return c;
        }
        else
        {
            return new_particle_color(i.uv);
        }
    }

    ENDCG

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_init_position
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_init_color
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_update_position
            #pragma multi_compile __ USE_HAND_POSITION
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_update_color
            #pragma multi_compile __ USE_HAND_POSITION
            ENDCG
        }
    }
}
