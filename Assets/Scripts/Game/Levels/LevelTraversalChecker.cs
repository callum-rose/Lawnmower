using Core;
using Game.Core;
using Game.Tiles;
using System;
using Game.Levels.Editorr;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = nameof(LevelTraversalChecker), menuName = SONames.GameDir + nameof(LevelTraversalChecker))]
    internal class LevelTraversalChecker : BaseLevelTraversalChecker
    {
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
            return tile.IsTraversable(IsEditMode) ? CheckValue.Yes : CheckValue.NonTraversableTile;
        }

        #endregion
    }
}
