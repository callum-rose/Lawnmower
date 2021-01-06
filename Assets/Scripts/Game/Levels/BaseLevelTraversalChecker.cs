using Game.Core;
using Game.Levels.Editorr;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
	internal abstract class BaseLevelTraversalChecker : ScriptableObject, IHasEditMode
	{
		[SerializeField] private TilePrefabsHolder tilePrefabsHolder;
		
		public bool IsEditMode { get; set; }
		
		protected ReadOnlyTiles _tiles;

		public void SetTiles(ReadOnlyTiles tiles)
		{
			_tiles = tiles;
		}

		public abstract CheckValue CanTraverseTo(GridVector position);

		public enum CheckValue
		{
			Yes, NonTraversableTile, OutOfBounds
		}
	}
}