using UnityEngine;

namespace Game.Tiles
{
	internal class WoodTileObject : BaseTileObject
	{
		private WoodTile _data;

		public override void Bind(Tile data)
		{
			_data = (WoodTile)data;
		}

		public override void Dispose()
		{
            
		}
	}
}