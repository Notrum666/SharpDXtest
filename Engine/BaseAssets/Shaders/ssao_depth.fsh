struct vert_in
{
    float4 _ : SV_POSITION;
    float2 t : TEX;
};

cbuffer textureBuf
{
    Texture2D inputTex;
};

cbuffer ssaoDepthParamsBuf
{
    float4x4 camView;
};

SamplerState texSampler;

float main(vert_in v) : SV_Target
{
    float3 pos = inputTex.Sample(texSampler, v.t).xyz;
    return mul(float4(pos, 1.0f), camView).y;
}