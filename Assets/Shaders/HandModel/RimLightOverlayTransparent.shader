Shader "Custom/RimLightOverlayTransparent" {
	Properties {
		_RimColor ("Rim Color", Color) = (1,1,1,1)
		_RimPower ("Rim Power", Range(0.1, 10)) = 3
	}
	SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
		LOD 200
		Cull Back
		ZWrite Off
		ZTest Always

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input {
			float3 worldNormal;
			float3 worldPos;
		};

		fixed4 _RimColor;
		float _RimPower;
		float3 _WorldSpaceCenterCameraPos;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 viewDir = normalize(_WorldSpaceCenterCameraPos - IN.worldPos);
			float rim = pow(1.0 - saturate(dot(viewDir, IN.worldNormal)), _RimPower);
			o.Emission = _RimColor.rgb;
			o.Alpha = rim * _RimColor.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
