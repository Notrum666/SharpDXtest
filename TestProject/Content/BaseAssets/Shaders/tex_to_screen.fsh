struct vert_in
{
	float4 _ : SV_POSITION;
	float2 t : TEX;
};

cbuffer textureBuf
{
	Texture2D tex;
	SamplerState texSampler;
};

//float linearizeDepth(float depth)
//{
//    float z = depth * 2.0 - 1.0;
//    return (2.0 * near * far) / (far + near - z * (far - near));
//}

float4 main(vert_in v) : SV_Target
{
    //outColor = vec4(vec3(linearizeDepth(texture(tex, _vt).r) / far), 1.0f);
    return float4(tex.Sample(texSampler, v.t).rgb, 1.0f);
}