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
		[SerializeField] private UndoableEventChannel levelRuinedChannel;

		public UndoableAction LevelCompleted, LevelFailed;

		private IReadOnlyLevelData _levelData;
		private MowerMovementManager _mowerMovement;

		private bool _wasLevelCompleted;
		private bool _wasLevelRuined;

		#region API

		public void Init(IReadOnlyLevelData levelData, MowerMovementManager mowerMovement)
		{
			Assert.IsNotNull(levelData);
			_levelData = levelData;

			Assert.IsNotNull(mowerMovement);
			_mowerMovement = mowerMovement;
			_mowerMovement.Moved += OnMowerMoved;
		}

		public void Clear()
		{
			_levelData = null;

			if (_mowerMovement != null)
			{
				_mowerMovement.Moved -= OnMowerMoved;
				_mowerMovement = null;
			}
		}

		public void OnMowerMoved(GridVector prevPosition, GridVector targetPosition, Xor isInverted)
		{
			bool isLevelRuined = IsLevelRuined();
			bool cachedWasLevelRuined = _wasLevelRuined;
			_wasLevelRuined = isLevelRuined;
			if (isLevelRuined || isInverted && cachedWasLevelRuined)
			{
				LevelFailed.Invoke(isInverted);
				levelRuinedChannel.Raise(isInverted);
				return;
			}
			
			bool isLevelComplete = IsLevelComplete();
			if (isLevelComplete || isInverted && _wasLevelCompleted)
			{
				LevelCompleted.Invoke(isInverted);
			}

			_wasLevelCompleted = isLevelComplete;
		}

		#endregion

		#region Methods

		private bool IsLevelComplete()
		{
			foreach (Tilee tile in _levelData)
			{
				if (!tile.IsComplete)
				{
					return false;
				}
			}

			return true;
		}

		private bool IsLevelRuined()
		{
			foreach (Tilee tile in _levelData)
			{
				if (!tile.IsRuined)
				{
					return true;
				}
			}

			return false;
		}

		#endregion
	}
}