using Game.Core;
using Game.Mowers;
using Game.Tiles;
using Game.UndoSystem;
using System;
using Game.Levels.Editorr;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Levels
{
	internal class LevelManager : MonoBehaviour, IHasEditMode
	{
		[SerializeField] private LevelObjectFactory levelFactory;
		[SerializeField] private LevelTraversalChecker traversalChecker;
		[SerializeField] private LevelInteractor levelInteractor;
		[SerializeField] private LevelStateChecker levelStateChecker;
		[SerializeField] private Positioner positioner;

		public event Action<GameObject> TileAdded, TileDestroyed;
		public event Action LevelChanged;
		public event UndoableAction LevelCompleted, LevelFailed;

		public bool IsEditMode { get; set; }

		public IReadOnlyLevelData Level => _level;
		public GridVector MowerPosition => _mowerMovement.MowerPosition;

		private LevelData _level;
		private MowerMovementManager _mowerMovement;

		private GameObject[,] _tileObjects;

		#region Unity

		private void Awake()
		{
			levelStateChecker.LevelCompleted += OnLevelCompleted;
			levelStateChecker.LevelFailed += OnLevelFailed;
		}

		private void OnDestroy()
		{
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

			_tileObjects = levelFactory.Build(level);
			foreach (var tileObject in _tileObjects)
			{
				TileAdded.Invoke(tileObject);
			}

			SetDependenciesOfTiles();
			
			Assert.IsNotNull(level);
			_level = level;

			_mowerMovement.SetPosition(_level.StartPosition);

			positioner.OffsetContainer(-worldOffset);

			levelStateChecker.Init(_level, _mowerMovement);
			
			LevelChanged?.Invoke();
		}

		// public void UpdateTile(GridVector position, TileData data)
		// {
		//     if (_tileObjects == null)
		//     {
		//         throw new Exception("Cannot update tile before level is built");
		//     }
		//
		//     // update 
		//     try
		//     {
		//         Tile oldTile = _tileObjects[position.x, position.y];
		//         TileDestroyed.Invoke(oldTile);
		//         levelFactory.Destroy(oldTile);
		//     }
		//     catch (Exception e)
		//     {
		//         Debug.LogException(e);
		//     }
		//
		//     Tile newTile = levelFactory.BuildAt(position, data);
		//     TileAdded.Invoke(newTile);
		//     _tileObjects[position.x, position.y] = newTile;
		//
		//     // update internal level data
		//     _level.SetTile(position, data);
		//
		//     SetDependenciesOfTiles();
		// }

		// public Tilee GetTileData(GridVector position)
		// {
		// 	return _level.GetTile(position);
		// }

		public void ClearTiles()
		{
			if (_tileObjects != null)
			{
				foreach (GameObject tileObject in _tileObjects)
				{
					TileDestroyed.Invoke(tileObject);
				}

				levelFactory.Destroy(_tileObjects);
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
			LevelFailed.Invoke(isUndo);
		}

		#endregion

		#region Methods

		private void SetDependenciesOfTiles()
		{
			traversalChecker.SetTiles(Level);
			levelInteractor.SetTiles(Level);
		}

		#endregion
	}
}