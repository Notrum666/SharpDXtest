#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u0);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    tetrahedrons[index].indices = int4(-1, -1, -1, -1);
}