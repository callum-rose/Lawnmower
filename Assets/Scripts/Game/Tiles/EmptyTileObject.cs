using UnityEngine;

namespace Game.Tiles
{
	internal class EmptyTileObject : BaseTileObject
	{
		private EmptyTile _data;

		public override void Bind(Tile data)
		{
			_data = (EmptyTile)data;
		}

		public override void Dispose()
		{
            
		}
	}
}