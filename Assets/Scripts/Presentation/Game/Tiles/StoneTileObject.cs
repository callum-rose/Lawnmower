
using UnityEngine;

namespace Game.Tiles
{
	internal class StoneTileObject : BaseTileObject
	{
		private IReadonlyStoneTile _data;
		
		public override void Bind(IReadonlyTile data)
		{
			_data = (IReadonlyStoneTile)data;
			
			transform.rotation *= Quaternion.AngleAxis(_data.Direction * 90, Vector3.up);
		}

		public override void Dispose()
		{
            
		}
	}
}