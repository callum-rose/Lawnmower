using Unity.Mathematics;

namespace Game.Terrain
{
	/// <summary>
	/// The 3D terrain field built from a <see cref="TerrainFootprint"/>: a flat top, a rounded top edge and sloped sides
	/// that meet the tile grid exactly at <see cref="PivotHeight"/>, with noise roughening the sides but not the top.
	/// Below the pivot the sides get rougher the deeper they go: the slope wanders around the outline and large lumps
	/// grow in, so the cliff reads as natural rock while the shoreline still follows the grid.
	/// </summary>
	internal struct TerrainShape
	{
		public float TopHeight;
		public float PivotHeight;
		/// <summary>Horizontal run per unit of drop; 0 is a vertical wall.</summary>
		public float Slope;
		public float EdgeRadius;
		public float NoiseAmplitude;
		public float NoiseFrequency;
		/// <summary>The depth below the top over which noise fades in, so the walkable top stays flat.</summary>
		public float NoiseFadeDepth;
		public float3 NoiseOffset;

		/// <summary>The depth below the pivot over which the roughness below grows to full strength.</summary>
		public float RoughnessDepth;
		/// <summary>How much the slope varies around the outline at full roughness, as a fraction of it.</summary>
		public float SlopeVariation;
		public float SlopeVariationFrequency;
		/// <summary>How far the large lumps push the sides in and out at full roughness.</summary>
		public float LumpAmplitude;
		public float LumpFrequency;
		/// <summary>Scales the lumps' vertical frequency: above 1 squashes them into ledges.</summary>
		public float LumpVerticalScale;

		public float Evaluate(float3 p, float footprintDistance)
		{
			float rough = RoughnessDepth > 0 ? math.saturate((PivotHeight - p.y) / RoughnessDepth) : 0;

			float slope = Slope;
			if (SlopeVariation > 0)
			{
				float wander = noise.snoise(p.xz * SlopeVariationFrequency + NoiseOffset.yx);
				slope = math.max(0, Slope * (1 + wander * SlopeVariation * rough));
			}

			float slopeLength = math.sqrt(1 + slope * slope);
			float side = (footprintDistance + (p.y - PivotHeight) * slope) / slopeLength;
			float top = p.y - TopHeight;

			float distance = SmoothMax(top, side, EdgeRadius);

			if (NoiseAmplitude > 0)
			{
				float fade = math.saturate((TopHeight - p.y) / math.max(NoiseFadeDepth, 1e-4f));
				float n = noise.snoise(p * NoiseFrequency + NoiseOffset) +
				          0.5f * noise.snoise(p * (NoiseFrequency * 2.03f) + NoiseOffset.zxy);
				distance += n * NoiseAmplitude * fade;
			}

			if (LumpAmplitude > 0 && rough > 0)
			{
				float3 q = p * new float3(LumpFrequency, LumpFrequency * LumpVerticalScale, LumpFrequency) +
				           NoiseOffset.yzx;
				float n = noise.snoise(q) + 0.5f * noise.snoise(q * 2.07f + 5.3f) +
				          0.25f * noise.snoise(q * 4.11f + 11.7f);
				distance += n * (LumpAmplitude / 1.75f) * rough;
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
