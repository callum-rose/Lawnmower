using Game.Core;
using Game.Mowers;
using Game.Tiles;
using Game.UndoSystem;
using System;
using Game.Levels.Editorr;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using R = Sirenix.OdinInspector.RequiredAttribute;

namespace Game.Levels
{
	internal class LevelManager : MonoBehaviour, IHasEditMode
	{
		[SerializeField, R] private MowerMovementManager mowerMovementManager;
		[SerializeField, R] private LevelObjectFactory levelFactory;
		[SerializeField, R] private LevelTraversalChecker traversalChecker;
		[FormerlySerializedAs("levelInteractor")] [SerializeField, R] private TileInteractor tileInteractor;
		[SerializeField, R] private LevelStateChecker levelStateChecker;
		[SerializeField, R] private Positioner positioner;

		public event Action LevelChanged;
		public event UndoableAction LevelCompleted, LevelFailed;

		public bool IsEditMode { get; set; }

		public IReadOnlyLevelData Level { get; private set; }

		public GridVector MowerPosition => mowerMovementManager.MowerPosition;
		
		private GameObject[,] _tileObjects;

		#region Unity

		private void Awake()
		{
			Assert.IsNotNull(mowerMovementManager);
			tileInteractor.Init(mowerMovementManager);
			
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

		public void SetLevel(IReadOnlyLevelData level)
		{
			ClearTiles();

			_tileObjects = levelFactory.Build(level);

			SetDependenciesOfTiles();
			
			Assert.IsNotNull(level);
			Level = level;

			mowerMovementManager.SetPosition(Level.StartPosition);

			positioner.OffsetContainer(-GridVector.Zero);

			levelStateChecker.Init(Level, mowerMovementManager);
			
			LevelChanged?.Invoke();
			
			mowerMovementManager.IsRunning = true;
		}

		public void ClearTiles()
		{
			if (_tileObjects != null)
			{
				levelFactory.Destroy(_tileObjects);
			}
		}

		#endregion

		#region Events

		private void OnLevelCompleted(Xor isUndo)
		{
			bool isLevelResuming = isUndo;
			mowerMovementManager.IsRunning = isLevelResuming;

			LevelCompleted!.Invoke(isUndo);
		}

		private void OnLevelFailed(Xor isUndo)
		{
			LevelFailed!.Invoke(isUndo);
		}

		#endregion

		#region Methods

		private void SetDependenciesOfTiles()
		{
			traversalChecker.SetTiles(Level);
			tileInteractor.SetTiles(Level);
		}

		#endregion
	}
}