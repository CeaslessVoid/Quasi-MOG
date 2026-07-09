// Shared body for SpriteColorKeyOutline.shader's two passes. Kept separate so both passes
// stay identical without copy-pasting the fragment logic.

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;
float4 _Color;
float _KeyThreshold;
float _KeyFeather;
float4 _OutlineColor;
float _OutlineThickness;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
    float4 color      : COLOR;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv          : TEXCOORD0;
    float4 color       : COLOR;
};

Varyings Vert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = IN.uv;
    OUT.color = IN.color;
    return OUT;
}

// "Alpha" here means: how far this pixel's color is from pure black, remapped through the
// key threshold/feather so the cutout edge is antialiased instead of jagged.
float SampleKeyAlpha(float2 uv)
{
    float3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
    float dist = length(c);
    return smoothstep(_KeyThreshold, _KeyThreshold + _KeyFeather, dist);
}

static const float2 kOutlineOffsets[8] = {
    float2( 1, 0), float2(-1, 0), float2(0,  1), float2(0, -1),
    float2( 1, 1), float2(-1, 1), float2(1, -1), float2(-1, -1)
};

float4 Frag(Varyings IN) : SV_Target
{
    float centerAlpha = SampleKeyAlpha(IN.uv);

    if (centerAlpha < 0.5)
    {
        // Background pixel — check a ring of neighbors. If any neighbor is part of the
        // sprite, this pixel is just outside the silhouette, so draw the outline here.
        float2 texel = _MainTex_TexelSize.xy * _OutlineThickness;
        float ringAlpha = 0;
        [unroll]
        for (int i = 0; i < 8; i++)
        {
            ringAlpha = max(ringAlpha, SampleKeyAlpha(IN.uv + kOutlineOffsets[i] * texel));
        }

        if (ringAlpha > 0.5)
        {
            return float4(_OutlineColor.rgb, _OutlineColor.a * IN.color.a);
        }
        discard;
        return 0;
    }

    float3 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
    float3 finalColor = texColor * _Color.rgb;
    return float4(finalColor, _Color.a * IN.color.a);
}
