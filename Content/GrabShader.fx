sampler2D GrabTextureSampler;

float4 SampleGrab(float2 p)
{
    return tex2D(GrabTextureSampler, p);
}