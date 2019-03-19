sampler2D _PositionBuffer;
float4 _PositionBuffer_TexelSize;
sampler2D _ColorBuffer;

half _ColorMode;
half4 _Color;
half4 _Color2;
float _ScaleMin;
float _ScaleMax;
float _RandomSeed;

// PRNG function
float nrand(float2 uv, float salt)
{
    uv += float2(salt, _RandomSeed);
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

// Scale factor function
float calc_scale(float2 uv, float time01)
{
    float s = lerp(_ScaleMin, _ScaleMax, nrand(uv, 14));
    // Linear scaling animation with life.
    // (0, 0) -> (0.1, 1) -> (0.9, 1) -> (1, 0)
    return s * min(1.0, 5.0 - abs(5.0 - time01 * 10));
}

// Color function
float4 calc_color(float2 uv, float time01)
{
    return lerp(_Color, _Color2, (1.0 - time01) * _ColorMode);
}
