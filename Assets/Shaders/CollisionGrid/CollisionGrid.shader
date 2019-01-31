Shader "Unlit/CollisionGrid"
{
	Properties
	{
		_GridColor("Grid Color", Color) = (1,1,1,1)
		_GridFrequency("Grid Frequency", Vector) = (10, 0, 10, 0)
		_GridWireWidth("Grid Wire Width", Range(0, 0.5)) = 0.05
		_GridAntialias("Grid AntiAlias", Range(0, 0.1)) = 0.01
		_CollisionPointRange("Collision Point Range", Range(0, 1)) = 0.3
		_CollisionPointFalloff("Collision Point Falloff", Range(0.01, 10)) = 1
	}
	SubShader
	{
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			float4 _GridColor;
			float3 _GridFrequency;
			float _GridWireWidth;
			float _GridAntialias;
			float4 _CollisionPoints[10];
			int _CollisionPointsLength;
			float _CollisionPointRange;
			float _CollisionPointFalloff;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 grid = frac(i.worldPos * _GridFrequency.xyz) - 0.5;
				float a = min(min(abs(grid.x), abs(grid.y)), abs(grid.z));
				a = smoothstep(_GridWireWidth + _GridAntialias, _GridWireWidth, a);

				float glow = 0.0;
				for (int index = 0; index < _CollisionPointsLength; index++)
				{
					float4 p = _CollisionPoints[index];
					float dist = length(i.worldPos - p.xyz);
					glow = max(glow, (1.0 - pow(saturate(dist / _CollisionPointRange), _CollisionPointFalloff)) * p.w);
				}
				a *= glow;

				fixed4 col = fixed4(_GridColor.rgb, a * _GridColor.a);
				return col;
			}
			ENDCG
		}
	}
}
