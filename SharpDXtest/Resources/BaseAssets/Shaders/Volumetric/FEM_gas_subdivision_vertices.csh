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

cbuffer volumeData
{
    float3 volumeHalfSize;
};

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u1);
RWStructuredBuffer<MeshVertex> meshVertices : register(u2);
RWStructuredBuffer<int> freeMeshVerticesList : register(u3);
RWStructuredBuffer<int> octreeSubdivisionList : register(u4);

#define ascend_right \
[branch] if (index == parent.subLeavesBottom.x) { res = parent.subLeavesBottom.y; break; } \
[branch] if (index == parent.subLeavesBottom.z) { res = parent.subLeavesBottom.w; break; } \
[branch] if (index == parent.subLeavesTop.x)    { res = parent.subLeavesTop.y;    break; } \
[branch] if (index == parent.subLeavesTop.z)    { res = parent.subLeavesTop.w;    break; }

#define descend_right \
[branch] if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesBottom.x; \
[branch] if (index == parent.subLeavesBottom.w) res = octree[res].subLeavesBottom.z; \
[branch] if (index == parent.subLeavesTop.y)    res = octree[res].subLeavesTop.x; \
[branch] if (index == parent.subLeavesTop.w)    res = octree[res].subLeavesTop.z;

#define ascend_left \
[branch] if (index == parent.subLeavesBottom.y) { res = parent.subLeavesBottom.x; break; } \
[branch] if (index == parent.subLeavesBottom.w) { res = parent.subLeavesBottom.z; break; } \
[branch] if (index == parent.subLeavesTop.y)    { res = parent.subLeavesTop.x;    break; } \
[branch] if (index == parent.subLeavesTop.w)    { res = parent.subLeavesTop.z;    break; }

#define descend_left \
[branch] if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesBottom.y; \
[branch] if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesBottom.w; \
[branch] if (index == parent.subLeavesTop.x)    res = octree[res].subLeavesTop.y; \
[branch] if (index == parent.subLeavesTop.z)    res = octree[res].subLeavesTop.w;

#define ascend_forward \
[branch] if (index == parent.subLeavesBottom.x) { res = parent.subLeavesBottom.z; break; } \
[branch] if (index == parent.subLeavesBottom.y) { res = parent.subLeavesBottom.w; break; } \
[branch] if (index == parent.subLeavesTop.x)    { res = parent.subLeavesTop.z;    break; } \
[branch] if (index == parent.subLeavesTop.y)    { res = parent.subLeavesTop.w;    break; }

#define descend_forward \
[branch] if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesBottom.x; \
[branch] if (index == parent.subLeavesBottom.w) res = octree[res].subLeavesBottom.y; \
[branch] if (index == parent.subLeavesTop.z)    res = octree[res].subLeavesTop.x; \
[branch] if (index == parent.subLeavesTop.w)    res = octree[res].subLeavesTop.y;

#define ascend_back \
[branch] if (index == parent.subLeavesBottom.z) { res = parent.subLeavesBottom.x; break; } \
[branch] if (index == parent.subLeavesBottom.w) { res = parent.subLeavesBottom.y; break; } \
[branch] if (index == parent.subLeavesTop.z)    { res = parent.subLeavesTop.x;    break; } \
[branch] if (index == parent.subLeavesTop.w)    { res = parent.subLeavesTop.y;    break; }

#define descend_back \
[branch] if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesBottom.z; \
[branch] if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesBottom.w; \
[branch] if (index == parent.subLeavesTop.x)    res = octree[res].subLeavesTop.z; \
[branch] if (index == parent.subLeavesTop.y)    res = octree[res].subLeavesTop.w;

#define ascend_top \
[branch] if (index == parent.subLeavesBottom.x) { res = parent.subLeavesTop.x; break; } \
[branch] if (index == parent.subLeavesBottom.y) { res = parent.subLeavesTop.y; break; } \
[branch] if (index == parent.subLeavesBottom.z) { res = parent.subLeavesTop.z; break; } \
[branch] if (index == parent.subLeavesBottom.w) { res = parent.subLeavesTop.w; break; }

#define descend_top \
[branch] if (index == parent.subLeavesTop.x) res = octree[res].subLeavesBottom.x; \
[branch] if (index == parent.subLeavesTop.y) res = octree[res].subLeavesBottom.y; \
[branch] if (index == parent.subLeavesTop.z) res = octree[res].subLeavesBottom.z; \
[branch] if (index == parent.subLeavesTop.w) res = octree[res].subLeavesBottom.w;

#define ascend_bottom \
[branch] if (index == parent.subLeavesTop.x) { res = parent.subLeavesBottom.x; break; } \
[branch] if (index == parent.subLeavesTop.y) { res = parent.subLeavesBottom.y; break; } \
[branch] if (index == parent.subLeavesTop.z) { res = parent.subLeavesBottom.z; break; } \
[branch] if (index == parent.subLeavesTop.w) { res = parent.subLeavesBottom.w; break; }

#define descend_bottom \
[branch] if (index == parent.subLeavesBottom.x) res = octree[res].subLeavesTop.x; \
[branch] if (index == parent.subLeavesBottom.y) res = octree[res].subLeavesTop.y; \
[branch] if (index == parent.subLeavesBottom.z) res = octree[res].subLeavesTop.z; \
[branch] if (index == parent.subLeavesBottom.w) res = octree[res].subLeavesTop.w;

#define ascend_corner(origin1, origin2, opposite1, opposite2) \
[branch] if (index == parent.origin1) { res = parent.opposite1; break; } \
[branch] if (index == parent.origin2) { res = parent.opposite2; break; }

#define descend_corner(origin1, origin2, opposite1, opposite2) \
[branch] if (index == parent.opposite1) res = octree[res].origin1; \
[branch] if (index == parent.opposite2) res = octree[res].origin2;


#define  ascend_bottomLeft  ascend_corner(subLeavesTop.y, subLeavesTop.w, subLeavesBottom.x, subLeavesBottom.z)
#define descend_bottomLeft descend_corner(subLeavesTop.y, subLeavesTop.w, subLeavesBottom.x, subLeavesBottom.z)

#define  ascend_bottomBack  ascend_corner(subLeavesTop.z, subLeavesTop.w, subLeavesBottom.x, subLeavesBottom.y)
#define descend_bottomBack descend_corner(subLeavesTop.z, subLeavesTop.w, subLeavesBottom.x, subLeavesBottom.y)

#define  ascend_bottomRight  ascend_corner(subLeavesTop.x, subLeavesTop.z, subLeavesBottom.y, subLeavesBottom.w)
#define descend_bottomRight descend_corner(subLeavesTop.x, subLeavesTop.z, subLeavesBottom.y, subLeavesBottom.w)

#define  ascend_bottomForward  ascend_corner(subLeavesTop.x, subLeavesTop.y, subLeavesBottom.z, subLeavesBottom.w)
#define descend_bottomForward descend_corner(subLeavesTop.x, subLeavesTop.y, subLeavesBottom.z, subLeavesBottom.w)


#define  ascend_topLeft  ascend_corner(subLeavesBottom.y, subLeavesBottom.w, subLeavesTop.x, subLeavesTop.z)
#define descend_topLeft descend_corner(subLeavesBottom.y, subLeavesBottom.w, subLeavesTop.x, subLeavesTop.z)

#define  ascend_topBack  ascend_corner(subLeavesBottom.z, subLeavesBottom.w, subLeavesTop.x, subLeavesTop.y)
#define descend_topBack descend_corner(subLeavesBottom.z, subLeavesBottom.w, subLeavesTop.x, subLeavesTop.y)

#define  ascend_topRight  ascend_corner(subLeavesBottom.x, subLeavesBottom.z, subLeavesTop.y, subLeavesTop.w)
#define descend_topRight descend_corner(subLeavesBottom.x, subLeavesBottom.z, subLeavesTop.y, subLeavesTop.w)

#define  ascend_topForward  ascend_corner(subLeavesBottom.x, subLeavesBottom.y, subLeavesTop.z, subLeavesTop.w)
#define descend_topForward descend_corner(subLeavesBottom.x, subLeavesBottom.y, subLeavesTop.z, subLeavesTop.w)


#define  ascend_rightForward  ascend_corner(subLeavesBottom.x, subLeavesTop.x, subLeavesBottom.w, subLeavesTop.w)
#define descend_rightForward descend_corner(subLeavesBottom.x, subLeavesTop.x, subLeavesBottom.w, subLeavesTop.w)

#define  ascend_rightBack  ascend_corner(subLeavesBottom.z, subLeavesTop.z, subLeavesBottom.y, subLeavesTop.y)
#define descend_rightBack descend_corner(subLeavesBottom.z, subLeavesTop.z, subLeavesBottom.y, subLeavesTop.y)

#define  ascend_leftForward  ascend_corner(subLeavesBottom.y, subLeavesTop.y, subLeavesBottom.z, subLeavesTop.z)
#define descend_leftForward descend_corner(subLeavesBottom.y, subLeavesTop.y, subLeavesBottom.z, subLeavesTop.z)

#define  ascend_leftBack  ascend_corner(subLeavesBottom.w, subLeavesTop.w, subLeavesBottom.x, subLeavesTop.x)
#define descend_leftBack descend_corner(subLeavesBottom.w, subLeavesTop.w, subLeavesBottom.x, subLeavesTop.x)


void getStack(int octant, out int stack[32])
{
    int parentIndex = octree[octant].parent;
    int depth = 0;
    while (parentIndex >= 0)
    {      
        stack[depth] = octant;
        octant = parentIndex;
        parentIndex = octree[octant].parent;
        
        depth++;
    }
}

#define findNeighbourUniqueStackDeclaration(name, ascention, descention) \
int findNeighbourUniqueStack_##name(int index, out int depth) \
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
    [branch] if (res == -1) \
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

#define findNeighbourDeclaration(name, ascention, descention) \
int findNeighbour_##name(int index, in int stack[32], out int depth) \
{ \
    int parentIndex = stack[0]; \
    int res = -1; \
    OctreeNode parent; \
    depth = 0; \
    while (parentIndex >= 0) \
    { \
        parent = octree[parentIndex]; \
        ascention \
         \
        index = parentIndex; \
        depth++; \
        parentIndex = stack[depth]; \
    } \
     \
    [branch] if (res == -1) \
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

#define findNeighbourShortDeclaration(name) findNeighbourDeclaration(name, ascend_##name, descend_##name)
#define findNeighbourUniqueStackShortDeclaration(name) findNeighbourUniqueStackDeclaration(name, ascend_##name, descend_##name)

findNeighbourShortDeclaration(top)
findNeighbourShortDeclaration(bottom)
findNeighbourShortDeclaration(right)
findNeighbourShortDeclaration(left)
findNeighbourShortDeclaration(forward)
findNeighbourShortDeclaration(back)

findNeighbourUniqueStackShortDeclaration(top)
findNeighbourUniqueStackShortDeclaration(bottom)
findNeighbourUniqueStackShortDeclaration(right)
findNeighbourUniqueStackShortDeclaration(left)
findNeighbourUniqueStackShortDeclaration(forward)
findNeighbourUniqueStackShortDeclaration(back)

#define descendCornerBasedOnStackDeclaration(name) \
int descendCornerBasedOnStack_##name(int target, in int stack[32], inout int depth) \
{ \
    int res = target; \
    OctreeNode parent; \
    depth--; \
    int index = stack[depth]; \
     \
    while (depth > 0 && octree[res].subLeavesBottom.x >= 0) \
    { \
        parent = octree[index]; \
        depth--; \
        index = stack[depth]; \
        descend_##name \
    } \
     \
    return res; \
}

descendCornerBasedOnStackDeclaration(bottomLeft)
descendCornerBasedOnStackDeclaration(bottomBack)
descendCornerBasedOnStackDeclaration(bottomRight)
descendCornerBasedOnStackDeclaration(bottomForward)
descendCornerBasedOnStackDeclaration(topLeft)
descendCornerBasedOnStackDeclaration(topBack)
descendCornerBasedOnStackDeclaration(topRight)
descendCornerBasedOnStackDeclaration(topForward)
descendCornerBasedOnStackDeclaration(rightForward)
descendCornerBasedOnStackDeclaration(rightBack)
descendCornerBasedOnStackDeclaration(leftForward)
descendCornerBasedOnStackDeclaration(leftBack)

float SampleDensityAndVelocity(float3 location, out float3 velocity)
{
    float3 homogenous = location / volumeHalfSize; // from -1 to 1 within volume
    OctreeNode curLeaf = octree[0];
    while (true)
    {
        if (curLeaf.tetrahedronsEnd - curLeaf.tetrahedronsStart > 0)
            break;
        if (homogenous.z <= 0) // -z
        {
            homogenous.z = homogenous.z * 2.0f + 1.0f;
            if (homogenous.y <= 0) // -y
            {
                homogenous.y = homogenous.y * 2.0f + 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.x]; // (-,-,-)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.y]; // (+,-,-)
                    continue;
                }
            }
            else
            {
                homogenous.y = homogenous.y * 2.0f - 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.z]; // (-,+,-)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.w]; // (+,+,-)
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
                    curLeaf = octree[curLeaf.subLeavesTop.x]; // (-,-,+)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesTop.y]; // (+,-,+)
                    continue;
                }
            }
            else
            {
                homogenous.y = homogenous.y * 2.0f - 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    curLeaf = octree[curLeaf.subLeavesTop.z]; // (-,+,+)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesTop.w]; // (+,+,+)
                    continue;
                }
            }
        }
    }
    
    for (int i = curLeaf.tetrahedronsStart; i < curLeaf.tetrahedronsEnd; i++)
    {
        float4 barycentric = mul(tetrahedrons[i].alphaMatrix, float4(1.0f, location));
        if (barycentric.x >= -1e-6 &&
            barycentric.y >= -1e-6 &&
            barycentric.z >= -1e-6 &&
            barycentric.w >= -1e-6)
        {
            int4 indices = tetrahedrons[i].indices;
            velocity = meshVertices[indices.x].velocity * barycentric.x +
                       meshVertices[indices.y].velocity * barycentric.y +
                       meshVertices[indices.z].velocity * barycentric.z +
                       meshVertices[indices.w].velocity * barycentric.w;
            return meshVertices[indices.x].density * barycentric.x +
                   meshVertices[indices.y].density * barycentric.y +
                   meshVertices[indices.z].density * barycentric.z +
                   meshVertices[indices.w].density * barycentric.w;
        }
    }
    
    return 0.0f; // no tetrahedron containing current point was found, maybe as a result of floating point errors
}

MeshVertex createVertex(float3 position)
{
    MeshVertex vertex = (MeshVertex) 0;
    vertex.position = position;
    //float3 homogenous = position / volumeHalfSize;
    //float3 r = homogenous - float3(1.0f, 1.0f, 1.0f);
    //vertex.density = dot(r, r) <= 1.0f ? 10.0f : 0.0f; //pow(homogenous.x * 0.5f + 0.5f, 4.0f);
    vertex.density = max(SampleDensityAndVelocity(position, vertex.velocity), 0.0f);
    return vertex;
}

float3 ascendPoint(int originOctant, float3 p)
{
    int parentIndex = octree[originOctant].parent;
    while (parentIndex != -1)
    {
        p *= 0.5f;
        
        OctreeNode parent = octree[parentIndex];
        [branch] if (originOctant == parent.subLeavesBottom.y)
            p += float3(0.5f, 0.0f, 0.0f);
        [branch] if (originOctant == parent.subLeavesBottom.z)
            p += float3(0.0f, 0.5f, 0.0f);
        [branch] if (originOctant == parent.subLeavesBottom.w)
            p += float3(0.5f, 0.5f, 0.0f);
        [branch] if (originOctant == parent.subLeavesTop.x)
            p += float3(0.0f, 0.0f, 0.5f);
        [branch] if (originOctant == parent.subLeavesTop.y)
            p += float3(0.5f, 0.0f, 0.5f);
        [branch] if (originOctant == parent.subLeavesTop.z)
            p += float3(0.0f, 0.5f, 0.5f);
        [branch] if (originOctant == parent.subLeavesTop.w)
            p += float3(0.5f, 0.5f, 0.5f);
        
        originOctant = parentIndex;
        parentIndex = parent.parent;
    }
    
    return p;
}

float3 transformNPointToLocalSpace(float3 nPoint)
{
    return (nPoint * 2.0f - 1.0f) * volumeHalfSize;
}

int generateVertex(int octant, float3 homogenousLocation)
{
    int index = freeMeshVerticesList[freeMeshVerticesList.DecrementCounter()];
    meshVertices[index] = createVertex(transformNPointToLocalSpace(ascendPoint(octant, homogenousLocation)));
    return index;
}

#define fetchOrCreateVertexLowPriority(direction, neighbourOctantLocation, neighbourVertexLocation, homogenousVertexLocation) \
[branch] if (direction == -1 || direction##_depth > 0 || direction##Node.subLeavesBottom.x == -1) \
    vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
else \
    vertexIndex = octree[direction##Node.neighbourOctantLocation].neighbourVertexLocation;

#define fetchOrCreateVertexHighPriority(direction, neighbourOctantLocation, neighbourVertexLocation, homogenousVertexLocation) \
[branch] if (direction == -1 || direction##_depth > 0 || direction##Node.subLeavesBottom.x == -1) \
    vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
else \
{ \
    int neighbouringVertex = octree[direction##Node.neighbourOctantLocation].neighbourVertexLocation; \
    [branch] if (neighbouringVertex == -1) \
        vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
    else \
        vertexIndex = neighbouringVertex; \
}

#define fetchOrCreateEdgeVertexLowPriority(side1, side1octant, side1vertex, \
                                           side2, side2octant, side2vertex, \
                                           corner, cornerOctant, cornerVertex, \
                                           testPoint, AABB, direction, sign, \
                                           homogenousVertexLocation) \
int corner = -1; \
int corner##_depth; \
bool corner##Viable = false; \
OctreeNode corner##Node = (OctreeNode) 0; \
[branch] \
if (side1 != -1) \
{ \
    if (side1##_depth <= 0 || ascendPoint(side1, testPoint).direction sign AABB.direction) \
    { \
        corner = findNeighbourUniqueStack_##side2(side1, corner##_depth); \
        corner##Viable = corner != -1 && corner##_depth <= 0; \
        if (corner##Viable && side1##_depth > 0) \
        { \
            corner##_depth += side1##_depth; \
            corner = descendCornerBasedOnStack_##corner(corner, stack, corner##_depth); \
            corner##Viable = corner##Viable && corner##_depth <= 0; \
        } \
    } \
} \
 \
if (corner##Viable) \
{ \
    corner##Node = octree[corner]; \
    corner##Viable = corner##Viable && corner##Node.subLeavesBottom.x != -1; \
} \
 \
[branch] \
if (corner##Viable) \
{ \
    corner##Node = octree[corner]; \
    corner##Viable = corner##Viable && corner##Node.subLeavesBottom.x != -1; \
} \
 \
[branch] if (!side1##Viable && !side2##Viable && !corner##Viable) \
{ \
    vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
} \
else \
{ \
    vertexIndex = -1; \
    [branch] if (side1##Viable) \
        vertexIndex = max(vertexIndex, octree[side1##Node.side1octant].side1vertex); \
    [branch] if (side2##Viable) \
        vertexIndex = max(vertexIndex, octree[side2##Node.side2octant].side2vertex); \
    [branch] if (corner##Viable) \
        vertexIndex = max(vertexIndex, octree[corner##Node.cornerOctant].cornerVertex); \
}

#define fetchOrCreateEdgeVertexMediumPriority(highPrioritySide, highPrioritySideoctant, highPrioritySidevertex, \
                                              lowPrioritySide, lowPrioritySideoctant, lowPrioritySidevertex, \
                                              corner, cornerOctant, cornerVertex, cornerPriority, \
                                              testPoint, AABB, direction, sign, \
                                              homogenousVertexLocation) \
int corner = -1; \
int corner##_depth; \
bool corner##Viable = false; \
OctreeNode corner##Node = (OctreeNode) 0; \
[branch] \
if (highPrioritySide != -1) \
{ \
    if (highPrioritySide##_depth <= 0 || ascendPoint(highPrioritySide, testPoint).direction sign AABB.direction) \
    { \
        corner = findNeighbourUniqueStack_##lowPrioritySide(highPrioritySide, corner##_depth); \
        corner##Viable = corner != -1 && corner##_depth <= 0; \
        if (corner##Viable && highPrioritySide##_depth > 0) \
        { \
            corner##_depth += highPrioritySide##_depth; \
            corner = descendCornerBasedOnStack_##corner(corner, stack, corner##_depth); \
            corner##Viable = corner##Viable && corner##_depth <= 0; \
        } \
    } \
} \
 \
if (corner##Viable) \
{ \
    corner##Node = octree[corner]; \
    corner##Viable = corner##Viable && corner##Node.subLeavesBottom.x != -1; \
} \
 \
[branch] if (!highPrioritySide##Viable && !lowPrioritySide##Viable && !corner##Viable) \
{ \
    vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
} \
else \
{ \
    vertexIndex = -1; \
    [branch] if (highPrioritySide##Viable) \
        vertexIndex = max(vertexIndex, octree[highPrioritySide##Node.highPrioritySideoctant].highPrioritySidevertex); \
    [branch] if (lowPrioritySide##Viable) \
        vertexIndex = max(vertexIndex, octree[lowPrioritySide##Node.lowPrioritySideoctant].lowPrioritySidevertex); \
    [branch] if (corner##Viable) \
        vertexIndex = max(vertexIndex, octree[corner##Node.cornerOctant].cornerVertex); \
    [branch] if (vertexIndex == -1 && !highPrioritySide##Viable) \
    { \
        [branch] if (!corner##Viable || cornerPriority) \
            vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
    } \
}

#define fetchOrCreateEdgeVertexHighPriority(side1, side1octant, side1vertex, \
                                            side2, side2octant, side2vertex, \
                                            corner, cornerOctant, cornerVertex, \
                                            testPoint, AABB, direction, sign, \
                                            homogenousVertexLocation) \
int corner = -1; \
int corner##_depth; \
bool corner##Viable = false; \
OctreeNode corner##Node = (OctreeNode) 0; \
[branch] \
if (side1 != -1) \
{ \
    if (side1##_depth <= 0 || ascendPoint(side1, testPoint).direction sign AABB.direction) \
    { \
        corner = findNeighbourUniqueStack_##side2(side1, corner##_depth); \
        corner##Viable = corner != -1 && corner##_depth <= 0; \
        if (corner##Viable && side1##_depth > 0) \
        { \
            corner##_depth += side1##_depth; \
            corner = descendCornerBasedOnStack_##corner(corner, stack, corner##_depth); \
            corner##Viable = corner##Viable && corner##_depth <= 0; \
        } \
    } \
} \
 \
if (corner##Viable) \
{ \
    corner##Node = octree[corner]; \
    corner##Viable = corner##Viable && corner##Node.subLeavesBottom.x != -1; \
} \
 \
[branch] if (!side1##Viable && !side2##Viable && !corner##Viable) \
{ \
    vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
} \
else \
{ \
    vertexIndex = -1; \
    [branch] if (side1##Viable) \
        vertexIndex = max(vertexIndex, octree[side1##Node.side1octant].side1vertex); \
    [branch] if (side2##Viable) \
        vertexIndex = max(vertexIndex, octree[side2##Node.side2octant].side2vertex); \
    [branch] if (corner##Viable) \
        vertexIndex = max(vertexIndex, octree[corner##Node.cornerOctant].cornerVertex); \
    [branch] if (vertexIndex == -1) \
        vertexIndex = generateVertex(curOctantIndex, homogenousVertexLocation); \
}

#define gatherNeighbourData(side) \
side = findNeighbour_##side(curOctantIndex, stack, side##_depth); \
side##Viable = side != -1 && side##_depth <= 0; \
[branch] \
if (side##Viable) \
{ \
    side##Node = octree[side]; \
    side##Viable = side##Viable && side##Node.subLeavesBottom.x != -1; \
}

#define FUCK_SIDES
#define FUCK_EDGES

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    int curOctantIndex = octreeSubdivisionList[index];
    [branch] if (curOctantIndex == -1)
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
    
    // generating middle vertices for new octants
    
    if (bottomX.middleVertex == -1)
    {
        bottomX.middleVertex = generateVertex(curOctant.subLeavesBottom.x, float3(0.5f, 0.5f, 0.5f));
        bottomY.middleVertex = generateVertex(curOctant.subLeavesBottom.y, float3(0.5f, 0.5f, 0.5f));
        bottomZ.middleVertex = generateVertex(curOctant.subLeavesBottom.z, float3(0.5f, 0.5f, 0.5f));
        bottomW.middleVertex = generateVertex(curOctant.subLeavesBottom.w, float3(0.5f, 0.5f, 0.5f));
        topX.middleVertex = generateVertex(curOctant.subLeavesTop.x, float3(0.5f, 0.5f, 0.5f));
        topY.middleVertex = generateVertex(curOctant.subLeavesTop.y, float3(0.5f, 0.5f, 0.5f));
        topZ.middleVertex = generateVertex(curOctant.subLeavesTop.z, float3(0.5f, 0.5f, 0.5f));
        topW.middleVertex = generateVertex(curOctant.subLeavesTop.w, float3(0.5f, 0.5f, 0.5f));
    }
    
    float3 AABB_from = ascendPoint(curOctantIndex, float3(0.0f, 0.0f, 0.0f));
    float3 AABB_to = ascendPoint(curOctantIndex, float3(1.0f, 1.0f, 1.0f));
    
    int stack[32];
    getStack(curOctantIndex, stack);
    
    // fetching neighbours with common face
    int right, left, forward, back, top, bottom;
    int right_depth, left_depth, forward_depth, back_depth, top_depth, bottom_depth;
    OctreeNode rightNode, leftNode, forwardNode, backNode, topNode, bottomNode;
    bool rightViable, leftViable, forwardViable, backViable, topViable, bottomViable;
    gatherNeighbourData(right)
    gatherNeighbourData(left)
    gatherNeighbourData(forward)
    gatherNeighbourData(back)
    gatherNeighbourData(top)
    gatherNeighbourData(bottom)
    
    int vertexIndex;
    
    // fetching/creating vertices for the faces
    [branch] if (bottomY.verticesTop.w == -1)
    {
        #ifndef FUCK_SIDES
        fetchOrCreateVertexLowPriority(right, subLeavesBottom.x, verticesTop.z, float3(1.0f, 0.5f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(1.0f, 0.5f, 0.5f));
        #endif
        bottomY.verticesTop.w = vertexIndex;
        bottomW.verticesTop.y = vertexIndex;
        topY.verticesBottom.w = vertexIndex;
        topW.verticesBottom.y = vertexIndex;
    }
    
    [branch] if (bottomX.verticesTop.z == -1)
    {
        #ifndef FUCK_SIDES
        fetchOrCreateVertexHighPriority(left, subLeavesBottom.y, verticesTop.w, float3(0.0f, 0.5f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.0f, 0.5f, 0.5f));
        #endif
        bottomX.verticesTop.z = vertexIndex;
        bottomZ.verticesTop.x = vertexIndex;
        topX.verticesBottom.z = vertexIndex;
        topZ.verticesBottom.x = vertexIndex;
    }
    
    [branch] if (bottomZ.verticesTop.w == -1)
    {
        #ifndef FUCK_SIDES
        fetchOrCreateVertexLowPriority(forward, subLeavesBottom.x, verticesTop.y, float3(0.5f, 1.0f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 1.0f, 0.5f));
        #endif
        bottomZ.verticesTop.w = vertexIndex;
        bottomW.verticesTop.z = vertexIndex;
        topZ.verticesBottom.w = vertexIndex;
        topW.verticesBottom.z = vertexIndex;
    }
    
    [branch] if (bottomX.verticesTop.y == -1)
    {
        #ifndef FUCK_SIDES
        fetchOrCreateVertexHighPriority(back, subLeavesBottom.z, verticesTop.w, float3(0.5f, 0.0f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 0.0f, 0.5f));
        #endif
        bottomX.verticesTop.y = vertexIndex;
        bottomY.verticesTop.x = vertexIndex;
        topX.verticesBottom.y = vertexIndex;
        topY.verticesBottom.x = vertexIndex;
    }
    
    [branch] if (topX.verticesTop.w == -1)
    {
        #ifndef FUCK_SIDES
        fetchOrCreateVertexLowPriority(top, subLeavesBottom.x, verticesBottom.w, float3(0.5f, 0.5f, 1.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 0.5f, 1.0f));
        #endif
        topX.verticesTop.w = vertexIndex;
        topY.verticesTop.z = vertexIndex;
        topZ.verticesTop.y = vertexIndex;
        topW.verticesTop.x = vertexIndex;
    }
    
    [branch] if (bottomX.verticesBottom.w == -1)
    {
        #ifndef FUCK_SIDES
        fetchOrCreateVertexHighPriority(bottom, subLeavesTop.x, verticesTop.w, float3(0.5f, 0.5f, 0.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 0.5f, 0.0f));
        #endif
        bottomX.verticesBottom.w = vertexIndex;
        bottomY.verticesBottom.z = vertexIndex;
        bottomZ.verticesBottom.y = vertexIndex;
        bottomW.verticesBottom.x = vertexIndex;
    }
    
    // creating/fetching vertices for the edges
    // high priority
    [branch] if (bottomX.verticesBottom.z == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexHighPriority(bottom, subLeavesTop.x, verticesTop.z,
                                            left, subLeavesBottom.y, verticesBottom.w,
                                            bottomLeft, subLeavesTop.y, verticesTop.w,
                                            float3(0.0f, 0.0f, 0.0f), AABB_from, x, >=,
                                            float3(0.0f, 0.5f, 0.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.0f, 0.5f, 0.0f));
        #endif
        bottomX.verticesBottom.z = vertexIndex;
        bottomZ.verticesBottom.x = vertexIndex;
    }
    
    [branch] if (bottomX.verticesBottom.y == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexHighPriority(bottom, subLeavesTop.x, verticesTop.y,
                                            back, subLeavesBottom.z, verticesBottom.w,
                                            bottomBack, subLeavesTop.z, verticesTop.w,
                                            float3(0.0f, 0.0f, 0.0f), AABB_from, y, >=,
                                            float3(0.5f, 0.0f, 0.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 0.0f, 0.0f));
        #endif
        bottomX.verticesBottom.y = vertexIndex;
        bottomY.verticesBottom.x = vertexIndex;
    }
    
    [branch] if (bottomX.verticesTop.x == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexHighPriority(left, subLeavesBottom.y, verticesTop.y,
                                            back, subLeavesBottom.z, verticesTop.z,
                                            leftBack, subLeavesBottom.w, verticesTop.w,
                                            float3(0.0f, 0.0f, 0.0f), AABB_from, y, >=,
                                            float3(0.0f, 0.0f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.0f, 0.0f, 0.5f));
        #endif
        bottomX.verticesTop.x = vertexIndex;
        topX.verticesBottom.x = vertexIndex;
    }
    
    // low priority
    [branch] if (bottomW.verticesTop.w == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexLowPriority(right, subLeavesBottom.z, verticesTop.z,
                                           forward, subLeavesBottom.y, verticesTop.y,
                                           rightForward, subLeavesBottom.x, verticesTop.x,
                                           float3(1.0f, 1.0f, 1.0f), AABB_to, y, <=,
                                           float3(1.0f, 1.0f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(1.0f, 1.0f, 0.5f));
        #endif
        bottomW.verticesTop.w = vertexIndex;
        topW.verticesBottom.w = vertexIndex;
    }
    
    [branch] if (topY.verticesTop.w == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexLowPriority(right, subLeavesTop.x, verticesTop.z,
                                           top, subLeavesBottom.y, verticesBottom.w,
                                           topRight, subLeavesBottom.x, verticesBottom.z,
                                           float3(1.0f, 1.0f, 1.0f), AABB_to, z, <=,
                                           float3(1.0f, 0.5f, 1.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(1.0f, 0.5f, 1.0f));
        #endif
        topY.verticesTop.w = vertexIndex;
        topW.verticesTop.y = vertexIndex;
    }
    
    [branch] if (topZ.verticesTop.w == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexLowPriority(forward, subLeavesTop.x, verticesTop.y,
                                           top, subLeavesBottom.z, verticesBottom.w,
                                           topForward, subLeavesBottom.x, verticesBottom.y,
                                           float3(1.0f, 1.0f, 1.0f), AABB_to, z, <=,
                                           float3(0.5f, 1.0f, 1.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 1.0f, 1.0f));
        #endif
        topZ.verticesTop.w = vertexIndex;
        topW.verticesTop.z = vertexIndex;
    }
    
    // medium priority
    [branch] if (topX.verticesTop.y == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexMediumPriority(top, subLeavesBottom.x, verticesBottom.y,
                                              back, subLeavesTop.z, verticesTop.w,
                                              topBack, subLeavesBottom.z, verticesBottom.w, true,
                                              float3(0.0f, 0.0f, 0.0f), AABB_from, y, >=,
                                              float3(0.5f, 0.0f, 1.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 0.0f, 1.0f));
        #endif
        topX.verticesTop.y = vertexIndex;
        topY.verticesTop.x = vertexIndex;
    }
    
    [branch] if (bottomZ.verticesBottom.w == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexMediumPriority(forward, subLeavesBottom.x, verticesBottom.y,
                                              bottom, subLeavesTop.z, verticesTop.w,
                                              bottomForward, subLeavesTop.x, verticesTop.y, false,
                                              float3(0.0f, 0.0f, 0.0f), AABB_from, z, >=,
                                              float3(0.5f, 1.0f, 0.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.5f, 1.0f, 0.0f));
        #endif
        bottomZ.verticesBottom.w = vertexIndex;
        bottomW.verticesBottom.z = vertexIndex;
    }
    
    [branch] if (topX.verticesTop.z == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexMediumPriority(top, subLeavesBottom.x, verticesBottom.z,
                                              left, subLeavesTop.y, verticesTop.w,
                                              topLeft, subLeavesBottom.y, verticesBottom.w, true,
                                              float3(0.0f, 0.0f, 0.0f), AABB_from, y, >=,
                                              float3(0.0f, 0.5f, 1.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.0f, 0.5f, 1.0f));
        #endif
        topX.verticesTop.z = vertexIndex;
        topZ.verticesTop.x = vertexIndex;
    }
    
    [branch] if (bottomY.verticesBottom.w == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexMediumPriority(right, subLeavesBottom.x, verticesBottom.z,
                                              bottom, subLeavesTop.y, verticesTop.w,
                                              bottomRight, subLeavesTop.x, verticesTop.z, false,
                                              float3(0.0f, 0.0f, 0.0f), AABB_from, z, >=,
                                              float3(1.0f, 0.5f, 0.0f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(1.0f, 0.5f, 0.0f));
        #endif
        bottomY.verticesBottom.w = vertexIndex;
        bottomW.verticesBottom.y = vertexIndex;
    }
    
    [branch] if (bottomY.verticesTop.y == -1)
    {
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexMediumPriority(right, subLeavesBottom.x, verticesTop.x,
                                              back, subLeavesBottom.w, verticesTop.w,
                                              rightBack, subLeavesBottom.z, verticesTop.z, true,
                                              float3(0.0f, 0.0f, 0.0f), AABB_from, y, >=,
                                              float3(1.0f, 0.0f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(1.0f, 0.0f, 0.5f));
        #endif
        bottomY.verticesTop.y = vertexIndex;
        topY.verticesBottom.y = vertexIndex;
    }
    
    [branch] if (bottomZ.verticesTop.z == -1)
    {
        //int leftForward = -1;
        //int leftForward_depth;
        //bool leftForwardViable = false;
        //OctreeNode leftForwardNode = (OctreeNode) 0;
        //[branch]
        //if (forward != -1)
        //{
        //    [branch]
        //    if (forward_depth <= 0 || ascendPoint(forward, float3(1.0f, 1.0f, 1.0f)).y <= AABB_to.y)
        //    {
        //        leftForward = findNeighbourUniqueStack_left(forward, leftForward_depth);
        //        leftForwardViable = leftForward != -1 && leftForward_depth <= 0;
        //        [branch]
        //        if (leftForwardViable && forward_depth > 0)
        //        {
        //            leftForward_depth += forward_depth;
        //            leftForward = descendCornerBasedOnStack_leftForward(leftForward, stack, leftForward_depth);
        //            leftForwardViable = leftForwardViable && leftForward_depth <= 0;
        //        }
        //    }
        //}
        //
        //[branch] if (!forwardViable && !leftViable && !leftForwardViable)
        //{
        //    vertexIndex = generateVertex(curOctantIndex, float3(0.0f, 1.0f, 0.5f));
        //}
        //else
        //{
        //    vertexIndex = -1;
        //    [branch] if (forwardViable)
        //        vertexIndex = max(vertexIndex, octree[forwardNode.subLeavesBottom.x].verticesTop.x);
        //    [branch] if (leftViable)
        //        vertexIndex = max(vertexIndex, octree[leftNode.subLeavesBottom.w].verticesTop.w);
        //    [branch] if (leftForwardViable)
        //        vertexIndex = max(vertexIndex, octree[leftForwardNode.subLeavesBottom.y].verticesTop.y);
        //    [branch] if (vertexIndex == -1 && !forwardViable)
        //    {
        //        [branch] if (!leftForwardViable)
        //            vertexIndex = generateVertex(curOctantIndex, float3(0.0f, 1.0f, 0.5f));
        //    }
        //}
        #ifndef FUCK_EDGES
        fetchOrCreateEdgeVertexMediumPriority(forward, subLeavesBottom.x, verticesTop.x,
                                              left, subLeavesBottom.w, verticesTop.w,
                                              leftForward, subLeavesBottom.y, verticesTop.y, false,
                                              float3(1.0f, 1.0f, 1.0f), AABB_to, y, <=,
                                              float3(0.0f, 1.0f, 0.5f))
        #else
        vertexIndex = generateVertex(curOctantIndex, float3(0.0f, 1.0f, 0.5f));
        #endif
        bottomZ.verticesTop.z = vertexIndex;
        topZ.verticesBottom.z = vertexIndex;
    }
    
    octree[curOctant.subLeavesBottom.x] = bottomX;
    octree[curOctant.subLeavesBottom.y] = bottomY;
    octree[curOctant.subLeavesBottom.z] = bottomZ;
    octree[curOctant.subLeavesBottom.w] = bottomW;
    octree[curOctant.subLeavesTop.x] = topX;
    octree[curOctant.subLeavesTop.y] = topY;
    octree[curOctant.subLeavesTop.z] = topZ;
    octree[curOctant.subLeavesTop.w] = topW;
}