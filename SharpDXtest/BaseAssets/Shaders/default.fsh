struct vert_in
{
    float4 _ : SV_POSITION;
    float4 v : POS;
    float2 t : TEX;
    float4 n : NORM;
};

float4 main(vert_in v) : SV_Target
{
    return float4(v.n.x, v.n.y, -v.n.z, 1.0f);
}