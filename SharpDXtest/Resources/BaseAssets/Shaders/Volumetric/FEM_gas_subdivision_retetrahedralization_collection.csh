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
RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u1);
RWStructuredBuffer<int> octreeSubdivisionList : register(u2);
RWStructuredBuffer<int> toRetetrahedralizeList : register(u3);

#define ascend_top \
if (index == parent.subLeavesBottom.x) { res = parent.subLeavesTop.x; break; } \
if (index == parent.subLeavesBottom.y) { res = parent.subLeavesTop.y; break; } \
if (index == parent.subLeavesBottom.z) { res = parent.subLeavesTop.z; break; } \
if (index == parent.subLeavesBottom.w) { res = parent.subLeavesTop.w; break; }

#define descend_top \
if (index == parent.subLeavesTop.x) res = octree[res].subLeavesBottom.x; \
if (index == parent.subLeavesTop.y) res = octree[res].subLeavesBottom.y; \
if (index == parent.subLeavesTop.z) res = octree[res].subLeavesBottom.z; \
if (index == parent.subLeavesTop.w) res = octree[res].subLeavesBottom.w;

#define ascend_bottom \
if (index == parent.subLeavesTop.x) { res = parent.subLeavesBottom.x; break; } \
if (index == parent.subLeavesTop.y) { res = parent.subLeavesBottom.y; break; } \
if (index == parent.subLeavesTop.z) { res = parent.subLeavesBottom.z; break; } \
if (index == parent.subLeavesTop.w) { res = parent.subLeavesBottom.w; break; }

#define descend_bottom \
if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesTop.x; \
if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesTop.y; \
if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesTop.z; \
if (index == parent.subLeavesBottom.w) res = octree[res].subLeavesTop.w;

#define ascend_right \
if (index == parent.subLeavesBottom.x) { res = parent.subLeavesBottom.y; break; } \
if (index == parent.subLeavesBottom.z) { res = parent.subLeavesBottom.w; break; } \
if (index == parent.subLeavesTop.x)    { res = parent.subLeavesTop.y;    break; } \
if (index == parent.subLeavesTop.z)    { res = parent.subLeavesTop.w;    break; }

#define descend_right \
if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesBottom.x; \
if (index == parent.subLeavesBottom.w) res = octree[res].subLeavesBottom.z; \
if (index == parent.subLeavesTop.y)    res = octree[res].subLeavesTop.x; \
if (index == parent.subLeavesTop.w)    res = octree[res].subLeavesTop.z;

#define ascend_left \
if (index == parent.subLeavesBottom.y) { res = parent.subLeavesBottom.x; break; } \
if (index == parent.subLeavesBottom.w) { res = parent.subLeavesBottom.z; break; } \
if (index == parent.subLeavesTop.y)    { res = parent.subLeavesTop.x;    break; } \
if (index == parent.subLeavesTop.w)    { res = parent.subLeavesTop.z;    break; }

#define descend_left \
if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesBottom.y; \
if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesBottom.w; \
if (index == parent.subLeavesTop.x)    res = octree[res].subLeavesTop.y; \
if (index == parent.subLeavesTop.z)    res = octree[res].subLeavesTop.w;

#define ascend_forward \
if (index == parent.subLeavesBottom.x) { res = parent.subLeavesBottom.z; break; } \
if (index == parent.subLeavesBottom.y) { res = parent.subLeavesBottom.w; break; } \
if (index == parent.subLeavesTop.x)    { res = parent.subLeavesTop.z;    break; } \
if (index == parent.subLeavesTop.y)    { res = parent.subLeavesTop.w;    break; }

#define descend_forward \
if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesBottom.x; \
if (index == parent.subLeavesBottom.w) res = octree[res].subLeavesBottom.y; \
if (index == parent.subLeavesTop.z)    res = octree[res].subLeavesTop.x; \
if (index == parent.subLeavesTop.w)    res = octree[res].subLeavesTop.y;

#define ascend_back \
if (index == parent.subLeavesBottom.z) { res = parent.subLeavesBottom.x; break; } \
if (index == parent.subLeavesBottom.w) { res = parent.subLeavesBottom.y; break; } \
if (index == parent.subLeavesTop.z)    { res = parent.subLeavesTop.x;    break; } \
if (index == parent.subLeavesTop.w)    { res = parent.subLeavesTop.y;    break; }

#define descend_back \
if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesBottom.z; \
if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesBottom.w; \
if (index == parent.subLeavesTop.x)    res = octree[res].subLeavesTop.z; \
if (index == parent.subLeavesTop.y)    res = octree[res].subLeavesTop.w;

#define findNeighbourDeclaration(name, ascention, descention) \
int findNeighbour_##name(int index, out int depth) \
{ \
    int parentIndex = octree[index].parent; \
    int res = -1; \
    int stack[32]; \
    OctreeNode parent; \
    depth = 0; \
    while (parentIndex >= 0) \
    { \
        parent = octree[parentIndex]; \
        ascention \
         \
        stack[depth] = index; \
        index = parentIndex; \
        parentIndex = octree[index].parent; \
         \
        depth++; \
    } \
     \
    if (res == -1) \
        return -1; \
     \
    while (depth > 0 && octree[res].subLeavesBottom.x >= 0) \
    { \
        parent = octree[index]; \
        depth--; \
        index = stack[depth]; \
        descention \
    } \
     \
    return res; \
}

#define findNeighbourShortDeclaration(side) findNeighbourDeclaration(side, ascend_##side, descend_##side)

findNeighbourShortDeclaration(top)
findNeighbourShortDeclaration(bottom)
findNeighbourShortDeclaration(right)
findNeighbourShortDeclaration(left)
findNeighbourShortDeclaration(forward)
findNeighbourShortDeclaration(back)

#define depthSearch(origin, childA, childB, childC, childD, vertexA, vertexB, vertexC, vertexD) \
{ \
    stack[pos++] = origin; \
    while (pos > 0) \
    { \
        pos--; \
        OctreeNode stackNode = octree[stack[pos]]; \
        if (stackNode.subLeavesBottom.x >= 0) \
        { \
            stack[pos++] = stackNode.childA; \
            stack[pos++] = stackNode.childB; \
            stack[pos++] = stackNode.childC; \
            stack[pos++] = stackNode.childD; \
        } \
        else \
        { \
            triangles[count++] = int3(stackNode.vertexA, stackNode.vertexB, stackNode.vertexD); \
            triangles[count++] = int3(stackNode.vertexA, stackNode.vertexC, stackNode.vertexD); \
        } \
    } \
}

#define checkOctant(side) \
if (side != -1 && (side##_depth > 0 || octree[side].subLeavesBottom.x == -1)) \
    toRetetrahedralizeList[side] = 1;

#define findAndCheckNeighbour(side) \
int side, side##_depth; \
side = findNeighbour_##side(curOctantIndex, side##_depth); \
checkOctant(side)

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    int curOctantIndex = octreeSubdivisionList[index];
    [branch] if (curOctantIndex < 0)
        return;
    
    octreeSubdivisionList[index] = -1;
    
    OctreeNode curOctant = octree[curOctantIndex];
    
    for (int i = curOctant.tetrahedronsStart; i < curOctant.tetrahedronsEnd; i++)
        tetrahedrons[i].indices = int4(-1, -1, -1, -1);
    curOctant.tetrahedronsStart = 0;
    curOctant.tetrahedronsEnd = 0;
    
    octree[curOctantIndex] = curOctant;
    
    findAndCheckNeighbour(top);
    findAndCheckNeighbour(bottom);
    findAndCheckNeighbour(right);
    findAndCheckNeighbour(left);
    findAndCheckNeighbour(forward);
    findAndCheckNeighbour(back);
    
    toRetetrahedralizeList[curOctant.subLeavesBottom.x] = 1;
    toRetetrahedralizeList[curOctant.subLeavesBottom.y] = 1;
    toRetetrahedralizeList[curOctant.subLeavesBottom.z] = 1;
    toRetetrahedralizeList[curOctant.subLeavesBottom.w] = 1;
    toRetetrahedralizeList[curOctant.subLeavesTop.x] = 1;
    toRetetrahedralizeList[curOctant.subLeavesTop.y] = 1;
    toRetetrahedralizeList[curOctant.subLeavesTop.z] = 1;
    toRetetrahedralizeList[curOctant.subLeavesTop.w] = 1;
}