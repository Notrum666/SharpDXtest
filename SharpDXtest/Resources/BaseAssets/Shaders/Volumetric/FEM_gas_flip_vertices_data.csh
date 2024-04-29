#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y

#define UINT_MAX 4294967295U

struct MeshVertex
{
    float3 position;
    float density;
    float3 velocity;
    float nextDensity;
};

RWStructuredBuffer<MeshVertex> meshVertices : register(u0);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    MeshVertex curVertex = meshVertices[index];
    
    if (curVertex.density < 0.0f) // ignore non-existing vertices
        return;
    
    curVertex.density = curVertex.nextDensity;
    
    meshVertices[index] = curVertex;
}