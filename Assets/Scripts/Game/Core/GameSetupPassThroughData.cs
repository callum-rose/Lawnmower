using Game.Levels;
using Game.Mowers;
using System;
using UnityEngine;

namespace Game.Core
{
	[Serializable]
	internal struct GameSetupPassThroughData
	{
		[SerializeField] private MowerData mower;
		[SerializeField] private IReadOnlyLevelDataContainer levelDataContainer;

		private IReadOnlyLevelData _levelData;

		public GameSetupPassThroughData(MowerData mower, IReadOnlyLevelData level)
		{
			(this.mower, _levelData, levelDataContainer) = (mower, level, null);
		}

		public MowerData Mower => mower;
		public IReadOnlyLevelData Level => _levelData ?? levelDataContainer.Result;
	}
}