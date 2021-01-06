using Core;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels.Editorr
{
	[CreateAssetMenu(fileName = nameof(TileTraversalUpgrader), menuName = SONames.GameDir + nameof(TileTraversalUpgrader))]
	internal class TileTraversalUpgrader : ScriptableObject
	{
		public bool CanUpgradeTile(TileData tileData, out TileData upgradeToTile)
		{
			switch (tileData.Type)
			{
				case TileType.Grass:
					var data = tileData.Data as GrassTileSetupData;
					if (data.grassHeight == GrassTile.MaxGrassHeight)
					{
						upgradeToTile = new TileData { Type = TileType.Wood };
						return true;
					}
					else
					{
						upgradeToTile = default;
						return false;
					}
				default:
					upgradeToTile = default;
					return false;
			}
		}
	}
}