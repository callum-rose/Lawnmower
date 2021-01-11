using UnityEngine;

namespace Game.Tiles
{
	internal class StoneTileObject : BaseTileObject<StoneTile>
	{
		private StoneTile _data;

		public override void Setup(StoneTile data)
		{
			_data = data;
		}

		public override void Dispose()
		{
            
		}
	}
}