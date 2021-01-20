using System;
using Core;
using Game.Core;
using Game.Levels.Editorr;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(EditModeLevelTraversalChecker),
		menuName = SONames.GameDir + nameof(EditModeLevelTraversalChecker))]
	internal class EditModeLevelTraversalChecker : ScriptableObject, ILevelTraversalChecker, IHasEditMode
	{
		[SerializeField] private LevelTraversalChecker standardTraversalChecker;
		[SerializeField] private EditModeLevelTileUpgrader levelTileUpgrader;

		public bool IsEditMode { get; set; }

		private IReadOnlyLevelData _levelData;

		public void Init(IReadOnlyLevelData levelData)
		{
			_levelData = levelData;
			standardTraversalChecker.Init(levelData);
		}

		public TileTraversalStatus CanTraverseTo(GridVector position)
		{
			if (_levelData == null)
			{
				throw new NullReferenceException("Tiles object is null");
			}

			if (!IsEditMode)
			{
				return standardTraversalChecker.CanTraverseTo(position);
			}

			if (levelTileUpgrader.UpgradeIfPossible(position))
			{
				return TileTraversalStatus.Yes;
			}

			return _levelData.GetTile(position).IsTraversable(false) ? TileTraversalStatus.Yes : TileTraversalStatus.NonTraversable;
		}
	}
}