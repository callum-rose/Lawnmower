using System;
using Game.Core;

namespace Game.Levels.Serialization
{
	/// <summary>
	/// Unity-serialized, type-name-free representation of one <see cref="Game.Tiles.Tile"/>.
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

		/// <summary>Spring only.</summary>
		public GridVector springLanding;

		public bool Equals(TileRecord other) =>
			kind == other.kind && grassHeight == other.grassHeight && stoneDirection == other.stoneDirection &&
			springLanding.Equals(other.springLanding);

		public override bool Equals(object obj) => obj is TileRecord other && Equals(other);

		public override int GetHashCode() => HashCode.Combine((int)kind, grassHeight, stoneDirection, springLanding);

		public override string ToString() => kind switch
		{
			TileKind.Grass => $"Grass:{grassHeight}",
			TileKind.Stone => $"Stone:{stoneDirection}",
			TileKind.Spring => $"Spring:{springLanding}",
			_ => kind.ToString()
		};
	}
}
