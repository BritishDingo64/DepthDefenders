Shader "Custom/UnderwaterLightRay"
{
    Properties
    {
        _Color ("Color", Color) = (0.65, 0.9, 1.0, 0.45)
        _Intensity ("Intensity", Range(0, 5)) = 1.2
        _EdgeSoftness ("Edge Softness", Range(0.5, 8)) = 3.5
        _PulseSpeed ("Pulse Speed", Range(0, 4)) = 0.7
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Intensity;
            float _EdgeSoftness;
            float _PulseSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float x = abs(uv.x - 0.5) * 2.0;
                float edge = pow(saturate(1.0 - x), _EdgeSoftness);

                float topFade = smoothstep(0.0, 0.18, uv.y);
                float bottomFade = smoothstep(0.0, 0.22, 1.0 - uv.y);
                float verticalFade = topFade * bottomFade;

                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed + uv.y * 8.0);
                float alpha = edge * verticalFade * _Color.a * pulse;

                fixed3 col = _Color.rgb * _Intensity * alpha;
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
