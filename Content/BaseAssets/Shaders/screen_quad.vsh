struct vert_out
{
	float4 sv_pos : SV_POSITION;
	float2 t : TEX;
};

struct vert_in
{
	float2 v : SV_POSITION;
	float2 t : TEX;
};

static const vert_in vertexes[6] =
{
	float2(-1.0f, -1.0f), float2(0.0f, 1.0f),
	float2(1.0f, -1.0f), float2(1.0f, 1.0f),
	float2(1.0f, 1.0f), float2(1.0f, 0.0f),
	float2(-1.0f, -1.0f), float2(0.0f, 1.0f),
	float2(1.0f, 1.0f), float2(1.0f, 0.0f),
	float2(-1.0f, 1.0f), float2(0.0f, 0.0f)
};

vert_out main(uint id : SV_VertexID)
{
	vert_out res = (vert_out) 0;
	res.sv_pos = float4(vertexes[id].v, 0.0f, 1.0f);
	res.t = vertexes[id].t;
	return res;
}
