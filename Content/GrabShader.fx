sampler2D GrabTexture : register(S1);

float2 GrabScale;

float4 SampleGrab(float2 p)
{
    return tex2D(GrabTexture, p * GrabScale);
}