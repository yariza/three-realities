Shader "Unlit/ZedAttraction"
{
	Properties
	{
        _ParticleSize ("Particle Size", float) = 0.1
		_ParticleSizeBump ("Particle Size Bump", float) = 0.3
        [Toggle] _Distance("Apply Distance", Float) = 1
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

			sampler2D _DepthTex;

			// sampler2D _XYZTex;
			sampler2D _ColorTex;
			float4 _DepthTex_TexelSize;

			float4x4 _Position;
			float4x4 _InverseViewMatrix;
			float4x4 _ViewMatrix;
			float3 _CameraPosition;

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

			v2f vert (appdata_full v, uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				v2f o;

				float2 uv = float2(
							clamp(fmod(instance_id, _TexelSize.z) * _TexelSize.x, _TexelSize.x, 1.0 - _TexelSize.x),
							clamp(((instance_id -fmod(instance_id, _TexelSize.z) * _TexelSize.x) / _TexelSize.z) * _TexelSize.y, _TexelSize.y, 1.0 - _TexelSize.y)
							);

				float depth = tex2Dlod(_DepthTex, float4(uv, 0.0, 0.0)).x;
				if (isinf(depth) && depth > 0) depth = 20.0;
				if (isinf(depth) && depth < 0) depth = 0.1;

				float4 dir = float4(-0.5 + uv.x, 0.5 - uv.y, 0.0, 1.0);
				dir = mul(_Position, dir);
				dir.xyz *= depth / dir.z;

				float4 origDir = dir;
				origDir.z *= -1.0;
				origDir = mul(UNITY_MATRIX_P, float4(origDir.xyz, 1.0));

				float4 screenCoord = ComputeScreenPos(origDir);
				screenCoord.xy /= screenCoord.w;
				float mask = tex2Dlod(_HandMaskTex, float4(screenCoord.x, screenCoord.y, 0, 0)).r;

				// model matrix is identity
				float3 worldPos = mul(_InverseViewMatrix, dir).xyz;

				float3 gridIndex = worldPos * _PhysicsGridSizeInv.xyz;

				float3 posOffset = _PhysicsGridPositionTex.SampleLevel(_TrilinearRepeatSampler, gridIndex, 0).xyz;
				posOffset *= step(mask, 0.5);
				worldPos += posOffset;

				// float3 center = float3(0, 0.75, 0);
				// float3 distDir = worldPos - center;
				// distDir.y = 0;
				// float dist = length(distDir);
				// float offset = (sin(dist * 10.0 - _Time.y * 3.0)) * 0.1;
				// offset *= pow(2.0, -dist);
				// offset *= (1.0 - mask);
				// worldPos.y += offset;

				// worldPos += normalize(distDir) * offset * 0.1;

				dir = mul(_ViewMatrix, float4(worldPos, 1.0));

				dir.z *= -1.0;
				dir = mul(UNITY_MATRIX_P, float4(dir.xyz, 1.0));

				o.position = dir;

				float3 color = tex2Dlod(_ColorTex, float4(uv, 0.0, 0.0)).bgr;
				// color = lerp(color, _GrabColor.rgb, saturate((posOffset.a + 1.0)) * (1.0 - mask) * _GrabColorStrength);
				// color *= frac(gridIndex);
				// color *= offset;
				// o.color = float4(color, 1.0);

				// float alpha;
				// alpha = frac(dist * -0.05 + _Time.y * 0.15);
				// alpha = pow(saturate(1.0 - alpha * 1.7), 1.5);
				// alpha *= saturate(0.8 / dist);
				o.color = float4(color, 1.0);
				o.uv = float2(0,0);
				o.psize = _ParticleSize + _ParticleSizeBump * length(posOffset);

				// UNITY_TRANSFER_FOG(o,o.position);
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

                // newxy = input[0].position.xy + float2 (0.5, 0.5) * psize * aspect;
                // newVertex.position = float4(newxy.x, newxy.y, input[0].position.z, input[0].position.w);
                // newVertex.uv = float2 (1.0, 1.0);
                // outputStream.Append(newVertex);
            }

			fixed4 frag (v2f i) : SV_Target
			{
				float2 off = i.uv;
				clip(1 - dot(off, off));
				// sample the texture
				fixed4 col = fixed4(i.color);
				// outDepth = i.depth;
				// apply fog
				// UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
