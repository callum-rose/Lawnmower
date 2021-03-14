using System;
using Game.Core;
using UnityEngine;

namespace Game.Tiles
{
	internal class SpringTileObject : BaseTileObject
	{
		private IReadonlySpringTile _data;

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.position, FindObjectOfType<Positioner>().GetWorldPosition(_data.LandingPosition));
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