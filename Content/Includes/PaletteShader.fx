sampler2D PaletteTex;
sampler2D FadePaletteTex;

// RG - fade room center, B - fade value, A - unused
sampler2D FadePosValTex;

int FadeSize;
float4 FadeRect;

float remap(float iMin, float iMax, float oMin, float oMax, float v)
{
    float t = (v - iMin) / (iMax - iMin);
    return lerp(oMin, oMax, t);
}

float2 getFadePos(int index)
{
    return tex2Dlod(FadePosValTex, float4((index + .5) / FadeSize, .5, 0, 0)).xy;
}

float getFadeVal(int index)
{
    return tex2Dlod(FadePosValTex, float4((index + .5) / FadeSize, .5, 0, 0)).z;
}

float fadeGradient(float2 uv)
{
    if (FadeSize == 1)
    {
        return getFadeVal(0);
    }
    
    bool nodesFound[4] = { 0, 0, 0, 0 };
    float2 nodePos[4] =
    {
        { 0, 0 },
        { 0, 0 },
        { 0, 0 },
        { 0, 0 },
    };
    int nodeIndexes[4] = { -1, -1, -1, -1 };

    int2 nodeDirs[4] =
    {
        { -1, -1 },
        { 1, -1 },
        { -1, 1 },
        { 1, 1 },
    };

    [loop]
    for (int i = 0; i < FadeSize; i++)
    {
        float2 fadePos = getFadePos(i);
        float2 dir = fadePos - uv;

        [unroll(4)]
        for (int j = 0; j < 4; j++)
        {
            bool2 cond = dir == 0 || sign(dir) == nodeDirs[j];
            if (cond.x && cond.y && (!nodesFound[j] || length(uv - fadePos) < length(uv - nodePos[j])))
            {
                nodesFound[j] = true;
                nodePos[j] = fadePos;
                nodeIndexes[j] = i;
            }
        }
    }

    float top = -1;
    float topPos = -1;
    bool hasTop = true;

    if (nodesFound[0] && nodesFound[1])
    {
        top = remap(nodePos[0].x, nodePos[1].x, getFadeVal(nodeIndexes[0]), getFadeVal(nodeIndexes[1]), uv.x);
        topPos = remap(nodePos[0].x, nodePos[1].x, nodePos[0].y, nodePos[1].y, uv.x);
    }
    else if (nodesFound[0])
    {
        top = getFadeVal(nodeIndexes[0]);
        topPos = nodePos[0].y;
    }
    else if (nodesFound[1])
    {
        top = getFadeVal(nodeIndexes[1]);
        topPos = nodePos[1].y;
    }
    else
        hasTop = false;

    float bottom = -1;
    float bottomPos = -1;
    bool hasBottom = true;

    if (nodesFound[2] && nodesFound[3])
    {
        bottom = remap(nodePos[2].x, nodePos[3].x, getFadeVal(nodeIndexes[2]), getFadeVal(nodeIndexes[3]), uv.x);
        bottomPos = remap(nodePos[2].x, nodePos[3].x, nodePos[2].y, nodePos[3].y, uv.x);
    }
    else if (nodesFound[2])
    {
        bottom = getFadeVal(nodeIndexes[2]);
        bottomPos = nodePos[2].y;
    }
    else if (nodesFound[3])
    {
        bottom = getFadeVal(nodeIndexes[3]);
        bottomPos = nodePos[3].y;
    }
    else
        hasBottom = false;

    if (hasTop && hasBottom)
    {
        return remap(topPos, bottomPos, top, bottom, uv.y);
    }
    else if (hasTop)
    {
        return top;
    }
    else if (hasBottom)
    {
        return bottom;
    }
    return 0;
}

float CacheFade(float2 uv)
{
    float2 screenPos = float2(lerp(FadeRect.x, FadeRect.y, uv.x), lerp(FadeRect.z, FadeRect.w, uv.y));
    return fadeGradient(screenPos);
}

float4 SamplePalette(float x, float y, float fade)
{
    x += 0.5;
    y += 0.5;

    x /= 32.0;
    y /= 8.0;

    float4 pos = float4(x, (1 - y) / 2, 0, 0);
    return lerp(tex2Dlod(PaletteTex, pos), tex2Dlod(FadePaletteTex, pos), fade);
}