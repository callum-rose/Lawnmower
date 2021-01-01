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

		public GameSetupPassThroughData(MowerData mower, LevelData level) => (this.mower, this.level) = (mower, level);

		public MowerData Mower => mower;
		public LevelData Level => level;
	}
}