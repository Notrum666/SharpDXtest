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

struct MeshVertex
{
    float3 position;
    float density;
    float3 velocity;
    float nextDensity;
};

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<int> freeOctantsList : register(u1);
RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u2);
RWStructuredBuffer<MeshVertex> meshVertices : register(u3);
RWStructuredBuffer<int> freeMeshVerticesList : register(u4);
RWStructuredBuffer<int> octreeUnsubdivisionList : register(u5);

void deleteVertex(int index)
{   
    freeMeshVerticesList[freeMeshVerticesList.IncrementCounter()] = index;
    meshVertices[index].density = -1;
}

#define deleteTetrahedronsOf(octant) \
for (i = octant.tetrahedronsStart; i < octant.tetrahedronsEnd; i++) \
    tetrahedrons[i].indices = int4(-1, -1, -1, -1);

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    int curOctantIndex = octreeUnsubdivisionList[index];
    if (curOctantIndex == -1)
        return;
    
    OctreeNode curOctant = octree[curOctantIndex];
    
    OctreeNode bottomX = octree[curOctant.subLeavesBottom.x];
    OctreeNode bottomY = octree[curOctant.subLeavesBottom.y];
    OctreeNode bottomZ = octree[curOctant.subLeavesBottom.z];
    OctreeNode bottomW = octree[curOctant.subLeavesBottom.w];
    OctreeNode topX = octree[curOctant.subLeavesTop.x];
    OctreeNode topY = octree[curOctant.subLeavesTop.y];
    OctreeNode topZ = octree[curOctant.subLeavesTop.z];
    OctreeNode topW = octree[curOctant.subLeavesTop.w];
    
    int i;
    deleteTetrahedronsOf(bottomX)
    deleteTetrahedronsOf(bottomY)
    deleteTetrahedronsOf(bottomZ)
    deleteTetrahedronsOf(bottomW)
    deleteTetrahedronsOf(topX)
    deleteTetrahedronsOf(topY)
    deleteTetrahedronsOf(topZ)
    deleteTetrahedronsOf(topW)
    
    deleteVertex(bottomX.middleVertex);
    deleteVertex(bottomY.middleVertex);
    deleteVertex(bottomZ.middleVertex);
    deleteVertex(bottomW.middleVertex);
    deleteVertex(topX.middleVertex);
    deleteVertex(topY.middleVertex);
    deleteVertex(topZ.middleVertex);
    deleteVertex(topW.middleVertex);
    
    deleteVertex(bottomX.verticesTop.y); // back
    deleteVertex(bottomZ.verticesTop.w); // front
    deleteVertex(bottomY.verticesTop.w); // right
    deleteVertex(bottomX.verticesTop.z); // left
    deleteVertex(bottomX.verticesBottom.w); // bottom
    deleteVertex(topX.verticesTop.w); // top
    
    deleteVertex(bottomX.verticesBottom.z); // bottomLeft
    deleteVertex(bottomX.verticesBottom.y); // bottomBack
    deleteVertex(bottomY.verticesBottom.w); // bottomRight
    deleteVertex(bottomZ.verticesBottom.w); // bottomForward
    deleteVertex(topX.verticesTop.z); // topLeft
    deleteVertex(topX.verticesTop.y); // topBack
    deleteVertex(topY.verticesTop.w); // topRight
    deleteVertex(topZ.verticesTop.w); // topForward
    deleteVertex(bottomY.verticesTop.y); // rightBack
    deleteVertex(bottomW.verticesTop.w); // rightForward
    deleteVertex(bottomX.verticesTop.x); // leftBack
    deleteVertex(bottomZ.verticesTop.z); // leftForward
    
    OctreeNode cleanOctant = (OctreeNode) 0;
    cleanOctant.subLeavesBottom = int4(-1, -1, -1, -1);
    cleanOctant.subLeavesTop = int4(-1, -1, -1, -1);
    cleanOctant.parent = -1;
    cleanOctant.middleVertex = -1;
    cleanOctant.verticesBottom = int4(-1, -1, -1, -1);
    cleanOctant.verticesTop = int4(-1, -1, -1, -1);
    cleanOctant.tetrahedronsStart = -1;
    cleanOctant.tetrahedronsEnd = -1;
    
    octree[curOctant.subLeavesBottom.x] = cleanOctant;
    octree[curOctant.subLeavesBottom.y] = cleanOctant;
    octree[curOctant.subLeavesBottom.z] = cleanOctant;
    octree[curOctant.subLeavesBottom.w] = cleanOctant;
    octree[curOctant.subLeavesTop.x] = cleanOctant;
    octree[curOctant.subLeavesTop.y] = cleanOctant;
    octree[curOctant.subLeavesTop.z] = cleanOctant;
    octree[curOctant.subLeavesTop.w] = cleanOctant;
    
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesBottom.x;
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesBottom.y;
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesBottom.z;
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesBottom.w;
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesTop.x;
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesTop.y;
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesTop.z;
    freeOctantsList[freeOctantsList.IncrementCounter()] = curOctant.subLeavesTop.w;
    
    curOctant.subLeavesBottom = int4(-1, -1, -1, -1);
    curOctant.subLeavesTop = int4(-1, -1, -1, -1);
    
    octree[curOctantIndex] = curOctant;
}