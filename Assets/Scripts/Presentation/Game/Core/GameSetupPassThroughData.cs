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
		[SerializeField] private LevelData level;
		[SerializeField] private bool isTutorial;

		public GameSetupPassThroughData(MowerData mower, LevelData level, bool isTutorial = false)
		{
			(this.mower, this.level, this.isTutorial) = (mower, level, isTutorial);
		}
		
		public MowerData Mower => mower;
		public LevelData Level => level;
		public bool IsTutorial => isTutorial;
	}
}
