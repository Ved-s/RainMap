#define LEVELREG s2

#include "Includes/GrabShader.fx"
#include "Includes/LevelShader.fx"

sampler2D MainTex : register(s0);

float4x4 Projection;

struct ShaderData
{
    float4 pos : POSITION0;
    float2 uv  : TEXCOORD0;
    float2 leveluv : TEXCOORD1;
    float4 color : COLOR0;
};

float4 BlurSample(float2 uv)
{
    //return tex2D(MainTex, uv);
    
    float2 fw = 0.005;
    
    float4 c = tex2D(MainTex, uv);
    c += tex2D(MainTex, uv + float2(0, -fw.y));
    c += tex2D(MainTex, uv + float2(fw.x, 0));
    c += tex2D(MainTex, uv + float2(0, fw.y));
    c += tex2D(MainTex, uv + float2(-fw.x, 0));
    
    return (c / 5) / 2 + 0.5;
}

void MainVS(inout ShaderData data)
{
    data.pos = mul(data.pos, Projection);
}

float4 MainPS(ShaderData data) : COLOR
{
    if (data.leveluv.x < 0 || data.leveluv.y < 0 || data.leveluv.x > 1 || data.leveluv.y > 1)
        clip(-1);

    float4 texcol = SampleLevelParallax(data.leveluv);

    int red = texcol.x * 255;

    int paletteColor = floor((red % 90.0)/30.0);
    if(texcol.y >= 16.0/255.0)
        paletteColor = 3;

    float dist = ((red - 1) % 30.0)/30.0;
    if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) 
        dist = 1.0;
        
    float2 dir = normalize(data.uv.xy - float2(0.5, 0.5)); 

    float centerDist = clamp(distance(data.uv.xy, float2(0.5, 0.5)), 0, 0.5);
    float2 shadowPos = data.leveluv - (dir * pow(centerDist, 1.25) * pow(dist, 2) * 0.3);

    //float2 highLightPos = data.leveluv - (dir * lerp(0.002, 0.01, abs((6.0/30.0)-dist)) * pow(sin(centerDist*3.14*2), 0.2));
    float2 highLightPos = data.leveluv - (dir * 0.003 * pow(centerDist*2, 0.25));

    float2 oldShadowPos = data.leveluv - (dir * pow(centerDist, 1.25) * pow(dist, 1.5) * 0.3);

    texcol = SampleLevelParallax(shadowPos);
    red = texcol.x * 255 - 1;
    float shadowDist = (red % 30.0)/30.0;
    if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) 
        shadowDist = 1.0;

    texcol = SampleLevelParallax(highLightPos);
    red = texcol.x * 255 - 1;
    float highLightDist = (red % 30.0)/30.0;
    if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0) 
        highLightDist = 1.0;

    if (dist > 5.0/30.0)
    {
        float4 grabColor = SampleGrab(data.leveluv);
        if( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
            return float4(0,0,0,0);
    }

    if (shadowDist > 5.0/30.0)
    {
        float4 grabColor = SampleGrab(oldShadowPos);
        if( grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
            shadowDist = 6.0/30.0;
    }

    float shadow = dist - shadowDist - (paletteColor == 1 ? 0 : 2.0/30.0);
    shadow = pow(clamp(shadow, 0, 1), lerp(1.0-dist, 0.5, 0.5));

    float highLight = 0;
    if(highLightDist > dist + 0.05) 
        highLight = sin(centerDist*3.14*2);

    if(paletteColor == 0)
    {
        float2 sd2Pos = data.leveluv - (dir * 0.01 * centerDist);
        red = SampleLevelParallax(sd2Pos).x * 255;
        float sd2 = (red % 30.0)/30.0;
        if(sd2 < dist && sd2 > dist - 0.1) 
            shadow = lerp(shadow, 1, pow(centerDist*2.0, 2.5-4.0*centerDist));
    }

    float d = dist;

    if(dist < 0.2) 
        dist = pow(1.0-(dist * 5.0), 0.35);
    else 
        dist = clamp((dist - 0.2) * 1.3, 0, 1);

    dist = 1.0-dist;
    dist *= pow(pow((1-pow(centerDist * 2, 2)), 3.5), lerp(0.5, 3.5, d));

    dist = clamp(lerp(dist, 0, shadow)-shadow*0.3, 0, 1);
    if(paletteColor == 0) 
        dist *= 0.8;
    else if (paletteColor == 2) 
        dist = pow(dist, 0.8);
    else if(paletteColor == 3)
    {
        dist *= 0.2;
        highLight = 0;
    }

    float2 fw = fwidth(data.uv);
    
    dist *= BlurSample(data.uv).x;

    float alpha = clamp(dist * data.color.w * (0.65 + highLight * 0.35), 0, 1);
    return float4(data.color.xyz, 1) * alpha;
}

technique Main
{
	pass Main
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}