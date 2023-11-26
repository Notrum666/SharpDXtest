struct vert_in
{
    float4 _ : SV_POSITION;
    float2 t : TEX;
};

cbuffer textureBuf
{
    Texture2D inputTex;
    Texture2D colorTex;
};

cbuffer ssaoParamsBuf
{
    float2 texelSize;
};

SamplerState texSampler;

float4 main(vert_in v) : SV_Target
{
    float result = 0.0;
    for (int x = -2; x < 2; ++x)
    {
        for (int y = -2; y < 2; ++y)
        {
            float2 offset = float2(float(x) * texelSize.x, float(y) * texelSize.y);
            result += inputTex.Sample(texSampler, v.t + offset).r;
        }
    }
    result = result / (4.0f * 4.0f);
    
    
    return float4(float3(result, result, result), 1.0f);
}