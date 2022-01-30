#define MAX_DIRECTIONAL_LIGHTS 8
#define MAX_SPOT_LIGHTS 8

struct vert_in
{
    float3 v : POS;
    float2 t : TEX;
    float3 n : NORM;
};

struct vert_out
{
    float4 sv_pos : SV_POSITION;
    float3 v : POS;
    float2 t : TEX;
	float3 n : NORM;
    
	float3 vdl[MAX_DIRECTIONAL_LIGHTS] : POS_IN_DIR_LIGHT;
	float3 vsl[MAX_SPOT_LIGHTS] : POS_IN_SPOT_LIGHT;
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

cbuffer mat
{
    float4x4 proj;
    float4x4 view;
    float4x4 model;
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
	res.n = mul(float4(vert.n, 0.0f), model).xyz;
	
	float4 tmp;
	int i;
	for (i = 0; i < directionalLightsCount; i++)
	{
		tmp = mul(v_world, directionalLights[i].lightSpace);
		res.vdl[i] = tmp.xyz / tmp.w;
	}
	for (i = 0; i < spotLightsCount; i++)
	{
		tmp = mul(v_world, spotLights[i].lightSpace);
		res.vsl[i] = tmp.xyz / tmp.w;
	}
    
	return res;
}