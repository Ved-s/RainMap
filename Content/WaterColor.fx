#include "PaletteShader.fx"

sampler2D LevelTex : register(s0);
sampler2D NoiseTex;
sampler2D GrabTexture;

float _RAIN;
float2 _screenOff;
float2 _screenPos;
float2 _screenSize;
float _waterDepth;

float4x4 Projection;
float4 clr;

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

float4 MainPS(ShaderData data, float2 scrPos : VPOS) : COLOR
{
	if (data.uv.x < 0 || data.uv.y < 0 || data.uv.x > 1 || data.uv.y > 1)
		clip(-1);

	float rbcol = (sin((_RAIN + (tex2D(NoiseTex, _screenOff + float2(data.uv.x*1.2, data.uv.y*1.2) ).x * 3) + 0/12.0) * 3.14 * 2)*0.5)+0.5;
	
	float2 distortion = float2(lerp(-0.002, 0.002, rbcol)*lerp(1, 20, pow(data.depth, 200)), -0.02 * pow(data.depth, 8));
	distortion.x = floor(distortion.x*_screenSize.x)/_screenSize.x;
	distortion.y = floor(distortion.y*_screenSize.y)/_screenSize.y;
	
    float4 texcol = tex2D(LevelTex, data.uv + distortion);

	float plrLightDst = clamp(distance(float2(0,0),  float2((scrPos.x - clr.x)*(_screenSize.x/_screenSize.y), scrPos.y - clr.y))*lerp(21, 8, clr.z), 0, 1);


	float grad = fmod((texcol.x * 255), 30.0)/30.0;
	
	plrLightDst = clamp(plrLightDst + pow(1.0-grad, 3), 0, 1);
	
	if(texcol.x == 1.0 && texcol.y == 1.0 && texcol.z == 1.0)
		grad = 1;
 
	float4 grabColor = tex2D(GrabTexture, float2(scrPos.x+distortion.x, 1.0-scrPos.y-distortion.y));

	if (grabColor.x == 0.0 && grabColor.y == 2.0/255.0 && grabColor.z == 0.0)
		grad = 1;
	else if (grad > 6.0/30.0)
	{
		if (grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0)
        {
            if (grabColor.x == 0.0 && grabColor.y == 1.0 / 255.0 && grabColor.z == 0.0)
            {
                grad = 1;
                grabColor = float4(0, 0, 0, 0);
            }
            else
            {
                grad = 6.0 / 30.0;
                grabColor *= lerp(SamplePalette(5, 7), float4(1, 1, 1, 1), 0.75);
                plrLightDst = 1;
            }
        }
    }
	else
		grabColor = float4(0,0,0,1);

	if(fmod((tex2D(LevelTex, data.uv).x*255), 30.0)<_waterDepth*31.0) return float4(0, 0, 0, 0);
	
    grad = pow(grad, clamp(1 - pow(data.depth, 10), 0.5, 1)) * data.depth;
	
	float whatToSine = (_RAIN*6) + (tex2D(NoiseTex, _screenOff + float2((grad/10)+lerp(data.uv.x, 0.5, grad/3)*2.1 + distortion.x,  (_RAIN*0.1)+(grad/5)+lerp(data.uv.y, 0.5, grad/3)*2.1+distortion.y) ).x * 7);
	float col = (sin(whatToSine * 3.14 * 2)*0.5)+0.5;
	
	whatToSine = (_RAIN*2.7) + (tex2D(NoiseTex, _screenOff + float2((grad/7)+lerp(data.uv.x, 0.5, grad/5)*1.3 + distortion.x,  (_RAIN*-0.21)+(grad/8)+lerp(data.uv.y, 0.5, grad/6)*1.3+distortion.y) ).x * 6.33);
	float col2 = (sin(whatToSine * 3.14 * 2)*0.5)+0.5;
 
	col = pow(max(col, col2), 47);
	
	if(col >= 0.8)
        grad = min(grad + 0.1 * (1.0 - abs(0.5 - grad) * 2.0) * data.depth, 1);
	 
	plrLightDst = pow(plrLightDst, 3);
	 
	grad = lerp(grad, pow(grad, lerp(0.2, 1, plrLightDst)), clr.z);
   
	texcol = lerp(SamplePalette(5, 7), SamplePalette(4, 7), grad);
	
	if(grabColor.x > 1.0/255.0 || grabColor.y != 0.0 || grabColor.z != 0.0) 
        return lerp(texcol, grabColor, pow(clamp((data.depth - 0.9) * 10, 0, 1), 3));
	
	else 
		return texcol;
}

technique Main
{
	pass Main
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
}