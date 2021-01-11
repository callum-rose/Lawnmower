using Game.Core;
using Game.Levels.Editorr;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
	internal abstract class BaseLevelTraversalChecker : ScriptableObject, IHasEditMode
	{
		[SerializeField] private TilePrefabsManager tilePrefabsManager;
		
		public bool IsEditMode { get; set; }
		
		protected IReadOnlyLevelData LevelData;

		public void SetTiles(IReadOnlyLevelData levelData)
		{
			LevelData = levelData;
		}

		public abstract CheckValue CanTraverseTo(GridVector position);

		public enum CheckValue
		{
			Yes, NonTraversableTile, OutOfBounds
		}
	}
}