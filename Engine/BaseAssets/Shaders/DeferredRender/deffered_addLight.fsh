struct vert_in
{
	float4 _ : SV_POSITION;
	float2 t : TEX;
};

cbuffer textureBuf
{
	Texture2D radianceTex;

	Texture2D worldPosTex;
	Texture2D albedoTex;
	Texture2D ambientOcclusionTex;
};

SamplerState texSampler;

float4 main(vert_in v) : SV_Target
{
	float4 worldPos = worldPosTex.Sample(texSampler, v.t);
	if (worldPos.w == 0.0f)
		discard;

	float3 albedo = albedoTex.Sample(texSampler, v.t).rgb;
	float ambientOcclusion = ambientOcclusionTex.Sample(texSampler, v.t).x;

	float3 ambient = float3(0.03f, 0.03f, 0.03f) * albedo * ambientOcclusion;
    return float4(ambient + radianceTex.Sample(texSampler, v.t).rgb, 1.0f);
}