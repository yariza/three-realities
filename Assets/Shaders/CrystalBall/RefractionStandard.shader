Shader "Custom/RefractionStandard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _RefractionTex ("Refraction Texture", Cube) = "" {}
        _RefractionIndex ("Refraction Index", Range(0, 10)) = 1
        _Fresnel ("Fresnel Coefficient", float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        samplerCUBE _RefractionTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 viewDir;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _RefractionIndex;
        float _Fresnel;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

            half3 n = normalize (IN.worldNormal);
            half3 v = normalize (IN.viewDir);
            // half fr = pow(1.0f - dot(v, n), _Fresnel) * _Reflectance;

            // half3 reflectDir = reflect(-v, n);
            half3 refractDir = refract(-v, n, _RefractionIndex);

            // half3 reflectColor = texCUBE (_EnvTex, reflectDir).rgb;
            half3 refractColor = texCUBE (_RefractionTex, refractDir, float3(0,0,0), float3(0,0,0)).rgb;
            half fr = pow(dot(v, n), _Fresnel);
            o.Emission = refractColor * fr;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
