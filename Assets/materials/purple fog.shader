Shader "Unlit/purple fog"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _FogColorA ("Fog Color A", Color) = (0.14, 0.03, 0.28, 0.65)
        _FogColorB ("Fog Color B", Color) = (0.46, 0.12, 0.72, 0.9)
        _Density ("Fog Density", Range(0, 2)) = 0.8
        _Alpha ("Fog Alpha", Range(0, 1)) = 0.72
        _SpeedX ("Flow Speed X", Range(-2, 2)) = 0.08
        _SpeedY ("Flow Speed Y", Range(-2, 2)) = 0.05
        _NoiseScale ("Noise Scale", Range(0.1, 8)) = 1.8
        _DetailScale ("Detail Scale", Range(0.1, 12)) = 4.2
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.35
        _NoiseContrast ("Noise Contrast", Range(0.5, 4)) = 1.9
        _Softness ("Edge Softness", Range(0.1, 8)) = 2.5
        _PlaneFill ("Plane Fill", Range(0, 1)) = 0.35
        _HeightFade ("Height Fade", Range(0, 4)) = 1.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

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
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _FogColorA;
            float4 _FogColorB;
            float _Density;
            float _Alpha;
            float _SpeedX;
            float _SpeedY;
            float _NoiseScale;
            float _DetailScale;
            float _DetailStrength;
            float _NoiseContrast;
            float _Softness;
            float _PlaneFill;
            float _HeightFade;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos.xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 flow = float2(_SpeedX, _SpeedY) * _Time.y;
                float2 uvA = i.uv * _NoiseScale + flow;
                float2 uvB = i.uv * (_NoiseScale * 1.7) - flow * 0.65;
                float2 uvC = i.uv * _DetailScale + flow * 1.35;

                float nA = tex2D(_MainTex, uvA).r;
                float nB = tex2D(_MainTex, uvB).g;
                float nC = tex2D(_MainTex, uvC).b;

                float baseNoise = nA * 0.55 + nB * 0.30 + nC * _DetailStrength;
                float noise = saturate(pow(saturate(baseNoise * _Density), _NoiseContrast));

                float3 N = normalize(i.worldNormal);
                float3 V = normalize(i.viewDir);
                float edge = pow(1.0 - saturate(dot(N, V)), _Softness);
                float topFace = saturate(dot(N, float3(0, 1, 0)));

                float heightMask = saturate(1.0 - i.worldPos.y * _HeightFade * 0.1);
                float blend = saturate(noise * 0.82 + edge * 0.18);

                float3 finalColor = lerp(_FogColorA.rgb, _FogColorB.rgb, blend);
                float alpha = saturate((_Alpha * noise + edge * 0.18 + topFace * _PlaneFill) * heightMask);

                fixed4 finalCol = fixed4(finalColor, alpha);
                UNITY_APPLY_FOG(i.fogCoord, finalCol);
                return finalCol;
            }
            ENDCG
        }
    }
}
