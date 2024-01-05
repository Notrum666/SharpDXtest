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
    float deltaTime;
};

cbuffer emissionBuf
{
    float3 location;
    int toEmit;
};

RWStructuredBuffer<Particle> particles;

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    if (index >= min(toEmit, maxParticles))
        return;
    
    particles.IncrementCounter();
    
    particles[maxParticles - index - 1].position = location;
    particles[maxParticles - index - 1].energy = 5.0f;
    particles[maxParticles - index - 1].velocity = float3(0.0f, 0.0f, 0.0f);
}
	