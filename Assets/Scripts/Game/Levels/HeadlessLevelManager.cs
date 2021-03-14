using Game.Core;
using Game.Mowers;
using Game.UndoSystem;
using System;
using Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using R = Sirenix.OdinInspector.RequiredAttribute;

namespace Game.Levels
{
	[UnreferencedScriptableObject]
	[CreateAssetMenu(fileName = nameof(HeadlessLevelManager), menuName = SoNames.GameDir + nameof(HeadlessLevelManager))]
	internal class HeadlessLevelManager : ScriptableObject, ILevelManager
	{
		[TitleGroup("Assets")] 
		[SerializeField, R] private MowerMovementManager mowerMovementManager;
		[SerializeField, R] private ILevelTraversalCheckerContainer levelTraversalCheckerContainer;
		[SerializeField, R] private LevelTileInteractor tileInteractor;
		[SerializeField, R] private LevelStateChecker levelStateChecker;
		
		[TitleGroup("Event Channels")]
		[SerializeField, R] private ILevelDataEventChannelTransmitterContainer levelStartedEventChannelContainer;

		private ILevelDataEventChannelTransmitter LevelStartedEventChannel => levelStartedEventChannelContainer?.Result;

		public event Action LevelChanged;
		public event UndoableAction LevelCompleted;
		public event UndoableAction LevelFailed;

		public bool IsEditMode { get; set; }

		public IReadOnlyLevelData Level { get; private set; }

		public GridVector MowerPosition => mowerMovementManager.MowerPosition;

		private ILevelTraversalChecker LevelTraversalChecker => levelTraversalCheckerContainer.Result;
		
		#region Unity

		private void OnEnable()
		{
			if (levelStateChecker)
			{
				levelStateChecker.LevelCompleted += OnLevelCompleted;
				levelStateChecker.LevelFailed += OnLevelFailed;
			}
		}

		private void OnDisable()
		{
			if (levelStateChecker)
			{
				levelStateChecker.LevelCompleted -= OnLevelCompleted;
				levelStateChecker.LevelFailed -= OnLevelFailed;
			}
		}

		#endregion

		#region API

		internal void Construct(MowerMovementManager mowerMovementManager,
			ILevelTraversalChecker levelTraversalChecker,
			LevelTileInteractor tileInteractor,
			LevelStateChecker levelStateChecker)
		{
			this.mowerMovementManager = mowerMovementManager;
			levelTraversalCheckerContainer = new ILevelTraversalCheckerContainer { Result = levelTraversalChecker };
			this.tileInteractor = tileInteractor;
			this.levelStateChecker = levelStateChecker;
			
			OnEnable();
		}
		
		public void Init(IReadOnlyLevelData level)
		{
			Assert.IsNotNull(level);
			Level = level;

			foreach (Tile tile in level)
			{
				switch (tile)
				{
					case SpringTile springTile:
						springTile.TraversedOnto += (direction, inverted) => mowerMovementManager.MoveToPosition(springTile.LandingPosition);
						break;
				}
			}
			
			LevelTraversalChecker.Init(Level);
			tileInteractor.SetLevel(Level);

			mowerMovementManager.SetInitialPosition(Level.StartPosition);

			levelStateChecker.Init(Level, mowerMovementManager);

			LevelChanged?.Invoke();
			LevelStartedEventChannel?.Raise(level);

			mowerMovementManager.IsRunning = true;
		}

		public void Clear()
		{
			if (levelStateChecker)
			{
				levelStateChecker.Clear();
			}
		}

		#endregion

		#region Events

		private void OnLevelCompleted(Xor isUndo)
		{
			bool isLevelResuming = isUndo;
			mowerMovementManager.IsRunning = isLevelResuming;

			LevelCompleted?.Invoke(isUndo);
		}

		private void OnLevelFailed(Xor isUndo)
		{
			LevelFailed?.Invoke(isUndo);
		}

		#endregion
	}
}