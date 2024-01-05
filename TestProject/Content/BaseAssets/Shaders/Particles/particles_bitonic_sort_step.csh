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

cbuffer sortBuf
{
    int subArraySize;
    int compareDist;
};

RWStructuredBuffer<Particle> particles;

[numthreads(THREADS_X, THREADS_Y, 1)]
void main(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{	
    int index = groupID.x * THREADS_TOTAL + groupIndex;
    if (index * 2 > maxParticles)
        return;
    
    int subArrayIndex = index / (subArraySize >> 1);
    int pairIndex = index % (subArraySize >> 1);
    int elementIndex = subArrayIndex * subArraySize + pairIndex;
    int compareDistance = compareDist;
    if ((subArrayIndex & 1) > 0)
    {
        elementIndex += compareDist;
        compareDistance = -compareDistance;
    }
    if (particles[elementIndex].energy < particles[elementIndex + compareDistance].energy)
    {
        Particle tmp = particles[elementIndex];
        particles[elementIndex] = particles[elementIndex + compareDistance];
        particles[elementIndex + compareDistance] = tmp;
    }
}
	