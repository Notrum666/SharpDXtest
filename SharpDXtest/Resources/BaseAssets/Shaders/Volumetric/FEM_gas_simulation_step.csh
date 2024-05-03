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

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

cbuffer volumeData
{
    float3 invHalfSize;
};

cbuffer simulationData
{
    float deltaTime;
    bool sourceEnabled;
};

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<MeshVertex> meshVertices : register(u1);
RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u2);

float SampleDensity(float3 location)
{
    float3 homogenous = location * invHalfSize; // from -1 to 1 within volume
    OctreeNode curLeaf = octree[0];
    while (true)
    {
        if (homogenous.z <= 0) // -z
        {
            homogenous.z = homogenous.z * 2.0f + 1.0f;
            if (homogenous.y <= 0) // -y
            {
                homogenous.y = homogenous.y * 2.0f + 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    if (curLeaf.subLeavesBottom.x < 0) // (-,-,-)
                        break;
                    curLeaf = octree[curLeaf.subLeavesBottom.x];
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    if (curLeaf.subLeavesBottom.y < 0)  // (+,-,-)
                        break;
                    curLeaf = octree[curLeaf.subLeavesBottom.y];
                    continue;
                }
            }
            else
            {
                homogenous.y = homogenous.y * 2.0f - 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    if (curLeaf.subLeavesBottom.z < 0) // (-,+,-)
                        break;
                    curLeaf = octree[curLeaf.subLeavesBottom.z];
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    if (curLeaf.subLeavesBottom.w < 0)  // (+,+,-)
                        break;
                    curLeaf = octree[curLeaf.subLeavesBottom.w];
                    continue;
                }
            }
        }
        else // +z
        {
            homogenous.z = homogenous.z * 2.0f - 1.0f;
            if (homogenous.y <= 0) // -y
            {
                homogenous.y = homogenous.y * 2.0f + 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    if (curLeaf.subLeavesTop.x < 0) // (-,-,+)
                        break;
                    curLeaf = octree[curLeaf.subLeavesTop.x];
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    if (curLeaf.subLeavesTop.y < 0)  // (+,-,+)
                        break;
                    curLeaf = octree[curLeaf.subLeavesTop.y];
                    continue;
                }
            }
            else
            {
                homogenous.y = homogenous.y * 2.0f - 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    if (curLeaf.subLeavesTop.z < 0) // (-,+,+)
                        break;
                    curLeaf = octree[curLeaf.subLeavesTop.z];
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    if (curLeaf.subLeavesTop.w < 0)  // (+,+,+)
                        break;
                    curLeaf = octree[curLeaf.subLeavesTop.w];
                    continue;
                }
            }
        }
    }
    
    for (int i = curLeaf.tetrahedronsStart; i < curLeaf.tetrahedronsEnd; i++)
    {
        float4 barycentric = mul(tetrahedrons[i].alphaMatrix, float4(1.0f, location));
        if (barycentric.x >= -1e-4 &&
            barycentric.y >= -1e-4 &&
            barycentric.z >= -1e-4 &&
            barycentric.w >= -1e-4)
        {
            int4 indices = tetrahedrons[i].indices;
            return meshVertices[indices.x].density * barycentric.x +
                   meshVertices[indices.y].density * barycentric.y +
                   meshVertices[indices.z].density * barycentric.z +
                   meshVertices[indices.w].density * barycentric.w;
        }
    }
    
    return -1.0f; // no tetrahedron containing current point was found
}

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    MeshVertex curVertex = meshVertices[index];
    
    if (curVertex.density < 0.0f) // ignore non-existing vertices
        return;
    
    float radius = 3.0f;
    float3 origin = 1.0f / invHalfSize - float3(radius, radius, radius) * 0.5f;
    float3 r = curVertex.position - origin;
    float source = 5.0f * deltaTime * (dot(r, r) <= (radius * radius)) * sourceEnabled;
    curVertex.nextDensity = SampleDensity(curVertex.position - curVertex.velocity * deltaTime);
    if (curVertex.nextDensity < 0)
        curVertex.nextDensity = curVertex.density;
    curVertex.nextDensity += source;
    curVertex.nextDensity *= 0.99;
    curVertex.nextDensity = max(curVertex.nextDensity, 0.0f);
    //curVertex.nextDensity = all(abs(curVertex.position - curVertex.position.x) < 0.1) * 2.0f * sourceEnabled;
    
    meshVertices[index] = curVertex;
}