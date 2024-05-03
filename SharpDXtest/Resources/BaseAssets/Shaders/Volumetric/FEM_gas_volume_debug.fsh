struct vert_in
{
    float4 _ : SV_POSITION;
};

cbuffer debugData
{
    float3 color;
};

[earlydepthstencil]
float4 main(vert_in v) : SV_Target
{
    return float4(color, 1.0f);
}
