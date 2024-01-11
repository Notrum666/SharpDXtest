#define MAX_DIRECTIONAL_LIGHTS 4
#define MAX_DIRECTIONAL_LIGHT_CASCADES 4
#define MAX_SPOT_LIGHTS 8

struct vert_in
{
    float3 v : POS;
    float2 t : TEX;
	float3 n : NORM;
	float3 tx : TANGENT;
};

struct vert_out
{
    float4 sv_pos : SV_POSITION;
    float3 v : POS;
    float2 t : TEX;
	float3 n : NORM;
	// ttw - tangent to world
	float3x3 ttw : TBN;
    
	float3 vdl[MAX_DIRECTIONAL_LIGHTS][MAX_DIRECTIONAL_LIGHT_CASCADES] : POS_IN_DIR_LIGHT;
	float3 vsl[MAX_SPOT_LIGHTS] : POS_IN_SPOT_LIGHT;
};

struct DirectionalLight
{
	float3 direction;
	float brightness;
	float3 color;
	float4x4 lightSpaces[MAX_DIRECTIONAL_LIGHT_CASCADES];
	int cascadesCount;
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

cbuffer mat
{
    float4x4 proj;
    float4x4 view;
    float4x4 model;
	float4x4 modelNorm;
};

cbuffer lightsBuf
{
	DirectionalLight directionalLights[MAX_DIRECTIONAL_LIGHTS];
	int directionalLightsCount;
	SpotLight spotLights[MAX_SPOT_LIGHTS];
	int spotLightsCount;
};

vert_out main(vert_in vert)
{
	float4 v_world = mul(float4(vert.v, 1.0f), model);
	vert_out res = (vert_out) 0;
	res.sv_pos = mul(mul(v_world, view), proj);
    
	res.v = v_world.xyz;
	res.t = vert.t;
	res.n = normalize(mul(float4(vert.n, 0.0f), modelNorm).xyz);
	float3 tangent = normalize(mul(float4(vert.tx, 0.0f), modelNorm).xyz);
	float3 bitangent = cross(res.n, tangent);
	res.ttw = float3x3(tangent, bitangent, res.n);

	float4 tmp;
	int i, j;
	for (i = 0; i < directionalLightsCount && i < MAX_DIRECTIONAL_LIGHTS; i++)
	{
		for (j = 0; j < directionalLights[i].cascadesCount && j < MAX_DIRECTIONAL_LIGHT_CASCADES; j++)
		{
			tmp = mul(v_world, directionalLights[i].lightSpaces[j]);
			tmp.y *= -1;
			res.vdl[i][j] = tmp.xyz / tmp.w;
		}
	}

	for (i = 0; i < spotLightsCount; i++)
	{
		tmp = mul(v_world, spotLights[i].lightSpace);
		tmp.y *= -1;
		res.vsl[i] = tmp.xyz / tmp.w;
	}
    
	return res;
}