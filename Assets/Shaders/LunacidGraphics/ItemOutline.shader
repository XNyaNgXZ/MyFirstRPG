Shader "Custom/ItemOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.9, 0.3, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }

        Pass
        {
            Name "Outline"
            Cull Front      // рендерим только внутренние грани — они торчат снаружи
            ZWrite Off      // не пишем в буфер глубины
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Смещаем вершины вдоль нормали — создаём раздутую копию
                float3 inflated = IN.positionOS.xyz + IN.normalOS * _OutlineWidth;
                OUT.positionCS  = TransformObjectToHClip(inflated);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
