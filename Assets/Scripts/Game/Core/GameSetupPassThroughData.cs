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
		[SerializeField] private bool isTutorial;
		
		private IReadOnlyLevelData _levelData;

		public GameSetupPassThroughData(MowerData mower, IReadOnlyLevelData level, bool isTutorial = false)
		{
			(this.mower, _levelData, levelDataContainer, this.isTutorial) = (mower, level, null, isTutorial);
		}
		
		public MowerData Mower => mower;
		public IReadOnlyLevelData Level => _levelData ?? levelDataContainer.Result;
	}
}