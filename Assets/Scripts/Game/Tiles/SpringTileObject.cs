using Game.Core;
using UnityEngine;

namespace Game.Tiles
{
	internal class SpringTileObject : BaseTileObject
	{
		private IReadonlySpringTile _data;
		private Positioner _positioner;

		private void OnDrawGizmos()
		{
			// Nothing to draw until the view has been bound to a spring tile.
			if (_data == null)
			{
				return;
			}

			if (_positioner == null)
			{
				_positioner = FindAnyObjectByType<Positioner>();

				if (_positioner == null)
				{
					return;
				}
			}

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.position, _positioner.GetWorldPosition(_data.LandingPosition));
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
