using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Tiles;

namespace Lawnmower.Model
{
	/// <summary>
	/// Runtime level: fresh <see cref="Tile"/> objects built from a <see cref="LevelDefinition"/>.
	/// Publicly read-only; tile state changes only through <see cref="LevelSimulation"/>.
	/// </summary>
	public sealed class Level
	{
		public Guid Id { get; }
		public int Width { get; }
		public int Depth { get; }
		public GridVector StartPosition { get; }

		private readonly Tile[] _tiles;

		private Level(Guid id, int width, int depth, GridVector startPosition, Tile[] tiles)
		{
			Id = id;
			Width = width;
			Depth = depth;
			StartPosition = startPosition;
			_tiles = tiles;
			LinkRabbitHoles();
		}

		public static Level FromDefinition(LevelDefinition definition)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			Tile[] tiles = definition.Tiles.Select(spec => TileFactory.Create(spec)).ToArray();
			return new Level(definition.Id, definition.Width, definition.Depth, definition.StartPosition, tiles);
		}

		/// <summary>The level's current state as a definition (grass heights as they are now).</summary>
		public LevelDefinition ToDefinition() =>
			new LevelDefinition(Id, Width, Depth, StartPosition, _tiles.Select(t => t.ToSpec()).ToArray());

		/// <summary>Deep copy of the current tile state, with no event subscribers.</summary>
		public Level Clone() => new Level(Id, Width, Depth, StartPosition, _tiles.Select(t => t.Clone()).ToArray());

		public bool Contains(GridVector p) => p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Depth;

		/// <summary>Null outside the level.</summary>
		public Tile GetTile(GridVector p) => Contains(p) ? _tiles[p.x + p.y * Width] : null;

		public Tile GetTile(int x, int y) => GetTile(new GridVector(x, y));

		/// <summary>Row-major (index = x + y * Width).</summary>
		public IReadOnlyList<Tile> Tiles => _tiles;

		public IEnumerable<GridVector> Positions
		{
			get
			{
				for (int y = 0; y < Depth; y++)
				{
					for (int x = 0; x < Width; x++)
					{
						yield return new GridVector(x, y);
					}
				}
			}
		}

		public bool IsComplete => _tiles.All(t => t.IsComplete);
		public bool IsRuined => _tiles.Any(t => t.IsRuined);

		/// <summary>Points each rabbit hole at the other hole of its pair. A pair without exactly two holes isn't linked.</summary>
		private void LinkRabbitHoles()
		{
			// The solver clones levels constantly, so keep the common no-holes case to one pass.
			Dictionary<int, List<int>> pairs = null;
			for (int i = 0; i < _tiles.Length; i++)
			{
				if (_tiles[i] is RabbitHoleTile hole)
				{
					pairs ??= new Dictionary<int, List<int>>();
					if (!pairs.TryGetValue(hole.Pair, out List<int> holes))
					{
						pairs.Add(hole.Pair, holes = new List<int>(2));
					}

					holes.Add(i);
				}
			}

			if (pairs == null)
			{
				return;
			}

			foreach (List<int> holes in pairs.Values)
			{
				if (holes.Count != 2)
				{
					continue;
				}

				((RabbitHoleTile)_tiles[holes[0]]).Partner = new GridVector(holes[1] % Width, holes[1] / Width);
				((RabbitHoleTile)_tiles[holes[1]]).Partner = new GridVector(holes[0] % Width, holes[0] / Width);
			}
		}
	}
}
