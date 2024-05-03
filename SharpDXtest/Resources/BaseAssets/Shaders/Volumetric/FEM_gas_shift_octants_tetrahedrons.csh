#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y

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

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<int> tetrahedronsCounter : register(u1);
RWStructuredBuffer<int> tetrahedronsShiftLocation : register(u2);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    int deletedCount = tetrahedronsShiftLocation[1];
    if (deletedCount <= 0)
        return;
    
    if (index == 0)
        tetrahedronsCounter[0] -= deletedCount;
    
    int shiftStart = tetrahedronsShiftLocation[0];
    OctreeNode curOctant = octree[index];
    if (curOctant.tetrahedronsStart <= shiftStart)
        return;
    
    curOctant.tetrahedronsStart -= deletedCount;
    curOctant.tetrahedronsEnd -= deletedCount;
    
    octree[index] = curOctant;
}