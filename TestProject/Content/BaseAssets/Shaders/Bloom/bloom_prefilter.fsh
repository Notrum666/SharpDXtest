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
    float treshold;
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

float3 prefilter(float3 color)
{
    float brightness = max(color.r, max(color.g, color.b));
    float contribution = max(0, brightness - treshold);
    contribution /= max(brightness, 0.00001);
    return color.rgb * contribution;
}

float4 main(vert_in v) : SV_Target
{
    return float4(prefilter(box(v.t, texelSize, 1.0f)), 1.0f);
}