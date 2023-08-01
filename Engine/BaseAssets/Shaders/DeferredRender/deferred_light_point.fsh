#define PI 3.14159265f
#define FLOAT_EPSILON 1e-6

struct vert_in
{
	float4 _ : SV_POSITION;
	float2 t : TEX;
};

struct PointLight
{
	float3 position;
	float radius;
	float brightness;
	float intensity;
	float3 color;
};

cbuffer textureBuf
{
	Texture2D worldPosTex;
	Texture2D albedoTex;
	Texture2D normalTex;
	Texture2D metallicTex;
	Texture2D roughnessTex;
};

cbuffer lightBuf
{
	PointLight pointLight;
	float3 camPos;
};

SamplerState texSampler;

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

float pointLightAttenuation(PointLight light, float3 lightVec)
{
	float dist = length(lightVec);
	return light.brightness * attenuationFunction(dist, light.radius, light.intensity);
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

	float3 lightVec = pointLight.position - worldPos.xyz;
	if (dot(lightVec, lightVec) > pointLight.radius * pointLight.radius)
		return float4(curRadiance, 0.0f);

	float attenuation = pointLightAttenuation(pointLight, lightVec);

	float3 lightDir = normalize(lightVec);
	float3 halfway = normalize(lightDir + camDir);
	float3 radiance = pointLight.color * attenuation;

	float ndotl = max(dot(normal, lightDir), 0.0f);

	float3 baseRefl = float3(0.04f, 0.04f, 0.04f);
	baseRefl = baseRefl * (1.0f - metallic) + albedo * metallic;
	float3 F = FSF(baseRefl, halfway, camDir);
	float D = NDFGGX(normal, halfway, roughness);
	float G = GSF(normal, lightDir, camDir, roughness);

	float denominator = 4.0f * ndotc * ndotl + FLOAT_EPSILON;

	float3 specular = D * G * F / denominator;
	float3 diffuse = (1.0f - F) * (1.0f - metallic);

	curRadiance += (diffuse * albedo / PI + specular) * radiance * ndotl;

	return float4(curRadiance, 1.0f);
}