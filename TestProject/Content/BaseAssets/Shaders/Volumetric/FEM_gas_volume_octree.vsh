struct vert_out
{
	float4 sv_pos : SV_POSITION;
};

struct OctreeNode
{
    // (-,-,-), (+,-,-), (-,+,-), (+,+,-)
    int4 subLeavesBottom;
    // (-,-,+), (+,-,+), (-,+,+), (+,+,+)
    int4 subLeavesTop;
    int parent;
    int tetrahedronsStart, tetrahedronsEnd;
    int middleVertex;
    int4 verticesBottom;
    int4 verticesTop;
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

StructuredBuffer<OctreeNode> octree : register(t0);
StructuredBuffer<MeshVertex> meshVertices : register(t1);

static const int pairs[24] =
{
    0, 1,
    2, 3,
    4, 5,
    6, 7,
    0, 2,
    1, 3,
    4, 6,
    5, 7,
    0, 4,
    1, 5,
    2, 6,
    3, 7
};

vert_out main(uint id : SV_VertexID)
{
    int octant = id / 24;
    int index = id % 24;
    
    int meshVertexIndex;
    switch (pairs[index])
    {
        case 0: meshVertexIndex = octree[octant].verticesBottom.x; break;
        case 1: meshVertexIndex = octree[octant].verticesBottom.y; break;
        case 2: meshVertexIndex = octree[octant].verticesBottom.z; break;
        case 3: meshVertexIndex = octree[octant].verticesBottom.w; break;
        case 4: meshVertexIndex = octree[octant].verticesTop.x; break;
        case 5: meshVertexIndex = octree[octant].verticesTop.y; break;
        case 6: meshVertexIndex = octree[octant].verticesTop.z; break;
        case 7: meshVertexIndex = octree[octant].verticesTop.w; break;
    };
    
	vert_out res = (vert_out) 0;
	
    res.sv_pos = mul(float4(meshVertices[meshVertexIndex].position, 1.0f), modelViewProj);
	
	return res;
}
