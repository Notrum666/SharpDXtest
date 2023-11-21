static const int SAMPLES_COUNT = 64;

struct vert_in
{
    float4 _ : SV_POSITION;//pixel coord
    float2 t : TEX;
};

cbuffer textureBuf
{
    Texture2D worldPosTex;
    Texture2D normalTex;
    Texture2D depthTex;
};

cbuffer ssaoParamsBuf
{
    float2 texSize;
    float4x4 camView;
    float4x4 camProj;
    float4x4 camInvProj;
    float3 randomVectors[SAMPLES_COUNT];
    float3 noise[16];
    float sampleRad;
};

SamplerState texSampler;

float4 main(vert_in v) : SV_Target
{
    int2 texPos = v.t * texSize;
    
    float3 n = mul(normalTex.Sample(texSampler, v.t), camView).xyz;
    
    if (dot(n, n) < 0.00001)
        return (float4)1.0f;
    
    float3 wp = worldPosTex.Sample(texSampler, v.t).xyz;
    float3 pos = mul(float4(wp, 1.0f), camView).xyz;
    
    float3 rv = noise[texPos.x % 4 + (texPos.y % 4) * 4];
    
    float3 t = normalize(rv - n * dot(rv, n));
    float3 b = cross(t, n);
    float3x3 tbn = float3x3(t, n, b);
    
    float occlusion = 0.0f;
    float bias = 0.001;
    
    float depth = pos.y; //linear_depth(v.t, texSize).y;
    
    [unroll]
    for (int i = 0; i < SAMPLES_COUNT; i++)
    {
        float3 samplePos = pos + mul(randomVectors[i], tbn) * sampleRad;

        float4 offset = float4(samplePos, 1.0f);
        
        offset = mul(offset, camProj);
        
        offset.x = (offset.x / offset.w) * 0.5f + 0.5f;
        offset.y = -(offset.y / offset.w) * 0.5f + 0.5f;
        
        float sampleDepth = depthTex.Sample(texSampler, offset.xy).x; //linear_depth(offset.xy, texSize).y;
        
        float distScale = smoothstep(0.0f, 1.0f, sampleRad / abs(depth - sampleDepth));
        occlusion += (sampleDepth <= samplePos.y - bias ? distScale : 0.0f);
    }
    
    occlusion = 1.0f - (occlusion / (float) SAMPLES_COUNT);
    
    return float4(occlusion, occlusion, occlusion, 1.0f);
}