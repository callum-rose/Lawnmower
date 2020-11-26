using Game.Core;
using Game.Mowers;
using Game.Tiles;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Levels
{
    internal class LevelManager : MonoBehaviour, IHasEditMode
    {
        [SerializeField] private LevelFactory levelFactory;
        [SerializeField] private LevelTraversalChecker traversalChecker;
        [SerializeField] private LevelInteractor levelInteractor;
        [SerializeField] private LevelStateChecker levelStateChecker;
        [SerializeField] private Positioner positioner;

        public event Action<Tile> TileAdded, TileDestroyed;
        public event Action LevelChanged;
        public event UndoableAction LevelCompleted, LevelFailed;

        public bool IsEditMode { get; set; }

        public ReadOnlyTiles Tiles => new ReadOnlyTiles(_tiles);

        public IReadOnlyLevelData Level => _level;
        public GridVector MowerPosition => _mowerMovement.MowerPosition;

        private LevelData _level;
        private MowerMovementManager _mowerMovement;

        private Tile[,] _tiles;

        #region Unity

        private void Awake()
        {
            TileAdded += levelStateChecker.AddTile;
            TileDestroyed += levelStateChecker.RemoveTile;

            levelStateChecker.LevelCompleted += OnLevelCompleted;
            levelStateChecker.LevelFailed += OnLevelFailed;
        }

        private void OnDestroy()
        {
            TileAdded -= levelStateChecker.AddTile;
            TileDestroyed -= levelStateChecker.RemoveTile;

            levelStateChecker.LevelCompleted -= OnLevelCompleted;
            levelStateChecker.LevelFailed -= OnLevelFailed;
        }

        #endregion

        #region API

        public void Init(MowerMovementManager mowerMovement)
        {
            Assert.IsNotNull(mowerMovement);
            levelInteractor.Init(mowerMovement);

            _mowerMovement = mowerMovement;
        }

        public void SetLevel(LevelData level)
        {
            SetLevelAfterResize(level, GridVector.Zero);
            _mowerMovement.IsRunning = true;
        }

        public void SetLevelAfterResize(LevelData level, GridVector worldOffset)
        {
            ClearTiles();

            _tiles = levelFactory.Build(level);
            foreach (var tile in _tiles)
            {
                TileAdded.Invoke(tile);
            }
            SetDependanciesOfTiles();

            _mowerMovement.SetPosition(level.StartPosition);

            positioner.OffsetContainer(-worldOffset);

            levelStateChecker.Init(_tiles, _mowerMovement);

            Assert.IsNotNull(level);
            _level = level;
            LevelChanged.Invoke();
        }

        public void UpdateTile(GridVector position, TileData data)
        {
            if (_tiles == null)
            {
                throw new Exception("Cannot update tile before level is built");
            }

            // update 
            try
            {
                Tile oldTile = _tiles[position.x, position.y];
                TileDestroyed.Invoke(oldTile);
                levelFactory.Destroy(oldTile);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Tile newTile = levelFactory.BuildAt(position, data);
            TileAdded.Invoke(newTile);
            _tiles[position.x, position.y] = newTile;

            // update internal level data
            _level.SetTile(position, data);

            SetDependanciesOfTiles();
        }

        public TileData GetTileData(GridVector position)
        {
            return _level.GetTile(position);
        }

        public void ClearTiles()
        {
            if (_tiles != null)
            {
                foreach (var t in _tiles)
                {
                    TileDestroyed.Invoke(t);
                }
                levelFactory.Destroy(_tiles);
            }
        }

        #endregion

        #region Events

        private void OnLevelCompleted(Xor isUndo)
        {
            bool isLevelResuming = isUndo;
            _mowerMovement.IsRunning = isLevelResuming;

            LevelCompleted.Invoke(isUndo);
        }

        private void OnLevelFailed(Xor isUndo)
        {
            bool isLevelResuming = isUndo;
            //_mowerMovement.IsRunning = isLevelResuming;

            LevelFailed.Invoke(isUndo);
        }

        #endregion

        #region Methods

        private void SetDependanciesOfTiles()
        {
            ReadOnlyTiles readOnlyTiles = Tiles;
            traversalChecker.SetTiles(readOnlyTiles);
            levelInteractor.SetTiles(readOnlyTiles);
        }

        #endregion
    }
}
