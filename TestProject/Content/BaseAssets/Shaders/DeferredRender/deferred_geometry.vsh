#define MAX_DIRECTIONAL_LIGHTS 4
#define MAX_DIRECTIONAL_LIGHT_CASCADES 4
#define MAX_SPOT_LIGHTS 8

struct vert_in
{
    float3 v : POS;
    float2 t : TEX;
    float3 n : NORM;
    float3 tx : TANGENT;
    int4 bones : BONE;
    float4 weights : WEIGHT;
};

struct vert_out
{
    float4 sv_pos : SV_POSITION;
    float4 v : POS;
    float2 t : TEX;
    float3 n : NORM;
	// ttw - tangent to world
    float3x3 ttw : TBN;
};

cbuffer mat
{
    float4x4 proj;
    float4x4 view;
    float4x4 model;
    float4x4 modelNorm;
};

vert_out main(vert_in vert)
{
    vert_out res = (vert_out) 0;
    
    float4 v_world = mul(float4(vert.v, 1.f), model);
    
    res.sv_pos = mul(mul(v_world, view), proj);
    res.v = v_world;
    res.t = vert.t;
    res.n = normalize(mul(float4(vert.n, 0.0f), modelNorm).xyz);

    float3 tangent = normalize(mul(float4(vert.tx, 0.0f), modelNorm).xyz);
    float3 bitangent = cross(res.n, tangent);
    res.ttw = float3x3(tangent, bitangent, res.n);

    return res;
}
