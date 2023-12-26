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

	float4x4 boneTransform = float4x4(1, 0, 0, 0,
									  0, 1, 0, 0,
									  0, 0, 1, 0,
									  0, 0, 0, 1);
	if (vert.weights.x) {
		boneTransform = gBones[vert.bones.x] * vert.weights.x;
		boneTransform += gBones[vert.bones.y] * vert.weights.y;
		boneTransform += gBones[vert.bones.z] * vert.weights.z;
		boneTransform += gBones[vert.bones.w] * vert.weights.w;
	}

	float4 skinned_pos = mul(float4(vert.v, 1.0f), boneTransform);

	float4 v_world = mul(skinned_pos, model);

	res.sv_pos = mul(mul(v_world, view), proj);
	res.v = v_world;
	res.t = vert.t;
	res.n = normalize(mul(float4(vert.n, 0.0f), modelNorm).xyz);

	float3 tangent = normalize(mul(float4(vert.tx, 0.0f), modelNorm).xyz);
	float3 bitangent = cross(res.n, tangent);
	res.ttw = float3x3(tangent, bitangent, res.n);

	return res;
}
