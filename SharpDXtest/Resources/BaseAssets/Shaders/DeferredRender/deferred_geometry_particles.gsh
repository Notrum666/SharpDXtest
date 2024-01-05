struct vert_in
{
    float4 v : SV_POSITION;
};

struct vert_out
{
    float4 sv_pos: SV_POSITION;
    float4 v : POS;
    float2 t : TEX;
    float3 n : NORM;
	// ttw - tangent to world
    float3x3 ttw : TBN;
};

cbuffer mat
{
    float4x4 proj;
    float4x4 view;
    float4x4 model;
};

cbuffer billboardBuf
{
    float2 size;
    float3 camDir;
    float3 camUp;
};

[maxvertexcount(6)]
void main(point vert_in p[1], inout TriangleStream<vert_out> stream)
{
    vert_out vertices[4];
    
    float3 right = cross(camDir, camUp);
    
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        vertices[i].t = float2(i & 1, i >> 1);
        vertices[i].v = p[0].v + float4(right * size.x * (vertices[i].t.x * 2.0f - 1.0f) + camUp * size.y * (1.0f - vertices[i].t.y * 2.0f), 0.0f) * 0.5f;
        vertices[i].sv_pos = mul(mul(vertices[i].v, view), proj);
        vertices[i].n = -camDir;
        
        vertices[i].ttw = float3x3(right, -camUp, vertices[i].n);
    }

    stream.Append(vertices[0]);
    stream.Append(vertices[2]);
    stream.Append(vertices[3]);
    stream.RestartStrip();
    stream.Append(vertices[0]);
    stream.Append(vertices[3]);
    stream.Append(vertices[1]);
}