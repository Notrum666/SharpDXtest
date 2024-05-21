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
RWStructuredBuffer<int> freeOctantsList : register(u1);
RWStructuredBuffer<MeshVertex> meshVertices : register(u2);
RWStructuredBuffer<int> freeMeshVerticesList : register(u3);

MeshVertex createVertex(float3 position)
{
    MeshVertex vertex = (MeshVertex) 0;
    vertex.position = position;
    //float3 homogenous = position / volumeHalfSize;
    //float3 r = homogenous - float3(1.0f, 1.0f, 1.0f);
    //vertex.density = dot(r, r) <= 1.0f ? 2.0f : 0.0f; //pow(homogenous.x * 0.5f + 0.5f, 4.0f);
    vertex.density = 0.0f;
    vertex.nextDensity = 0.0f;
    vertex.velocity = float3(0.0f, 0.0f, 0.0f);
    //vertex.velocity = float3(-1.0f, -1.0f, -1.0f) * 5.0f + float3(0.0f, 10.0f, 4.0f) * (-homogenous.x * 0.5f + 0.5f);
    //vertex.velocity *= 4.0f;
    return vertex;
}

float3 transformNPointToLocalSpace(float3 nPoint)
{
    return (nPoint * 2.0f - 1.0f) * volumeHalfSize;
}

int generateVertex(float3 homogenousLocation)
{
    int index = freeMeshVerticesList[freeMeshVerticesList.DecrementCounter()];
    meshVertices[index] = createVertex(transformNPointToLocalSpace(homogenousLocation));
    return index;
}

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
        freeOctantsList.DecrementCounter();
        curNode.tetrahedronsStart = 0;
        curNode.tetrahedronsEnd = 0;
        curNode.middleVertex = generateVertex(float3(0.5f, 0.5f, 0.5f));
        curNode.verticesBottom.x = generateVertex(float3(0.0f, 0.0f, 0.0f));
        curNode.verticesBottom.y = generateVertex(float3(1.0f, 0.0f, 0.0f));
        curNode.verticesBottom.z = generateVertex(float3(0.0f, 1.0f, 0.0f));
        curNode.verticesBottom.w = generateVertex(float3(1.0f, 1.0f, 0.0f));
        curNode.verticesTop.x    = generateVertex(float3(0.0f, 0.0f, 1.0f));
        curNode.verticesTop.y    = generateVertex(float3(1.0f, 0.0f, 1.0f));
        curNode.verticesTop.z    = generateVertex(float3(0.0f, 1.0f, 1.0f));
        curNode.verticesTop.w    = generateVertex(float3(1.0f, 1.0f, 1.0f));
    }
    
    octree[index] = curNode;
}