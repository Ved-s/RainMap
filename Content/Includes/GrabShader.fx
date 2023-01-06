#ifndef GRABREG
#define GRABREG s1
#endif

sampler2D GrabTexture : register(GRABREG);

float2 GrabScale;

float4 SampleGrab(float2 p)
{
    return tex2D(GrabTexture, p * GrabScale);
}