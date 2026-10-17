using Unity.Mathematics;

namespace Game.Terrain
{
	/// <summary>
	/// The 3D terrain field built from a <see cref="TerrainFootprint"/>: a flat top that runs <see cref="Shore"/> past the
	/// tiles, a rounded top edge and sloped sides that meet that shoreline exactly at <see cref="PivotHeight"/>, with noise
	/// roughening the sides but not the top.
	/// </summary>
	internal struct TerrainShape
	{
		public float TopHeight;
		public float PivotHeight;
		/// <summary>How far the top runs past the tiles' outline, as bare shore for the edging rocks.</summary>
		public float Shore;
		/// <summary>Horizontal run per unit of drop; 0 is a vertical wall.</summary>
		public float Slope;
		public float EdgeRadius;
		public float NoiseAmplitude;
		public float NoiseFrequency;
		/// <summary>The depth below the top over which noise fades in, so the walkable top stays flat.</summary>
		public float NoiseFadeDepth;
		public float3 NoiseOffset;

		public float Evaluate(float3 p, float footprintDistance)
		{
			float slopeLength = math.sqrt(1 + Slope * Slope);
			float side = (footprintDistance - Shore + (p.y - PivotHeight) * Slope) / slopeLength;
			float top = p.y - TopHeight;

			float distance = SmoothMax(top, side, EdgeRadius);

			if (NoiseAmplitude > 0)
			{
				float fade = math.saturate((TopHeight - p.y) / math.max(NoiseFadeDepth, 1e-4f));
				float n = noise.snoise(p * NoiseFrequency + NoiseOffset) +
				          0.5f * noise.snoise(p * (NoiseFrequency * 2.03f) + NoiseOffset.zxy);
				distance += n * NoiseAmplitude * fade;
			}

			return distance;
		}

		/// <summary>Polynomial smooth maximum: rounds the edge where the top meets the sides over about <paramref name="k"/>.</summary>
		private static float SmoothMax(float a, float b, float k)
		{
			if (k <= 0)
			{
				return math.max(a, b);
			}

			float h = math.saturate(0.5f + 0.5f * (a - b) / k);
			return math.lerp(b, a, h) + k * h * (1 - h);
		}
	}
}
