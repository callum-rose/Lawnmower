using System.Linq;
using Core;
using Core.EventChannels;
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

		private bool IsLevelComplete => _levelData.All(tile => tile.IsComplete);
		private bool IsLevelRuined => _levelData.Any(tile => tile.IsRuined);
		
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
		
		#endregion
		
		#region Events

		private void OnMowerMoved(GridVector prevPosition, GridVector targetPosition, Xor isInverted)
		{
			bool isLevelRuined = IsLevelRuined;
			bool cachedWasLevelRuined = _wasLevelRuined;
			_wasLevelRuined = isLevelRuined;
			if (isLevelRuined || isInverted && cachedWasLevelRuined)
			{
				LevelFailed?.Invoke(isInverted);
				if (levelRuinedChannel)
				{
					levelRuinedChannel.Raise(isInverted);
				}

				return;
			}
			
			bool isLevelComplete = IsLevelComplete;
			if (isLevelComplete || isInverted && _wasLevelCompleted)
			{
				LevelCompleted?.Invoke(isInverted);
			}

			_wasLevelCompleted = isLevelComplete;
		}

		#endregion
	}
}