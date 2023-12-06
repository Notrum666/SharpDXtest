struct vert_in
{
	float4 sv_pos : SV_POSITION;
};

float main(vert_in v) : SV_Depth
{
	return v.sv_pos.z;
}
