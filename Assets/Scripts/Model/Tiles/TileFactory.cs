using System;
using System.Collections.Generic;
using Game.Core;
using Game.Tiles;

namespace Lawnmower.Model
{
	public static class TileFactory
	{
		public static Tile Create(in TileSpec spec)
		{
			switch (spec.Kind)
			{
				case TileKind.Empty:
					return new EmptyTile();
				case TileKind.Grass:
					return new GrassTile(spec.GrassHeight);
				case TileKind.Stone:
					return new StoneTile(spec.StoneDirection);
				case TileKind.Water:
					return new WaterTile();
				case TileKind.Wood:
					return new WoodTile();
				case TileKind.Spring:
					return new SpringTile(spec.SpringOffset);
				default:
					throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, "Unknown tile kind");
			}
		}
	}

	/// <summary>Every distinct tile configuration a level can contain (springs use a placeholder offset).</summary>
	public static class TileCatalog
	{
		public static IReadOnlyList<TileSpec> All { get; } = new[]
		{
			TileSpec.Empty,
			TileSpec.Grass(1),
			TileSpec.Grass(2),
			TileSpec.Grass(3),
			TileSpec.Stone(0),
			TileSpec.Water,
			TileSpec.Wood,
			TileSpec.Spring(GridVector.Up * 2)
		};
	}
}
