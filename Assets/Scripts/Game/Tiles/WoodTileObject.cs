using UnityEngine;

namespace Game.Tiles
{
	internal class WoodTileObject : BaseTileObject<WoodTile>
	{
		private WoodTile _data;

		public override void Setup(WoodTile data)
		{
			_data = data;
		}

		public override void Dispose()
		{
            
		}
	}
}