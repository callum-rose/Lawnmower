using Core;
using Game.Core;
using Game.Levels.Editorr;
using Game.Mowers;
using Game.Tiles;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = nameof(TileInteractor), menuName = SONames.GameDir + nameof(TileInteractor))]
    internal class TileInteractor : ScriptableObject, IHasEditMode
    {
        private MowerMovementManager _mowerMovement;
        private IReadOnlyLevelData _levelData;

        public bool IsEditMode { get; set; }

        #region API

        public void Init(MowerMovementManager mowerMovement)
        {
            _mowerMovement = mowerMovement;
            _mowerMovement.Moved += Interact;
            _mowerMovement.Bumped += Bump;
        }

        public void SetTiles(IReadOnlyLevelData levelData)
        {
            _levelData = levelData;
        }

        #endregion

        #region Events

        private void Interact(GridVector prevPosition, GridVector targetPosition, Xor isUndo)
        {
            Xor inverted = Xor.Combine(IsEditMode, isUndo);

            GridVector positionToInteract;
            GridVector positionMovingFrom;
            if (inverted)
            {
                positionToInteract = prevPosition;
                positionMovingFrom = targetPosition;
            }
            else
            {
                positionToInteract = targetPosition;
                positionMovingFrom = prevPosition;
            }

            GridVector direction = positionToInteract - positionMovingFrom;

            Tile tileTo = _levelData.GetTile(positionToInteract);
            tileTo.TraverseOnto(direction, inverted);

            Tile tileFrom = _levelData.GetTile(positionMovingFrom);
            tileFrom.TraverseAway(direction, inverted);
        }

        private void Bump(GridVector prevPosition, GridVector targetPosition, Xor isUndo)
        {
            if (IsEditMode || isUndo)
            {
                return;
            }

            Tile tile = _levelData.GetTile(targetPosition);
            tile.BumpInto(targetPosition - prevPosition, isUndo);
        }

        #endregion
    }
}