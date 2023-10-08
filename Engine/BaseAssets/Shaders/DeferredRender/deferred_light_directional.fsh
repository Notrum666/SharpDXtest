#define PI 3.14159265f
#define FLOAT_EPSILON 1e-6

#define SHADOW_BIAS_MIN 0.01f
#define SHADOW_BIAS_MAX 0.01f

#define MAX_DIRECTIONAL_LIGHT_CASCADES 8

struct vert_in
{
	float4 _ : SV_POSITION;
	float2 t : TEX;
};

struct DirectionalLight
{
	float3 direction;
	float brightness;
	float3 color;
	float4x4 lightSpaces[MAX_DIRECTIONAL_LIGHT_CASCADES];
	float cascadesDepths[MAX_DIRECTIONAL_LIGHT_CASCADES + 1];
	int cascadesCount;
	Texture2DArray shadowMaps;
	float2 shadowMapSize;
};

cbuffer textureBuf
{
	Texture2D depthTex;

	Texture2D worldPosTex;
	Texture2D albedoTex;
	Texture2D normalTex;
	Texture2D metallicTex;
	Texture2D roughnessTex;
};

cbuffer lightBuf
{
	DirectionalLight directionalLight;
	float3 camPos;
	float cam_NEAR;
	float cam_FAR;
};

SamplerState texSampler;
SamplerComparisonState shadowSampler;

// normal distribution function
float NDFGGX(float3 norm, float3 halfway, float roughness)
{
	float a = roughness * roughness;
	float a2 = a * a;
	float ndoth = max(dot(norm, halfway), 0.0f);

	float denom = (ndoth * ndoth * (a2 - 1.0f) + 1.0f);

	return a2 / (PI * denom * denom);
}

// geometry Schlick GGX function
float GSGGXF(float3 norm, float3 dir, float roughness)
{
	float r = (roughness + 1.0f);
	float k = (r * r) / 8.0f;

	float ndotd = max(dot(norm, dir), 0.0f);
	return ndotd / (ndotd * (1.0f - k) + k);
}

// geometry smith function
float GSF(float3 norm, float3 lightDir, float3 obsDir, float roughness)
{
	return GSGGXF(norm, lightDir, roughness) * GSGGXF(norm, obsDir, roughness);
}

// Fresnel Schlick function
float3 FSF(float3 baseRefl, float3 halfway, float3 obsDir)
{
	float oneMinusDot = clamp(1.0f - dot(halfway, obsDir), 0.0f, 1.0f);
	return baseRefl + (1.0f - baseRefl) * oneMinusDot * oneMinusDot * oneMinusDot * oneMinusDot * oneMinusDot;
}

float attenuationFunction(float x, float maximum, float curvature)
{
	if (x >= maximum)
		return 0.0f;
	return pow((maximum - x) / maximum, 1.0f / curvature - 1.0f);
}

float directionalLightAttenuation(DirectionalLight light)
{
	return light.brightness;
}

float delinearizeDepth(float z, float n, float f)
{
	z = n + (f - n) * z;
	return f * (z - n) / ((f - n) * z);
}

float PercentageCloserFiltering(Texture2DArray shadowMaps, int slice, float2 shadowMapSize, float2 uv, float z)
{
	float deltaX = 1.0f / shadowMapSize.x;
	float deltaY = 1.0f / shadowMapSize.y;
	float totalFactor = 0.0f;
	for (int x = -1; x <= 1; x++)
		for (int y = -1; y <= 1; y++)
			totalFactor += shadowMaps.SampleCmpLevelZero(shadowSampler, float3(uv + float2(deltaX * x, deltaY * y), slice), z);

	return totalFactor / 9.0f;
}

float4 main(vert_in v) : SV_Target
{
    float3 curRadiance = float3(0.0f, 0.0f, 0.0f);

	float4 worldPos = worldPosTex.Sample(texSampler, v.t);
	if (worldPos.w == 0.0f)
		return float4(curRadiance, 0.0f);

	float3 albedo = albedoTex.Sample(texSampler, v.t).rgb;
	float3 normal = normalTex.Sample(texSampler, v.t).xyz;
	float metallic = metallicTex.Sample(texSampler, v.t).x;
	float roughness = roughnessTex.Sample(texSampler, v.t).x;

	float3 camDir = normalize(camPos - worldPos.xyz);
	float ndotc = max(dot(camDir, normal), 0.0f);

	float3 lightDir = -directionalLight.direction;
	int cascadeIndex = 0;
	float depth = depthTex.Sample(texSampler, v.t).x;
	[unroll(MAX_DIRECTIONAL_LIGHT_CASCADES)]
	while (cascadeIndex < MAX_DIRECTIONAL_LIGHT_CASCADES)
	{
		if (cascadeIndex >= directionalLight.cascadesCount - 1 ||
			delinearizeDepth(directionalLight.cascadesDepths[cascadeIndex + 1], cam_NEAR, cam_FAR) >= depth)
			break;

		cascadeIndex++;
	}

	float deltaDepth = directionalLight.lightSpaces[cascadeIndex]._m12;
	float bias = max(SHADOW_BIAS_MAX * deltaDepth * (1.0f - dot(normal, lightDir)), SHADOW_BIAS_MIN * deltaDepth);


	float4 lightSpacePos = mul(worldPos, directionalLight.lightSpaces[cascadeIndex]);
	lightSpacePos.y *= -1;
	lightSpacePos = float4(lightSpacePos.xyz / lightSpacePos.w, 1.0f);
	float shadowFactor = PercentageCloserFiltering(directionalLight.shadowMaps, cascadeIndex, directionalLight.shadowMapSize, lightSpacePos.xy * 0.5f + 0.5f, lightSpacePos.z - bias);
	//shadowFactor = 0.0f;
	//if (v.vdl[i][cascadeIndex].z - bias <= directionalLights[i].shadowMaps.Sample(shadowSampler, float3(v.vdl[i][cascadeIndex].xy * 0.5f + 0.5f, cascadeIndex)).x)
	//	shadowFactor = 1.0f;

	//if (shadowFactor == 0.0f)
	//{
	//	if (cascadeIndex == 0)
	//		curRadiance += float3(1.0f, 0.0f, 0.0f);
	//	if (cascadeIndex == 1)
	//		curRadiance += float3(0.0f, 1.0f, 0.0f);
	//	if (cascadeIndex == 2)
	//		curRadiance += float3(0.0f, 0.0f, 1.0f);
	//}
	if (dot(lightDir, normal) > 0.0f && shadowFactor > 0.0f)
	{
		float attenuation = directionalLightAttenuation(directionalLight);
		float3 halfway = normalize(lightDir + camDir);
		float3 radiance = directionalLight.color * attenuation;

		float ndotl = max(dot(normal, lightDir), 0.0f);

		float3 baseRefl = float3(0.04f, 0.04f, 0.04f);
		baseRefl = baseRefl * (1.0f - metallic) + albedo * metallic;
		float3 F = FSF(baseRefl, halfway, camDir);
		float D = NDFGGX(normal, halfway, roughness);
		float G = GSF(normal, lightDir, camDir, roughness);

		float denominator = 4.0f * ndotc * ndotl + FLOAT_EPSILON;

		float3 specular = D * G * F / denominator;
		float3 diffuse = (1.0f - F) * (1.0f - metallic);

		curRadiance += (diffuse * albedo / PI + specular) * radiance * ndotl * shadowFactor;
	}

	return float4(curRadiance, 1.0f);
}