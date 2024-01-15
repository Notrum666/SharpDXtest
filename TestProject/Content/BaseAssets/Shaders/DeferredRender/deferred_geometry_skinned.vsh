#define MAX_DIRECTIONAL_LIGHTS 4
#define MAX_DIRECTIONAL_LIGHT_CASCADES 4
#define MAX_SPOT_LIGHTS 8

struct vert_in
{
    float3 v : POS;
    float2 t : TEX;
    float3 n : NORM;
    float3 tx : TANGENT;
    int4 bones : BONE;
    float4 weights : WEIGHT;
};

struct vert_out
{
    float4 sv_pos : SV_POSITION;
    float4 v : POS;
    float2 t : TEX;
    float3 n : NORM;
	// ttw - tangent to world
    float3x3 ttw : TBN;
};

cbuffer mat
{
    float4x4 proj;
    float4x4 view;
    float4x4 model;
    float4x4 modelNorm;
};

StructuredBuffer<float4x4> gBones : register(t0);
StructuredBuffer<float4x4> gInverseTransposeBones : register(t1);

vert_out main(vert_in vert)
{
    vert_out res = (vert_out) 0;

    float4x4 boneTransform = (float4x4) 0;
    float4x4 normalTransform = (float4x4) 0;
    if (vert.weights.x > 0)
    {
        boneTransform += mul(gBones[vert.bones.x], vert.weights.x);
        normalTransform += mul(gInverseTransposeBones[vert.bones.x], vert.weights.x);
    }
    if (vert.weights.y > 0)
    {
        boneTransform += mul(gBones[vert.bones.y], vert.weights.y);
        normalTransform += mul(gInverseTransposeBones[vert.bones.y], vert.weights.y);
    }
    if (vert.weights.z > 0)
    {
        boneTransform += mul(gBones[vert.bones.z], vert.weights.z);
        normalTransform += mul(gInverseTransposeBones[vert.bones.z], vert.weights.z);
    }
    if (vert.weights.w > 0)
    {
        boneTransform += mul(gBones[vert.bones.w], vert.weights.w);
        normalTransform += mul(gInverseTransposeBones[vert.bones.w], vert.weights.w);
    }
    float4 v_world = mul(mul(float4(vert.v, 1.f), boneTransform), model);

    res.sv_pos = mul(mul(v_world, view), proj);
    res.v = v_world;
    res.t = vert.t;
    res.n = normalize(mul(mul(float4(vert.n, 0.0f), normalTransform), modelNorm).xyz);

    float3 tangent = normalize(mul(float4(vert.tx, 0.0f), modelNorm).xyz);
    float3 bitangent = cross(res.n, tangent);
    res.ttw = float3x3(tangent, bitangent, res.n);

    return res;
}
