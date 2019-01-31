Shader "Custom/Interactable/Opaque" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0

		[Header(Occlusion Rim)]
		_RimColor ("Rim Color", Color) = (1,1,1,1)
		_RimPower ("Rim Power", Range(0.1, 10)) = 3
		_RimEmission ("Rim Emission Factor", Range(0, 1)) = 0

		[Header(Grab)]
		_ContactColor ("Contact Color", Color) = (0.3,0.3,1,1)
		_GrabColor ("Grab Color", Color) = (1,1,0,1)
		_GrabRadius ("Grab Radius", Range(0, 1.0)) = 0.02
		_GrabPower ("Grab Power", Range(0.1, 100)) = 0.5
		_GrabOffset ("Grab Offset", Range(-10, 10)) = 0
		_ContactThreshold ("Contact Threshold", Range(0, 100)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Transparent-1" }
		LOD 200

		ZTest Greater
		ZWrite Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		fixed4 _RimColor;
		float _RimPower;

		float _Grab;
		float4 _ContactColor;
		float4 _GrabColor;
		float _GrabRadius;
		float _GrabPower;
		int _ContactsLength;
		float4 _Contacts[10];

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

			float4 rimColor = lerp(_RimColor, _GrabColor, _Grab);
			rimColor.a *= rim;

			o.Emission = rimColor.rgb;
			o.Alpha = rimColor.a;
		}
		ENDCG

		ZTest LEqual
		ZWrite On

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
	    sampler2D _BumpMap;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		fixed4 _RimColor;
		float _RimPower;
		float _RimEmission;

		float _Grab;
		float4 _ContactColor;
		float4 _GrabColor;
		float _GrabRadius;
		float _GrabPower;
		float _GrabOffset;
		int _ContactsLength;
		float3 _Contacts[10];

		float _ContactThreshold;

		float3 _WorldSpaceCenterCameraPos;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)
		
		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;

			float contact = 0;

			for (int i = 0; i < _ContactsLength; i++)
			{
				float dist = length(IN.worldPos - _Contacts[i].xyz);
				dist = pow(1.0 - saturate(dist / _GrabRadius), _GrabPower);
				contact += dist;
			}

			contact += _GrabOffset;
			contact = smoothstep(_ContactThreshold - 0.01, _ContactThreshold, contact);

			float4 contactColor = lerp(_ContactColor, _GrabColor, _Grab);
			contactColor.a *= contact;

			float3 viewDir = normalize(_WorldSpaceCenterCameraPos - IN.worldPos);
			float rim = pow(1.0 - saturate(dot(viewDir, IN.worldNormal)), _RimPower);
			float4 rimColor = lerp(_RimColor, _GrabColor, _Grab);

			o.Emission = contactColor.rgb * contactColor.a + rimColor.rgb * rim * _RimEmission;
		}
		ENDCG

	}
	FallBack "Diffuse"
}
