#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u0);
RWStructuredBuffer<int> tetrahedronsCounter : register(u1);
RWStructuredBuffer<int> tetrahedronsShiftLocation : register(u2);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    int count = tetrahedronsCounter[0];
    if (index >= count)
        return;
    
    if (tetrahedrons[index].indices.x == -1 && (index == 0 || tetrahedrons[index - 1].indices.x != -1))
    {
        int deletedCount = 1;
        while (index + deletedCount < count && tetrahedrons[index + deletedCount].indices.x == -1)
            deletedCount++;
        int prevDeletedCount;
        InterlockedMax(tetrahedronsShiftLocation[1], deletedCount, prevDeletedCount);
        int original;
        if (deletedCount >= prevDeletedCount)
            InterlockedExchange(tetrahedronsShiftLocation[0], index, original);
    }
}