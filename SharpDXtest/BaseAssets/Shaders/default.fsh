struct vert_in
{
    float4 _ : SV_POSITION;
    float4 v : POS;
    float2 t : TEX;
    float4 n : NORM;
};

Texture2D shaderTexture;
SamplerState SampleType;

float4 main(vert_in v) : SV_Target
{
    return shaderTexture.Sample(SampleType, v.t); //float4(v.v.x, v.v.y, -v.n.z, 1.0f);
}