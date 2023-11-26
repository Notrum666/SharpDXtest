static const int SAMPLES_COUNT = 64;

struct vert_in
{
    float4 p : SV_POSITION;
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
    
    float3 n = mul(normalTex.SampleLevel(texSampler, v.t, 0), camView).xyz;
    
    if (dot(n, n) < 0.00001)
        return (float4)1.0f;
    
    n = normalize(n);
    
    float3 wp = worldPosTex.SampleLevel(texSampler, v.t, 0).xyz;
    float3 pos = mul(float4(wp, 1.0f), camView).xyz;
    
    float3 rv = noise[texPos.x % 4 + (texPos.y % 4) * 4];
    
    float3 t = normalize(rv - n * dot(rv, n));
    float3 b = cross(t, n);
    float3x3 tbn = float3x3(t, n, b);
    
    float occlusion = 0.0f;
    float bias = 0.001;
    
    float depth = pos.y;
    
    [unroll]
    for (int i = 0; i < SAMPLES_COUNT; i++)
    {
        float3 samplePos = pos + mul(randomVectors[i], tbn) * sampleRad;

        float4 offset = float4(samplePos, 1.0f);
        
        offset = mul(offset, camProj);
        
        offset.x = (offset.x / offset.w) * 0.5f + 0.5f;
        offset.y = -(offset.y / offset.w) * 0.5f + 0.5f;
             
        int mipLevel = (1.0f - min(depth / (sampleRad * 5.0f), 1.0f)) * 2.0f;
        float sampleDepth = depthTex.Load(int3(offset.xy * texSize / pow(2, mipLevel), mipLevel)).x;
        
        if (sampleDepth > samplePos.y - bias)
            continue;
        
        float distScale = smoothstep(0.0f, 1.0f, sampleRad / abs(depth - sampleDepth));
        occlusion += distScale;
    }
    
    occlusion = 1.0f - (occlusion / (float) SAMPLES_COUNT);
    
    return float4(occlusion, occlusion, occlusion, 1.0f);
}