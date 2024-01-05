struct vert_in
{
	float3 v : POS;
	float2 t : TEX;
	float3 n : NORM;
	float3 tx : TANGENT;
};

struct vert_out
{
	float4 sv_pos : SV_POSITION;
};

cbuffer mat
{
	float4x4 model;
	float4x4 view;
};

vert_out main(vert_in vert)
{
	vert_out res = (vert_out) 0;
	
	res.sv_pos = mul(mul(float4(vert.v, 1.0), model), view);
	
	return res;
}
