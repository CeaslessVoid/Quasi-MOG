// Use this on: the Wall Tilemap's material, and door SpriteRenderers.
// Not for InstancedPropRenderer props — use InstancedPropColorKeyOutline for those instead.
//
// Why color-key instead of alpha: the source art (Wall_Atlas_Smooth.png) is an 8-bit
// grayscale PNG with no alpha channel, painted on pure black. We treat "distance from black"
// as the alpha instead of reading a real alpha channel.
//
// Why a tint multiply instead of just displaying the art as-is: the grayscale values encode
// shading/AO only. Multiplying by a material color (e.g. steel gray 105,105,105) is how the
// same atlas becomes different materials later (brick, wood, etc.) without new art.
Shader "BattleAngel/SpriteColorKeyOutline"
{
    Properties
    {
        _MainTex ("Texture (grayscale, black background)", 2D) = "white" {}
        _Color ("Material Tint", Color) = (0.412, 0.412, 0.412, 1)
        _KeyThreshold ("Key Threshold", Range(0, 1)) = 0.06
        _KeyFeather ("Key Feather (antialiasing)", Range(0.001, 0.3)) = 0.05
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness (texels)", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        // Two passes so this works whether the project's URP is set up with the 2D Renderer
        // (Universal2D) or a standard 3D Renderer used in orthographic top-down (UniversalForward).
        // Only the pass matching the active renderer actually draws — the other is skipped.
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ColorKeyOutlineBody.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ColorKeyOutlineBody.hlsl"
            ENDHLSL
        }
    }
}
