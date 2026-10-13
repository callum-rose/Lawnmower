using System.Collections.Generic;
using Game.Levels;
using Lawnmower.Presenters;

namespace LevelSelect
{
	internal sealed class LevelDataManagerCatalog : ILevelCatalog
	{
		private readonly LevelDataManager _levels;

		public LevelDataManagerCatalog(LevelDataManager levels)
		{
			_levels = levels;
		}

		public IReadOnlyList<LevelSummary> GetLevels()
		{
			IList<LevelInfo> infos = _levels.GetAllLevels();
			List<LevelSummary> result = new List<LevelSummary>(infos.Count);

			for (int i = 0; i < infos.Count; i++)
			{
				result.Add(new LevelSummary(i, infos[i].LevelData.Id, infos[i].Locked));
			}

			return result;
		}
	}
}
