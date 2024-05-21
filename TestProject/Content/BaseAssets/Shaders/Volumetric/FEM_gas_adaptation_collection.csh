#define THREADS_X 32
#define THREADS_Y 32
#define THREADS_TOTAL THREADS_X * THREADS_Y
#define MAX_SUBDIVISIONS 7

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

struct MeshVertex
{
    float3 position;
    float density;
    float3 velocity;
    float nextDensity;
};

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u1);
RWStructuredBuffer<MeshVertex> meshVertices : register(u2);
RWStructuredBuffer<int> octreeSubdivisionList : register(u3);
RWStructuredBuffer<int> octreeUnsubdivisionList : register(u4);

float breakpointFunc(float x)
{
    //return 4.0f / (5.99f - x) - 0.5f;
    float res = sqrt(0.5f * MAX_SUBDIVISIONS / (MAX_SUBDIVISIONS - x + 0.001f) - 0.45f);
    return res > 0.0f ? res : 1.#INF;
}

#define checkMinMaxForOctant(octant) \
tetrahedronsEnd = octant.tetrahedronsEnd; \
for (i = octant.tetrahedronsStart; i < tetrahedronsEnd; i++) \
{ \
    int4 indices = tetrahedrons[i].indices; \
    float4 densities = float4(meshVertices[indices.x].density, \
                              meshVertices[indices.y].density, \
                              meshVertices[indices.z].density, \
                              meshVertices[indices.w].density); \
    minDensity = min(minDensity, min(min(densities.x, densities.y), min(densities.z, densities.w))); \
    maxDensity = max(maxDensity, max(max(densities.x, densities.y), max(densities.z, densities.w))); \
}

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    OctreeNode curNode = octree[index];
    
    if (curNode.tetrahedronsStart < 0 || // allow only existing octants which are
        curNode.subLeavesBottom.x >= 0 && // leaf octants or parents of 8 leaves
       (octree[curNode.subLeavesBottom.x].subLeavesBottom.x >= 0 ||
        octree[curNode.subLeavesBottom.y].subLeavesBottom.x >= 0 ||
        octree[curNode.subLeavesBottom.z].subLeavesBottom.x >= 0 ||
        octree[curNode.subLeavesBottom.w].subLeavesBottom.x >= 0 ||
        octree[curNode.subLeavesTop.x].subLeavesBottom.x >= 0 ||
        octree[curNode.subLeavesTop.y].subLeavesBottom.x >= 0 ||
        octree[curNode.subLeavesTop.z].subLeavesBottom.x >= 0 ||
        octree[curNode.subLeavesTop.w].subLeavesBottom.x >= 0))
        return;
    
    int depth = 0;
    int parent = curNode.parent;
    while (parent != -1)
    {
        depth++;
        parent = octree[parent].parent;
    }
    
    //if (depth >= 6)
    //    return;
    
    float breakpoint = breakpointFunc(depth);
    
    float minDensity = 1.#INF, maxDensity = -1.#INF;
    int tetrahedronsEnd, i;
    if (curNode.subLeavesBottom.x < 0) // leaf
    {
        checkMinMaxForOctant(curNode)
        
        float diff = maxDensity - minDensity;
        if (diff < breakpoint * 1.05f)
            return;
    
        octreeSubdivisionList[octreeSubdivisionList.IncrementCounter()] = index;
    }
    else // parent of 8 leaves
    {
        checkMinMaxForOctant(octree[curNode.subLeavesBottom.x])
        checkMinMaxForOctant(octree[curNode.subLeavesBottom.y])
        checkMinMaxForOctant(octree[curNode.subLeavesBottom.z])
        checkMinMaxForOctant(octree[curNode.subLeavesBottom.w])
        checkMinMaxForOctant(octree[curNode.subLeavesTop.x])
        checkMinMaxForOctant(octree[curNode.subLeavesTop.y])
        checkMinMaxForOctant(octree[curNode.subLeavesTop.z])
        checkMinMaxForOctant(octree[curNode.subLeavesTop.w])
        
        float diff = maxDensity - minDensity;
        if (diff > breakpoint / 1.05f)
            return;
        
        octreeUnsubdivisionList[octreeUnsubdivisionList.IncrementCounter()] = index;
    }
}