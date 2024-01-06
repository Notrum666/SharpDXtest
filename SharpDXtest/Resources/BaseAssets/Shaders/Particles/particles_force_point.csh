﻿#define THREADS_X 64
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

cbuffer effectBuf
{
    float3 location;
    float force;
};

RWStructuredBuffer<Particle> particles;

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    if (index > maxParticles)
        return;
    
    if (particles[index].energy <= 0.0f)
        return;
    
    float3 direction = location - particles[index].position;
    if (dot(direction, direction) <= 1e-6)
        return;
    
    particles[index].velocity += normalize(direction) * force * deltaTime;
}