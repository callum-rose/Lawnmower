using Core;
using Game.Core;
using Game.Mowers;
using Game.Tiles;
using Game.UndoSystem;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = nameof(LevelStateChecker), menuName = SONames.GameDir + nameof(LevelStateChecker))]
    internal class LevelStateChecker : ScriptableObject
    {
        public UndoableAction LevelCompleted, LevelFailed;

        private ReadOnlyTiles _tiles;
        private MowerMovementManager _mowerMovement;

        private bool _wasLevelCompleted;

        #region API

        public void Init(ReadOnlyTiles tiles, MowerMovementManager mowerMovement)
        {
            Assert.IsNotNull(tiles);
            _tiles = tiles;

            Assert.IsNotNull(mowerMovement);
            _mowerMovement = mowerMovement;
            _mowerMovement.Moved += OnMowerMoved;
        }

        public void Clear()
        {
            _tiles = null;

            if (_mowerMovement != null)
            {
                _mowerMovement.Moved -= OnMowerMoved;
                _mowerMovement = null;
            }
        }

        public void OnMowerMoved(GridVector _prevPosition, GridVector _targetPosition, Xor isUndo)
        {
            bool isLevelComplete = IsLevelComplete();
            if ((isUndo && _wasLevelCompleted) || isLevelComplete)
            {
                LevelCompleted.Invoke(isUndo);
            }

            _wasLevelCompleted = isLevelComplete;
        }

        public void AddTile(Tile tile)
        {
            switch (tile)
            {
                case GrassTile gTile:
                    gTile.Ruined += OnTileRuined;
                    break;
            }
        }

        public void RemoveTile(Tile tile)
        {
            switch (tile)
            {
                case GrassTile gTile:
                    gTile.Ruined -= OnTileRuined;
                    break;
            }
        }

        #endregion

        #region Events

        private void OnTileRuined(Xor isUndo)
        {
            LevelFailed.Invoke(isUndo);
        }

        #endregion

        #region Methods

        private bool IsLevelComplete()
        {
            foreach (Tile tile in _tiles)
            {
                if (!tile.IsComplete)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
