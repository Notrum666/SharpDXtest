#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y

struct MeshVertex
{
    float3 position;
    float density;
    float3 velocity;
    float nextDensity;
};

cbuffer volumeData
{
    float3 invHalfSize;
};

RWStructuredBuffer<MeshVertex> meshVertices : register(u0);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    MeshVertex curVertex = meshVertices[index];
    
    if (curVertex.density < 0.0f) // ignore non-existing vertices
        return;
    
    float3 homogenous = curVertex.position * invHalfSize;
    curVertex.density = 3.0f * (homogenous.z >= 0.71f);
    //curVertex.density = 1.0f;
    
    curVertex.nextDensity = curVertex.density;
    
    meshVertices[index] = curVertex;
}