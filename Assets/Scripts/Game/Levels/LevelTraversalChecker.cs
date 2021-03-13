using System;
using Core;
using Game.Core;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelTraversalChecker), menuName = SoNames.GameDir + nameof(LevelTraversalChecker))]
	internal class LevelTraversalChecker : ScriptableObject, ILevelTraversalChecker
	{
		private IReadOnlyLevelData _levelData;

		public void Init(IReadOnlyLevelData levelData)
		{
			_levelData = levelData;
		}

		public TileTraversalStatus CanTraverseTo(GridVector position)
		{
			if (_levelData == null)
			{
				throw new NullReferenceException("Tiles object is null");
			}

			if (position.x < 0 || position.y < 0 || position.x >= _levelData.Width || position.y >= _levelData.Depth)
			{
				return TileTraversalStatus.OutOfBounds;
			}

			Tile tile = _levelData.GetTile(position);
			return tile.IsTraversable(false) ? TileTraversalStatus.Yes : TileTraversalStatus.NonTraversable;
		}
	}
}