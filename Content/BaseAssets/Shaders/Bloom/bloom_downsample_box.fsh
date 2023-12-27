struct vert_in
{
    float4 _ : SV_POSITION;
    float2 t : TEX;
};

cbuffer textureBuf
{
    Texture2D tex;
};

cbuffer textureParams
{
    float2 texelSize;
};

SamplerState texSampler;

float3 box(float2 uv, float2 texelSize, float delta)
{
    float3 color;
    
    color = tex.Sample(texSampler, uv + float2(texelSize.x, texelSize.y)).rgb;
    color += tex.Sample(texSampler, uv + float2(-texelSize.x, texelSize.y)).rgb;
    color += tex.Sample(texSampler, uv + float2(texelSize.x, -texelSize.y)).rgb;
    color += tex.Sample(texSampler, uv + float2(-texelSize.x, -texelSize.y)).rgb;
    
    return color * 0.25f;
}

float4 main(vert_in v) : SV_Target
{
    return float4(box(v.t, texelSize, 1.0f), 1.0f);
}