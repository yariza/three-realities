// Vertex format:
// position.xyz = vertex position
//
// Position buffer format:
// .xyz = particle position
// .w   = life (+0.5 -> -0.5)
//
//
Shader "ZedParticle/Opaque"
{
    Properties
    {
        _PositionBuffer ("-", 2D) = "black"{}
        _ColorBuffer ("-", 2D) = "black"{}

        [Enum(Add, 0, AlphaBlend, 1)]
        _BlendMode ("-", Float) = 0

        [KeywordEnum(Single, Animate)]
        _ColorMode ("-", Float) = 0

        [HDR] _Color  ("-", Color) = (1, 1, 1, 1)
        [HDR] _Color2 ("-", Color) = (0.5, 0.5, 0.5, 1)

        _MainTex      ("-", 2D) = "white"{}

        _ScaleMin ("-", Float) = 1
        _ScaleMax ("-", Float) = 1

        _ParticleSize ("Particle Size", float) = 0.01

        _RandomSeed ("-", Float) = 0

        _InstanceOffset ("-", Int) = 0
    }
    CGINCLUDE

    #include "UnityCG.cginc"
    #include "Common.cginc"

    sampler2D _MainTex;
    float4 _MainTex_ST;
    half _BlendMode;
    float _ParticleSize;

    uint _InstanceOffset;

    struct appdata
    {
        float4 vertex : POSITION;
    };

    struct v2f
    {
        float4 position : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 color : COLOR;
        // float psize : TEXCOORD1;
    };

    #define SQRT_THREE 1.73205080757
    #define SQRT_THREE_HALF 0.86602540378

    static float2 s_Triangle[3] =
    {
        float2(-SQRT_THREE_HALF, -0.5),
        float2 (0.0, 1.0),
        float2 (SQRT_THREE_HALF, -0.5)
    };

    v2f vert(appdata v, uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
    {
        uint iid = instance_id + _InstanceOffset;
        float4 uv = float4(
            clamp(fmod(iid, _PositionBuffer_TexelSize.z) * _PositionBuffer_TexelSize.x, _PositionBuffer_TexelSize.x, 1.0 - _PositionBuffer_TexelSize.x),
            clamp(((iid -fmod(iid, _PositionBuffer_TexelSize.z) * _PositionBuffer_TexelSize.x) / _PositionBuffer_TexelSize.z) * _PositionBuffer_TexelSize.y, _PositionBuffer_TexelSize.y, 1.0 - _PositionBuffer_TexelSize.y),
            0.0, 0.0
        );

        float4 p = tex2Dlod(_PositionBuffer, uv);
        float4 c = tex2Dlod(_ColorBuffer, uv);

        float l = p.w + 0.5;
        float s = calc_scale(uv, l);

        float psize = _ParticleSize * s;
        float2 aspect = float2(_ScreenParams.y * _ScreenParams.z - _ScreenParams.y, 1.0);

        v2f o;

        p = UnityWorldToClipPos(p.xyz);
        p.xy += s_Triangle[vertex_id] * psize * aspect;

        // o.position = UnityWorldToClipPos(p.xyz);
        // o.uv = float2(0,0);
        o.position = p;
        o.uv = s_Triangle[vertex_id] * 2.0;
        o.color = calc_color(uv, l) * c;
        // o.psize = _ParticleSize * s;
        return o;
    }

    // [maxvertexcount(3)]
    // void geom (point v2f input[1], inout TriangleStream<v2f> outputStream) {
    //     v2f newVertex;
    //     newVertex.color = input[0].color;
    //     float2 newxy;
    //     // float psize = input[0].psize * input[0].position.w;
    //     float psize = input[0].psize;
    //     float2 aspect = float2(_ScreenParams.y * _ScreenParams.z - _ScreenParams.y, 1.0);

    //     newVertex.psize = 0;

    //     newxy = input[0].position.xy + float2 (-SQRT_THREE_HALF, -0.5) * psize * aspect;
    //     newVertex.position = float4(newxy.x, newxy.y, input[0].position.z, input[0].position.w);
    //     newVertex.uv = float2 (-SQRT_THREE, -1.0);
    //     outputStream.Append(newVertex);

    //     newxy = input[0].position.xy + float2 (0.0, 1.0) * psize * aspect;
    //     newVertex.position = float4(newxy.x, newxy.y, input[0].position.z, input[0].position.w);
    //     newVertex.uv = float2 (0.0, 2.0);
    //     outputStream.Append(newVertex);

    //     newxy = input[0].position.xy + float2 (SQRT_THREE_HALF, -0.5) * psize * aspect;
    //     newVertex.position = float4(newxy.x, newxy.y, input[0].position.z, input[0].position.w);
    //     newVertex.uv = float2 (SQRT_THREE, -1.0);
    //     outputStream.Append(newVertex);
    // }

    half4 frag(v2f i) : SV_Target
    {
        float2 off = i.uv;
        clip(1 - dot(off, off));
        fixed4 col = fixed4(i.color);
        return col;
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
		ZWrite On
		ZTest On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
			// #pragma geometry geom
            #pragma fragment frag
            ENDCG
        }
    }
}
