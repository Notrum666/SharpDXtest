struct vert_in
{
    float4 _ : SV_POSITION;
    float2 t : TEX;
};

cbuffer textureBuf
{
    Texture2D inputTex;
};

cbuffer ssaoParamsBuf
{
    float2 texSize;
};


float4 main(vert_in v) : SV_Target
{
    float2 texPos = v.t * texSize;
    
    float result = 0.0;
    for (int x = -2; x < 2; ++x)
    {
        for (int y = -2; y < 2; ++y)
        {
            float2 offset = float2(float(x), float(y));
            result += inputTex.Load(float3(texPos + offset, 0.0f)).r;
        }
    }
    result = result / (4.0f * 4.0f);
    return float4(float3(result, result, result), 1.0f);
}