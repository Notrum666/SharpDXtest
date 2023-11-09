struct vert_in
{
    float4 _ : SV_POSITION;
    float2 t : TEX;
};

struct AmbientLight
{
    float brightness;
    float3 color;
};

cbuffer textureBuf
{
    Texture2D worldPosTex;
    Texture2D albedoTex;
    Texture2D normalTex;
    Texture2D metallicTex;
    Texture2D roughnessTex;
};

cbuffer lightBuf
{
    AmbientLight ambientLight;
};


float4 main(vert_in v) : SV_Target
{
    return ambientLight.color * ambientLight.brightness;
}