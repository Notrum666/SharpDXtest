#define PI 3.14159265f
#define FLOAT_EPSILON 1e-6

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
};

struct SpotLight
{
	float3 position;
	float3 direction;
	float radius;
	float brightness;
	float intensity;
	float angle;
	float3 color;
	float4x4 lightSpace;
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
	Texture2D albedoTex;
	float metallic;
	float roughness;
	float ambientOcclusion;
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
	float exposure;
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

float ambientLightAttenuation(AmbientLight light)
{
	return light.brightness;
}

float directionalLightAttenuation(DirectionalLight light)
{
	return light.brightness;
}

float spotLightAttenuation(SpotLight light, float3 lightVec)
{
	return 0.0f;
}

float pointLightAttenuation(PointLight light, float3 lightVec)
{
	float dist = length(lightVec);
	return light.brightness * pow((light.radius - dist) / light.radius, 1.0f / light.intensity - 1.0f);
}

float4 main(vert_in v) : SV_Target
{
	float3 albedo = albedoTex.Sample(texSampler, v.t).xyz;
	
	float3 camDir = normalize(camPos - v.v);
	float ndotc = max(dot(camDir, v.n), 0.0f);
	
	float3 totalRadiance = float3(0.0f, 0.0f, 0.0f);

	int i;
	for (i = 0; i < ambientLightsCount; i++)
	{
		totalRadiance += ambientLights[i].color * ambientLightAttenuation(ambientLights[i]);
	}
	for (i = 0; i < directionalLightsCount; i++)
	{
		float attenuation = directionalLightAttenuation(directionalLights[i]);
		float3 lightDir = -directionalLights[i].direction;
		float3 halfway = normalize(lightDir + camDir);
		float3 radiance = directionalLights[i].color * attenuation;
		
		float ndotl = max(dot(v.n, lightDir), 0.0f);
		
		float3 baseRefl = float3(0.04f, 0.04f, 0.04f);
		baseRefl = baseRefl * (1.0f - metallic) + albedo * metallic;
		float3 F = FSF(baseRefl, halfway, camDir);
		float D = NDFGGX(v.n, halfway, roughness);
		float G = GSF(v.n, lightDir, camDir, roughness);
		
		float denominator = 4.0f * ndotc * ndotl + FLOAT_EPSILON;
		
		float3 specular = D * G * F / denominator;
		float3 diffuse = (1.0f - F) * (1.0f - metallic);
		
		totalRadiance += (diffuse * albedo / PI + specular) * radiance * ndotl;
	}
	for (i = 0; i < spotLightsCount; i++)
	{
		//curLightColor = float3(0.0f, 0.0f, 0.0f);
    	//
		//float3 lightDirection = normalize(spotLights[i].position - v.v.xyz);
		//if (dot(lightDirection, -spotLights[i].direction) >= cos(spotLights[i].angle / 2.0f))
		//{
		//	float3 vl = v.vsl[i] * 0.5f + 0.5f;
		//	//if ((2.0 * spot_near) / (spotLights[i].radius + spot_near - (vl.z * 2.0f - 1.0f) * (spotLights[i].radius - spot_near)) -
        //    //    max(SHADOW_BIAS_MAX * (1.0f - dot(_vn, lightDirection)), SHADOW_BIAS_MIN) <=
        //    //    (2.0 * spot_near) / (spotLights[i].radius + spot_near - (texture(spotLights[i].shadowTex, vl.xy).r * 2.0f - 1.0f) * (spotLights[i].radius - spot_near)))
		//	{
		//		float dist = distance(spotLights[i].position, v.v);
    	//
		//		curLightColor += max(0.0f, dot(lightDirection, v.n)) * diffuse;
		//		curLightColor += pow(max(0.0f, dot(v.n, normalize(lightDirection + normalize(camPos - v.v)))), metallic) * specular;
		//		curLightColor *= pow(spotLights[i].brightness * (spotLights[i].radius - dist) / spotLights[i].radius, (4.0f - 3.0f * spotLights[i].intensity) / 2.0f);
		//	}
		//}
        //
		//lightColor += spotLights[i].color * curLightColor;
	}
	for (i = 0; i < pointLightsCount; i++)
	{		
		float3 lightVec = pointLights[i].position - v.v;
		//float lightDistSqr = length(lightVec);
		//lightDistSqr *= lightDistSqr;
		//float attenuation = 1.0f / lightDistSqr;
		float attenuation = pointLightAttenuation(pointLights[i], lightVec);
		float3 lightDir = normalize(lightVec);
		float3 halfway = normalize(lightDir + camDir);
		float3 radiance = pointLights[i].color * attenuation;
		
		float ndotl = max(dot(v.n, lightDir), 0.0f);
		
		float3 baseRefl = float3(0.04f, 0.04f, 0.04f);
		baseRefl = baseRefl * (1.0f - metallic) + albedo * metallic;
		float3 F = FSF(baseRefl, halfway, camDir);
		float D = NDFGGX(v.n, halfway, roughness);
		float G = GSF(v.n, lightDir, camDir, roughness);
		
		float denominator = 4.0f * ndotc * ndotl + FLOAT_EPSILON;
		
		float3 specular = D * G * F / denominator;
		float3 diffuse = (1.0f - F) * (1.0f - metallic);
		
		totalRadiance += (diffuse * albedo / PI + specular) * radiance * ndotl;
	}
	
	float3 ambient = float3(0.03f, 0.03f, 0.03f) * albedo * ambientOcclusion;
	float3 result = ambient + totalRadiance;
	
	return float4(pow(result / (result + 1.0f), 1.0f / 2.2f), 1.0f);
	
	//float3 baseColor = tex.Sample(texSampler, v.t).rgb * lightColor;
	//return float4(pow(float3(1.0f, 1.0f, 1.0f) - exp(-baseColor * exposure), float3(1.0f / 2.2f, 1.0f / 2.2f, 1.0f / 2.2f)), 1.0f);
	
	//bloomColor = length(outColor.rgb) > 1.0f ? outColor : vec4(0);
}
	