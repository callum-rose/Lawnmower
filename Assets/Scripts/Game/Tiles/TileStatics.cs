namespace Game.Tiles
{
	internal static class TileStatics
	{
		public static readonly Tile[] AllTileConfigurations =
		{
			new EmptyTile(),
			new GrassTile(0),
			new GrassTile(1),
			new GrassTile(2),
			new GrassTile(3),
			new StoneTile(),
			new WaterTile(),
			new WoodTile()
		};
	}
}