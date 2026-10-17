using Game.Core;
using UnityEngine;

namespace Game.Tiles
{
	internal class SpringTileObject : BaseTileObject, IStandSurface
	{
		[Tooltip("Height of the top of the spring model, where the mower stands before it launches.")]
		[SerializeField, Min(0)] private float standHeight = 1f;
		[Tooltip("Marks the landing tile; spawned at the spring's offset.")]
		[SerializeField] private GameObject landingMarkerPrefab;

		private IReadonlySpringTile _data;

		public float StandHeight => standHeight;

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

			if (landingMarkerPrefab)
			{
				// A child, so it moves with the tile's intro and goes when the tile is destroyed.
				GameObject marker = Instantiate(landingMarkerPrefab, transform);
				marker.transform.localPosition = _data.Offset.ToXZ() * Game.Levels.LevelDimensions.TileSize;
			}
		}

		public override void Dispose()
		{

		}
	}
}
