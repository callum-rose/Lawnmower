using System;
using Game.Tiles;

namespace Game.Levels
{
	internal static class TEMP_TileDataToTileeConverter
	{
		public static Tile GetTilee(TileData data)
		{
			switch (data.Type)
			{
				case TileType.Empty:
					return new EmptyTile();
				case TileType.Grass:
					if (data.Data == null)
					{
						return new GrassTile(1);
					}
					int grassHeight = ((GrassTileSetupData) data.Data).grassHeight;
					return new GrassTile(grassHeight);
				case TileType.Stone:
					return new StoneTile();
				case TileType.Water:
					return new WaterTile();
				case TileType.Wood:
					return new WoodTile();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}