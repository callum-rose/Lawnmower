using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// Seeded generator of small valid levels: 2x2 to 5x5, grass 1-3, stone, water, empty and wood,
	/// plus 0-2 springs whose landings are in bounds and never on another spring (so no cycles).
	/// The start is always on grass or wood.
	/// </summary>
	public static class RandomLevels
	{
		public static LevelDefinition Generate(int seed)
		{
			Random random = new Random(seed);
			int width = random.Next(2, 6);
			int depth = random.Next(2, 6);
			TileSpec[] tiles = new TileSpec[width * depth];

			for (int i = 0; i < tiles.Length; i++)
			{
				int roll = random.Next(100);
				tiles[i] = roll < 15 ? TileSpec.Grass(1)
					: roll < 55 ? TileSpec.Grass(2)
					: roll < 68 ? TileSpec.Grass(3)
					: roll < 76 ? TileSpec.Wood
					: roll < 84 ? TileSpec.Stone(random.Next(4))
					: roll < 92 ? TileSpec.Water
					: TileSpec.Empty;
			}

			int startIndex = random.Next(tiles.Length);
			if (tiles[startIndex].Kind != TileKind.Grass && tiles[startIndex].Kind != TileKind.Wood)
			{
				tiles[startIndex] = TileSpec.Grass(random.Next(1, 4));
			}

			int springs = random.Next(3);
			List<int> candidates = Enumerable.Range(0, tiles.Length).Where(i => i != startIndex).ToList();
			HashSet<int> springCells = new HashSet<int>();
			for (int s = 0; s < springs && candidates.Count > 1; s++)
			{
				int cell = candidates[random.Next(candidates.Count)];
				candidates.Remove(cell);
				springCells.Add(cell);
			}

			foreach (int cell in springCells)
			{
				List<int> landings = Enumerable.Range(0, tiles.Length).Where(i => i != cell && !springCells.Contains(i)).ToList();
				int landing = landings[random.Next(landings.Count)];
				GridVector offset = new GridVector(landing % width - cell % width, landing / width - cell / width);
				tiles[cell] = TileSpec.Spring(offset);
			}

			GridVector start = new GridVector(startIndex % width, startIndex / width);
			return new LevelDefinition(new Guid(seed, 0, 0, new byte[8]), width, depth, start, tiles);
		}

		/// <summary>Seeded sequence of 0..maxLength input directions.</summary>
		public static GridVector[] Moves(int seed, int maxLength)
		{
			Random random = new Random(seed * 7919 + 17);
			return Enumerable.Range(0, random.Next(maxLength + 1))
				.Select(_ => Sim.Directions[random.Next(4)])
				.ToArray();
		}
	}
}
