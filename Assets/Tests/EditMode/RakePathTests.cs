using System.Diagnostics;
using Game.Terrain;
using NUnit.Framework;

namespace Tests
{
	public class RakePathTests
	{
		private static readonly (int x, int z)[] Steps = { (1, 0), (0, 1), (-1, 0), (0, -1) };

		[Test]
		public void FullLawn_IsOnePathThroughEveryTile()
		{
			bool[,] rakeable = Fill(4, 4);

			var sides = RakePath.Build(rakeable);

			AssertJoined(rakeable, sides);
			Assert.AreEqual(2, Ends(sides), "one path has two ends");
		}

		[Test]
		public void LawnAroundAStone_IsOnePathThroughEveryTile()
		{
			bool[,] rakeable = Fill(5, 5);
			rakeable[2, 2] = false;

			var sides = RakePath.Build(rakeable);

			AssertJoined(rakeable, sides);
			Assert.AreEqual(2, Ends(sides));
			Assert.AreEqual((RakePath.None, RakePath.None), sides[2, 2], "the stone isn't raked");
		}

		[Test]
		public void SeparateLawns_GetAPathEach()
		{
			bool[,] rakeable = Fill(5, 2);
			rakeable[2, 0] = rakeable[2, 1] = false;

			var sides = RakePath.Build(rakeable);

			AssertJoined(rakeable, sides);
			Assert.AreEqual(4, Ends(sides));
		}

		[TestCase(4, 4)]
		[TestCase(3, 4)]
		[TestCase(3, 3)]
		public void FullLawn_TurnsAsOftenAsAnyPathThroughEveryTile(int width, int depth)
		{
			var sides = RakePath.Build(Fill(width, depth));

			Assert.AreEqual(MostTurns(width, depth), RakePath.Turns(sides));
		}

		[Test]
		public void LargestLawn_IsOnePathThroughEveryTile_Quickly()
		{
			bool[,] rakeable = Fill(6, 6);
			rakeable[1, 4] = rakeable[4, 2] = false;

			Stopwatch stopwatch = Stopwatch.StartNew();
			var sides = RakePath.Build(rakeable);
			stopwatch.Stop();

			TestContext.WriteLine($"{stopwatch.ElapsedMilliseconds} ms, {RakePath.Turns(sides)} turns");
			AssertJoined(rakeable, sides);
			Assert.AreEqual(2, Ends(sides));
			Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
		}

		[Test]
		public void LawnWithNoPathThroughEveryTile_IsALongPathAndALoneTile_Quickly()
		{
			// Both holes on the same chessboard colour: a path can't go through every tile
			bool[,] rakeable = Fill(6, 6);
			rakeable[1, 4] = rakeable[4, 1] = false;

			Stopwatch stopwatch = Stopwatch.StartNew();
			var sides = RakePath.Build(rakeable);
			stopwatch.Stop();

			TestContext.WriteLine($"{stopwatch.ElapsedMilliseconds} ms, {RakePath.Turns(sides)} turns");
			Assert.AreEqual(2, Ends(sides), "one path's ends");
			int alone = 0;
			for (int x = 0; x < 6; x++)
			{
				for (int z = 0; z < 6; z++)
				{
					alone += rakeable[x, z] && sides[x, z] == (RakePath.None, RakePath.None) ? 1 : 0;
				}
			}

			Assert.AreEqual(1, alone, "tiles left out of it");
			Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
		}

		[Test]
		public void LoneTile_HasNoSides()
		{
			var sides = RakePath.Build(new[,] { { true } });

			Assert.AreEqual((RakePath.None, RakePath.None), sides[0, 0]);
		}

		// Brute force: every path through every tile of a full lawn
		private static int MostTurns(int width, int depth)
		{
			bool[,] visited = new bool[width, depth];
			int most = -1;
			for (int x = 0; x < width; x++)
			{
				for (int z = 0; z < depth; z++)
				{
					Walk(x, z, -1, 0, 1);
				}
			}

			return most;

			void Walk(int x, int z, int heading, int turns, int length)
			{
				visited[x, z] = true;
				if (length == width * depth)
				{
					most = System.Math.Max(most, turns);
				}

				for (int side = 0; side < Steps.Length; side++)
				{
					int nx = x + Steps[side].x, nz = z + Steps[side].z;
					if (nx >= 0 && nz >= 0 && nx < width && nz < depth && !visited[nx, nz])
					{
						Walk(nx, nz, side, turns + (heading >= 0 && side != heading ? 1 : 0), length + 1);
					}
				}

				visited[x, z] = false;
			}
		}

		private static bool[,] Fill(int width, int depth)
		{
			bool[,] tiles = new bool[width, depth];
			for (int x = 0; x < width; x++)
			{
				for (int z = 0; z < depth; z++)
				{
					tiles[x, z] = true;
				}
			}

			return tiles;
		}

		private static int Ends((byte a, byte b)[,] sides)
		{
			int ends = 0;
			foreach ((byte a, byte b) tile in sides)
			{
				bool raked = tile.a != RakePath.None || tile.b != RakePath.None;
				ends += raked ? (tile.a == RakePath.None ? 1 : 0) + (tile.b == RakePath.None ? 1 : 0) : 0;
			}

			return ends;
		}

		// Every side a rakeable tile's grooves leave by leads into a rakeable tile whose grooves come back the same way
		private static void AssertJoined(bool[,] rakeable, (byte a, byte b)[,] sides)
		{
			for (int x = 0; x < rakeable.GetLength(0); x++)
			{
				for (int z = 0; z < rakeable.GetLength(1); z++)
				{
					if (!rakeable[x, z])
					{
						continue;
					}

					Assert.AreNotEqual(sides[x, z].a, sides[x, z].b, $"({x}, {z}) isn't on a path, or leaves twice by one side");
					foreach (byte side in new[] { sides[x, z].a, sides[x, z].b })
					{
						if (side == RakePath.None)
						{
							continue;
						}

						int nx = x + Steps[side].x, nz = z + Steps[side].z;
						Assert.IsTrue(rakeable[nx, nz], $"({x}, {z}) leads off the lawn");
						byte back = (byte)((side + 2) % 4);
						Assert.IsTrue(sides[nx, nz].a == back || sides[nx, nz].b == back,
							$"({x}, {z}) leads into ({nx}, {nz}), which doesn't lead back");
					}
				}
			}
		}
	}
}
