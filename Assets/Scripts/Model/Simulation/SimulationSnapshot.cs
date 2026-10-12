using System;
using System.Linq;
using Game.Core;

namespace Lawnmower.Model
{
	/// <summary>
	/// Immutable copy of a simulation's mower position, status and tile state (no history, no
	/// events). The hash is precomputed and only valid within one process.
	/// </summary>
	public sealed class SimulationSnapshot : IEquatable<SimulationSnapshot>
	{
		public GridVector MowerPosition { get; }
		public LevelStatus Status { get; }

		private readonly TileSpec[] _tiles;
		private readonly int _hash;

		internal SimulationSnapshot(GridVector mowerPosition, LevelStatus status, Level level)
		{
			MowerPosition = mowerPosition;
			Status = status;
			_tiles = level.Tiles.Select(t => t.ToSpec()).ToArray();

			HashCode hash = new HashCode();
			hash.Add(mowerPosition);
			hash.Add(status);
			foreach (TileSpec tile in _tiles)
			{
				hash.Add(tile);
			}

			_hash = hash.ToHashCode();
		}

		/// <summary>Row-major tile state.</summary>
		public TileSpec GetTile(int index) => _tiles[index];

		public int TileCount => _tiles.Length;

		public bool Equals(SimulationSnapshot other)
		{
			if (other is null)
			{
				return false;
			}

			return ReferenceEquals(this, other) ||
			       _hash == other._hash && MowerPosition == other.MowerPosition && Status == other.Status &&
			       _tiles.SequenceEqual(other._tiles);
		}

		public override bool Equals(object obj) => obj is SimulationSnapshot other && Equals(other);

		public override int GetHashCode() => _hash;
	}
}
