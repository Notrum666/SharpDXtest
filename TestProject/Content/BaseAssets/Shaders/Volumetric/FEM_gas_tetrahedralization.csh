﻿#define THREADS_X 32
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

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

RWStructuredBuffer<OctreeNode> octree : register(u0);
RWStructuredBuffer<MeshVertex> vertices : register(u1);
RWStructuredBuffer<Tetrahedron> tetrahedrons : register(u2);
RWStructuredBuffer<int> tetrahedronsCounter : register(u3);

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

findNeighbourDeclaration(Top, ascend_top, descend_top)
findNeighbourDeclaration(Bottom, ascend_bottom, descend_bottom)
findNeighbourDeclaration(Right, ascend_right, descend_right)
findNeighbourDeclaration(Left, ascend_left, descend_left)
findNeighbourDeclaration(Forward, ascend_forward, descend_forward)
findNeighbourDeclaration(Back, ascend_back, descend_back)

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

#define depthSearch_Top depthSearch(top, subLeavesBottom.x, subLeavesBottom.y, subLeavesBottom.z, subLeavesBottom.w, \
                                         verticesBottom.x, verticesBottom.y, verticesBottom.z, verticesBottom.w)
#define depthSearch_Bottom depthSearch(bottom, subLeavesTop.x, subLeavesTop.y, subLeavesTop.z, subLeavesTop.w, \
                                               verticesTop.x, verticesTop.y, verticesTop.z, verticesTop.w)
#define depthSearch_Right depthSearch(right, subLeavesBottom.x, subLeavesBottom.z, subLeavesTop.x, subLeavesTop.z, \
                                             verticesBottom.x, verticesBottom.z, verticesTop.x, verticesTop.z)
#define depthSearch_Left depthSearch(left, subLeavesBottom.y, subLeavesBottom.w, subLeavesTop.y, subLeavesTop.w, \
                                           verticesBottom.y, verticesBottom.w, verticesTop.y, verticesTop.w)
#define depthSearch_Forward depthSearch(forward, subLeavesBottom.x, subLeavesBottom.y, subLeavesTop.x, subLeavesTop.y, \
                                                 verticesBottom.x, verticesBottom.y, verticesTop.x, verticesTop.y)
#define depthSearch_Back depthSearch(back, subLeavesBottom.z, subLeavesBottom.w, subLeavesTop.z, subLeavesTop.w, \
                                           verticesBottom.z, verticesBottom.w, verticesTop.z, verticesTop.w)

#define collectTriangles(origin, vertexA, vertexB, vertexC, vertexD, depthSearchMacro) \
if (origin == -1 || depth_##origin > 0 || (octree[origin].subLeavesBottom.x == -1)) \
{ \
    triangles[count++] = int3(curNode.vertexA, curNode.vertexB, curNode.vertexD); \
    triangles[count++] = int3(curNode.vertexA, curNode.vertexC, curNode.vertexD); \
} \
else \
    depthSearchMacro

float4x4 invert(float4x4 mat)
{
    float v00 = mat[0][0]; float v01 = mat[0][1]; float v02 = mat[0][2]; float v03 = mat[0][3];
    float v10 = mat[1][0]; float v11 = mat[1][1]; float v12 = mat[1][2]; float v13 = mat[1][3];
    float v20 = mat[2][0]; float v21 = mat[2][1]; float v22 = mat[2][2]; float v23 = mat[2][3];
    float v30 = mat[3][0]; float v31 = mat[3][1]; float v32 = mat[3][2]; float v33 = mat[3][3];
    
    float det00 = v11 * (v22 * v33 - v23 * v32) - v12 * (v21 * v33 - v23 * v31) + v13 * (v21 * v32 - v22 * v31);
    float det01 = v10 * (v22 * v33 - v23 * v32) - v12 * (v20 * v33 - v23 * v30) + v13 * (v20 * v32 - v22 * v30);
    float det02 = v10 * (v21 * v33 - v23 * v31) - v11 * (v20 * v33 - v23 * v30) + v13 * (v20 * v31 - v21 * v30);
    float det03 = v10 * (v21 * v32 - v22 * v31) - v11 * (v20 * v32 - v22 * v30) + v12 * (v20 * v31 - v21 * v30);

    float determinant = v00 * det00 - v01 * det01 + v02 * det02 - v03 * det03;

    float det10 = v01 * (v22 * v33 - v23 * v32) - v02 * (v21 * v33 - v23 * v31) + v03 * (v21 * v32 - v22 * v31);
    float det11 = v00 * (v22 * v33 - v23 * v32) - v02 * (v20 * v33 - v23 * v30) + v03 * (v20 * v32 - v22 * v30);
    float det12 = v00 * (v21 * v33 - v23 * v31) - v01 * (v20 * v33 - v23 * v30) + v03 * (v20 * v31 - v21 * v30);
    float det13 = v00 * (v21 * v32 - v22 * v31) - v01 * (v20 * v32 - v22 * v30) + v02 * (v20 * v31 - v21 * v30);
    float det20 = v01 * (v12 * v33 - v13 * v32) - v02 * (v11 * v33 - v13 * v31) + v03 * (v11 * v32 - v12 * v31);
    float det21 = v00 * (v12 * v33 - v13 * v32) - v02 * (v10 * v33 - v13 * v30) + v03 * (v10 * v32 - v12 * v30);
    float det22 = v00 * (v11 * v33 - v13 * v31) - v01 * (v10 * v33 - v13 * v30) + v03 * (v10 * v31 - v11 * v30);
    float det23 = v00 * (v11 * v32 - v12 * v31) - v01 * (v10 * v32 - v12 * v30) + v02 * (v10 * v31 - v11 * v30);
    float det30 = v01 * (v12 * v23 - v13 * v22) - v02 * (v11 * v23 - v13 * v21) + v03 * (v11 * v22 - v12 * v21);
    float det31 = v00 * (v12 * v23 - v13 * v22) - v02 * (v10 * v23 - v13 * v20) + v03 * (v10 * v22 - v12 * v20);
    float det32 = v00 * (v11 * v23 - v13 * v21) - v01 * (v10 * v23 - v13 * v20) + v03 * (v10 * v21 - v11 * v20);
    float det33 = v00 * (v11 * v22 - v12 * v21) - v01 * (v10 * v22 - v12 * v20) + v02 * (v10 * v21 - v11 * v20);

    return float4x4( det00, -det10,  det20, -det30,
                    -det01,  det11, -det21,  det31,
                     det02, -det12,  det22, -det32,
                    -det03,  det13, -det23,  det33) / determinant;
}

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	    
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    
    OctreeNode curNode = octree[index];
    
    if (curNode.tetrahedronsStart < 0 || curNode.subLeavesBottom.x >= 0) // ignore non-existing and non-leaf octree elements
        return;
    
    int depth_top, depth_bottom, depth_right, depth_left, depth_forward, depth_back;
    int top = findNeighbour_Top(index, depth_top);
    int bottom = findNeighbour_Bottom(index, depth_bottom);
    int right = findNeighbour_Right(index, depth_right);
    int left = findNeighbour_Left(index, depth_left);
    int forward = findNeighbour_Forward(index, depth_forward);
    int back = findNeighbour_Back(index, depth_back);
    
    int count = 0;
    int3 triangles[256];
    
    int stack[64];
    int pos = 0;

    collectTriangles(top, verticesTop.x, verticesTop.y, verticesTop.z, verticesTop.w, depthSearch_Top)
    collectTriangles(bottom, verticesBottom.x, verticesBottom.y, verticesBottom.z, verticesBottom.w, depthSearch_Bottom)
    collectTriangles(right, verticesBottom.y, verticesBottom.w, verticesTop.y, verticesTop.w, depthSearch_Right)
    collectTriangles(left, verticesBottom.x, verticesBottom.z, verticesTop.x, verticesTop.z, depthSearch_Left)
    collectTriangles(forward, verticesBottom.z, verticesBottom.w, verticesTop.z, verticesTop.w, depthSearch_Forward)
    collectTriangles(back, verticesBottom.x, verticesBottom.y, verticesTop.x, verticesTop.y, depthSearch_Back)
    
    int origin;
    InterlockedAdd(tetrahedronsCounter[0], count, origin);
    octree[index].tetrahedronsStart = origin;
    octree[index].tetrahedronsEnd = origin + count;
    
    int middleVertexIndex = curNode.middleVertex;
    for (int i = 0; i < count; i++)
    {
        int4 indices = int4(middleVertexIndex, triangles[i]);
        tetrahedrons[origin + i].indices = indices;
        tetrahedrons[origin + i].alphaMatrix = invert(transpose(float4x4(float4(1.0f, vertices[indices.x].position),
                                                                         float4(1.0f, vertices[indices.y].position),
                                                                         float4(1.0f, vertices[indices.z].position),
                                                                         float4(1.0f, vertices[indices.w].position))));
    }
}