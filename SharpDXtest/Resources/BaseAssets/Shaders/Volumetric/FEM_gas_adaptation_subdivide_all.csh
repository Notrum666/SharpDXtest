#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y
#define MAX_SUBDIVISIONS 6

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

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<int> octreeSubdivisionList : register(u1);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    OctreeNode curNode = octree[index];
    
    if (curNode.tetrahedronsStart < 0 || // allow only existing octants which are
        curNode.subLeavesBottom.x >= 0) // leaf octants
        return;
    
    octreeSubdivisionList[octreeSubdivisionList.IncrementCounter()] = index;
}