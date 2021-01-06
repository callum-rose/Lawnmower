using System;
using Core;
using Game.Core;
using Game.Levels.Editorr;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(EditModeLevelTraversalChecker), menuName = SONames.GameDir + nameof(EditModeLevelTraversalChecker))]
	internal class EditModeLevelTraversalChecker : BaseLevelTraversalChecker
	{
		[SerializeField] private LevelConverter levelConverter;
		[SerializeField] private TileTraversalUpgrader tileTraversalUpgrader;
		
		#region API

		public override CheckValue CanTraverseTo(GridVector position)
		{
			if (_tiles == null)
			{
				throw new NullReferenceException("Tiles object is null");
			}

			if (position.x < 0 || position.y < 0 || position.x >= _tiles.Width || position.y >= _tiles.Depth)
			{
				return CheckValue.OutOfBounds;
			}
			
			Tile tile = _tiles.GetTile(position);
			TileData tileData = levelConverter.ConvertTileToData(tile);
			if (tileTraversalUpgrader.CanUpgradeTile(tileData, out var newData))
			{
				
			}
			
			return tile.IsTraversable(IsEditMode) ? CheckValue.Yes : CheckValue.NonTraversableTile;
		}

		#endregion
	}
}