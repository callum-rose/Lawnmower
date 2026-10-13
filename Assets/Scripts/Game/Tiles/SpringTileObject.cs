using Game.Core;
using UnityEngine;

namespace Game.Tiles
{
	internal class SpringTileObject : BaseTileObject
	{
		private IReadonlySpringTile _data;

		private void OnDrawGizmos()
		{
			// Nothing to draw until the view has been bound to a spring tile.
			if (_data == null)
			{
				return;
			}

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.position, transform.position + _data.Offset.ToXZ() * Game.Levels.LevelDimensions.TileSize);
		}

		public override void Bind(IReadonlyTile data)
		{
			_data = (IReadonlySpringTile) data;
		}

		public override void Dispose()
		{
			
		}
	}
}
