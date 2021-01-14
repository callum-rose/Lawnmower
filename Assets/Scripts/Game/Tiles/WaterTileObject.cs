using UnityEngine;

namespace Game.Tiles
{
	internal class WaterTileObject : BaseTileObject
	{
		private WaterTile _data;

		public override void Bind(Tile data)
		{
			_data = (WaterTile)data;
		}

		public override void Dispose()
		{
            
		}
	}
}