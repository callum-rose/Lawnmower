using System;
using Core;
using Game.Levels;
using Lawnmower.Presenters;

namespace Game.Core
{
	/// <summary>Today's persistence (unlock count + per-level metadata). G1 replaces it with the progression service.</summary>
	internal sealed class LevelProgressStore : ILevelProgress
	{
		private readonly LevelDataManager _levels;
		private readonly LevelData _level;

		public LevelProgressStore(LevelDataManager levels, LevelData level)
		{
			_levels = levels;
			_level = level;
		}

		public void RecordCompleted(Guid levelId, AttemptStats stats, int par)
		{
			PersistantData.LevelModule.SaveLevelMetaData(levelId, new PersistantData.LevelModule.MetaData
			{
				timeTaken = (float)stats.Duration,
				undoCount = stats.Undos
			});

			if (_level.Id == levelId)
			{
				_levels.SetLevelCompleted(_level);
			}
		}
	}
}
