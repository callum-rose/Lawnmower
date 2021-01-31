using Core;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(EditModeLevelTileUpgrader), menuName = SONames.GameDir + nameof(EditModeLevelTileUpgrader))]
	internal class EditModeLevelTileUpgrader : ScriptableObject
	{
		[SerializeField, ListDrawerSettings(Expanded = true)] private TileUpgradePair[] tileUpgradePairs;
		
		private EditableLevelData _levelData;
		private IUndoSystem _undoSystem;

		public void Init(EditableLevelData levelData, IUndoSystem undoSystem)
		{
			_levelData = levelData;
			_undoSystem = undoSystem;
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

				Undoable undoable = new Undoable(
					() => _levelData.SetTile(position, tileToUpgradeTo),
					() => _levelData.SetTile(position, tile));
				_undoSystem.Do(undoable);
				
				return true;
			}

			return false;
		}
	}
}