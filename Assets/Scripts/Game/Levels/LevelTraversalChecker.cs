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
            if (LevelData == null)
            {
                throw new NullReferenceException("Tiles object is null");
            }

            if (position.x < 0 || position.y < 0 || position.x >= LevelData.Width || position.y >= LevelData.Depth)
            {
                return CheckValue.OutOfBounds;
            }

            Tilee tile = LevelData.GetTile(position);
            return tile.IsTraversable(IsEditMode) ? CheckValue.Yes : CheckValue.NonTraversableTile;
        }

        #endregion
    }
}
