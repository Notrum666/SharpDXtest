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
    int depthMipLevels;
};

SamplerState texSampler;

float4 main(vert_in v) : SV_Target
{
    int2 texPos = v.p.xy;
    
    float4 wp = worldPosTex.SampleLevel(texSampler, v.t, 0).xyzw;
    
    if (wp.w == 0.0f)
        return (float4)1.0f;
    
    float3 n = mul(normalTex.SampleLevel(texSampler, v.t, 0), camView).xyz;
    
    n = normalize(n);
    
    float3 pos = mul(float4(wp.xyz, 1.0f), camView).xyz;
    
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
        
        if(offset.x > 1 || offset.x < 0 || offset.y > 1 || offset.y < 0)
            continue;
        
        float2 offsetCoord = offset.xy * texSize;
        float treshhold = 10;
        
        float d = distance(offsetCoord, v.p.xy);
        
        int mipLevel = min(d / treshhold, depthMipLevels);
             
        float sampleDepth = depthTex.Load(int3(offset.xy * texSize / pow(2, mipLevel), mipLevel)).x;
        
        if (sampleDepth > samplePos.y - bias)
            continue;
        
        float distScale = smoothstep(0.0f, 1.0f, sampleRad / abs(depth - sampleDepth));
        occlusion += distScale;
    }
    
    occlusion = 1.0f - (occlusion / (float) SAMPLES_COUNT);
    
    occlusion = pow(occlusion, 2.0f);
    
    return float4(occlusion, occlusion, occlusion, 1.0f);
}