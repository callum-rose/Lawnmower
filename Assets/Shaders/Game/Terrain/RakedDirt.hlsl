// Raked dirt for the terrain's tops: grooves that follow RakePath's route through the tiles. Positions are in tiles (object xz), where tile (x, z) covers [x, x + 1] x [z, z + 1].
#ifndef RAKED_DIRT_INCLUDED
#define RAKED_DIRT_INCLUDED

TEXTURE2D(_TerrainRake);

#define RAKE_NONE 4

static const float2 RakeSides[4] = { float2(1, 0), float2(0, 1), float2(-1, 0), float2(0, -1) };

float RakeHash(float2 p)
{
	p = frac(p * float2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return frac(p.x * p.y);
}

float RakeNoise(float2 p)
{
	float2 i = floor(p);
	float2 f = frac(p);
	f = f * f * (3 - 2 * f);
	return lerp(lerp(RakeHash(i), RakeHash(i + float2(1, 0)), f.x),
		lerp(RakeHash(i + float2(0, 1)), RakeHash(i + float2(1, 1)), f.x), f.y);
}

// The two sides the tile's grooves leave by (RAKE_NONE where a path ends)
int2 RakeSidesOf(float2 cell)
{
	int2 size = int2(_TerrainRake_TexelSize.zw);
	int2 texel = clamp(int2(cell), 0, size - 1);
	return int2(round(LOAD_TEXTURE2D(_TerrainRake, texel).rg * 255));
}

// A rounded square's "radius": the circle's at _RakeTurnSquareness = 2, squarer above, so arcs reach further into
// corners. Exact along the axes, so grooves still meet the tile edges where circles would. gradient is its slope.
float SquareLength(float2 d, out float2 gradient)
{
	float p = _RakeTurnSquareness;
	float2 a = max(abs(d), 1e-5);
	float r = pow(pow(a.x, p) + pow(a.y, p), 1 / p);
	gradient = sign(d) * pow(a / max(r, 1e-5), p - 1);
	return r;
}

// Distance across the grooves, in tiles, and its gradient. Grooves sit at the same distances from a
// tile's corners on every side they leave by, so they run on unbroken into the next tile of the path. Where a path
// ends the rake was lifted, so the grooves fade out: toEnd is how far p is past the point where they're gone (negative
// before it, and very negative away from the ends).
float RakeAcross(float2 tile, out float2 gradient, out float toEnd)
{
	float2 cell = floor(tile);
	float2 f = tile - cell;
	int2 sides = RakeSidesOf(cell);
	int a = sides.x == RAKE_NONE ? sides.y : sides.x;
	int b = sides.x == RAKE_NONE ? RAKE_NONE : sides.y;
	toEnd = -1e3;

	if (a == RAKE_NONE)
	{
		// A tile on its own: one short stroke across it, along x
		gradient = float2(0, 1);
		toEnd = abs(f.x - 0.5) - (0.5 - _RakeEndMargin);
		return f.y;
	}

	if (b == RAKE_NONE || (a ^ b) == 2)
	{
		// Straight through, or on to the end of a path short of the far side
		gradient = a == 0 || a == 2 ? float2(0, 1) : float2(1, 0);
		if (b == RAKE_NONE)
		{
			float travelled = dot(f - (0.5 + 0.5 * RakeSides[a]), -RakeSides[a]);
			toEnd = travelled - (1 - _RakeEndMargin);
		}

		return dot(f, gradient);
	}
	// A turn: quarter arcs (squared off into the far corner) round the corner between the two sides
	float2 corner = 0.5 + 0.5 * (RakeSides[a] + RakeSides[b]);
	return SquareLength(f - corner, gradient);
}

// Height across one groove, x in [-0.5, 0.5] from its middle: a rounded cut with the dirt it pushed aside heaped
// along both edges, flat beyond
float GrooveHeight(float x, float w)
{
	float cut = exp(-x * x / (w * w));
	float heap = abs(x) - 1.7 * w;
	return 0.45 * exp(-heap * heap / (0.6 * w * w)) - cut;
}

struct DirtSurface
{
	half3 albedo;
	// The xz of the normal bent by the grooves, in object space (y up), for lighting
	float2 tilt;
};

DirtSurface RakedDirt(float2 tile, half3 dirt)
{
	DirtSurface surface;

	// Grooves, wandering a little as the rake was dragged: the whole pattern is drawn through a gentle warp, so every
	// prong drifts together and grooves still meet across tile edges
	float2 warp = float2(RakeNoise(tile * _RakeWobbleScale), RakeNoise(tile * _RakeWobbleScale + 31.4)) - 0.5;
	float2 raked = tile + warp * 2 * _RakeWobble;

	float2 across;
	float toEnd;
	float band = RakeAcross(raked, across, toEnd);
	float u = band * _RakeLines;
	// Faded out as the grooves get too fine for the pixels. Not fwidth(u): u can jump between tiles without its
	// grooves doing so, but it never grows faster than one line per 1 / _RakeLines tiles
	float2 footprint = fwidth(tile);
	float fade = saturate(1.5 - 2 * max(footprint.x, footprint.y) * _RakeLines);
	// Narrow grooves cut into flat dirt, midway between the lines u = whole numbers. Only the rake's width of them
	// (band <= 1): beyond it, in the tip of a turn's outer corner, the dirt is left flat. So every tile edge a path
	// doesn't cross is flat on both sides, and no tile edge shows.
	float x = frac(u) - 0.5;
	// Where the rake was lifted each groove gets shallower and narrower until it's gone, the prongs leaving at
	// slightly different points
	float lift = smoothstep(-_RakeEndFade, 0, toEnd + (RakeHash(floor(u)) - 0.5) * 0.1);
	float w = _RakeGrooveWidth * (1 - 0.5 * lift);
	// The rake pressed in harder in some places than others
	float pressure = 1 + (RakeNoise(raked * 2.3 + 7.7) - 0.5) * 2 * _RakePressureVariation;
	float strength = fade * step(band, 1) * (1 - lift) * pressure;
	float height = GrooveHeight(x, w) * strength;
	const float e = 0.01;
	float slope = (GrooveHeight(x + e, w) - GrooveHeight(x - e, w)) / (2 * e);
	// Grooves are _RakeDepth * 0.04 tiles deep; x grows _RakeLines times faster than the tile coordinates
	surface.tilt = -slope * 0.04 * _RakeDepth * _RakeLines * strength * across;

	float grain = RakeNoise(tile * 23) * 0.6 + RakeNoise(tile * 61) * 0.4;
	half3 albedo = dirt * (1 + _RakeGrooveShade * min(height, 0));
	albedo *= 1 + (grain - 0.5) * _DirtGrain;

	surface.albedo = albedo;
	return surface;
}

#endif
