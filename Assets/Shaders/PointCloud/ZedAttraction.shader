Shader "Unlit/ZedAttraction"
{
	Properties
	{
        _ParticleSize ("Particle Size", float) = 0.1
		_ParticleSizeBump ("Particle Size Bump", float) = 0.3
        [Toggle] _Distance("Apply Distance", Float) = 1
		[Toggle(SIZE_IN_PIXELS)] _SizeInPixels("Particle Size in Pixels", Float) = 0
		_HandMaskTex("Hand Mask Texture", 2D) = "gray" {}
		_PhysicsGridPositionTex("Position Texture", 3D) = "white" {}

		_GrabColor ("Grab Color", Color) = (1,1,0.3,1)
		_GrabColorStrength("Grab Color Strength", Range(0, 1)) = 0.4
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off
		ZWrite On
		ZTest On
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			// make fog work
			// #pragma multi_compile_fog

			#pragma shader_feature SIZE_IN_PIXELS

			#include "UnityCG.cginc"

			struct v2f
			{
				float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float psize : TEXCOORD1;
				// float3 normal : NORMAL;
				// float depth : TEXCOORD0;
                // UNITY_FOG_COORDS(0)
			};

            sampler2D _ColorTextureLeft;
            sampler2D _ColorTextureRight;
            sampler2D _DepthTextureLeft;
            sampler2D _DepthTextureRight;
            sampler2D _NormalTextureLeft;
            sampler2D _NormalTextureRight;

			float4 _TexelSize;

			float _ParticleSize;
			float _ParticleSizeBump;

			float4 _HandLeftPosition;
			float4 _HandRightPosition;

			float4 _GrabColor;
			float _GrabColorStrength;

			sampler2D _HandMaskTex;

			Texture3D _PhysicsGridPositionTex;
			float3 _PhysicsGridSize;
			float3 _PhysicsGridSizeInv;
			// float4 _PhysicsGridPositionTex_TexelSize;
			Texture3D _PhysicsGridVelocityTex;
			SamplerState _TrilinearRepeatSampler;

			float4x4 _CameraViewMat;

            #define NUM_EYES 2
			float4x4 _Transform;
            float4x4 _TransformMatrices[NUM_EYES];
			float4x4 _PlaneMatrices[NUM_EYES];

			v2f vert (appdata_full v, uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				v2f o;

				float2 uv = float2(
							clamp(fmod(instance_id, _TexelSize.z) * _TexelSize.x, _TexelSize.x, 1.0 - _TexelSize.x),
							clamp(((instance_id -fmod(instance_id, _TexelSize.z) * _TexelSize.x) / _TexelSize.z) * _TexelSize.y, _TexelSize.y, 1.0 - _TexelSize.y)
							);

				float4 vertex = float4(uv.x - 0.5, uv.y - 0.5, 0.0, 1.0);

				uv.y = 1.0 - uv.y;

				// float4 localPos = mul(_PlaneMatrices[unity_StereoEyeIndex], vertex);

				float depth;
				if (unity_StereoEyeIndex > 0)
				{
					depth = tex2Dlod(_DepthTextureRight, float4(uv, 0.0, 0.0)).x;
				}
				else
				{
					depth = tex2Dlod(_DepthTextureLeft, float4(uv, 0.0, 0.0)).x;
				}
				if (isinf(depth) && depth > 0) depth = 20.0;
				if (isinf(depth) && depth < 0) depth = 0.1;

				// float mask = tex2Dlod(_HandMaskTex, float4(uv.x, uv.y, 0, 0)).r;
				float mask = 0;

				float3 worldPos = mul(_TransformMatrices[unity_StereoEyeIndex], vertex).xyz;
				float3 viewPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0));
				viewPos.xyz *= -depth / viewPos.z;

				worldPos = mul(UNITY_MATRIX_I_V, float4(viewPos.xyz, 1.0)).xyz;

				float3 gridIndex = worldPos * _PhysicsGridSizeInv.xyz;
				float3 posOffset = _PhysicsGridPositionTex.SampleLevel(_TrilinearRepeatSampler, gridIndex, 0).xyz;
				posOffset *= step(mask, 0.5);

				worldPos += posOffset;

				float4 pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

				o.position = pos;

				float3 color;
				if (unity_StereoEyeIndex > 0)
				{
					color = tex2Dlod(_ColorTextureRight, float4(uv, 0.0, 0.0)).bgr;
				}
				else
				{
					color = tex2Dlod(_ColorTextureLeft, float4(uv, 0.0, 0.0)).bgr;
				}
				o.color = float4(color, 1.0);
				o.uv = float2(0,0);
				o.psize = _ParticleSize + _ParticleSizeBump * length(posOffset);

				return o;
			}

			#define SQRT_THREE 1.73205080757
			#define SQRT_THREE_HALF 0.86602540378

            [maxvertexcount(3)]
            void geom (point v2f input[1], inout TriangleStream<v2f> outputStream) {
                v2f newVertex;
                newVertex.color = input[0].color;
                float2 newxy;
				float psize = input[0].psize * input[0].position.w;
				float2 aspect = float2(_ScreenParams.y * _ScreenParams.z - _ScreenParams.y, 1.0);

				#ifdef SIZE_IN_PIXELS
				psize *= (_ScreenParams.z - 1.0);
				#endif

				newVertex.psize = 0;

                newxy = input[0].position.xy + float2 (-SQRT_THREE_HALF, -0.5) * psize * aspect;
                newVertex.position = float4(newxy.x, newxy.y, input[0].position.z, input[0].position.w);
                newVertex.uv = float2 (-SQRT_THREE, -1.0);
                outputStream.Append(newVertex);

                newxy = input[0].position.xy + float2 (0.0, 1.0) * psize * aspect;
                newVertex.position = float4(newxy.x, newxy.y, input[0].position.z, input[0].position.w);
                newVertex.uv = float2 (0.0, 2.0);
                outputStream.Append(newVertex);

                newxy = input[0].position.xy + float2 (SQRT_THREE_HALF, -0.5) * psize * aspect;
                newVertex.position = float4(newxy.x, newxy.y, input[0].position.z, input[0].position.w);
                newVertex.uv = float2 (SQRT_THREE, -1.0);
                outputStream.Append(newVertex);
            }

			fixed4 frag (v2f i) : SV_Target
			{
				float2 off = i.uv;
				clip(1 - dot(off, off));
				// sample the texture
				fixed4 col = fixed4(i.color);
				return col;
			}
			ENDCG
		}
	}
}
