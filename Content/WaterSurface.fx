#include "PaletteShader.fx"

sampler2D LevelTex : register(s0);

float4x4 Projection;
float _waterDepth;

struct ShaderData
{
    float4 pos : POSITION;
    float2 uv : TEXCOORD0;
    float depth : TEXCOORD1;
};

void MainVS(inout ShaderData data)
{
    data.pos = mul(data.pos, Projection);
}

float4 MainPS(ShaderData data) : COLOR
{
    if (data.uv.x < 0 || data.uv.y < 0 || data.uv.x > 1 || data.uv.y > 1)
        clip(-1);
    
    float4 texcol = tex2D(LevelTex, data.uv);
    
    float red = fmod((texcol.x * 255), 30.0);

    if (texcol.x != 1.0 || texcol.y != 1.0 || texcol.z != 1.0)
        if (red < _waterDepth * 31 || red / 30.0 < data.depth) // + lerp(0.02, -0.075, 1.0 - _waterDepth * 31.0))
            return 0;

    //if (i.uv.y + lerp(0.02, -0.075, 1.0 - _waterDepth * 31.0) > 6.0 / 30.0)
    //{
    //    half4 grabColor = tex2D(_GrabTexture, half2(i.scrPos.x, 1.0 - i.scrPos.y));
    //    if (grabColor.x > 1.0 / 255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
    //        return float4(0, 0, 0, 0);
    //}

    float4 watercolor = lerp(SamplePalette(4, 7), SamplePalette(7, 7), data.depth);

    return watercolor;
}

technique Main
{
    pass Main
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
}