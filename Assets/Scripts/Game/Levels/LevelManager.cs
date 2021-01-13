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

		public IReadOnlyLevelData Level { get; private set; }

		public GridVector MowerPosition => _mowerMovement.MowerPosition;

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

		public void SetLevel(IReadOnlyLevelData level)
		{
			ClearTiles();

			_tileObjects = levelFactory.Build(level);
			foreach (var tileObject in _tileObjects)
			{
				TileAdded.Invoke(tileObject);
			}

			SetDependenciesOfTiles();
			
			Assert.IsNotNull(level);
			Level = level;

			_mowerMovement.SetPosition(Level.StartPosition);

			positioner.OffsetContainer(-GridVector.Zero);

			levelStateChecker.Init(Level, _mowerMovement);
			
			LevelChanged?.Invoke();
			
			_mowerMovement.IsRunning = true;
		}

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