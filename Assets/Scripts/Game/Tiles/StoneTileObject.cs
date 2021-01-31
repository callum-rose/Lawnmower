
using UnityEngine;

namespace Game.Tiles
{
	internal class StoneTileObject : BaseTileObject
	{
		private StoneTile _data;
		
		public override void Bind(Tile data)
		{
			_data = (StoneTile)data;
			
			Random.InitState(transform.position.GetHashCode());
			int randomDirection = Random.Range(0, 4);
			transform.rotation *= Quaternion.AngleAxis(randomDirection * 90, Vector3.up);
		}

		public override void Dispose()
		{
            
		}
	}
}