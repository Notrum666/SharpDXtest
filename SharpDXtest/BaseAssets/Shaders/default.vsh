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

cbuffer mat
{
    float4x4 proj;
    float4x4 view;
    float4x4 model;
};

vert_out main(vert_in vert)
{
    vert_out res = (vert_out)0;
    res.sv_pos = mul(mul(mul(float4(vert.v, 1.0f), model), view), proj);
    
    res.v = res.sv_pos;
    res.t = vert.t;
    res.n = float4(vert.n, 0.0f);
    
    return res;
}