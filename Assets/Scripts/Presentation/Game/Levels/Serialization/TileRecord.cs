using System;
using Game.Core;
using Lawnmower.Model;

namespace Game.Levels.Serialization
{
	/// <summary>
	/// Unity-serialized, type-name-free representation of one tile (see <see cref="TileSpec"/>).
	/// Only the fields relevant to <see cref="kind"/> are meaningful; the rest stay at their defaults.
	/// </summary>
	[Serializable]
	internal struct TileRecord : IEquatable<TileRecord>
	{
		public TileKind kind;

		/// <summary>Grass only.</summary>
		public int grassHeight;

		/// <summary>Stone only (0-3).</summary>
		public int stoneDirection;

		/// <summary>Spring only: landing position relative to the spring's own cell.</summary>
		public GridVector springOffset;

		/// <summary>Rabbit hole only: which pair it belongs to.</summary>
		public int holePair;

		public bool Equals(TileRecord other) =>
			kind == other.kind && grassHeight == other.grassHeight && stoneDirection == other.stoneDirection &&
			springOffset.Equals(other.springOffset) && holePair == other.holePair;

		public override bool Equals(object obj) => obj is TileRecord other && Equals(other);

		public override int GetHashCode() => HashCode.Combine((int)kind, grassHeight, stoneDirection, springOffset, holePair);

		public override string ToString() => kind switch
		{
			TileKind.Grass => $"Grass:{grassHeight}",
			TileKind.Stone => $"Stone:{stoneDirection}",
			TileKind.Spring => $"Spring:{springOffset}",
			TileKind.RabbitHole => $"RabbitHole:{holePair}",
			_ => kind.ToString()
		};
	}
}
