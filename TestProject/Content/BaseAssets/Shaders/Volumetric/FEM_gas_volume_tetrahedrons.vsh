struct vert_out
{
	float4 sv_pos : SV_POSITION;
};

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

struct MeshVertex
{
    float3 position;
    float density;
    float3 velocity;
    float nextDensity;
};

cbuffer mat
{
    float4x4 modelViewProj;
};

StructuredBuffer<Tetrahedron> tetrahedrons : register(t0);
StructuredBuffer<MeshVertex> meshVertices : register(t1);

static const int pairs[12] =
{
    0, 1,
    1, 2,
    0, 2,
    0, 3,
    1, 3,
    2, 3
};

vert_out main(uint id : SV_VertexID)
{
    int tetrahedronIndex = id / 12;
    int index = id % 12;
    
	vert_out res = (vert_out) 0;
	
    Tetrahedron tetrahedron = tetrahedrons[tetrahedronIndex];
    if (tetrahedron.indices.x >= 0)
        res.sv_pos = mul(float4(meshVertices[tetrahedron.indices[pairs[index]]].position, 1.0f), modelViewProj);
	
	return res;
}
