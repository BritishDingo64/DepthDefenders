Shader "Unlit/bubble"
{
    Properties
    {
        _MainTex ("Surface Texture", 2D) = "white" {}
        _BubbleColor ("Bubble Base Color", Color) = (0.8, 0.95, 1.0, 0.6)
        _IridescenceStrength ("Iridescence", Range(0, 1)) = 0.7
        _RimColor ("Rim Highlight Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.5
        _RimStrength ("Rim Strength", Range(0, 2)) = 1.2
        _Smoothness ("Smoothness", Range(0, 1)) = 0.95
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5
        _SpecularHighlight ("Specular Highlight", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

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
                float3 viewDir : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BubbleColor;
            float _IridescenceStrength;
            float4 _RimColor;
            float _RimPower;
            float _RimStrength;
            float _Smoothness;
            float _FresnelPower;
            float _SpecularHighlight;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos.xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float3 N = normalize(i.worldNormal);
                float3 V = normalize(i.viewDir);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);

                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                float rimLight = pow(1.0 - saturate(dot(N, V)), _RimPower);

                float3 iridescence = float3(
                    sin(N.x * 3.14159 + N.y * 2.0) * 0.5 + 0.5,
                    sin(N.y * 3.14159 + N.z * 2.0 + 2.0944) * 0.5 + 0.5,
                    sin(N.z * 3.14159 + N.x * 2.0 + 4.1888) * 0.5 + 0.5
                );

                float3 baseColor = _BubbleColor.rgb * (1.0 - _IridescenceStrength * 0.4);
                baseColor += iridescence * _IridescenceStrength * 0.5;

                float specularMask = pow(max(0.0, dot(reflect(-L, N), V)), 32.0 * _Smoothness);
                float3 specular = _RimColor.rgb * specularMask * _SpecularHighlight;

                float3 finalColor = baseColor * (0.6 + 0.4 * col.r);
                finalColor += _RimColor.rgb * rimLight * _RimStrength;
                finalColor += specular;

                float alpha = _BubbleColor.a * (0.3 + 0.7 * (1.0 - fresnel * 0.6));

                fixed4 finalCol = fixed4(finalColor, alpha);
                UNITY_APPLY_FOG(i.fogCoord, finalCol);
                return finalCol;
            }
            ENDCG
        }
    }
}
