Shader "Custom/StoneWall"
{
    Properties
    {
        _StoneColorA ("Stone Color A", Color) = (0.46, 0.44, 0.41, 1)
        _StoneColorB ("Stone Color B", Color) = (0.31, 0.30, 0.28, 1)
        _MortarColor ("Mortar Color", Color) = (0.17, 0.17, 0.16, 1)
        _Tiling ("Tiling", Float) = 4.5
        _BrickAspect ("Brick Width", Range(1.0, 4.0)) = 2.0
        _MortarWidth ("Mortar Width", Range(0.01, 0.25)) = 0.09
        _EdgeSoftness ("Mortar Softness", Range(0.001, 0.12)) = 0.03
        _HeightStrength ("Height Strength", Range(0, 0.7)) = 0.25
        _ColorVariation ("Stone Color Variation", Range(0, 1)) = 0.35
        _StoneSmoothness ("Stone Smoothness", Range(0, 1)) = 0.18
        _MortarSmoothness ("Mortar Smoothness", Range(0, 1)) = 0.04
        _StoneMetallic ("Stone Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _StoneColorA;
        fixed4 _StoneColorB;
        fixed4 _MortarColor;
        float _Tiling;
        float _BrickAspect;
        float _MortarWidth;
        float _EdgeSoftness;
        float _HeightStrength;
        float _ColorVariation;
        float _StoneSmoothness;
        float _MortarSmoothness;
        float _StoneMetallic;

        struct Input
        {
            float3 worldPos;
        };

        float hash11(float n)
        {
            return frac(sin(n) * 43758.5453123);
        }

        // x: stone mask, y: per-brick id, z: distance from nearest brick edge
        float3 brickData(float2 uv)
        {
            // Wider-than-tall bricks
            float2 grid = float2(uv.x / _BrickAspect, uv.y);

            // Stagger every other row by half a brick
            float row = floor(grid.y);
            float rowOffset = frac(row * 0.5) * 2.0;
            float2 shifted = float2(grid.x + rowOffset * 0.5, grid.y);

            float2 cell = floor(shifted);
            float2 f = frac(shifted);

            // Distance to nearest rectangular edge (not circular)
            float edgeDist = min(min(f.x, 1.0 - f.x), min(f.y, 1.0 - f.y));
            float stoneMask = smoothstep(_MortarWidth, _MortarWidth + _EdgeSoftness, edgeDist);
            float brickId = hash11(dot(cell, float2(37.2, 113.5)));

            return float3(stoneMask, brickId, edgeDist);
        }

        float stoneHeight(float2 uv)
        {
            float3 b = brickData(uv);
            float stoneMask = b.x;
            float edgeDist = b.z;

            // Slightly raised middle of each brick
            float centerBulge = saturate((edgeDist - _MortarWidth) / max(0.0001, 0.5 - _MortarWidth));
            centerBulge *= centerBulge;

            float chipNoise = hash11(floor(uv.x * 10.0) + floor(uv.y * 10.0) * 71.0) * 0.16;
            float h = stoneMask * (0.45 + centerBulge * 0.45 - chipNoise);

            return h;
        }

        float3 stoneAlbedo(float2 uv)
        {
            float3 b = brickData(uv);
            float stoneMask = b.x;

            float3 stoneBase = lerp(_StoneColorA.rgb, _StoneColorB.rgb, b.y);

            // Extra subtle grain variation
            float grain = hash11(floor(uv.x * 6.0) + floor(uv.y * 6.0) * 131.0);
            stoneBase *= lerp(1.0 - _ColorVariation, 1.0 + _ColorVariation, grain);

            return lerp(_MortarColor.rgb, stoneBase, stoneMask);
        }

        float2 getProjectedUV(float3 worldPos, float3 worldNormal)
        {
            float3 n = abs(normalize(worldNormal));

            // Use the dominant axis so vertical walls don't get XZ stretching.
            if (n.y >= n.x && n.y >= n.z)
            {
                return worldPos.xz * _Tiling; // top/bottom
            }
            else if (n.x >= n.z)
            {
                return worldPos.zy * _Tiling; // side facing +/-X
            }

            return worldPos.xy * _Tiling; // side facing +/-Z
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 geomNormal = normalize(cross(ddy(IN.worldPos), ddx(IN.worldPos)));
            float2 uv = getProjectedUV(IN.worldPos, geomNormal);

            float h = stoneHeight(uv);
            float e = 0.02;
            float hx = stoneHeight(uv + float2(e, 0.0));
            float hy = stoneHeight(uv + float2(0.0, e));

            float dhx = (hx - h) / e;
            float dhy = (hy - h) / e;

            float stoneMask = brickData(uv).x;

            o.Albedo = stoneAlbedo(uv);
            o.Metallic = _StoneMetallic;
            o.Smoothness = lerp(_MortarSmoothness, _StoneSmoothness, stoneMask);
            o.Occlusion = lerp(0.85, 1.0, stoneMask);

            // Tangent-space style perturbation
            o.Normal = normalize(float3(-dhx * _HeightStrength, -dhy * _HeightStrength, 1.0));
        }
        ENDCG
    }

    FallBack "Standard"
}