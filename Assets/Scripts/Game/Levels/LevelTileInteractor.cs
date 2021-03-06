using Core;
using Game.Core;
using Game.Levels.Editorr;
using Game.Mowers;
using Game.Tiles;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Levels
{
    [UnreferencedScriptableObject]
    [CreateAssetMenu(fileName = nameof(LevelTileInteractor), menuName = SoNames.GameDir + nameof(LevelTileInteractor))]
    internal class LevelTileInteractor : ScriptableObject, IHasEditMode
    {
       [SerializeField] private MowerMovementManager mowerMovement;
       
        private IReadOnlyLevelData _levelData;

        public bool IsEditMode { get; set; }

        #region Unity

        public void OnEnable()
        {
            if (mowerMovement)
            {
                mowerMovement.Moved += Interact;
                mowerMovement.Bumped += Bump;
            }
        }

        public void OnDisable()
        {
            if (mowerMovement)
            {
                mowerMovement.Moved -= Interact;
                mowerMovement.Bumped -= Bump;
            }
        }

        #endregion

        #region API

        internal void Construct(MowerMovementManager mowerMovement)
        {
            this.mowerMovement = mowerMovement;
            
            OnEnable();
        }
        
        public void SetLevel(IReadOnlyLevelData levelData)
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

            Tile tileFrom = _levelData.GetTile(positionMovingFrom);
            tileFrom.TraverseAway(direction, inverted);
            
            Tile tileTo = _levelData.GetTile(positionToInteract);
            tileTo.TraverseOnto(direction, inverted);
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
