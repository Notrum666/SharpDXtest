struct vert_in
{
	float4 _ : SV_POSITION;
	float4 v : POS;
	float2 t : TEX;
	float3 n : NORM;
	// ttw - tangent to world
	float3x3 ttw : TBN;
};

cbuffer materialBuf
{
	Texture2D albedoMap;
	Texture2D normalMap;
	Texture2D metallicMap;
	Texture2D roughnessMap;
	Texture2D ambientOcclusionMap;
	Texture2D emissiveMap;
};

struct GBuffer
{
	float4 worldPos : SV_Target0;
	float4 albedo: SV_Target1;
	float4 normal : SV_Target2;
	float metallic : SV_Target3;
	float roughness : SV_Target4;
	float ambientOcclusion : SV_Target5;
	float emissive : SV_Target6;
};

SamplerState texSampler;

[earlydepthstencil]
GBuffer main(vert_in v)
{	
	GBuffer res = (GBuffer)0;

	res.worldPos = v.v;
	res.albedo = albedoMap.Sample(texSampler, v.t);
	res.normal = float4(normalize(mul(normalMap.Sample(texSampler, v.t).xyz * 2.0f - 1.0f, v.ttw)), 0.0f);
	res.metallic = metallicMap.Sample(texSampler, v.t).x;
	res.roughness = roughnessMap.Sample(texSampler, v.t).x;
	res.ambientOcclusion = ambientOcclusionMap.Sample(texSampler, v.t).x;
	res.emissive = emissiveMap.Sample(texSampler, v.t).x;

	return res;
}
