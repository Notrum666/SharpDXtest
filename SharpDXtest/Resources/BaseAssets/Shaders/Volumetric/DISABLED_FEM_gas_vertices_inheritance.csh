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

RWStructuredBuffer<OctreeNode> octree : register(u0);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    OctreeNode curNode = octree[index];
    
    if (curNode.tetrahedronsStart < 0 || curNode.subLeavesBottom.x < 0) // ignore non-existing and leaf octree elements
        return;
    
    curNode.verticesBottom.x = octree[curNode.subLeavesBottom.x].verticesBottom.x;
    curNode.verticesBottom.y = octree[curNode.subLeavesBottom.y].verticesBottom.y;
    curNode.verticesBottom.z = octree[curNode.subLeavesBottom.z].verticesBottom.z;
    curNode.verticesBottom.w = octree[curNode.subLeavesBottom.w].verticesBottom.w;
    curNode.verticesTop.x = octree[curNode.subLeavesTop.x].verticesTop.x;
    curNode.verticesTop.y = octree[curNode.subLeavesTop.y].verticesTop.y;
    curNode.verticesTop.z = octree[curNode.subLeavesTop.z].verticesTop.z;
    curNode.verticesTop.w = octree[curNode.subLeavesTop.w].verticesTop.w;
    curNode.middleVertex = octree[curNode.subLeavesBottom.x].verticesTop.w;
    
    octree[index] = curNode;
}