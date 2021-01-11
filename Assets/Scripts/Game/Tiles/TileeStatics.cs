namespace Game.Tiles
{
	internal static class TileeStatics
	{
		public static readonly Tilee[] AllTileConfigurations =
		{
			new EmptyTile(),
			new GrassTile(1),
			new GrassTile(2),
			new GrassTile(3),
			new StoneTile(),
			new WaterTile(),
			new WoodTile()
		};
	}
}