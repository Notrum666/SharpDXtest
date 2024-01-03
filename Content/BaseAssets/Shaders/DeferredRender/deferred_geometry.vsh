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

vert_out main(vert_in vert)
{
	vert_out res = (vert_out) 0;

	float3 boneTransform = (0).xxx;

	if (vert.weights.x > 0)
		boneTransform += mul(float4(boneTransform, 1.f), gBones[vert.bones.x]).xyz * vert.weights.x;
	if (vert.weights.y > 0)
		boneTransform += mul(float4(boneTransform, 1.f), gBones[vert.bones.y]).xyz * vert.weights.y;
	if (vert.weights.z > 0)
		boneTransform += mul(float4(boneTransform, 1.f), gBones[vert.bones.z]).xyz * vert.weights.z;
	if (vert.weights.w > 0)
		boneTransform += mul(float4(boneTransform, 1.f), gBones[vert.bones.w]).xyz * vert.weights.w;

	//	float4 skinned_pos = mul(float4(vert.v, 1.0f), boneTransform);

	float4 v_world = mul(float4(vert.v + boneTransform, 1.f), model);

	res.sv_pos = mul(mul(v_world, view), proj);
	res.v = v_world;
	res.t = vert.t;
	res.n = normalize(mul(float4(vert.n, 0.0f), modelNorm).xyz);

	float3 tangent = normalize(mul(float4(vert.tx, 0.0f), modelNorm).xyz);
	float3 bitangent = cross(res.n, tangent);
	res.ttw = float3x3(tangent, bitangent, res.n);

	return res;
}
