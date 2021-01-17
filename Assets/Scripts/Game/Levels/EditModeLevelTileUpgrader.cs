using Core;
using Game.Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(EditModeLevelTileUpgrader), menuName = SONames.GameDir + nameof(EditModeLevelTileUpgrader))]
	internal class EditModeLevelTileUpgrader : ScriptableObject
	{
		[SerializeField, ListDrawerSettings(Expanded = true)] private TileUpgradePair[] tileUpgradePairs;
		
		private EditableLevelData _levelData;

		public void Init(EditableLevelData levelData)
		{
			_levelData = levelData;
		}
		
		public bool UpgradeIfPossible(GridVector position)
		{
			Tile tile = _levelData.GetTile(position);
			foreach (TileUpgradePair pair in tileUpgradePairs)
			{
				if (!pair.IsMatch(tile, out Tile tileToUpgradeTo))
				{
					continue;
				}

				_levelData.SetTile(position, tileToUpgradeTo);
				return true;
			}

			return false;
		}
	}
}