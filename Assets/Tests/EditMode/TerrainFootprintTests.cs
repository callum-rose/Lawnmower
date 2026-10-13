using Game.Terrain;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Tests
{
	public class TerrainFootprintTests
	{
		private const float Tolerance = 1e-4f;
		private const float Convex = 0.3f;
		private const float Concave = 0.2f;

		private TerrainFootprint _footprint;

		[TearDown]
		public void TearDown()
		{
			_footprint.Dispose();
		}

		[Test]
		public void SingleTile_IsExactAlongItsSides()
		{
			Create(new[,] { { true } });

			Assert.AreEqual(-0.5f, _footprint.Distance(new float2(0.5f, 0.5f)), Tolerance, "centre");
			Assert.AreEqual(0, _footprint.Distance(new float2(1, 0.5f)), Tolerance, "on a side");
			Assert.AreEqual(0.25f, _footprint.Distance(new float2(1.25f, 0.5f)), Tolerance, "outside a side");
		}

		[Test]
		public void ConvexCorner_IsRoundedByTheConvexRadius()
		{
			Create(new[,] { { true } });

			// Outside the rounded corner along its diagonal: distance to the arc, not to the sharp corner
			float2 centre = new float2(1 - Convex, 1 - Convex);
			float2 diagonal = math.normalize(new float2(1, 1));
			float2 p = centre + diagonal * (Convex + 0.1f);
			Assert.AreEqual(0.1f, _footprint.Distance(p), Tolerance);

			// The sharp corner itself is now outside
			Assert.Greater(_footprint.Distance(new float2(0.99f, 0.99f)), 0);
		}

		[Test]
		public void ConcaveCorner_IsFilledByTheConcaveRadius()
		{
			// An L of three tiles: the empty (1, 1) tile has a concave corner at vertex (1, 1)
			Create(new[,] { { true, true }, { true, false } });

			Assert.Less(_footprint.Distance(new float2(1.01f, 1.01f)), 0, "just inside the empty tile's corner is filled");
			Assert.Greater(_footprint.Distance(new float2(1 + Concave, 1 + Concave)), 0, "the fillet ends within the radius");

			float2 centre = new float2(1 + Concave, 1 + Concave);
			float2 towardsVertex = -math.normalize(new float2(1, 1));
			float2 onFillet = centre + towardsVertex * Concave;
			Assert.AreEqual(0, _footprint.Distance(onFillet), Tolerance);
		}

		[Test]
		public void StraightEdgeBetweenTiles_HasNoSeam()
		{
			Create(new[,] { { true }, { true } });

			Assert.AreEqual(-0.5f, _footprint.Distance(new float2(1, 0.5f)), Tolerance, "the shared side is interior");
			Assert.AreEqual(0.2f, _footprint.Distance(new float2(1, 1.2f)), Tolerance, "the outer side runs straight past it");
		}

		[Test]
		public void DiagonalTiles_DoNotJoin()
		{
			Create(new[,] { { true, false }, { false, true } });

			Assert.Greater(_footprint.Distance(new float2(1, 1)), 0);
		}

		[Test]
		public void Distance_IsContinuousAroundEveryCorner()
		{
			Create(new[,] { { true, true, false }, { true, false, true }, { true, true, true } });

			const float step = 0.02f;
			for (float x = -0.5f; x < 3.5f; x += step)
			{
				for (float z = -0.5f; z < 3.5f; z += step)
				{
					float d = _footprint.Distance(new float2(x, z));
					float dx = _footprint.Distance(new float2(x + step, z));
					float dz = _footprint.Distance(new float2(x, z + step));

					// A distance field changes no faster than the distance moved
					Assert.LessOrEqual(math.abs(dx - d), step + Tolerance, $"at ({x}, {z}) along x");
					Assert.LessOrEqual(math.abs(dz - d), step + Tolerance, $"at ({x}, {z}) along z");
				}
			}
		}

		private void Create(bool[,] solid)
		{
			_footprint = TerrainFootprint.Create(solid, Convex, Concave, Allocator.Temp);
		}
	}
}
