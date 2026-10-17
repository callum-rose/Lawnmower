using System.Collections.Generic;

namespace Game.Terrain
{
	/// <summary>
	/// The route a rake took through a level's dirt: as few continuous paths as it can find through the rakeable tiles,
	/// each tile visited once, turning as often as it can so there are few long straight runs. Each tile is entered and
	/// left through two of its sides. The terrain shader draws grooves that follow these sides (straight across, arcs
	/// round a corner, a U-turn where a path ends), so they run on unbroken from tile to tile. Cell (x, z) covers
	/// [x, x + 1] x [z, z + 1].
	/// </summary>
	internal static class RakePath
	{
		/// <summary>Sides of a tile, by the direction they face: +x, +z, -x, -z. <see cref="None"/> ends a path.</summary>
		public const byte PositiveX = 0, PositiveZ = 1, NegativeX = 2, NegativeZ = 3, None = 4;

		private static readonly (int x, int z)[] Steps = { (1, 0), (0, 1), (-1, 0), (0, -1) };

		// Tiles visited in the search for each path, shared between its starts. Enough for a 6x6 level to find a path
		// through every tile and trade it up for one with more turns (about 150 ms in the editor); a bigger budget found
		// no more turns there. The best found so far is kept when it runs out
		private const int SearchBudget = 40000;
		// For the patches that turn out to have no path through every tile, which the first search can't tell
		private const int FallbackBudget = 20000;

		#region API

		/// <summary>The two sides each tile's grooves leave by, <see cref="None"/> for both on unrakeable tiles.</summary>
		public static (byte a, byte b)[,] Build(bool[,] rakeable)
		{
			int width = rakeable.GetLength(0), depth = rakeable.GetLength(1);
			var sides = new (byte a, byte b)[width, depth];
			var remaining = (bool[,])rakeable.Clone();

			for (int x = 0; x < width; x++)
			{
				for (int z = 0; z < depth; z++)
				{
					sides[x, z] = (None, None);
				}
			}

			while (true)
			{
				List<(int x, int z)> tiles = Tiles(remaining);
				if (tiles.Count == 0)
				{
					break;
				}

				// A connected patch at a time: a path can't cross between them
				bool[,] patch = Patch(remaining, tiles[0]);
				List<(int x, int z)> path = BestPath(patch);
				for (int i = 0; i < path.Count; i++)
				{
					byte from = i > 0 ? SideTowards(path[i], path[i - 1]) : None;
					byte to = i < path.Count - 1 ? SideTowards(path[i], path[i + 1]) : None;
					sides[path[i].x, path[i].z] = (from, to);
					remaining[path[i].x, path[i].z] = false;
				}
			}

			return sides;
		}

		/// <summary>How many tiles of a route its path turns on.</summary>
		public static int Turns((byte a, byte b)[,] sides)
		{
			int turns = 0;
			foreach ((byte a, byte b) in sides)
			{
				turns += a != None && b != None && (a ^ b) != 2 ? 1 : 0;
			}

			return turns;
		}

		#endregion

		#region Methods

		private static List<(int x, int z)> BestPath(bool[,] patch)
		{
			List<(int x, int z)> tiles = Tiles(patch);

			// Most hemmed-in first: a path through everything has to start or end at a dead end
			tiles.Sort((p, q) => Exits(patch, p).CompareTo(Exits(patch, q)));

			if (CouldGoThroughEveryTile(tiles))
			{
				Search search = Run(new Search(patch, tiles.Count, true), tiles, SearchBudget);
				if (search.Best.Count == tiles.Count)
				{
					return search.Best;
				}
			}

			return Run(new Search(patch, tiles.Count, false), tiles, FallbackBudget).Best;
		}

		// A path alternates between tiles coloured as on a chessboard, so it can only go through every tile if the two
		// colours' counts differ by at most one
		private static bool CouldGoThroughEveryTile(List<(int x, int z)> tiles)
		{
			int balance = 0;
			foreach ((int x, int z) in tiles)
			{
				balance += (x + z) % 2 == 0 ? 1 : -1;
			}

			return System.Math.Abs(balance) <= 1;
		}

		// Each start gets an even share of what the ones before it left over
		private static Search Run(Search search, List<(int x, int z)> starts, int budget)
		{
			for (int i = 0; i < starts.Count && budget > 0; i++)
			{
				budget -= search.Start(starts[i], budget / (starts.Count - i));
			}

			return search;
		}

		private static List<(int x, int z)> Tiles(bool[,] open)
		{
			List<(int x, int z)> tiles = new List<(int x, int z)>();
			for (int z = 0; z < open.GetLength(1); z++)
			{
				for (int x = 0; x < open.GetLength(0); x++)
				{
					if (open[x, z])
					{
						tiles.Add((x, z));
					}
				}
			}

			return tiles;
		}

		private static bool[,] Patch(bool[,] open, (int x, int z) seed)
		{
			bool[,] patch = new bool[open.GetLength(0), open.GetLength(1)];
			Stack<(int x, int z)> stack = new Stack<(int x, int z)>();
			patch[seed.x, seed.z] = true;
			stack.Push(seed);
			while (stack.Count > 0)
			{
				(int x, int z) tile = stack.Pop();
				foreach ((int x, int z) step in Steps)
				{
					(int x, int z) next = (tile.x + step.x, tile.z + step.z);
					if (IsOpen(open, next.x, next.z) && !patch[next.x, next.z])
					{
						patch[next.x, next.z] = true;
						stack.Push(next);
					}
				}
			}

			return patch;
		}

		private static int Exits(bool[,] open, (int x, int z) tile)
		{
			int exits = 0;
			foreach ((int x, int z) step in Steps)
			{
				exits += IsOpen(open, tile.x + step.x, tile.z + step.z) ? 1 : 0;
			}

			return exits;
		}

		private static bool IsOpen(bool[,] open, int x, int z)
		{
			return x >= 0 && z >= 0 && x < open.GetLength(0) && z < open.GetLength(1) && open[x, z];
		}

		private static byte SideTowards((int x, int z) from, (int x, int z) to)
		{
			for (byte side = 0; side < Steps.Length; side++)
			{
				if (from.x + Steps[side].x == to.x && from.z + Steps[side].z == to.z)
				{
					return side;
				}
			}

			return None;
		}

		#endregion

		/// <summary>
		/// Depth-first branch and bound for the longest path through a connected patch, and among the longest the one
		/// that turns most. Turning moves are tried first. Branches that can't beat the best turns are dropped (each move
		/// left can add at most one), and when looking only for paths through every tile, so are branches that rule one
		/// out: a tile cut off, a second dead end, or the rest split in two.
		/// </summary>
		private class Search
		{
			public List<(int x, int z)> Best = new List<(int x, int z)>();

			private readonly bool[,] _open;
			private readonly int _count;
			private readonly bool _throughEveryTile;
			private readonly List<(int x, int z)> _path = new List<(int x, int z)>();
			private readonly Stack<(int x, int z)> _flood = new Stack<(int x, int z)>();
			private readonly bool[,] _reached;
			private int _bestTurns = -1;
			private int _budget;

			public Search(bool[,] patch, int count, bool throughEveryTile)
			{
				_open = (bool[,])patch.Clone();
				_reached = new bool[patch.GetLength(0), patch.GetLength(1)];
				_count = count;
				_throughEveryTile = throughEveryTile;
			}

			/// <returns>How much of the budget it used.</returns>
			public int Start((int x, int z) tile, int budget)
			{
				_budget = budget;
				Walk(tile, -1, 0);
				return budget - System.Math.Max(_budget, 0);
			}

			/// <returns>Whether to stop: the budget ran out.</returns>
			private bool Walk((int x, int z) tile, int heading, int turns)
			{
				if (--_budget < 0)
				{
					return true;
				}

				_open[tile.x, tile.z] = false;
				_path.Add(tile);

				if (_path.Count > Best.Count || (_path.Count == Best.Count && turns > _bestTurns))
				{
					Best = new List<(int x, int z)>(_path);
					_bestTurns = turns;
				}

				bool stop = false;
				int left = _count - _path.Count;
				bool canBeBest = Best.Count < _count || turns + left > _bestTurns;
				if (left > 0 && canBeBest && (!_throughEveryTile || CanCoverTheRest(tile)))
				{
					List<(int side, bool turn, int exits)> nexts = new List<(int side, bool turn, int exits)>(Steps.Length);
					for (int side = 0; side < Steps.Length; side++)
					{
						(int x, int z) next = (tile.x + Steps[side].x, tile.z + Steps[side].z);
						if (IsOpen(_open, next.x, next.z))
						{
							nexts.Add((side, heading >= 0 && side != heading, Exits(_open, next)));
						}
					}

					// Turns first, then the most hemmed-in tile (Warnsdorff's rule), which finds full paths quickly
					nexts.Sort((p, q) => p.turn != q.turn ? q.turn.CompareTo(p.turn) : p.exits.CompareTo(q.exits));

					foreach ((int side, bool turn, int _) in nexts)
					{
						if (Walk((tile.x + Steps[side].x, tile.z + Steps[side].z), side, turns + (turn ? 1 : 0)))
						{
							stop = true;
							break;
						}
					}
				}

				_open[tile.x, tile.z] = true;
				_path.RemoveAt(_path.Count - 1);
				return stop;
			}

			// Whether a path from tile could still go through every open tile: none cut off, at most one dead end (where
			// it would finish), and all of them reachable from here
			private bool CanCoverTheRest((int x, int z) tile)
			{
				int deadEnds = 0;
				for (int x = 0; x < _open.GetLength(0); x++)
				{
					for (int z = 0; z < _open.GetLength(1); z++)
					{
						_reached[x, z] = false;
						if (!_open[x, z])
						{
							continue;
						}

						int exits = Exits(_open, (x, z)) + (System.Math.Abs(x - tile.x) + System.Math.Abs(z - tile.z) == 1 ? 1 : 0);
						if (exits == 0 || (exits == 1 && ++deadEnds > 1))
						{
							return false;
						}
					}
				}

				int reached = 0;
				_flood.Push(tile);
				while (_flood.Count > 0)
				{
					(int x, int z) current = _flood.Pop();
					foreach ((int x, int z) step in Steps)
					{
						(int x, int z) next = (current.x + step.x, current.z + step.z);
						if (IsOpen(_open, next.x, next.z) && !_reached[next.x, next.z])
						{
							_reached[next.x, next.z] = true;
							reached++;
							_flood.Push(next);
						}
					}
				}

				return reached == _count - _path.Count;
			}
		}
	}
}
