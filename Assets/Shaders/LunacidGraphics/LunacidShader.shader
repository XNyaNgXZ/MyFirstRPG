Shader "Custom/Lunacid"
{
    Properties
    {
        _MainTex        ("Texture",          2D)            = "white" {}
        _Color          ("Color Tint",       Color)         = (1,1,1,1)

        [Toggle] _WorldUV ("World Space UV", Float) = 0
        _WorldUVScale ("World UV Scale", Float) = 0.5

        [Header(Vertex Snap PS1)]
        _SnapStrength   ("Vertex Snap",      Range(1, 300)) = 80

        [Header(Affine Texture Warping)]
        _AffineBlend    ("Affine Strength",  Range(0, 1))   = 0.85

        [Header(Vertex Lighting)]
        _AmbientColor   ("Ambient Color",    Color)         = (0.03, 0.02, 0.06, 1)
        _LightIntensity ("Light Intensity",  Range(0, 2))   = 0.9

        [Header(Color Depth)]
        _ColorDepth     ("Color Depth",      Range(4, 256)) = 24

        [Header(Dithering)]
        _DitherStrength ("Dither Strength",  Range(0, 1))   = 0.35
        _DitherScale    ("Dither Scale",     Range(1, 8))   = 1

        [Header(Distance Fog)]
        _FogColor       ("Fog Color",        Color)         = (0.01, 0.01, 0.04, 1)
        _FogStart       ("Fog Start",        Float)         = 4
        _FogEnd         ("Fog End",          Float)         = 22
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        LOD 100

        // ── Основной проход ───────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float3 worldPos : TEXCOORD5;
                float3 normalWS : TEXCOORD6;
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;   // аффинные UV (умножены на W)
                float  clipW       : TEXCOORD1;   // W для восстановления UV
                float3 vertexLight : TEXCOORD2;   // Per-vertex освещение
                float  fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _WorldUV;
                float _WorldUVScale;
                float4 _MainTex_ST;
                float4 _Color;
                float  _SnapStrength;
                float  _AffineBlend;
                float4 _AmbientColor;
                float  _LightIntensity;
                float  _ColorDepth;
                float  _DitherStrength;
                float  _DitherScale;
                float4 _FogColor;
                float  _FogStart;
                float  _FogEnd;
            CBUFFER_END

            // ── Матрица Байера 4×4 ────────────────────────────────────────
            static const float BayerMatrix[16] =
            {
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            };

            float GetDither(float2 screenPos)
            {
                int x = (int)fmod(screenPos.x / _DitherScale, 4.0);
                int y = (int)fmod(screenPos.y / _DitherScale, 4.0);
                return BayerMatrix[y * 4 + x];
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // ── Vertex Snap (вершинный джиттер PS1) ──────────────────
                float4 clipPos = TransformObjectToHClip(IN.positionOS.xyz);
                float2 snapped = round(clipPos.xy / clipPos.w * _SnapStrength)
                                 / _SnapStrength * clipPos.w;
                clipPos.xy = snapped;
                OUT.positionCS = clipPos;
                OUT.clipW      = clipPos.w;

                // ── Affine Texture Mapping ────────────────────────────────
                // PS1 не делал perspective-correct UV → текстуры «плывут»
                float3 wPos    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 wNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldPos   = wPos;
                OUT.normalWS   = wNormal;

                // Стандартные UV оставляем как запасные
                float2 uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uv    = lerp(uv, uv * clipPos.w, _AffineBlend);

                // ── Per-vertex освещение (не per-pixel — как PS1) ─────────
                float3 normalWS  = TransformObjectToWorldNormal(IN.normalOS);
                float3 worldPos  = TransformObjectToWorld(IN.positionOS.xyz);

                // Основной источник
                Light mainLight  = GetMainLight();
                float NdotL      = saturate(dot(normalWS, mainLight.direction));
                float3 lighting  = _AmbientColor.rgb
                                 + mainLight.color * NdotL * _LightIntensity;

                // Дополнительные точечные источники (факелы, свечи)
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                int lightCount = GetAdditionalLightsCount();
                for (int i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, worldPos);
                    float addNdotL = saturate(dot(normalWS, addLight.direction));
                    lighting += addLight.color * addLight.distanceAttenuation
                              * addNdotL * _LightIntensity;
                }
            #endif

                OUT.vertexLight = lighting;

                // ── Туман ─────────────────────────────────────────────────
                float dist       = distance(_WorldSpaceCameraPos, worldPos);
                OUT.fogFactor    = saturate((dist - _FogStart) / (_FogEnd - _FogStart));

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ── Восстанавливаем perspective-correct UV ────────────────
float2 uv = lerp(IN.uv, IN.uv / IN.clipW, _AffineBlend);

                // ✅ World Space UV — текстуры стыкуются между кусками стен
                if (_WorldUV > 0.5)
                {
                    float3 absN = abs(IN.normalWS);
                    float2 wuv;
                    if (absN.y > absN.x && absN.y > absN.z)
                        wuv = IN.worldPos.xz;          // пол / потолок
                    else if (absN.x > absN.z)
                        wuv = IN.worldPos.zy;          // левая / правая стена
                else
                        wuv = IN.worldPos.xy;          // передняя / задняя стена
                    uv = wuv * _WorldUVScale;
                }

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                col *= _Color;

                // ── Per-vertex освещение ───────────────────────────────────
                col.rgb *= IN.vertexLight;

                // ── Дизеринг (Bayer 4×4) ─────────────────────────────────
                if (_DitherStrength > 0.001)
                {
                    float dither = GetDither(IN.positionCS.xy);
                    float bias   = (dither - 0.5) * _DitherStrength * 0.15;
                    col.rgb      = saturate(col.rgb + bias);
                }

                // ── Снижение глубины цвета (ограниченная палитра) ─────────
                col.rgb = round(col.rgb * _ColorDepth) / _ColorDepth;

                // ── Туман ─────────────────────────────────────────────────
                col.rgb = lerp(col.rgb, _FogColor.rgb, IN.fogFactor);

                return col;
            }
            ENDHLSL
        }

        // ── Проход теней ──────────────────────────────────────────────────
        Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }
    ZWrite On ZTest LEqual ColorMask 0

    HLSLPROGRAM
    #pragma vertex   shadowVert
    #pragma fragment shadowFrag
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    float3 _LightDirection;

    struct ShadowAttr { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
    struct ShadowVary { float4 positionCS : SV_POSITION; };

    ShadowVary shadowVert(ShadowAttr IN)
    {
        ShadowVary OUT;
        float3 worldPos    = TransformObjectToWorld(IN.positionOS.xyz);
        float3 worldNormal = TransformObjectToWorldNormal(IN.normalOS);

        // ✅ Без ApplyShadowBias — простой bias вручную
        worldPos += worldNormal * 0.01;
        OUT.positionCS = TransformWorldToHClip(worldPos);
        return OUT;
    }

    half4 shadowFrag(ShadowVary IN) : SV_Target { return 0; }
    ENDHLSL
}
    }

    FallBack "Universal Render Pipeline/Lit"
}
