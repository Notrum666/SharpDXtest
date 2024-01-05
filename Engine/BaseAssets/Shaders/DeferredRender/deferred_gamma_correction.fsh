struct vert_in
{
	float4 _ : SV_POSITION;
	float2 t : TEX;
};

cbuffer textureBuf
{
	Texture2D colorTex;
};

SamplerState texSampler;

float4 main(vert_in v) : SV_Target
{
	float3 color = colorTex.Sample(texSampler, v.t).rgb;
	
    return float4(pow(color / (color + 1.0f), 1.0f / 2.2f), 1.0f);
}