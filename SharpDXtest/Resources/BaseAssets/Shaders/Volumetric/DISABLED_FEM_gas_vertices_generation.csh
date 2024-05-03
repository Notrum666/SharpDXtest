#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y

#define UINT_MAX 4294967295U

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

struct MeshVertex
{
    float3 position;
    float density;
    float3 velocity;
    float nextDensity;
};

cbuffer volumeData
{
    float3 volumeHalfSize;
};

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<MeshVertex> meshVertices : register(u1);

MeshVertex createVertex(float3 position)
{
    MeshVertex vertex = (MeshVertex) 0;
    vertex.position = position;
    //vertex.density = (float)((uint) (abs(position.x * position.y + position.x * position.z + position.y * position.z +
    //                    position.x + position.y + position.z) * 100000.0f) * 196314165U + 907633515U) / UINT_MAX;
    float3 homogenous = position / volumeHalfSize;
    //float3 r = homogenous - float3(0.5f, 0.5f, 0.0f);
    //vertex.density = dot(r, r) <= 0.25f ? 10.0f : 0.0f;
    vertex.density = homogenous.x * 0.5f + 0.5f;
    //vertex.density *= vertex.density;
    //vertex.velocity = float3(position.y, -position.x, 0) / 5.0f;
    //vertex.velocity = vertex.velocity / max(dot(vertex.velocity, vertex.velocity), 1.0f);
    //vertex.density = max(0.0f, 1.0f - length(position / volumeHalfSize));
    return vertex;
}

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

//#define ascend_bottom \
//if (index == parent.subLeavesTop.x) { res = parent.subLeavesBottom.x; break; } \
//if (index == parent.subLeavesTop.y) { res = parent.subLeavesBottom.y; break; } \
//if (index == parent.subLeavesTop.z) { res = parent.subLeavesBottom.z; break; } \
//if (index == parent.subLeavesTop.w) { res = parent.subLeavesBottom.w; break; }
//
//#define descend_bottom \
//if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesTop.x; \
//if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesTop.y; \
//if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesTop.z; \
//if (index == parent.subLeavesBottom.w) res = octree[res].subLeavesTop.w;

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

//#define ascend_left \
//if (index == parent.subLeavesBottom.y) { res = parent.subLeavesBottom.x; break; } \
//if (index == parent.subLeavesBottom.w) { res = parent.subLeavesBottom.z; break; } \
//if (index == parent.subLeavesTop.y)    { res = parent.subLeavesTop.x;    break; } \
//if (index == parent.subLeavesTop.w)    { res = parent.subLeavesTop.z;    break; }
//
//#define descend_left \
//if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesBottom.y; \
//if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesBottom.w; \
//if (index == parent.subLeavesTop.x)    res = octree[res].subLeavesTop.y; \
//if (index == parent.subLeavesTop.z)    res = octree[res].subLeavesTop.w;

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

//#define ascend_back \
//if (index == parent.subLeavesBottom.z) { res = parent.subLeavesBottom.x; break; } \
//if (index == parent.subLeavesBottom.w) { res = parent.subLeavesBottom.y; break; } \
//if (index == parent.subLeavesTop.z)    { res = parent.subLeavesTop.x;    break; } \
//if (index == parent.subLeavesTop.w)    { res = parent.subLeavesTop.y;    break; }
//
//#define descend_back \
//if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesBottom.z; \
//if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesBottom.w; \
//if (index == parent.subLeavesTop.x)    res = octree[res].subLeavesTop.z; \
//if (index == parent.subLeavesTop.y)    res = octree[res].subLeavesTop.w;

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
//findNeighbourDeclaration(Bottom, ascend_bottom, descend_bottom)
findNeighbourDeclaration(Right, ascend_right, descend_right)
//findNeighbourDeclaration(Left, ascend_left, descend_left)
findNeighbourDeclaration(Forward, ascend_forward, descend_forward)
//findNeighbourDeclaration(Back, ascend_back, descend_back)

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

float3 transformNPointToLocalSpace(float3 nPoint)
{
    return (nPoint * 2.0f - 1.0f) * volumeHalfSize;
}

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    OctreeNode curNode = octree[index];
    
    if (curNode.tetrahedronsStart < 0 || curNode.subLeavesBottom.x >= 0) // ignore non-existing and non-leaf octree elements
        return;
    
    float3 ownPoints[9] =
    {
        ascendPoint(index, float3(0.0f, 0.0f, 0.0f)),
        ascendPoint(index, float3(1.0f, 0.0f, 0.0f)),
        ascendPoint(index, float3(0.0f, 1.0f, 0.0f)),
        ascendPoint(index, float3(1.0f, 1.0f, 0.0f)),
        ascendPoint(index, float3(0.0f, 0.0f, 1.0f)),
        ascendPoint(index, float3(1.0f, 0.0f, 1.0f)),
        ascendPoint(index, float3(0.0f, 1.0f, 1.0f)),
        ascendPoint(index, float3(1.0f, 1.0f, 1.0f)),
        ascendPoint(index, float3(0.5f, 0.5f, 0.5f))
    };
    
    int depth_top, depth_right, depth_forward;
    int top     = findNeighbour_Top    (index, depth_top    );
    int right   = findNeighbour_Right  (index, depth_right  );
    int forward = findNeighbour_Forward(index, depth_forward);
    
    float3 topPoints[3];
    if (top != -1 && depth_top > 0)
    {
        topPoints[0] = ascendPoint(top, float3(0.0f, 0.0f, 0.0f));
        topPoints[1] = ascendPoint(top, float3(1.0f, 0.0f, 0.0f));
        topPoints[2] = ascendPoint(top, float3(0.0f, 1.0f, 0.0f));
    }
    float3 rightPoints[3];
    if (right != -1 && depth_right > 0)
    {
        rightPoints[0] = ascendPoint(right, float3(0.0f, 0.0f, 0.0f));
        rightPoints[1] = ascendPoint(right, float3(0.0f, 1.0f, 0.0f));
        rightPoints[2] = ascendPoint(right, float3(0.0f, 0.0f, 1.0f));
    }
    float3 forwardPoints[3];
    if (forward != -1 && depth_forward > 0)
    {
        forwardPoints[0] = ascendPoint(right, float3(0.0f, 0.0f, 0.0f));
        forwardPoints[1] = ascendPoint(right, float3(1.0f, 0.0f, 0.0f));
        forwardPoints[2] = ascendPoint(right, float3(0.0f, 0.0f, 1.0f));
    }
    
    // FUCK
    // does not work in case of edge-touching octants with no octants of same size between them
    // [] 
    //   []
    
    int vertIndex = meshVertices.IncrementCounter(); // (-, -, -) - always generate
    curNode.verticesBottom.x = vertIndex;
    meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[0]));
    
    if (right == -1 || depth_right > 0 && any(rightPoints[0] != ownPoints[1])) // (+, -, -) - generate only if not generated by right neighbour
    {
        vertIndex = meshVertices.IncrementCounter();
        curNode.verticesBottom.y = vertIndex;
        meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[1]));
    }
    
    if (forward == -1 || depth_forward > 0 && any(forwardPoints[0] != ownPoints[2])) // (-, +, -) - generate only if not generated by forward neighbour
    {
        vertIndex = meshVertices.IncrementCounter();
        curNode.verticesBottom.z = vertIndex;
        meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[2]));
    }
    
    if ((forward == -1 || depth_forward > 0 && any(forwardPoints[0] != ownPoints[3])) && // (+, +, -) - generate only if not generated by
        (right == -1 || depth_right > 0 && any(rightPoints[1] != ownPoints[3])))         // forward neighbour or right neighbour
    {
        vertIndex = meshVertices.IncrementCounter();
        curNode.verticesBottom.w = vertIndex;
        meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[3]));
    }
    
    if (top == -1 || depth_top > 0 && any(topPoints[0] != ownPoints[4])) // (-, -, +) - generate only if not generated by top neighbour
    {
        vertIndex = meshVertices.IncrementCounter();
        curNode.verticesTop.x = vertIndex;
        meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[4]));
    }
    
    if ((right == -1 || depth_right > 0 && any(rightPoints[2] != ownPoints[5])) && // (+, -, +) - generate only if not generated by
        (top == -1 || depth_top > 0 && any(topPoints[1] != ownPoints[5])))         // right neighbour or top neighbour
    {
        vertIndex = meshVertices.IncrementCounter();
        curNode.verticesTop.y = vertIndex;
        meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[5]));
    }
    
    if ((forward == -1 || depth_forward > 0 && any(forwardPoints[2] != ownPoints[6])) && // (-, +, +) - generate only if not generated by
        (top == -1 || depth_top > 0 && any(topPoints[2] != ownPoints[6])))               // forward neighbour or top neighbour
    {
        vertIndex = meshVertices.IncrementCounter();
        curNode.verticesTop.z = vertIndex;
        meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[6]));
    }
    
    if (top == -1 && right == -1 && forward == -1) // (+, +, +) - generate only if it's the corner of the volume
    {
        vertIndex = meshVertices.IncrementCounter();
        curNode.verticesTop.w = vertIndex;
        meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[7]));
    }
    
    vertIndex = meshVertices.IncrementCounter(); // middle point - always generate
    curNode.middleVertex = vertIndex;
    meshVertices[vertIndex] = createVertex(transformNPointToLocalSpace(ownPoints[8]));

    octree[index] = curNode;
}