struct vert_in
{
    float3 v : POS;
};

struct vert_out
{
    float4 sv_pos : SV_POSITION;
};

cbuffer mat
{
    float4x4 modelViewProj;
};

vert_out main(vert_in vert)
{
	vert_out res = (vert_out) 0;

    res.sv_pos = mul(float4(vert.v, 1.0f), modelViewProj);
    
	return res;
}