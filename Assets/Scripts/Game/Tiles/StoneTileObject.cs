
using UnityEngine;

namespace Game.Tiles
{
	internal class StoneTileObject : BaseTileObject
	{
		private StoneTile _data;
		
		public override void Bind(Tile data)
		{
			_data = (StoneTile)data;
			
			transform.rotation *= Quaternion.AngleAxis(_data.Direction * 90, Vector3.up);
		}

		public override void Dispose()
		{
            
		}
	}
}