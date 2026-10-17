// The level intro's grass growth, shared by the grass (each blade grows) and the terrain (raked dirt shows until the
// grass over it has grown). Driven by TileAnimator through the GrassGrow globals.
#ifndef GRASS_GROWTH_INCLUDED
#define GRASS_GROWTH_INCLUDED

// 1 -> 0 over the level intro; 0 (the default) is fully grown
float GrassGrowRemaining;
float3 GrassGrowCentre;
float GrassGrowRadius;
float GrassGrowSpread;

// Keep in step with TileAnimator.WaveDistanceWeight
#define GRASS_GROW_DISTANCE_WEIGHT 0.75

// How far the wave's front wanders from a circle, as a fraction of the level's radius, and how big its bulges are
#define GRASS_GROW_FRONT_WOBBLE 0.45
#define GRASS_GROW_FRONT_SCALE 0.55

float GrassGrowHash(float2 p)
{
	p = frac(p * float2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return frac(p.x * p.y);
}

float GrassGrowNoise(float2 p)
{
	float2 i = floor(p);
	float2 f = frac(p);
	f = f * f * (3 - 2 * f);
	return lerp(lerp(GrassGrowHash(i), GrassGrowHash(i + float2(1, 0)), f.x),
		lerp(GrassGrowHash(i + float2(0, 1)), GrassGrowHash(i + float2(1, 1)), f.x), f.y);
}

// 0 -> 1 as the grass at positionWS grows. It starts once the wave from the level's centre reaches it, held back by up
// to the rest of the spread by jitter (0-1). The wave's front is warped by noise so it spreads in uneven fingers, and
// headStart (a fraction of the radius) lets a blade come up that far ahead of it.
float GrassGrowthLinear(float3 positionWS, float jitter, float headStart)
{
	if (GrassGrowRemaining <= 0)
	{
		return 1;
	}

	float wobble = (GrassGrowNoise(positionWS.xz * GRASS_GROW_FRONT_SCALE + GrassGrowCentre.xz) - 0.5) *
		GRASS_GROW_FRONT_WOBBLE;
	float wave = length(positionWS.xz - GrassGrowCentre.xz) / max(GrassGrowRadius, 0.001) + wobble - headStart;
	float delay = saturate(saturate(wave) * GRASS_GROW_DISTANCE_WEIGHT + jitter * (1 - GRASS_GROW_DISTANCE_WEIGHT)) *
		GrassGrowSpread;
	return saturate((1 - GrassGrowRemaining - delay) / max(1 - GrassGrowSpread, 0.001));
}

// As above, eased out with a little overshoot before it settles
float GrassGrowth(float3 positionWS, float jitter, float headStart)
{
	float t = GrassGrowthLinear(positionWS, jitter, headStart);
	const float overshoot = 1.2;
	float u = t - 1;
	return 1 + (overshoot + 1) * u * u * u + overshoot * u * u;
}

#endif
