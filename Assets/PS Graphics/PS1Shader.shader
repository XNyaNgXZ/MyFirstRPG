Shader "Custom/PS1"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SnapStrength ("Vertex Snap", Range(1, 200)) = 50
        _Color ("Color Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _SnapStrength;
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Стандартное преобразование
                float4 clipPos = TransformObjectToHClip(IN.positionOS.xyz);

                // ✅ Snap вершин — создаёт характерное "дрожание" PS1
                float2 snappedXY = round(clipPos.xy / clipPos.w * _SnapStrength)
                                   / _SnapStrength * clipPos.w;
                clipPos.xy = snappedXY;

                OUT.positionCS = clipPos;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return col * _Color;
            }
            ENDHLSL
        }
    }
}