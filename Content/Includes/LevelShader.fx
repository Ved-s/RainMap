
#ifndef LEVELREG
#define LEVELREG s0
#endif

sampler2D LevelTex : register(LEVELREG);

float2 ParallaxCenter;
float ParallaxDistance;

float4 SampleLevel(float2 pos)
{
    return tex2Dlod(LevelTex, float4(pos, 0, 0));
}

float4 SampleLevelParallax(float2 pos)
{
    float4 texcol = SampleLevel(pos);
    float2 parallaxDir = pos - ParallaxCenter;

    if (ParallaxDistance < 0)
        parallaxDir = ParallaxCenter - pos;

    if (ParallaxDistance != 0)
    {
        int steps = clamp(length(parallaxDir), 0, 1) * 50 + 30;
        float sc = (float) steps / 30;
        parallaxDir /= sc;

        parallaxDir *= abs(ParallaxDistance);

        for (int t = 0; t < steps; t++)
        {
            int layer = (((float) t / steps) * 30.0);

            bool effect = texcol.g > 0 && layer > 0;

            int red = (texcol.r * 255) % 30;
            if (effect ? layer == red : layer > red)
            {
                if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0)
                    break;

                int upperRed = (texcol.r * 255) / 30;
                int fullRed = upperRed * 30 + layer;
                texcol.r = fullRed / 255.0;
                break;
            }

            pos += parallaxDir;
            texcol = SampleLevel(pos);
        }
    }
    
    return texcol;
}