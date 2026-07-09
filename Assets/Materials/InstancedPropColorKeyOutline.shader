// Use this on the material assigned to InstancedPropRenderer. Same color-key/tint/outline
// technique as SpriteColorKeyOutline, plus atlas sub-rect selection: _SpriteIndex picks a
// cell out of an _AtlasCols x _AtlasRows grid (row-major, top row = row 0), matching how
// InstancedPropRenderer groups instances by sprite index and sets _SpriteIndex once per
// batch via MaterialPropertyBlock (all instances in a batch share the same sprite, so this
// does NOT need to be a true per-instance instanced property).
Shader "BattleAngel/InstancedPropColorKeyOutline"
{
    Properties
    {
        _MainTex ("Atlas Texture (grayscale, black background)", 2D) = "white" {}
        _Color ("Material Tint", Color) = (0.412, 0.412, 0.412, 1)
        _KeyThreshold ("Key Threshold", Range(0, 1)) = 0.06
        _KeyFeather ("Key Feather (antialiasing)", Range(0.001, 0.3)) = 0.05
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness (texels)", Range(0, 4)) = 1
        _SpriteIndex ("Sprite Index (row-major)", Float) = 0
        _AtlasCols ("Atlas Columns", Float) = 1
        _AtlasRows ("Atlas Rows", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "InstancedColorKeyOutlineBody.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "InstancedColorKeyOutlineBody.hlsl"
            ENDHLSL
        }
    }
}
