using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Lawnmower.Model
{
	/// <summary>Immutable level layout. Tiles are row-major: index = x + y * Width.</summary>
	public sealed class LevelDefinition : IEquatable<LevelDefinition>
	{
		public Guid Id { get; }
		public int Width { get; }
		public int Depth { get; }
		public GridVector StartPosition { get; }
		public IReadOnlyList<TileSpec> Tiles { get; }

		public TileSpec this[int x, int y] => Tiles[x + y * Width];
		public TileSpec this[GridVector p] => this[p.x, p.y];

		public LevelDefinition(Guid id, int width, int depth, GridVector startPosition, IReadOnlyList<TileSpec> tiles)
		{
			if (width < 1 || depth < 1)
			{
				throw new ArgumentException($"Level must be at least 1x1, got {width}x{depth}");
			}

			if (tiles == null || tiles.Count != width * depth)
			{
				throw new ArgumentException($"Expected {width * depth} tiles for {width}x{depth}, got {tiles?.Count ?? 0}");
			}

			if (startPosition.x < 0 || startPosition.y < 0 || startPosition.x >= width || startPosition.y >= depth)
			{
				throw new ArgumentException($"Start position {startPosition} is outside {width}x{depth}");
			}

			Id = id;
			Width = width;
			Depth = depth;
			StartPosition = startPosition;
			Tiles = tiles.ToArray();
		}

		public LevelDefinition WithStartPosition(GridVector startPosition) =>
			new LevelDefinition(Id, Width, Depth, startPosition, Tiles);

		public bool Contains(GridVector p) => p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Depth;

		public bool Equals(LevelDefinition other)
		{
			if (other is null)
			{
				return false;
			}

			return ReferenceEquals(this, other) ||
			       Id == other.Id && Width == other.Width && Depth == other.Depth &&
			       StartPosition == other.StartPosition && Tiles.SequenceEqual(other.Tiles);
		}

		public override bool Equals(object obj) => obj is LevelDefinition other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Id, Width, Depth, StartPosition);
	}
}
