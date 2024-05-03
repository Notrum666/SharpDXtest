#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

cbuffer shiftData
{
    int shiftStart;
};

RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u0);
RWStructuredBuffer<int> tetrahedronsCounter : register(u1);
RWStructuredBuffer<int> tetrahedronsShiftLocation : register(u2);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = shiftStart + groupIndex;
    
    int deletedCount = tetrahedronsShiftLocation[1];
    
    Tetrahedron tmp = tetrahedrons[index + deletedCount];
    AllMemoryBarrierWithGroupSync();
    tetrahedrons[index] = tmp;
}