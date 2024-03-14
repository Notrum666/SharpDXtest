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

findNeighbourDeclaration(Top, ascend_top, descend_top)
findNeighbourDeclaration(Right, ascend_right, descend_right)
findNeighbourDeclaration(Forward, ascend_forward, descend_forward)

float3 ascendPoint(int index, float3 p)
{
    int parentIndex = octree[index].parent;
    while (parentIndex != -1)
    {
        p *= 0.5f;
        
        OctreeNode parent = octree[parentIndex];
        if (index == parent.subLeavesBottom.y)
            p += float3(0.5f, 0.0f, 0.0f);
        if (index == parent.subLeavesBottom.z)
            p += float3(0.0f, 0.5f, 0.0f);
        if (index == parent.subLeavesBottom.w)
            p += float3(0.5f, 0.5f, 0.0f);
        if (index == parent.subLeavesTop.x)
            p += float3(0.0f, 0.0f, 0.5f);
        if (index == parent.subLeavesTop.y)
            p += float3(0.5f, 0.0f, 0.5f);
        if (index == parent.subLeavesTop.z)
            p += float3(0.0f, 0.5f, 0.5f);
        if (index == parent.subLeavesTop.w)
            p += float3(0.5f, 0.5f, 0.5f);
        
        index = parentIndex;
        parentIndex = parent.parent;
    }
    
    return p;
}

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    OctreeNode curNode = octree[index];
    
    if (curNode.tetrahedronsStart < 0 || curNode.subLeavesBottom.x >= 0) // ignore non-existing and non-leaf octree elements
        return;
    
    if (curNode.verticesBottom.y >= 0 && curNode.verticesBottom.z >= 0 && curNode.verticesBottom.w >= 0 &&
        curNode.verticesTop.x >= 0 && curNode.verticesTop.y >= 0 && curNode.verticesTop.z >= 0 && curNode.verticesTop.w >= 0)
        return;
    
    float3 ownPoints[4] =
    {
        ascendPoint(index, float3(1.0f, 1.0f, 0.0f)), // (+, +, -)
        ascendPoint(index, float3(1.0f, 0.0f, 1.0f)), // (+, -, +)
        ascendPoint(index, float3(0.0f, 1.0f, 1.0f)), // (-, +, +)
        ascendPoint(index, float3(1.0f, 1.0f, 1.0f)) // (+, +, +)
    };
    
    int depth_top, depth_right, depth_forward;
    int top = findNeighbour_Top(index, depth_top);
    int right = findNeighbour_Right(index, depth_right);
    int forward = findNeighbour_Forward(index, depth_forward);
    
    float3 topPoints[3];
    if (top != -1 && depth_top > 0)
    {
        topPoints[0] = ascendPoint(top, float3(1.0f, 0.0f, 0.0f));
        topPoints[1] = ascendPoint(top, float3(0.0f, 1.0f, 0.0f));
        topPoints[2] = ascendPoint(top, float3(1.0f, 1.0f, 0.0f));
    }
    float3 rightPoints[3];
    if (right != -1 && depth_right > 0)
    {
        rightPoints[0] = ascendPoint(right, float3(0.0f, 1.0f, 0.0f));
        rightPoints[1] = ascendPoint(right, float3(0.0f, 0.0f, 1.0f));
        rightPoints[2] = ascendPoint(right, float3(0.0f, 1.0f, 1.0f));
    }
    float3 forwardPoints[3];
    if (forward != -1 && depth_forward > 0)
    {
        forwardPoints[0] = ascendPoint(right, float3(1.0f, 0.0f, 0.0f));
        forwardPoints[1] = ascendPoint(right, float3(0.0f, 0.0f, 1.0f));
        forwardPoints[2] = ascendPoint(right, float3(1.0f, 0.0f, 1.0f));
    }
    
    if (curNode.verticesBottom.y == -1)
        curNode.verticesBottom.y = octree[right].verticesBottom.x;
    if (curNode.verticesBottom.z == -1)
        curNode.verticesBottom.z = octree[forward].verticesBottom.x;
    if (curNode.verticesBottom.w == -1)
    {
        if (right != -1 && (depth_right == 0 || all(ownPoints[0] == rightPoints[0])))
            curNode.verticesBottom.w = octree[right].verticesBottom.z;
        else
            curNode.verticesBottom.w = octree[forward].verticesBottom.y;
    }
    if (curNode.verticesTop.x == -1)
        curNode.verticesTop.x = octree[top].verticesBottom.x;
    if (curNode.verticesTop.y == -1)
    {
        if (right != -1 && (depth_right == 0 || all(ownPoints[1] == rightPoints[1])))
            curNode.verticesTop.y = octree[right].verticesTop.x;
        else
            curNode.verticesTop.y = octree[top].verticesBottom.y;
    }
    if (curNode.verticesTop.z == -1)
    {
        if (forward != -1 && (depth_forward == 0 || all(ownPoints[2] == forwardPoints[1])))
            curNode.verticesTop.z = octree[forward].verticesTop.x;
        else
            curNode.verticesTop.z = octree[top].verticesBottom.z;
    }
    if (curNode.verticesTop.w == -1)
    {
        if (right != -1 && (depth_right == 0 || all(ownPoints[3] == rightPoints[2])))
            curNode.verticesTop.w = octree[right].verticesTop.z;
        else
        {
            if (forward != -1 && (depth_forward == 0 || all(ownPoints[3] == forwardPoints[2])))
                curNode.verticesTop.w = octree[forward].verticesTop.y;
            else
                curNode.verticesTop.w = octree[top].verticesBottom.w;
        }
    }
    
    octree[index] = curNode;
}