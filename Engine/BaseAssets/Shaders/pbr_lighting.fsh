#define PI 3.14159265f
#define FLOAT_EPSILON 1e-6

#define SHADOW_BIAS_MIN 0.005f
#define SHADOW_BIAS_MAX 0.01f

#define MAX_AMBIENT_LIGHTS 8
#define MAX_DIRECTIONAL_LIGHTS 8
#define MAX_SPOT_LIGHTS 8
#define MAX_POINT_LIGHTS 8

struct vert_in
{
	float4 _ : SV_POSITION;
	float3 v : POS;
	float2 t : TEX;
	float3 n : NORM;
	// ttw - tangent to world
	float3x3 ttw : TBN;
    
	float3 vdl[MAX_DIRECTIONAL_LIGHTS] : POS_IN_DIR_LIGHT;
	float3 vsl[MAX_SPOT_LIGHTS] : POS_IN_SPOT_LIGHT;
};

struct AmbientLight
{
	float brightness;
	float3 color;
};

struct DirectionalLight
{
	float3 direction;
	float brightness;
	float3 color;
	float4x4 lightSpace;
	Texture2D shadowMap;
};

struct SpotLight
{
	float3 position;
	float3 direction;
	float radius;
	float brightness;
	float intensity;
	float angularIntensity;
	float angle;
	float3 color;
	float4x4 lightSpace;
	Texture2D shadowMap;
};

struct PointLight
{
	float3 position;
	float radius;
	float brightness;
	float intensity;
	float3 color;
};

cbuffer materialBuf
{
	Texture2D albedoMap;
	Texture2D normalMap;
	Texture2D metallicMap;
	Texture2D roughnessMap;
	Texture2D ambientOcclusionMap;
};

cbuffer lightsBuf
{
	AmbientLight ambientLights[MAX_AMBIENT_LIGHTS];
	int ambientLightsCount;
	DirectionalLight directionalLights[MAX_DIRECTIONAL_LIGHTS];
	int directionalLightsCount;
	SpotLight spotLights[MAX_SPOT_LIGHTS];
	int spotLightsCount;
	PointLight pointLights[MAX_POINT_LIGHTS];
	int pointLightsCount;
	float3 camPos;
	float spotLight_NEAR;
};

SamplerState texSampler;
SamplerState shadowSampler;

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

float ambientLightAttenuation(AmbientLight light)
{
	return light.brightness;
}

float directionalLightAttenuation(DirectionalLight light)
{
	return light.brightness;
}

float attenuationFunction(float x, float maximum, float curvature)
{
	return max(pow((maximum - x) / maximum, 1.0f / curvature - 1.0f), 0.0f);
}

float spotLightAttenuation(SpotLight light, float3 lightVec)
{
	float dist = length(lightVec);
	float angle = acos(dot(normalize(-lightVec), light.direction));
	return light.brightness * attenuationFunction(dist, light.radius, light.intensity) * attenuationFunction(angle, light.angle, light.angularIntensity);
}

float linearizeDepth(float depth, float near, float far)
{
	float z = depth * 2.0 - 1.0;
	return (2.0 * near) / (far + near - z * (far - near));
}

float pointLightAttenuation(PointLight light, float3 lightVec)
{
	float dist = length(lightVec);
	return light.brightness * attenuationFunction(dist, light.radius, light.intensity);
}

float4 main(vert_in v) : SV_Target
{	
	float3 albedo = albedoMap.Sample(texSampler, v.t).xyz;
	float3 normal = normalize(mul(normalMap.Sample(texSampler, v.t).xyz * 2.0f - 1.0f, v.ttw));
	float metallic = metallicMap.Sample(texSampler, v.t).x;
	float roughness = roughnessMap.Sample(texSampler, v.t).x;
	float ambientOcclusion = ambientOcclusionMap.Sample(texSampler, v.t).x;
	
	float3 camDir = normalize(camPos - v.v);
	float ndotc = max(dot(camDir, normal), 0.0f);
	
	float3 totalRadiance = float3(0.0f, 0.0f, 0.0f);

	int i;
	for (i = 0; i < ambientLightsCount; i++)
	{
		totalRadiance += ambientLights[i].color * ambientLightAttenuation(ambientLights[i]);
	}
	for (i = 0; i < directionalLightsCount; i++)
	{
		float3 lightDir = -directionalLights[i].direction;
		if (dot(lightDir, v.n) > 0 && v.vdl[i].z - max(SHADOW_BIAS_MAX * (1.0f - dot(normal, lightDir)), SHADOW_BIAS_MIN) < directionalLights[i].shadowMap.Sample(shadowSampler, v.vdl[i].xy * 0.5f + 0.5f).r)
		{
			float attenuation = directionalLightAttenuation(directionalLights[i]);
			float3 halfway = normalize(lightDir + camDir);
			float3 radiance = directionalLights[i].color * attenuation;
		
			float ndotl = max(dot(normal, lightDir), 0.0f);
		
			float3 baseRefl = float3(0.04f, 0.04f, 0.04f);
			baseRefl = baseRefl * (1.0f - metallic) + albedo * metallic;
			float3 F = FSF(baseRefl, halfway, camDir);
			float D = NDFGGX(normal, halfway, roughness);
			float G = GSF(normal, lightDir, camDir, roughness);
		
			float denominator = 4.0f * ndotc * ndotl + FLOAT_EPSILON;
		
			float3 specular = D * G * F / denominator;
			float3 diffuse = (1.0f - F) * (1.0f - metallic);
		
			totalRadiance += (diffuse * albedo / PI + specular) * radiance * ndotl;
		}
	}
	for (i = 0; i < spotLightsCount; i++)
	{
		float3 lightVec = spotLights[i].position - v.v;
		float3 lightDir = normalize(lightVec);
		if (dot(lightDir, v.n) > 0 && linearizeDepth(v.vsl[i].z, spotLight_NEAR, spotLights[i].radius) - 
			max(SHADOW_BIAS_MAX * (1.0f - dot(v.n, lightDir)), SHADOW_BIAS_MIN) < 
			linearizeDepth(spotLights[i].shadowMap.Sample(shadowSampler, v.vsl[i].xy * 0.5f + 0.5f).r, spotLight_NEAR, spotLights[i].radius))
		{
			float attenuation = spotLightAttenuation(spotLights[i], lightVec);
			float3 halfway = normalize(lightDir + camDir);
			float3 radiance = spotLights[i].color * attenuation;
		
			float ndotl = max(dot(normal, lightDir), 0.0f);
		
			float3 baseRefl = float3(0.04f, 0.04f, 0.04f);
			baseRefl = baseRefl * (1.0f - metallic) + albedo * metallic;
			float3 F = FSF(baseRefl, halfway, camDir);
			float D = NDFGGX(normal, halfway, roughness);
			float G = GSF(normal, lightDir, camDir, roughness);
		
			float denominator = 4.0f * ndotc * ndotl + FLOAT_EPSILON;
		
			float3 specular = D * G * F / denominator;
			float3 diffuse = (1.0f - F) * (1.0f - metallic);
		
			totalRadiance += (diffuse * albedo / PI + specular) * radiance * ndotl;
		}
	}
	for (i = 0; i < pointLightsCount; i++)
	{		
		float3 lightVec = pointLights[i].position - v.v;

		float attenuation = pointLightAttenuation(pointLights[i], lightVec);
		float3 lightDir = normalize(lightVec);
		float3 halfway = normalize(lightDir + camDir);
		float3 radiance = pointLights[i].color * attenuation;
		
		float ndotl = max(dot(normal, lightDir), 0.0f);
		
		float3 baseRefl = float3(0.04f, 0.04f, 0.04f);
		baseRefl = baseRefl * (1.0f - metallic) + albedo * metallic;
		float3 F = FSF(baseRefl, halfway, camDir);
		float D = NDFGGX(normal, halfway, roughness);
		float G = GSF(normal, lightDir, camDir, roughness);
		
		float denominator = 4.0f * ndotc * ndotl + FLOAT_EPSILON;
		
		float3 specular = D * G * F / denominator;
		float3 diffuse = (1.0f - F) * (1.0f - metallic);
		
		totalRadiance += (diffuse * albedo / PI + specular) * radiance * ndotl;
	}
	
	float3 ambient = float3(0.03f, 0.03f, 0.03f) * albedo * ambientOcclusion;
	float3 result = ambient + totalRadiance;
	
	return float4(pow(result / (result + 1.0f), 1.0f / 2.2f), 1.0f);
}
	