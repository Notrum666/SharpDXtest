#define DENSITY_THRESHOLD 4.60517f
#define TRANSMITTANCE_THRESHOLD 0.01f
#define BASE_STEP 1.0f
#define MAX_STEP 10.0f
//#define TRAPEZOIDAL_INTEGRATION

struct vert_in
{
    float4 sv_pos : SV_POSITION;
};

struct OctreeNode
{
    // (-,-,-), (+,-,-), (-,+,-), (+,+,-)
    int4 subLeavesBottom;
    // (-,-,+), (+,-,+), (-,+,+), (+,+,+)
    int4 subLeavesTop;
    int parent;
    int tetrahedronsStart, tetrahedronsEnd;
    int middleVertex;
    int4 verticesBottom;
    int4 verticesTop;
};

struct MeshVertex
{
    float3 position;
    float density;
    float3 velocity;
    float nextDensity;
};

struct Tetrahedron
{
    int4 indices;
    float4x4 alphaMatrix;
};

cbuffer volumeBuf
{
    float3 ambientLight;
    float absorptionCoef;
    float scatteringCoef;
    
    float3 negLightDir;
    float3 invNegLightDir;
    float3 lightIntensity;
    
    float3 halfSize;
    float3 invHalfSize;
    float3 camToMinusHalfSize;
    float3 camToHalfSize;
    float3 relCamPos;
    float cam_near;
    float cam_far;
    float cam_farDivFarMinusNear;
    
    float2 invScreenSize;
};

cbuffer mat
{
    float4x4 modelViewProj;
    float4x4 invModelViewProj;
};

StructuredBuffer<OctreeNode> octree : register(t0);
StructuredBuffer<MeshVertex> meshVertices : register(t1);
StructuredBuffer<Tetrahedron> tetrahedrons : register(t2);

Texture2D depthTex;
SamplerState texSampler;

float delinearizeDepth(float z)
{
    return cam_farDivFarMinusNear * (1.0f - cam_near / z);
}

float linearizeDepth(float z)
{
    return cam_farDivFarMinusNear * cam_near / (cam_farDivFarMinusNear - z);
}

float SampleDensity(float3 location)
{
    float3 homogenous = location * invHalfSize; // from -1 to 1 within volume
    OctreeNode curLeaf = octree[0];
    while (true)
    {
        if (curLeaf.tetrahedronsEnd - curLeaf.tetrahedronsStart > 0)
            break;
        if (homogenous.z <= 0) // -z
        {
            homogenous.z = homogenous.z * 2.0f + 1.0f;
            if (homogenous.y <= 0) // -y
            {
                homogenous.y = homogenous.y * 2.0f + 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.x]; // (-,-,-)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.y]; // (+,-,-)
                    continue;
                }
            }
            else
            {
                homogenous.y = homogenous.y * 2.0f - 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.z]; // (-,+,-)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesBottom.w]; // (+,+,-)
                    continue;
                }
            }
        }
        else // +z
        {
            homogenous.z = homogenous.z * 2.0f - 1.0f;
            if (homogenous.y <= 0) // -y
            {
                homogenous.y = homogenous.y * 2.0f + 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    curLeaf = octree[curLeaf.subLeavesTop.x]; // (-,-,+)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesTop.y]; // (+,-,+)
                    continue;
                }
            }
            else
            {
                homogenous.y = homogenous.y * 2.0f - 1.0f;
                if (homogenous.x <= 0) // -x
                {
                    homogenous.x = homogenous.x * 2.0f + 1.0f;
                    curLeaf = octree[curLeaf.subLeavesTop.z]; // (-,+,+)
                    continue;
                }
                else // +x
                {
                    homogenous.x = homogenous.x * 2.0f - 1.0f;
                    curLeaf = octree[curLeaf.subLeavesTop.w]; // (+,+,+)
                    continue;
                }
            }
        }
    }
    
    for (int i = curLeaf.tetrahedronsStart; i < curLeaf.tetrahedronsEnd; i++)
    {
        float4 barycentric = mul(tetrahedrons[i].alphaMatrix, float4(1.0f, location));
        if (barycentric.x >= -1e-6 &&
            barycentric.y >= -1e-6 &&
            barycentric.z >= -1e-6 &&
            barycentric.w >= -1e-6)
        {
            int4 indices = tetrahedrons[i].indices;
            return meshVertices[indices.x].density * barycentric.x +
                   meshVertices[indices.y].density * barycentric.y +
                   meshVertices[indices.z].density * barycentric.z +
                   meshVertices[indices.w].density * barycentric.w;
        }
    }
    
    return 0.0f; // no tetrahedron containing current point was found, maybe as a result of floating point errors
    
    //float x = length(location);
    //return max(0.0f, 1.0f - x * x) * octree[0].density;
    
    //return 1.0f;
    //float plane = (sin(location.x * 0.4f) + sin(location.y * 0.4f)) * 2.0f;
    //return min(1.0f, max(0.0f, (plane + 5.0f - location.z) * 0.5f));
    //return 10.0f * (location.z + 5.0f <= plane);
}

float GetOutT(float3 rayOrigin, float3 invRayDirection)
{
    float t1 = (-halfSize.x - rayOrigin.x) * invRayDirection.x;
    float t2 = (halfSize.x - rayOrigin.x) * invRayDirection.x;
    
    float tmax = max(min(t1, 1.#INF), min(t2, 1.#INF));
    
    t1 = (-halfSize.y - rayOrigin.y) * invRayDirection.y;
    t2 = (halfSize.y - rayOrigin.y) * invRayDirection.y;
    
    tmax = min(tmax, max(min(t1, 1.#INF), min(t2, 1.#INF)));
    
    t1 = (-halfSize.z - rayOrigin.z) * invRayDirection.z;
    t2 = (halfSize.z - rayOrigin.z) * invRayDirection.z;
    
    tmax = min(tmax, max(min(t1, 1.#INF), min(t2, 1.#INF)));
    
    return tmax;
}

float2 GetInOutT(float3 invRayDirection)
{
    float t1 = camToMinusHalfSize.x * invRayDirection.x;
    float t2 = camToHalfSize.x * invRayDirection.x;

    float tmin = min(t1, t2);
    float tmax = max(t1, t2);
    
    t1 = camToMinusHalfSize.y * invRayDirection.y;
    t2 = camToHalfSize.y * invRayDirection.y;

    tmin = max(tmin, min(t1, t2));
    tmax = min(tmax, max(t1, t2));
    
    t1 = camToMinusHalfSize.z * invRayDirection.z;
    t2 = camToHalfSize.z * invRayDirection.z;

    tmin = max(tmin, min(t1, t2));
    tmax = min(tmax, max(t1, t2));
    
    tmin = max(tmin, 0.0f);
    
    return float2(tmin, tmax);
}

float PhaseReyleigh(float angleCos)
{
    return (1.0f + angleCos * angleCos) * 0.5f;
}

float RaymarchLight(float3 location, float3 direction, float distance, float prevDensity)
{
    float extinctionCoef = absorptionCoef + scatteringCoef;
    
    float densityApprox = 0.0f;
    float step = 1.0f;
    float curStep = 0.0f;
    
    do
    {
        step = min(step, distance - curStep);
        curStep += step;
        location += direction * step;
        
        float curDensity = SampleDensity(location);
        
#ifdef TRAPEZOIDAL_INTEGRATION
        densityApprox += (curDensity + prevDensity) * 0.5f * step;
        prevDensity = curDensity;
#else
        densityApprox += curDensity * step;
#endif
    } while (curStep < distance && densityApprox * extinctionCoef <= DENSITY_THRESHOLD);
    
    return exp(-densityApprox * extinctionCoef);
}

float4 Raymarch(float3 location, float3 direction, float distance)
{
    float extinctionCoef = absorptionCoef + scatteringCoef;
    
    float3 lightInScattering = lightIntensity * PhaseReyleigh(dot(normalize(direction), negLightDir));
    
    float transmittanceApprox = 1.0f;
    float3 radianceApprox = float3(0.0f, 0.0f, 0.0f);
    float step = 1.0;
    float curStep = 0.0f;
    #ifdef TRAPEZOIDAL_INTEGRATION
    float prevDensity = SampleDensity(location);
    #endif
    
    do
    {
        step = min(step, distance - curStep);
        curStep += step;
        location += direction * step;
        location = clamp(location, -halfSize, halfSize);
        //float curDensity = SampleDensity(float3((location.x + time * 5.0f) % (halfSize.x * 2.0f) - halfSize.x, location.y, location.z));
        float curDensity = SampleDensity(location);
        
#ifdef TRAPEZOIDAL_INTEGRATION
        float curOpticalDensity = (curDensity + prevDensity) * 0.5f * extinctionCoef;
        prevDensity = curDensity;
#else
        float curOpticalDensity = curDensity * extinctionCoef;
#endif
        
        //if (curOpticalDensity <= DENSITY_THRESHOLD)
        if (curOpticalDensity > 0.0f)
        {
            float curTransmittance = exp(-curOpticalDensity * step);
            
            transmittanceApprox *= curTransmittance;
        
            //float outT = GetOutT(location, invNegLightDir);
            
            float3 inScattering = ambientLight; // no light
            //float3 inScattering = ambientLight + lightInScattering * RaymarchLight(location, negLightDir, outT, curDensity); // light
            
            float outScattering = scatteringCoef * curDensity;

            float3 currentRadiance = inScattering * outScattering;
        
            radianceApprox += transmittanceApprox * currentRadiance * step;
        }
        
        //step = curDensity > 0.01f ? BASE_STEP : min(MAX_STEP, step * 2.0f);
    } while (curStep < distance && transmittanceApprox >= TRANSMITTANCE_THRESHOLD);
    
    return float4(radianceApprox, 1.0f - transmittanceApprox);
}

float4 main(vert_in v) : SV_Target
{    
    float2 t = v.sv_pos.xy * invScreenSize;
    float2 homogenous = 2.0f * t - float2(1.0f, 1.0f);
    homogenous.y = -homogenous.y;
    float4 endpoint = mul(float4(homogenous, 1.0f, 1.0f), invModelViewProj);
    float3 dir = endpoint.xyz / endpoint.w - relCamPos;
    
    float2 inOutDistances = GetInOutT(1.0f / dir) * cam_far;
    
    inOutDistances.y = min(inOutDistances.y, linearizeDepth(depthTex.Sample(texSampler, t).x));
    
    float depth = inOutDistances.y - inOutDistances.x;
    
    if (depth <= 0)
        discard;
    
    dir /= cam_far;
    
    return Raymarch(relCamPos + dir * inOutDistances.x, dir, depth);
}
	