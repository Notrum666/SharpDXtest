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

#define createAndSetNewLeaf(target) \
newIndex = octree.IncrementCounter(); \
octree[newIndex].parent = index; \
octree[newIndex].tetrahedronsStart = 0; \
octree[newIndex].tetrahedronsEnd = 0; \
curNode.target = newIndex;

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    OctreeNode curNode = octree[index];
    
    if (curNode.tetrahedronsStart < 0 || curNode.subLeavesBottom.x >= 0) // ignore non-existing and non-leaf octree elements
        return;
    
    if (curNode.parent != -1)
    {
        OctreeNode parent = octree[curNode.parent];
        //if (index == parent.subLeavesBottom.x || // subdivide only non-left octants
        //    index == parent.subLeavesBottom.z ||
        //    index == parent.subLeavesTop.x ||
        //    index == parent.subLeavesTop.z)
        if (!(index == parent.subLeavesTop.y &&
            index != 6 || index == 8))
            return;
    }
    
    int newIndex;
    
    createAndSetNewLeaf(subLeavesBottom.x)
    createAndSetNewLeaf(subLeavesBottom.y)
    createAndSetNewLeaf(subLeavesBottom.z)
    createAndSetNewLeaf(subLeavesBottom.w)
    createAndSetNewLeaf(subLeavesTop.x)
    createAndSetNewLeaf(subLeavesTop.y)
    createAndSetNewLeaf(subLeavesTop.z)
    createAndSetNewLeaf(subLeavesTop.w)
    
    octree[index] = curNode;
}