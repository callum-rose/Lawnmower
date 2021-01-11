using UnityEngine;

namespace Game.Tiles
{
	internal class WaterTileObject : BaseTileObject<WaterTile>
	{
		private WaterTile _data;

		public override void Setup(WaterTile data)
		{
			_data = data;
		}

		public override void Dispose()
		{
            
		}
	}
}