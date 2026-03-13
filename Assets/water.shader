Shader "Unlit/water"
{
    Properties
    {
        _MainTex ("Surface Texture", 2D) = "white" {}
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.8, 1.0, 0.75)
        _DeepColor ("Deep Color", Color) = (0.03, 0.2, 0.45, 0.75)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(0, 10)) = 2.5
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.2
        _ToonSteps ("Toon Steps", Range(2, 8)) = 4
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.78
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.5
        _FlowSpeed ("Flow Speed", Range(0, 4)) = 1.1
        _Distortion ("Flow Distortion", Range(0, 0.2)) = 0.05
        _GlowIntensity ("Glow Intensity", Range(0, 4)) = 1.4
        _Alpha ("Orb Alpha", Range(0, 1)) = 0.8
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 1.8
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.2
        _TextureStrength ("Texture Strength", Range(0, 2)) = 1.25
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend One OneMinusSrcAlpha
        ZWrite Off

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
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ShallowColor;
            float4 _DeepColor;
            float4 _FoamColor;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _ToonSteps;
            float _FoamThreshold;
            float _RimPower;
            float _RimStrength;
            float _FlowSpeed;
            float _Distortion;
            float _GlowIntensity;
            float _Alpha;
            float _PulseSpeed;
            float _PulseAmount;
            float _TextureStrength;

            v2f vert (appdata v)
            {
                v2f o;

                float3 objN = normalize(v.normal);
                float wave = sin((objN.x + objN.z) * _WaveFrequency + _Time.y * _WaveSpeed)
                           * cos((objN.y - objN.x) * _WaveFrequency + _Time.y * (_WaveSpeed * 1.2));
                v.vertex.xyz += objN * (wave * _WaveAmplitude * 0.6);

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos.xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float flowT = _Time.y * _FlowSpeed;
                float2 flowDir1 = float2(0.8, 0.35);
                float2 flowDir2 = float2(-0.45, 0.7);

                float2 uv1 = i.uv + flowDir1 * flowT;
                float2 uv2 = i.uv + flowDir2 * flowT * 0.7;

                fixed4 texA = tex2D(_MainTex, uv1);
                fixed4 texB = tex2D(_MainTex, uv2);
                float texMix = texA.r * 0.6 + texB.g * 0.4;

                float2 distortedUv = i.uv + (texA.rg * 2.0 - 1.0) * _Distortion;
                fixed4 col = tex2D(_MainTex, distortedUv);

                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                float ndl = saturate(dot(N, L));
                float toon = floor(ndl * _ToonSteps) / max(_ToonSteps - 1.0, 1.0);

                float fresnel = pow(1.0 - saturate(dot(N, V)), _RimPower);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float4 waterColor = lerp(_DeepColor, _ShallowColor, saturate(0.15 + fresnel + texMix * (0.35 * _TextureStrength)));

                float foamMask = step(_FoamThreshold, ndl + (col.r * 0.2) + fresnel * 0.7 + texMix * 0.2);
                float3 finalRgb = waterColor.rgb * (0.32 + 0.68 * toon);
                finalRgb = lerp(finalRgb, _FoamColor.rgb, fresnel * _RimStrength);
                finalRgb = lerp(finalRgb, _FoamColor.rgb, foamMask * 0.5);

                float textureDetail = saturate(col.r * 0.55 + texA.b * 0.35 + texB.g * 0.25);
                finalRgb *= (0.85 + textureDetail * (0.45 * _TextureStrength));

                finalRgb *= (_GlowIntensity * pulse);

                fixed4 finalCol = fixed4(finalRgb, saturate(_Alpha * waterColor.a));
                UNITY_APPLY_FOG(i.fogCoord, finalCol);
                return finalCol;
            }
            ENDCG
        }
    }
}
