sampler2D PaletteTex;
sampler2D FadePaletteTex;

float FadePalette;

float4 SamplePalette(float x, float y)
{
    x += 0.5;
    y += 0.5;

    x /= 32.0;
    y /= 8.0;

    float2 pos = float2(x, (1 - y) / 2);

    return lerp(tex2D(FadePaletteTex, pos), tex2D(PaletteTex, pos), FadePalette);
}