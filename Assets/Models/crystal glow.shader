Shader "Unlit/crystal glow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.45, 0.9, 1.0, 1)
        _GlowColor ("Glow Color", Color) = (0.2, 0.8, 1.0, 1)
        _GlowStrength ("Glow Strength", Range(0, 8)) = 2.5
        _Alpha ("Alpha", Range(0.05, 1)) = 0.55
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3
        _FresnelStrength ("Fresnel Strength", Range(0, 4)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            fixed4 _GlowColor;
            float _GlowStrength;
            float _Alpha;
            float _FresnelPower;
            float _FresnelStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDirWS = _WorldSpaceCameraPos.xyz - worldPos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(n, v)), _FresnelPower) * _FresnelStrength;

                fixed3 baseCol = tex.rgb * _TintColor.rgb;
                fixed3 glow = _GlowColor.rgb * (_GlowStrength + fresnel);

                fixed4 col;
                col.rgb = baseCol + glow;
                col.a = saturate(tex.a * _Alpha + fresnel * 0.35);
                return col;
            }
            ENDCG
        }
    }
}
