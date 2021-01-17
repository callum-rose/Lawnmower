namespace Game.Tiles
{
	internal class StoneTileObject : BaseTileObject
	{
		private StoneTile _data;

		public override void Bind(Tile data)
		{
			_data = (StoneTile)data;
		}

		public override void Dispose()
		{
            
		}
	}
}