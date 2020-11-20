using Core;
using Game.Core;
using Game.Tiles;
using System;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = nameof(LevelTraversalChecker), menuName = SONames.GameDir + nameof(LevelTraversalChecker))]
    internal class LevelTraversalChecker : ScriptableObject, IHasEditMode
    {
        [SerializeField] private TilePrefabsHolder tilePrefabsHolder;

        public bool IsEditMode { get; set; }

        private ReadOnlyTiles _tiles;

        #region API

        public void SetTiles(ReadOnlyTiles tiles)
        {
            _tiles = tiles;
        }

        public CheckValue CanTraverseTo(GridVector position)
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
            return tile.IsTraversable(IsEditMode) ? CheckValue.Yes : CheckValue.NonTraversableTile;
        }

        #endregion

        public enum CheckValue
        {
            Yes, NonTraversableTile, OutOfBounds
        }
    }
}
