using UnityEngine;

namespace Game.Tiles
{
	internal class EmptyTileObject : BaseTileObject<EmptyTile>
	{
		private EmptyTile _data;

		public override void Setup(EmptyTile data)
		{
			_data = data;
		}

		public override void Dispose()
		{
            
		}
	}
}