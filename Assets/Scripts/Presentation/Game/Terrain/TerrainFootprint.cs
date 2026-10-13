using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Terrain
{
	/// <summary>
	/// A quarter circle rounding one grid corner. <see cref="Facing"/> points from the corner's grid vertex into the
	/// quadrant the arc's centre sits in: the solid cell for a convex corner, the empty cell for a concave one.
	/// </summary>
	internal struct FootprintArc
	{
		public float2 Centre;
		public float2 Facing;
		public float Radius;
	}

	/// <summary>
	/// The exact 2D signed distance to the outline of a grid of solid cells, with its convex and concave corners rounded
	/// by separate radii. Cell (x, z) covers [x, x + 1] x [z, z + 1]; negative is inside. The outline is built once as
	/// straight segments and quarter-circle arcs, so the distance is exact everywhere, not just near the corners.
	/// Diagonal-only neighbours are treated as two convex corners: they don't join, matching the game's 4-way moves.
	/// </summary>
	internal struct TerrainFootprint : IDisposable
	{
		private const float Far = 1e4f;

		[ReadOnly] public NativeArray<float4> Segments;
		[ReadOnly] public NativeArray<FootprintArc> Arcs;
		[ReadOnly] public NativeArray<byte> Solid;

		public int Width;
		public int Depth;
		public float ConvexRadius;
		public float ConcaveRadius;

		#region API

		public static TerrainFootprint Create(bool[,] solid, float convexRadius, float concaveRadius, Allocator allocator)
		{
			int width = solid.GetLength(0);
			int depth = solid.GetLength(1);

			// Above half a tile, corners at either end of a one-tile edge would overlap
			convexRadius = math.clamp(convexRadius, 0, 0.5f);
			concaveRadius = math.clamp(concaveRadius, 0, 0.5f);

			TerrainFootprint footprint = new TerrainFootprint
			{
				Width = width,
				Depth = depth,
				ConvexRadius = convexRadius,
				ConcaveRadius = concaveRadius,
				Solid = new NativeArray<byte>(width * depth, allocator),
			};

			for (int z = 0; z < depth; z++)
			{
				for (int x = 0; x < width; x++)
				{
					footprint.Solid[x + z * width] = (byte)(solid[x, z] ? 1 : 0);
				}
			}

			NativeList<float4> segments = new NativeList<float4>(Allocator.Temp);
			NativeList<FootprintArc> arcs = new NativeList<FootprintArc>(Allocator.Temp);

			footprint.AddSegments(ref segments);
			footprint.AddArcs(ref arcs);

			footprint.Segments = new NativeArray<float4>(segments.AsArray(), allocator);
			footprint.Arcs = new NativeArray<FootprintArc>(arcs.AsArray(), allocator);

			segments.Dispose();
			arcs.Dispose();

			return footprint;
		}

		public bool IsSolid(int x, int z)
		{
			return x >= 0 && z >= 0 && x < Width && z < Depth && Solid[x + z * Width] != 0;
		}

		public float Distance(float2 p)
		{
			float distance = Far;

			for (int i = 0; i < Segments.Length; i++)
			{
				float4 segment = Segments[i];
				distance = math.min(distance, SegmentDistance(p, segment.xy, segment.zw));
			}

			for (int i = 0; i < Arcs.Length; i++)
			{
				distance = math.min(distance, ArcDistance(p, Arcs[i]));
			}

			return IsInside(p) ? -distance : distance;
		}

		public bool IsInside(float2 p)
		{
			int2 cell = (int2)math.floor(p);
			bool inside = IsSolid(cell.x, cell.y);

			int2 vertex = (int2)math.round(p);
			float2 local = p - (float2)vertex;
			int2 facing = new int2(local.x >= 0 ? 1 : -1, local.y >= 0 ? 1 : -1);

			// The cells either side of this quadrant, across the vertex's two grid lines
			bool acrossX = IsSolid(cell.x - facing.x, cell.y);
			bool acrossZ = IsSolid(cell.x, cell.y - facing.y);
			bool opposite = IsSolid(cell.x - facing.x, cell.y - facing.y);
			float2 inQuadrant = local * facing;

			if (inside && !acrossX && !acrossZ)
			{
				return !IsCutByArc(inQuadrant, ConvexRadius);
			}

			if (!inside && acrossX && acrossZ && opposite)
			{
				return IsCutByArc(inQuadrant, ConcaveRadius);
			}

			return inside;
		}

		public void Dispose()
		{
			if (Segments.IsCreated)
			{
				Segments.Dispose();
			}

			if (Arcs.IsCreated)
			{
				Arcs.Dispose();
			}

			if (Solid.IsCreated)
			{
				Solid.Dispose();
			}
		}

		#endregion

		#region Methods

		private void AddSegments(ref NativeList<float4> segments)
		{
			int2 right = new int2(1, 0);
			int2 forward = new int2(0, 1);

			for (int z = 0; z < Depth; z++)
			{
				for (int x = 0; x < Width; x++)
				{
					if (!IsSolid(x, z))
					{
						continue;
					}

					int2 cell = new int2(x, z);
					AddSideSegment(ref segments, cell, right, forward);
					AddSideSegment(ref segments, cell, -right, forward);
					AddSideSegment(ref segments, cell, forward, right);
					AddSideSegment(ref segments, cell, -forward, right);
				}
			}
		}

		private void AddSideSegment(ref NativeList<float4> segments, int2 cell, int2 normal, int2 tangent)
		{
			if (IsSolid(cell.x + normal.x, cell.y + normal.y))
			{
				return;
			}

			float2 sideCentre = (float2)cell + 0.5f + (float2)normal * 0.5f;
			float2 start = sideCentre - (float2)tangent * (0.5f - EndTrim(cell, normal, -tangent));
			float2 end = sideCentre + (float2)tangent * (0.5f - EndTrim(cell, normal, tangent));

			if (math.distancesq(start, end) > 1e-8f)
			{
				segments.Add(new float4(start, end));
			}
		}

		/// <summary>How far a side is cut back from the vertex at its <paramref name="tangent"/> end for that corner's arc.</summary>
		private float EndTrim(int2 cell, int2 normal, int2 tangent)
		{
			int2 next = cell + tangent;
			int2 across = next + normal;

			if (!IsSolid(next.x, next.y))
			{
				return ConvexRadius;
			}

			return IsSolid(across.x, across.y) ? ConcaveRadius : 0;
		}

		private void AddArcs(ref NativeList<FootprintArc> arcs)
		{
			for (int vz = 0; vz <= Depth; vz++)
			{
				for (int vx = 0; vx <= Width; vx++)
				{
					for (int i = 0; i < 4; i++)
					{
						int2 facing = new int2((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1);
						AddCornerArc(ref arcs, new int2(vx, vz), facing);
					}
				}
			}
		}

		private void AddCornerArc(ref NativeList<FootprintArc> arcs, int2 vertex, int2 facing)
		{
			int2 cell = vertex + math.min(facing, 0);
			bool solid = IsSolid(cell.x, cell.y);
			bool acrossX = IsSolid(cell.x - facing.x, cell.y);
			bool acrossZ = IsSolid(cell.x, cell.y - facing.y);
			bool opposite = IsSolid(cell.x - facing.x, cell.y - facing.y);

			float radius;
			if (solid && !acrossX && !acrossZ)
			{
				radius = ConvexRadius;
			}
			else if (!solid && acrossX && acrossZ && opposite)
			{
				radius = ConcaveRadius;
			}
			else
			{
				return;
			}

			if (radius <= 0)
			{
				return;
			}

			arcs.Add(new FootprintArc
			{
				Centre = (float2)vertex + (float2)facing * radius,
				Facing = facing,
				Radius = radius,
			});
		}

		/// <summary>Whether a point near a corner, in that corner quadrant's frame, lies outside the rounding circle.</summary>
		private static bool IsCutByArc(float2 inQuadrant, float radius)
		{
			return radius > 0 && math.all(inQuadrant < radius) &&
			       math.lengthsq(inQuadrant - radius) > radius * radius;
		}

		private static float SegmentDistance(float2 p, float2 a, float2 b)
		{
			float2 ab = b - a;
			float t = math.saturate(math.dot(p - a, ab) / math.dot(ab, ab));
			return math.distance(p, a + ab * t);
		}

		private static float ArcDistance(float2 p, FootprintArc arc)
		{
			float2 fromCentre = p - arc.Centre;

			// The arc spans the quarter facing back towards its grid vertex; past its ends the trimmed sides are nearer
			if (math.any(fromCentre * arc.Facing > 0))
			{
				return Far;
			}

			return math.abs(math.length(fromCentre) - arc.Radius);
		}

		#endregion
	}
}
