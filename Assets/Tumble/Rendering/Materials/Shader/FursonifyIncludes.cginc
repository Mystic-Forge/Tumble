#if !defined(FURSONIFY_INCLUDES_CGINC)
#define FURSONIFY_INCLUDES_CGINC

#define hash(p)  frac(sin(dot(p, float2(11.9898, 78.233))) * 43758.5453)

float blueNoise(float2 U)
{
	float v = hash(U + float2(-1, 0))
	+ hash(U + float2( 1, 0))
	+ hash(U + float2( 0, 1))
	+ hash(U + float2( 0,-1));
	return hash(U) - v / 4. + .5;
}

// Used for smoothing out low sample counts
float pixelDither(int2 pixelCoord)
{
	return blueNoise(float2(pixelCoord.x, pixelCoord.y));
}

float pixelDitherFine(int2 pixelCoord)
{
	return pixelCoord.x % 2 == pixelCoord.y % 2;
}

#define HashMultiplier 437.585453123

float fastHash(float s)
{
	return frac(sin(s) * HashMultiplier);
}

float fastHash(float2 p)
{
	const float2 ps = sin(p) * HashMultiplier;
	return abs(ps.x + ps.y) % 1;
}

float2 fastHash2(float2 p)
{
	p = float2(dot(p, float2(127.1, 311.7)),
	           dot(p, float2(269.5, 183.3)));
	return frac(sin(p) * HashMultiplier);
}

#define M1 1597334677U     //1719413*929
#define M2 3812015801U     //140473*2467*11


float hash2(uint2 q)
{
	q *= uint2(M1, M2);
	uint n = (q.x ^ q.y) * M1;
	return float(n) * (1.0 / float(0xffffffffU));
}

float fastGradNoise(float p)
{
	const float i = floor(p);
	const float f = frac(p);
	const float u = f * f * (3.0 - 2.0 * f);
	return lerp(fastHash(i), fastHash(i + 1.0), u);
}

float fastGradNoise(float2 p)
{
	const float2 i = floor(p);
	const float2 f = frac(p);
	const float2 u = f * f * (3.0 - 2.0 * f);
	return lerp(
		lerp(fastHash(i + float2(0.0, 0.0)), fastHash(i + float2(1.0, 0.0)), u.x),
		lerp(fastHash(i + float2(0.0, 1.0)), fastHash(i + float2(1.0, 1.0)), u.x),
		u.y);
}

float3 voronoi(in float2 uv, in float variance, in float time)
{
	const float2 cellMin = floor(uv);
	const float2 cellOffset = frac(uv);

	float3 m = 8.0;
	for (int j = -1; j <= 1; j++)
		for (int i = -1; i <= 1; i++) {
			float2 localCell = float2(i, j);
			float2 cellId = float2(fastHash2(cellMin + localCell));
			float2 cellDirection = sin(time + 6.2831 * cellId);
			float2 r = localCell - cellOffset + (0.5 * variance * cellDirection);
			float  distanceToCell = dot(r, r);
			if (distanceToCell < m.x) m = float3(distanceToCell, r);
		}

	float distance = saturate(sqrt(m.x));
	return float3(distance, m.yz);
}


float edgeVoronoi(in float2 uv, in float variance, in float time)
{
	const float2 cellMin = floor(uv);
	const float2 cellOffset = frac(uv);

	float secondNearest = 8.0;
	float nearest = 8.0;
	for (int j = -1; j <= 1; j++)
		for (int i = -1; i <= 1; i++) {
			float2 localCell = float2(i, j);
			float2 cellId = float2(fastHash2(cellMin + localCell));
			float2 cellDirection = sin(time + 6.2831 * cellId);
			float2 r = localCell - cellOffset + (0.5 * variance * cellDirection);
			float  distanceToCell = dot(r, r);
			if (distanceToCell < nearest) {
				secondNearest = nearest;
				nearest = distanceToCell;
			} else if(distanceToCell < secondNearest) {
				secondNearest = distanceToCell;
			}
		}

	float distance = sqrt(secondNearest) - sqrt(nearest);
	return distance;
}

#include "UnityLightingCommon.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

float blinnPhongSpecular(float3 normal, float3 lightDir, float3 viewDir, float power)
{
	float3 halfDir = normalize(lightDir - viewDir);
	float  nDotH = max(0.0, dot(normal, halfDir));
	return pow(nDotH, power) * saturate(dot(normal, lightDir));
}

float specular(float3 normal, float3 lightDir, float3 viewDir, float power)
{
	float nDotH = max(0.0, dot(reflect(viewDir, normal), lightDir));
	return pow(nDotH, power);
}

float3 boxProjection(
	float3 direction, float3       position,
	float4 cubemapPosition, float4 boxMin, float4 boxMax
)
{
	UNITY_BRANCH
	if (cubemapPosition.w > 0) {
		float3 factors =
		((direction > 0 ? boxMax.xyz : boxMin.xyz) - position) / direction;
		float scalar = min(min(factors.x, factors.y), factors.z);
		direction = direction * scalar + (position - cubemapPosition.xyz);
	}
	return direction;
}

float3 sampleEnvironment(const float3 worldPos, const float3 normal, float lod)
{
	float3 uvw = boxProjection(
		normal, worldPos, unity_SpecCube0_ProbePosition,
		unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax
	);
	float3 color = UNITY_SAMPLE_TEXCUBE_LOD(
		unity_SpecCube0, uvw, lod
	);

	color = DecodeHDR(half4(color, 1), unity_SpecCube0_HDR);

	const float blend = unity_SpecCube0_BoxMin.w;
	if (blend < 0.99999) {
		uvw = boxProjection(
			normal, worldPos,
			unity_SpecCube1_ProbePosition,
			unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax
		);
		float3 sample = UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(
			unity_SpecCube1, unity_SpecCube0, uvw, lod
		);
		sample = DecodeHDR(half4(sample, 1), unity_SpecCube1_HDR);
		color = lerp(sample.rgb, color, blend);
	}
	return color;
}

// Thank you mr. bgolus: https://forum.unity.com/threads/converting-a-clip-space-point-to-world-space.1497056/
float3 ClipToWorldPos(float4 clipPos)
{
	#ifdef UNITY_REVERSED_Z
	float3 ndc = clipPos.xyz / clipPos.w;
	ndc = float3(ndc.x, ndc.y * _ProjectionParams.x, (1.0 - ndc.z) * 2.0 - 1.0);
	float3 viewPos = mul(unity_CameraInvProjection, float4(ndc * clipPos.w, clipPos.w));
	#else
	float3 viewPos = mul(unity_CameraInvProjection, clipPos);
	#endif
	return mul(unity_MatrixInvV, float4(viewPos, 1.0)).xyz;
}

#endif // FURSONIFY_INCLUDES_CGINC
