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
    
    OctreeNode curNode = (OctreeNode) 0;
    
    curNode.subLeavesBottom = int4(-1, -1, -1, -1);
    curNode.subLeavesTop = int4(-1, -1, -1, -1);
    curNode.parent = -1;
    curNode.middleVertex = -1;
    curNode.verticesBottom = int4(-1, -1, -1, -1);
    curNode.verticesTop = int4(-1, -1, -1, -1);
    curNode.tetrahedronsStart = -1;
    curNode.tetrahedronsEnd = -1;
    
    if (index == 0)
    {
        octree.IncrementCounter();
        curNode.tetrahedronsStart = 0;
        curNode.tetrahedronsEnd = 0;
    }
    
    octree[index] = curNode;
}