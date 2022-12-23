#include "PaletteShader.fx"

sampler2D LevelTex : register(s0);
sampler2D NoiseTex;
sampler2D GrabTex;
sampler2D EffectColors;

float4x4 Projection;

float _RAIN;
float _light = 0;
float4 _spriteRect;
float4 _lightDirAndPixelSize;
float _waterLevel;
float _Grime;
float _SwarmRoom;
float _WetTerrain;
float _cloudsSpeed;

float EffectColorA;
float EffectColorB;

struct ShaderData
{
    float4 pos : POSITION0;
    float2 uv : TEXCOORD0;
};

void MainVS(inout ShaderData data)
{
    data.pos = mul(data.pos, Projection);
}

float4 MainPS(ShaderData i) : COLOR
{
    //float rand = frac(sin(dot(i.uv.x, 12.98232)+_RAIN-i.uv.y) * 43758.5453);
    float4 setColor = float4(0.0, 0.0, 0.0, 1.0);
    bool checkMaskOut = false;

    float ugh = fmod(fmod(tex2D(LevelTex, float2(i.uv.x, i.uv.y)).x * 255, 90) - 1, 30) / 300.0;
    float displace = tex2D(NoiseTex, float2((i.uv.x * 1.5) - ugh + (_RAIN * 0.01), (i.uv.y * 0.25) - ugh + _RAIN * 0.05)).x;
    displace = clamp((sin((displace + i.uv.x + i.uv.y + _RAIN * 0.1) * 3 * 3.14) - 0.95) * 20, 0, 1);

    float2 screenPos = float2(lerp(_spriteRect.x, _spriteRect.z, i.uv.x), lerp(_spriteRect.y, _spriteRect.w, i.uv.y));

    if (_WetTerrain < 0.5 || 1 - screenPos.y > _waterLevel) 
        displace = 0;

    float4 texcol = tex2D(LevelTex, float2(i.uv.x, i.uv.y + displace * 0.001));

    //if(texcol.y * 255 > 7 && texcol.y * 255 < 11) return float4(0,0,1,1);
    if (texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0)
    {
        setColor = SamplePalette(0, 7);
        //setColor = float4(0,0,0,0);
        checkMaskOut = true;
    }
    else
    {
        int red = texcol.x * 255;
        int green = texcol.y * 255;

        float effectCol = 0;
        float notFloorDark = 1;
        if (green >= 16)
        {
            notFloorDark = 0;
            green -= 16;
        }
        if (green >= 8)
        {
            effectCol = 100;
           //green = 8;
            green -= 8;
           //return float4(0,0,0,0);
        }
        else
            effectCol = green;

        half shadow = tex2D(NoiseTex, float2((i.uv.x * 0.5) + (_RAIN * 0.1 * _cloudsSpeed) - (0.003 * fmod(red, 30.0)), 1 - (i.uv.y * 0.5) + (_RAIN * 0.2 * _cloudsSpeed) - (0.003 * fmod(red, 30.0)))).x;

        shadow = 0.5 + sin(fmod(shadow + (_RAIN * 0.1 * _cloudsSpeed) - i.uv.y, 1) * 3.14 * 2) * 0.5;
        shadow = clamp(((shadow - 0.5) * 6) + 0.5 - (_light * 4), 0, 1);

        if (red > 90)
            red -= 90;
        else
            shadow = 1.0;

        float paletteColor = clamp(floor(red / 30), 0, 2); //some distant objects want to get palette color 3, so we clamp it

        red = red % 30;

        if (shadow != 1 && red >= 5)
        {
            float2 grabPos = float2(screenPos.x + -_lightDirAndPixelSize.x * _lightDirAndPixelSize.z * (red - 5), 1 - screenPos.y + -_lightDirAndPixelSize.y * _lightDirAndPixelSize.w * (red - 5));
            grabPos = ((grabPos - float2(0.5, 0.3)) * (1 + (red - 5.0) / 460.0)) + float2(0.5, 0.3);
            float4 grabTexCol2 = tex2D(
            GrabTex, grabPos);
            if (grabTexCol2.x != 0.0 || grabTexCol2.y != 0.0 || grabTexCol2.z != 0.0)
            {
                shadow = 1;
            }
        }

        setColor = lerp(SamplePalette(red * notFloorDark, paletteColor + 3), SamplePalette(red * notFloorDark, paletteColor), shadow);
        half rbcol = (sin((_RAIN + (tex2D(NoiseTex, float2(i.uv.x * 2, i.uv.y * 2)).x * 4) + red / 12.0) * 3.14 * 2) * 0.5) + 0.5;
        setColor = lerp(setColor, SamplePalette((5.5 + rbcol * 25), 6.5), (green >= 4 ? 0.2 : 0.0) * _Grime);

        if (effectCol == 100)
        {
            float4 decalCol = tex2D(LevelTex
        , float2((255.5 - texcol.z * 255.0) / 1400.0, 1 - 799.5 / 800.0));
            if (paletteColor == 2) 
                decalCol = lerp(decalCol, float4(1, 1, 1, 1), 0.2 - shadow * 0.1);

            decalCol = lerp(decalCol, SamplePalette(1, 7), red / 60.0);
            setColor = lerp(lerp(setColor, decalCol, 0.7), setColor * decalCol * 1.5, lerp(0.9, 0.3 + 0.4 * shadow, clamp((red - 3.5) * 0.3, 0, 1)));
        }
        else if (green > 0 && green < 3)
        {
            effectCol = paletteColor == 0 ? EffectColorA : EffectColorB;

            if (effectCol >= 0)
            {
                float4 tl = tex2D(EffectColors, float2((effectCol * 2 + 0.5) / 44, 0.125));
                float4 tr = tex2D(EffectColors, float2((effectCol * 2 + 1.5) / 44, 0.125));

                setColor = lerp(setColor, lerp(tl, tr, shadow), texcol.z);
            }
            else 
            {
                setColor = float4(1, 0, 0, 1);
            }
        }
        else if (green == 3)
        {
            setColor = lerp(setColor, float4(1, 1, 1, 1), texcol.z * _SwarmRoom);
        }

        float4 fogAmountPixel = SamplePalette(9, 7);
        float fogAmount = 1 - fogAmountPixel.r;
        if (fogAmountPixel.r == 0 && fogAmountPixel.g == 0 && fogAmountPixel.b > 0)
	    {
	    	fogAmount = 1 + fogAmountPixel.b;
	    }

        setColor = lerp(setColor, SamplePalette(1, 7), clamp(red * (red < 10 ? lerp(notFloorDark, 1, 0.5) : 1) * fogAmount / 30.0, 0, 1));

        if (red >= 5)
        {
            checkMaskOut = true;
        }
    }
    
    if (checkMaskOut)
    {
        float4 grabTexCol = tex2D(
        GrabTex, float2(screenPos.x, 1 - screenPos.y));
        if (grabTexCol.x > 1.0 / 255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0)
        {
            setColor.w = 0;
        }
    }
    return setColor;
}

technique Main
{
    pass Main
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
}