struct vert_in
{
    float3 v : POS;
    float2 t : TEX;
    float3 n : NORM;
};

struct vert_out
{
    float4 sv_pos : SV_POSITION;
    float4 v : POS;
    float2 t : TEX;
    float4 n : NORM;
};

float4x4 proj;
//float4x4 view;
//float4x4 model;

vert_out main(vert_in vert)
{
    vert_out res = (vert_out)0;
    res.sv_pos = mul(proj/* * view * model*/, float4(vert.v, 1.0f));
    
    res.v = mul(proj/* * view * model*/, float4(vert.v, 1.0f));
    res.t = vert.t;
    res.n = float4(vert.n, 0.0f);
    
    return res;
}