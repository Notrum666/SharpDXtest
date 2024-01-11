#define THREADS_X 64
#define THREADS_Y 1
#define THREADS_TOTAL THREADS_X * THREADS_Y
#define UINT_MAX 4294967295U
#define PI 3.14159265f

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
    float radius;
    float innerRadius;
};

RWStructuredBuffer<Particle> particles;
shared RWStructuredBuffer<uint> rng;

void mutateSeed(int seedId)
{
    rng[seedId] = rng[seedId] * 196314165U + 907633515U;
}

uint rand(int seedId)
{
    mutateSeed(seedId);
    return rng[seedId];
}

float randFloat(int seedId)
{
    mutateSeed(seedId);
    return ((float) rng[seedId]) / UINT_MAX;
}

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    if (index >= min(toEmit, maxParticles))
        return;
    
    float phi = randFloat(groupID.x) * PI;
    float theta = randFloat(groupID.x) * 2.0f * PI;
    float r = innerRadius + (radius - innerRadius) * max(randFloat(groupID.x), randFloat(groupID.x));
    float3 offset = float3(cos(theta) * sin(phi), sin(theta) * sin(phi), -cos(phi)) * r;
    
    particles.IncrementCounter();
    
    particles[maxParticles - index - 1].position = location + offset;
    particles[maxParticles - index - 1].energy = 5.0f;
    particles[maxParticles - index - 1].velocity = float3(0.0f, 0.0f, 0.0f);
}
	