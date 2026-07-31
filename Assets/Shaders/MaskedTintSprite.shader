Shader "Custom/MaskedTintSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "black" {}
        _Color ("Primary Tint", Color) = (1,1,1,1)
        _SecondaryColor ("Secondary Tint", Color) = (1,1,1,1)
        _HasMask ("Has Mask", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _SecondaryColor;
            float _HasMask;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float3 rgb = baseCol.rgb;

                if (_HasMask > 0.5)
                {
                    float4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                    rgb = lerp(rgb, rgb * _Color.rgb, mask.r);
                    rgb = lerp(rgb, rgb * _SecondaryColor.rgb, mask.g);
                }
                else
                {
                    rgb *= _Color.rgb;
                }

                rgb *= i.color.rgb;
                float a = baseCol.a * i.color.a * _Color.a;
                return float4(rgb, a);
            }
            ENDHLSL
        }
    }
}