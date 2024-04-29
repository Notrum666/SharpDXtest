struct vert_in
{
    float4 _ : SV_POSITION;
};

[earlydepthstencil]
float4 main(vert_in v) : SV_Target
{
    return float4(0.5f, 1.0f, 0.5f, 1.0f);
}
