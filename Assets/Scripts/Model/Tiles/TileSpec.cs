using System;
using Game.Core;

namespace Lawnmower.Model
{
	/// <summary>
	/// Immutable description of one tile: its kind plus whatever state distinguishes it (grass
	/// height, stone direction, spring offset, rabbit hole pair). Used for level definitions and state snapshots.
	/// </summary>
	public readonly struct TileSpec : IEquatable<TileSpec>
	{
		public TileKind Kind { get; }

		/// <summary>Grass only.</summary>
		public int GrassHeight { get; }

		/// <summary>Stone only (0-3).</summary>
		public int StoneDirection { get; }

		/// <summary>Spring only: landing position relative to the spring's own cell.</summary>
		public GridVector SpringOffset { get; }

		/// <summary>Rabbit hole only: which pair it belongs to. The level links each hole to the other hole of its pair.</summary>
		public int HolePair { get; }

		private TileSpec(TileKind kind, int grassHeight = 0, int stoneDirection = 0, GridVector springOffset = default,
			int holePair = 0)
		{
			Kind = kind;
			GrassHeight = grassHeight;
			StoneDirection = stoneDirection;
			SpringOffset = springOffset;
			HolePair = holePair;
		}

		public static TileSpec Empty => new TileSpec(TileKind.Empty);
		public static TileSpec Water => new TileSpec(TileKind.Water);
		public static TileSpec Wood => new TileSpec(TileKind.Wood);
		public static TileSpec Grass(int height) => new TileSpec(TileKind.Grass, grassHeight: height);
		public static TileSpec Stone(int direction) => new TileSpec(TileKind.Stone, stoneDirection: ((direction % 4) + 4) % 4);
		public static TileSpec Spring(GridVector offset) => new TileSpec(TileKind.Spring, springOffset: offset);
		public static TileSpec RabbitHole(int pair) => new TileSpec(TileKind.RabbitHole, holePair: pair);

		public bool Equals(TileSpec other) =>
			Kind == other.Kind && GrassHeight == other.GrassHeight && StoneDirection == other.StoneDirection &&
			SpringOffset.Equals(other.SpringOffset) && HolePair == other.HolePair;

		public override bool Equals(object obj) => obj is TileSpec other && Equals(other);

		public override int GetHashCode() => HashCode.Combine((int)Kind, GrassHeight, StoneDirection, SpringOffset, HolePair);

		public static bool operator ==(TileSpec a, TileSpec b) => a.Equals(b);
		public static bool operator !=(TileSpec a, TileSpec b) => !a.Equals(b);

		public override string ToString() => Kind switch
		{
			TileKind.Grass => $"grass:{GrassHeight}",
			TileKind.Stone => $"stone:{StoneDirection}",
			TileKind.Spring => $"spring:{SpringOffset.x},{SpringOffset.y}",
			TileKind.RabbitHole => $"hole:{HolePair}",
			_ => Kind.ToString().ToLowerInvariant()
		};
	}
}
