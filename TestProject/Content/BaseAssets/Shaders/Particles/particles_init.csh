#define THREADS_X 64
#define THREADS_Y 1
#define THREADS_TOTAL THREADS_X * THREADS_Y

struct Particle
{
	float3 position;
    float energy;
	float3 velocity;
};

cbuffer simulationBuf
{
    int maxParticles;
};

RWStructuredBuffer<Particle> particles;

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    if (index >= maxParticles)
        return;
    
    particles[index].position = float3(0.0f, 0.0f, 0.0f);
    particles[index].energy = 0.0f;
    particles[index].velocity = float3(0.0f, 0.0f, 0.0f);
}
	