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
RWStructuredBuffer<int> freeOctantsList : register(u1);
RWStructuredBuffer<int> octreeSubdivisionList : register(u2);

#define createOctantDeclaration(name, side, direction, oppositeSide, oppositeDirection) \
int createOctant_##name(int parentIndex, OctreeNode parent) \
{ \
    OctreeNode curNode = (OctreeNode) 0; \
     \
    curNode.subLeavesBottom = int4(-1, -1, -1, -1); \
    curNode.subLeavesTop = int4(-1, -1, -1, -1); \
    curNode.parent = parentIndex; \
    curNode.middleVertex = -1; \
    curNode.verticesBottom = int4(-1, -1, -1, -1); \
    curNode.verticesTop = int4(-1, -1, -1, -1); \
    curNode.tetrahedronsStart = 0; \
    curNode.tetrahedronsEnd = 0; \
    curNode.vertices##oppositeSide.oppositeDirection = parent.middleVertex; \
    curNode.vertices##side.direction = parent.vertices##side.direction; \
     \
    int index = freeOctantsList[freeOctantsList.DecrementCounter()]; \
    octree[index] = curNode; \
    return index; \
}

createOctantDeclaration(BottomX, Bottom, x, Top, w)
createOctantDeclaration(BottomY, Bottom, y, Top, z)
createOctantDeclaration(BottomZ, Bottom, z, Top, y)
createOctantDeclaration(BottomW, Bottom, w, Top, x)
createOctantDeclaration(TopX, Top, x, Bottom, w)
createOctantDeclaration(TopY, Top, y, Bottom, z)
createOctantDeclaration(TopZ, Top, z, Bottom, y)
createOctantDeclaration(TopW, Top, w, Bottom, x)

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    int curOctantIndex = octreeSubdivisionList[index];
    if (curOctantIndex == -1)
        return;
    
    OctreeNode curOctant = octree[curOctantIndex];
    
    curOctant.subLeavesBottom.x = createOctant_BottomX(curOctantIndex, curOctant);
    curOctant.subLeavesBottom.y = createOctant_BottomY(curOctantIndex, curOctant);
    curOctant.subLeavesBottom.z = createOctant_BottomZ(curOctantIndex, curOctant);
    curOctant.subLeavesBottom.w = createOctant_BottomW(curOctantIndex, curOctant);
    curOctant.subLeavesTop.x = createOctant_TopX(curOctantIndex, curOctant);
    curOctant.subLeavesTop.y = createOctant_TopY(curOctantIndex, curOctant);
    curOctant.subLeavesTop.z = createOctant_TopZ(curOctantIndex, curOctant);
    curOctant.subLeavesTop.w = createOctant_TopW(curOctantIndex, curOctant);
    
    octree[curOctantIndex] = curOctant;
}