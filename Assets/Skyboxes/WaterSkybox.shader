Shader "Custom/WaterSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.05, 0.35, 0.65, 1)
        _HorizonColor ("Horizon Color", Color) = (0.16, 0.55, 0.78, 1)
        _BottomColor ("Bottom Color", Color) = (0.00, 0.12, 0.22, 1)
        _LightColor ("Light Color", Color) = (0.70, 0.92, 1.0, 1)
        _LightDirection ("Light Direction", Vector) = (0.1, 1.0, 0.2, 0)
        _WaveStrength ("Wave Strength", Range(0, 0.2)) = 0.04
        _WaveFrequency ("Wave Frequency", Range(0.1, 10)) = 2.4
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 0.6
        _DetailScale ("Detail Scale", Range(0.1, 20)) = 5.0
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.18
        _DetailScroll ("Detail Scroll", Range(0, 3)) = 0.25
        _LightShaftIntensity ("Light Shaft Intensity", Range(0, 4)) = 1.2
        _LightShaftFocus ("Light Shaft Focus", Range(1, 64)) = 14
        _LightFragmentScale ("Light Fragment Scale", Range(0.5, 20)) = 8.0
        _LightFragmentIntensity ("Light Fragment Intensity", Range(0, 2)) = 0.75
        _LightFragmentSpeed ("Light Fragment Speed", Range(0, 5)) = 0.8
        _Exposure ("Exposure", Range(0, 8)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            fixed4 _LightColor;
            float4 _LightDirection;
            float _WaveStrength;
            float _WaveFrequency;
            float _WaveSpeed;
            float _DetailScale;
            float _DetailStrength;
            float _DetailScroll;
            float _LightShaftIntensity;
            float _LightShaftFocus;
            float _LightFragmentScale;
            float _LightFragmentIntensity;
            float _LightFragmentSpeed;
            float _Exposure;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float noise2d(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += noise2d(p) * amplitude;
                    p = p * 2.03 + float2(17.7, 9.2);
                    amplitude *= 0.5;
                }
                return value;
            }

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);

                // Vertical blend factor (0=bottom, 1=top)
                float h = saturate(dir.y * 0.5 + 0.5);

                // Animated horizon wave ripple
                float t = _Time.y * _WaveSpeed;
                float wave = sin((dir.x + dir.z) * _WaveFrequency + t) * _WaveStrength;
                float horizonBand = saturate(1.0 - abs((h + wave) - 0.5) * 2.0);

                fixed3 sky = lerp(_BottomColor.rgb, _TopColor.rgb, h);
                sky = lerp(sky, _HorizonColor.rgb, horizonBand * 0.75);

                // Water texture detail / depth breakup
                float2 detailUV = dir.xz * _DetailScale + float2(t * _DetailScroll, -t * _DetailScroll * 0.7);
                float detailA = fbm(detailUV);
                float detailB = fbm(detailUV * 1.7 + float2(11.3, 5.7));
                float detail = (detailA * 0.7 + detailB * 0.3);
                sky *= (1.0 + (detail - 0.5) * _DetailStrength);

                // Light shafts / fragments shining down through water
                float3 lightDir = normalize(_LightDirection.xyz);
                float towardLight = saturate(dot(dir, lightDir));
                float shaftMask = pow(towardLight, _LightShaftFocus) * saturate(h + 0.1);

                float2 fragUV = (dir.xz / max(0.2, dir.y + 0.45)) * _LightFragmentScale;
                fragUV += float2(_Time.y * _LightFragmentSpeed, -_Time.y * _LightFragmentSpeed * 0.6);
                float fragmentNoise = fbm(fragUV);
                float fragments = smoothstep(0.55, 0.95, fragmentNoise) * _LightFragmentIntensity;

                float lightContribution = shaftMask * (0.35 + fragments) * _LightShaftIntensity;
                sky += _LightColor.rgb * lightContribution;

                sky *= _Exposure;

                return fixed4(sky, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
