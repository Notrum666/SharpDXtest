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
};

cbuffer ssaoParamsBuf
{
    float2 texSize;
    float4x4 camViewProj;
    float4x4 camView;
    float4x4 camProj;
    float3 randomVectors[SAMPLES_COUNT];
    float3 noise[16];
    float sampleRad;
};

//float4 sphere_pass(vert_in v)
//{
//    int2 texPos = v.t * texSize;
//    float3 pos = world2view(worldPosTex.Load(float3(texPos, 0.0f)).xyz).xyz;
    
//    float occlusion = 0.0f;
//    float bias = 0.0f;
    
//    for (int i = 0; i < SAMPLES_COUNT; i++)
//    {
//        float3 samplePos = randomVectors[i];
//        samplePos = pos + samplePos * sampleRad;

//        float4 offset = float4(samplePos, 1.0f);
        
//        offset = mul(offset, camProj);
        
//        offset.x = (offset.x / offset.w) * 0.5f + 0.5f;
//        offset.y = -(offset.y / offset.w) * 0.5f + 0.5f;
        
//        float sampleDepth = world2view(worldPosTex.Load(float3(offset.xy * texSize, 0.0f)).xyz).y;
        
        
//        float rangeCheck = smoothstep(0.0f, 1.0f, sampleRad / abs(pos.y - sampleDepth));
//        occlusion += (sampleDepth >= samplePos.y + bias ? 1.0f : 0.0f) * rangeCheck;
//    }
    
//    occlusion = occlusion / (float) SAMPLES_COUNT;

//    return float4((float3) occlusion, 1.0f);
//}

float4 half_sphere_pass(vert_in v)
{
    int2 texPos = v.t * texSize;
    
    float3 wp = worldPosTex.Load(float3(texPos, 0.0f)).xyz; //worldPosTex.Sample(texSampler, v.t).xyz;
    float3 pos = mul(float4(wp, 1.0f), camView).xyz;
    
    float3 n = mul(normalTex.Load(float3(texPos, 0)), camView).xyz;
    float3 rv = noise[texPos.x % 4 + (texPos.y % 4) * 4];
    
    float3 t = normalize(rv - n * dot(rv, n));
    float3 b = cross(t, n);
    float3x3 tbn = float3x3(t, n, b);
    
    float occlusion = 0.0f;
    float bias = 1e-5;
    
    for (int i = 0; i < SAMPLES_COUNT; i++)
    {
        float3 samplePos = pos + mul(randomVectors[i], tbn) * sampleRad;

        float4 offset = float4(samplePos, 1.0f);
        
        offset = mul(offset, camProj);
        
        offset.x = (offset.x / offset.w) * 0.5f + 0.5f;
        offset.y = -(offset.y / offset.w) * 0.5f + 0.5f;
        
        float3 wpp = worldPosTex.Load(float3(offset.xy * texSize, 0.0f)).xyz;
        float sampleDepth = mul(float4(wpp, 1.0f), camView).y;
        
        float distScale = smoothstep(0.0f, 1.0f, sampleRad / abs(pos.y - sampleDepth));
        occlusion += (sampleDepth <= samplePos.y - bias ? distScale : 0.0f);
    }
    
    occlusion = 1.0f - (occlusion / (float) SAMPLES_COUNT);

    return float4(float3(occlusion, occlusion, occlusion), 1.0f);
}

float4 main(vert_in v) : SV_Target
{
    return half_sphere_pass(v);
}