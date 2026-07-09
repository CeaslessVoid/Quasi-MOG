// Shared body for InstancedPropColorKeyOutline.shader's two passes.

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;
float4 _Color;
float _KeyThreshold;
float _KeyFeather;
float4 _OutlineColor;
float _OutlineThickness;
float _SpriteIndex;
float _AtlasCols;
float _AtlasRows;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 atlasUV      : TEXCOORD0;
    float4 cellBounds   : TEXCOORD1; // xy = cell min, zw = cell max, for clamping outline samples
};

// Remaps a 0..1 UV within a single quad into the correct cell of the atlas grid.
// Also outputs the cell's min/max UV bounds via cellOrigin/cellSize so the caller can
// clamp outline ring-samples and avoid bleeding into the neighboring atlas cell.
float2 RemapToAtlasCell(float2 uv, float spriteIndex, float cols, float rows, out float4 cellBounds)
{
    float col = fmod(spriteIndex, cols);
    float row = floor(spriteIndex / cols);
    float2 cellSize = float2(1.0 / cols, 1.0 / rows);
    float2 cellOrigin = float2(col, (rows - 1.0) - row) * cellSize;
    cellBounds = float4(cellOrigin, cellOrigin + cellSize);
    return cellOrigin + uv * cellSize;
}

Varyings Vert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.atlasUV = RemapToAtlasCell(IN.uv, _SpriteIndex, max(_AtlasCols, 1), max(_AtlasRows, 1), OUT.cellBounds);
    return OUT;
}

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
    float centerAlpha = SampleKeyAlpha(IN.atlasUV);

    if (centerAlpha < 0.5)
    {
        float2 texel = _MainTex_TexelSize.xy * _OutlineThickness;
        float ringAlpha = 0;
        [unroll]
        for (int i = 0; i < 8; i++)
        {
            float2 sampleUV = clamp(IN.atlasUV + kOutlineOffsets[i] * texel,
                                     IN.cellBounds.xy, IN.cellBounds.zw);
            ringAlpha = max(ringAlpha, SampleKeyAlpha(sampleUV));
        }

        if (ringAlpha > 0.5)
        {
            return float4(_OutlineColor.rgb, _OutlineColor.a);
        }
        discard;
        return 0;
    }

    float3 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.atlasUV).rgb;
    float3 finalColor = texColor * _Color.rgb;
    return float4(finalColor, _Color.a);
}
