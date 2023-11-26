struct vert_in
{
    float4 p : SV_POSITION;
    float2 t : TEX;
};

cbuffer textureBuf
{
    Texture2D inputTex;
};

float main(vert_in v) : SV_Target
{
    float4 texel;
    texel.x = inputTex.Load(int3(v.p.xy * 2, 0)).x;
    texel.y = inputTex.Load(int3(v.p.xy * 2 + int2(1, 0), 0)).x;
    texel.z = inputTex.Load(int3(v.p.xy * 2 + int2(0, 1), 0)).x;
    texel.w = inputTex.Load(int3(v.p.xy * 2 + int2(1, 1), 0)).x;
    
    return float4(min(texel.x, min(texel.y, min(texel.z, texel.w))), 0.0f, 0.0f, 0.0f);
}