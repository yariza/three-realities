Shader "Unlit/ZedGrid"
{
	Properties
	{
        _PointSize("Point Size", Float) = 0.05
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
                half psize : PSIZE;
				// float depth : TEXCOORD0;
                UNITY_FOG_COORDS(0)
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

			float _PointSize;

			float4 _HandLeftPosition;
			float4 _HandRightPosition;

			v2f vert (appdata_full v, uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				v2f o;

				o.normal = v.normal;

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

				// model matrix is identity
				float3 worldPos = mul(_InverseViewMatrix, dir).xyz;
				dir = mul(_ViewMatrix, float4(worldPos, 1.0));

				dir.z *= -1.0;
				dir = mul(UNITY_MATRIX_P, float4(dir.xyz, 1.0));

				o.position = dir;

				float dist = length(worldPos - _CameraPosition);
				// float3 color = tex2Dlod(_ColorTex, float4(uv, 0.0, 0.0)).bgr;
				// o.color = float4(color, 1.0);

				float alpha;
				float3 grid = abs(frac(worldPos * 10.0) - 0.5);
				alpha = min(grid.x, min(grid.y, grid.z));
				alpha = smoothstep(0.02, 0.01, alpha);

				// alpha = frac(dist * -0.05 + _Time.y * 0.15);
				// alpha = pow(saturate(1.0 - alpha * 1.7), 1.5);
				alpha *= saturate(0.03 / dist);
				o.color = float4(alpha, alpha, alpha, 1.0);

				o.psize = _PointSize;

				UNITY_TRANSFER_FOG(o,o.position);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = fixed4(i.color);
				// outDepth = i.depth;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
