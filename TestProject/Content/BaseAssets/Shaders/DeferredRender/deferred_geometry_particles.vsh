struct vert_out
{
    float4 v : SV_POSITION;
};

cbuffer mat
{
    float4x4 proj;
    float4x4 view;
    float4x4 model;
};


struct Particle
{
    float3 position;
    float energy;
    float3 velocity;
};

StructuredBuffer<Particle> particles : register(t0);

vert_out main(uint id : SV_VertexID)
{
	vert_out res = (vert_out) 0;

    float4 v_world = mul(float4(particles[id].position, 1.0f), model);

	res.v = v_world;
    
	return res;
}